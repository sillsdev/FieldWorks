-- Update database from version 200258 to 200259
-------------------------------------------------------------------------------
-- TE-8370 (and TE-5582, again): This fix should prevent duplicate styles from
-- being created via an update or insert (regardless .
-------------------------------------------------------------------------------
BEGIN TRANSACTION  --( will be rolled back if wrong version#)
IF OBJECT_ID('TR_StStyle_Ins_trig') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StStyle_Ins_trig';
	DROP TRIGGER TR_StStyle_Ins_trig
END
GO

PRINT 'creating trigger TR_StStyle_InsUpd_trig';
GO
CREATE TRIGGER TR_StStyle_InsUpd_trig ON StStyle FOR INSERT, UPDATE
AS
	IF NOT EXISTS(SELECT TOP 1 * FROM inserted)
		RETURN

	BEGIN TRY
		DECLARE
			@Count INT,
			@NewName NVARCHAR(4000),
			@NewOwner$ INT,
			@String NVARCHAR(4000);

		--( The StStyle should have an associated CmObject record already.

		SELECT TOP 1 @NewName = i.Name, @NewOwner$ = o.Owner$
		FROM inserted i
		JOIN CmObject o ON o.Id = i.Id;

		IF @@ROWCOUNT = 0 BEGIN
			SELECT @NewName = i.Name FROM inserted i;
			SET @String = 'No CmObject record created for StStyle "' +
				RTRIM(@NewName) + '" yet.';
			RAISERROR (@String, 16, 1);
		END

		--( No duplicate styles should exist for the owner StStyle.
		IF NOT EXISTS (SELECT * FROM inserted WHERE [Name] IS NOT NULL)
			RETURN -- This allows an optimization in LoadXML to create a bunch of empty styles and then fill them in

		SELECT TOP 1 @NewName = MAX(Name)
		FROM StStyle_
		WHERE Name IS NOT NULL
		GROUP BY Owner$, OwnFlid$, Name
		HAVING COUNT(*) > 1

		IF @@ROWCOUNT > 0 BEGIN
			SET @String = 'The style "' + RTRIM(@NewName) + '" already exists in StStyle.';
			RAISERROR (@String, 16, 1);
		END

	END TRY
	BEGIN CATCH
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );
	END CATCH;
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200258
BEGIN
	UPDATE Version$ SET DbVer = 200259
	COMMIT TRANSACTION
	PRINT 'database updated to version 200259'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200258 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
