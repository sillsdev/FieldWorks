// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The rows an entry shows for its variants (the VariantFormEntryBackRefs section): each
	/// shows a part of another entry, and its commands act on that entry. Delete Reference
	/// (message DataTreeDeleteReference) unlinks the variant from the shown entry.
	/// </summary>
	public static class VariantBackReferences
	{
		/// <summary>
		/// The entry a row shows on the shown entry's behalf: the owner of the row's object
		/// when that is an entry other than <paramref name="root"/>. Null when the root is no
		/// entry, or the row's object is the root or belongs to it.
		/// </summary>
		public static ILexEntry BackReferencedEntry(ICmObject root, ICmObject rowObject)
		{
			if (!(root is ILexEntry) || rowObject == null || rowObject == root)
				return null;
			var owner = rowObject.Owner;
			if (owner == null || owner == root)
				return null;
			return owner as ILexEntry;
		}

		/// <summary>The object a row's commands act on: the back-referenced entry when the row
		/// shows one, else the row's object.</summary>
		public static ICmObject CommandTarget(ICmObject root, ICmObject rowObject)
			=> BackReferencedEntry(root, rowObject) ?? rowObject;

		/// <summary>The refs of <paramref name="variant"/> that list <paramref name="root"/>
		/// among their components, in the variant's order.</summary>
		public static IReadOnlyList<ILexEntryRef> ReferencesTo(ILexEntry root, ILexEntry variant)
		{
			if (root == null || variant == null)
				return new ILexEntryRef[0];
			return variant.EntryRefsOS.Where(r => r.ComponentLexemesRS.Contains(root)).ToList();
		}

		/// <summary>
		/// Unlinks the variant from <paramref name="root"/> in one undoable step: the root
		/// leaves the ref's components, and the ref leaves its entry.
		/// </summary>
		public static void DeleteReference(ILexEntry root, ILexEntryRef reference)
		{
			if (root == null)
				throw new ArgumentNullException(nameof(root));
			if (reference == null)
				throw new ArgumentNullException(nameof(reference));
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoDeleteRef, FdoUiStrings.ksRedoDeleteRef, reference, () =>
			{
				reference.ComponentLexemesRS.Remove(root);
				if (reference.PrimaryLexemesRS.Contains(root))
					reference.PrimaryLexemesRS.Remove(root);
				var entry = reference.OwningEntry;
				if (entry.EntryRefsOS.Contains(reference))
					entry.EntryRefsOS.Remove(reference);
			});
		}
	}
}
