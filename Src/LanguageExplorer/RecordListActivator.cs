// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace LanguageExplorer
{
	internal sealed class RecordListActivator : IDisposable
	{
		private static IRecordList s_dictionaryRecordList;
		private static IRecordList s_reversalIndexRecordList;
		private IRecordListRepository _recordListRepository;
		private bool _isDisposed;

		internal IRecordList ActiveRecordList { get; private set; }

		private RecordListActivator(IRecordListRepository recordListRepository, IRecordList currentRecordList)
		{
			_recordListRepository = recordListRepository;
			_recordListRepository.ActiveRecordList = null;
			ActiveRecordList = currentRecordList;
		}

		#region disposal
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				s_dictionaryRecordList?.BecomeInactive();
				s_reversalIndexRecordList?.BecomeInactive();
				_recordListRepository.ActiveRecordList = ActiveRecordList;
			}
			ActiveRecordList = null;
			_recordListRepository = null;

			_isDisposed = true;
		}

		~RecordListActivator()
		{
			Dispose(false);
		}
		#endregion disposal

		private static void CacheRecordList(string recordListType, IRecordList recordList)
		{
			switch (recordListType)
			{
				case LanguageExplorerConstants.DictionaryType:
					s_dictionaryRecordList = recordList;
					break;
				case LanguageExplorerConstants.ReversalType:
					s_reversalIndexRecordList = recordList;
					break;
			}
		}

		internal static RecordListActivator ActivateRecordListMatchingExportType(string exportType, StatusBar statusBar, IPropertyTable propertyTable)
		{
			var isDictionary = exportType == LanguageExplorerConstants.DictionaryType;
			var recordListId = isDictionary ? LanguageExplorerConstants.Entries : LanguageExplorerConstants.AllReversalEntries;
			var activeRecordListRepository = propertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository);
			var activeRecordList = activeRecordListRepository.ActiveRecordList;
			if (activeRecordList != null && activeRecordList.Id == recordListId)
			{
				return null; // No need to juggle record lists if the one we want is already active
			}
			var tempRecordList = isDictionary ? s_dictionaryRecordList : s_reversalIndexRecordList;
			if (tempRecordList == null)
			{
				tempRecordList = isDictionary ? activeRecordListRepository.GetRecordList(LanguageExplorerConstants.Entries, statusBar, EntriesFactoryMethod) : activeRecordListRepository.GetRecordList(LanguageExplorerConstants.AllReversalEntries, statusBar, AllReversalEntriesFactoryMethod);
				CacheRecordList(exportType, tempRecordList);
			}
			var retval = new RecordListActivator(activeRecordListRepository, activeRecordList);
			if (!tempRecordList.IsSubservientRecordList)
			{
				if (propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList != tempRecordList)
				{
					// Some tests may not have a window.
					if (propertyTable.TryGetValue<Form>(FwUtilsConstants.window, out var form))
					{
						RecordListServices.SetRecordList(form.Handle, tempRecordList);
					}
				}
				tempRecordList.ActivateUI(false);
			}
			tempRecordList.UpdateList(true, true);
			return retval; // ensure the current active record list is reactivated after we use another record list temporarily.
		}

		internal static IRecordList EntriesFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == LanguageExplorerConstants.Entries, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{LanguageExplorerConstants.Entries}'.");
			/*
			<clerk id="entries">
				<recordList owner="LexDb" property="Entries" />
				<filters />
				<sortMethods>
				<sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
				<sortMethod label="Primary Gloss" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="PrimaryGloss" />
				</sortMethods>
			</clerk>
			*/
			var recordList = new RecordList(recordListId, statusBar,
				cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), false,
				new VectorPropertyParameterObject(cache.LanguageProject.LexDbOA, "Entries", cache.MetaDataCacheAccessor.GetFieldId2(cache.LanguageProject.LexDbOA.ClassID, "Entries", false)),
				new Dictionary<string, PropertyRecordSorter>
				{
					{ AreaServices.Default, new PropertyRecordSorter(AreaServices.ShortName) },
					{ "PrimaryGloss", new PropertyRecordSorter("PrimaryGloss") }
				});
			return recordList;
		}

		internal static IRecordList AllReversalEntriesFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == LanguageExplorerConstants.AllReversalEntries, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{LanguageExplorerConstants.AllReversalEntries}'.");
			/*
			<clerk id="AllReversalEntries">
				<dynamicloaderinfo assemblyPath="LexEdDll.dll" class="SIL.FieldWorks.XWorks.LexEd.ReversalEntryClerk" />
				<recordList owner="ReversalIndex" property="AllEntries">
				<dynamicloaderinfo assemblyPath="LexEdDll.dll" class="SIL.FieldWorks.XWorks.LexEd.AllReversalEntriesRecordList" />
				</recordList>
				<filters />
				<sortMethods>
				<sortMethod label="Form" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
				</sortMethods>
				<!--<recordFilterListProvider assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.WfiRecordFilterListProvider"/>-->
			</clerk>
			*/
			var currentGuid = FwUtils.GetObjectGuidIfValid(flexComponentParameters.PropertyTable, LanguageExplorerConstants.ReversalIndexGuid);
			IReversalIndex revIdx = null;
			if (currentGuid != Guid.Empty)
			{
				revIdx = (IReversalIndex)cache.ServiceLocator.GetObject(currentGuid);
				// This looks like our best chance to update a global "Current Reversal Index Writing System" value.
				WritingSystemServices.CurrentReversalWsId = cache.WritingSystemFactory.GetWsFromStr(revIdx.WritingSystem);
				// Generate and store the expected path to a configuration file specific to this reversal index.  If it doesn't
				// exist, code elsewhere will make up for it.
				var layoutName = Path.Combine(LcmFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder), "ReversalIndex", revIdx.ShortName + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				flexComponentParameters.PropertyTable.SetProperty("ReversalIndexPublicationLayout", layoutName, true, true);
			}
			return new AllReversalEntriesRecordList(statusBar, cache.ServiceLocator, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), revIdx);
		}

		internal static IRecordList ReversalIndexPOSFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == LanguageExplorerConstants.ReversalEntriesPOS, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{LanguageExplorerConstants.ReversalEntriesPOS}'.");
			/*
            <clerk id="ReversalEntriesPOS">
              <dynamicloaderinfo assemblyPath="LexEdDll.dll" class="SIL.FieldWorks.XWorks.LexEd.ReversalEntryPOSClerk" />
              <recordList owner="ReversalIndex" property="PartsOfSpeech">
                <dynamicloaderinfo assemblyPath="LexEdDll.dll" class="SIL.FieldWorks.XWorks.LexEd.ReversalIndexPOSRecordList" />
              </recordList>
              <filters />
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
              <!--<recordFilterListProvider assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.WfiRecordFilterListProvider"/>-->
            </clerk>
			*/
			// NB: No need to pass 'recordListId' to the constructor, since it supplies ReversalIndexPOSRecordList.ReversalEntriesPOS for the id.
			return new ReversalIndexPOSRecordList(statusBar, cache.ServiceLocator, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(),
				(IReversalIndex)cache.ServiceLocator.GetObject(FwUtils.GetObjectGuidIfValid(flexComponentParameters.PropertyTable, LanguageExplorerConstants.ReversalIndexGuid)));
		}

		private abstract class ReversalListBase : RecordList, IReversalRecordList
		{
			internal ReversalListBase(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, RecordFilterParameterObject recordFilterParameterObject = null, RecordSorter defaultSorter = null)
				: base(id, statusBar, decorator, usingAnalysisWs, vectorPropertyParameterObject, recordFilterParameterObject, defaultSorter)
			{
			}

			private IReversalRecordList AsIReversalRecordList => this;

			#region Overrides of RecordList

			/// <summary>
			/// Initialize a FLEx component with the basic interfaces.
			/// </summary>
			/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
			public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
			{
				base.InitializeFlexComponent(flexComponentParameters);

				AsIReversalRecordList.ChangeOwningObjectIfPossible();
				Subscriber.Subscribe(LanguageExplorerConstants.ReversalIndexGuid, ReversalIndexGuid_Handler);
				Subscriber.Subscribe(LanguageExplorerConstants.ToolForAreaNamed_ + LanguageExplorerConstants.LexiconAreaMachineName, JumpToIndex_Handler);
			}

			private void JumpToIndex_Handler(object obj)
			{
				var rootIndex = GetRootIndex(CurrentIndex);
				JumpToIndex(rootIndex);
			}

			private void ReversalIndexGuid_Handler(object obj)
			{
				ChangeOwningObject(Guid.Parse((string)obj));
			}

			/// <summary />
			/// <returns><c>true</c> if we changed or initialized a new sorter,
			/// <c>false</c>if the one installed matches the one we had stored to persist.</returns>
			protected override bool TryRestoreSorter()
			{
				var fakevc = new XmlBrowseViewVc
				{
					SuppressPictures = true, // SuppressPictures to make sure that we don't leak anything as this will not be disposed.
					DataAccess = VirtualListPublisher,
					Cache = m_cache
				};
				if (base.TryRestoreSorter() && Sorter is GenRecordSorter genRecordSorter)
				{
					if (genRecordSorter.Comparer is StringFinderCompare stringFinderComparer)
					{
						var colSpec = ReflectionHelper.GetField(stringFinderComparer.Finder, "m_colSpec") as XElement ?? BrowseViewFormCol;
						Sorter = new GenRecordSorter(new StringFinderCompare(LayoutFinder.CreateFinder(m_cache, colSpec, fakevc, PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App)), stringFinderComparer.SubComparer));
					}
					return true;
				}
				if (Sorter is GenRecordSorter) // If we already have a GenRecordSorter, it's probably an existing, valid one.
				{
					return false;
				}
				// Try to create a sorter based on the current Reversal Index's WritingSystem
				var newGuid = FwUtils.GetObjectGuidIfValid(PropertyTable, LanguageExplorerConstants.ReversalIndexGuid);
				if (newGuid.Equals(Guid.Empty))
				{
					return false;
				}
				if (!(m_cache.ServiceLocator.GetObject(newGuid) is IReversalIndex reversalIndex))
				{
					return false;
				}
				var writingSystem = (CoreWritingSystemDefinition)m_cache.WritingSystemFactory.get_Engine(reversalIndex.WritingSystem);
				Sorter = new GenRecordSorter(new StringFinderCompare(LayoutFinder.CreateFinder(m_cache, BrowseViewFormCol, fakevc, PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App)), new WritingSystemComparer(writingSystem)));
				return true;
			}

			#endregion

			/// <summary>
			/// Returns the index of the root object whose descendent is the object at lastValidIndex.
			/// </summary>
			private int GetRootIndex(int lastValidIndex)
			{
				var item = SortItemAt(lastValidIndex);
				if (item == null)
				{
					return lastValidIndex;
				}
				var parentIndex = IndexOfParentOf(item.KeyObject);
				return parentIndex == -1 ? lastValidIndex : GetRootIndex(parentIndex);
			}

			/// <summary>
			/// Returns the XmlNode which configures the FormColumn in the BrowseView associated with BulkEdit of ReversalEntries
			/// </summary>
			private XElement BrowseViewFormCol
			{
				get
				{
					var doc = XDocument.Parse(LexiconResources.ReversalBulkEditReversalEntriesToolParameters);
					return doc.Root.Element("columns").Elements("column").First(col => col.Attribute("label").Value == "Reversal Form");
				}
			}

			void IReversalRecordList.ChangeOwningObjectIfPossible()
			{
				ChangeOwningObject(FwUtils.GetObjectGuidIfValid(PropertyTable, LanguageExplorerConstants.ReversalIndexGuid));
			}

			private void ChangeOwningObject(Guid newGuid)
			{
				if (newGuid.Equals(Guid.Empty))
				{
					// We need to find another reversal index. Any will do.
					newGuid = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances().First().Guid;
					PropertyTable.SetProperty(LanguageExplorerConstants.ReversalIndexGuid, newGuid.ToString(), true, true, SettingsGroup.LocalSettings);
				}
				var ri = m_cache.ServiceLocator.GetObject(newGuid) as IReversalIndex;
				if (ri == null)
				{
					return;
				}
				// This looks like our best chance to update a global "Current Reversal Index Writing System" value.
				WritingSystemServices.CurrentReversalWsId = m_cache.WritingSystemFactory.GetWsFromStr(ri.WritingSystem);
				// Generate and store the expected path to a configuration file specific to this reversal index.  If it doesn't
				// exist, code elsewhere will make up for it.
				var layoutName = Path.Combine(LcmFileHelper.GetConfigSettingsDir(m_cache.ProjectId.ProjectFolder), "ReversalIndex", ri.ShortName + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				PropertyTable.SetProperty("ReversalIndexPublicationLayout", layoutName, true, true);
				var newOwningObj = NewOwningObject(ri);
				if (ReferenceEquals(newOwningObj, OwningObject))
				{
					return;
				}
				UpdateFiltersAndSortersIfNeeded(); // Load the index-specific sorter
				OnChangeSorter(); // Update the column headers with sort arrows
				OwningObject = newOwningObj; // This automatically reloads (and sorts) the list
				Publisher.Publish(new PublisherParameterObject(LanguageExplorerConstants.MasterRefresh));
			}

			/// <summary />
			protected abstract ICmObject NewOwningObject(IReversalIndex ri);

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					Subscriber.Unsubscribe(LanguageExplorerConstants.ReversalIndexGuid, ReversalIndexGuid_Handler);
					Subscriber.Unsubscribe(LanguageExplorerConstants.ToolForAreaNamed_ + LanguageExplorerConstants.LexiconAreaMachineName, ReversalIndexGuid_Handler);
				}

				base.Dispose(disposing);
			}
		}

		/// <summary>
		/// List used in tools: "Reversal Indexes" & "Bulk Edit Reversal Entries".
		/// </summary>
		private sealed class AllReversalEntriesRecordList : ReversalListBase
		{
			private IReversalIndexEntry _newItem;

			/// <summary />
			internal AllReversalEntriesRecordList(StatusBar statusBar, ILcmServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
				: base(LanguageExplorerConstants.AllReversalEntries, statusBar, decorator, true, new VectorPropertyParameterObject(reversalIndex, "AllEntries", ReversalIndexTags.kflidEntries))
			{
				m_fontName = serviceLocator.WritingSystemManager.Get(reversalIndex.WritingSystem).DefaultFontName;
				m_oldLength = 0;
			}

			private void SelectNewItem()
			{
				JumpToRecord(_newItem.Hvo);
			}

			#region Overrides of RecordList

			/// <summary />
			protected override bool CanInsertClass(string className)
			{
				return base.CanInsertClass(className) || className == "ReversalIndexEntry";
			}

			/// <summary />
			protected override bool CreateAndInsert(string className)
			{
				if (className != "ReversalIndexEntry")
				{
					return base.CreateAndInsert(className);
				}
				_newItem = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
				var reversalIndex = (IReversalIndex)OwningObject;
				reversalIndex.EntriesOC.Add(_newItem);
				var extensions = m_cache.ActionHandlerAccessor as IActionHandlerExtensions;
				extensions?.DoAtEndOfPropChanged(SelectNewItem);
				return true;
			}

			/// <summary />
			protected override IEnumerable<int> GetObjectSet()
			{
				var reversalIndex = OwningObject as IReversalIndex;
				Debug.Assert(reversalIndex != null && reversalIndex.IsValidObject, "The owning IReversalIndex object is invalid!?");
				return new List<int>(reversalIndex.AllEntries.Select(rie => rie.Hvo));
			}

			/// <summary>
			/// Delete the current object.
			/// In some cases thingToDelete is not actually the current object, but it should always
			/// be related to it.
			/// </summary>
			protected override void DeleteCurrentObject(ICmObject thingToDelete = null)
			{
				base.DeleteCurrentObject(thingToDelete);

				ReloadList();
			}

			/// <summary />
			protected override string PropertyTableId(string sorterOrFilter)
			{
				var reversalPub = PropertyTable.GetValue<string>("ReversalIndexPublicationLayout");
				if (reversalPub == null)
				{
					return null; // there is no current Reversal Index; don't try to find Properties (sorter & filter) for a nonexistent Reversal Index
				}
				var reversalLang = reversalPub.Substring(reversalPub.IndexOf('-') + 1); // strip initial "publishReversal-"
																						// Dependent lists do not have owner/property set. Rather they have class/field.
				var className = VirtualListPublisher.MetaDataCache.GetOwnClsName((int)m_flid);
				var fieldName = VirtualListPublisher.MetaDataCache.GetFieldName((int)m_flid);
				if (string.IsNullOrEmpty(PropertyName) || PropertyName == fieldName)
				{
					return $"{className}.{fieldName}-{reversalLang}_{sorterOrFilter}";
				}
				return $"{className}.{PropertyName}-{reversalLang}_{sorterOrFilter}";
			}

			#endregion

			#region Overrides of ReversalListBase

			/// <summary />
			protected override ICmObject NewOwningObject(IReversalIndex ri)
			{
				return ri;
			}

			#endregion
		}

		/// <summary />
		private sealed class ReversalIndexPOSRecordList : ReversalListBase
		{
			/// <summary />
			internal ReversalIndexPOSRecordList(StatusBar statusBar, ILcmServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
				: base(LanguageExplorerConstants.ReversalEntriesPOS, statusBar, decorator, true, new VectorPropertyParameterObject(reversalIndex.PartsOfSpeechOA, "Possibilities", CmPossibilityListTags.kflidPossibilities))
			{
				m_fontName = serviceLocator.WritingSystemManager.Get(reversalIndex.WritingSystem).DefaultFontName;
				m_oldLength = 0;
			}

			#region Overrides of RecordList

			protected override IEnumerable<int> GetObjectSet()
			{
				return ((ICmPossibilityList)OwningObject).PossibilitiesOS.ToHvoArray();
			}

			/// <summary />
			protected override ClassAndPropInfo GetMatchingClass(string className)
			{
				if (className != "PartOfSpeech")
				{
					return null;
				}
				// A possibility list only allows one type of possibility to be owned in the list.
				return m_cache.DomainDataByFlid.MetaDataCache.GetClassName(((ICmPossibilityList)OwningObject).ItemClsid) != className ? null : m_insertableClasses.FirstOrDefault(cpi => cpi.signatureClassName == className);
			}

			/// <summary />
			public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
			{
				if (OwningObject != null && OwningObject.Hvo != hvo)
				{
					return;     // This PropChanged doesn't really apply to us.
				}
				if (tag == m_flid)
				{
					ReloadList();
				}
				else
				{
					base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
				}
			}

			#endregion

			#region Overrides of ReversalListBase

			/// <summary />
			protected override ICmObject NewOwningObject(IReversalIndex ri)
			{
				return ri.PartsOfSpeechOA;
			}

			#endregion
		}
	}
}