// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;
using static SIL.FieldWorks.XWorks.DetailMenuTestSupport;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Notebook Edit's menus in the Avalonia detail view, answered natively through the product
	/// <see cref="RecordEditView"/>: every row menu of a record with subrecords and text, the
	/// subrecord commands, the pane bar's Demote Record, and the text commands of a
	/// structured-text row on the main menu and the row's own menu.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class NotebookMenuTests : XWorksAppTestBase
	{
		private const string SubrecordMenuId = "mnuDataTree-SubRecordSummary";

		private PropertyTable m_propertyTable;
		private RecordEditView m_view;
		private IRnResearchNbk m_notebook;
		private HashSet<IRnGenericRec> m_recordsBefore;
		private IRnGenericRec m_top;
		private IRnGenericRec m_other;
		private IRnGenericRec m_sub1;
		private IRnGenericRec m_sub2;
		private IRnGenericRec m_sub3;
		private IStTxtPara m_paragraph;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			Cache.ProjectId.Path = Path.Combine(Path.GetTempPath(), Cache.ProjectId.Name,
				Cache.ProjectId.Name + ".junk");
		}

		[SetUp]
		public void SetUpWindow()
		{
			ClipboardUtils.Manager.SetClipboardAdapter(new ClipboardStub());
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			TestLocalizationManagerBootstrap.EnsureInitialized();
			TestLocalizationManagerBootstrap.EnsureHelpTopicProvider(m_propertyTable);
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateRecords);
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView(m_propertyTable, "notebookEdit");
			DrainMediatorAndIdleQueues();
			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the notebook edit RecordEditView to load");
			m_view.Clerk.JumpToRecord(m_top.Hvo);
			DrainMediatorAndIdleQueues();
		}

		[TearDown]
		public void TearDownWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var record in m_notebook.RecordsOC.Where(r => !m_recordsBefore.Contains(r)).ToList())
					record.Delete();
			});
			m_view = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		[Test]
		public void EveryRowMenu_OfARecordWithSubrecordsAndText_IsAnsweredNatively()
		{
			var rows = ShownRows();
			Assert.That(rows.Any(r => r.MenuId == SubrecordMenuId), Is.True, "precondition: a subrecord row");
			Assert.That(TextRow().ContextMenuId, Is.EqualTo(StructuredTextMenuAuthority.MenuId),
				"a structured-text row has the text menu");

			var unanswered = new List<string>();
			foreach (var row in rows)
			{
				foreach (var menu in MenusOf(row))
				{
					var authority = m_view.CreateMenuAuthority(DetailMenuRequest.FromAnchor(null, row, menu.Kind));
					if (!XCoreMenuBridge.OwnsAll(authority, menu.Ids))
						unanswered.Add(row.Field + " " + menu.Kind + ": " + string.Join(", ", menu.Ids));
				}
			}
			Assert.That(unanswered, Is.Empty, "no Notebook menu needs the hidden tree");
		}

		[Test]
		public void ASubrecordsMenu_OffersTheRecordListsInserts_AsTheRecordListRules()
		{
			var items = MenuItems(SubrecordRow(m_sub2));

			Assert.That(Item(items, "Insert Subrecord").IsEnabled, Is.True);
			Assert.That(Item(items, "Insert Subrecord of Subrecord").IsEnabled, Is.False,
				"the shown record is a top-level record");
		}

		// Not run: the record list's insert asks with the modal Insert Record dialog.
		[Test]
		public void TheSubrecordsSectionLinks_OfferInsertSubrecord_AsTheRecordListRules()
		{
			var section = ShownRows().First(f => f.HotlinksId == "mnuDataTree-SubRecords-Hotlinks");
			var subrecord = ShownRows().First(f => f.HotlinksId == "mnuDataTree-Subrecord-Hotlinks");

			foreach (var row in new[] { section, subrecord })
			{
				var items = m_view.HotlinkItemsFor(row);

				Assert.That(items.Select(i => i.Label), Is.EqualTo(new[] { "Insert Subrecord" }), row.Field);
				Assert.That(items[0].IsEnabled && items[0].Execute != null, Is.True, row.Field);
			}
		}

		[Test]
		public void MoveDown_MovesTheSubrecord_InOneUndoableStep_AndMoveUpIsDisabledOnTheFirst()
		{
			Assert.That(Item(MenuItems(SubrecordRow(m_sub1)), "Move Up").IsEnabled, Is.False);

			Invoke(SubrecordRow(m_sub1), "Move Down");

			Assert.That(m_top.SubRecordsOS.ToList(), Is.EqualTo(new[] { m_sub2, m_sub1, m_sub3 }));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_top.SubRecordsOS.ToList(), Is.EqualTo(new[] { m_sub1, m_sub2, m_sub3 }), "one Undo");
		}

		[Test]
		public void Promote_MakesTheSubrecordTopLevel_AndTheRecordListShowsIt()
		{
			Invoke(SubrecordRow(m_sub2), "Promote");

			Assert.That(m_sub2.Owner, Is.EqualTo(m_notebook));
			Assert.That(m_top.SubRecordsOS.ToList(), Is.EqualTo(new[] { m_sub1, m_sub3 }));
			Assert.That(m_view.Clerk.CurrentObject, Is.EqualTo(m_sub2), "the record list jumps to it");
		}

		[Test]
		public void Demote_PutsTheSubrecordUnderTheSiblingTheUserPicks()
		{
			IReadOnlyList<IRnGenericRec> offered = null;
			m_view.DemotedRecordOwnerChosen = (candidates, subrecord) =>
			{
				Assert.That(subrecord, Is.True);
				offered = candidates;
				return m_sub3;
			};

			Invoke(SubrecordRow(m_sub2), "Demote...");

			Assert.That(offered, Is.EqualTo(new[] { m_sub1, m_sub3 }));
			Assert.That(m_sub3.SubRecordsOS.ToList(), Is.EqualTo(new[] { m_sub2 }));
		}

		[Test]
		public void TheCommandAuthority_InNotebookEdit_AnswersTheTextCommandsAndDemoteRecord()
		{
			Assert.That(m_view.CreateCommandAuthority().AnsweredMessages, Is.EquivalentTo(new[]
			{
				RecordEditView.LexiconLookupMessage, RecordEditView.AddToLexiconMessage, RecordEditView.DemoteRecordMessage
			}));
		}

		[Test]
		public void DemoteRecord_PutsTheShownRecordUnderTheRecordTheUserPicks()
		{
			m_view.DemotedRecordOwnerChosen = (candidates, subrecord) =>
			{
				Assert.That(subrecord, Is.False);
				Assert.That(candidates, Does.Contain(m_other).And.Not.Contain(m_top));
				return m_other;
			};
			var authority = CommandAuthorities.Of(m_propertyTable);
			var display = Display();
			Assert.That(authority.UpdateDisplay(CommandOf("CmdDemoteRecord"), display), Is.True);
			Assert.That(display.Enabled, Is.True);

			Assert.That(authority.Execute(CommandOf("CmdDemoteRecord")), Is.True);
			DrainMediatorAndIdleQueues();

			Assert.That(m_other.SubRecordsOS.ToList(), Is.EqualTo(new[] { m_top }));
		}

		[Test]
		public void FindInDictionary_OnTheMainMenu_IsHiddenWithoutATextSelection_AndLooksUpTheWordAtIt()
		{
			var authority = m_view.CreateCommandAuthority();
			m_view.CurrentTextSelectionSource = () => null;
			var display = Display();
			Assert.That(authority.UpdateDisplay(CommandOf("CmdLexiconLookup"), display), Is.True);
			Assert.That(display.Visible, Is.False);

			m_view.CurrentTextSelectionSource = () => Selection(7, 7);
			var looked = new List<Tuple<int, int, int, int>>();
			m_view.WordLookupLaunch = (paragraph, ws, ichMin, ichLim)
				=> looked.Add(Tuple.Create(paragraph.Hvo, ws, ichMin, ichLim));
			Assert.That(authority.Execute(CommandOf("CmdLexiconLookup")), Is.True);

			Assert.That(looked, Is.EqualTo(new[] { Tuple.Create(m_paragraph.Hvo, Cache.DefaultVernWs, 5, 11) }),
				"the word around the caret, in the saved paragraph");
		}

		[Test]
		public void AddToDictionary_OffersTheWordAtTheSelectionStart_OnlyInTheVernacular()
		{
			var added = new List<string>();
			m_view.AddWordLaunch = form => added.Add(form.Text);
			var leaf = TextMenuLeaf(RecordEditView.AddToLexiconMessage);

			var item = m_view.StructuredTextItem(leaf, Selection(9, 1));
			Assert.That(item.IsEnabled, Is.True);
			item.Execute();
			Assert.That(added, Is.EqualTo(new[] { "nous" }), "the selection starts in the first word");

			var analysis = new DetailTextSelectionSnapshot(TextRow(), 0,
				DetailRichTextEditAlgorithms.FromRuns("hello", new[] { new DetailTextRun("hello", "en") }), 1, 1, true);
			Assert.That(m_view.StructuredTextItem(leaf, analysis).IsEnabled, Is.False);
			Assert.That(m_view.StructuredTextItem(TextMenuLeaf(RecordEditView.LexiconLookupMessage), analysis).IsEnabled,
				Is.True, "Find in Dictionary takes any word");
		}

		[Test]
		public void Cut_PutsTheSelectedTextOnTheClipboard_AndRemovesIt_InOneUndoableStep()
		{
			var item = m_view.StructuredTextItem(TextMenuLeaf(RecordEditView.EditCutMessage), Selection(0, 5));
			Assert.That(item.IsEnabled, Is.True);

			item.Execute();
			DrainMediatorAndIdleQueues();

			Assert.That(m_paragraph.Contents.Text, Is.EqualTo("allons"));
			Assert.That(m_view.TextClipboard.GetText().PlainText, Is.EqualTo("nous "));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_paragraph.Contents.Text, Is.EqualTo("nous allons"), "one Undo");
		}

		[Test]
		public void Paste_PutsTheClipboardsTextInPlaceOfTheSelection_UpToALineBreak()
		{
			m_view.TextClipboard.SetText(new FwClipboardText("vite\r\nmore"));
			var item = m_view.StructuredTextItem(TextMenuLeaf(RecordEditView.EditPasteMessage), Selection(5, 11));
			Assert.That(item.IsEnabled, Is.True);

			item.Execute();
			DrainMediatorAndIdleQueues();

			Assert.That(m_paragraph.Contents.Text, Is.EqualTo("nous vite"));
			Assert.That(m_paragraph.Contents.get_WritingSystemAt(6), Is.EqualTo(Cache.DefaultVernWs),
				"plain text takes the writing system of the text before it");
		}

		[Test]
		public void Copy_PutsTheSelectedTextOnTheClipboard_AndLeavesTheParagraph()
		{
			m_view.StructuredTextItem(TextMenuLeaf(RecordEditView.EditCopyMessage), Selection(5, 11)).Execute();

			Assert.That(m_view.TextClipboard.GetText().PlainText, Is.EqualTo("allons"));
			Assert.That(m_paragraph.Contents.Text, Is.EqualTo("nous allons"));
		}

		[Test]
		public void TheEditItems_AreDisabled_WhereTheyCannotAct()
		{
			Assert.That(m_view.StructuredTextItem(TextMenuLeaf(RecordEditView.EditCopyMessage), Selection(3, 3)).IsEnabled,
				Is.False, "Copy needs a selection");
			Assert.That(m_view.StructuredTextItem(TextMenuLeaf(RecordEditView.EditCutMessage), Selection(3, 3)).IsEnabled,
				Is.False, "Cut needs a selection");
			m_view.TextClipboard.SetText(new FwClipboardText("x"));
			Assert.That(m_view.StructuredTextItem(TextMenuLeaf(RecordEditView.EditPasteMessage),
				Selection(3, 3, canEdit: false)).IsEnabled, Is.False, "Paste needs a paragraph that takes edits");
			Assert.That(m_view.StructuredTextItem(TextMenuLeaf(RecordEditView.EditCopyMessage), null).IsEnabled,
				Is.False, "no editor, no text command");
		}

		[Test]
		public void ATextCommand_DoesNothing_WhenTheSavedParagraphNoLongerHoldsTheText()
		{
			var selection = Selection(0, 4);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_paragraph.Contents = TsStringUtils.MakeString("changed", Cache.DefaultVernWs));

			Assert.That(m_view.ReplaceSelectedText(selection, cut: true), Is.False);
			Assert.That(m_view.RunTextWordCommand(selection, addToLexicon: false), Is.False);
			Assert.That(m_paragraph.Contents.Text, Is.EqualTo("changed"));
		}

		// Text is saved decomposed, while the editor holds what was typed, often precomposed.
		[Test]
		public void TheTextCommands_FindTheSelectedText_InADecomposedSavedParagraph()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_paragraph.Contents = TsStringUtils.MakeString("été allons", Cache.DefaultVernWs));
			var vernacular = Cache.WritingSystemFactory.GetStrFromWs(Cache.DefaultVernWs);
			DetailTextSelectionSnapshot Typed(int anchor, int end) => new DetailTextSelectionSnapshot(TextRow(), 0,
				DetailRichTextEditAlgorithms.FromRuns("été allons",
					new[] { new DetailTextRun("été allons", vernacular) }), anchor, end, true);
			var looked = new List<Tuple<int, int>>();
			m_view.WordLookupLaunch = (paragraph, ws, ichMin, ichLim) => looked.Add(Tuple.Create(ichMin, ichLim));

			Assert.That(m_view.RunTextWordCommand(Typed(6, 6), addToLexicon: false), Is.True);
			Assert.That(m_view.ReplaceSelectedText(Typed(4, 10), cut: true), Is.True);

			Assert.That(looked, Is.EqualTo(new[] { Tuple.Create(6, 12) }), "'allons' in the saved text");
			Assert.That(m_paragraph.Contents.Text, Is.EqualTo("été "));
			Assert.That(m_view.TextClipboard.GetText().PlainText, Is.EqualTo("allons"));
		}

		[Test]
		public void AddToDictionary_OnTheMainMenu_FollowsEachNewSelection()
		{
			var authority = m_view.CreateCommandAuthority();
			var vernacular = Selection(1, 1);
			var analysis = new DetailTextSelectionSnapshot(TextRow(), 0,
				DetailRichTextEditAlgorithms.FromRuns("hello", new[] { new DetailTextRun("hello", "en") }), 1, 1, true);
			bool Enabled(DetailTextSelectionSnapshot selection)
			{
				m_view.CurrentTextSelectionSource = () => selection;
				var display = Display();
				authority.UpdateDisplay(CommandOf("CmdAddToLexicon"), display);
				return display.Enabled;
			}

			Assert.That(Enabled(vernacular), Is.True);
			Assert.That(Enabled(vernacular), Is.True, "asked again about the same selection");
			Assert.That(Enabled(analysis), Is.False, "a new selection is judged on its own word");
			Assert.That(Enabled(vernacular), Is.True);
		}

		// Two top-level records; the first has three subrecords and a Description paragraph.
		private void CreateRecords()
		{
			m_notebook = Cache.LangProject.ResearchNotebookOA;
			if (m_notebook == null)
			{
				Cache.LangProject.ResearchNotebookOA = m_notebook
					= Cache.ServiceLocator.GetInstance<IRnResearchNbkFactory>().Create();
			}
			m_recordsBefore = new HashSet<IRnGenericRec>(m_notebook.RecordsOC);
			var records = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>();
			m_notebook.RecordsOC.Add(m_top = records.Create());
			m_top.Title = TsStringUtils.MakeString("First", Cache.DefaultAnalWs);
			m_notebook.RecordsOC.Add(m_other = records.Create());
			m_other.Title = TsStringUtils.MakeString("Second", Cache.DefaultAnalWs);
			m_top.SubRecordsOS.Add(m_sub1 = records.Create());
			m_top.SubRecordsOS.Add(m_sub2 = records.Create());
			m_top.SubRecordsOS.Add(m_sub3 = records.Create());
			m_top.DescriptionOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			m_paragraph = m_top.DescriptionOA.AddNewTextPara(null);
			m_paragraph.Contents = TsStringUtils.MakeString("nous allons", Cache.DefaultVernWs);
		}

		// The Description row's first paragraph with a selection, as its editor reports it.
		private DetailTextSelectionSnapshot Selection(int anchor, int end, bool canEdit = true)
			=> new DetailTextSelectionSnapshot(TextRow(), 0, TextRow().Paragraphs[0].Text, anchor, end, canEdit);

		private DetailField TextRow()
			=> ShownRows().Single(f => f.Kind == DetailFieldKind.StructuredText && f.ObjectHvo == m_top.Hvo
				&& f.Field == "Description");

		private DetailField SubrecordRow(IRnGenericRec subrecord)
			=> ShownRows().First(f => f.ObjectHvo == subrecord.Hvo && f.MenuId == SubrecordMenuId);

		private IReadOnlyList<DetailMenuItem> MenuItems(DetailField row)
			=> DetailMenuTestSupport.MenuItems(m_view, m_propertyTable, row);

		private void Invoke(DetailField row, string label)
		{
			Run(MenuItems(row), label);
			DrainMediatorAndIdleQueues();
		}

		private DetailMenuLeaf TextMenuLeaf(string message)
			=> XCoreMenuBridge.ResolveMenu(m_propertyTable.GetValue<XWindow>("window"), StructuredTextMenuAuthority.MenuId)
				.Leaves.Single(l => l.Message == message);

		private Command CommandOf(string id) => DetailMenuTestSupport.CommandOf(m_propertyTable, id);

		private List<DetailField> ShownRows() => DetailMenuTestSupport.ShownRows(m_view);
	}
}
