// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// What a structured-text editor tells its host about a paragraph's selection: a
	/// right-click raises the row's text menu with that paragraph's text and selection, and
	/// the focused paragraph reports its selection as it changes.
	/// </summary>
	[TestFixture]
	public class StructuredTextMenuRequestTests
	{
		private static DetailField Field(params string[] paragraphs)
			=> new DetailField(
				stableId: "RnGenericRec/Description@1", label: "Description", field: "Description",
				writingSystem: null, kind: DetailFieldKind.StructuredText,
				editorClassification: EditorClassification.Known, automationId: "Description",
				localizationKey: null, routing: HostRouting.Product, values: null, options: null,
				selectedOptionKey: null, isEditable: true,
				paragraphs: paragraphs.Select(text => new DetailParagraph(DetailRichTextEditAlgorithms.FromRuns(text,
					new[] { new DetailTextRun(text, "fr") }))).ToList());

		private static (Window Window, IReadOnlyList<TextBox> Boxes) Show(DetailField field,
			Action<DetailMenuRequest> menuRequested, Action<DetailTextSelectionSnapshot> selectionChanged)
		{
			var control = new FwStructuredTextField(field, field.AutomationId, null, menuRequested: menuRequested,
				textSelectionChanged: selectionChanged);
			var window = new Window { Content = control, Width = 420, Height = 220 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (window, control.GetVisualDescendants().OfType<TextBox>().ToList());
		}

		[AvaloniaTest]
		public void ARightClick_RaisesTheRowsTextMenu_WithTheParagraphAndItsSelection()
		{
			var requests = new List<DetailMenuRequest>();
			var (window, boxes) = Show(Field("first one", "nous allons"), requests.Add, null);
			var second = boxes[1];
			second.Focus();
			second.SelectionStart = 0;
			second.SelectionEnd = 4;

			var point = second.TranslatePoint(new Point(2, 2), window);
			Assert.That(point, Is.Not.Null, "the paragraph must be laid out");
			window.MouseDown(point.Value, MouseButton.Right);
			window.MouseUp(point.Value, MouseButton.Right);
			Dispatcher.UIThread.RunJobs();

			Assert.That(requests, Has.Count.EqualTo(1), "one menu, the row's, not the theme's flyout");
			var request = requests[0];
			Assert.That(request.Kind, Is.EqualTo(DetailMenuKind.ContextMenu));
			Assert.That(request.TextSnapshot, Is.Not.Null);
			Assert.That(request.TextSnapshot.ParagraphIndex, Is.EqualTo(1));
			Assert.That(request.TextSnapshot.PlainText, Is.EqualTo("nous allons"));
			Assert.That(request.TextSnapshot.SelectedText, Is.EqualTo("nous"),
				"a right press inside the selection keeps it");
			Assert.That(second.ContextFlyout, Is.Null);
			window.Close();
		}

		[AvaloniaTest]
		public void TheFocusedParagraph_ReportsItsSelection_AsItChanges()
		{
			var reports = new List<DetailTextSelectionSnapshot>();
			var (window, boxes) = Show(Field("first one", "nous allons"), null, reports.Add);
			var second = boxes[1];

			second.SelectionStart = 5;
			Assert.That(reports, Is.Empty, "an unfocused paragraph reports nothing");

			second.Focus();
			second.SelectionStart = 5;
			second.SelectionEnd = 11;

			var last = reports.Last();
			Assert.That(last.ParagraphIndex, Is.EqualTo(1));
			Assert.That(last.SelectedText, Is.EqualTo("allons"));
			Assert.That(last.CanEdit, Is.False, "a row without an edit context accepts no edits");
			window.Close();
		}
	}
}
