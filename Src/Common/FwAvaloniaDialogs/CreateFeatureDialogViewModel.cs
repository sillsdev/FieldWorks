// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the small "Create New Feature" / "Create New Feature Value" name-entry dialog (Phase-1 §19b
	/// Stage 3) — the LCModel-free collector behind the inline create affordances of
	/// <see cref="FwFeatureStructureEditor"/>. It is the Avalonia replacement for the
	/// <c>MasterInflectionFeatureListDlg</c> / <c>MasterPhonologicalFeatureListDlg</c> blank-create link (and the
	/// feature-system add-value flow): the user types a name (and optional abbreviation) and the LCModel-aware
	/// launcher creates the feature/value in the feature system, then feeds the resulting <see cref="FwFeatureNode"/>
	/// back to the editor.
	///
	/// The dialog is intentionally minimal (the heavy MGA-catalog import path is a documented PARITY deferral — it
	/// needs the MGA assembly + GlossList XML parsing, outside this stage's clean reach). OK is gated on a non-empty
	/// name. The same VM serves both the feature and the value flows, parameterized by its labels (so the view binds
	/// the right captions without a second VM type).
	/// </summary>
	public partial class CreateFeatureDialogViewModel : DialogViewModelBase
	{
		[ObservableProperty] private string _name = string.Empty;
		[ObservableProperty] private string _abbreviation = string.Empty;

		public CreateFeatureDialogViewModel()
			: this(FwAvaloniaDialogsStrings.CreateFeatureTitle, FwAvaloniaDialogsStrings.CreateFeatureNameLabel,
				FwAvaloniaDialogsStrings.CreateFeatureAbbrLabel, FwAvaloniaDialogsStrings.CreateFeatureNameRequired)
		{
		}

		public CreateFeatureDialogViewModel(string title, string nameLabel, string abbrLabel, string nameRequiredMessage)
		{
			Title = title ?? FwAvaloniaDialogsStrings.CreateFeatureTitle;
			NameLabel = nameLabel ?? FwAvaloniaDialogsStrings.CreateFeatureNameLabel;
			AbbreviationLabel = abbrLabel ?? FwAvaloniaDialogsStrings.CreateFeatureAbbrLabel;
			NameRequiredMessage = nameRequiredMessage ?? FwAvaloniaDialogsStrings.CreateFeatureNameRequired;
		}

		/// <summary>Builds the VM for the create-FEATURE flow (the feature-system create-feature affordance).</summary>
		public static CreateFeatureDialogViewModel ForFeature() => new CreateFeatureDialogViewModel(
			FwAvaloniaDialogsStrings.CreateFeatureTitle, FwAvaloniaDialogsStrings.CreateFeatureNameLabel,
			FwAvaloniaDialogsStrings.CreateFeatureAbbrLabel, FwAvaloniaDialogsStrings.CreateFeatureNameRequired);

		/// <summary>Builds the VM for the create-VALUE flow (a closed feature's add-value affordance).</summary>
		public static CreateFeatureDialogViewModel ForValue() => new CreateFeatureDialogViewModel(
			FwAvaloniaDialogsStrings.CreateValueTitle, FwAvaloniaDialogsStrings.CreateValueNameLabel,
			FwAvaloniaDialogsStrings.CreateValueAbbrLabel, FwAvaloniaDialogsStrings.CreateValueNameRequired);

		/// <summary>The dialog window title.</summary>
		public string Title { get; }

		/// <summary>The label for the name field.</summary>
		public string NameLabel { get; }

		/// <summary>The label for the abbreviation field.</summary>
		public string AbbreviationLabel { get; }

		/// <summary>The OK-gate message shown when the name is empty.</summary>
		public string NameRequiredMessage { get; }

		/// <summary>The trimmed name the user entered (read by the launcher on OK).</summary>
		public string ChosenName => (Name ?? string.Empty).Trim();

		/// <summary>The trimmed abbreviation the user entered (read by the launcher on OK); may be empty.</summary>
		public string ChosenAbbreviation => (Abbreviation ?? string.Empty).Trim();

		partial void OnNameChanged(string value) => RefreshCanOk();

		/// <summary>OK is gated on a non-empty name (a feature/value must be named).</summary>
		protected override IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrWhiteSpace(Name))
				yield return NameRequiredMessage;
		}
	}
}
