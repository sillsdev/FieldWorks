-- Update database from version 200195 to 200196
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- TE-5582: Two users can't add the same Style at the same time. (fix last attempt)
-------------------------------------------------------------------------------
IF OBJECT_ID('TR_StStyle_InsUpd_trig') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StStyle_InsUpd_trig';
	DROP TRIGGER TR_StStyle_InsUpd_trig
END
GO


IF OBJECT_ID('TR_StStyle_Ins_trig') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StStyle_Ins_trig';
	DROP TRIGGER TR_StStyle_Ins_trig
END
GO

PRINT 'creating trigger TR_StStyle_Ins_trig';
GO
CREATE TRIGGER TR_StStyle_Ins_trig ON StStyle FOR INSERT
AS
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

		SELECT @Count = COUNT(s.Id)
		FROM StStyle s
		JOIN CmObject o ON o.Id = s.Id
		WHERE s.Name = @NewName AND o.Owner$ = @NewOwner$;

		IF @Count > 1 BEGIN --( The inserted record counts as 1.
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

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200195
BEGIN
	UPDATE Version$ SET DbVer = 200196
	COMMIT TRANSACTION
	PRINT 'database updated to version 200196'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200195 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
