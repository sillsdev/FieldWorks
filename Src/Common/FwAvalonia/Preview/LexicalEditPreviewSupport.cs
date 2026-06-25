// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Preview
{
	/// <summary>
	/// Preview-host window for the shared lexical-edit region renderer. The host sets the
	/// <see cref="Window.DataContext"/> from <see cref="LexicalEditPreviewDataProvider"/>; this
	/// window responds by creating a fresh <see cref="LexicalEditRegionView"/> for that scenario.
	/// </summary>
	public sealed class LexicalEditPreviewWindow : Window
	{
		public LexicalEditPreviewWindow()
		{
			Width = 900;
			Height = 520;
			AutomationProperties.SetAutomationId(this, "LexicalEditPreviewWindow");
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			base.OnDataContextChanged(e);
			if (DataContext is LexicalEditPreviewScenario scenario)
				Content = new LexicalEditRegionView(scenario.Model, scenario.EditContext);
		}
	}

	/// <summary>
	/// Preview/sample data provider for the shared lexical-edit preview window. Keeps the preview
	/// host detached from LCModel by returning a region-model scenario plus a lightweight in-memory
	/// edit context.
	/// </summary>
	public sealed class LexicalEditPreviewDataProvider : IFwPreviewDataProvider
	{
		public object CreateDataContext(string dataMode)
			=> CreateScenario(string.Equals(dataMode, "sample", StringComparison.OrdinalIgnoreCase));

		internal static LexicalEditPreviewScenario CreateScenario(bool sample)
		{
			var formValues = new List<RegionWsValue>
			{
				new RegionWsValue("seh", sample ? "kumila" : string.Empty, "Charis SIL", wsTag: "seh"),
				new RegionWsValue("en", sample ? "travel" : string.Empty, "Times New Roman", wsTag: "en")
			};

			var glossValues = new List<RegionWsValue>
			{
				new RegionWsValue("en", sample ? "go on a trip" : string.Empty, "Times New Roman", wsTag: "en"),
				new RegionWsValue("pt", sample ? "viajar" : string.Empty, "Times New Roman", wsTag: "pt")
			};

			var options = new List<RegionChoiceOption>
			{
				new RegionChoiceOption("stem", "stem"),
				new RegionChoiceOption("root", "root"),
				new RegionChoiceOption("prefix", "prefix"),
				new RegionChoiceOption("suffix", "suffix")
			};

			var fields = new List<LexicalEditRegionField>
			{
				new LexicalEditRegionField(
					"LexEntry/preview/#0",
					FwAvaloniaStrings.LexemeFormLabel,
					"Form",
					"all vernacular",
					RegionFieldKind.Text,
					EditorClassification.Known,
					"LexemeFormEditor",
					null,
					SurfaceRouting.Preview,
					formValues,
					null,
					null),
				new LexicalEditRegionField(
					"LexEntry/preview/#1",
					FwAvaloniaStrings.MorphTypeLabel,
					"MorphType",
					null,
					RegionFieldKind.Chooser,
					EditorClassification.Known,
					"MorphTypeChooser",
					null,
					SurfaceRouting.Preview,
					null,
					options,
					"stem"),
				new LexicalEditRegionField(
					"LexEntry/preview/#2",
					FwAvaloniaStrings.GlossLabel,
					"Gloss",
					"all analysis",
					RegionFieldKind.Text,
					EditorClassification.Known,
					"SenseGlossEditor",
					null,
					SurfaceRouting.Preview,
					glossValues,
					null,
					null)
			};

			return new LexicalEditPreviewScenario(
				new LexicalEditRegionModel("LexEntry", "preview", fields, Array.Empty<ViewDiagnostic>()),
				new PreviewRegionEditContext());
		}
	}

	/// <summary>
	/// Preview data at the region-model boundary: the shared renderer plus a lightweight edit seam,
	/// not a separate DTO/slice/editor stack.
	/// </summary>
	public sealed class LexicalEditPreviewScenario
	{
		public LexicalEditPreviewScenario(LexicalEditRegionModel model, IRegionEditContext editContext)
		{
			Model = model;
			EditContext = editContext;
		}

		public LexicalEditRegionModel Model { get; }
		public IRegionEditContext EditContext { get; }
	}

	internal sealed class PreviewRegionEditContext : IRegionEditContext
	{
		public bool IsOpen { get; private set; }

		public bool TrySetText(LexicalEditRegionField field, string ws, string value)
		{
			IsOpen = true;
			return true;
		}

		public bool TrySetRichText(LexicalEditRegionField field, string ws, RegionRichTextValue value)
		{
			IsOpen = true;
			return true;
		}

		public bool TrySetOption(LexicalEditRegionField field, string optionKey)
		{
			IsOpen = true;
			return true;
		}

		public bool TryAddReferenceItem(LexicalEditRegionField field, string optionKey)
		{
			IsOpen = true;
			return true;
		}

		public bool TryRemoveReferenceItem(LexicalEditRegionField field, string optionKey)
		{
			IsOpen = true;
			return true;
		}

		// §19a: the preview context accepts every gesture so the preview shows editable StText affordances.
		public bool TrySetParagraphText(LexicalEditRegionField field, int paragraphIndex, RegionRichTextValue value)
		{
			IsOpen = true;
			return true;
		}

		public bool TrySetParagraphStyle(LexicalEditRegionField field, int paragraphIndex, string styleName)
		{
			IsOpen = true;
			return true;
		}

		public bool TryInsertParagraph(LexicalEditRegionField field, int afterParagraphIndex)
		{
			IsOpen = true;
			return true;
		}

		public bool TryDeleteParagraph(LexicalEditRegionField field, int paragraphIndex)
		{
			IsOpen = true;
			return true;
		}

		// §19d: the preview support context stages pictures/audio as "accepted" so the preview renders the
		// affordances; the preview never touches real LCModel/files (it is the detached preview path).
		public bool TryInsertPicture(LexicalEditRegionField field, string sourceFile, RegionPictureMetadata metadata)
		{ IsOpen = true; return true; }
		public bool TryReplacePictureFile(LexicalEditRegionField field, string sourceFile)
		{ IsOpen = true; return true; }
		public bool TryDeletePicture(LexicalEditRegionField field)
		{ IsOpen = true; return true; }
		public bool TrySetPictureMetadata(LexicalEditRegionField field, RegionPictureMetadata metadata)
		{ IsOpen = true; return true; }
		public bool TryInsertPictureOrc(LexicalEditRegionField field, string ws, int caretPosition,
			string sourceFile, RegionPictureMetadata metadata)
		{ IsOpen = true; return true; }

		public IReadOnlyList<string> Validate()
			=> Array.Empty<string>();

		public void Commit()
		{
			IsOpen = false;
		}

		public void Cancel()
		{
			IsOpen = false;
		}
	}
}
