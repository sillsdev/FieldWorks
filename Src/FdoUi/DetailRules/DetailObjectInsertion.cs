// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Insert command of a detail row (message DataTreeInsert): whether a field can take
	/// a new object now, which object and field the row's command inserts into, and where.
	/// Creation stays with <c>CmObjectUi.CreateNewUiObject</c>.
	/// </summary>
	public static class DetailObjectInsertion
	{
		/// <summary>The insertion position of a new object in an owning collection.</summary>
		public const int CollectionPosition = -1;

		/// <summary>The insertion position of a new object in an owning atomic field.</summary>
		public const int AtomicPosition = -2;

		/// <summary>
		/// Whether a new object can go into <paramref name="fieldName"/> of
		/// <paramref name="owner"/>: a vector, a virtual reference vector, or an empty atomic
		/// field can take one; a plain reference field cannot.
		/// </summary>
		/// <param name="index">The end of the vector; -1 for an atomic field or when the
		/// answer is false.</param>
		public static bool CanInsertInto(ICmObject owner, string fieldName, out int index)
		{
			index = -1;
			if (owner == null || !owner.IsValidObject || string.IsNullOrEmpty(fieldName))
				return false;
			var cache = owner.Cache;
			var flid = FieldOf(cache, owner.ClassID, fieldName);
			if (flid == 0)
				return false;
			var mdc = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			if (cache.IsReferenceProperty(flid) && !mdc.get_IsVirtual(flid))
				return false;
			var type = (CellarPropertyType)mdc.GetFieldType(flid);
			switch (type)
			{
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.ReferenceSequence:
				case CellarPropertyType.ReferenceCollection:
					index = cache.DomainDataByFlid.get_VecSize(owner.Hvo, flid);
					return true;
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.ReferenceAtomic:
					return cache.DomainDataByFlid.get_ObjectProp(owner.Hvo, flid) == 0;
				default:
					return false;
			}
		}

		/// <summary>
		/// The display rule of an Insert item: enabled when the shown record can take a new
		/// object in <paramref name="fieldName"/>, else when the row's object can.
		/// </summary>
		/// <param name="index">The end of the vector that answered; -1 otherwise.</param>
		public static bool CanInsert(ICmObject root, ICmObject rowObject, string fieldName, out int index)
		{
			if (root != null && !root.IsValidObject)
			{
				index = -1;
				return false;
			}
			if (CanInsertInto(root, fieldName, out index))
				return true;
			return rowObject != root && CanInsertInto(rowObject, fieldName, out index);
		}

		/// <summary>
		/// Whether some class owns objects in a field named <paramref name="fieldName"/>, so
		/// an Insert into it creates an object rather than choosing one.
		/// </summary>
		public static bool IsOwningFieldName(LcmCache cache, string fieldName)
		{
			if (cache == null || string.IsNullOrEmpty(fieldName))
				return false;
			var mdc = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			foreach (var flid in mdc.GetFieldIds())
			{
				if (mdc.GetFieldName(flid) != fieldName)
					continue;
				switch ((CellarPropertyType)mdc.GetFieldType(flid))
				{
					case CellarPropertyType.OwningAtomic:
					case CellarPropertyType.OwningCollection:
					case CellarPropertyType.OwningSequence:
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// The field a command inserts into, from <paramref name="candidate"/>: the field of
		/// that name on <paramref name="ownerClassId"/> when it has one, else on the
		/// candidate's class; 0 when neither has it.
		/// </summary>
		public static int ResolveFlid(ICmObject candidate, string fieldName, int ownerClassId)
		{
			if (candidate == null || string.IsNullOrEmpty(fieldName))
				return 0;
			var cache = candidate.Cache;
			var flid = FieldOf(cache, candidate.ClassID, fieldName);
			var ownerFlid = ownerClassId != 0 ? FieldOf(cache, ownerClassId, fieldName) : 0;
			return ownerFlid != 0 ? ownerFlid : flid;
		}

		/// <summary>
		/// The object that owns <paramref name="flid"/>: <paramref name="candidate"/> when the
		/// field belongs to its class, else its owner when it belongs to the owner's; null when
		/// it belongs to neither.
		/// </summary>
		public static ICmObject OwnerOf(ICmObject candidate, int flid)
		{
			if (candidate == null || flid == 0)
				return null;
			var cache = candidate.Cache;
			var fieldClass = ((IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor).GetOwnClsId(flid);
			if (cache.ClassIsOrInheritsFrom(candidate.ClassID, fieldClass))
				return candidate;
			var owner = candidate.Owner;
			return owner != null && cache.ClassIsOrInheritsFrom(owner.ClassID, fieldClass) ? owner : null;
		}

		/// <summary>
		/// The concrete class to create for <paramref name="classId"/>: itself when concrete;
		/// for the abstract MoForm on an entry, the entry's default allomorph class; 0 for any
		/// other abstract class.
		/// </summary>
		public static int ConcreteClass(ICmObject owner, int classId)
		{
			if (owner == null || classId == 0)
				return 0;
			var mdc = (IFwMetaDataCacheManaged)owner.Cache.MetaDataCacheAccessor;
			if (!mdc.GetAbstract(classId))
				return classId;
			if (classId == MoFormTags.kClassId && owner is ILexEntry entry)
				return entry.GetDefaultClassForNewAllomorph();
			return 0;
		}

		/// <summary>
		/// Where a new object goes in <paramref name="flid"/> of <paramref name="owner"/>: in
		/// a sequence, after the first of <paramref name="anchorHvos"/> that is one of its
		/// items, else at its end; <see cref="CollectionPosition"/> or
		/// <see cref="AtomicPosition"/> for those field kinds; 0 for any other.
		/// </summary>
		/// <param name="anchorHvos">The objects the insert is relative to, nearest
		/// first.</param>
		public static int InsertionPosition(ICmObject owner, int flid, IEnumerable<int> anchorHvos)
		{
			var cache = owner.Cache;
			var sda = cache.DomainDataByFlid;
			switch ((CellarPropertyType)cache.MetaDataCacheAccessor.GetFieldType(flid))
			{
				case CellarPropertyType.OwningCollection:
					return CollectionPosition;
				case CellarPropertyType.OwningAtomic:
					return AtomicPosition;
				case CellarPropertyType.OwningSequence:
					var count = sda.get_VecSize(owner.Hvo, flid);
					if (anchorHvos != null)
					{
						foreach (var anchor in anchorHvos)
						{
							for (var i = 0; i < count; i++)
							{
								if (sda.get_VecItem(owner.Hvo, flid, i) == anchor)
									return i + 1;
							}
						}
					}
					return count;
				default:
					return 0;
			}
		}

		/// <summary>
		/// The target of a row's Insert command: the first of the row's object and its owners
		/// (the owner first when <paramref name="insertAtOwner"/>) with the command's field,
		/// the object that owns that field, the concrete class, and the position after the
		/// row's object or the nearest of its owners among the field's items. Null when no
		/// owning target resolves.
		/// </summary>
		/// <param name="ownerClassName">The class expected to own the field; when empty, the
		/// class of the row's object's owner.</param>
		/// <param name="insertAtOwner">True to insert beside the row's object rather than
		/// under it, as the command's <c>slice="owner"</c> asks.</param>
		public static DetailInsertTarget ResolveTarget(ICmObject rowObject, string fieldName, string className,
			string ownerClassName, bool insertAtOwner)
		{
			if (rowObject == null || !rowObject.IsValidObject || string.IsNullOrEmpty(fieldName))
				return null;
			var cache = rowObject.Cache;
			var mdc = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			var classId = ClassOf(mdc, className);
			if (classId == 0)
				return null;
			if (string.IsNullOrEmpty(ownerClassName) && rowObject.Owner != null)
				ownerClassName = rowObject.Owner.ClassName;
			var ownerClassId = ClassOf(mdc, ownerClassName);

			var start = insertAtOwner && rowObject.Owner != null ? rowObject.Owner : rowObject;
			for (var candidate = start; candidate != null; candidate = candidate.Owner)
			{
				var flid = ResolveFlid(candidate, fieldName, ownerClassId);
				if (flid == 0)
					continue;
				// A reference field takes no new object; this rule does not choose one.
				if (cache.IsReferenceProperty(flid))
					return null;
				var owner = OwnerOf(candidate, flid);
				if (owner == null)
					continue;
				var concreteClass = ConcreteClass(owner, classId);
				if (concreteClass == 0)
					return null;
				var position = InsertionPosition(owner, flid, OwnerChainHvos(rowObject));
				return new DetailInsertTarget(owner, flid, concreteClass, position);
			}
			return null;
		}

		// The object and its owners, nearest first.
		private static IEnumerable<int> OwnerChainHvos(ICmObject obj)
		{
			for (var o = obj; o != null; o = o.Owner)
				yield return o.Hvo;
		}

		private static int FieldOf(LcmCache cache, int classId, string fieldName)
		{
			var mdc = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			return classId != 0 && mdc.FieldExists(classId, fieldName, true) ? mdc.GetFieldId2(classId, fieldName, true) : 0;
		}

		private static int ClassOf(IFwMetaDataCacheManaged mdc, string className)
			=> !string.IsNullOrEmpty(className) && mdc.ClassExists(className) ? mdc.GetClassId(className) : 0;
	}

	/// <summary>Where a row's Insert command creates its object.</summary>
	public sealed class DetailInsertTarget
	{
		public DetailInsertTarget(ICmObject owner, int flid, int classId, int position)
		{
			Owner = owner;
			Flid = flid;
			ClassId = classId;
			Position = position;
		}

		/// <summary>The object that takes the new one.</summary>
		public ICmObject Owner { get; }

		/// <summary>The owning field of <see cref="Owner"/> it goes into.</summary>
		public int Flid { get; }

		/// <summary>The concrete class to create.</summary>
		public int ClassId { get; }

		/// <summary>Its position: an index in a sequence, else
		/// <see cref="DetailObjectInsertion.CollectionPosition"/> or
		/// <see cref="DetailObjectInsertion.AtomicPosition"/>.</summary>
		public int Position { get; }
	}
}
