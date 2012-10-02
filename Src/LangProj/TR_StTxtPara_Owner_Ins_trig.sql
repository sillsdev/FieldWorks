/***********************************************************************************************
 * Trigger: TR_StTxtPara_Owner_Ins
 *
 * Description:
 *	Update UpdDttm on the CmObject record of the owning StText record. Also update the owner
 *  of the owning StText record:
 *
 *		x --> StText --> StTxtPara
 *
 *  Update UpdDttm of x and StText.
 *
 * Type: 	Inserrt
 * Table:	StTxtPara
 **********************************************************************************************/

IF object_id('TR_StTxtPara_Owner_Ins') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StTxtPara_Owner_Ins'
	DROP TRIGGER [TR_StTxtPara_Owner_Ins]
END
GO
PRINT 'creating trigger TR_StTxtPara_Owner_Ins'
GO

CREATE TRIGGER [TR_StTxtPara_Owner_Ins] on [StTxtPara] FOR INSERT
AS
	DECLARE @iErr INT, @fIsNocountOn INT

	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	UPDATE owner
		SET [UpdDttm] = getdate()
		FROM inserted ins
		JOIN [CmObject] AS owned ON owned.[Id] = ins.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$

	UPDATE grandowner
		SET [UpdDttm] = getdate()
		FROM inserted ins
		JOIN [CmObject] AS owned ON owned.[Id] = ins.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$
		JOIN [CmObject] AS grandowner ON grandowner.[Id] = owner.Owner$

	SET @iErr = @@error
	IF @iErr <> 0 GOTO LFail

	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	RETURN

LFail:
	ROLLBACK TRAN
	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	Raiserror ('TR_StTxtPara_Owner_Ins: SQL Error %d; Unable to insert rows into the StTxtPara.', 16, 1, @iErr)
	RETURN
GO
