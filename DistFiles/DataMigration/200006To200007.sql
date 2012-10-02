-- update database from version 200006 to 200007
BEGIN TRANSACTION

UPDATE ClassPar$ SET Depth = 2 WHERE Src = 5054 AND Depth = 1
INSERT INTO ClassPar$ VALUES (5054, 5, 1)
update Class$ set Base = 5 where id = 5054

drop view Text_
go
create view [Text_] as select [CmMajorObject_].*,[Text].[SoundFilePath]from [CmMajorObject_] join [Text] on [CmMajorObject_].[Id] = [Text].[Id]
go
insert into CmMajorObject (id) select id from Text

IF OBJECT_ID('fnGetOwnedIds') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200006
		PRINT 'removing function fnGetOwnedIds'
	DROP FUNCTION fnGetOwnedIds
END
GO
if (select DbVer from Version$) = 200006
	PRINT 'creating function fnGetOwnedIds'
GO

CREATE FUNCTION fnGetOwnedIds (
	@nOwner INT,
	@nTopFlid INT,
	@nSubFlid INT)
RETURNS @tblObjects TABLE (
	[Id] INT,
	Guid$ UNIQUEIDENTIFIER,
	Class$ INT ,
	Owner$ INT,
	OwnFlid$ INT,
	OwnOrd$ INT,
	UpdStmp BINARY(8),
	UpdDttm SMALLDATETIME,
	[Level] INT)
AS
BEGIN
	DECLARE
		@nLevel INT,
		@nRowCount INT

	IF @nTopFlid IS NULL
		SET @nTopFlid = 8008 --( Possibility
	IF @nSubFlid IS NULL
		SET @nSubFlid = 7004 --( Subpossibility

	--( Get the first level of owned objects
	SET @nLevel = 1

	INSERT INTO @tblObjects
	SELECT
		[Id],
		Guid$,
		Class$,
		Owner$,
		OwnFlid$,
		OwnOrd$,
		UpdStmp,
		UpdDttm,
		@nLevel
	FROM CmObject (READUNCOMMITTED)
	WHERE Owner$ = @nOwner AND OwnFlid$ = @nTopFlid --( e.g. possibility, 8008

	SET @nRowCount = @@ROWCOUNT --( Using @@ROWCOUNT alone was flakey in the loop.

	--( Get the sublevels of owned objects
	WHILE @nRowCount != 0 BEGIN

		INSERT INTO @tblObjects
		SELECT
			o.[Id],
			o.Guid$,
			o.Class$,
			o.Owner$,
			o.OwnFlid$,
			o.OwnOrd$,
			o.UpdStmp,
			o.UpdDttm,
			(@nLevel + 1)
		FROM @tblObjects obj
		JOIN CmObject o (READUNCOMMITTED) ON o.Owner$ = obj.[Id]
			AND  o.OwnFlid$ = @nSubFlid --( e.g. subpossibility, 7004
		WHERE obj.[Level] = @nLevel

		SET @nRowCount = @@ROWCOUNT
		SET @nLevel = @nLevel + 1
	END

	RETURN
END
GO


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200006
begin
	update Version$ set DbVer = 200007
	COMMIT TRANSACTION
	print 'database updated to version 200007'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200006 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
