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
	/// The two dictionary-visibility marks a complex form's components can carry: a component
	/// is a primary lexeme when the complex form is shown as a subentry under it, and a
	/// component is in the show-in list when it shows the complex form as a referenced complex
	/// form. Each mark is a subsequence of the complex-form ref's components, kept in
	/// component order.
	/// </summary>
	public static class ComplexFormVisibility
	{
		/// <summary>
		/// Whether <paramref name="component"/> can be marked as showing the subentry of
		/// <paramref name="complexFormRef"/>: the ref is a complex form and the component is an
		/// entry or a sense.
		/// </summary>
		public static bool CanShowSubentryUnderComponent(ILexEntryRef complexFormRef, ICmObject component)
			=> complexFormRef != null && complexFormRef.RefType == LexEntryRefTags.krtComplexForm
				&& (component is ILexEntry || component is ILexSense);

		/// <summary>
		/// Whether the complex form is shown as a subentry under the component.
		/// </summary>
		public static bool ShowsSubentryUnderComponent(ILexEntryRef complexFormRef, ICmObject component)
			=> complexFormRef.PrimaryLexemesRS.Contains(component);

		/// <summary>
		/// Marks the component as showing the subentry, or unmarks it when it already does, as
		/// one undoable task.
		/// </summary>
		public static void ToggleSubentryUnderComponent(ILexEntryRef complexFormRef, ICmObject component,
			string undoText, string redoText)
			=> Toggle(complexFormRef.PrimaryLexemesRS, complexFormRef, component, undoText, redoText);

		/// <summary>
		/// The ref through which <paramref name="complexForm"/> is a complex form, or null when
		/// it is not one.
		/// </summary>
		public static ILexEntryRef ComplexFormRefOf(ILexEntry complexForm)
			=> complexForm?.EntryRefsOS.FirstOrDefault(r => r.RefType == LexEntryRefTags.krtComplexForm);

		/// <summary>
		/// Whether the component shows the complex form as a referenced complex form.
		/// </summary>
		public static bool ShowsComplexFormIn(ILexEntryRef complexFormRef, ICmObject component)
			=> complexFormRef.ShowComplexFormsInRS.Contains(component);

		/// <summary>
		/// Marks the component as showing the complex form, or unmarks it when it already does,
		/// as one undoable task.
		/// </summary>
		public static void ToggleShowComplexFormIn(ILexEntryRef complexFormRef, ICmObject component,
			string undoText, string redoText)
			=> Toggle(complexFormRef.ShowComplexFormsInRS, complexFormRef, component, undoText, redoText);

		// Removes a marked component; otherwise inserts it where the ref's component order puts
		// it. A component the ref does not list is left unmarked.
		private static void Toggle(ILcmReferenceSequence<ICmObject> marked, ILexEntryRef complexFormRef,
			ICmObject component, string undoText, string redoText)
		{
			UndoableUnitOfWorkHelper.Do(undoText, redoText, complexFormRef.Cache.ActionHandlerAccessor, () =>
			{
				if (marked.Contains(component))
				{
					marked.Remove(component);
					return;
				}
				var index = 0;
				foreach (var listed in complexFormRef.ComponentLexemesRS)
				{
					if (listed == component)
					{
						marked.Insert(index, component);
						return;
					}
					if (marked.Contains(listed))
						index++;
				}
			});
		}
	}
}
