// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Preview
{
	/// <summary>
	/// Preview-host window for the shared lexical-edit detail renderer. The host sets the
	/// <see cref="Window.DataContext"/> from <see cref="DetailPreviewDataProvider"/>; this
	/// window responds by creating a fresh <see cref="DataTree"/> for that scenario.
	/// </summary>
	public sealed class DetailPreviewWindow : Window
	{
		public DetailPreviewWindow()
		{
			Width = 900;
			Height = 520;
			AutomationProperties.SetAutomationId(this, "RegionPreviewWindow");
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			base.OnDataContextChanged(e);
			if (DataContext is DetailPreviewScenario scenario)
				Content = new DataTree(scenario.Model, scenario.EditContext);
		}
	}

	/// <summary>
	/// Preview/sample data provider for the shared lexical-edit preview window. Keeps the preview
	/// host detached from LCModel by returning a detail-model scenario plus a lightweight in-memory
	/// edit context.
	/// </summary>
	public sealed class DetailPreviewDataProvider : IFwPreviewDataProvider
	{
		public object CreateDataContext(string dataMode)
			=> CreateScenario(string.Equals(dataMode, "sample", StringComparison.OrdinalIgnoreCase));

		internal static DetailPreviewScenario CreateScenario(bool sample)
		{
			var formValues = new List<DetailWsValue>
			{
				new DetailWsValue("seh", sample ? "kumila" : string.Empty, "Charis SIL", wsTag: "seh"),
				new DetailWsValue("en", sample ? "travel" : string.Empty, "Times New Roman", wsTag: "en")
			};

			var glossValues = new List<DetailWsValue>
			{
				new DetailWsValue("en", sample ? "go on a trip" : string.Empty, "Times New Roman", wsTag: "en"),
				new DetailWsValue("pt", sample ? "viajar" : string.Empty, "Times New Roman", wsTag: "pt")
			};

			var options = new List<DetailChoiceOption>
			{
				new DetailChoiceOption("stem", "stem"),
				new DetailChoiceOption("root", "root"),
				new DetailChoiceOption("prefix", "prefix"),
				new DetailChoiceOption("suffix", "suffix")
			};

			var fields = new List<DetailField>
			{
				new DetailField(
					"LexEntry/preview/#0",
					FwAvaloniaStrings.LexemeFormLabel,
					"Form",
					"all vernacular",
					DetailFieldKind.Text,
					EditorClassification.Known,
					"LexemeFormEditor",
					null,
					SurfaceRouting.Preview,
					formValues,
					null,
					null),
				new DetailField(
					"LexEntry/preview/#1",
					FwAvaloniaStrings.MorphTypeLabel,
					"MorphType",
					null,
					DetailFieldKind.Chooser,
					EditorClassification.Known,
					"MorphTypeChooser",
					null,
					SurfaceRouting.Preview,
					null,
					options,
					"stem"),
				new DetailField(
					"LexEntry/preview/#2",
					FwAvaloniaStrings.GlossLabel,
					"Gloss",
					"all analysis",
					DetailFieldKind.Text,
					EditorClassification.Known,
					"SenseGlossEditor",
					null,
					SurfaceRouting.Preview,
					glossValues,
					null,
					null)
			};

			return new DetailPreviewScenario(
				new DetailModel("LexEntry", "preview", fields, Array.Empty<ViewDiagnostic>()),
				new PreviewDetailEditContext());
		}
	}

	/// <summary>
	/// Preview data at the detail-model boundary: the shared renderer plus a lightweight edit seam,
	/// not a separate DTO/slice/editor stack.
	/// </summary>
	public sealed class DetailPreviewScenario
	{
		public DetailPreviewScenario(DetailModel model, IDetailEditContext editContext)
		{
			Model = model;
			EditContext = editContext;
		}

		public DetailModel Model { get; }
		public IDetailEditContext EditContext { get; }
	}

	internal sealed class PreviewDetailEditContext : IDetailEditContext, IStructuredTextEditing
	{
		public bool IsOpen { get; private set; }

		public bool TrySetText(DetailField field, string ws, string value)
		{
			IsOpen = true;
			return true;
		}

		public bool TrySetRichText(DetailField field, string ws, DetailRichTextValue value)
		{
			IsOpen = true;
			return true;
		}

		public bool TrySetOption(DetailField field, string optionKey)
		{
			IsOpen = true;
			return true;
		}

		public bool TryAddReferenceItem(DetailField field, string optionKey)
		{
			IsOpen = true;
			return true;
		}

		public bool TryRemoveReferenceItem(DetailField field, string optionKey)
		{
			IsOpen = true;
			return true;
		}

		// The preview context accepts every gesture so the preview shows editable StText affordances.
		public bool TrySetParagraphText(DetailField field, int paragraphIndex, DetailRichTextValue value)
		{
			IsOpen = true;
			return true;
		}

		public bool TrySetParagraphStyle(DetailField field, int paragraphIndex, string styleName)
		{
			IsOpen = true;
			return true;
		}

		public bool TryInsertParagraph(DetailField field, int afterParagraphIndex)
		{
			IsOpen = true;
			return true;
		}

		public bool TryDeleteParagraph(DetailField field, int paragraphIndex)
		{
			IsOpen = true;
			return true;
		}

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
