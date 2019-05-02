// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.BulkEditEntries
{
	/// <summary>
	/// ITool implementation for the "bulkEditEntriesOrSenses" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class BulkEditEntriesOrSensesTool : ITool
	{
		private BulkEditEntriesOrSensesMenuHelper _bulkEditEntriesOrSensesMenuHelper;
		private BrowseViewContextMenuFactory _browseViewContextMenuFactory;
		private const string EntriesOrChildren = "entriesOrChildren";
		private PaneBarContainer _paneBarContainer;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
		[Import(AreaServices.LexiconAreaMachineName)]
		private IArea _area;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// This will also remove any event handlers set up by the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);

			// Dispose these after the main UI stuff.
			_bulkEditEntriesOrSensesMenuHelper.Dispose();
			_browseViewContextMenuFactory.Dispose();

			_bulkEditEntriesOrSensesMenuHelper = null;
			_browseViewContextMenuFactory = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// Crashes in RecordList "CheckExpectedListItemsClassInSync" with: for some reason BulkEditBar.ExpectedListItemsClassId({0}) does not match SortItemProvider.ListItemsClass({1}).
			// BulkEditBar expected 5002, but
			// SortItemProvider was: 5035
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(EntriesOrChildren, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}
			_bulkEditEntriesOrSensesMenuHelper = new BulkEditEntriesOrSensesMenuHelper(majorFlexComponentParameters, this, _recordList);
			_browseViewContextMenuFactory = new BrowseViewContextMenuFactory();
#if RANDYTODO
			// TODO: Set up factory method for the browse view.
#endif

			var root = XDocument.Parse(LexiconResources.BulkEditEntriesOrSensesToolParameters).Root;
			var parametersElement = root.Element("parameters");
			parametersElement.Element("includeColumns").ReplaceWith(XElement.Parse(LexiconResources.LexiconBrowseDialogColumnDefinitions));
			OverrideServices.OverrideVisibiltyAttributes(parametersElement.Element("columns"), root.Element("overrides"));
			_recordBrowseView = new RecordBrowseView(parametersElement, _browseViewContextMenuFactory, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);

			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer,
				_recordBrowseView);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_recordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			_recordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordList.VirtualListPublisher).Refresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.BulkEditEntriesOrSensesMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "CRASHES: Bulk Edit Entries";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.BrowseView.SetBackgroundColor(Color.Magenta);

		#endregion

		internal static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Guard.AssertThat(recordListId == EntriesOrChildren, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{EntriesOrChildren}'.");
			/*
            <clerk id="entriesOrChildren">
              <recordList owner="LexDb" property="Entries">
                <!-- by default load for Entries but can be for AllSenses too -->
                <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.EntriesOrChildClassesRecordList" />
                <PartOwnershipTree>
                  <!-- the ClassOwnershipTree describes the relative relationship between the target classes in the possible source properties
								 loaded by this list. This especially helps in maintaining the CurrentIndex when switching from on property to the next. -->
                  <ClassOwnershipTree>
                    <LexEntry sourceField="Entries">
                      <LexEntryRef sourceField="AllEntryRefs" altSourceField="ComplexEntryTypes:AllComplexEntryRefPropertyTargets;VariantEntryTypes:AllVariantEntryRefPropertyTargets" />
                      <LexPronunciation sourceField="AllPossiblePronunciations" />
                      <LexEtymology sourceField="AllPossibleEtymologies" />
                      <MoForm sourceField="AllPossibleAllomorphs" />
                      <LexSense sourceField="AllSenses">
                        <LexExampleSentence sourceField="AllExampleSentenceTargets">
                          <CmTranslation sourceField="AllExampleTranslationTargets" />
                        </LexExampleSentence>
                        <LexExtendedNote sourceField="AllExtendedNoteTargets" />
                        <CmPicture sourceField="AllPossiblePictures" />
                      </LexSense>
                    </LexEntry>
                  </ClassOwnershipTree>
                  <ParentClassPathsToChildren>
                    <!-- ClassOwnershipPaths describes how to get from the parent ListItemsClass to the destinationClass objects
									 of the list properties -->
                    <part id="LexEntry-Jt-AllPossiblePronunciations" type="jtview">
                      <seq class="LexEntry" field="Pronunciations" firstOnly="true" layout="empty">
                        <int class="LexPronunciation" field="Self" />
                      </seq>
                      <!-- NOTE: AllPossiblePronunciations can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllPossibleEtymologies" type="jtview">
                      <seq class="LexEntry" field="Etymology" firstOnly="true" layout="empty">
                        <int class="LexEtymology" field="Self" />
                      </seq>
                      <!-- NOTE: AllPossibleEtymologies can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllComplexEntryRefPropertyTargets" type="jtview">
                      <seq class="LexEntry" field="EntryRefs" firstOnly="true" layout="empty">
                        <int class="LexEntryRef" field="Self" />
                      </seq>
                      <!-- NOTE: AllComplexEntryRefPropertyTargets can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllVariantEntryRefPropertyTargets" type="jtview">
                      <seq class="LexEntry" field="EntryRefs" firstOnly="true" layout="empty">
                        <int class="LexEntryRef" field="Self" />
                      </seq>
                      <!-- NOTE: AllVariantEntryRefPropertyTargets can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllPossibleAllomorphs" type="jtview">
                      <seq class="LexEntry" field="AlternateForms" firstOnly="true" layout="empty">
                        <int class="MoForm" field="Self" />
                      </seq>
                      <!-- NOTE: AllPossibleAllomorphs can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllEntryRefs" type="jtview">
                      <seq class="LexEntry" field="EntryRefs" firstOnly="true" layout="empty">
                        <int class="LexEntryRef" field="Self" />
                      </seq>
                    </part>
                    <part id="LexEntry-Jt-AllSenses" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <int class="LexSense" field="Self" />
                      </seq>
                    </part>
                    <!-- the next item is needed to prevent a crash -->
                    <part id="LexSense-Jt-AllSenses" type="jtview">
                      <obj class="LexSense" field="Self" firstOnly="true" layout="empty" />
                    </part>
                    <part id="LexEntry-Jt-AllExampleSentenceTargets" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <seq class="LexSense" field="Examples" firstOnly="true" layout="empty">
                          <int class="LexExampleSentence" field="Self" />
                        </seq>
                      </seq>
                    </part>
                    <part id="LexSense-Jt-AllExampleSentenceTargets" type="jtview">
                      <seq class="LexSense" field="Examples" firstOnly="true" layout="empty">
                        <int class="LexExampleSentence" field="Self" />
                      </seq>
                    </part>
                    <part id="LexEntry-Jt-AllPossiblePictures" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <seq class="LexSense" field="Pictures" firstOnly="true" layout="empty">
                          <int class="CmPicture" field="Self" />
                        </seq>
                      </seq>
                    </part>
                    <part id="LexSense-Jt-AllPossiblePictures" type="jtview">
                      <seq class="LexSense" field="Pictures" firstOnly="true" layout="empty">
                        <int class="CmPicture" field="Self" />
                      </seq>
                    </part>
                    <part id="LexEntry-Jt-AllExampleTranslationTargets" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <seq class="LexSense" field="Examples" firstOnly="true" layout="empty">
                          <seq class="LexExampleSentence" field="Translations" firstOnly="true" layout="empty">
                            <int class="CmTranslation" field="Self" />
                          </seq>
                        </seq>
                      </seq>
                    </part>
                    <part id="LexSense-Jt-AllExampleTranslationTargets" type="jtview">
                      <seq class="LexSense" field="Examples" firstOnly="true" layout="empty">
                        <seq class="LexExampleSentence" field="Translations" firstOnly="true" layout="empty">
                          <int class="CmTranslation" field="Self" />
                        </seq>
                      </seq>
                    </part>
                    <part id="LexExampleSentence-Jt-AllExampleTranslationTargets" type="jtview">
                      <seq class="LexExampleSentence" field="Translations" firstOnly="true" layout="empty">
                        <int class="CmTranslation" field="Self" />
                      </seq>
                    </part>
                    <part id="LexEntry-Jt-AllExtendedNoteTargets" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <seq class="LexSense" field="ExtendedNote" firstOnly="true" layout="empty">
                          <int class="LexExtendedNote" field="Self" />
                        </seq>
                      </seq>
                    </part>
                    <part id="LexSense-Jt-AllExtendedNoteTargets" type="jtview">
                      <seq class="LexSense" field="ExtendedNote" firstOnly="true" layout="empty">
                        <int class="LexExtendedNote" field="Self" />
                      </seq>
                    </part>
                  </ParentClassPathsToChildren>
                </PartOwnershipTree>
              </recordList>
              <filters />
              <!-- only the default sortMethod is needed -->
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
                <sortMethod label="Primary Gloss" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="PrimaryGloss" />
              </sortMethods>
            </clerk>
			 */
			return new EntriesOrChildClassesRecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), cache.LanguageProject.LexDbOA);
		}

		private sealed class BulkEditEntriesOrSensesMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;

			internal BulkEditEntriesOrSensesMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;

				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				// Only register tool for now. The tool's RecordBrowseView will register as a UserControl, so a tool must be registered before that happens.
				majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~BulkEditEntriesOrSensesMenuHelper()
			{
				// The base class finalizer is called automatically.
				Dispose(false);
			}

			/// <inheritdoc />
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SuppressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (_isDisposed)
				{
					// No need to run it more than once.
					return;
				}
				if (disposing)
				{
				}
				_majorFlexComponentParameters = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
