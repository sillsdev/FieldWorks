-- Update database from version 200055 to 200056
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Define the new fields for LexicalDatabase.
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5005019, 23, 5005, 8, 'References',0,Null, null, null, null)

-- Define the new LexReference class.
insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5120, 5, 0, 0, 'LexReference')
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5120001, 14, 5120, null, 'Comment',0, null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5120002, 28, 5120, 0, 'Targets',0, null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5120003, 16, 5120, null, 'Name',0, null, null, null, null)

-- Define the new LexReferenceType class.
insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5119, 5, 7, 0, 'LexReferenceType')
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5119001, 16, 5119, null, 'ReverseAbbreviation',0, null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5119002, 2, 5119, null, 'MappingType',0, null, 0, 127, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5119003, 25, 5119, 5120, 'Members',0, null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5119004, 16, 5119, null, 'ReverseName',0, null, null, null, null)

-- According to Andy Black there should be no user data associated with these feature changes.
-- Therefore, we won't try to migrate any Feature data.

-- Define CatalogSourceId in PartOfSpeech
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5049013, 15, 5049, null, 'CatalogSourceId',0, null, null, null, null)

-- Define Features in FsFeatureSystem
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(49003, 25, 49, 55, 'Features',0, null, null, null, null)

-- Redefine Features in FsFeatureStructureType from owning to reference collection
delete from [Field$] where [id]=59004
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(59004, 28, 59, 55, 'Features',0, null, null, null, null)

-- DataMigration:
-- 1. Create References List in LexicalDatabase (+ add to NewLangProj.xml)
DECLARE @hvoLexDb int, @wsEn int, @hvoLexRefList int
SELECT @hvoLexDb=Dst from LanguageProject_LexicalDatabase
SELECT @wsEn=id from LgWritingSystem WHERE ICULocale=N'en'
EXEC CreateObject_CmPossibilityList @wsEn, N'Lexical Reference Types', null, null,	null, null, null, 1, 0,	0, 0, 1, 0, @wsEn, N'RefTyp',
	null, 0, 0, 5119, 0, -3, @hvoLexDb, 5005019, null, @hvoLexRefList output, null, 0, null

-- 2. Create LexReferenceType for each LexicalRelationshipGroup. Copy the AVariableName/Abbr to CmPossibility.Name/Abbr
-- Copy the BVariableName/Abbr to LexReferenceType.ReverseName/Abbr. Set LexReferenceType.MappingType to the appropriate
-- value based on LexicalRelationGroup.SetType (even though we don't yet know all the mappings).
-- 2.5 For each LexicalRelationGroup, once the LexReferenceType is created, go through the Members to create the LexReference
-- objects and link them to the LexReferenceType.

DECLARE grpCursor CURSOR local static forward_only read_only FOR
	SELECT g.[id], g.[SetType]
	FROM LexicalRelationGroup g

DECLARE @hvoRelation int, @cSetComment int, @hvoReference int, @cSetName int, @hvoSetMember int
DECLARE @ordSetMember int, @hvoRelGroup int, @hvoRefType int, @nSetType int

OPEN grpCursor
FETCH NEXT FROM grpCursor INTO @hvoRelGroup, @nSetType
WHILE @@FETCH_STATUS = 0
BEGIN

	DECLARE @nMappingType int
	SELECT @nMappingType =
		CASE
			WHEN @nSetType=1 THEN 0
			WHEN @nSetType=2 THEN 1
			WHEN @nSetType=3 THEN 3
			WHEN @nSetType=4 THEN 2
			ELSE 0
		END

	EXEC CreateObject_LexReferenceType @CmPossibility_Name_ws=null, @CmPossibility_Name_txt=null, 		@CmPossibility_Abbreviation_ws=null, @CmPossibility_Abbreviation_txt=null, @CmPossibility_Description_ws=null,
		@CmPossibility_Description_txt=null, @CmPossibility_Description_fmt=null, @CmPossibility_SortSpec=0,		@CmPossibility_DateCreated = null, @CmPossibility_DateModified = null, 	@CmPossibility_HelpId = null,		@CmPossibility_ForeColor = 0, @CmPossibility_BackColor = 0, @CmPossibility_UnderColor= 0, @CmPossibility_UnderStyle = 0,		@CmPossibility_Hidden = 0, 	@CmPossibility_IsProtected = 0, @LexReferenceType_ReverseAbbreviation_ws = null,
		@LexReferenceType_ReverseAbbreviation_txt = null,
		@LexReferenceType_MappingType = @nMappingType,
		@LexReferenceType_ReverseName_ws = null,
		@LexReferenceType_ReverseName_txt = null, 		@Owner = @hvoLexRefList,
		@OwnFlid = 8008,
		@StartObj = null,
		@NewObjId = @hvoRefType output,
		@NewObjGuid = null,
		@fReturnTimestamp = 0,
		@NewObjTimestamp = null

	INSERT INTO CmPossibility_Name (Obj, Ws, Txt)
		SELECT @hvoRefType, Ws, Txt FROM MultiStr$ WHERE Obj=@hvoRelGroup AND Flid=5006005 -- AVariableName

	INSERT INTO CmPossibility_Abbreviation (Obj, Ws, Txt)
		SELECT @hvoRefType, Ws, Txt FROM MultiStr$ WHERE Obj=@hvoRelGroup AND Flid=5006004 -- AVariableAbbr

	INSERT INTO LexReferenceType_ReverseName (Obj, Ws, Txt)
		SELECT @hvoRefType, Ws, Txt FROM MultiStr$ WHERE Obj=@hvoRelGroup AND Flid=5006008 -- BVariableName

	INSERT INTO LexReferenceType_ReverseAbbreviation (Obj, Ws, Txt)
		SELECT @hvoRefType, Ws, Txt FROM MultiStr$ WHERE Obj=@hvoRelGroup AND Flid=5006007 -- BVariableAbbr

	declare @cSets int, @cPairs int, @cTrees int, @cScales int
	select @cSets=Count(lss.[id]), @cPairs=Count(lpr.[id]), @cTrees=Count(ltr.[id]), @cScales=Count(lsc.[id])
	from LexicalRelationGroup g
		left outer join LexicalRelationGroup_Members m on m.Src=g.[id]
		left outer join LexSimpleSet lss on lss.[id]=m.[dst]
		left outer join LexPairRelation lpr on lpr.[id]=m.[dst]
		left outer join LexTreeRelation ltr on ltr.[id]=m.[dst]
		left outer join LexScale lsc on lsc.[id]=m.[dst]
		where g.[id]=@hvoRelGroup

	DECLARE @hvoSense int

	-- variables used for processing trees
	DECLARE @nLevel int, @cRows int, @cRowsPrev int, @nLevelLim int, @hvoSrc int, @hvoDst int, @hvoMember int
	DECLARE @TreeData TABLE (
			[Src] int NOT NULL ,
			[Dst] int NOT NULL ,
			[Ord] int NOT NULL ,
			[Level] int NOT NULL,
			[Member] int NOT NULL,
			[Sense] int NULL
	)
	DECLARE @TreeChildren TABLE (
		Src	int,
		Dst int,
		Ord int,
		Sense int
		)

	-- LexSimpleSet to sense collection
	IF @cSets<>0
	BEGIN
		DECLARE setCursor CURSOR local static forward_only read_only FOR
			SELECT s.[id]
			FROM LexSimpleSet s JOIN CmObject c on c.[id]=s.[id] WHERE c.[Owner$]=@hvoRelGroup
		OPEN setCursor
		FETCH NEXT FROM setCursor INTO @hvoRelation
		WHILE @@FETCH_STATUS = 0
		BEGIN

			EXEC CreateObject_LexReference
				@LexReference_Comment_ws = null, @LexReference_Comment_txt = null, @LexReference_Comment_fmt = null, 				@LexReference_Name_ws  = null, @LexReference_Name_txt = null, 				@Owner = @hvoRefType,
				@OwnFlid = 5119003,	-- Members
				@StartObj = null,
				@NewObjId = @hvoReference output,
				@NewObjGuid = null,
				@fReturnTimestamp = 0,
				@NewObjTimestamp = null

			SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017002 -- LexSet_Comment
			IF @cSetComment<>0
				BEGIN
					UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
						WHERE Obj=@hvoRelation AND Flid=5017002 -- LexSet_Comment
				END
			SELECT @cSetName=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017003
			IF @cSetName<>0
				BEGIN
					INSERT INTO LexReference_Name (Obj, Ws, Txt)
						SELECT @hvoReference, Ws, Txt
						FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017003 -- Name
				END
			ELSE
				BEGIN
					INSERT INTO LexReference_Name (Obj, Ws, Txt)
						SELECT @hvoReference, Ws, Txt
						FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017001 -- Abbreviation
				END

			DECLARE setMemCursor  CURSOR local static forward_only read_only FOR
			SELECT m.[Dst]
			FROM LexSimpleSet_Members m WHERE m.[Src]=@hvoRelation

			SET @ordSetMember=1
			OPEN setMemCursor
			FETCH NEXT FROM setMemCursor INTO @hvoSetMember
			WHILE @@FETCH_STATUS = 0
				BEGIN
					SELECT @hvoSense=Sense FROM LexSetItem WHERE [id]=@hvoSetMember
					IF @hvoSense is not null
						BEGIN
							INSERT INTO LexReference_Targets (Src, Dst, Ord)
							VALUES(@hvoReference, @hvoSense, @ordSetMember)
						END

					IF @cSetComment=0
						BEGIN
							SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoSetMember AND Flid=5018001 -- LexSetItem Comment
							IF @cSetComment<>0
								BEGIN
									UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
										WHERE Obj=@hvoSetMember AND Flid=5018001 -- LexSetItem Comment
								END
						END

					SET @ordSetMember=@ordSetMember+1
					FETCH NEXT FROM setMemCursor INTO @hvoSetMember
				END

			CLOSE setMemCursor
			DEALLOCATE setMemCursor
			FETCH NEXT FROM setCursor INTO @hvoRelation
		END
		CLOSE setCursor
		DEALLOCATE setCursor
	END

	-- LexPairRelation to sense pair
	IF @cPairs<>0
	BEGIN
		DECLARE pairCursor CURSOR local static forward_only read_only FOR
			SELECT p.[id]
			FROM LexPairRelation p
		OPEN pairCursor
		FETCH NEXT FROM pairCursor INTO @hvoRelation
		WHILE @@FETCH_STATUS = 0
		BEGIN
			DECLARE memCursor CURSOR local static forward_only read_only FOR
				SELECT m.[Dst]
				FROM LexPairRelation_Members m
			DECLARE @hvoPair int
			OPEN memCursor
			FETCH NEXT FROM memCursor INTO @hvoPair
			WHILE @@FETCH_STATUS = 0
			BEGIN

				EXEC CreateObject_LexReference
					@LexReference_Comment_ws = null, @LexReference_Comment_txt = null, @LexReference_Comment_fmt = null, 					@LexReference_Name_ws  = null, @LexReference_Name_txt = null, 					@Owner = @hvoRefType,
					@OwnFlid = 5119003,	-- Members
					@StartObj = null,
					@NewObjId = @hvoReference output,
					@NewObjGuid = null,
					@fReturnTimestamp = 0,
					@NewObjTimestamp = null

				SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoPair AND Flid=5010001 -- LexPair_Comment
				IF @cSetComment<>0
					BEGIN
						UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
							WHERE Obj=@hvoPair AND Flid=5010001 -- LexPair_Comment
					END

				IF @cSetComment=0
					BEGIN
						SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017002 -- LexSet_Comment
						IF @cSetComment<>0
							BEGIN
								UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
									WHERE Obj=@hvoRelation AND Flid=5017002 -- LexSet_Comment
							END
					END
				SELECT @cSetName=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017003
				IF @cSetName<>0
					BEGIN
						INSERT INTO LexReference_Name (Obj, Ws, Txt)
							SELECT @hvoReference, Ws, Txt
							FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017003 -- Name
					END
				ELSE
					BEGIN
						INSERT INTO LexReference_Name (Obj, Ws, Txt)
							SELECT @hvoReference, Ws, Txt
							FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017001 -- Abbreviation
					END

				INSERT INTO LexReference_Targets (Src, Dst, Ord)
					SELECT @hvoReference, Sense, 1
					FROM LexPair_MemberA m JOIN LexSetItem lsi on lsi.[id]=m.[Dst]
					WHERE m.[Src]=@hvoPair
				INSERT INTO LexReference_Targets (Src, Dst, Ord)
					SELECT @hvoReference, Sense, 2
					FROM LexPair_MemberB m JOIN LexSetItem lsi on lsi.[id]=m.[Dst]
					WHERE m.[Src]=@hvoPair

				IF @cSetComment=0
					BEGIN
						SELECT @cSetComment=COUNT(m.Obj)
						FROM LexPair_MemberA a
							JOIN MultiStr$ m on m.[Obj]=a.[Dst] AND m.[Flid]=5018001 -- LexSetItem_Comment
							WHERE a.[Src]=@hvoPair

						IF @cSetComment<>0
							BEGIN
								UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
									WHERE Obj=(SELECT Dst FROM LexPair_MemberA WHERE Src=@hvoPair) AND Flid=5018001 -- LexSetItem_Comment
							END
					END

				IF @cSetComment=0
					BEGIN
						SELECT @cSetComment=COUNT(m.Obj)
						FROM LexPair_MemberB a
							JOIN MultiStr$ m on m.[Obj]=a.[Dst] AND m.[Flid]=5018001 -- LexSetItem_Comment
							WHERE a.[Src]=@hvoPair

						IF @cSetComment<>0
							BEGIN
								UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
									WHERE Obj=(SELECT Dst FROM LexPair_MemberB WHERE Src=@hvoPair) AND Flid=5018001 -- LexSetItem_Comment
							END
					END

				FETCH NEXT FROM memCursor INTO @hvoPair
			END
			CLOSE memCursor
			DEALLOCATE memCursor
			FETCH NEXT FROM pairCursor INTO @hvoRelation
		END
		CLOSE pairCursor
		DEALLOCATE pairCursor
	END

	-- LexTreeRelation to sense tree
	IF @cTrees<>0
	BEGIN
		DECLARE treeCursor CURSOR local static forward_only read_only FOR
			SELECT t.[id]
			FROM LexTreeRelation t
		OPEN treeCursor
		FETCH NEXT FROM treeCursor INTO @hvoRelation
		WHILE @@FETCH_STATUS = 0
		BEGIN
			SET @nLevel=0
			SET @cRowsPrev=0
			INSERT INTO @TreeData
				SELECT ltri.Src, ltri.Dst, ltri.Ord, @nLevel, lsi.[id], lsi.Sense
				FROM LexTreeRelation ltr
					LEFT OUTER JOIN LexTreeRelation_Items ltri on ltri.Src=ltr.[id]
					LEFT OUTER JOIN LexTreeItem_Member ltim on ltim.[Src]=ltri.[Dst]
					LEFT OUTER JOIN LexSetItem lsi on lsi.[id]=ltim.[Dst]
					WHERE ltri.Src=@hvoRelation
			SELECT @cRows=COUNT(*) FROM @TreeData
			WHILE @cRows > @cRowsPrev
				BEGIN
					SET @cRowsPrev = @cRows
					SET @nLevel = @nLevel + 1
					INSERT INTO @TreeData
						SELECT ltii.Src, ltii.Dst, ltii.Ord, @nLevel, lsi.[id], lsi.Sense
						FROM LexTreeItem_Items ltii
						LEFT OUTER JOIN LexTreeItem_Member ltim on ltim.Src=ltii.Dst
						LEFT OUTER JOIN LexSetItem lsi on lsi.[id]=ltim.[Dst]
						WHERE ltii.Src in (SELECT Dst FROM @TreeData WHERE [Level]=@nLevel-1)
					SELECT @cRows=COUNT(*) FROM @TreeData
				END
			SET @nLevelLim = @nLevel
			SET @nLevel = 0
			WHILE @nLevel < @nLevelLim
				BEGIN
					DECLARE levCursor CURSOR local static forward_only read_only FOR
						SELECT Src, Dst, Member, Sense
						FROM @TreeData
						WHERE [Level] = @nLevel AND Sense IS NOT NULL
					OPEN levCursor
					FETCH NEXT FROM levCursor INTO @hvoSrc, @hvoDst, @hvoMember, @hvoSense
					WHILE @@FETCH_STATUS = 0
					BEGIN
						DELETE FROM @TreeChildren
						INSERT INTO @TreeChildren
							SELECT Src, Dst, Ord, Sense
							FROM @TreeData
							WHERE Src = @hvoDst AND [Level] = @nLevel+1 AND Sense is not null
						SELECT @cRows=COUNT(*) FROM @TreeChildren
						IF @nLevel = 0 OR @cRows > 0
						BEGIN
							-- create LexReference
							EXEC CreateObject_LexReference
								@LexReference_Comment_ws = null, @LexReference_Comment_txt = null, @LexReference_Comment_fmt = null, 								@LexReference_Name_ws  = null, @LexReference_Name_txt = null, 								@Owner = @hvoRefType,
								@OwnFlid = 5119003,	-- Members
								@StartObj = null,
								@NewObjId = @hvoReference output,
								@NewObjGuid = null,
								@fReturnTimestamp = 0,
								@NewObjTimestamp = null

							INSERT INTO LexReference_Targets (Src, Dst, Ord)
								VALUES (@hvoReference, @hvoSense, 1)
							INSERT INTO LexReference_Targets
								SELECT @hvoReference, Sense, Ord + 1 FROM @TreeChildren

							-- Set the Name and Comment
							SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017002 -- LexSet_Comment
							IF @cSetComment<>0
								BEGIN
									UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
										WHERE Obj=@hvoRelation AND Flid=5017002 -- LexSet_Comment
								END
							SELECT @cSetName=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017003
							IF @cSetName<>0
								BEGIN
									INSERT INTO LexReference_Name (Obj, Ws, Txt)
										SELECT @hvoReference, Ws, Txt
										FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017003 -- Name
								END
							ELSE
								BEGIN
									INSERT INTO LexReference_Name (Obj, Ws, Txt)
										SELECT @hvoReference, Ws, Txt
										FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017001 -- Abbreviation
								END
							IF @cSetComment=0
							BEGIN
								SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoMember AND Flid=5018001 -- LexSetItem Comment
								IF @cSetComment<>0
									BEGIN
										UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
											WHERE Obj=@hvoMember AND Flid=5018001 -- LexSetItem Comment
								END
							END
						END
						FETCH NEXT FROM levCursor INTO @hvoSrc, @hvoDst, @hvoMember, @hvoSense
					END
					CLOSE levCursor
					DEALLOCATE levCursor

					SET @nLevel = @nLevel + 1
				END
			DELETE FROM @TreeData
			FETCH NEXT FROM treeCursor INTO @hvoRelation
		END
		CLOSE treeCursor
		DEALLOCATE treeCursor
	END

	-- LexScale to sense sequence
	IF @cScales<>0
	BEGIN
		DECLARE scaleCursor CURSOR local static forward_only read_only FOR
			SELECT sc.[id]
			FROM LexScale sc
		OPEN scaleCursor
		FETCH NEXT FROM scaleCursor INTO @hvoRelation
		WHILE @@FETCH_STATUS = 0
		BEGIN

			EXEC CreateObject_LexReference
				@LexReference_Comment_ws = null, @LexReference_Comment_txt = null, @LexReference_Comment_fmt = null, 				@LexReference_Name_ws  = null, @LexReference_Name_txt = null, 				@Owner = @hvoRefType,
				@OwnFlid = 5119003,	-- Members
				@StartObj = null,
				@NewObjId = @hvoReference output,
				@NewObjGuid = null,
				@fReturnTimestamp = 0,
				@NewObjTimestamp = null

			SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017002 -- LexSet_Comment
			IF @cSetComment<>0
				BEGIN
					UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
						WHERE Obj=@hvoRelation AND Flid=5017002 -- LexSet_Comment
				END
			SELECT @cSetName=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017003
			IF @cSetName<>0
				BEGIN
					INSERT INTO LexReference_Name (Obj, Ws, Txt)
						SELECT @hvoReference, Ws, Txt
						FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017003 -- Name
				END
			ELSE
				BEGIN
					INSERT INTO LexReference_Name (Obj, Ws, Txt)
						SELECT @hvoReference, Ws, Txt
						FROM MultiStr$ WHERE Obj=@hvoRelation AND Flid=5017001 -- Abbreviation
				END

			-- LexScale_Positive
			DECLARE posCursor  CURSOR local static forward_only read_only FOR
			SELECT p.[Dst]
			FROM LexScale_Positive p WHERE p.[Src]=@hvoRelation ORDER BY p.[Ord]

			SET @ordSetMember=1
			OPEN posCursor
			FETCH NEXT FROM posCursor INTO @hvoSetMember
			WHILE @@FETCH_STATUS = 0
				BEGIN
					SELECT @hvoSense=Sense FROM LexSetItem WHERE [id]=@hvoSetMember
					IF @hvoSense is not null
						BEGIN
							INSERT INTO LexReference_Targets (Src, Dst, Ord)
							VALUES(@hvoReference, @hvoSense, @ordSetMember)
						END
					IF @cSetComment=0
						BEGIN
							SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoSetMember AND Flid=5018001 -- LexSetItem Comment
							IF @cSetComment<>0
								BEGIN
									UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
										WHERE Obj=@hvoSetMember AND Flid=5018001 -- LexSetItem Comment
								END
						END
					SET @ordSetMember=@ordSetMember+1
					FETCH NEXT FROM posCursor INTO @hvoSetMember
				END

			CLOSE posCursor
			DEALLOCATE posCursor

			-- LexScale_Neutral
			SELECT @hvoSetMember=Dst FROM LexScale_Neutral WHERE Src=@hvoRelation
			SELECT @hvoSense=Sense FROM LexSetItem WHERE [id]=@hvoSetMember
			IF @hvoSense is not null
				BEGIN
					INSERT INTO LexReference_Targets (Src, Dst, Ord)
					VALUES(@hvoReference, @hvoSense, @ordSetMember)
				END
			IF @cSetComment=0
				BEGIN
					SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoSetMember AND Flid=5018001 -- LexSetItem Comment
					IF @cSetComment<>0
						BEGIN
							UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
								WHERE Obj=@hvoSetMember AND Flid=5018001 -- LexSetItem Comment
						END
				END
			SET @ordSetMember=@ordSetMember+1

			-- LexScale_Negative
			DECLARE negCursor  CURSOR local static forward_only read_only FOR
			SELECT n.[Dst]
			FROM LexScale_Negative n WHERE n.[Src]=@hvoRelation ORDER BY n.[Ord]

			OPEN negCursor
			FETCH NEXT FROM negCursor INTO @hvoSetMember
			WHILE @@FETCH_STATUS = 0
				BEGIN
					SELECT @hvoSense=Sense FROM LexSetItem WHERE [id]=@hvoSetMember
					IF @hvoSense is not null
						BEGIN
							INSERT INTO LexReference_Targets (Src, Dst, Ord)
							VALUES(@hvoReference, @hvoSense, @ordSetMember)
						END
					IF @cSetComment=0
						BEGIN
							SELECT @cSetComment=COUNT(*) FROM MultiStr$ WHERE Obj=@hvoSetMember AND Flid=5018001 -- LexSetItem Comment
							IF @cSetComment<>0
								BEGIN
									UPDATE MultiStr$ SET Obj=@hvoReference, Flid=5120001 -- LexReference Comment
										WHERE Obj=@hvoSetMember AND Flid=5018001 -- LexSetItem Comment
								END
						END
					SET @ordSetMember=@ordSetMember+1
					FETCH NEXT FROM negCursor INTO @hvoSetMember
				END
			CLOSE negCursor
			DEALLOCATE negCursor
			FETCH NEXT FROM scaleCursor INTO @hvoRelation
		END
		CLOSE scaleCursor
		DEALLOCATE scaleCursor
	END

	FETCH NEXT FROM grpCursor INTO @hvoRelGroup, @nSetType
END
CLOSE grpCursor
DEALLOCATE grpCursor


-- TEST (BEGIN)
/*
SELECT g.[SetType], s1.[Obj], s1.[Txt],
	s3.[Obj], s3.[Txt], lsi.[Sense],
	s2.[Obj], s2.[Flid], s2.[Ws], s2.[Txt]
FROM LexSimpleSet s
	JOIN CmObject c on c.[id]=s.[id]
	JOIN LexicalRelationGroup g on g.[id]=c.[Owner$]
	LEFT OUTER JOIN MultiStr$ s1 on s1.[Obj]=g.[id] AND s1.[Ws]=@wsEn AND s1.[Flid]=5006005
	LEFT OUTER JOIN MultiStr$ s3 on s3.[Obj]=s.[id] AND s3.[Ws]=@wsEn AND s3.[Flid]=5017003
	LEFT OUTER JOIN LexSimpleSet_Members m on m.[Src]=s.[id]
	LEFT OUTER JOIN LexSetItem lsi on lsi.[Id]=m.[Dst]
	LEFT OUTER JOIN MultiStr$ s2 on s2.[Obj]=lsi.[id] AND s2.[Ws]=@wsEn -- AND s2.[Flid]=5006005

SELECT g.[SetType], s1.[Obj], s1.[Txt], s4.[Txt],
	s3.[Obj], s3.[Txt], lsi.[Sense],
	ltim.[Src], ltim.[Dst], s2.[Txt]
FROM LexTreeRelation ltr
	JOIN CmObject c on c.[id]=ltr.[id]
	JOIN LexicalRelationGroup g on g.[id]=c.[Owner$]
	LEFT OUTER JOIN MultiStr$ s1 on s1.[Obj]=g.[id] AND s1.[Ws]=@wsEn AND s1.[Flid]=5006005
	LEFT OUTER JOIN MultiStr$ s4 on s4.[Obj]=g.[id] AND s4.[Ws]=@wsEn AND s4.[Flid]=5006008
	LEFT OUTER JOIN MultiStr$ s3 on s3.[Obj]=ltr.[id] AND s3.[Ws]=@wsEn AND s3.[Flid]=5017003
	LEFT OUTER JOIN LexTreeRelation_Items ltri on ltri.Src=ltr.[id] OR ltri.Src in (SELECT Dst FROM LexTreeItem_Items WHERE Src=ltr.[id])
	LEFT OUTER JOIN LexTreeItem_Member ltim on ltim.[Src]=ltri.[Dst]
	LEFT OUTER JOIN LexSetItem lsi on lsi.[id]=ltim.[Dst]
	LEFT OUTER JOIN MultiStr$ s2 on s2.[Obj]=lsi.[id] AND s2.[Ws]=@wsEn

	-- LexPairRelation
SELECT g.[SetType], s1.[Obj], s1.[Txt] 'Grp AName', s4.[Txt] 'Grp BName',
	s3.[Obj], s3.[Txt] 'Set Name', s6.[Txt] 'Set Comment', s7.[Txt] 'Pair Comment', lsi.[Sense], lsi2.[Sense],
	lpma.[Src], lpma.[Dst], s2.[Txt] 'Item Comment',
	lpmb.[Dst], s5.[Txt] 'Item Comment'
FROM LexPairRelation lpr
	JOIN CmObject c on c.[id]=lpr.[id]
	JOIN LexicalRelationGroup g on g.[id]=c.[Owner$]
	LEFT OUTER JOIN MultiStr$ s1 on s1.[Obj]=g.[id] AND s1.[Ws]=@wsEn AND s1.[Flid]=5006005 -- AVariableName
	LEFT OUTER JOIN MultiStr$ s4 on s4.[Obj]=g.[id] AND s4.[Ws]=@wsEn AND s4.[Flid]=5006008	-- BVariableName
	LEFT OUTER JOIN MultiStr$ s3 on s3.[Obj]=lpr.[id] AND s3.[Ws]=@wsEn AND s3.[Flid]=5017003 -- LexSet_Name
	LEFT OUTER JOIN LexPairRelation_Members lprm on lprm.Src=lpr.[id]
	LEFT OUTER JOIN LexPair_MemberA lpma on lpma.[Src]=lprm.[Dst]
	LEFT OUTER JOIN LexSetItem lsi on lsi.[id]=lpma.[Dst]
	LEFT OUTER JOIN MultiStr$ s2 on s2.[Obj]=lsi.[id] AND s2.[Ws]=@wsEn
	LEFT OUTER JOIN LexPair_MemberB lpmb on lpmb.[Src]=lprm.[Dst]
	LEFT OUTER JOIN LexSetItem lsi2 on lsi2.[id]=lpmb.[Dst]
	LEFT OUTER JOIN MultiStr$ s5 on s5.[Obj]=lsi2.[id] AND s5.[Ws]=@wsEn
	LEFT OUTER JOIN MultiStr$ s6 on s6.[Obj]=lpr.[id] AND s6.[Ws]=@wsEn AND s6.[Flid]=5017002 -- LexSet_Comment
	LEFT OUTER JOIN MultiStr$ s7 on s7.[Obj]=lpr.[id] AND s7.[Ws]=@wsEn AND s7.[Flid]=5010001 -- LexPair_Comment

	-- LexScale
SELECT g.[SetType], s1.[Obj], s1.[Txt] 'Grp AName', s4.[Txt] 'Grp BName',
	s3.[Obj], s3.[Txt] 'Set Name', s6.[Txt] 'Set Comment', s7.[Txt] 'Pair Comment',
	lsi.[Sense], lsi2.[Sense], lsi3.[Sense],
	lsp.[Src], lsp.[Dst], s2.[Txt] 'Item Comment',
	lsn.[Dst], s5.[Txt] 'Item Comment',
	lsng.[Dst], s7.[Txt] 'Item Comment'
FROM LexScale ls
	JOIN CmObject c on c.[id]=ls.[id]
	JOIN LexicalRelationGroup g on g.[id]=c.[Owner$]
	LEFT OUTER JOIN MultiStr$ s1 on s1.[Obj]=g.[id] AND s1.[Ws]=@wsEn AND s1.[Flid]=5006005 -- AVariableName
	LEFT OUTER JOIN MultiStr$ s4 on s4.[Obj]=g.[id] AND s4.[Ws]=@wsEn AND s4.[Flid]=5006008	-- BVariableName
	LEFT OUTER JOIN MultiStr$ s3 on s3.[Obj]=ls.[id] AND s3.[Ws]=@wsEn AND s3.[Flid]=5017003 -- LexSet_Name
	LEFT OUTER JOIN MultiStr$ s6 on s6.[Obj]=ls.[id] AND s6.[Ws]=@wsEn AND s6.[Flid]=5017002 -- LexSet_Comment
	LEFT OUTER JOIN LexScale_Positive lsp on lsp.Src=ls.[id]
	LEFT OUTER JOIN LexSetItem lsi on lsi.[id]=lsp.[Dst]
	LEFT OUTER JOIN MultiStr$ s2 on s2.[Obj]=lsi.[id] AND s2.[Ws]=@wsEn
	LEFT OUTER JOIN LexScale_Neutral lsn on lsn.[Src]=ls.[id]
	LEFT OUTER JOIN LexSetItem lsi2 on lsi2.[id]=lsn.[Dst]
	LEFT OUTER JOIN MultiStr$ s5 on s5.[Obj]=lsi2.[id] AND s5.[Ws]=@wsEn
	LEFT OUTER JOIN LexScale_Negative lsng on lsng.[Src]=ls.[id]
	LEFT OUTER JOIN LexSetItem lsi3 on lsi3.[id]=lsng.[Dst]
	LEFT OUTER JOIN MultiStr$ s7 on s7.[Obj]=lsi3.[id] AND s7.[Ws]=@wsEn


SELECT t.[MappingType], n.[Obj], n.[Txt], lrn.[obj], lrn.[Txt], lrt.[Dst], lrt.[Ord]
FROM LexReference lr
	JOIN CmObject c on c.[id]=lr.[id]
	JOIN LexReferenceType t on t.[id]=c.[Owner$]
	LEFT OUTER JOIN CmPossibility_Name n on n.[obj]=t.[id] AND n.[Ws]=@wsEn
	LEFT OUTER JOIN LexReference_Name lrn on lrn.[obj]=lr.[id] AND lrn.[Ws]=@wsEn
	LEFT OUTER JOIN LexReference_Targets lrt on lrt.[Src]=lr.[id]

*/
-- TEST (END)

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200055
begin
	update Version$ set DbVer = 200056
	COMMIT TRANSACTION
	print 'database updated to version 200056'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200055 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO