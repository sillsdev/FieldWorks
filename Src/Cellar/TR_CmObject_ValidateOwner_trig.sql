/***********************************************************************************************
 * Trigger: TR_CmObject_ValidateOwner
 *
 * Description:
 *	This trigger validates an object's owner information. These validations include: make
 *	sure the owner's class is a subclass of the class the owning field belongs to, make
 *	sure the inserted object's class is a subclass of the owning field's destination class,
 *	make sure the OwnOrd$ column contains a proper value, make sure only one object at most
 *	is owned through an atomic relationship, and make sure there are no duplicate Ord
 *	values within the same sequence
 *.
 * Type: 	Update
 * Table:	CmObject
 *
 * Notes:
 *	Obviously since this is an update trigger these validations are not performed when an
 *	object is initially created. Instead, the MakeObj_* procedures that exist for
 *	each non-abstract class contain the appropriate validations.
 *
 *	This trigger returns immediately if neither Owner$, OwnFlid$, or OwnOrd$ are modified.
 *
 **********************************************************************************************/
if object_id('TR_CmObject_ValidateOwner') is not null begin
	print 'removing trigger TR_CmObject_ValidateOwner'
	drop trigger [TR_CmObject_ValidateOwner]
end
go
print 'creating trigger TR_CmObject_ValidateOwner'
go
create trigger [TR_CmObject_ValidateOwner] on [CmObject] for update
as
	--( We used to check to not allow an object's class to be changed,
	--( similar to the check for update([Id]) immediately below. We
	--( have since found the need to change Lex Entries. For instance,
	--( a LexSubEntry can turn into a LexMajorEntry.

	if update([Id]) begin
		raiserror('An object''s Id cannot be changed', 16, 1)
		rollback tran
	end

	-- only perform checks if one of the following columns are updated: id, owner$, ownflid$, or
	--	ownord$	because updates to UpdDttm or UpdStmp do not require the below validations
	if not ( update([Owner$]) or update([OwnFlid$]) or update([OwnOrd$]) )  return

	declare @idBad int, @own int, @flid int, @ord int, @cnt int
	declare @dupownId int, @dupseqId int
	declare @fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	if update([Owner$]) or update([OwnFlid$]) or update([OwnOrd$]) begin
		-- Get the owner's class and make sure it is a subclass of the field's type. Get the
		--	inserted object's class and make sure it is a subclass of the field's dst type.
		-- 	Make sure the OwnOrd$ field is consistent with the field type (if not sequence
		--	then it should be null). Make sure more than one object is not added as a child
		--	of an object with an atomic owning relationship. Make sure there are no duplicate
		--	Ord values within a sequence
		select top 1
			@idBad = ins.[Id],
			@own = ins.[Owner$],
			@flid = ins.[OwnFlid$],
			@ord = ins.[OwnOrd$],
			@dupownId = dupown.[Id],
			@dupseqId = dupseq.[Id]
		from	inserted ins
			-- If there is no owner, there is nothing to check so an inner join is OK here.
			join [CmObject] own on own.[Id] = ins.[Owner$]
			-- The constraints on CmObject guarantee this join.
			join [Field$] fld on fld.[Id] = ins.[OwnFlid$]
			-- If this join has no matches the owner is of the wrong type.
			left outer join [ClassPar$] ot on ot.[Src] = own.[Class$]
				and ot.[Dst] = fld.[Class]
			-- If this join has no matches the inserted object is of the wrong type.
			left outer join [ClassPar$] it on it.[Src] = ins.[Class$]
				and it.[Dst] = fld.[DstCls]
			-- if this join has matches there is more than one owned object in an atomic relationship
			left outer join [CmObject] dupown on fld.[Type] = kcptOwningAtom and dupown.[Owner$] = ins.[Owner$]
				and dupown.[OwnFlid$] = ins.[OwnFlid$]
				and dupown.[Id] <> ins.[Id]
			-- if this join has matches there is a duplicate sequence order in a sequence relationship
			left outer join [CmObject] dupseq on fld.[Type] = kcptOwningSequence and dupseq.[Owner$] = ins.[Owner$]
				and dupseq.[OwnFlid$] = ins.[OwnFlid$]
				and dupseq.[OwnOrd$] = ins.[OwnOrd$]
				and dupseq.[Id] <> ins.[Id]
		where
			ot.[Src] is null
			or it.[Src] is null
			or (fld.[Type] = kcptOwningAtom and ins.[OwnOrd$] is not null)
			or (fld.[Type] = kcptOwningCollection and ins.[OwnOrd$] is not null)
			or (fld.[Type] = kcptOwningSequence and ins.[OwnOrd$] is null)
			or dupown.[Id] is not null
			or dupseq.[Id] is not null

		if @@rowcount <> 0 begin
			if @dupownId is not null begin
				raiserror('More than one owned object in an atomic relationship: New ID=%d, Owner=%d, OwnFlid=%d, Already Owned Id=%d', 16, 1,
						@idBad, @own, @flid, @dupownId)
			end
			else if @dupseqId is not null begin
				raiserror('Duplicate OwnOrd in a sequence relationship: New ID=%d, Owner=%d, OwnFlid=%d, OwnOrd=%d, Duplicate Id=%d', 16, 1,
						@idBad, @own, @flid, @ord, @dupseqId)
			end
			else begin
				raiserror('Bad owner information ID=%d, Owner$=%d, OwnFlid$=%d, OwnOrd$=%d', 16, 1, @idBad, @own, @flid, @ord)
			end
			rollback tran
		end
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
go
