// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// The optional typed-text capability of a reference field -- creating a target object from
	/// what the user types, and re-pointing an existing item at what they type over it. Kept off
	/// the core <see cref="IDetailEditContext"/> so only a context that can actually reconcile
	/// text against the domain carries it. A caller acquires it with
	/// <c>ctx as IReferenceTextEditing</c> and treats a null result as "this row picks from the
	/// list only", exactly as <see cref="IStructuredTextEditing"/> is acquired.
	///
	/// It exists because some reference rows are BOTH a chooser and a typed editor.
	/// Environments is the case in hand: <c>PhoneEnvReferenceLauncher</c> opens a
	/// <c>SimpleListChooser</c> over the existing environments, while its inline
	/// <c>PhoneEnvReferenceView</c> lets the user type a new environment string that
	/// <c>ConnectToRealCache</c> reconciles against the project -- finding an existing
	/// <c>PhEnvironment</c> or creating one. A picker alone cannot reach an environment the
	/// project does not have yet.
	/// </summary>
	public interface IReferenceTextEditing
	{
		/// <summary>
		/// Whether <paramref name="field"/> accepts creation from typed text. Drives whether the
		/// picker offers its create row at all, so it must not depend on what the user has typed.
		/// </summary>
		bool CanCreateReferenceItem(DetailField field);

		/// <summary>
		/// Finds or creates the target object named by <paramref name="text"/> and stages adding
		/// it to <paramref name="field"/>. Returns false -- without opening the session -- for a
		/// field that cannot create, or for text the domain cannot turn into an object at all.
		///
		/// Matching is the domain's business, not the caller's: environments match with spaces
		/// stripped (<c>PhoneEnvReferenceView.RemoveSpaces</c>), so "/ # _" and "/#_" must
		/// resolve to the SAME object rather than creating a second one.
		///
		/// Validity is NOT a precondition. The object is created whether or not it passes
		/// domain validation and annotates the invalid one instead; rejecting it here would
		/// discard what the user typed.
		/// </summary>
		bool TryCreateAndAddReferenceItem(DetailField field, string text);

		/// <summary>
		/// Whether <paramref name="field"/> lets its existing items be retyped, which decides
		/// whether the row renders them as editable text at all. Independent of any particular
		/// item, and of what the user has typed.
		/// </summary>
		bool CanEditReferenceItemText(DetailField field);

		/// <summary>
		/// Re-points the item named by <paramref name="itemKey"/> at whatever
		/// <paramref name="text"/> names, staging the change. Returns false -- without opening
		/// the session -- for a field that cannot do this, or a key the field does not carry.
		///
		/// Addressed by key rather than position because these rows are reference COLLECTIONS.
		/// PhoneEnv is unordered, so an index means only "wherever it sat when composed".
		///
		/// Reconciliation belongs to the domain, and is NOT simply "find or create". An
		/// environment is identified by its text with spaces stripped. Text stripping to what
		/// the item already names leaves the reference alone and RENAMES the shared object,
		/// which every field referencing it then shows. Text stripping differently re-points
		/// the item, creating the target only when the project has none.
		///
		/// Blank text is a REMOVAL, not a rejected edit: emptying an item takes it off the
		/// field and creates nothing. Whitespace alone counts as blank.
		///
		/// Validity is not a precondition, for the same reason it is not on creation: the
		/// value is staged and annotated, never corrected or discarded.
		/// </summary>
		bool TrySetReferenceItemText(DetailField field, string itemKey, string text);
	}
}
