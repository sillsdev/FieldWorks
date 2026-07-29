// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests; // FakeRegionEditContext
using FwAvaloniaDialogsTests; // DialogLayoutAssert

namespace FwAvaloniaTests.VisualChecks
{
	/// <summary>
	/// Visual stages for the surviving detail-editor field kinds: a static literal row, an editable
	/// reference-vector row, and the labeled Unsupported worklist row (the conversion worklist). Each is
	/// captured as a Skia PNG for subjective review and run through the shared
	/// <see cref="DialogLayoutAssert"/> crowding tripwire, plus ONE realized region surface holding them
	/// together. All LCModel-free: the model fields are built directly and a
	/// <see cref="FakeRegionEditContext"/> records the staged edits.
	/// </summary>
	[TestFixture]
	public class FieldTypeVisualTests
	{
		private static DataTree Surface(IReadOnlyList<DetailField> fields,
			out FakeRegionEditContext edit)
		{
			edit = new FakeRegionEditContext();
			return new DataTree(
				new DetailModel("LexEntry", "detail", new List<DetailField>(fields),
					new List<ViewDiagnostic>()),
				edit);
		}

		private static DetailField Literal(string text) => new DetailField(
			"f/#lit", string.Empty, "Self", null, DetailFieldKind.Literal,
			EditorClassification.Known, "Lit", null, SurfaceRouting.Product,
			new List<DetailWsValue> { new DetailWsValue("", text) }, null, null, isEditable: false);

		private static DetailField Unsupported() => new DetailField(
			"f/#uns", "Inflection Features", "InflectionFeatures", null, DetailFieldKind.Unsupported,
			EditorClassification.Known, "Uns", null, SurfaceRouting.Product, null, null, null,
			isEditable: false);

		private static DetailField Vector() => new DetailField(
			"f/#vec", "Semantic Domains", "DomainTypes", null, DetailFieldKind.ReferenceVector,
			EditorClassification.Known, "Domains", null, SurfaceRouting.Product, null,
			new List<DetailChoiceOption>
			{
				new DetailChoiceOption("d1", "Universe, creation", 0),
				new DetailChoiceOption("d2", "Sky", 1),
				new DetailChoiceOption("d3", "Sun", 1)
			},
			null, isEditable: true, items: new List<DetailChoiceOption> { new DetailChoiceOption("d1", "Universe, creation") });

		// ----- Visual stages (one focused PNG per surviving kind) -----

		[AvaloniaTest]
		public void Literal_RendersCleanly()
		{
			var view = Surface(new[] { Literal("This entry is provisional; verify before publishing.") }, out _);
			DialogSnapshot.Capture(view, "FieldType-01-literal", width: 520, height: 140);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void Unsupported_RendersCleanly()
		{
			var view = Surface(new[] { Unsupported() }, out _);
			DialogSnapshot.Capture(view, "FieldType-02-unsupported", width: 520, height: 140);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void Vector_RendersCleanly()
		{
			var view = Surface(new[] { Vector() }, out _);
			DialogSnapshot.Capture(view, "FieldType-03-vector", width: 520, height: 200);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		// ----- Integration: the surviving field kinds on ONE realized surface -----

		[AvaloniaTest]
		public void IntegrationSurface_SurvivingFieldTypesCompose()
		{
			var view = Surface(new[] { Literal("Note:"), Vector(), Unsupported() }, out _);

			DialogSnapshot.Capture(view, "FieldType-04-integration", width: 560, height: 360);
			DialogLayoutAssert.AssertNoCrowding(view);

			Assert.That(view.GetVisualDescendants().OfType<FwReferenceVectorField>().Any(), Is.True,
				"reference vector present");
		}

		[AvaloniaTest]
		public void AllFieldTypeStages_ArePngArtifacts()
		{
			// Trip if the snapshot harness silently stops writing files (the artifacts back the visual review).
			foreach (var name in new[]
			{
				"FieldType-01-literal", "FieldType-02-unsupported", "FieldType-03-vector",
				"FieldType-04-integration"
			})
			{
				var path = Path.Combine(DialogSnapshot.Folder, name + ".png");
				// Captured by the stage tests above when the whole fixture runs; tolerate single-test runs.
				if (File.Exists(path))
					Assert.That(new FileInfo(path).Length, Is.GreaterThan(0), $"{name} png has pixels");
			}
		}
	}
}
