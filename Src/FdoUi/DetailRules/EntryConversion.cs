// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Lexeme Form conversions of an entry (messages ConvertEntryIntoComplexForm and
	/// ConvertEntryIntoVariant): each adds an entry ref of the unspecified type, whose
	/// components the user then fills in.
	/// </summary>
	public static class EntryConversion
	{
		/// <summary>
		/// Whether <paramref name="entry"/> can become a complex form: it has no entry ref with a
		/// complex form type yet.
		/// </summary>
		public static bool CanConvertIntoComplexForm(ILexEntry entry)
		{
			return entry != null && entry.IsValidObject
				&& entry.EntryRefsOS.All(entryRef => entryRef.ComplexEntryTypesRS.Count == 0);
		}

		/// <summary>
		/// Makes <paramref name="entry"/> a complex form, in one undoable step: a new first
		/// entry ref of the unspecified complex form type, and a root lexeme form becomes a stem.
		/// </summary>
		/// <returns>The new entry ref.</returns>
		public static ILexEntryRef ConvertIntoComplexForm(ILexEntry entry, string undoText, string redoText)
		{
			ILexEntryRef entryRef = null;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, entry, () =>
			{
				entryRef = entry.Services.GetInstance<ILexEntryRefFactory>().Create();
				entry.EntryRefsOS.Insert(0, entryRef);
				entryRef.ComplexEntryTypesRS.Add(EntryType(entry, LexEntryTypeTags.kguidLexTypeUnspecifiedComplexForm));
				entryRef.RefType = LexEntryRefTags.krtComplexForm;
				entryRef.HideMinorEntry = 0; // LT-10928
				entry.ChangeRootToStem();
			});
			return entryRef;
		}

		/// <summary>
		/// Makes <paramref name="entry"/> a variant, in one undoable step: a new last entry ref
		/// of the unspecified variant type.
		/// </summary>
		/// <returns>The new entry ref.</returns>
		public static ILexEntryRef ConvertIntoVariant(ILexEntry entry, string undoText, string redoText)
		{
			ILexEntryRef entryRef = null;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, entry, () =>
			{
				entryRef = entry.Services.GetInstance<ILexEntryRefFactory>().Create();
				entry.EntryRefsOS.Add(entryRef);
				entryRef.VariantEntryTypesRS.Add(EntryType(entry, LexEntryTypeTags.kguidLexTypeUnspecifiedVar));
				entryRef.RefType = LexEntryRefTags.krtVariant;
				entryRef.HideMinorEntry = 0;
			});
			return entryRef;
		}

		private static ILexEntryType EntryType(ILexEntry entry, System.Guid guid)
			=> entry.Services.GetInstance<ILexEntryTypeRepository>().GetObject(guid);
	}
}
