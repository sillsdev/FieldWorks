/***********************************************************************************************
 * Function: GetHeadwordsForEntriesOrSenses
 *
 * Description:
 *	Generate the "headword" string for all LexEntry or LexSense objects that are the targets
 * 	of either LexReference_Targets or LexEntry_MainEntriesOrSenses.
 *
 * Parameters:
 * 	None.
 * Returns:
 *	A table containing headword strings for LexEntry and LexSense objects which are
 * 	targets of cross references in the lexicon. The table also contains the object id and class
 *  id for the LexEntry and LexSense objects.
 **********************************************************************************************/
if object_id('GetHeadwordsForEntriesOrSenses') is not null begin
	print 'removing function GetHeadwordsForEntriesOrSenses'
	drop function [GetHeadwordsForEntriesOrSenses]
end
print 'creating function GetHeadwordsForEntriesOrSenses'
go

CREATE FUNCTION dbo.GetHeadwordsForEntriesOrSenses ()
RETURNS @ObjIdInfo TABLE (
	ObjId int,
	ClassId int,
	Headword nvarchar(4000))
AS
BEGIN

DECLARE @nvcAllo nvarchar(4000), @nHomograph int, @nvcPostfix nvarchar(4000),
		@nvcPrefix nvarchar(4000), @nvcHeadword nvarchar(4000)
DECLARE @objId int, @objOwner int, @objOwnFlid int, @objClass int, @hvoEntry int,
	@nvcSenseNum nvarchar(4000), @objId2 int
INSERT INTO @ObjIdInfo (ObjId, ClassId, Headword)
	SELECT Dst, NULL, NULL FROM LexReference_Targets
	UNION
	SELECT Dst, NULL, NULL FROM LexEntry_MainEntriesOrSenses
DECLARE cur CURSOR local static forward_only read_only FOR
	SELECT id, Class$, Owner$, OwnFlid$
		FROM CmObject
		WHERE Id in (SELECT ObjId FROM @ObjIdInfo)
OPEN cur
FETCH NEXT FROM cur INTO @objId, @objClass, @objOwner, @objOwnFlid
WHILE @@FETCH_STATUS = 0
BEGIN
	IF @objClass = 5002 BEGIN -- LexEntry
		SET @hvoEntry=@objId
	END
	ELSE BEGIN
		IF @objOwnFlid = 5002011 BEGIN -- LexEntry_Senses
			SET @hvoEntry=@objOwner
		END
		ELSE BEGIN
			while @objOwnFlid != 5002011
			begin
				set @objId2=@objOwner
				select 	@objOwner=isnull(Owner$, 0), @objOwnFlid=OwnFlid$
				from	CmObject
				where	Id=@objId2
				if @objOwner = 0 begin
					SET @objOwnFlid = 5002011
				end
			end
			SET @hvoEntry=@objOwner
		END
	END

	SELECT @nvcAllo=f.Txt, @nHomograph=le.HomographNumber, @nvcPostfix=t.Postfix,
			@nvcPrefix=t.Prefix, @nvcSenseNum=s.SenseNum
		FROM LexEntry le
		LEFT OUTER JOIN LexEntry_LexemeForm a on a.Src=le.id
		LEFT OUTER JOIN MoForm_Form f on f.Obj=a.Dst
		LEFT OUTER JOIN MoForm mf on mf.Id=a.Dst
		LEFT OUTER JOIN MoMorphType t on t.Id=mf.MorphType
		LEFT OUTER JOIN dbo.fnGetSensesInEntry$ (@hvoEntry) s on s.SenseId=@objId
		WHERE le.Id = @hvoEntry

	IF @nvcPrefix is null SET @nvcHeadword=@nvcAllo
	ELSE SET @nvcHeadword=@nvcPrefix+@nvcAllo
	IF @nvcPostfix is not null SET @nvcHeadword=@nvcHeadword+@nvcPostfix
	IF @nHomograph <> 0 SET @nvcHeadword=@nvcHeadword+CONVERT(nvarchar(20), @nHomograph)
	IF @nvcSenseNum is not null SET @nvcHeadword=@nvcHeadword+' '+@nvcSenseNum
	UPDATE @ObjIdInfo SET Headword=@nvcHeadword, ClassId=@objClass
		WHERE ObjId=@objId

	FETCH NEXT FROM cur INTO @objId, @objClass, @objOwner, @objOwnFlid
END
CLOSE cur
DEALLOCATE cur

RETURN
END
go
