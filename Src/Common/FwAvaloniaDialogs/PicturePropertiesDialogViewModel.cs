// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using CommunityToolkit.Mvvm.ComponentModel;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// §19d: view-model for the Avalonia picture-properties dialog — the parity replacement for the
	/// WinForms <c>PicturePropertiesDialog</c>. Edits a picture's caption / description / license / creator
	/// and (for a NEW picture, or when replacing) carries the chosen image file. OK snapshots the edited
	/// metadata + file into <see cref="Result"/>; the launcher's Apply reads it. Spec-only (no LCModel,
	/// no file work here) — exactly like the chooser/InsertEntry/Find-Replace dialogs. For a new picture OK
	/// is gated on a chosen file (you cannot create a picture with no image); for an existing one the file
	/// is optional (metadata-only edits are allowed, replace is optional).
	/// </summary>
	public partial class PicturePropertiesDialogViewModel : DialogViewModelBase
	{
		private readonly bool _isNew;

		[ObservableProperty] private string _caption = string.Empty;
		[ObservableProperty] private string _description = string.Empty;
		[ObservableProperty] private string _license = string.Empty;
		[ObservableProperty] private string _creator = string.Empty;
		[ObservableProperty] private string _imageFile;

		public PicturePropertiesDialogViewModel() : this(null, true)
		{
		}

		public PicturePropertiesDialogViewModel(RegionPictureMetadata initial, bool isNew)
		{
			_isNew = isNew;
			var seed = initial ?? new RegionPictureMetadata();
			_caption = seed.Caption ?? string.Empty;
			_description = seed.Description ?? string.Empty;
			_license = seed.License ?? string.Empty;
			_creator = seed.Creator ?? string.Empty;
		}

		/// <summary>True for a new-picture dialog (OK requires a chosen file); false when editing an existing one.</summary>
		public bool IsNew => _isNew;

		/// <summary>The OK button caption: "Insert" for a new picture, "OK" otherwise.</summary>
		public string OkCaption => _isNew
			? FwAvaloniaDialogsStrings.PicturePropertiesInsert
			: FwAvaloniaDialogsStrings.Ok;

		/// <summary>The chosen/displayed image file path ("(no file chosen)" when none); the view binds it.</summary>
		public string ImageFileDisplay => string.IsNullOrEmpty(ImageFile)
			? FwAvaloniaDialogsStrings.PicturePropertiesNoFile
			: ImageFile;

		/// <summary>Snapshot written on OK; null until then. The launcher reads metadata + file from it.</summary>
		public RegionPictureDialogResult Result { get; private set; }

		/// <summary>
		/// Sets the chosen image file (called by the view's "Choose image…" button after the host's file
		/// picker returns) and re-gates OK.
		/// </summary>
		public void SetImageFile(string path)
		{
			ImageFile = path;
		}

		partial void OnImageFileChanged(string value)
		{
			OnPropertyChanged(nameof(ImageFileDisplay));
			RefreshCanOk();
		}

		// OK gate: a NEW picture must have a chosen file; an existing one may be metadata-only.
		protected override bool CanOk => !_isNew || !string.IsNullOrEmpty(ImageFile);

		protected override void ApplyChanges()
		{
			var metadata = new RegionPictureMetadata(
				caption: Caption ?? string.Empty,
				description: Description ?? string.Empty,
				license: License ?? string.Empty,
				creator: Creator ?? string.Empty);
			Result = new RegionPictureDialogResult(metadata, ImageFile);
		}
	}
}
