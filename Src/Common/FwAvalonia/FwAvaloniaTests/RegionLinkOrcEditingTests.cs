// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests.VisualChecks;
using FwAvaloniaDialogsTests;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Kind-aware ORC editing on the owned multi-WS text editor: external-link
	/// insert / edit / delete (the dialog-light URL prompt flyout), generic ORC delete (any kind), and
	/// the picture/footnote DEFER contract (rendered + deletable, NOT insert/caption here). An ORC is
	/// not a blanket read-only block. LCModel-free: a recording fake context.
	/// </summary>
	[TestFixture]
	public class RegionLinkOrcEditingTests
	{
		private const char ExternalLink = (char)4;
		private const char Picture = (char)8;
		private const char FootnoteOwn = (char)5;

		private static DetailField FieldWith(DetailRichTextValue rich, bool isEditable = true)
			=> new DetailField("LexEntry/Bib@1", "Bibliography", "Bibliography", null,
				DetailFieldKind.Text, EditorClassification.Known, "BibEditor", null, SurfaceRouting.Product,
				new List<DetailWsValue> { new DetailWsValue("anal", rich.PlainText, wsTag: "en", richText: rich) },
				null, null, isEditable: isEditable);

		private static (FwMultiWsTextField Control, FakeDetailEditContext Context, Window Window)
			Show(DetailField field)
		{
			var context = new FakeDetailEditContext();
			var control = new FwMultiWsTextField(field, "BibEditor", context, null);
			var window = new Window { Content = control, Width = 480, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			return (control, context, window);
		}

		private static T Find<T>(Control root, string id) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == id);

		// The link / delete-embedded-object operations live on the value box's right-click
		// menu, so they are MenuItems in the box's ContextFlyout, not visual-tree children. The link item
		// carries the prompt flyout it opens in its Tag.
		private static MenuItem FindMenuItem(TextBox box, string id)
			=> (box.ContextFlyout as MenuFlyout)?.Items.OfType<MenuItem>()
				.FirstOrDefault(mi => AutomationProperties.GetAutomationId(mi) == id);

		// The link flyout content lives in a popup (not a visual descendant of the control), so reach the
		// URL box / Apply button through the flyout content's LOGICAL tree (the StackPanel's children),
		// which is populated independent of popup realization.
		private static T FindInFlyout<T>(Flyout flyout, string id) where T : Control
		{
			var content = (StackPanel)flyout.Content;
			return content.Children.OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == id);
		}

		// ---- insert a hyperlink over a selection ----

		[AvaloniaTest]
		public void LinkAffordance_InsertsAHyperlinkOverTheSelection()
		{
			var rich = DetailRichTextEditAlgorithms.FromRuns("see SIL here",
				new[] { new DetailTextRun("see SIL here", "en") });
			var (control, context, window) = Show(FieldWith(rich));

			var box = Find<TextBox>(control, "BibEditor.en");
			var linkItem = FindMenuItem(box, "BibEditor.en.Link");
			Assert.That(linkItem, Is.Not.Null, "an editable row offers the link operation on its right-click menu");

			box.SelectionStart = 4; // select "SIL"
			box.SelectionEnd = 7;
			Dispatcher.UIThread.RunJobs();

			var flyout = (Flyout)linkItem.Tag;
			// Choosing the menu item runs the open handler (selection snapshot + URL pre-fill) and opens the
			// prompt flyout anchored at the box; raise it the same way a user click would.
			linkItem.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, "Region-LinkOrc-01-link-prompt");

			var url = FindInFlyout<TextBox>(flyout, "BibEditor.en.Link.Url");
			Assert.That(url, Is.Not.Null, "the link flyout prompts for a URL");
			url.Text = "https://software.sil.org/fieldworks";
			var ok = FindInFlyout<Button>(flyout, "BibEditor.en.Link.Apply");
			ok.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1), "the link stages through the rich seam");
			var staged = context.RichTextEdits[0].Value;
			var linkRun = staged.Runs.Single(r => r.OrcKind == DetailOrcKind.ExternalLink);
			Assert.That(linkRun.Text, Is.EqualTo("SIL"));
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://software.sil.org/fieldworks"));
		}

		[AvaloniaTest]
		public void LinkAffordance_PrefillsAndEditsAnExistingLinksUrl()
		{
			var rich = DetailRichTextEditAlgorithms.FromRuns("a SIL b",
				new[]
				{
					new DetailTextRun("a ", "en"),
					new DetailTextRun("SIL", "en", objectData: ExternalLink + "https://old.example"),
					new DetailTextRun(" b", "en")
				});
			var (control, context, _) = Show(FieldWith(rich));

			var box = Find<TextBox>(control, "BibEditor.en");
			box.SelectionStart = 2; // inside the existing link "SIL"
			box.SelectionEnd = 5;
			Dispatcher.UIThread.RunJobs();

			var linkItem = FindMenuItem(box, "BibEditor.en.Link");
			var flyout = (Flyout)linkItem.Tag;
			linkItem.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			var url = FindInFlyout<TextBox>(flyout, "BibEditor.en.Link.Url");
			Assert.That(url.Text, Is.EqualTo("https://old.example"),
				"the prompt pre-fills the existing link's URL for editing");
			url.Text = "https://new.example";
			FindInFlyout<Button>(flyout, "BibEditor.en.Link.Apply")
				.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1));
			var linkRun = context.RichTextEdits[0].Value.Runs.Single(r => r.OrcKind == DetailOrcKind.ExternalLink);
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://new.example"));
		}

		[AvaloniaTest]
		public void LinkFlyout_WithBlankUrl_StagesNothing()
		{
			var rich = DetailRichTextEditAlgorithms.FromRuns("text",
				new[] { new DetailTextRun("text", "en") });
			var (control, context, _) = Show(FieldWith(rich));
			var box = Find<TextBox>(control, "BibEditor.en");
			box.SelectionStart = 0;
			box.SelectionEnd = 4;
			Dispatcher.UIThread.RunJobs();

			var linkItem = FindMenuItem(box, "BibEditor.en.Link");
			var flyout = (Flyout)linkItem.Tag;
			linkItem.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			FindInFlyout<TextBox>(flyout, "BibEditor.en.Link.Url").Text = "";
			FindInFlyout<Button>(flyout, "BibEditor.en.Link.Apply")
				.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Is.Empty, "a blank URL inserts no link");
		}

		// ---- delete an ORC (any kind) ----

		[AvaloniaTest]
		public void OrcDelete_RemovesAPictureOrc_OverTheSelection()
		{
			var rich = DetailRichTextEditAlgorithms.FromRuns("a￼b",
				new[]
				{
					new DetailTextRun("a", "en"),
					new DetailTextRun("￼", "en", objectData: Picture.ToString()),
					new DetailTextRun("b", "en")
				});
			var (control, context, window) = Show(FieldWith(rich));

			var box = Find<TextBox>(control, "BibEditor.en");
			box.SelectionStart = 1; // over the picture ORC
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();

			// The delete-embedded-object operation is offered on the row's right-click menu.
			var deleteOrc = FindMenuItem(box, "BibEditor.en.OrcDelete");
			Assert.That(deleteOrc, Is.Not.Null, "a row offers the delete-embedded-object operation on its right-click menu");
			deleteOrc.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, "Region-LinkOrc-02-orc-selected-for-delete");

			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1));
			var staged = context.RichTextEdits[0].Value;
			Assert.That(staged.PlainText, Is.EqualTo("ab"), "the picture ORC was removed");
			Assert.That(staged.Runs.Any(r => r.IsOrc), Is.False);
		}

		[AvaloniaTest]
		public void OrcDelete_AlsoRemovesAFootnoteOrc_DeferredButDeletable()
		{
			var rich = DetailRichTextEditAlgorithms.FromRuns("x￼y",
				new[]
				{
					new DetailTextRun("x", "en"),
					new DetailTextRun("￼", "en", objectData: FootnoteOwn.ToString()),
					new DetailTextRun("y", "en")
				});
			var (control, context, _) = Show(FieldWith(rich));
			var box = Find<TextBox>(control, "BibEditor.en");
			box.SelectionStart = 1;
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();

			FindMenuItem(box, "BibEditor.en.OrcDelete")
				.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits.Single().Value.PlainText, Is.EqualTo("xy"),
				"a footnote ORC is render+deletable here (full editing deferred)");
		}

		[AvaloniaTest]
		public void LinkRow_IsEditable_NotABlanketReadOnlyBlock()
		{
			var rich = DetailRichTextEditAlgorithms.FromRuns("SIL",
				new[] { new DetailTextRun("SIL", "en", objectData: ExternalLink + "https://software.sil.org") });
			var (control, _, _) = Show(FieldWith(rich));
			var box = Find<TextBox>(control, "BibEditor.en");
			Assert.That(box.IsReadOnly, Is.False, "a link ORC value is editable (§19c)");
		}
	}
}
