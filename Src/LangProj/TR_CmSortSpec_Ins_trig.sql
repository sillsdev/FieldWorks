/***********************************************************************************************
 *	Trigger: TR_CmSortSpec_Ins
 *
 *	Description:
 *		Gets the primary, secondary, and tertiary sort fields out of a CmSortSpec record
 *		and creates records for them in FlidCollations$
 *
 *	Notes:
 *		When a custom sort is specified through the UI, a CmSortSpec is first created. This
 *		means that this trigger will be fired for insert with nulls, and have no affect. The
 *		UI then does an update on the CmSortSpec, and that's when all the data getes put into
 *		the CmSortSpec record.
 **********************************************************************************************/

IF OBJECT_ID('TR_CmSortSpec_Ins') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_CmSortSpec_Ins'
	DROP TRIGGER [TR_CmSortSpec_Ins]
END
GO
PRINT 'creating trigger TR_CmSortSpec_Ins'
GO
CREATE TRIGGER [TR_CmSortSpec_Ins] ON CmSortSpec FOR INSERT
AS

	DECLARE
		@nvcPrimary NVARCHAR(4000),
		@nvcSecondary NVARCHAR(4000),
		@nvcTertiary NVARCHAR(4000),
		@nID INT

	DECLARE curCmSortSpecIns CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT [PrimaryField], [SecondaryField], [TertiaryField] FROM Inserted

	OPEN curCmSortSpecIns
	FETCH curCmSortSpecIns INTO @nvcPrimary, @nvcSecondary, @nvcTertiary
	WHILE @@FETCH_STATUS = 0 BEGIN

		SET @nID = dbo.fnGetLastCommaDelimID$(@nvcPrimary)
		EXEC CreateFlidCollations NULL, NULL, @nID

		SET @nID = dbo.fnGetLastCommaDelimID$(@nvcSecondary)
		EXEC CreateFlidCollations NULL, NULL, @nID

		SET @nID = dbo.fnGetLastCommaDelimID$(@nvcTertiary)
		EXEC CreateFlidCollations NULL, NULL, @nID

		FETCH curCmSortSpecIns INTO @nvcPrimary, @nvcSecondary, @nvcTertiary
	END
	CLOSE curCmSortSpecIns
	DEALLOCATE curCmSortSpecIns

GO
