// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests; // FakeDetailEditContext — the LCModel-free editing seam fake (DetailEditingTests.cs)
using FwAvaloniaDialogsTests; // DialogLayoutAssert — the shared layout tripwire (linked in via the csproj)

namespace FwAvaloniaTests.VisualChecks
{
	/// <summary>
	/// Self-test for the <see cref="DialogSnapshot"/> PNG harness: every visual headless test can emit a
	/// real Skia-rendered frame to the gitignored ephemeral folder so the agent (via Read) and the user can
	/// eyeball whether the capture looks right -- the subjective check that complements the deterministic
	/// <see cref="DialogLayoutAssert"/> tripwire. The PNG is ALWAYS produced, even when sanity is clean.
	/// </summary>
	[TestFixture]
	public class DialogSnapshotTests
	{
		[AvaloniaTest]
		public void Capture_WritesANonEmptyPngUnderTheEphemeralFolder()
		{
			var view = new StackPanel
			{
				Children =
				{
					new TextBlock { Text = "Snapshot harness self-test" },
					new Button { Content = "OK" }
				}
			};

			var path = DialogSnapshot.Capture(view, "HarnessSelfTest-01-initial");

			Assert.That(File.Exists(path), Is.True, $"the snapshot should be written to {path}");
			Assert.That(new FileInfo(path).Length, Is.GreaterThan(0), "the PNG must contain pixels");
			Assert.That(path.Replace('\\', '/'), Does.Contain("Output/Snapshots/HarnessSelfTest-01-initial.png"),
				"snapshots go to ONE flat folder with a prefixed file name");
		}
	}

	/// <summary>
	/// Detail visual coverage: the owned non-dialog control (the detail view)
	/// gets the SAME treatment as dialogs -- a real PNG snapshot for subjective review AND the
	/// shared <see cref="DialogLayoutAssert"/> hard-fail tripwire (overlap / zero-area text / crowding) --
	/// so the visual standard is one standard across the Avalonia UI, not dialogs only.
	/// Capture happens BEFORE the assertion so the artifact exists for review even when the assertion fails.
	/// The crowding tripwire itself now skips SPLITTER CONTROLS (a GridSplitter / any "Splitter"-named control
	/// straddles a column boundary by design); that splitter-aware exception lives inside DialogLayoutAssert,
	/// so these tests just call AssertNoCrowding directly -- no in-test splitter workaround.
	/// </summary>
	[TestFixture]
	public class DetailSnapshotTests
	{
		[AvaloniaTest]
		public void DetailEditView_RendersCleanly()
		{
			// Read-only display stage: the detail view is FLAT with subtle field separators (the WinForms
			// DataTree look) -- labels + values at the WinForms density font, no boxing per value.
			var model = DetailModelProjector.FromViewDefinition(DetailDefinition(), new TwoFieldProvider());
			var view = new DataTree(model);

			DialogSnapshot.Capture(view, "Detail-01-initial", width: 420, height: 200);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void DetailEditView_Editable_RendersCleanly()
		{
			// Editable stage: an edit context is supplied so the value editors are live; the view must still
			// read flat/dense (no per-field box) the way the legacy editable DataTree does.
			var model = DetailModelProjector.FromViewDefinition(DetailDefinition(), new TwoFieldProvider());
			var view = new DataTree(model, new FakeDetailEditContext());

			DialogSnapshot.Capture(view, "Detail-02-editable", width: 420, height: 200);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		// ----- realistic detail fixture (10 fields of varied kinds, no LCModel) -----

		[AvaloniaTest]
		public void DetailEditView_RealisticMultiField_RendersCleanly()
		{
			// Read-only display of a realistic, dense entry: multistring vernacular + analysis, a single-line
			// citation, a part-of-speech chooser, a date, a generic-date, an enum/option chooser, a boolean,
			// a reference vector, and a multi-line note -- the spread of kinds a real lexeme-entry detail shows.
			// It must still read FLAT/dense (the WinForms DataTree look) with thin field separators, no boxing.
			var view = new DataTree(RealisticDetailModel());

			DialogSnapshot.Capture(view, "Detail-03-multi-field", width: 520, height: 420);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void DetailEditView_RealisticMultiField_Editable_RendersCleanly()
		{
			// The same realistic field spread, now editable (an edit context makes the text/chooser editors
			// live; dropped editors render Unsupported). The dense flat look must survive: live editors, no
			// per-field box, aligned columns.
			var view = new DataTree(RealisticDetailModel(), new FakeDetailEditContext());

			DialogSnapshot.Capture(view, "Detail-04-editable-multi", width: 520, height: 420);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void DetailEditView_Reference_RendersCleanly()
		{
			// A focused stage on a reference-vector row (the legacy possibility-vector
			// slice), editable -- confirming chip-like items and the add launcher render
			// without crowding neighbours.
			var fields = new List<DetailField>
			{
				ReferenceVectorField("d/#ref", "Publish In", "PublishIn",
					new[] { ("p1", "Main Dictionary"), ("p2", "Pocket Dictionary") },
					new[] { ("p1", "Main Dictionary"), ("p2", "Pocket Dictionary"), ("p3", "School Dictionary") })
			};
			var view = new DataTree(
				new DetailModel("LexEntry", "detail", fields, new List<ViewDiagnostic>()),
				new FakeDetailEditContext());

			DialogSnapshot.Capture(view, "Detail-06-reference", width: 520, height: 200);
			DialogLayoutAssert.AssertNoCrowding(view);
		}


		// ----- minimal fixtures (no LCModel) -----

		private static ViewDefinitionModel DetailDefinition() => new ViewDefinitionModel(
			"LexEntry", "detail", "detail", new List<ViewNode>
			{
				new ViewNode("d/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: HostRouting.Product),
				new ViewNode("d/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "GlossEditor", routing: HostRouting.Product)
			}, new List<ViewDiagnostic>());

		private sealed class TwoFieldProvider : IDetailValueProvider
		{
			public IReadOnlyList<DetailWsValue> GetValues(ViewNode fieldNode)
				=> new[] { new DetailWsValue("en", fieldNode.Field == "Form" ? "casa" : "house", wsTag: "en") };
			public IReadOnlyList<DetailChoiceOption> GetOptions(ViewNode fieldNode) => Array.Empty<DetailChoiceOption>();
			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		// ----- realistic detail fixture builders: fields are built directly (not via the mapper) so kinds beyond Text/Chooser/Unsupported, which the mapper doesn't classify, get exercised too -----

		// A realistic lexeme-entry detail: 10 fields of varied kinds, mirroring what the lexical edit pane shows.
		private static DetailModel RealisticDetailModel()
		{
			var fields = new List<DetailField>
			{
				// Multistring vernacular (two writing systems) -- the headword.
				TextField("d/#0", "Lexeme Form", "LexemeForm", "LexemeFormEditor",
					new[] { ("seh", "casa", "seh"), ("pt", "casa", "pt") }),
				// Single-line citation form.
				TextField("d/#1", "Citation Form", "CitationForm", "CitationFormEditor",
					new[] { ("seh", "casa", "seh") }),
				// Multistring analysis gloss (two analysis writing systems).
				TextField("d/#2", "Gloss", "Gloss", "GlossEditor",
					new[] { ("en", "house", "en"), ("pt", "casa", "pt") }),
				// Part-of-speech chooser (atomic reference).
				ChooserField("d/#3", "Grammatical Info.", "MorphoSyntaxAnalysis", "g2",
					new[] { ("g1", "Verb"), ("g2", "Noun"), ("g3", "Adjective") }),
				// Dropped editors (date/gendate) now render the labeled Unsupported worklist row.
				UnsupportedField("d/#4", "Date Created", "DateCreated"),
				UnsupportedField("d/#5", "Date Of Birth", "DateOfBirth"),
				// Enum/option chooser.
				ChooserField("d/#6", "Status", "Status", "s2",
					new[] { ("s1", "Confirmed"), ("s2", "Pending"), ("s3", "Disproven") }),
				// Dropped editor (boolean checkbox) now renders the labeled Unsupported worklist row.
				UnsupportedField("d/#7", "Exclude As Headword", "ExcludeAsHeadword"),
				// Reference vector (current items + add slot).
				ReferenceVectorField("d/#8", "Publish In", "PublishIn",
					new[] { ("p1", "Main Dictionary") },
					new[] { ("p1", "Main Dictionary"), ("p2", "Pocket Dictionary") }),
				// Multi-line note.
				TextField("d/#9", "General Note", "GeneralNote", "GeneralNoteEditor",
					new[] { ("en", "Borrowed from Portuguese; common across the region.", "en") })
			};
			return new DetailModel("LexEntry", "detail", fields, new List<ViewDiagnostic>());
		}

		private static DetailField TextField(string stableId, string label, string field,
			string automationId, (string abbrev, string value, string tag)[] values)
		{
			var wsValues = new List<DetailWsValue>();
			foreach (var v in values)
				wsValues.Add(new DetailWsValue(v.abbrev, v.value, wsTag: v.tag));
			return new DetailField(stableId, label, field, null, DetailFieldKind.Text,
				EditorClassification.Known, automationId, null, HostRouting.Product, wsValues, null, null);
		}

		private static DetailField ChooserField(string stableId, string label, string field,
			string selectedKey, (string key, string name)[] options)
		{
			var opts = new List<DetailChoiceOption>();
			foreach (var o in options)
				opts.Add(new DetailChoiceOption(o.key, o.name));
			return new DetailField(stableId, label, field, null, DetailFieldKind.Chooser,
				EditorClassification.Known, field + "Chooser", null, HostRouting.Product, null, opts, selectedKey);
		}

		private static DetailField UnsupportedField(string stableId, string label, string field)
			=> new DetailField(stableId, label, field, null, DetailFieldKind.Unsupported,
				EditorClassification.Known, field + "Editor", null, HostRouting.Product, null, null, null,
				isEditable: false);

		private static DetailField ReferenceVectorField(string stableId, string label, string field,
			(string key, string name)[] items, (string key, string name)[] options)
		{
			var itemList = new List<DetailChoiceOption>();
			foreach (var i in items)
				itemList.Add(new DetailChoiceOption(i.key, i.name));
			var optList = new List<DetailChoiceOption>();
			foreach (var o in options)
				optList.Add(new DetailChoiceOption(o.key, o.name));
			return new DetailField(stableId, label, field, null, DetailFieldKind.ReferenceVector,
				EditorClassification.Known, field, null, HostRouting.Product, null, optList, null,
				isEditable: true, items: itemList);
		}

	}
}
