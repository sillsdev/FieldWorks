/***********************************************************************************************
 *	Trigger: TR_CmSortSpec_Del
 *
 *	Description:
 *		Gets the primary, secondary, and tertiary sort fields out of a CmSortSpec record
 *		and deletes their records win FlidCollations$
 *
 *	Notes:
 *		When a custom sort is specified through the UI, a CmSortSpec is first created. This
 *		means that this trigger will be fired for insert with nulls, and have no affect. The
 *		UI then does an update on the CmSortSpec, and that's when all the data getes put into
 *		the CmSortSpec record.
 **********************************************************************************************/

IF OBJECT_ID('TR_CmSortSpec_Del') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_CmSortSpec_Del'
	DROP TRIGGER [TR_CmSortSpec_Del]
END
GO
PRINT 'creating trigger TR_CmSortSpec_Del'
GO
CREATE TRIGGER [TR_CmSortSpec_Del] ON CmSortSpec FOR DELETE
AS

	DECLARE
		@nvcPrimary NVARCHAR(4000),
		@nvcSecondary NVARCHAR(4000),
		@nvcTertiary NVARCHAR(4000),
		@nID INT,
		@nFlid INT

	DECLARE curCmSortSpecDel CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT [PrimaryField], [SecondaryField], [TertiaryField] FROM Deleted

	OPEN curCmSortSpecDel
	FETCH curCmSortSpecDel INTO @nvcPrimary, @nvcSecondary, @nvcTertiary
	WHILE @@FETCH_STATUS = 0 BEGIN

		-- this needs to work
		SELECT TOP 1 [PrimaryWs] FROM [CmSortSpec] WHERE [PrimaryWs] = @nFlid
		UNION
		SELECT TOP 1 [SecondaryWs] FROM [CmSortSpec] WHERE [SecondaryWs] = @nFlid
		UNION
		SELECT TOP 1 [TertiaryWs] FROM [CmSortSpec] WHERE [TertiaryWs] = @nFlid

		IF @@ROWCOUNT = 0
		SET @nID = dbo.fnGetLastCommaDelimID$(@nvcPrimary)
		EXEC DeleteFlidCollations NULL, NULL, @nID

		FETCH curCmSortSpecDel INTO @nvcPrimary, @nvcSecondary, @nvcTertiary
	END
	CLOSE curCmSortSpecDel
	DEALLOCATE curCmSortSpecDel

GO
