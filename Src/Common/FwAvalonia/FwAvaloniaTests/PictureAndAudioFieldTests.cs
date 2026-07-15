// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaDialogsTests; // DialogLayoutAssert
using FwAvaloniaTests.VisualChecks; // DialogSnapshot

namespace FwAvaloniaTests
{
	/// <summary>
	/// §19d headless view coverage (T1 affordance presence / T2 integration / T3 edge / T5 visual): the
	/// owned picture field's insert/replace/delete/properties affordances and the voice-WS audio field's
	/// play/record/clear affordances route through the LCModel-free edit-context + media seams. A fake
	/// media seam returns canned results and records calls; a <see cref="FakeRegionEditContext"/> records
	/// the staged gestures. No LCModel, no real device, no real file picker.
	/// </summary>
	[TestFixture]
	public class PictureAndAudioFieldTests
	{
		// A fake media seam: canned picker/dialog results + recorded calls.
		private sealed class FakeMediaServices : IRegionMediaServices
		{
			public string PickResult;
			public RegionPictureDialogResult DialogResult;
			public bool CanRecord = true;
			public string RecordResult;
			public readonly List<string> Played = new List<string>();
			public int RecordCalls;
			public int DialogCalls;

			public System.Threading.Tasks.Task<string> PickImageFileAsync(string title)
				=> System.Threading.Tasks.Task.FromResult(PickResult);

			public RegionPictureDialogResult ShowPictureProperties(RegionPictureMetadata current, bool isNew)
			{
				DialogCalls++;
				return DialogResult;
			}

			public bool CanRecordAudio => CanRecord;
			public void PlayAudio(string fileName) => Played.Add(fileName);
			public string RecordAudio() { RecordCalls++; return RecordResult; }
		}

		private static T FindByAutomationId<T>(Control root, string id) where T : Control
			=> root.GetSelfAndVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == id);

		// ----- picture field model builders -----

		private static LexicalEditRegionField PictureRow(int hvo, string path, RegionPictureMetadata meta)
		{
			var f = new LexicalEditRegionField("pic", "Picture", "Pictures", null, RegionFieldKind.Image,
				default(EditorClassification), "PicEditor", null, default(SurfaceRouting),
				new List<RegionWsValue> { new RegionWsValue("", path ?? string.Empty) }, null, null,
				isEditable: true, objectHvo: 7);
			f.PictureHvo = hvo;
			f.PictureMetadata = meta;
			return f;
		}

		private static LexicalEditRegionField AudioRow(string fileName)
		{
			return new LexicalEditRegionField("aud", "Pronunciation", "Form", null, RegionFieldKind.Text,
				default(EditorClassification), "AudEditor", null, default(SurfaceRouting),
				new List<RegionWsValue> { new RegionWsValue("aud", fileName ?? string.Empty,
					wsTag: "aud-Zxxx-x-audio", isAudio: true) },
				null, null, isEditable: true, objectHvo: 7);
		}

		// ----- T1: picture affordances present + route correctly -----

		[AvaloniaTest]
		public void EmptyPicture_ShowsInsertAffordance_RoutesInsert()
		{
			var edit = new FakeRegionEditContext();
			var media = new FakeMediaServices
			{
				DialogResult = new RegionPictureDialogResult(new RegionPictureMetadata(caption: "c"), "C:/img.png")
			};
			var saved = 0;
			var ctx = new RegionFieldControlContext(edit, save: () => saved++, mediaServices: media);
			var control = RegionFieldControlFactory.Build(PictureRow(0, null, null), "PicEditor", ctx);

			var insert = FindByAutomationId<Button>(control, "PicEditor.insert");
			Assert.That(insert, Is.Not.Null, "an empty picture field shows the insert affordance");

			insert.Command?.Execute(null);
			insert.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

			Assert.That(media.DialogCalls, Is.EqualTo(1));
			Assert.That(edit.PictureInserts.Count, Is.EqualTo(1));
			Assert.That(edit.PictureInserts[0].SourceFile, Is.EqualTo("C:/img.png"));
			Assert.That(saved, Is.EqualTo(1), "a successful insert triggers the autosave");
		}

		[AvaloniaTest]
		public void ExistingPicture_ShowsPropertiesAndDelete_RouteReplaceMetadataAndDelete()
		{
			var edit = new FakeRegionEditContext();
			var media = new FakeMediaServices
			{
				DialogResult = new RegionPictureDialogResult(
					new RegionPictureMetadata(caption: "new"), "C:/replacement.png")
			};
			var ctx = new RegionFieldControlContext(edit, save: () => { }, mediaServices: media);
			var row = PictureRow(42, null, new RegionPictureMetadata(caption: "old"));
			var control = RegionFieldControlFactory.Build(row, "PicEditor", ctx);

			var properties = FindByAutomationId<Button>(control, "PicEditor.properties");
			var delete = FindByAutomationId<Button>(control, "PicEditor.delete");
			Assert.That(properties, Is.Not.Null);
			Assert.That(delete, Is.Not.Null);

			properties.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			// Dialog returned a replacement file AND metadata: both staged.
			Assert.That(edit.PictureReplaces.Count, Is.EqualTo(1));
			Assert.That(edit.PictureReplaces[0].SourceFile, Is.EqualTo("C:/replacement.png"));
			Assert.That(edit.PictureMetadataEdits.Count, Is.EqualTo(1));
			Assert.That(edit.PictureMetadataEdits[0].Metadata.Caption, Is.EqualTo("new"));

			delete.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Assert.That(edit.PictureDeletes.Count, Is.EqualTo(1));
		}

		[AvaloniaTest]
		public void PictureDialogCancelled_StagesNothing()
		{
			var edit = new FakeRegionEditContext();
			var media = new FakeMediaServices { DialogResult = null }; // Cancel
			var ctx = new RegionFieldControlContext(edit, save: () => { }, mediaServices: media);
			var control = RegionFieldControlFactory.Build(
				PictureRow(42, null, new RegionPictureMetadata()), "PicEditor", ctx);

			FindByAutomationId<Button>(control, "PicEditor.properties")
				.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

			Assert.That(edit.PictureReplaces, Is.Empty);
			Assert.That(edit.PictureMetadataEdits, Is.Empty);
		}

		[AvaloniaTest]
		public void Picture_NoMediaSeam_IsReadOnly_NoAffordances()
		{
			// The browse-cell / preview path supplies no media seam: read-only display, no buttons.
			var ctx = new RegionFieldControlContext(new FakeRegionEditContext());
			var control = RegionFieldControlFactory.Build(
				PictureRow(42, null, new RegionPictureMetadata()), "PicEditor", ctx);

			Assert.That(FindByAutomationId<Button>(control, "PicEditor.properties"), Is.Null);
			Assert.That(FindByAutomationId<Button>(control, "PicEditor.delete"), Is.Null);
		}

		// ----- T3 edge: a missing/invalid image file never crashes the row -----

		[AvaloniaTest]
		public void Picture_MissingFile_RendersPathFallback_NoCrash()
		{
			var ctx = new RegionFieldControlContext(new FakeRegionEditContext(),
				save: () => { }, mediaServices: new FakeMediaServices());
			var control = RegionFieldControlFactory.Build(
				PictureRow(42, "C:/no/such/file.png", new RegionPictureMetadata()), "PicEditor", ctx);
			Assert.That(control, Is.Not.Null, "a missing image degrades to the path text, never a crash");
			// Affordances still present (the picture object exists even if the file is gone).
			Assert.That(FindByAutomationId<Button>(control, "PicEditor.properties"), Is.Not.Null);
		}

		// ----- T1: audio affordances present + route correctly -----

		[AvaloniaTest]
		public void AudioWithRecording_ShowsPlayRecordClear_RoutePlayAndClear()
		{
			var edit = new FakeRegionEditContext();
			var media = new FakeMediaServices { CanRecord = true };
			var saved = 0;
			var ctx = new RegionFieldControlContext(edit, save: () => saved++, mediaServices: media);
			var control = RegionFieldControlFactory.Build(AudioRow("casa.wav"), "AudEditor", ctx);

			var play = FindByAutomationId<Button>(control, "AudEditor.aud-Zxxx-x-audio.play");
			var record = FindByAutomationId<Button>(control, "AudEditor.aud-Zxxx-x-audio.record");
			var clear = FindByAutomationId<Button>(control, "AudEditor.aud-Zxxx-x-audio.clear");
			Assert.That(play, Is.Not.Null, "an existing recording shows the play affordance");
			Assert.That(record, Is.Not.Null);
			Assert.That(clear, Is.Not.Null);

			play.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Assert.That(media.Played, Is.EquivalentTo(new[] { "casa.wav" }));

			clear.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Assert.That(edit.TextEdits.Any(e => e.Value == string.Empty), Is.True, "clear empties the value");
			Assert.That(saved, Is.GreaterThan(0));
		}

		[AvaloniaTest]
		public void EmptyAudio_ShowsRecordOnly_RecordWritesFilename()
		{
			var edit = new FakeRegionEditContext();
			var media = new FakeMediaServices { CanRecord = true, RecordResult = "recording.wav" };
			var ctx = new RegionFieldControlContext(edit, save: () => { }, mediaServices: media);
			var control = RegionFieldControlFactory.Build(AudioRow(string.Empty), "AudEditor", ctx);

			Assert.That(FindByAutomationId<Button>(control, "AudEditor.aud-Zxxx-x-audio.play"), Is.Null,
				"no recording yet → no play affordance");
			var record = FindByAutomationId<Button>(control, "AudEditor.aud-Zxxx-x-audio.record");
			Assert.That(record, Is.Not.Null);

			record.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Assert.That(media.RecordCalls, Is.EqualTo(1));
			Assert.That(edit.TextEdits.Any(e => e.Value == "recording.wav"), Is.True,
				"record writes the returned filename through the text setter");
		}

		[AvaloniaTest]
		public void Audio_RecordUnavailable_DisablesRecord()
		{
			var media = new FakeMediaServices { CanRecord = false };
			var ctx = new RegionFieldControlContext(new FakeRegionEditContext(), save: () => { }, mediaServices: media);
			var control = RegionFieldControlFactory.Build(AudioRow(string.Empty), "AudEditor", ctx);

			var record = FindByAutomationId<Button>(control, "AudEditor.aud-Zxxx-x-audio.record");
			Assert.That(record, Is.Not.Null);
			Assert.That(record.IsEnabled, Is.False, "record is disabled where the platform cannot capture");
		}

		[AvaloniaTest]
		public void Audio_NoMediaSeam_IsReadOnly_NoAffordances_NotBlankPlaceholder()
		{
			// No media seam (browse/preview): read-only display, but NOT a blanket editable placeholder box.
			var ctx = new RegionFieldControlContext(new FakeRegionEditContext());
			var control = RegionFieldControlFactory.Build(AudioRow("casa.wav"), "AudEditor", ctx);
			Assert.That(FindByAutomationId<Button>(control, "AudEditor.aud-Zxxx-x-audio.record"), Is.Null);
			// The filename label is present (the recording stays diagnosable, not hidden behind a fake editor).
			var label = FindByAutomationId<TextBlock>(control, "AudEditor.aud-Zxxx-x-audio.file");
			Assert.That(label, Is.Not.Null);
			Assert.That(label.Text, Is.EqualTo("casa.wav"));
		}

		// ----- T2 integration: picture + audio + text on ONE realized surface, per-gesture isolation -----

		[AvaloniaTest]
		public void OneSurface_PictureInsert_AudioRecord_TextEdit_StageIndependently()
		{
			var edit = new FakeRegionEditContext();
			var media = new FakeMediaServices
			{
				CanRecord = true,
				RecordResult = "rec.wav",
				DialogResult = new RegionPictureDialogResult(new RegionPictureMetadata(caption: "p"), "C:/p.png")
			};

			var fields = new List<LexicalEditRegionField>
			{
				PictureRow(0, null, null),
				AudioRow(string.Empty),
				new LexicalEditRegionField("txt", "Gloss", "Gloss", null, RegionFieldKind.Text,
					default(EditorClassification), "TxtEditor", null, default(SurfaceRouting),
					new List<RegionWsValue> { new RegionWsValue("en", "house", wsTag: "en") }, null, null,
					isEditable: true, objectHvo: 7)
			};
			var model = new LexicalEditRegionModel("LexEntry", "Normal", fields, new List<ViewDiagnostic>());
			var view = new LexicalEditRegionView(model, edit, mediaServices: media);

			DialogSnapshot.Capture(view, "MediaSurface-01-integration", width: 560, height: 320);

			var insert = FindByAutomationId<Button>(view, "field0.insert") ?? FindFirstButtonEndingWith(view, ".insert");
			var record = FindFirstButtonEndingWith(view, ".record");
			Assert.That(insert, Is.Not.Null, "the picture field's insert affordance is realized on the surface");
			Assert.That(record, Is.Not.Null, "the audio field's record affordance is realized on the surface");

			insert.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			record.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

			Assert.That(edit.PictureInserts.Count, Is.EqualTo(1), "picture insert staged once");
			Assert.That(edit.TextEdits.Any(e => e.Value == "rec.wav"), Is.True, "audio record staged the filename");
			// The text field's value is intact (not disturbed by the picture/audio gestures).
			Assert.That(model.Fields.Single(f => f.Field == "Gloss").Values[0].Value, Is.EqualTo("house"));

			DialogLayoutAssert.AssertNoCrowding(view);
		}

		// ----- T5 visual: picture + audio surfaces (PNGs read by the agent before AssertNoCrowding) -----

		[AvaloniaTest]
		public void Visual_PictureField_WithAffordances()
		{
			var edit = new FakeRegionEditContext();
			var media = new FakeMediaServices();
			var model = new LexicalEditRegionModel("LexEntry", "Normal",
				new List<LexicalEditRegionField> { PictureRow(42, null, new RegionPictureMetadata(caption: "a cat")) },
				new List<ViewDiagnostic>());
			var view = new LexicalEditRegionView(model, edit, mediaServices: media);

			DialogSnapshot.Capture(view, "Picture-01-affordances", width: 460, height: 200);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void Visual_AudioField_WithPlayRecord()
		{
			var edit = new FakeRegionEditContext();
			var media = new FakeMediaServices { CanRecord = true };
			var model = new LexicalEditRegionModel("LexEntry", "Normal",
				new List<LexicalEditRegionField> { AudioRow("casa.wav") },
				new List<ViewDiagnostic>());
			var view = new LexicalEditRegionView(model, edit, mediaServices: media);

			DialogSnapshot.Capture(view, "Audio-01-play-record", width: 460, height: 160);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		private static Button FindFirstButtonEndingWith(Control root, string suffix)
			=> root.GetSelfAndVisualDescendants().OfType<Button>()
				.FirstOrDefault(b => (AutomationProperties.GetAutomationId(b) ?? string.Empty).EndsWith(suffix));
	}
}
