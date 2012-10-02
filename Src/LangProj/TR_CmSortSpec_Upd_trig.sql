/***********************************************************************************************
 *	Trigger: TR_CmSortSpec_Upd
 *
 *	Description:
 *		Gets the primary, secondary, and tertiary sort fields out of a CmSortSpec record.
 *
 *	Notes:
 *		When a custom sort is specified through the UI, a CmSortSpec is first created. This
 *		means that this trigger will be fired for insert with nulls, and have no affect. The
 *		UI then does an update on the CmSortSpec, and that's when all the data getes put into
 *		the CmSortSpec record.
 **********************************************************************************************/

IF OBJECT_ID('TR_CmSortSpec_Upd') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_CmSortSpec_Upd'
	DROP TRIGGER [TR_CmSortSpec_Upd]
END
GO
PRINT 'creating trigger TR_CmSortSpec_Upd'
GO
CREATE TRIGGER [TR_CmSortSpec_Upd] ON CmSortSpec FOR INSERT
AS

	DECLARE
		@nvcPrimaryIns NVARCHAR(4000),
		@nvcSecondaryIns NVARCHAR(4000),
		@nvcTertiaryIns NVARCHAR(4000),
		@nvcPrimaryDel NVARCHAR(4000),
		@nvcSecondaryDel NVARCHAR(4000),
		@nvcTertiaryDel NVARCHAR(4000),
		@nID INT

	DECLARE curCmSortSpecIns CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT
			ins.[PrimaryField] AS [PrimaryIns],
			ins.[SecondaryField] AS [SecondaryIns],
			ins.[TertiaryField] AS [TertiaryIns],
			del.[PrimaryField] AS [PrimaryDel],
			del.[SecondaryField] AS [SecondaryDel],
			del.[TertiaryField] AS [TertiaryDel]
		FROM Inserted ins
		JOIN Deleted del ON del.[ID] = ins.[Id]

	OPEN curCmSortSpecIns
	FETCH curCmSortSpecIns INTO
		@nvcPrimaryIns,
		@nvcSecondaryIns,
		@nvcTertiaryIns,
		@nvcPrimaryDel,
		@nvcSecondaryDel,
		@nvcTertiaryDel
	WHILE @@FETCH_STATUS = 0 BEGIN

		IF @nvcPrimaryIns IS NOT NULL AND @nvcPrimaryDel IS NULL BEGIN
			-- insert new
			SET @nId = NULL  --dummy line to make the compiler happy
		END
		ELSE IF @nvcPrimaryIns IS NULL AND @nvcPrimaryDel IS NOT NULL BEGIN
			-- delete old one
			SET @nId = NULL  --dummy line to make the compiler happy
		END
		ELSE IF @nvcPrimaryIns <> @nvcPrimaryDel BEGIN
			SET @nID = dbo.fnGetLastCommaDelimID$(@nvcPrimaryIns)
			EXEC CreateFlidCollations NULL, NULL, @nID
		END
		---
			SET @nID = dbo.fnGetLastCommaDelimID$(@nvcSecondaryIns)
			EXEC CreateFlidCollations NULL, NULL, @nID

			SET @nID = dbo.fnGetLastCommaDelimID$(@nvcTertiaryIns)
			EXEC CreateFlidCollations NULL, NULL, @nID

		FETCH curCmSortSpecIns INTO @nvcPrimaryIns, @nvcSecondaryIns, @nvcTertiaryIns
	END
	CLOSE curCmSortSpecIns
	DEALLOCATE curCmSortSpecIns

GO
