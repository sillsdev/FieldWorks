// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// End-to-end proof that the Lexicon menu commands the native authority answers change
	/// the model through the product <see cref="RecordEditView"/>: the item's own Execute runs
	/// the host, which asks for confirmation, edits in one undoable step and tells the re-show
	/// where to put the keyboard.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class LexiconMenuExecutionTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private ILexSense m_sense1;
		private ILexSense m_sense2;
		private ILexExampleSentence m_example;
		private ICmTranslation m_translation;
		private ILexExtendedNote m_note1;
		private ILexExtendedNote m_note2;
		private ILexEtymology m_etymology1;
		private ILexEtymology m_etymology2;
		private ILexEntryRef m_complexFormRef;
		private RecordEditView m_view;

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
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			TestLocalizationManagerBootstrap.EnsureInitialized();
			TestLocalizationManagerBootstrap.EnsureHelpTopicProvider(m_propertyTable);
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateTestEntry);
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();
			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the lexicon edit RecordEditView to load");
			m_view.Clerk.JumpToRecord(m_entry.Hvo);
			DrainMediatorAndIdleQueues();
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
			});
			m_createdObjects = null;
			m_entry = null;
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
		public void DeleteTranslation_RemovesIt_InOneUndoableStep_WhenTheUserConfirms()
		{
			var confirmed = 0;
			m_view.DeleteConfirmation = ui =>
			{
				Assert.That(ui.Object.Hvo, Is.EqualTo(m_translation.Hvo), "the confirmation names the row's object");
				confirmed++;
				return true;
			};
			var translationHvo = m_translation.Hvo;

			InvokeDelete(TranslationRow(), "Delete Translation");

			Assert.That(confirmed, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.ObjectRepository.IsValidObjectId(translationHvo), Is.False, "the translation is gone");
			Assert.That(m_example.TranslationsOC, Is.Empty);
			Assert.That(DetailComposer.Compose(m_entry, Cache).Model.Fields.Any(f => f.ObjectHvo == translationHvo), Is.False,
				"the recomposed detail view has no row for it");

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_example.TranslationsOC, Has.Count.EqualTo(1), "one Undo brings the translation back");
		}

		[Test]
		public void DeleteTranslation_LeavesTheModelAlone_WhenTheUserDeclines()
		{
			m_view.DeleteConfirmation = ui => false;

			InvokeDelete(TranslationRow(), "Delete Translation");

			Assert.That(m_translation.IsValidObject, Is.True);
			Assert.That(m_example.TranslationsOC, Has.Count.EqualTo(1));
		}

		[Test]
		public void DeleteComplexFormInfo_RemovesTheEntryRef()
		{
			m_view.DeleteConfirmation = ui => true;
			var row = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.MenuId == "mnuDataTree-ComplexFormSpec");
			Assert.That(row.ObjectHvo, Is.EqualTo(m_complexFormRef.Hvo), "precondition: the row shows the entry's own ref");

			InvokeDelete(row, "Delete Complex Form Info");

			Assert.That(m_entry.EntryRefsOS, Is.Empty);
			Assert.That(m_entry.IsValidObject, Is.True, "only the ref goes, not the entry");
		}

		// Candidates: the rows before the object's rows (its own and its descendants'), nearest
		// first, then the rows after. The header before a last translation dies with it.
		[Test]
		public void RowsToFocusAfterDeleting_AreTheNeighbours_BeforeThenAfter_NearestFirst()
		{
			var model = DetailComposer.Compose(m_entry, Cache).Model;
			var fields = model.Fields.ToList();
			var ownedByTranslation = fields.Where(f => f.ObjectHvo == m_translation.Hvo).ToList();
			var first = fields.IndexOf(ownedByTranslation.First());
			var last = fields.IndexOf(ownedByTranslation.Last());
			Assert.That(first, Is.GreaterThan(1), "precondition: rows precede the translation's");
			Assert.That(fields[first - 1].Field, Is.EqualTo("Translations"), "precondition: the section header is just before");
			Assert.That(fields[first - 2].ObjectHvo, Is.EqualTo(m_example.Hvo), "precondition: the example's row is next");

			var candidates = RecordEditView.RowsToFocusAfterDeleting(model, m_translation);

			var expected = Enumerable.Range(0, first).Reverse().Select(i => fields[i].RowAutomationId)
				.Concat(Enumerable.Range(last + 1, fields.Count - last - 1).Select(i => fields[i].RowAutomationId));
			Assert.That(candidates, Is.EqualTo(expected.ToList()));
			Assert.That(RecordEditView.RowsToFocusAfterDeleting(model, m_example)[0],
				Is.EqualTo(fields[fields.FindIndex(f => f.ObjectHvo == m_example.Hvo) - 1].RowAutomationId),
				"the translation's rows belong to the example that owns it");
			Assert.That(RecordEditView.RowsToFocusAfterDeleting(model, m_entry), Is.Empty,
				"every row belongs to the entry, so nothing is left to focus");
			Assert.That(RecordEditView.RowsToFocusAfterDeleting(null, m_translation), Is.Empty);
		}

		[Test]
		public void InsertSubsense_FromTheSubsensesRow_AppendsItUnderTheSense_InOneUndoableStep()
		{
			var row = Rows().Single(f => f.MenuId == "mnuDataTree-Subsenses" && f.ObjectHvo == m_sense1.Hvo);
			Assert.That(row.GhostPrompt, Is.Not.Null, "precondition: no subsense yet, so the prompt row binds the id");

			InvokeItem(row, DetailMenuKind.SliceMenu, "Insert Subsense");

			Assert.That(m_sense1.SensesOS, Has.Count.EqualTo(1));
			var subsense = m_sense1.SensesOS[0];
			Assert.That(subsense.MorphoSyntaxAnalysisRA, Is.SameAs(m_sense1.MorphoSyntaxAnalysisRA),
				"the new subsense takes its parent's grammatical info");
			Assert.That(Rows().Any(f => f.ObjectHvo == subsense.Hvo), Is.True, "the recomposed view shows it");

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense1.SensesOS, Is.Empty, "one Undo removes it");
		}

		[Test]
		public void InsertSense_FromTheFirstSensesHotlinks_GoesRightAfterIt()
		{
			InvokeItem(HeaderRowOf(m_sense1), DetailMenuKind.Hotlinks, "Insert Sense");

			Assert.That(m_entry.SensesOS, Has.Count.EqualTo(3));
			Assert.That(m_entry.SensesOS[0], Is.SameAs(m_sense1));
			Assert.That(m_entry.SensesOS[2], Is.SameAs(m_sense2), "the new sense sits between the two");
			Assert.That(m_entry.SensesOS[1].MorphoSyntaxAnalysisRA, Is.SameAs(m_sense1.MorphoSyntaxAnalysisRA),
				"the new sense takes the grammatical info of the one before it");
		}

		[Test]
		public void InsertExample_FromTheSensesHotlinks_GoesLast()
		{
			InvokeItem(HeaderRowOf(m_sense1), DetailMenuKind.Hotlinks, "Insert Example");

			Assert.That(m_sense1.ExamplesOS, Has.Count.EqualTo(2));
			Assert.That(m_sense1.ExamplesOS[0], Is.SameAs(m_example));
		}

		[Test]
		public void InsertExtendedNote_FromTheFirstNotesHotlinks_GoesRightAfterIt()
		{
			InvokeItem(HeaderRowOf(m_note1), DetailMenuKind.Hotlinks, "Insert Extended Note");

			Assert.That(m_sense1.ExtendedNoteOS, Has.Count.EqualTo(3));
			Assert.That(m_sense1.ExtendedNoteOS[0], Is.SameAs(m_note1));
			Assert.That(m_sense1.ExtendedNoteOS[2], Is.SameAs(m_note2), "the new note sits between the two");
		}

		// The Extended Notes section shows only with data, so no prompt row inserts the first.
		[Test]
		public void InsertExtendedNote_FromTheSectionRow_GoesLast()
		{
			var section = Rows().Single(f => f.MenuId == "mnuDataTree-ExtendedNotes" && f.ObjectHvo == m_sense1.Hvo);
			Assert.That(section.Kind, Is.EqualTo(DetailFieldKind.Header), "precondition: the section header binds the id");
			Assert.That(Rows().Any(f => f.MenuId == "mnuDataTree-ExtendedNotes" && f.ObjectHvo == m_sense2.Hvo), Is.False,
				"precondition: the second sense has no note, so no section row");

			InvokeItem(section, DetailMenuKind.SliceMenu, "Insert Extended Note");

			Assert.That(m_sense1.ExtendedNoteOS, Has.Count.EqualTo(3));
			Assert.That(m_sense1.ExtendedNoteOS[1], Is.SameAs(m_note2), "the new note is last");
		}

		[Test]
		public void InsertEtymology_FromTheFirstEtymologysHotlinks_GoesRightAfterIt()
		{
			InvokeItem(HeaderRowOf(m_etymology1), DetailMenuKind.Hotlinks, "Insert Etymology");

			Assert.That(m_entry.EtymologyOS, Has.Count.EqualTo(3));
			Assert.That(m_entry.EtymologyOS[0], Is.SameAs(m_etymology1));
			Assert.That(m_entry.EtymologyOS[2], Is.SameAs(m_etymology2), "the new etymology sits between the two");
		}

		[Test]
		public void InsertDetailObject_ReturnsTheNewObject_AndNothingWhenTheRowsObjectIsGone()
		{
			var leaf = XCoreMenuBridge.ResolveMenu(m_propertyTable.GetValue<XWindow>("window"), "mnuDataTree-Etymology-Hotlinks")
				.Leaves.Single();
			var row = HeaderRowOf(m_etymology2);
			var gone = new DetailField(row.StableId, row.Label, row.Field, null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null,
				hotlinksId: row.HotlinksId, objectHvo: int.MaxValue - 1);

			var created = m_view.InsertDetailObject(row, leaf.Command);

			Assert.That(created, Is.InstanceOf<ILexEtymology>());
			Assert.That(created.Owner, Is.SameAs(m_entry));
			Assert.That(m_view.InsertDetailObject(gone, leaf.Command), Is.Null);
			Assert.That(m_entry.EtymologyOS, Has.Count.EqualTo(3));
		}

		// Builds the row's label menu as the host does and runs the named item's own Execute.
		private void InvokeDelete(DetailField row, string label) => InvokeItem(row, DetailMenuKind.SliceMenu, label);

		// Builds the row's menu of the given kind as the host does and runs the named item's
		// own Execute.
		private void InvokeItem(DetailField row, DetailMenuKind kind, string label)
		{
			var ids = kind == DetailMenuKind.Hotlinks
				? new[] { row.HotlinksId }
				: RecordEditView.ComposeSliceMenuIds(row.MenuId, row.IsMultiStringRow).ToArray();
			var window = m_propertyTable.GetValue<XWindow>("window");
			var request = DetailMenuRequest.FromAnchor(null, row, kind);
			var items = XCoreMenuBridge.CreateMenuItems(window, ids, null, null, m_view.CreateMenuAuthority(request));
			var item = items.FirstOrDefault(i => i.Label == label);
			Assert.That(item, Is.Not.Null, "the menu offers '{0}'", label);
			Assert.That(item.Execute, Is.Not.Null, "'{0}' is enabled", label);
			item.Execute();
			DrainMediatorAndIdleQueues();
		}

		private IReadOnlyList<DetailField> Rows() => DetailComposer.Compose(m_entry, Cache).Model.Fields;

		// The object's item header row, the one its hotlinks hang off.
		private DetailField HeaderRowOf(ICmObject obj)
			=> Rows().First(f => f.ObjectHvo == obj.Hvo && f.Kind == DetailFieldKind.Header && f.HotlinksId != null);

		private DetailField TranslationRow()
			=> DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Single(f => f.Field == "Translation" && f.ObjectHvo == m_translation.Hvo);

		// An entry with two senses and two etymologies; the first sense has an example with
		// a free translation and two extended notes. The entry is a complex form of another.
		private void CreateTestEntry()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "command-entry", stemMorphType, "first gloss", noun);
			m_sense1 = m_entry.SensesOS[0];
			m_sense2 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(m_sense2);
			m_note1 = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
			m_sense1.ExtendedNoteOS.Add(m_note1);
			m_note2 = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
			m_sense1.ExtendedNoteOS.Add(m_note2);
			m_etymology1 = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			m_entry.EtymologyOS.Add(m_etymology1);
			m_etymology2 = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			m_entry.EtymologyOS.Add(m_etymology2);
			m_example = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create();
			m_sense1.ExamplesOS.Add(m_example);
			m_example.Example.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("una casa", Cache.DefaultVernWs));
			var type = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>()
				.GetObject(CmPossibilityTags.kguidTranFreeTranslation);
			m_translation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(m_example, type);
			m_translation.Translation.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("a house", Cache.DefaultAnalWs));

			var component = AddLexeme(m_createdObjects, "part", stemMorphType, "part gloss", noun);
			m_complexFormRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			m_entry.EntryRefsOS.Add(m_complexFormRef);
			m_complexFormRef.RefType = LexEntryRefTags.krtComplexForm;
			m_complexFormRef.ComponentLexemesRS.Add(component);
			m_complexFormRef.PrimaryLexemesRS.Add(component);
			m_complexFormRef.ComplexEntryTypesRS.Add(Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>()
				.GetObject(LexEntryTypeTags.kguidLexTypCompound));
		}

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(windowConfiguration, Is.Not.Null);
			var controlNode = windowConfiguration.SelectSingleNode(
				string.Format("//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']", toolValue));
			Assert.That(controlNode, Is.Not.Null, "Expected the RecordEditView configuration node for tool '{0}'.", toolValue);

			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
		}
	}
}
