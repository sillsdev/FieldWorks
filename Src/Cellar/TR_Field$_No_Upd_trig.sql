/***********************************************************************************************
 * Trigger: TR_Field$_No_Upd
 *
 * Description:
 *	Limit most data in Field$ from being updated.
 *
 * Type: 	Update
 * Table:	Field$
 *
 *	The fields UserLabel, HelpString, ListRootId, WsSelector, and XmlUI were added to this
 *	table much later than the other fields. They are meant to be used with custom fields. The
 *	original trigger prohibited updates on the table altogether, but we would like to update
 *	these new fields.
 **********************************************************************************************/

if object_id('TR_Field$_No_Upd') is not null begin
	print 'removing trigger TR_Field$_No_Upd'
	drop trigger [TR_Field$_No_Upd]
end
go
print 'creating trigger TR_Field$_No_Upd'
go
create trigger [TR_Field$_No_Upd] on [Field$] for update
as

	DECLARE @nId INT

	--( This doesn't deal with IDs. Hopefully no on will ever be so rash
	--( as to do that.

	--( Many of the fields have constraints of their own, which makes it
	--( hard to meet or test the conditions below.

	SELECT @nId = i.[Id]
	FROM inserted i
	JOIN deleted d ON d.[Id] = i.[Id]
	WHERE COALESCE(i.Type, 0) != COALESCE(d.Type, 0)
		OR COALESCE(i.Class, 0) != COALESCE(d.Class, 0)
		OR COALESCE(i.DstCls, 0) != COALESCE(d.DstCls, 0)
		OR i.[Name] != d.[Name]
		OR i.Custom != d.Custom
		--( Guids are 36 in length, but just in case...
		OR COALESCE(CONVERT(VARCHAR(100), i.CustomId), '0') != COALESCE(CONVERT(VARCHAR(100), d.CustomId), '0')
		OR COALESCE(i.[Min], 0) != COALESCE(d.[Min], 0)
		OR COALESCE(i.[Max], 0) != COALESCE(d.[Max], 0)
		OR COALESCE(i.Big, 0) != COALESCE(d.Big, 0)

	--( This forms a kind of assert. No one should be touching the metadata fields
	--( of Field$
	IF @@ROWCOUNT != 0 BEGIN
		raiserror('Update is not allowed on the Field$ table metadata fields.  Delete the field record and reinsert it with the new values.', 16, 1)
		rollback tran
	END

	return
go
