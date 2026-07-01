// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// §19d (T1 dialog / T3 cancel-vs-commit / T5 visual): the picture-properties dialog — the Avalonia
	/// replacement for the WinForms PicturePropertiesDialog. Edits caption/description/license/creator and
	/// carries the chosen image file. A NEW picture gates OK on a chosen file; an existing one allows
	/// metadata-only edits. OK snapshots the result; Cancel writes nothing.
	/// </summary>
	[TestFixture]
	public class PicturePropertiesDialogTests
	{
		[AvaloniaTest]
		public void NewPicture_OkBlockedUntilFileChosen_ThenCarriesMetadataAndFile()
		{
			var vm = new PicturePropertiesDialogViewModel(null, isNew: true)
			{
				Caption = "a kitten",
				Creator = "Ada",
				License = "CC-BY"
			};
			Assert.That(vm.IsValid, Is.False, "a new picture cannot be created without an image file");

			vm.SetImageFile("C:/kitten.png");
			Assert.That(vm.IsValid, Is.True, "choosing a file enables OK");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.Result, Is.Not.Null);
			Assert.That(vm.Result.SourceFile, Is.EqualTo("C:/kitten.png"));
			Assert.That(vm.Result.Metadata.Caption, Is.EqualTo("a kitten"));
			Assert.That(vm.Result.Metadata.Creator, Is.EqualTo("Ada"));
			Assert.That(vm.Result.Metadata.License, Is.EqualTo("CC-BY"));
		}

		[AvaloniaTest]
		public void ExistingPicture_MetadataOnly_Ok_NoFile()
		{
			var vm = new PicturePropertiesDialogViewModel(
				new RegionPictureMetadata(caption: "old", creator: "Bo"), isNew: false);
			Assert.That(vm.IsValid, Is.True, "an existing picture allows metadata-only edits (no file required)");

			vm.Caption = "edited";
			vm.OkCommand.Execute(null);

			Assert.That(vm.Result, Is.Not.Null);
			Assert.That(vm.Result.SourceFile, Is.Null, "no replacement file → metadata-only");
			Assert.That(vm.Result.Metadata.Caption, Is.EqualTo("edited"));
			Assert.That(vm.Result.Metadata.Creator, Is.EqualTo("Bo"));
		}

		[AvaloniaTest]
		public void Cancel_WritesNothing()
		{
			var vm = new PicturePropertiesDialogViewModel(
				new RegionPictureMetadata(caption: "keep"), isNew: false);
			vm.Caption = "should not persist";
			vm.CancelCommand.Execute(null);

			Assert.That(vm.Accepted, Is.False);
			Assert.That(vm.Result, Is.Null, "a cancelled dialog snapshots nothing");
		}

		[AvaloniaTest]
		public void Visual_PicturePropertiesDialog_RendersCleanly()
		{
			var vm = new PicturePropertiesDialogViewModel(
				new RegionPictureMetadata(caption: "a kitten", description: "tabby", license: "CC-BY", creator: "Ada"),
				isNew: false);
			var view = new PicturePropertiesDialogView(vm);

			// Capture hosts the view in a headless window itself (do not pre-parent it).
			DialogSnapshot.Capture(view, "PictureProperties-01-initial", width: 440, height: 360);
			DialogLayoutAssert.AssertNoCrowding(view);
		}
	}
}
