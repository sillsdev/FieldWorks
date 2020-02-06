// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas
{
	internal abstract class ReversalListBase : RecordList
	{
		internal ReversalListBase(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, RecordFilterParameterObject recordFilterParameterObject = null, RecordSorter defaultSorter = null)
			: base(id, statusBar, decorator, usingAnalysisWs, vectorPropertyParameterObject, recordFilterParameterObject, defaultSorter)
		{
		}

		#region Overrides of RecordList

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			ChangeOwningObjectIfPossible();
			Subscriber.Subscribe(LanguageExplorerConstants.ReversalIndexGuid, ReversalIndexGuid_Handler);
			Subscriber.Subscribe(AreaServices.ToolForAreaNamed_ + AreaServices.LexiconAreaMachineName, JumpToIndex_Handler);
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
			if (base.TryRestoreSorter() && Sorter is GenRecordSorter)
			{
				var sorter = (GenRecordSorter)Sorter;
				var stringFinderComparer = sorter.Comparer as StringFinderCompare;
				if (stringFinderComparer != null)
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
			var newGuid = ReversalIndexServices.GetObjectGuidIfValid(PropertyTable, LanguageExplorerConstants.ReversalIndexGuid);
			if (newGuid.Equals(Guid.Empty))
			{
				return false;
			}
			var ri = m_cache.ServiceLocator.GetObject(newGuid) as IReversalIndex;
			if (ri == null)
			{
				return false;
			}
			var writingSystem = (CoreWritingSystemDefinition)m_cache.WritingSystemFactory.get_Engine(ri.WritingSystem);
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
		protected XElement BrowseViewFormCol
		{
			get
			{
				var doc = XDocument.Parse(LexiconResources.ReversalBulkEditReversalEntriesToolParameters);
				return doc.Root.Element("columns").Elements("column").First(col => col.Attribute("label").Value == "Reversal Form");
			}
		}

		internal void ChangeOwningObjectIfPossible()
		{
			ChangeOwningObject(ReversalIndexServices.GetObjectGuidIfValid(PropertyTable, LanguageExplorerConstants.ReversalIndexGuid));
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
			Publisher.Publish("MasterRefresh", null);
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
				Subscriber.Unsubscribe(AreaServices.ToolForAreaNamed_ + AreaServices.LexiconAreaMachineName, ReversalIndexGuid_Handler);
			}

			base.Dispose(disposing);
		}
	}
}