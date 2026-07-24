// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests; // FakeRegionEditContext — the LCModel-free editing seam fake (RegionEditingTests.cs)
using FwAvaloniaDialogsTests; // DialogLayoutAssert — the shared layout tripwire (linked in via the csproj)

namespace FwAvaloniaTests.VisualChecks
{
	/// <summary>
	/// Self-test for the <see cref="DialogSnapshot"/> PNG harness: every visual headless test can emit a
	/// real Skia-rendered frame to the gitignored ephemeral folder so the agent (via Read) and the user can
	/// eyeball whether the surface looks right — the subjective check that complements the deterministic
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
				"snapshots go to ONE flat folder with a surface-prefixed file name");
		}
	}

	/// <summary>
	/// Region visual coverage: the owned non-dialog surface (the lexical-edit detail view)
	/// gets the SAME treatment as dialogs — a real PNG snapshot for subjective review AND the
	/// shared <see cref="DialogLayoutAssert"/> hard-fail tripwire (overlap / zero-area text / crowding) —
	/// so the visual standard is one standard across every Avalonia surface, not dialogs only.
	/// Capture happens BEFORE the assertion so the artifact exists for review even when the assertion fails.
	/// The crowding tripwire itself now skips SPLITTER CONTROLS (a GridSplitter / any "Splitter"-named control
	/// straddles a column boundary by design); that splitter-aware exception lives inside DialogLayoutAssert,
	/// so these tests just call AssertNoCrowding directly — no in-test splitter workaround.
	/// </summary>
	[TestFixture]
	public class RegionSnapshotTests
	{
		[AvaloniaTest]
		public void RegionEditView_RendersCleanly()
		{
			// Read-only display stage: the detail view is FLAT with subtle field separators (the WinForms
			// DataTree look) — labels + values at the WinForms density font, no boxing per value.
			var model = LexicalEditRegionMapper.FromViewDefinition(RegionDefinition(), new TwoFieldProvider());
			var view = new LexicalEditRegionView(model);

			DialogSnapshot.Capture(view, "Region-01-initial", width: 420, height: 200);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void RegionEditView_Editable_RendersCleanly()
		{
			// Editable stage: an edit context is supplied so the value editors are live; the surface must still
			// read flat/dense (no per-field box) the way the legacy editable DataTree does.
			var model = LexicalEditRegionMapper.FromViewDefinition(RegionDefinition(), new TwoFieldProvider());
			var view = new LexicalEditRegionView(model, new FakeRegionEditContext());

			DialogSnapshot.Capture(view, "Region-02-editable", width: 420, height: 200);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		// ----- realistic region detail fixture (10 fields of varied kinds, no LCModel) -----

		[AvaloniaTest]
		public void RegionEditView_RealisticMultiField_RendersCleanly()
		{
			// Read-only display of a realistic, dense entry: multistring vernacular + analysis, a single-line
			// citation, a part-of-speech chooser, a date, a generic-date, an enum/option chooser, a boolean,
			// a reference vector, and a multi-line note — the spread of kinds a real lexeme-entry detail shows.
			// It must still read FLAT/dense (the WinForms DataTree look) with thin field separators, no boxing.
			var view = new LexicalEditRegionView(RealisticRegionModel());

			DialogSnapshot.Capture(view, "Region-03-multi-field", width: 520, height: 420);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void RegionEditView_RealisticMultiField_Editable_RendersCleanly()
		{
			// The same realistic field spread, now editable (an edit context makes the text/chooser/date/enum
			// editors live). The dense flat look must survive: live editors, no per-field box, aligned columns.
			var view = new LexicalEditRegionView(RealisticRegionModel(), new FakeRegionEditContext());

			DialogSnapshot.Capture(view, "Region-04-editable-multi", width: 520, height: 420);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void RegionEditView_DateAndEnum_RendersCleanly()
		{
			// A focused stage on the type-specific editors: an exact date, a generic (vague) date, and an
			// enum/option chooser, editable — confirming the date entries and the option row render with
			// WinForms density and no clipping at their own (narrower) widths.
			var fields = new List<LexicalEditRegionField>
			{
				DateField("d/#date", "Date Created", "DateCreated", "3 Jun 2026", RegionDateKind.Date),
				DateField("d/#gendate", "Date Of Birth", "DateOfBirth", "early 1900s", RegionDateKind.GenDate),
				ChooserField("d/#enum", "Status", "Status", "s2",
					new[] { ("s1", "Confirmed"), ("s2", "Pending"), ("s3", "Disproven") })
			};
			var view = new LexicalEditRegionView(
				new LexicalEditRegionModel("LexEntry", "detail", fields, new List<ViewDiagnostic>()),
				new FakeRegionEditContext());

			DialogSnapshot.Capture(view, "Region-05-date-and-enum", width: 520, height: 220);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void RegionEditView_Reference_RendersCleanly()
		{
			// A focused stage on a reference-vector row (the legacy possibility-vector slice with its current
			// items + trailing add slot), editable — confirming the chip-like items and the add launcher render
			// without crowding their neighbours.
			var fields = new List<LexicalEditRegionField>
			{
				ReferenceVectorField("d/#ref", "Publish In", "PublishIn",
					new[] { ("p1", "Main Dictionary"), ("p2", "Pocket Dictionary") },
					new[] { ("p1", "Main Dictionary"), ("p2", "Pocket Dictionary"), ("p3", "School Dictionary") })
			};
			var view = new LexicalEditRegionView(
				new LexicalEditRegionModel("LexEntry", "detail", fields, new List<ViewDiagnostic>()),
				new FakeRegionEditContext());

			DialogSnapshot.Capture(view, "Region-06-reference", width: 520, height: 200);
			DialogLayoutAssert.AssertNoCrowding(view);
		}


		// ----- minimal surface fixtures (no LCModel) -----

		private static ViewDefinitionModel RegionDefinition() => new ViewDefinitionModel(
			"LexEntry", "detail", "detail", new List<ViewNode>
			{
				new ViewNode("d/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: SurfaceRouting.Product),
				new ViewNode("d/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "GlossEditor", routing: SurfaceRouting.Product)
			}, new List<ViewDiagnostic>());

		private sealed class TwoFieldProvider : IRegionValueProvider
		{
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> new[] { new RegionWsValue("en", fieldNode.Field == "Form" ? "casa" : "house", wsTag: "en") };
			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode) => Array.Empty<RegionChoiceOption>();
			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		// ----- realistic region fixture builders (fields built directly so kinds beyond Text/Chooser —
		// Date, Boolean, ReferenceVector — are exercised; the mapper only classifies Text/Chooser/Unsupported) -----

		// A realistic lexeme-entry detail: 10 fields of varied kinds, mirroring what the lexical edit pane shows.
		private static LexicalEditRegionModel RealisticRegionModel()
		{
			var fields = new List<LexicalEditRegionField>
			{
				// Multistring vernacular (two writing systems) — the headword.
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
				// Exact date.
				DateField("d/#4", "Date Created", "DateCreated", "3 Jun 2026", RegionDateKind.Date),
				// Generic (vague) date.
				DateField("d/#5", "Date Of Birth", "DateOfBirth", "early 1900s", RegionDateKind.GenDate),
				// Enum/option chooser.
				ChooserField("d/#6", "Status", "Status", "s2",
					new[] { ("s1", "Confirmed"), ("s2", "Pending"), ("s3", "Disproven") }),
				// Boolean checkbox.
				BooleanField("d/#7", "Exclude As Headword", "ExcludeAsHeadword", false),
				// Reference vector (current items + add slot).
				ReferenceVectorField("d/#8", "Publish In", "PublishIn",
					new[] { ("p1", "Main Dictionary") },
					new[] { ("p1", "Main Dictionary"), ("p2", "Pocket Dictionary") }),
				// Multi-line note.
				TextField("d/#9", "General Note", "GeneralNote", "GeneralNoteEditor",
					new[] { ("en", "Borrowed from Portuguese; common across the region.", "en") })
			};
			return new LexicalEditRegionModel("LexEntry", "detail", fields, new List<ViewDiagnostic>());
		}

		private static LexicalEditRegionField TextField(string stableId, string label, string field,
			string automationId, (string abbrev, string value, string tag)[] values)
		{
			var wsValues = new List<RegionWsValue>();
			foreach (var v in values)
				wsValues.Add(new RegionWsValue(v.abbrev, v.value, wsTag: v.tag));
			return new LexicalEditRegionField(stableId, label, field, null, RegionFieldKind.Text,
				EditorClassification.Known, automationId, null, SurfaceRouting.Product, wsValues, null, null);
		}

		private static LexicalEditRegionField ChooserField(string stableId, string label, string field,
			string selectedKey, (string key, string name)[] options)
		{
			var opts = new List<RegionChoiceOption>();
			foreach (var o in options)
				opts.Add(new RegionChoiceOption(o.key, o.name));
			return new LexicalEditRegionField(stableId, label, field, null, RegionFieldKind.Chooser,
				EditorClassification.Known, field + "Chooser", null, SurfaceRouting.Product, null, opts, selectedKey);
		}

		private static LexicalEditRegionField DateField(string stableId, string label, string field,
			string value, RegionDateKind dateKind)
			=> new LexicalEditRegionField(stableId, label, field, null, RegionFieldKind.Date,
				EditorClassification.Known, field + "Editor", null, SurfaceRouting.Product,
				new List<RegionWsValue> { new RegionWsValue("en", value, wsTag: "en") }, null, null,
				dateKind: dateKind);

		private static LexicalEditRegionField BooleanField(string stableId, string label, string field, bool value)
			=> new LexicalEditRegionField(stableId, label, field, null, RegionFieldKind.Boolean,
				EditorClassification.Known, field + "Editor", null, SurfaceRouting.Product, null, null,
				value ? "true" : "false");

		private static LexicalEditRegionField ReferenceVectorField(string stableId, string label, string field,
			(string key, string name)[] items, (string key, string name)[] options)
		{
			var itemList = new List<RegionChoiceOption>();
			foreach (var i in items)
				itemList.Add(new RegionChoiceOption(i.key, i.name));
			var optList = new List<RegionChoiceOption>();
			foreach (var o in options)
				optList.Add(new RegionChoiceOption(o.key, o.name));
			return new LexicalEditRegionField(stableId, label, field, null, RegionFieldKind.ReferenceVector,
				EditorClassification.Known, field, null, SurfaceRouting.Product, null, optList, null,
				isEditable: true, items: itemList);
		}

	}
}
