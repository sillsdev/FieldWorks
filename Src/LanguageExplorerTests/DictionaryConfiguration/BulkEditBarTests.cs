// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Areas.Lexicon.Tools.BulkEditEntries;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using LanguageExplorer.Impls;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.WritingSystems;

namespace LanguageExplorerTests.DictionaryConfiguration
{
#if RANDYTODO
	// TODO: Tests have the same problem with mis-matched classes, as does the "Bulk Edit Entries" tool. Fix one, fix both (maybe).
	// (FLEx) JohnT: I did a first rough cut at updating these tests, but it will take really understanding them
	// to figure out what needs to be mocked or set up so they can work.
	internal class BulkEditBarTestsBase : MemoryOnlyBackendProviderTestBase
	{
		private ICmPossibilityFactory m_possFact;
		private ICmPossibilityRepository m_possRepo;
		private IPartOfSpeechFactory m_posFact;
		private IPartOfSpeechRepository m_posRepo;
		private ILexEntryFactory m_entryFact;
		private ILexSenseFactory m_senseFact;
		private IMoStemAllomorphFactory m_stemFact;
		private IMoAffixAllomorphFactory m_affixFact;
		private FlexComponentParameters _flexComponentParameters;
		private RecordBrowseViewForTests _recordBrowseViewForTests;
		protected BulkEditBarForTests _bulkEditBarForTests;
		protected BrowseViewerForTests _browseViewerForTests;
		protected List<ICmObject> _createdObjectList;
		private StatusBar _statusBar;
		private EntriesOrChildClassesRecordList _entriesOrChildClassesRecordList;
		private IRecordListRepository _recordListRepository;
		private DummyApp _dummyApp;
		private bool _sldrInitializedByMe;

	#region Setup and Teardown

		public override void FixtureSetup()
		{
			base.FixtureSetup();

			if (!Sldr.IsInitialized)
			{
				_sldrInitializedByMe = true;
				Sldr.Initialize();
			}
			// Cache factories.
			var servLoc = Cache.ServiceLocator;
			m_possFact = servLoc.GetInstance<ICmPossibilityFactory>();
			m_possRepo = servLoc.GetInstance<ICmPossibilityRepository>();
			m_posFact = servLoc.GetInstance<IPartOfSpeechFactory>();
			m_posRepo = servLoc.GetInstance<IPartOfSpeechRepository>();
			m_entryFact = servLoc.GetInstance<ILexEntryFactory>();
			m_senseFact = servLoc.GetInstance<ILexSenseFactory>();
			m_stemFact = servLoc.GetInstance<IMoStemAllomorphFactory>();
			m_affixFact = servLoc.GetInstance<IMoAffixAllomorphFactory>();
			_createdObjectList = new List<ICmObject>();
		}

		/// <summary />
		public override void FixtureTeardown()
		{
			if (_sldrInitializedByMe)
			{
				Sldr.Cleanup();
				_sldrInitializedByMe = false;
			}
			_createdObjectList = null;
		}

		/// <summary />
		public override void TestSetup()
		{
			base.TestSetup();

			_flexComponentParameters = TestSetupServices.SetupEverything(Cache);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var stemMT = GetMorphTypeOrCreateOne("stem");
				var rootMT = GetMorphTypeOrCreateOne("root");
				var boundRootMT = GetMorphTypeOrCreateOne("bound root");
				var adjPOS = GetGrammaticalCategoryOrCreateOne("adjective", Cache.LangProject.PartsOfSpeechOA);
				var verbPOS = GetGrammaticalCategoryOrCreateOne("verb", Cache.LangProject.PartsOfSpeechOA);
				var transVbPOS = GetGrammaticalCategoryOrCreateOne("transitive verb", verbPOS);
				var nounPOS = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				var concNounPOS = GetGrammaticalCategoryOrCreateOne("concrete noun", nounPOS);
				// le='pus' mtype='root' Sense1(gle='green' pos='adj.')
				AddLexeme(_createdObjectList, "pus", rootMT, "green", adjPOS);
				// le='bili' mtype='bound root' Sense1(gle='to.see' pos='trans.verb' inflCls=1)
				//     cit.form='himbilira'     Sense2(gle='to.understand' pos='trans.verb' inflCls=1)
				var le = AddLexeme(_createdObjectList, "bili", "himbilira", boundRootMT, "to.see", transVbPOS);
				AddSenseToEntry(_createdObjectList, le, "to.understand", transVbPOS);
				// le='underlying form' mtype='root' Sense1(gle='English gloss' pos='noun')
				//     cit.form='ztestmain'     (... several more senses...)
				var le2 = AddLexeme(_createdObjectList, "underlying form", "ztestmain", rootMT, "English gloss", nounPOS);
				AddSubSenseToSense(_createdObjectList, le2.AllSenses[0], "English subsense gloss1.1", concNounPOS);
				AddSenseToEntry(_createdObjectList, le2, "English gloss2", null);
				AddSubSenseToSense(_createdObjectList, le2.AllSenses[0], "English subsense gloss1.2", null);
				var stemAllomorph = m_stemFact.Create();
				le2.AlternateFormsOS.Add(stemAllomorph);
				stemAllomorph.Form.set_String(Cache.DefaultVernWs, "stem allomorph");
				_createdObjectList.Add(stemAllomorph);
				var affixAllomorph = m_affixFact.Create();
				le2.AlternateFormsOS.Add(affixAllomorph);
				affixAllomorph.Form.set_String(Cache.DefaultVernWs, "affix allomorph");
				_createdObjectList.Add(affixAllomorph);
			});
			_dummyApp = new DummyApp();
			_flexComponentParameters.PropertyTable.SetProperty(LanguageExplorerConstants.App, _dummyApp);
			_statusBar = new StatusBar();
			_entriesOrChildClassesRecordList = new EntriesOrChildClassesRecordList(AreaServices.EntriesOrChildren, _statusBar, Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), Cache.LanguageProject.LexDbOA);
			_entriesOrChildClassesRecordList.InitializeFlexComponent(_flexComponentParameters);
			_recordListRepository = _flexComponentParameters.PropertyTableGetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository);
			_recordListRepository.ActiveRecordList = _entriesOrChildClassesRecordList;
			var root = XDocument.Parse(LexiconResources.BulkEditEntriesOrSensesToolParameters).Root;
			var parametersElement = root.Element("parameters");
			parametersElement.Element("includeColumns").ReplaceWith(XElement.Parse(LexiconResources.LexiconBrowseDialogColumnDefinitions));
			OverrideServices.OverrideVisibiltyAttributes(parametersElement.Element("columns"), root.Element("overrides"));
			_recordBrowseViewForTests = new RecordBrowseViewForTests(parametersElement, Cache, _entriesOrChildClassesRecordList);
			_recordBrowseViewForTests.InitializeFlexComponent(_flexComponentParameters);
			_browseViewerForTests = (BrowseViewerForTests)_recordBrowseViewForTests.BrowseViewer;
			_bulkEditBarForTests = (BulkEditBarForTests)_browseViewerForTests.BulkEditBar;
		}

		public override void TestTearDown()
		{
			UndoAllActions();
			_flexComponentParameters.PropertyTable.RemoveProperty(LanguageExplorerConstants.App);
			_flexComponentParameters.PropertyTable.RemoveProperty(LanguageExplorerConstants.RecordListRepository);
			TestSetupServices.DisposeTrash(_flexComponentParameters);
			_flexComponentParameters = null;
			_statusBar?.Dispose();
			_statusBar = null;
			_dummyApp?.Dispose();
			_dummyApp = null;
			_recordListRepository?.Dispose();
			_recordListRepository = null;
			_entriesOrChildClassesRecordList?.Dispose();
			_entriesOrChildClassesRecordList = null;
			_recordBrowseViewForTests?.Dispose();
			_recordBrowseViewForTests = null;
			_browseViewerForTests?.Dispose();
			_browseViewerForTests = null;
			_bulkEditBarForTests?.Dispose();
			_bulkEditBarForTests = null;

			base.TestTearDown();
		}

		private void UndoAllActions()
		{
			// Often looping through Undo() is not enough because changing
			// 'CurrentContentControl' zaps undo stack!
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var obj in _createdObjectList)
				{
					if (!obj.IsValidObject)
					{
						continue; // owned object could have been deleted already by owner
					}
					if (obj is IMoMorphType || obj is IPartOfSpeech)
					{
						continue; // these don't need to be deleted between tests
					}
					if (obj is ILexEntry)
					{
						obj.Delete();
					}
					if (obj is ICmSemanticDomain)
					{
						obj.Delete();
					}
					if (obj is ILexEntryRef)
					{
						obj.Delete();
					}
					// Some types won't get deleted directly (e.g. ILexSense),
					// but should get deleted by their owner.
				}

			});
			_createdObjectList.Clear();
		}

		/// <summary>
		/// Will find a morph type (if one exists) with the given (analysis ws) name.
		/// If not found, will create the morph type in the Lexicon MorphTypes list.
		/// </summary>
		protected IMoMorphType GetMorphTypeOrCreateOne(string morphTypeName)
		{
			var poss = m_possRepo.AllInstances().FirstOrDefault(someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == morphTypeName);
			if (poss != null)
			{
				return poss as IMoMorphType;
			}
			var owningList = Cache.LangProject.LexDbOA.MorphTypesOA;
			var ws = Cache.DefaultAnalWs;
			poss = m_possFact.Create(new Guid(), owningList);
			poss.Name.set_String(ws, morphTypeName);
			return poss as IMoMorphType;
		}

		/// <summary>
		/// Will find a variant entry type (if one exists) with the given (analysis ws) name.
		/// If not found, will create the variant entry type in the Lexicon VariantEntryTypes list.
		/// </summary>
		protected ILexEntryType GetVariantTypeOrCreateOne(string variantTypeName)
		{
			var poss = m_possRepo.AllInstances().FirstOrDefault(someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == variantTypeName);
			if (poss != null)
			{
				return poss as ILexEntryType;
			}
			// shouldn't get past here; they're already defined.
			var owningList = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
			var ws = Cache.DefaultAnalWs;
			poss = m_possFact.Create(new Guid(), owningList);
			poss.Name.set_String(ws, variantTypeName);
			return poss as ILexEntryType;
		}

		/// <summary>
		/// Will find a grammatical category (if one exists) with the given (analysis ws) name.
		/// If not found, will create a category as a subpossibility of a grammatical category.
		/// </summary>
		protected IPartOfSpeech GetGrammaticalCategoryOrCreateOne(string catName, IPartOfSpeech owningCategory)
		{
			return GetGrammaticalCategoryOrCreateOne(catName, null, owningCategory);
		}

		/// <summary>
		/// Will find a grammatical category (if one exists) with the given (analysis ws) name.
		/// If not found, will create the grammatical category in the owning list.
		/// </summary>
		protected IPartOfSpeech GetGrammaticalCategoryOrCreateOne(string catName, ICmPossibilityList owningList)
		{
			return GetGrammaticalCategoryOrCreateOne(catName, owningList, null);
		}

		/// <summary>
		/// Will find a grammatical category (if one exists) with the given (analysis ws) name.
		/// If not found, will create a grammatical category either as a possibility of a list,
		/// or as a subpossibility of a category.
		/// </summary>
		protected IPartOfSpeech GetGrammaticalCategoryOrCreateOne(string catName, ICmPossibilityList owningList, IPartOfSpeech owningCategory)
		{
			var category = m_posRepo.AllInstances().FirstOrDefault(someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == catName);
			if (category != null)
			{
				return category;
			}
			var ws = Cache.DefaultAnalWs;
			if (owningList == null)
			{
				if (owningCategory == null)
				{
					throw new ArgumentException("Grammatical category not found and insufficient information given to create one.");
				}
				category = m_posFact.Create(new Guid(), owningCategory);
			}
			else
			{
				category = m_posFact.Create(new Guid(), owningList);
			}
			category.Name.set_String(ws, catName);
			return category;
		}

		protected ILexEntry AddLexeme(IList<ICmObject> addList, string lexForm, string citationForm, IMoMorphType morphTypePoss, string gloss, IPartOfSpeech catPoss)
		{
			var ws = Cache.DefaultVernWs;
			var le = AddLexeme(addList, lexForm, morphTypePoss, gloss, catPoss);
			le.CitationForm.set_String(ws, citationForm);
			return le;
		}

		protected ILexEntry AddLexeme(IList<ICmObject> addList, string lexForm, IMoMorphType morphTypePoss, string gloss, IPartOfSpeech categoryPoss)
		{
			var msa = new SandboxGenericMSA { MainPOS = categoryPoss };
			var comp = new LexEntryComponents { MorphType = morphTypePoss, MSA = msa };
			comp.GlossAlternatives.Add(TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
			comp.LexemeFormAlternatives.Add(TsStringUtils.MakeString(lexForm, Cache.DefaultVernWs));
			var entry = m_entryFact.Create(comp);
			addList.Add(entry);
			return entry;
		}

		protected ILexEntry AddVariantLexeme(IList<ICmObject> addList, IVariantComponentLexeme origLe, string lexForm, IMoMorphType morphTypePoss, string gloss,
			IPartOfSpeech categoryPoss, ILexEntryType varType)
		{
			Guard.ArgumentNotNull(varType, nameof(varType));

			var msa = new SandboxGenericMSA { MainPOS = categoryPoss };
			var comp = new LexEntryComponents { MorphType = morphTypePoss, MSA = msa };
			comp.GlossAlternatives.Add(TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
			comp.LexemeFormAlternatives.Add(TsStringUtils.MakeString(lexForm, Cache.DefaultVernWs));
			var entry = m_entryFact.Create(comp);
			var ler = entry.MakeVariantOf(origLe, varType);
			addList.Add(entry);
			addList.Add(ler);
			return entry;
		}

		protected ILexSense AddSenseToEntry(IList<ICmObject> addList, ILexEntry le, string gloss, IPartOfSpeech catPoss)
		{
			var msa = new SandboxGenericMSA
			{
				MainPOS = catPoss
			};
			var sense = m_senseFact.Create(le, msa, gloss);
			addList.Add(sense);
			return sense;
		}

		protected ILexSense AddSubSenseToSense(IList<ICmObject> addList, ILexSense ls, string gloss, IPartOfSpeech catPoss)
		{
			var msa = new SandboxGenericMSA
			{
				MainPOS = catPoss
			};
			var sense = m_senseFact.Create(new Guid(), ls);
			sense.SandboxMSA = msa;
			sense.Gloss.set_String(Cache.DefaultAnalWs, gloss);
			addList.Add(sense);
			return sense;
		}

	#endregion Setup and Teardown

		internal sealed class BulkEditBarForTests : BulkEditBar
		{
			internal BulkEditBarForTests(BrowseViewer bv, XElement spec, FlexComponentParameters flexComponentParameters, LcmCache cache)
				: base(bv, spec, flexComponentParameters, cache)
			{
			}

			internal void SwitchTab(string tabName)
			{
				var tabIndex = (int)Enum.Parse(typeof(BulkEditBarTabs), tabName);
				m_operationsTabControl.SelectedIndex = tabIndex;
			}

			/// <summary>
			///
			/// </summary>
			internal int SelectedTab => m_operationsTabControl.SelectedIndex;

			/// <summary>
			///
			/// </summary>
			internal FieldComboItem SelectedTargetFieldItem => CurrentTargetCombo.SelectedItem as FieldComboItem;

			internal FieldComboItem SetTargetField(string label)
			{
				CurrentTargetCombo.Text = label;
				if (CurrentTargetCombo.Text != label)
				{
					throw new ApplicationException($"Couldn't change to target field {label}, need to ShowColumn()");
				}
				// trigger event explicitly, since tests don't do it reliably.
				return SelectedTargetFieldItem;
			}

			internal List<FieldComboItem> GetTargetFields()
			{
				var items = new List<FieldComboItem>();
				foreach (FieldComboItem item in CurrentTargetCombo.Items)
				{
					items.Add(item);
				}
				return items;
			}

			internal Control GetTabControlChild(string controlName)
			{
				var matches = m_operationsTabControl.SelectedTab.Controls.Find(controlName, true);
				if (matches != null && matches.Length > 0)
				{
					return matches[0];
				}
				return null;
			}

			internal IBulkEditSpecControl CurrentBulkEditSpecControl => m_beItems[m_itemIndex].BulkEditControl;

			protected internal override void SaveSettings()
			{
				if (PersistSettings)
				{
					base.SaveSettings();
				}
			}

			internal bool PersistSettings { get; set; }

			internal void ClickPreview()
			{
				m_previewButton_Click(null, EventArgs.Empty);
			}

			internal void ClickApply()
			{
				m_ApplyButton_Click(null, EventArgs.Empty);
			}

			internal void ClickSuggest()
			{
				m_suggestButton_Click(null, EventArgs.Empty);
			}
		}

		internal class BrowseViewerForTests : BrowseViewer
		{
			internal BrowseViewerForTests(XElement nodeSpec, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sdaRecordList)
				: base(nodeSpec, hvoRoot, cache, sortItemProvider, sdaRecordList)
			{
			}

			///  <summary />
			protected override BulkEditBar CreateBulkEditBar(BrowseViewer bv, XElement spec, FlexComponentParameters flexComponentParameters, LcmCache cache)
			{
				return new BulkEditBarForTests(bv, spec, flexComponentParameters, cache);
			}

			private AnywhereMatcher CreateAnywhereMatcher(string pattern, int ws)
			{
				IVwPattern ivwpattern = VwPatternClass.Create();
				ivwpattern.Pattern = TsStringUtils.MakeString(pattern, ws);
				ivwpattern.MatchCase = true;
				ivwpattern.MatchDiacritics = true;

				// Default values because we don't set these here
				ivwpattern.MatchOldWritingSystem = false;
				ivwpattern.MatchWholeWord = false;
				ivwpattern.UseRegularExpressions = false;
				ivwpattern.IcuLocale = Cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
				return new AnywhereMatcher(ivwpattern);
			}

			internal FilterSortItem SetFilter(string columnName, string filterType, string query)
			{
				// get ColumnInfo for specified column
				var fsiTarget = FindColumnInfo(columnName);
				var index = fsiTarget.Combo.FindStringExact(filterType);
				if (index < 0)
				{
					return null;
				}
				var fci = fsiTarget.Combo.Items[index] as FilterComboItem;
				if (filterType.EndsWith("..."))
				{
					// these are dialogs
					if (filterType == "Filter for...")
					{
						var ws = (fci as FindComboItem).Ws;
						(fci as FindComboItem).Matcher = CreateAnywhereMatcher(query, ws);
						fci.InvokeWithInstalledMatcher();
					}
					else if (filterType == "Choose...")
					{
						// by default match on "Any"
						(fci as ListChoiceComboItem).InvokeWithColumnSpecFilter(ListMatchOptions.Any, new List<string>(new string[] { query }));
					}
				}
				else
				{
					// invoke simple filters.
					fci.Invoke();
				}
				return fsiTarget;
			}

			private FilterSortItem FindColumnInfo(string columnName)
			{
				FilterSortItem fsiTarget = null;
				foreach (var fsi in FilterBar.ColumnInfo)
				{
					if (fsi.Spec.Attribute("label").Value == columnName || fsi.Spec.Attribute("headerlabel") != null && fsi.Spec.Attribute("headerlabel").Value == columnName)
					{
						fsiTarget = fsi;
						break;
					}
				}
				return fsiTarget;
			}

			internal FilterSortItem SetSort(string columnName)
			{
				var fsiTarget = FindColumnInfo(columnName);
				var fsiList = new List<FilterSortItem>(FilterBar.ColumnInfo);
				var indexColSpec = fsiList.IndexOf(fsiTarget);
				var indexOfColumnHeader = indexColSpec + ColumnIndexOffset();
				m_lvHeader_ColumnLeftClick(this, new ColumnClickEventArgs(indexOfColumnHeader));
				return fsiTarget;
			}

			internal void ShowColumn(string layoutName)
			{
				// get column matching the given layoutName
				var possibleColumns = BrowseView.Vc.ComputePossibleColumns();
				var colSpec = XmlViewsUtils.FindNodeWithAttrVal(possibleColumns, "layout", layoutName);
				if (IsColumnHidden(colSpec))
				{
					AppendColumn(colSpec);
					UpdateColumnList();
				}
			}

			internal void OnUncheckAll()
			{
				OnUncheckAll(null, EventArgs.Empty);
			}

			internal void UnselectItem(int hvo)
			{
				SetItemCheckedState(hvo, false, false);
			}

			internal void SelectItem(int hvo)
			{
				SetItemCheckedState(hvo, true, false);
			}

			internal void UncheckItems(IEnumerable<int> items)
			{
				foreach (var hvo in items)
				{
					UnselectItem(hvo);
				}
			}

			internal IList<int> UncheckedItems()
			{
				IList<int> uncheckedItems = new List<int>();
				foreach (var hvoItem in AllItems)
				{
					if (!IsItemChecked(hvoItem))
					{
						uncheckedItems.Add(hvoItem);
					}
				}

				return uncheckedItems;
			}
		}

		internal class RecordBrowseViewForTests : RecordBrowseView
		{

			internal RecordBrowseViewForTests(XElement browseViewDefinitions, LcmCache cache, IRecordList recordList)
				: base(browseViewDefinitions, cache, recordList)
			{
			}

			protected override BrowseViewer CreateBrowseViewer(XElement nodeSpec, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda, UiWidgetController uiWidgetController)
			{
				return new BrowseViewerForTests(nodeSpec, hvoRoot, cache, sortItemProvider, sda);
			}

			protected override void PersistSortSequence()
			{
				// Do no persisting.
			}

		}
	}

	[TestFixture]
	internal class BulkEditBarTests : BulkEditBarTestsBase
	{
	#region BulkEditEntries tests
		[Test]
		public void ChoiceFilters()
		{
			_bulkEditBarForTests.PersistSettings = true;
			_bulkEditBarForTests.SwitchTab("ListChoice");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			//FilterSortItem fsFilter = _browseViewerForTests.SetFilter("Lexeme Form", "Filter for...", "underlying form");
			_browseViewerForTests.SetFilter("Lexeme Form", "Filter for...", "underlying form");
			// next make a chooser filter on "Entry Type" column
			//fsFilter = _browseViewerForTests.SetFilter("Morph Type", "Choose...", "root");
			_browseViewerForTests.SetFilter("Morph Type", "Choose...", "root");
			_browseViewerForTests.SetSort("Lexeme Form");
			// Make sure our filters have worked to limit the data
			Assert.AreEqual(1, _browseViewerForTests.AllItems.Count);
			// now switch list items to senses, and see if our Main Entry filter still has results.
			// TargetField == Sense (e.g. "Grammatical Category")
			_bulkEditBarForTests.SetTargetField("Grammatical Category");
			Assert.AreEqual("Grammatical Category", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
			// make sure current record is a Sense
			// Make sure filter is still applied on right column during the transition.
			// verify there are 4 rows
			Assert.AreEqual(4, _browseViewerForTests.AllItems.Count);
			Assert.AreEqual("Grammatical Category", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
			Assert.AreEqual(4, _browseViewerForTests.AllItems.Count);
		}

		[Test]
		public void ChooseLabel()
		{
			// Setup test
			AddOneVariantEachToHimbiliraSenseAndPusEntry();
			// LT-9940 Bulk Edit List Choice tab, the "Choose..." button loses its label.
			_bulkEditBarForTests.PersistSettings = true;
			_bulkEditBarForTests.SwitchTab("ListChoice");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			_browseViewerForTests.ShowColumn("VariantEntryTypesBrowse");
			using (var fsFilter = _browseViewerForTests.SetFilter("Variant Types", "Non-blanks", ""))
			{
				_browseViewerForTests.SetSort("Lexeme Form");
				Assert.AreEqual(2, _browseViewerForTests.AllItems.Count);
				// TargetField == Complex or Variant Entry References (e.g. "Variant Types")
				_bulkEditBarForTests.SetTargetField("Variant Types");
				Assert.AreEqual("Variant Types", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
				// verify there are 2 rows
				Assert.AreEqual(2, _browseViewerForTests.AllItems.Count);
				Assert.AreEqual("Choose...", _bulkEditBarForTests.CurrentBulkEditSpecControl.Control.Text);
				Assert.AreEqual("Variant Types", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
				Assert.AreEqual(2, _browseViewerForTests.AllItems.Count);
				Assert.AreEqual("Choose...", _bulkEditBarForTests.CurrentBulkEditSpecControl.Control.Text);
			}
		}

		private List<ILexEntry> AddOneVariantEachToHimbiliraSenseAndPusEntry()
		{
			var result = new List<ILexEntry>();
			var le1 = Cache.LangProject.LexDbOA.Entries.FirstOrDefault(e => e.HeadWord.Text == "pus");
			var rootMT = GetMorphTypeOrCreateOne("root");
			var dialVar = GetVariantTypeOrCreateOne("Dialectal Variant");
			var nounPOS = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				result.Add(AddVariantLexeme(_createdObjectList, le1, "pusa", rootMT, "greenish", nounPOS, dialVar));
			});
			result.Add(AddOneVariantToHimbiliraSense());
			return result;
		}

		private ILexEntry AddOneVariantToHimbiliraSense()
		{
			ILexEntry result = null;
			var entry = Cache.LangProject.LexDbOA.Entries.First(e => e.HeadWord.Text == "*himbilira");
			var rootMT = GetMorphTypeOrCreateOne("root");
			var spellVar = GetVariantTypeOrCreateOne("Spelling Variant");
			var verbPOS = GetGrammaticalCategoryOrCreateOne("verb", Cache.LangProject.PartsOfSpeechOA);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				result = AddVariantLexeme(_createdObjectList, entry.SensesOS[1], "rimbolira", rootMT, "to.understand", verbPOS, spellVar);
			});
			return result;
		}

		private int GetClassOfObject(int hvo)
		{
			return Cache.ServiceLocator.GetObject(hvo).ClassID;
		}

		[Test]
		public void ListChoiceTargetSelection()
		{
			//MessageBox.Show("Debug ListChoiceTargetSelection");
			_bulkEditBarForTests.SwitchTab("ListChoice");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			using (_browseViewerForTests.SetFilter("Lexeme Form", "Filter for...", "underlying form"))
			{
				_browseViewerForTests.SetSort("Lexeme Form");
				Assert.AreEqual(1, _browseViewerForTests.AllItems.Count);
				// Make sure we have the expected target fields
				var targetFields = _bulkEditBarForTests.GetTargetFields();
				Assert.AreEqual(2, targetFields.Count);
				Assert.AreEqual("Morph Type", targetFields[0].ToString());
				Assert.AreEqual("Grammatical Category", targetFields[1].ToString());
				// TargetField == Entry (e.g. "Morph Type")
				_bulkEditBarForTests.SetTargetField("Morph Type");
				Assert.AreEqual("Morph Type", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
				// make sure current record is an Entry
				var hvoOfCurrentEntry = _browseViewerForTests.AllItems[_browseViewerForTests.SelectedIndex];
				Assert.AreEqual(LexEntryTags.kClassId, GetClassOfObject(hvoOfCurrentEntry));
				// verify there is still only 1 row.
				Assert.AreEqual(1, _browseViewerForTests.AllItems.Count);
				// Set sorter on a sense field and make sure unchecking one entry unchecks them all
				_browseViewerForTests.SetSort("Grammatical Category");
				var numOfEntryRows = _browseViewerForTests.AllItems.Count;
				// we expect to have more than one Entry rows when sorted on a sense field
				Assert.Less(1, numOfEntryRows);
				Assert.AreEqual(numOfEntryRows, _browseViewerForTests.CheckedItems.Count);	// all checked.
				// check current item, should check all rows.
				_browseViewerForTests.SetCheckedItems(new List<int>());	// uncheck all rows.
				Assert.AreEqual(0, _browseViewerForTests.CheckedItems.Count);
				_browseViewerForTests.SetCheckedItems(new List<int>(new[] { hvoOfCurrentEntry }));
				Assert.AreEqual(numOfEntryRows, _browseViewerForTests.CheckedItems.Count);
				// TargetField == Sense (e.g. "Grammatical Category")
				_bulkEditBarForTests.SetTargetField("Grammatical Category");
				Assert.AreEqual("Grammatical Category", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
				// make sure current record is a Sense
				var hvoOfCurrentSense = _browseViewerForTests.AllItems[_browseViewerForTests.SelectedIndex];
				Assert.AreEqual(LexSenseTags.kClassId, GetClassOfObject(hvoOfCurrentSense));
				// Make sure filter is still applied on right column during the transition.
				// verify there are 4 rows
				Assert.AreEqual(4, _browseViewerForTests.AllItems.Count);
				// make sure checking only one sense should only check one row.
				_browseViewerForTests.SetCheckedItems(new List<int>());	// uncheck all rows.
				_browseViewerForTests.SetCheckedItems(new List<int>(new[] { hvoOfCurrentSense }));
				Assert.AreEqual(1, _browseViewerForTests.CheckedItems.Count);
				// take off the filter and make sure switching between Senses/Entries maintains a selection
				// in the ownership tree.
				_browseViewerForTests.SetFilter("Lexeme Form", "Show All", null);
				hvoOfCurrentSense = _browseViewerForTests.AllItems[_browseViewerForTests.SelectedIndex];
				// now switch back to Entry level
				_bulkEditBarForTests.SetTargetField("Morph Type");
				hvoOfCurrentEntry = _browseViewerForTests.AllItems[_browseViewerForTests.SelectedIndex];
				Assert.AreEqual(LexEntryTags.kClassId, GetClassOfObject(hvoOfCurrentEntry));
				// make sure this entry owns the Sense we were on.
				Assert.AreEqual(hvoOfCurrentEntry, Cache.ServiceLocator.GetObject(hvoOfCurrentSense).OwnerOfClass<ILexEntry>().Hvo);
			}
		}

		[Test]
		public void ListChoiceTargetSemDomSuggest()
		{
			// Add some Semantic Domains
			var semDomDict = AddSemanticDomains(_browseViewerForTests.Cache);
			var oilSemDom = semDomDict["oil"];
			var greenSemDom = semDomDict["green"];
			var subsenseSemDom = semDomDict["subsense"];
			ILexSense green, see, understand, english1, subsense1, subsense2; // 'out' values
			GrabSensesWeNeed(out green, out see, out understand, out subsense1, out subsense2, out english1);
			// give 'understand' a pre-existing 'oil' semantic domain
			AddSemanticDomainToSense(understand, oilSemDom);
			_bulkEditBarForTests.SwitchTab("ListChoice");
			_browseViewerForTests.ShowColumn("DomainsOfSensesForSense");
			Assert.AreEqual(3, _browseViewerForTests.AllItems.Count);
			// Make sure we have the expected target fields
			var targetFields = _bulkEditBarForTests.GetTargetFields();
			Assert.AreEqual(3, targetFields.Count);
			Assert.AreEqual("Morph Type", targetFields[0].ToString());
			Assert.AreEqual("Grammatical Category", targetFields[1].ToString());
			Assert.AreEqual("Semantic Domains", targetFields[2].ToString());
			// TargetField == Sense (e.g. "Semantic Domains")
			using (_bulkEditBarForTests.SetTargetField("Semantic Domains"))
			{
				Assert.AreEqual("Semantic Domains", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
				// make sure current record is an Sense
				var hvoOfCurrentSense = _browseViewerForTests.AllItems[_browseViewerForTests.SelectedIndex];
				Assert.AreEqual(LexSenseTags.kClassId, GetClassOfObject(hvoOfCurrentSense));
				// verify there are now 7 rows.
				Assert.AreEqual(7, _browseViewerForTests.AllItems.Count);
				// make sure checking only one sense should only check one row.
				_browseViewerForTests.SetCheckedItems(new List<int>()); // uncheck all rows.
				_browseViewerForTests.SetCheckedItems(new List<int>(new[] {hvoOfCurrentSense}));
				Assert.AreEqual(1, _browseViewerForTests.CheckedItems.Count);
				// Set all items to be checked (so ClickApply works on all of them)
				_browseViewerForTests.SetCheckedItems(_browseViewerForTests.AllItems);
				_bulkEditBarForTests.ClickSuggest(); // make sure we don't crash clicking Suggest button
				_bulkEditBarForTests.ClickApply();
				// Verify that clicking Apply adds "semantic domains" to any entries
				// whose glosses match something in the domain name (and that it doesn't for others)
				Assert.AreEqual(greenSemDom, green.SemanticDomainsRC.FirstOrDefault(), "'green' should have gotten a matching domain");
				Assert.AreEqual(oilSemDom, understand.SemanticDomainsRC.FirstOrDefault(), "'to.understand' should still have its pre-existing domain");
				Assert.AreEqual(0, see.SemanticDomainsRC.Count, "'to.see' should not have gotten a domain");
				Assert.AreEqual(0, english1.SemanticDomainsRC.Count, "'English gloss' should not have gotten a domain");
				Assert.AreEqual(subsenseSemDom, subsense1.SemanticDomainsRC.FirstOrDefault(), "'English subsense gloss1.1' should have gotten a matching domain");
				Assert.AreEqual(subsenseSemDom, subsense2.SemanticDomainsRC.FirstOrDefault(), "'English subsense gloss1.2' should have gotten a matching domain");
			}
		}

		private void AddSemanticDomainToSense(ILexSense understand, ICmSemanticDomain oilSemDom)
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => understand.SemanticDomainsRC.Add(oilSemDom));
		}

		private void GrabSensesWeNeed(out ILexSense green, out ILexSense see, out ILexSense understand,
			out ILexSense subsense1, out ILexSense subsense2, out ILexSense english1)
		{
			// kinda a "hackish" way to do this, but it gets the job done
			green = see = understand = subsense1 = subsense2 = english1 = null;
			var lexSenses = Cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances();
			foreach (var sense in lexSenses)
			{
				switch (sense.Gloss.AnalysisDefaultWritingSystem.Text)
				{
					case "green":
						green = sense;
						break;
					case "to.see":
						see = sense;
						break;
					case "to.understand":
						understand = sense;
						break;
					case "English subsense gloss1.1":
						subsense1 = sense;
						break;
					case "English subsense gloss1.2":
						subsense2 = sense;
						break;
					case "English gloss":
						english1 = sense;
						break;
				}
			}
		}

		private Dictionary<string, ICmSemanticDomain> AddSemanticDomains(LcmCache cache)
		{
			var semDomFact = cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
			var semDomList = cache.LangProject.SemanticDomainListOA;
			// These aren't very "semantic domain-ish", but they match the gloss of test words
			var domainWordsToCreate = new[] {"green", "subsense", "oil"};
			var result = new Dictionary<string, ICmSemanticDomain>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				result = domainWordsToCreate.ToDictionary(domainWord => domainWord, domainWord => MakeNewSemDom(semDomFact, semDomList, domainWord));
			});
			return result;
		}

		private ICmSemanticDomain MakeNewSemDom(ICmSemanticDomainFactory semDomFact, ICmPossibilityList semDomList, string domainWord)
		{
			var newDomain = semDomFact.Create();
			semDomList.PossibilitiesOS.Add(newDomain);
			newDomain.Name.SetAnalysisDefaultWritingSystem(domainWord);
			_createdObjectList.Add(newDomain);
			return newDomain;
		}

		[Test]
		public void BulkCopyTargetSelection()
		{
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			var fsFilter = _browseViewerForTests.SetFilter("Lexeme Form", "Filter for...", "underlying form"); // 'underlying form'
			_browseViewerForTests.SetSort("Lexeme Form");
			Assert.AreEqual(1, _browseViewerForTests.AllItems.Count);
			// Make sure we have the expected target fields
			var targetFields = _bulkEditBarForTests.GetTargetFields();
			Assert.AreEqual(4, targetFields.Count);
			Assert.AreEqual("Lexeme Form", targetFields[0].ToString());
			Assert.AreEqual("Citation Form", targetFields[1].ToString());
			Assert.AreEqual("Glosses", targetFields[2].ToString());
			Assert.AreEqual("Definition", targetFields[3].ToString());
			// TargetField == Entry
			_bulkEditBarForTests.SetTargetField("Citation Form");
			// make sure current record is an Entry
			var hvoOfCurrentEntry = _browseViewerForTests.AllItems[_browseViewerForTests.SelectedIndex];
			Assert.AreEqual(LexEntryTags.kClassId, GetClassOfObject(hvoOfCurrentEntry));
			// verify there is still only 1 row.
			Assert.AreEqual(1, _browseViewerForTests.AllItems.Count);
			// TargetField == Sense
			_bulkEditBarForTests.SetTargetField("Glosses");
			// make sure current record is a Sense
			int hvoOfCurrentSense = _browseViewerForTests.AllItems[_browseViewerForTests.SelectedIndex];
			Assert.AreEqual(LexSenseTags.kClassId, GetClassOfObject(hvoOfCurrentSense));
			// Make sure filter is still applied on right column during the transition.
			// verify there are 4 rows
			Assert.AreEqual(4, _browseViewerForTests.AllItems.Count);
		}

		[Test]
		public void DeleteTargetSelection()
		{
			_bulkEditBarForTests.SwitchTab("Delete");
			// first apply a filter on Lexeme Form for 'underlying form' to limit browse view to one Entry.
			_browseViewerForTests.SetFilter("Lexeme Form", "Filter for...", "underlying form");
			_browseViewerForTests.SetSort("Lexeme Form");
			Assert.AreEqual(1, _browseViewerForTests.AllItems.Count);
			// Make sure we have the expected target fields
			var targetFields = _bulkEditBarForTests.GetTargetFields();
			Assert.AreEqual(7, targetFields.Count);
			Assert.AreEqual("Lexeme Form", targetFields[0].ToString());
			Assert.AreEqual("Citation Form", targetFields[1].ToString());
			Assert.AreEqual("Glosses", targetFields[2].ToString());
			Assert.AreEqual("Definition", targetFields[3].ToString());
			Assert.AreEqual("Grammatical Category", targetFields[4].ToString());
			Assert.AreEqual("Entries (Rows)", targetFields[5].ToString());
			Assert.AreEqual("Senses (Rows)", targetFields[6].ToString());
			// TargetField == Sense
			_bulkEditBarForTests.SetTargetField("Senses (Rows)");
			// make sure current record is a Sense
			int hvoOfCurrentSense = _browseViewerForTests.AllItems[_browseViewerForTests.SelectedIndex];
			Assert.AreEqual(LexSenseTags.kClassId, GetClassOfObject(hvoOfCurrentSense));
			// Make sure filter is still applied on right column during the transition.
			// verify there are 4 rows
			Assert.AreEqual(4, _browseViewerForTests.AllItems.Count);
			// TargetField == Entry
			_bulkEditBarForTests.SetTargetField("Entries (Rows)");
			// make sure current record is an Entry
			var hvoOfCurrentEntry = _browseViewerForTests.AllItems[_browseViewerForTests.SelectedIndex];
			Assert.AreEqual(LexEntryTags.kClassId, GetClassOfObject(hvoOfCurrentEntry));
			// verify there is still only 1 row.
			Assert.AreEqual(1, _browseViewerForTests.AllItems.Count);
			_browseViewerForTests.ShowColumn("VariantEntryTypesBrowse");
			targetFields = _bulkEditBarForTests.GetTargetFields();
			Assert.AreEqual(9, targetFields.Count);
			Assert.AreEqual("Variant Types", targetFields[5].ToString());
			Assert.AreEqual("Complex or Variant Entry References (Rows)", targetFields[8].ToString());
		}

		/// <summary>
		/// (LT8958) List choice: Locations
		/// </summary>
		[Test]
		public void Pronunciations_ListChoice_Locations()
		{
			_bulkEditBarForTests.PersistSettings = true;
			// setup data.
			AddPronunciation();
			AddTwoLocations();
			ILexPronunciation firstPronunciation;
			ILexEntry firstEntryWithPronunciation;
			ILexEntry firstEntryWithoutPronunciation;
			List<ILexEntry> entriesWithoutPronunciations;
			List<ILexPronunciation> pronunciations;
			SetupPronunciationData(out firstPronunciation,
				out firstEntryWithPronunciation,
				out firstEntryWithoutPronunciation,
				out entriesWithoutPronunciations,
				out pronunciations);
			var recordList = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			// SUT
			// first select an entry with a pronunciation, and see if we move to that entry's pronunciation
			// when we switch to pronunciations list.
			recordList.JumpToRecord(firstEntryWithPronunciation.Hvo);
			Assert.AreEqual(firstEntryWithPronunciation.Hvo, recordList.CurrentObject.Hvo);
			// make sure we're not on the first index, since when we switch to pronunciations,
			// we want to make sure there is logic in place for keeping the index on a child pronunciation of this entry.
			Assert.Less(0, recordList.CurrentIndex);
			_bulkEditBarForTests.SwitchTab("ListChoice");
			var cOriginal = _browseViewerForTests.ColumnSpecs.Count;
			// add column for Pronunciation Location
			_browseViewerForTests.ShowColumn("Location");
			// make sure column got added.
			Assert.AreEqual(cOriginal + 1, _browseViewerForTests.ColumnSpecs.Count);
			_bulkEditBarForTests.SetTargetField("Pronunciation-Location");
			Assert.AreEqual("Pronunciation-Location", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
			// check number of options and first is "jungle" (or Empty?)
			var listChoiceControl = _bulkEditBarForTests.GetTabControlChild("m_listChoiceControl") as FwComboBox;
			Assert.IsNotNull(listChoiceControl);
			// expect to have some options.
			Assert.Less(2, listChoiceControl.Items.Count);
			// expect the first option to be of class CmLocation
			var item = listChoiceControl.Items[0] as HvoTssComboItem;
			Assert.AreEqual(CmLocationTags.kClassId, GetClassOfObject(item.Hvo));
			// check browse view class changed to LexPronunciation
			Assert.AreEqual(LexPronunciationTags.kClassId, _browseViewerForTests.ListItemsClass);
			// check that clerk list has also changed.
			Assert.AreEqual(LexPronunciationTags.kClassId, _browseViewerForTests.SortItemProvider.ListItemsClass);
			// make sure the list size includes all pronunciations, and all entries that don't have pronunciations.
			Assert.AreEqual(recordList.ListSize, pronunciations.Count + entriesWithoutPronunciations.Count);
			// make sure we're on the pronunciation of the entry we changed from
			Assert.AreEqual(firstPronunciation.Hvo, recordList.CurrentObject.Hvo);
			// change the first pronunciation's (non-existing) location to something else
			Assert.AreEqual(null, firstPronunciation.LocationRA);
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new int[] { firstPronunciation.Hvo }));
			// set list choice to the first location (eg. 'jungle')
			listChoiceControl.SelectedItem = item;
			var cPronunciations = firstEntryWithPronunciation.PronunciationsOS.Count;
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			// make sure we changed the list option and didn't add another separate pronunciation.
			Assert.AreEqual(item.Hvo, firstPronunciation.LocationRA.Hvo);
			Assert.AreEqual(cPronunciations, firstEntryWithPronunciation.PronunciationsOS.Count);
			Assert.AreEqual(recordList.ListSize, pronunciations.Count + entriesWithoutPronunciations.Count);
			// now create a new pronunciation on an entry that does not have one.
			cPronunciations = firstEntryWithoutPronunciation.PronunciationsOS.Count;
			Assert.AreEqual(0, cPronunciations);
			recordList.JumpToRecord(firstEntryWithoutPronunciation.Hvo);
			Assert.AreEqual(firstEntryWithoutPronunciation.Hvo, recordList.CurrentObject.Hvo);
			var currentIndex = recordList.CurrentIndex;
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new[] { firstEntryWithoutPronunciation.Hvo }));
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			// check that current index has remained the same.
			Assert.AreEqual(currentIndex, recordList.CurrentIndex);
			// but current object (entry) still does not have a Pronunciation
			Assert.AreEqual(0, firstEntryWithoutPronunciation.PronunciationsOS.Count);
			// now change the location to something else, and make sure we still didn't create a pronunciation.
			var item2 = listChoiceControl.Items[1] as HvoTssComboItem;
			listChoiceControl.SelectedItem = item2;
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			Assert.AreEqual(0, firstEntryWithoutPronunciation.PronunciationsOS.Count);
			Assert.AreEqual(recordList.ListSize, pronunciations.Count + entriesWithoutPronunciations.Count);
			recordList = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			Assert.AreEqual(firstEntryWithoutPronunciation.Hvo, recordList.CurrentObject.Hvo);
			// also make sure the total count of the list has not changed.
			// we only converted an entry (ghost) to pronunciation.
			Assert.AreEqual(recordList.ListSize, pronunciations.Count + entriesWithoutPronunciations.Count);
		}

		private void AddTwoLocations()
		{
			var locList = Cache.LangProject.LocationsOA;
			if (locList.PossibilitiesOS.Count > 0)
			{
				return; // already created by previous test
			}
			var analWs = Cache.DefaultAnalWs;
			var locFact = Cache.ServiceLocator.GetInstance<ICmLocationFactory>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var firstLoc = locFact.Create();
				locList.PossibilitiesOS.Add(firstLoc);
				var secondLoc = locFact.Create();
				locList.PossibilitiesOS.Add(secondLoc);
				firstLoc.Name.set_String(analWs, "jungle");
				secondLoc.Name.set_String(analWs, "desert");
			});
		}

		private void AddPronunciation()
		{
			var entry = Cache.LangProject.LexDbOA.Entries.FirstOrDefault(e => e.HeadWord.Text == "pus");
			AddPronunciation(entry);
		}

		private void AddPronunciation(ILexEntry entry)
		{
			var pronuncFact = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var pronunciation = pronuncFact.Create();
				entry.PronunciationsOS.Add(pronunciation);
				pronunciation.Form.set_String(Cache.DefaultVernWs, "Pronunciation");
				Console.WriteLine($"*** We just added a pronunciation called 'Pronunciation' to entry {entry.HeadWord.Text}.");
			});
		}

		private void SetupPronunciationData(out ILexPronunciation firstPronunciation, out ILexEntry firstEntryWithPronunciation, out ILexEntry firstEntryWithoutPronunciation, out List<ILexEntry> entriesWithoutPronunciations, out List<ILexPronunciation> pronunciations)
		{
			firstPronunciation = null;
			firstEntryWithPronunciation = null;
			firstEntryWithoutPronunciation = null;
			entriesWithoutPronunciations = new List<ILexEntry>();
			pronunciations = new List<ILexPronunciation>();
			// find an entry with pronunciations.
			foreach (var entry in Cache.LangProject.LexDbOA.Entries)
			{
				if (entry.PronunciationsOS.Count > 0)
				{
					pronunciations.AddRange(entry.PronunciationsOS);
					if (firstPronunciation == null)
					{
						firstEntryWithPronunciation = entry;
						firstPronunciation = entry.PronunciationsOS[0];
						var newPronunciation = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
						pronunciations.Add(newPronunciation);
						NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => entry.PronunciationsOS.Add(newPronunciation));
					}
				}
				else
				{
					entriesWithoutPronunciations.Add(entry);
					if (firstEntryWithoutPronunciation == null)
					{
						firstEntryWithoutPronunciation = entry;
					}
				}
			}
		}

		/// <summary>
		/// (LT8958) Bulk Copy/Click Copy/Bulk Replace/Process: Pronunciation(Form), CV Pattern, Tone
		/// </summary>
		[Test]
		public void Pronunciations_StringFields_Multilingual()
		{
			// Setup data
			AddPronunciation();
			ILexPronunciation firstPronunciation;
			ILexEntry firstEntryWithPronunciation;
			ILexEntry firstEntryWithoutPronunciation;
			List<ILexEntry> entriesWithoutPronunciations;
			List<ILexPronunciation> pronunciations;
			SetupPronunciationData(out firstPronunciation,
				out firstEntryWithPronunciation,
				out firstEntryWithoutPronunciation,
				out entriesWithoutPronunciations,
				out pronunciations);
			// do a bulk copy from LexemeForm to Pronunciations
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_browseViewerForTests.ShowColumn("Pronunciation");
			_browseViewerForTests.ShowColumn("CVPattern");
			_browseViewerForTests.ShowColumn("Tone");
			var bulkCopySourceCombo = _bulkEditBarForTests.GetTabControlChild("m_bulkCopySourceCombo") as FwOverrideComboBox;
			var bcNonEmptyTargetControl = _bulkEditBarForTests.GetTabControlChild("m_bcNonEmptyTargetControl") as NonEmptyTargetControl;
			// set to overwrite
			bcNonEmptyTargetControl.NonEmptyMode = NonEmptyTargetOptions.Overwrite;
			bulkCopySourceCombo.Text = "Lexeme Form";
			// first bulk copy the "Pronunciations" field, which is a multilingual field
			_bulkEditBarForTests.SetTargetField("Pronunciations");
			// first bulk copy into an existing pronunciation
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new[] { firstPronunciation.Hvo }));
			Assert.AreEqual(firstPronunciation.Form.VernacularDefaultWritingSystem.Text, "Pronunciation");
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			var lexemeForm = firstEntryWithPronunciation.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text;
			Assert.AreEqual(lexemeForm, firstPronunciation.Form.VernacularDefaultWritingSystem.Text);
			// next bulk copy into an empty (ghost) pronunciation
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new[] { firstEntryWithoutPronunciation.Hvo }));
			lexemeForm = firstEntryWithoutPronunciation.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text;
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			Assert.AreEqual(1, firstEntryWithoutPronunciation.PronunciationsOS.Count);
			Assert.AreEqual(lexemeForm, firstEntryWithoutPronunciation.PronunciationsOS[0].Form.VernacularDefaultWritingSystem.Text);
		}

		/// <summary>
		/// (LT8958) Bulk Copy/Click Copy/Bulk Replace/Process: Pronunciation(Form), CV Pattern, Tone
		/// </summary>
		[Test]
		public void Pronunciations_StringFields_SimpleString()
		{
			// Setup data.
			AddPronunciation();
			ILexPronunciation firstPronunciation;
			ILexEntry firstEntryWithPronunciation;
			ILexEntry firstEntryWithoutPronunciation;
			List<ILexEntry> entriesWithoutPronunciations;
			List<ILexPronunciation> pronunciations;
			SetupPronunciationData(out firstPronunciation,
				out firstEntryWithPronunciation,
				out firstEntryWithoutPronunciation,
				out entriesWithoutPronunciations,
				out pronunciations);
			// do a bulk copy from LexemeForm to Pronunciations
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_browseViewerForTests.ShowColumn("Pronunciation");
			_browseViewerForTests.ShowColumn("CVPattern");
			_browseViewerForTests.ShowColumn("Tone");
			var bulkCopySourceCombo = _bulkEditBarForTests.GetTabControlChild("m_bulkCopySourceCombo") as FwOverrideComboBox;
			var bcNonEmptyTargetControl = _bulkEditBarForTests.GetTabControlChild("m_bcNonEmptyTargetControl") as NonEmptyTargetControl;
			// set to overwrite
			bcNonEmptyTargetControl.NonEmptyMode = NonEmptyTargetOptions.Overwrite;
			bulkCopySourceCombo.Text = "Lexeme Form";
			// first bulk copy the "Pronunciations" field, which is a multilingual field
			_bulkEditBarForTests.SetTargetField("Tones");
			// first bulk copy into an existing pronunciation
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new[] { firstPronunciation.Hvo }));
			Assert.AreEqual(firstPronunciation.Tone.Text, null);
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			var lexemeForm = firstEntryWithPronunciation.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text;
			Assert.AreEqual(lexemeForm, firstPronunciation.Tone.Text);
			// next bulk copy into an empty (ghost) pronunciation
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new[] { firstEntryWithoutPronunciation.Hvo }));
			lexemeForm = firstEntryWithoutPronunciation.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text;
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			Assert.AreEqual(1, firstEntryWithoutPronunciation.PronunciationsOS.Count);
			Assert.AreEqual(lexemeForm, firstEntryWithoutPronunciation.PronunciationsOS[0].Tone.Text);
		}

		/// <summary>
		/// (LT-13041) Bulk Copy to a Complex Form Comment field
		/// </summary>
		[Test]
		public void ComplexForm_BulkCopy_Comment()
		{
			// Setup data.
			var pusEntry = Cache.LangProject.LexDbOA.Entries.FirstOrDefault(e => e.HeadWord.Text == "pus");
			var complexEntry = AddOneComplexEntry(pusEntry);
			var complexEntryRef = complexEntry.EntryRefsOS[0];
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				complexEntry.Comment.SetAnalysisDefaultWritingSystem("Complex Form note");
				complexEntryRef.Summary.SetAnalysisDefaultWritingSystem("existing comment");
			});
			// do a bulk copy from LexemeForm to Pronunciations
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_browseViewerForTests.ShowColumn("NoteForEntry");
			_browseViewerForTests.ShowColumn("ComplexFormsSummaryPub");
			var bulkCopySourceCombo = _bulkEditBarForTests.GetTabControlChild("m_bulkCopySourceCombo") as FwOverrideComboBox;
			var bcNonEmptyTargetControl = _bulkEditBarForTests.GetTabControlChild("m_bcNonEmptyTargetControl") as NonEmptyTargetControl;
			// set to overwrite
			bcNonEmptyTargetControl.NonEmptyMode = NonEmptyTargetOptions.Overwrite;
			bulkCopySourceCombo.Text = "Note";
			// first try to bulk copy the "Note" field to the Complex Form Comment field, which is a multilingual field
			using (_bulkEditBarForTests.SetTargetField("Complex Form Comment"))
			{
				// try bulk copy into an existing Comment
				_browseViewerForTests.OnUncheckAll();
				_browseViewerForTests.SetCheckedItems(new List<int>(new[] {complexEntryRef.Hvo}));
				Assert.AreEqual("existing comment", complexEntryRef.Summary.AnalysisDefaultWritingSystem.Text);
				_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
				_bulkEditBarForTests.ClickApply();
				var result = complexEntry.EntryRefsOS[0].Summary.AnalysisDefaultWritingSystem.Text;
				Assert.AreEqual("Complex Form note", result);
			}
		}

		/// <summary>
		/// (LT-13041) Bulk Copy to a Variant Comment field
		/// </summary>
		[Test]
		public void Variant_BulkCopy_Comment()
		{
			// Setup data.
			var variantEntry = AddOneVariantToHimbiliraSense();
			var variantEntryRef = variantEntry.EntryRefsOS[0];
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				variantEntry.Comment.SetAnalysisDefaultWritingSystem("Variant note");
			});
			// do a bulk copy from Entry-level Note to Variant Comment
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_browseViewerForTests.ShowColumn("NoteForEntry");
			_browseViewerForTests.ShowColumn("VariantsSummaryPub");
			var bulkCopySourceCombo = _bulkEditBarForTests.GetTabControlChild("m_bulkCopySourceCombo") as FwOverrideComboBox;
			var bcNonEmptyTargetControl = _bulkEditBarForTests.GetTabControlChild("m_bcNonEmptyTargetControl") as NonEmptyTargetControl;
			// try bulk copy into an empty Comment
			bcNonEmptyTargetControl.NonEmptyMode = NonEmptyTargetOptions.DoNothing;
			bulkCopySourceCombo.Text = "Note";
			// try to bulk copy the "Note" field to the Variant Comment field, which is a multilingual field
			using (_bulkEditBarForTests.SetTargetField("Variant Comment"))
			{
				_browseViewerForTests.OnUncheckAll();
				_browseViewerForTests.SetCheckedItems(new List<int>(new[] {variantEntryRef.Hvo}));
				Assert.IsNullOrEmpty(variantEntryRef.Summary.AnalysisDefaultWritingSystem.Text);
				_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
				_bulkEditBarForTests.ClickApply();
				var result = variantEntry.EntryRefsOS[0].Summary.AnalysisDefaultWritingSystem.Text;
				Assert.AreEqual("Variant note", result);
			}
		}

		private void SetupAllomorphsData(out IMoForm firstAllomorphOut,
			out ILexEntry firstEntryWithAllomorphOut,
			out ILexEntry firstEntryWithoutAllomorphOut,
			out List<ILexEntry> entriesWithoutAllomorphsOut,
			out List<IMoForm> allomorphsOut)
		{
			IMoForm firstAllomorph = null;
			ILexEntry firstEntryWithAllomorph = null;
			ILexEntry firstEntryWithoutAllomorph = null;
			var entriesWithoutAllomorphs = new List<ILexEntry>();
			var allomorphs = new List<IMoForm>();
			UndoableUnitOfWorkHelper.Do("SetupAllomorphsData", "SetupAllomorphsData", Cache.ActionHandlerAccessor, () =>
			{
				// find an entry with allomorphs.
				foreach (var entry in Cache.LangProject.LexDbOA.Entries)
				{
					if (entry.AlternateFormsOS.Count > 0)
					{
						allomorphs.AddRange(entry.AlternateFormsOS);
						if (firstAllomorph == null)
						{
							firstEntryWithAllomorph = entry;
							firstAllomorph = entry.AlternateFormsOS[0];
							var newAllomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
							entry.AlternateFormsOS.Add(newAllomorph);
							allomorphs.Add(newAllomorph);
						}
					}
					else
					{
						entriesWithoutAllomorphs.Add(entry);
						if (firstEntryWithoutAllomorph == null)
						{
							firstEntryWithoutAllomorph = entry;
						}
					}
				}
			});
			firstAllomorphOut = firstAllomorph;
			firstEntryWithAllomorphOut = firstEntryWithAllomorph;
			firstEntryWithoutAllomorphOut = firstEntryWithoutAllomorph;
			entriesWithoutAllomorphsOut = entriesWithoutAllomorphs;
			allomorphsOut = allomorphs;
		}

		/// <summary>
		/// LT-4268 Bulk Edit Allomorphs, list choice for IsAbstractForm (boolean field)
		/// </summary>
		[Test]
		public void Allomorphs_IsAbstractForm()
		{
			_bulkEditBarForTests.PersistSettings = true;
			// setup data.
			IMoForm firstAllomorph;
			ILexEntry firstEntryWithAllomorph;
			ILexEntry firstEntryWithoutAllomorph;
			List<ILexEntry> entriesWithoutAllomorphs;
			List<IMoForm> allomorphs;
			SetupAllomorphsData(out firstAllomorph,
				out firstEntryWithAllomorph,
				out firstEntryWithoutAllomorph,
				out entriesWithoutAllomorphs,
				out allomorphs);
			var recordlist = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			// first select an entry with an allomorph , and see if we move to that entry's allomorphs
			// when we switch to "Is Abstract Form (Allomorph)" for target field.
			recordlist.JumpToRecord(firstEntryWithAllomorph.Hvo);
			Assert.AreEqual(firstEntryWithAllomorph.Hvo, recordlist.CurrentObject.Hvo);
			// make sure we're not on the first index, since when we switch to pronunciations,
			// we want to make sure there is logic in place for keeping the index on a child pronunciation of this entry.
			Assert.Less(0, recordlist.CurrentIndex);
			_bulkEditBarForTests.SwitchTab("ListChoice");
			var cOriginal = _browseViewerForTests.ColumnSpecs.Count;
			// add column for "Is Abstract Form (Allomorph)"
			_browseViewerForTests.ShowColumn("IsAbstractFormForAllomorph");
			// make sure column got added.
			Assert.AreEqual(cOriginal + 1, _browseViewerForTests.ColumnSpecs.Count);
			_bulkEditBarForTests.SetTargetField("Is Abstract Form (Allomorph)");
			Assert.AreEqual("Is Abstract Form (Allomorph)", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
			// check number of options and second is "yes"
			var listChoiceControl = _bulkEditBarForTests.GetTabControlChild("m_listChoiceControl") as ComboBox;
			Assert.IsNotNull(listChoiceControl);
			// expect to have some options (yes & no).
			Assert.AreEqual(2, listChoiceControl.Items.Count);
			var item = listChoiceControl.Items[1] as IntComboItem;
			Assert.AreEqual("yes", item.ToString()); // 'yes'
			// check browse view class changed to MoForm
			Assert.AreEqual(MoFormTags.kClassId, _browseViewerForTests.ListItemsClass);
			// check that clerk list has also changed.
			Assert.AreEqual(MoFormTags.kClassId, _browseViewerForTests.SortItemProvider.ListItemsClass);
			// make sure the list size includes all allomorphs, and all entries that don't have allomorphs.
			Assert.AreEqual(recordlist.ListSize, allomorphs.Count + entriesWithoutAllomorphs.Count);
			// make sure we're on the first allomorph of the entry we changed from
			Assert.AreEqual(firstAllomorph.Hvo, recordlist.CurrentObject.Hvo);
			// change the first allomorphs's IsAbstract to something else
			Assert.AreEqual(false, firstAllomorph.IsAbstract);
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new[] { firstAllomorph.Hvo }));
			listChoiceControl.SelectedItem = item; // change to 'yes'
			var cAllomorphs = firstEntryWithAllomorph.AlternateFormsOS.Count;
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			// make sure we changed the list option and didn't add another separate allomorph.
			Assert.AreEqual(Convert.ToBoolean(item.Value), firstAllomorph.IsAbstract);
			Assert.AreEqual(cAllomorphs, firstEntryWithAllomorph.AlternateFormsOS.Count);
			Assert.AreEqual(recordlist.ListSize, allomorphs.Count + entriesWithoutAllomorphs.Count);
			// now try previewing and setting IsAbstract on an entry that does not have an allomorph.
			cAllomorphs = firstEntryWithoutAllomorph.AlternateFormsOS.Count;
			Assert.AreEqual(0, cAllomorphs);
			recordlist.JumpToRecord(firstEntryWithoutAllomorph.Hvo);
			Assert.AreEqual(firstEntryWithoutAllomorph.Hvo, recordlist.CurrentObject.Hvo);
			int currentIndex = recordlist.CurrentIndex;
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new[] { firstEntryWithoutAllomorph.Hvo }));
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			// check that current index has remained the same.
			Assert.AreEqual(currentIndex, recordlist.CurrentIndex);
			// We no longer create allomorphs as a side-effect of setting "Is Abstract Form (Allomorph)"
			Assert.AreEqual(0, firstEntryWithoutAllomorph.AlternateFormsOS.Count);
			// now try changing the (non-existent) IsAbstract to something else, and make sure we didn't
			// create another allomorph.
			var item2 = listChoiceControl.Items[0] as IntComboItem;
			listChoiceControl.SelectedItem = item2;
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			// make sure there still isn't a new allomorph.
			Assert.AreEqual(0, firstEntryWithoutAllomorph.AlternateFormsOS.Count);
			Assert.AreEqual(recordlist.ListSize, allomorphs.Count + entriesWithoutAllomorphs.Count);
			recordlist = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			Assert.AreEqual(firstEntryWithoutAllomorph.Hvo, recordlist.CurrentObject.Hvo);
			// also make sure the total count of the list has not changed.
			Assert.AreEqual(recordlist.ListSize, allomorphs.Count + entriesWithoutAllomorphs.Count);
		}

		/// <summary>
		/// </summary>
		[Test]
		public void EntryRefs_ListChoice_VariantEntryTypes()
		{
			// setup data.
			var variantList = AddOneVariantEachToHimbiliraSenseAndPusEntry();
			var secondVariantRef = variantList[1].EntryRefsOS[0];
			var choiceFreeVariant = GetVariantTypeOrCreateOne("Free Variant");
			// SUT
			_bulkEditBarForTests.SwitchTab("ListChoice");
			var cOriginal = _browseViewerForTests.ColumnSpecs.Count;
			// add column for Pronunciation Location
			_browseViewerForTests.ShowColumn("VariantEntryTypesBrowse");
			// make sure column got added.
			Assert.AreEqual(cOriginal + 1, _browseViewerForTests.ColumnSpecs.Count);
			_bulkEditBarForTests.SetTargetField("Variant Types");
			Assert.AreEqual("Variant Types", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
			var recordlist = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			recordlist.JumpToRecord(secondVariantRef.Hvo);
			Assert.AreEqual(secondVariantRef, recordlist.CurrentObject as ILexEntryRef);
			// make sure we're not on the first index, since when we switch to pronunciations,
			// we want to make sure there is logic in place for keeping the index on a child pronunciation of this entry.
			Assert.Less(0, recordlist.CurrentIndex);
			secondVariantRef = recordlist.CurrentObject as ILexEntryRef;
			var firstVariantRefType = secondVariantRef.VariantEntryTypesRS[0];
			Assert.AreEqual("Spelling Variant", firstVariantRefType.Name.AnalysisDefaultWritingSystem.Text);
			// check number of options
			var listChoiceControl = _bulkEditBarForTests.CurrentBulkEditSpecControl as ComplexListChooserBEditControl;
			Assert.IsNotNull(listChoiceControl);
			// check browse view class changed to LexPronunciation
			Assert.AreEqual(LexEntryRefTags.kClassId, _browseViewerForTests.ListItemsClass);
			// check that clerk list has also changed.
			Assert.AreEqual(LexEntryRefTags.kClassId, _browseViewerForTests.SortItemProvider.ListItemsClass);
			// allow changing an existing variant entry type to something else.
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new[] { secondVariantRef.Hvo }));
			// set list choice to "Free Variant" and Replace mode.
			listChoiceControl.ChosenObjects = new ICmObject[] { choiceFreeVariant };
			listChoiceControl.ReplaceMode = true;
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			// make sure we gave the LexEntryRef the expected type.
			Assert.AreEqual(choiceFreeVariant.Hvo, secondVariantRef.VariantEntryTypesRS[0].Hvo);
			// Now try to add a variant entry type to a complex entry reference,
			// verify nothing changed.
			// Setup data.
			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var entryPus = entryRepo.AllInstances().First();
			var complexEntry = AddOneComplexEntry(entryPus);
			var hvoComplexRef = complexEntry.EntryRefsOS[0].Hvo;
			// SUT (2)
			_browseViewerForTests.ShowColumn("ComplexEntryTypesBrowse");
			// make sure column got added.
			Assert.AreEqual(cOriginal + 2, _browseViewerForTests.ColumnSpecs.Count);
			_bulkEditBarForTests.SetTargetField("Complex Form Types");
			Assert.AreEqual("Complex Form Types", _bulkEditBarForTests.SelectedTargetFieldItem.ToString());
			recordlist.JumpToRecord(hvoComplexRef);
			var complexEntryRef = recordlist.CurrentObject as ILexEntryRef;
			Assert.AreEqual(0, complexEntryRef.VariantEntryTypesRS.Count);
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new[] { hvoComplexRef }));
			// set list choice to "Free Variant" and Replace mode.
			listChoiceControl.ChosenObjects = new ICmObject[] { choiceFreeVariant };
			listChoiceControl.ReplaceMode = true;
			_bulkEditBarForTests.ClickPreview(); // make sure we don't crash clicking preview button.
			_bulkEditBarForTests.ClickApply();
			// make sure we didn't add a variant entry type to the complex entry ref.
			Assert.AreEqual(0, complexEntryRef.VariantEntryTypesRS.Count);
		}

		private ILexEntry AddOneComplexEntry(ILexEntry part)
		{
			ILexEntry result = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				result = AddLexeme(_createdObjectList, "pus compound", GetMorphTypeOrCreateOne("phrase"), "envy", GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA));
				var ler = MakeComplexFormLexEntryRef(result);
				ler.PrimaryLexemesRS.Add(part);
				_createdObjectList.Add(ler);
				_createdObjectList.Add(result);
			});
			return result;
		}

		private ILexEntryRef MakeComplexFormLexEntryRef(ILexEntry ownerEntry)
		{
			var result = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			ownerEntry.EntryRefsOS.Add(result);
			result.RefType = LexEntryRefTags.krtComplexForm;
			return result;
		}

	#endregion BulkEditEntries tests
	}

	/// <summary>
	/// Maintain consistency of checked boxes when switching to target fields owned by different classes. (LT-8986)
	/// </summary>
	[TestFixture]
	internal class BulkEditCheckBoxBehaviorTests : BulkEditBarTestsBase
	{
		/// <summary>
		/// queries the lexical database to find an entry with multiple descendents
		/// </summary>
		/// <returns></returns>
		private ILexEntry CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList()
		{
			ILexPronunciation dummy;
			var ZZZparentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation(out dummy);
			var recordlist = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			recordlist.UpdateList(true);
			return ZZZparentEntry;
		}

		private ILexEntry CreateZZZparentEntryWithMultipleSensesAndPronunciation(out ILexPronunciation pronunciation)
		{
			var formLexEntry = "ZZZparentEntry";
			ILexEntry parentEntry = null;
			ILexPronunciation pronunc;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var clsidForm = 0;
				parentEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm),
					TsStringUtils.MakeString(formLexEntry, Cache.DefaultVernWs), "ZZZparentEntry.sense1", null);
				var parentEntrySense1 = parentEntry.SensesOS[0];
				var parentEntrySense2 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(parentEntry, null, "ZZZparentEntry.sense2");
				pronunc = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
				parentEntry.PronunciationsOS.Add(pronunc);
				pronunc.Form.set_String(Cache.DefaultVernWs, "samplePronunciation");
				_createdObjectList.Add(parentEntry);
				_createdObjectList.Add(parentEntrySense1);
				_createdObjectList.Add(parentEntrySense2);
			});
			pronunciation = parentEntry.PronunciationsOS.Last();
			return parentEntry;
		}

		private IEnumerable<ILexEntry> FindEntriesWithoutSenses()
		{
			return Cache.LangProject.LexDbOA.Entries.Where(e => e.SensesOS.Count == 0).ToList();
		}

		private IDictionary<int, int> GetParentOfClassMap(IList<int> items, int clsidParent)
		{
			IDictionary<int, int> itemToParent = new Dictionary<int, int>();
			foreach (var hvoItem in items)
			{
				var objItem = Cache.ServiceLocator.GetObject(hvoItem);
				var owner = objItem.OwnerOfClass(clsidParent);
				if (owner == null)
				{
					continue;
				}
				var hvoEntry = owner.Hvo;
				itemToParent.Add(hvoItem, hvoEntry);

			}
			return itemToParent;
		}

	#region CheckboxBehavior_LT8986

		/// <summary>
		/// 1. When the view first comes up (has not been displayed since database opened),
		/// all items should be checked;
		/// don't persist set of checked items.
		///
		/// Review:
		///     In the future it may be helpful to hang onto it if they just switched to another view and came back.
		/// </summary>
		[Test]
		public virtual void CheckboxBehavior_AllItemsShouldBeInitiallyCheckedPlusRefreshBehavior()
		{
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_bulkEditBarForTests.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntryTags.kClassId, _browseViewerForTests.ListItemsClass);
			var recordList = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			// check that record list has also changed.
			Assert.AreEqual(recordList.ListSize, _browseViewerForTests.CheckedItems.Count);
			Assert.AreEqual(recordList.ListSize, _browseViewerForTests.CheckedItems.Count);
			// Try again in unchecked state
			_browseViewerForTests.OnUncheckAll();
			Assert.AreEqual(0, _browseViewerForTests.CheckedItems.Count);
			// Verify that Refresh doesn't change current selection state
			Assert.AreEqual(0, _browseViewerForTests.CheckedItems.Count);
		}

		/// <summary>
		/// 2. When a change of filter or similar operation causes new items to be added to the list,
		/// restore any previous selected state (or default to 'selected')
		/// </summary>
		[Test]
		public virtual void CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Selected()
		{
			var ZZZparentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();
			var recordList = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_bulkEditBarForTests.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntryTags.kClassId, _browseViewerForTests.ListItemsClass);
			// select only "ZZZparentEntry" before we filter it out.
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SetCheckedItems(new List<int>(new int[] { ZZZparentEntry.Hvo }));
			Assert.AreEqual(1, _browseViewerForTests.CheckedItems.Count);
			// Filter on "pus" and make sure everything now unselected.
			_browseViewerForTests.SetFilter("Lexeme Form", "Filter for...", "pus");
			Assert.AreEqual(0, _browseViewerForTests.CheckedItems.Count);
			// Broaden the to include everything again, and make sure that
			// our entry is still selected.
			_browseViewerForTests.SetFilter("Lexeme Form", "Show All", null);
			Assert.AreEqual(1, _browseViewerForTests.CheckedItems.Count);
			Assert.AreEqual(ZZZparentEntry.Hvo, _browseViewerForTests.CheckedItems[0]);
		}

		[Test]
		public virtual void CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Unselected()
		{
			var ZZZparentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_bulkEditBarForTests.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntryTags.kClassId, _browseViewerForTests.ListItemsClass);
			var recordList = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			// unselect our test data
			_browseViewerForTests.UnselectItem(ZZZparentEntry.Hvo);
			var unselectedItems = _browseViewerForTests.UncheckedItems();
			Assert.AreEqual(1, unselectedItems.Count);
			Assert.AreEqual(ZZZparentEntry.Hvo, unselectedItems[0]);
			// Filter on "pus" and make sure nothing is unselected.
			_browseViewerForTests.SetFilter("Lexeme Form", "Filter for...", "pus");
			var unselectedItemsAfterFilterPus = _browseViewerForTests.UncheckedItems();
			Assert.AreEqual(0, unselectedItemsAfterFilterPus.Count);
			// Extend our filter and make sure we've restored the thing we had selected.
			_browseViewerForTests.SetFilter("Lexeme Form", "Show All", null);
			var unselectedItemsAfterShowAll = _browseViewerForTests.UncheckedItems();
			Assert.AreEqual(1, unselectedItemsAfterShowAll.Count);
			Assert.AreEqual(ZZZparentEntry.Hvo, unselectedItemsAfterShowAll[0]);
		}

		/// <summary>
		/// 3. When we change the bulk edit field from a class to a "descendent" class
		/// (for example, from Entry to Sense...potentially many rows in the new list for each row in the old list),
		/// check all the items that are descendents of the ones that were checked before
		/// (e.g., all the senses of each checked Entry will get checked).
		/// </summary>
		[Test]
		public virtual void CheckboxBehavior_DescendentItemsShouldInheritSelection_Select()
		{
			// find a lex entry that has multiple senses (i.e. descendents).
			var entryWithMultipleDescendents = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_bulkEditBarForTests.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntryTags.kClassId, _browseViewerForTests.ListItemsClass);
			_browseViewerForTests.OnUncheckAll();
			// select the entry.
			_browseViewerForTests.SetCheckedItems(new List<int>(new [] { entryWithMultipleDescendents.Hvo }));
			using (FilterBehavior.Create(this))
			{
				_bulkEditBarForTests.SetTargetField("Glosses");
			}
			var allSensesForEntry = new HashSet<int>(entryWithMultipleDescendents.AllSenses.Select(s => s.Hvo));
			var checkedItems = new HashSet<int>(_browseViewerForTests.CheckedItems);
			Assert.AreEqual(allSensesForEntry.Count, checkedItems.Count, "Checked items mismatched.");
			Assert.IsTrue(checkedItems.SetEquals(allSensesForEntry), "Checked items mismatched.");
		}

		[Test]
		public virtual void CheckboxBehavior_DescendentItemsShouldInheritSelection_UnSelect()
		{
			// find a lex entry that has multiple senses (i.e. descendents).
			var entryWithMultipleDescendents = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_bulkEditBarForTests.SetTargetField("Lexeme Form");
			Assert.AreEqual(LexEntryTags.kClassId, _browseViewerForTests.ListItemsClass);
			var recordList = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			// unselect the entry.
			_browseViewerForTests.UnselectItem(entryWithMultipleDescendents.Hvo);
			using (FilterBehavior.Create(this))
			{
				_bulkEditBarForTests.SetTargetField("Glosses");
			}
			var allSensesForEntry = new HashSet<int>(entryWithMultipleDescendents.AllSenses.Select(s => s.Hvo));
			var uncheckedItems = new HashSet<int>(_browseViewerForTests.UncheckedItems());
			Assert.AreEqual(allSensesForEntry.Count, uncheckedItems.Count, "Unchecked items mismatched.");
			Assert.IsTrue(uncheckedItems.SetEquals(allSensesForEntry), "Unchecked items mismatched.");
		}

		/// <summary>
		/// 4. When we change the bulk edit field to a "parent" class
		/// (for example, from Sense to Entry...one row in the new list for many in the old),
		/// check any item in the new list where are least one descendent is checked in the old...
		/// for example, an Entry will be checked if any of its senses was checked.
		/// </summary>
		[Test]
		public void CheckboxBehavior_ParentClassesItemsShouldInheritSelection_Selected()
		{
			// find a lex entry that has multiple senses (i.e. descendents).
			var entryWithMultipleDescendents = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();
			// some entries (like variants) don't have senses, so we need to factor those into our results.
			var entriesWithoutSenses = FindEntriesWithoutSenses();
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_bulkEditBarForTests.SetTargetField("Glosses");
			Assert.AreEqual(LexSenseTags.kClassId, _browseViewerForTests.ListItemsClass);
			_browseViewerForTests.OnUncheckAll();
			// select the sense.
			_browseViewerForTests.SetCheckedItems(new int[] { entryWithMultipleDescendents.AllSenses[0].Hvo });
			using (FilterBehavior.Create(this))
			{
				_bulkEditBarForTests.SetTargetField("Lexeme Form");
			}
			var selectedEntries = new HashSet<int> {entryWithMultipleDescendents.Hvo};
			selectedEntries.UnionWith(entriesWithoutSenses.Select(e => e.Hvo));
			var checkedItems = new HashSet<int>(_browseViewerForTests.CheckedItems);
			Assert.AreEqual(selectedEntries.Count, checkedItems.Count, "Checked items mismatched.");
			Assert.IsTrue(checkedItems.SetEquals(selectedEntries), "Checked items mismatched.");
		}

		/// <summary>
		/// parents whose children are all unselected should be unselected.
		/// </summary>
		[Test]
		public void CheckboxBehavior_ParentClassesItemsShouldInheritSelection_UnSelected()
		{
			// find a lex entry that has multiple senses (i.e. descendents).
			var entryWithMultipleDescendents = CreateZZZparentEntryWithMultipleSensesAndPronunciation_AndUpdateList();
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_bulkEditBarForTests.SetTargetField("Glosses");
			Assert.AreEqual(LexSenseTags.kClassId, _browseViewerForTests.ListItemsClass);
			// unselect all the senses belonging to this entry
			_browseViewerForTests.UncheckItems(entryWithMultipleDescendents.AllSenses.Select(s => s.Hvo));
			// switch to the parent list
			using (FilterBehavior.Create(this))
			{
				_bulkEditBarForTests.SetTargetField("Lexeme Form");
			}
			var unselectedEntries = new HashSet<int> {entryWithMultipleDescendents.Hvo};
			var uncheckedItems = new HashSet<int>(_browseViewerForTests.UncheckedItems());
			Assert.AreEqual(unselectedEntries.Count, uncheckedItems.Count, "Unchecked items mismatched.");
			Assert.IsTrue(uncheckedItems.SetEquals(unselectedEntries), "Unchecked items mismatched.");
		}

		/// <summary>
		/// 5. When we change fields to a "sibling" class (for example, from Sense to Pronunciation....many:many relationship),
		/// apply rule 4 to decide which common ancestors should be checked,
		/// then rule 3 to decide which items in the new list should be.
		/// For example, check all the rows for a pronunciation field
		/// that belongs to an entry for which at least one sense was checked.
		/// </summary>
		[Test]
		public void CheckboxBehavior_SiblingClassesItemsShouldInheritSelectionThroughParent_Selected()
		{
			// first create an entry with a pronunciation and some senses.
			ILexPronunciation pronunciation;
			var parentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation(out pronunciation);
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_browseViewerForTests.ShowColumn("Pronunciation");
			_bulkEditBarForTests.SetTargetField("Pronunciations");
			// uncheck everything before we switch to sibling list.
			_browseViewerForTests.OnUncheckAll();
			_browseViewerForTests.SelectItem(pronunciation.Hvo);
			// now switch to (sense) sibling list
			using (FilterBehavior.Create(this))
			{
				_bulkEditBarForTests.SetTargetField("Glosses");
			}
			// validate that only the siblings are selected.
			var hvoSenseSiblings = new HashSet<int>(parentEntry.AllSenses.Select(s => s.Hvo));
			Assert.AreEqual(hvoSenseSiblings.Count, _browseViewerForTests.CheckedItems.Count);
			Assert.IsTrue(hvoSenseSiblings.SetEquals(new HashSet<int>(_browseViewerForTests.CheckedItems)));
		}

		[Test]
		public void CheckboxBehavior_SiblingClassesItemsShouldInheritSelectionThroughParent_UnSelected()
		{
			// first create an entry with a pronunciation and some senses.
			ILexPronunciation pronunciation;
			var parentEntry = CreateZZZparentEntryWithMultipleSensesAndPronunciation(out pronunciation);
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_browseViewerForTests.ShowColumn("Pronunciation");
			_bulkEditBarForTests.SetTargetField("Pronunciations");
			// Unselect one sibling
			_browseViewerForTests.UnselectItem(pronunciation.Hvo);
			// now switch to (sense) sibling list
			using (FilterBehavior.Create(this))
			{
				_bulkEditBarForTests.SetTargetField("Glosses");
			}
			// validate that only the siblings are unselected.
			var hvoSenseSiblings = new HashSet<int>(parentEntry.AllSenses.Select(s => s.Hvo));
			var uncheckedItems = new HashSet<int>(_browseViewerForTests.UncheckedItems());
			Assert.AreEqual(hvoSenseSiblings.Count, uncheckedItems.Count);
			Assert.IsTrue(hvoSenseSiblings.SetEquals(uncheckedItems));
		}

		/// <summary />
		[Test]
		public void CheckboxBehavior_SiblingClassesItemsShouldInheritSelectionThroughParent_UnselectAll()
		{
			// some entries (like variants) don't have senses, so we need to factor those into our results.
			var entriesWithoutSenses = FindEntriesWithoutSenses();
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_browseViewerForTests.ShowColumn("Allomorph");
			_bulkEditBarForTests.SetTargetField("Glosses");
			// Unselect All
			_browseViewerForTests.OnUncheckAll();
			// now switch to allomorphs
			using (FilterBehavior.Create(this))
			{
				_bulkEditBarForTests.SetTargetField("Allomorphs");
			}
			// validate that everything (except variant allomorph?) is still not selected.
			var checkedItems = new HashSet<int>(_browseViewerForTests.CheckedItems);
			var selectedEntries = new HashSet<int>(entriesWithoutSenses.Select(e => e.Hvo));
			Assert.AreEqual(selectedEntries.Count, checkedItems.Count);
		}

		/// <summary>
		/// 6 (2b) When we [move up] two or more levels in the hierarchy (e.g., from Example translations to Entries),
		/// it may be that new items appear in the list (e.g., entries none of whose senses have examples that could have translations).
		/// Following rule 2, these should be checked (if anything is checked?).
		/// NOTE: typically moving up the ownership tree will reduce the number of items, but
		/// "Example translations" is exceptional in that we haven't provided the capability
		/// of ghosting objects that belong to ghostable parents (ie. Examples need to exist before
		/// we can ghost Translations owned by Examples.)
		/// </summary>
		[Test]
		public void CheckboxBehavior_SelectParentsThatWereNotInOwnershipTreeOfChildList()
		{
			_bulkEditBarForTests.SwitchTab("BulkCopy");
			_browseViewerForTests.ShowColumn("ExampleTranslation");
			_bulkEditBarForTests.SetTargetField("Example Translations");
			var recordList = (_browseViewerForTests.Parent as RecordBrowseViewForTests).MyRecordList;
			// having fewer translations than parent entries is strange
			// but it's currently the only way we can allow bulk editing translations.
			// We can allow ghosting for Examples that don't have translations
			// but not for a translation of a ghosted (not-yet existing) Example.
			Assert.Less(recordList.ListSize, Cache.LangProject.LexDbOA.Entries.Count());
			// Uncheck everything before we switch to parent list
			_browseViewerForTests.OnUncheckAll();
			var uncheckedTranslationItems = _browseViewerForTests.UncheckedItems();
			Assert.AreEqual(uncheckedTranslationItems.Count, recordList.ListSize);
			// go through each of the translation items, and find the LexEntry owner.
			var translationsToEntries = GetParentOfClassMap(uncheckedTranslationItems, LexEntryTags.kClassId);
			var expectedUnselectedEntries = new HashSet<int>(translationsToEntries.Values);
			// Now switch to Entries and expect the new parent items to be selected.
			using (FilterBehavior.Create(this))
			{
				_bulkEditBarForTests.SetTargetField("Lexeme Form");
			}
			var entriesSelected = new HashSet<int>(_browseViewerForTests.CheckedItems);
			var entriesUnselected = new HashSet<int>(_browseViewerForTests.UncheckedItems());
			Assert.AreEqual(expectedUnselectedEntries.Count, entriesUnselected.Count, "Unselected items mismatched.");
			Assert.IsTrue(expectedUnselectedEntries.SetEquals(entriesUnselected), "Unselected items mismatched.");
			Assert.Greater(entriesSelected.Count, 0);
		}

	#endregion MaintainCheckboxesSwitchingTargetListOwners_LT8986

		private abstract class FilterBehavior : IDisposable
		{
			private BulkEditCheckBoxBehaviorTests m_testFixture;

			private FilterBehavior(BulkEditCheckBoxBehaviorTests testFixture)
			{
				m_testFixture = testFixture;
				FirstBehavior();
			}

			internal static FilterBehavior Create(BulkEditCheckBoxBehaviorTests testFixture)
			{
				if (testFixture is BulkEditCheckBoxBehaviorTestsWithFilterChanges)
				{
					return new PusAndShowAll(testFixture);
				}
				return new NoFilter(testFixture);
			}

	#region Disposable stuff
			/// <summary/>
			~FilterBehavior()
			{
				Dispose(false);
			}

			/// <summary/>
			private bool IsDisposed { get; set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " *******");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					// dispose managed and unmanaged objects
					FinalBehavior();
					m_testFixture = null;
				}
				IsDisposed = true;
			}
	#endregion

			protected abstract void FirstBehavior();

			protected abstract void FinalBehavior();

			private sealed class PusAndShowAll : FilterBehavior
			{
				internal PusAndShowAll(BulkEditCheckBoxBehaviorTests testFixture)
					: base(testFixture)
				{
				}

				protected override void FirstBehavior()
				{
					m_testFixture._browseViewerForTests.SetFilter("Lexeme Form", "Filter for...", "pus");
				}

				protected override void FinalBehavior()
				{
					m_testFixture._browseViewerForTests.SetFilter("Lexeme Form", "Show All", null);
				}
			}

			private sealed class NoFilter : FilterBehavior
			{
				internal NoFilter(BulkEditCheckBoxBehaviorTests testFixture)
					: base(testFixture)
				{
				}

				protected override void FirstBehavior()
				{
					// no behavior
				}

				protected override void FinalBehavior()
				{
					// no behavior
				}
			}
		}
	}


	/// <summary>
	/// Add a layer of complexity to certain BulkEditCheckBoxBehaviorTests by performing a filter
	/// before switching list classes (e.g. entries to senses).
	/// </summary>
	[TestFixture]
	internal class BulkEditCheckBoxBehaviorTestsWithFilterChanges : BulkEditCheckBoxBehaviorTests
	{
		/// <summary />
		[Test, Ignore("no need to test again.")]
		public override void CheckboxBehavior_AllItemsShouldBeInitiallyCheckedPlusRefreshBehavior()
		{
			// no need to test again, when subclass has already done so.
		}

		/// <summary />
		[Test, Ignore("no need to test again.")]
		public override void CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Selected()
		{
			// no need to test again, when subclass has already done so.
		}

		/// <summary />
		[Test, Ignore("no need to test again.")]
		public override void CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Unselected()
		{
			// no need to test again, when subclass has already done so.
		}
	}
#endif
}
