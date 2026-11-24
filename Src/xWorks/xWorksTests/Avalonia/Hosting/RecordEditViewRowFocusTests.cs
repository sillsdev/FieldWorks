// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;
using static SIL.FieldWorks.XWorks.DetailMenuTestSupport;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The rows the product <see cref="RecordEditView"/> gives the keyboard to when asked from
	/// outside: Jump to field in the dictionary preview, in the Avalonia view and in the
	/// WinForms one, and a record the clerk opens for editing, in the Avalonia view.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class RecordEditViewRowFocusTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private ILexSense m_sense;
		private ILexEntry m_other;
		private ILexSense m_otherSense;
		private RecordEditView m_view;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			// The WinForms tree keys its layout inventory by the project path.
			Cache.ProjectId.Path = Path.Combine(Path.GetTempPath(), Cache.ProjectId.Name,
				Cache.ProjectId.Name + ".junk");
		}

		[SetUp]
		public void SetUpWindow()
		{
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			TestLocalizationManagerBootstrap.EnsureInitialized();
			TestLocalizationManagerBootstrap.EnsureHelpTopicProvider(m_propertyTable);
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var stem = GetMorphTypeOrCreateOne("stem");
				var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				m_entry = AddLexeme(m_createdObjects, "focus-entry", stem, "first gloss", noun);
				m_sense = m_entry.SensesOS[0];
				m_other = AddLexeme(m_createdObjects, "focus-other", stem, "other gloss", noun);
				m_otherSense = m_other.SensesOS[0];
			});
		}

		[TearDown]
		public void TearDownWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var obj in Enumerable.Reverse(m_createdObjects))
				{
					if (obj.IsValidObject)
						obj.Delete();
				}
				foreach (var feature in Cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.ToList())
					feature.Delete();
			});
			m_createdObjects = null;
			m_view = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		// Loads the tool's detail view in the given UI.
		private void LoadView(string tool, string uiMode = "New")
		{
			m_propertyTable.SetProperty("UIMode", uiMode, true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView(m_propertyTable, tool);
			DrainMediatorAndIdleQueues();
			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the RecordEditView of " + tool + " to load");
		}

		private void ShowEntry(ILexEntry entry)
		{
			m_view.Clerk.JumpToRecord(entry.Hvo);
			DrainMediatorAndIdleQueues();
		}

		// Publishes the request as the dictionary preview does.
		private void PublishJumpToField(int recordHvo, ICmObject fieldObject, string fieldName, string text)
			=> FwUtils.Publisher.Publish(new PublisherParameterObject(EventConstants.JumpToField,
				new JumpToFieldRequest(recordHvo, fieldObject.Hvo, fieldName, text), m_propertyTable.GetWindow()));

		// The automation id of the shown row for that field of that object.
		private string ShownRowId(ICmObject obj, string field)
			=> ShownRows(m_view).Single(f => f.ObjectHvo == obj.Hvo && f.Field == field
				&& f.Kind != DetailFieldKind.Header).RowAutomationId;

		[Test]
		public void JumpToField_OnTheShownEntry_FocusesThatFieldsRowAtOnce()
		{
			LoadView("lexiconEdit");
			ShowEntry(m_entry);
			Assert.That(m_view.RowsAskedToFocus, Is.Null, "precondition: showing a record asks for no row");

			PublishJumpToField(0, m_sense, "Gloss", "first gloss");

			Assert.That(m_view.RowsAskedToFocus, Is.EqualTo(new[] { ShownRowId(m_sense, "Gloss") }));
		}

		[Test]
		public void JumpToField_ForAFieldOfAnotherEntry_ShowsThatEntry_ThenFocusesTheRow()
		{
			LoadView("lexiconEdit");
			ShowEntry(m_entry);
			var showsBefore = m_view.DetailShowCount;

			PublishJumpToField(m_other.Hvo, m_otherSense, "Gloss", "other gloss");

			Assert.That(m_view.Clerk.CurrentObject, Is.SameAs(m_other), "the clerk jumps at once");
			Assert.That(m_view.DetailShowCount, Is.EqualTo(showsBefore), "precondition: the entry is not shown yet");
			Assert.That(m_view.RowsAskedToFocus, Is.Null, "no row of the entry still shown is focused");

			DrainMediatorAndIdleQueues();

			Assert.That(ShownRows(m_view).Any(f => f.ObjectHvo == m_otherSense.Hvo), Is.True, "the other entry is shown");
			Assert.That(m_view.RowsAskedToFocus, Is.EqualTo(new[] { ShownRowId(m_otherSense, "Gloss") }));

			ShowEntry(m_entry);
			Assert.That(m_view.RowsAskedToFocus, Is.Null, "the request is answered once");
		}

		[Test]
		public void JumpToField_ForARecordTheClerkCannotShow_FocusesNothing()
		{
			LoadView("lexiconEdit");
			ShowEntry(m_entry);

			PublishJumpToField(Cache.LangProject.Hvo, m_sense, "Gloss", "first gloss");
			DrainMediatorAndIdleQueues();

			Assert.That(m_view.Clerk.CurrentObject, Is.SameAs(m_entry));
			Assert.That(m_view.RowsAskedToFocus, Is.Null);
		}

		[Test]
		public void JumpToField_ThatLandsOnNoRow_FocusesNothing()
		{
			LoadView("lexiconEdit");
			ShowEntry(m_entry);

			PublishJumpToField(0, m_other, "Bibliography", "no row shows this");

			Assert.That(m_view.RowsAskedToFocus, Is.Null);
		}

		// The preview names some fields as no row is named: the rule both views share maps
		// them onto the rows a composed entry really has.
		[Test]
		public void FieldJumpRow_FindsThePreviewsOwnFieldNames_AmongAComposedEntrysRows()
		{
			LoadView("lexiconEdit");
			ShowEntry(m_entry);
			var detail = ComposedDetail();

			var headword = m_view.FieldJumpRow(detail, m_entry,
				new JumpToFieldRequest(0, m_entry.Hvo, "MLHeadWord", "focus-entry"));
			Assert.That(headword, Is.Not.Null, "the headword lands on a row");
			Assert.That(headword.ObjectHvo, Is.EqualTo(m_entry.LexemeFormOA.Hvo));
			Assert.That(headword.Field, Is.EqualTo("Form"));

			var gloss = m_view.FieldJumpRow(detail, m_entry,
				new JumpToFieldRequest(0, m_sense.Hvo, "DefinitionOrGloss", "first gloss"));
			Assert.That(gloss, Is.Not.Null, "the gloss lands on a row");
			Assert.That(gloss.ObjectHvo, Is.EqualTo(m_sense.Hvo));
			Assert.That(gloss.Field, Is.EqualTo("Gloss"));

			var category = m_view.FieldJumpRow(detail, m_entry,
				new JumpToFieldRequest(0, m_sense.MorphoSyntaxAnalysisRA.Hvo, "MLPartOfSpeech", "noun"));
			Assert.That(category, Is.Not.Null, "the grammatical category lands on a row");
			Assert.That(category.ObjectHvo, Is.EqualTo(m_sense.Hvo));
			Assert.That(category.Field, Is.EqualTo("MorphoSyntaxAnalysis"));

			Assert.That(m_view.FieldJumpRow(detail, m_entry,
				new JumpToFieldRequest(0, m_other.Hvo, "Bibliography", "no row shows this")), Is.Null);
		}

		private DetailModel ComposedDetail() => m_view.ComposeDetail(m_entry, showHidden: false).Model;

		// The WinForms tests jump to the grammatical category: its slice takes focus without
		// placing a caret, which a view never realized on screen cannot do.
		[Test]
		public void JumpToField_InTheWinFormsView_MakesThatFieldsSliceCurrent()
		{
			LoadView("lexiconEdit", "Legacy");
			ShowEntry(m_entry);

			PublishJumpToField(0, m_sense.MorphoSyntaxAnalysisRA, "MLPartOfSpeech", "noun");

			var slice = m_view.DatTree.CurrentSlice;
			Assert.That(slice, Is.Not.Null, "a slice became current");
			Assert.That(slice.Object, Is.SameAs(m_sense));
			Assert.That(slice.Flid, Is.EqualTo(LexSenseTags.kflidMorphoSyntaxAnalysis));
		}

		[Test]
		public void JumpToField_InTheWinFormsView_ForAnotherEntry_WaitsForItsSlices()
		{
			LoadView("lexiconEdit", "Legacy");
			ShowEntry(m_entry);

			PublishJumpToField(m_other.Hvo, m_otherSense.MorphoSyntaxAnalysisRA, "MLPartOfSpeech", "noun");

			Assert.That(m_view.Clerk.CurrentObject, Is.SameAs(m_other), "the clerk jumps at once");
			Assert.That(m_view.DatTree.Root, Is.SameAs(m_entry), "precondition: the entry is not shown yet");

			DrainMediatorAndIdleQueues();

			Assert.That(m_view.DatTree.Root, Is.SameAs(m_other));
			var slice = m_view.DatTree.CurrentSlice;
			Assert.That(slice, Is.Not.Null, "a slice became current");
			Assert.That(slice.Object, Is.SameAs(m_otherSense));
			Assert.That(slice.Flid, Is.EqualTo(LexSenseTags.kflidMorphoSyntaxAnalysis));
		}

		[Test]
		public void JumpToField_OnAWinFormsTreeJustShown_IsAnsweredOnceItsSlicesAreReady()
		{
			LoadView("lexiconEdit", "Legacy");
			ShowEntry(m_entry);
			var tree = m_view.DatTree;

			tree.ShowObject(m_other, tree.RootLayoutName, null, m_other, true);
			tree.JumpToField(m_otherSense.MorphoSyntaxAnalysisRA.Hvo, "MLPartOfSpeech", "noun");

			Assert.That(tree.CurrentSlice, Is.Null, "the new slices cannot take focus yet");

			DrainMediatorAndIdleQueues();

			var slice = tree.CurrentSlice;
			Assert.That(slice, Is.Not.Null, "a slice became current");
			Assert.That(slice.Object, Is.SameAs(m_otherSense));
			Assert.That(slice.Flid, Is.EqualTo(LexSenseTags.kflidMorphoSyntaxAnalysis));
		}

		// The request names a field of the entry shown second, so answering it late would
		// make that entry's slice current.
		[Test]
		public void JumpToField_WaitingOnAWinFormsTree_IsDroppedWhenAnotherRecordIsShownFirst()
		{
			LoadView("lexiconEdit", "Legacy");
			ShowEntry(m_entry);
			var tree = m_view.DatTree;
			tree.ShowObject(m_other, tree.RootLayoutName, null, m_other, true);
			tree.JumpToField(m_sense.MorphoSyntaxAnalysisRA.Hvo, "MLPartOfSpeech", "noun");

			tree.ShowObject(m_entry, tree.RootLayoutName, null, m_entry, true);
			DrainMediatorAndIdleQueues();

			Assert.That(tree.CurrentSlice, Is.Null, "the waiting request went with the record it was asked on");
		}

		[Test]
		public void InsertingARecord_PutsTheKeyboardInItsFirstEditor_AlsoInAnEmptyList()
		{
			LoadView("ProdRestrictEdit");
			var features = Cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS;
			Assert.That(features, Is.Empty, "precondition: the list starts empty");
			var insert = CommandOf(m_propertyTable, "CmdInsertExceptionFeature");

			Assert.That(m_view.Clerk.OnInsertItemInVector(insert), Is.True);
			DrainMediatorAndIdleQueues();

			Assert.That(features, Has.Count.EqualTo(1));
			var first = features[0];
			AssertKeyboardWentToTheFirstEditorOf(first);

			Assert.That(m_view.Clerk.OnInsertItemInVector(insert), Is.True);
			DrainMediatorAndIdleQueues();

			Assert.That(features, Has.Count.EqualTo(2));
			AssertKeyboardWentToTheFirstEditorOf(features.Single(f => f != first));
		}

		private void AssertKeyboardWentToTheFirstEditorOf(ICmObject record)
		{
			Assert.That(m_view.Clerk.CurrentObject, Is.SameAs(record), "the record is current");
			Assert.That(ShownRows(m_view).Any(f => f.ObjectHvo == record.Hvo), Is.True, "the record is shown");
			var firstEditor = ShownRows(m_view).First(f => f.Kind != DetailFieldKind.Header
				&& f.Kind != DetailFieldKind.Unsupported);
			Assert.That(m_view.RowsAskedToFocus, Is.Not.Null.And.Not.Empty, "the show asks for a row");
			Assert.That(m_view.RowsAskedToFocus[0], Is.EqualTo(firstEditor.RowAutomationId));
		}

		[Test]
		public void APopupJump_PutsTheKeyboardInTheEntrysFirstEditor()
		{
			LoadView("lexiconEdit");
			ShowEntry(m_entry);

			Assert.That(m_view.Clerk.OnJumpToPopupRecord(m_other.Hvo), Is.True);
			DrainMediatorAndIdleQueues();

			AssertKeyboardWentToTheFirstEditorOf(m_other);
		}

		[Test]
		public void APopupJump_ToTheEntryAlreadyShown_FocusesItsFirstEditorAtOnce()
		{
			LoadView("lexiconEdit");
			ShowEntry(m_entry);

			Assert.That(m_view.Clerk.OnJumpToPopupRecord(m_entry.Hvo), Is.True);

			AssertKeyboardWentToTheFirstEditorOf(m_entry);
		}

		[Test]
		public void MovingToAnotherRecord_AsksForNoRow()
		{
			LoadView("lexiconEdit");
			ShowEntry(m_entry);
			Assert.That(m_view.Clerk.OnJumpToPopupRecord(m_other.Hvo), Is.True);
			DrainMediatorAndIdleQueues();
			Assert.That(m_view.RowsAskedToFocus, Is.Not.Null, "precondition");

			ShowEntry(m_entry);

			Assert.That(m_view.RowsAskedToFocus, Is.Null, "a plain move leaves the keyboard where it is");
		}

		[Test]
		public void EditorRows_AreTheRowsWithAnEditor_InViewOrder()
		{
			LoadView("lexiconEdit");
			ShowEntry(m_entry);
			var detail = ComposedDetail();
			Assert.That(detail.Fields.Any(f => f.Kind == DetailFieldKind.Header), Is.True, "precondition: headers");

			var rows = RecordEditView.EditorRows(detail);

			Assert.That(rows, Is.EqualTo(detail.Fields.Where(f => f.Kind != DetailFieldKind.Header
				&& f.Kind != DetailFieldKind.Unsupported).Select(f => f.RowAutomationId).ToList()));
			Assert.That(rows[0], Is.EqualTo(ShownRowId(m_entry.LexemeFormOA, "Form")), "the lexeme form comes first");
		}
	}
}
