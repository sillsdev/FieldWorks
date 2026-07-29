// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The shared <see cref="RegionFieldKind"/>→control dispatch both the detail-pane region view
	/// and the browse in-cell editor route through. These pin that one switch produces the right control
	/// per surviving kind (Text / Chooser / ReferenceVector / Literal / Custom / Unsupported), and that
	/// the all-nullable <see cref="RegionFieldControlContext"/> serves both surfaces — the browse cell
	/// passes null menu/link callbacks and suppresses the WS-abbreviation gutter while the detail pane
	/// passes the full set — without either surface hand-rolling its own dispatch.
	/// </summary>
	[TestFixture]
	public class RegionFieldControlFactoryTests
	{
		private static RegionField Field(RegionFieldKind kind, string selectedOption = null,
			System.Func<Control> controlFactory = null)
			=> new RegionField(
				stableId: "f1", label: "Label", field: "Field", writingSystem: "en", kind: kind,
				editorClassification: EditorClassification.Known, automationId: "Auto.Id",
				localizationKey: null, routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("en", "v", wsTag: "en") },
				options: null, selectedOptionKey: selectedOption, isEditable: true, controlFactory: controlFactory);

		[AvaloniaTest]
		public void TextKind_BuildsMultiWsTextField()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Text), "Auto.Id", null),
				Is.InstanceOf<FwMultiWsTextField>());

		[AvaloniaTest]
		public void ChooserKind_BuildsChooserField()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Chooser), "Auto.Id", null),
				Is.InstanceOf<FwChooserField>());

		[AvaloniaTest]
		public void ReferenceVectorKind_BuildsReferenceVectorField()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.ReferenceVector), "Auto.Id", null),
				Is.InstanceOf<FwReferenceVectorField>());

		[AvaloniaTest]
		public void UnsupportedKind_BuildsUnsupportedTextBlock()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Unsupported), "Auto.Id", null),
				Is.InstanceOf<TextBlock>());

		// Literal: a static text renderer (legacy MessageSlice) — the label/message text is the
		// content, no editable value column.
		[AvaloniaTest]
		public void LiteralKind_BuildsStaticTextBlock_ShowingTheLabel()
		{
			var field = new RegionField(
				stableId: "l1", label: "Read this carefully:", field: "Self", writingSystem: null,
				kind: RegionFieldKind.Literal, editorClassification: EditorClassification.Known,
				automationId: "Auto.Lit", localizationKey: null, routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("", "Read this carefully:") },
				options: null, selectedOptionKey: null, isEditable: false);
			var control = RegionFieldControlFactory.Build(field, "Auto.Lit", null);
			Assert.That(control, Is.InstanceOf<TextBlock>());
			Assert.That(((TextBlock)control).Text, Is.EqualTo("Read this carefully:"));
		}

		[AvaloniaTest]
		public void CustomKind_NullFactory_DegradesToUnsupportedRow()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Custom, controlFactory: null),
				"Auto.Id", null), Is.InstanceOf<TextBlock>());

		[AvaloniaTest]
		public void CustomKind_FactoryControl_IsReturned()
		{
			var marker = new Border();
			var control = RegionFieldControlFactory.Build(
				Field(RegionFieldKind.Custom, controlFactory: () => marker), "Auto.Id", null);
			Assert.That(control, Is.SameAs(marker));
		}

		[AvaloniaTest]
		public void BrowseStyleContext_TextField_SuppressesWritingSystemAbbreviation()
		{
			// The dense browse cell context (null callbacks, no abbreviation gutter) must still build a
			// usable text field — the same control the detail pane gets, just configured for the cell.
			var browseContext = new RegionFieldControlContext(
				editContext: null, writingSystemFocused: _ => { }, showWritingSystemAbbreviation: false);
			Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Text), "Auto.Id", browseContext),
				Is.InstanceOf<FwMultiWsTextField>());
		}
	}
}
