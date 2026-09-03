// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// The optional create-from-typed-text capability of a reference field, kept off the core
	/// <see cref="IDetailEditContext"/> so only a context that can actually mint a target object
	/// carries it. A caller acquires it with <c>ctx as IReferenceItemCreation</c> and treats a
	/// null
	/// result as "this row picks from the list only", exactly as
	/// <see cref="IStructuredTextEditing"/> is acquired.
	///
	/// It exists because some legacy reference slices are BOTH a chooser and a typed editor.
	/// Environments is the case in hand: <c>PhoneEnvReferenceLauncher</c> opens a
	/// <c>SimpleListChooser</c> over the existing environments, while its inline
	/// <c>PhoneEnvReferenceView</c> lets the user type a new environment string that
	/// <c>ConnectToRealCache</c> reconciles against the project -- finding an existing
	/// <c>PhEnvironment</c> or creating one. A picker alone cannot reach an environment the
	/// project does not have yet.
	/// </summary>
	public interface IReferenceItemCreation
	{
		/// <summary>
		/// Whether <paramref name="field"/> accepts creation from typed text. Drives whether the
		/// picker offers its create row at all, so it must not depend on what the user has typed.
		/// </summary>
		bool CanCreateReferenceItem(DetailField field);

		/// <summary>
		/// Finds or creates the target object named by <paramref name="text"/> and stages adding
		/// it
		/// to <paramref name="field"/>. Returns false -- without opening the session -- for a
		/// field
		/// that cannot create, or for text the domain cannot turn into an object at all.
		///
		/// Matching is the domain's business, not the caller's: environments match with spaces
		/// stripped (legacy's <c>RemoveSpaces</c>), so "/ # _" and "/#_" must resolve to the SAME
		/// object rather than creating a second one.
		///
		/// Validity is NOT a precondition. Legacy creates the object whether or not it passes
		/// domain validation and annotates the invalid one instead; rejecting it here would
		/// discard
		/// what the user typed.
		/// </summary>
		bool TryCreateAndAddReferenceItem(DetailField field, string text);
	}
}
