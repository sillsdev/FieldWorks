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
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests.VisualChecks;
using FwAvaloniaDialogsTests;

namespace FwAvaloniaTests
{
	/// <summary>
	/// §19c (T1/T3/T5) — kind-aware ORC editing on the owned multi-WS text editor: external-link
	/// insert / edit / delete (the dialog-light URL prompt flyout), generic ORC delete (any kind), and
	/// the picture/footnote DEFER contract (rendered + deletable, NOT insert/caption here). An ORC is no
	/// longer a blanket read-only block. LCModel-free: a recording fake context.
	/// </summary>
	[TestFixture]
	public class RegionLinkOrcEditingTests
	{
		private const char ExternalLink = (char)4;
		private const char Picture = (char)8;
		private const char FootnoteOwn = (char)5;

		private static LexicalEditRegionField FieldWith(RegionRichTextValue rich, bool isEditable = true)
			=> new LexicalEditRegionField("LexEntry/Bib@1", "Bibliography", "Bibliography", null,
				RegionFieldKind.Text, EditorClassification.Known, "BibEditor", null, SurfaceRouting.Product,
				new List<RegionWsValue> { new RegionWsValue("anal", rich.PlainText, wsTag: "en", richText: rich) },
				null, null, isEditable: isEditable);

		private static (FwMultiWsTextField Control, FakeRegionEditContext Context, Window Window)
			Show(LexicalEditRegionField field)
		{
			var context = new FakeRegionEditContext();
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

		// The link flyout content lives in a popup (not a visual descendant of the control), so reach the
		// URL box / Apply button through the flyout content's LOGICAL tree (the StackPanel's children),
		// which is populated independent of popup realization.
		private static T FindInFlyout<T>(Button flyoutButton, string id) where T : Control
		{
			var content = (StackPanel)((Flyout)flyoutButton.Flyout).Content;
			return content.Children.OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == id);
		}

		// ---- insert a hyperlink over a selection ----

		[AvaloniaTest]
		public void LinkAffordance_InsertsAHyperlinkOverTheSelection()
		{
			var rich = RegionRichTextEditAlgorithms.FromRuns("see SIL here",
				new[] { new RegionTextRun("see SIL here", "en") });
			var (control, context, window) = Show(FieldWith(rich));

			var box = Find<TextBox>(control, "BibEditor.en");
			var linkButton = Find<Button>(control, "BibEditor.en.Link");
			Assert.That(linkButton, Is.Not.Null, "an editable row exposes the link affordance");

			box.SelectionStart = 4; // select "SIL"
			box.SelectionEnd = 7;
			Dispatcher.UIThread.RunJobs();

			var flyout = (Flyout)linkButton.Flyout;
			// The button's own Click opens the flyout AND runs the open handler (selection snapshot +
			// URL pre-fill); raise it the same way a user click would.
			linkButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			flyout.ShowAt(linkButton);
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, "Region-LinkOrc-01-link-prompt");

			var url = FindInFlyout<TextBox>(linkButton, "BibEditor.en.Link.Url");
			Assert.That(url, Is.Not.Null, "the link flyout prompts for a URL");
			url.Text = "https://software.sil.org/fieldworks";
			var ok = FindInFlyout<Button>(linkButton, "BibEditor.en.Link.Apply");
			ok.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1), "the link stages through the rich seam");
			var staged = context.RichTextEdits[0].Value;
			var linkRun = staged.Runs.Single(r => r.OrcKind == RegionOrcKind.ExternalLink);
			Assert.That(linkRun.Text, Is.EqualTo("SIL"));
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://software.sil.org/fieldworks"));
		}

		[AvaloniaTest]
		public void LinkAffordance_PrefillsAndEditsAnExistingLinksUrl()
		{
			var rich = RegionRichTextEditAlgorithms.FromRuns("a SIL b",
				new[]
				{
					new RegionTextRun("a ", "en"),
					new RegionTextRun("SIL", "en", objectData: ExternalLink + "https://old.example"),
					new RegionTextRun(" b", "en")
				});
			var (control, context, _) = Show(FieldWith(rich));

			var box = Find<TextBox>(control, "BibEditor.en");
			box.SelectionStart = 2; // inside the existing link "SIL"
			box.SelectionEnd = 5;
			Dispatcher.UIThread.RunJobs();

			var linkButton = Find<Button>(control, "BibEditor.en.Link");
			var flyout = (Flyout)linkButton.Flyout;
			linkButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			flyout.ShowAt(linkButton);
			Dispatcher.UIThread.RunJobs();

			var url = FindInFlyout<TextBox>(linkButton, "BibEditor.en.Link.Url");
			Assert.That(url.Text, Is.EqualTo("https://old.example"),
				"the prompt pre-fills the existing link's URL for editing");
			url.Text = "https://new.example";
			FindInFlyout<Button>(linkButton, "BibEditor.en.Link.Apply")
				.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1));
			var linkRun = context.RichTextEdits[0].Value.Runs.Single(r => r.OrcKind == RegionOrcKind.ExternalLink);
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://new.example"));
		}

		[AvaloniaTest]
		public void LinkFlyout_WithBlankUrl_StagesNothing()
		{
			var rich = RegionRichTextEditAlgorithms.FromRuns("text",
				new[] { new RegionTextRun("text", "en") });
			var (control, context, _) = Show(FieldWith(rich));
			var box = Find<TextBox>(control, "BibEditor.en");
			box.SelectionStart = 0;
			box.SelectionEnd = 4;
			Dispatcher.UIThread.RunJobs();

			var linkButton = Find<Button>(control, "BibEditor.en.Link");
			linkButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			((Flyout)linkButton.Flyout).ShowAt(linkButton);
			Dispatcher.UIThread.RunJobs();
			FindInFlyout<TextBox>(linkButton, "BibEditor.en.Link.Url").Text = "";
			FindInFlyout<Button>(linkButton, "BibEditor.en.Link.Apply")
				.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Is.Empty, "a blank URL inserts no link");
		}

		// ---- delete an ORC (any kind) ----

		[AvaloniaTest]
		public void OrcDelete_RemovesAPictureOrc_OverTheSelection()
		{
			var rich = RegionRichTextEditAlgorithms.FromRuns("a￼b",
				new[]
				{
					new RegionTextRun("a", "en"),
					new RegionTextRun("￼", "en", objectData: Picture.ToString()),
					new RegionTextRun("b", "en")
				});
			var (control, context, window) = Show(FieldWith(rich));

			var box = Find<TextBox>(control, "BibEditor.en");
			box.SelectionStart = 1; // over the picture ORC
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();

			// The delete-ORC affordance is enabled because the selection overlaps an ORC run.
			var deleteOrc = Find<Button>(control, "BibEditor.en.OrcDelete");
			Assert.That(deleteOrc, Is.Not.Null, "a selection over an ORC exposes the delete-embedded-object affordance");
			deleteOrc.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
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
			var rich = RegionRichTextEditAlgorithms.FromRuns("x￼y",
				new[]
				{
					new RegionTextRun("x", "en"),
					new RegionTextRun("￼", "en", objectData: FootnoteOwn.ToString()),
					new RegionTextRun("y", "en")
				});
			var (control, context, _) = Show(FieldWith(rich));
			var box = Find<TextBox>(control, "BibEditor.en");
			box.SelectionStart = 1;
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();

			Find<Button>(control, "BibEditor.en.OrcDelete")
				.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits.Single().Value.PlainText, Is.EqualTo("xy"),
				"a footnote ORC is render+deletable here (full editing deferred)");
		}

		[AvaloniaTest]
		public void LinkRow_IsEditable_NotABlanketReadOnlyBlock()
		{
			var rich = RegionRichTextEditAlgorithms.FromRuns("SIL",
				new[] { new RegionTextRun("SIL", "en", objectData: ExternalLink + "https://software.sil.org") });
			var (control, _, _) = Show(FieldWith(rich));
			var box = Find<TextBox>(control, "BibEditor.en");
			Assert.That(box.IsReadOnly, Is.False, "a link ORC value is editable (§19c)");
		}
	}
}
