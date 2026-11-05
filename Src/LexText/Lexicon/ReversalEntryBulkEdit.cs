// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using SIL.LCModel;
using SIL.LCModel.Application;
using XCore;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for AllReversalEntriesRecordList.
	/// </summary>
	public class AllReversalEntriesRecordList : RecordList
	{
		public override void Init(LcmCache cache, Mediator mediator, PropertyTable propertyTable, XmlNode recordListNode)
		{
			CheckDisposed();

			// <recordList owner="IReversalIndex" property="AllEntries" assemblyPath="RBRExtensions.dll" class="RBRExtensions.AllReversalEntriesRecordList"/>
			BaseInit(cache, mediator, propertyTable, recordListNode);
			//string owner = XmlUtils.GetOptionalAttributeValue(recordListNode, "owner");
			m_flid = ReversalIndexTags.kflidEntries; //LT-12577 a record list needs a real flid.
			//LT-14722 Crash when clicking Reversal Indexes
			//Clerk is null when going to Reversal Indexes for the first time.
			if (Clerk != null)
			{
				Guid riGuid = GetReversalIndexGuid(mediator, propertyTable);
				if (riGuid != Guid.Empty)
				{
					IReversalIndex ri = cache.ServiceLocator.GetObject(riGuid) as IReversalIndex;
					m_owningObject = ri;
					m_fontName = cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem).DefaultFontName;
				}
			}
			m_oldLength = 0;
		}

		public override bool CanInsertClass(string className)
		{
			if (base.CanInsertClass(className))
				return true;
			return className == "ReversalIndexEntry";
		}

		public override bool CreateAndInsert(string className, out ICmObject newObj)
		{
			if (className != "ReversalIndexEntry")
				return base.CreateAndInsert(className, out newObj);
			m_newItem = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
			newObj = m_newItem;
			var ri = (IReversalIndex)m_owningObject;
			ri.EntriesOC.Add(m_newItem);
			var extensions = m_cache.ActionHandlerAccessor as IActionHandlerExtensions;
			if (extensions != null)
				extensions.DoAtEndOfPropChanged(SelectNewItem);
			return true;
		}

		private IReversalIndexEntry m_newItem;

		void SelectNewItem()
		{
			Clerk.OnJumpToRecord(m_newItem.Hvo);
		}

		/// <summary>
		/// Get the current reversal index guid.  If there is none, create a new reversal index
		/// since there must not be any.  This fixes LT-6653.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <returns></returns>
		internal static Guid GetReversalIndexGuid(Mediator mediator, PropertyTable propertyTable)
		{
			var riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(propertyTable, "ReversalIndexGuid");

			if (riGuid.Equals(Guid.Empty))
			{
				try
				{
					riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(propertyTable, "ReversalIndexGuid");
				}
				catch
				{
					return Guid.Empty;
				}
			}
			return riGuid;
		}

		protected override IEnumerable<int> GetObjectSet()
		{
			IReversalIndex ri = m_owningObject as IReversalIndex;
			Debug.Assert(ri != null && ri.IsValidObject, "The owning IReversalIndex object is invalid!?");
			// Review: is there a better to to convert from List<Subclass> to List<Class>???
			List<IReversalIndexEntry> rgrie = ri.AllEntries;
			var rgcmo = new List<int>(rgrie.Count);
			foreach (IReversalIndexEntry rie in rgrie)
				rgcmo.Add(rie.Hvo);
			return rgcmo;
		}

		/// <summary>
		/// Delete the current object, reporting progress as far as possible.
		/// </summary>
		/// <param name="state"></param>
		public override void DeleteCurrentObject(ProgressState state, ICmObject thingToDelete)
		{
			CheckDisposed();

			base.DeleteCurrentObject(state, thingToDelete);

			ReloadList();
		}

		/// <summary>
		/// The settings key for this list's sorter, sort name or filter, keyed by the writing
		/// system of its reversal index, or null when no reversal index is current.
		/// </summary>
		/// <param name="revConfig">Names the reversal index by its writing system. When null,
		/// the index is the one the ReversalIndexGuid property names.</param>
		protected override string PropertyTableId(string sorterOrFilter, DictionaryConfigurationModel revConfig = null)
		{
			var wsId = ReversalWritingSystemId(revConfig);
			return wsId == null ? null : PropertyTableIdFor(wsId, sorterOrFilter);
		}

		/// <summary>
		/// Reads the setting under the writing-system key. A value found only under a key built
		/// from a configuration file path or an empty layout property moves to the writing-system
		/// key, which every later read for this index uses.
		/// </summary>
		protected override string GetPersistedSetting(string sorterOrFilter, DictionaryConfigurationModel revConfig = null)
		{
			var value = base.GetPersistedSetting(sorterOrFilter, revConfig);
			if (value != null)
				return value;
			var wsId = ReversalWritingSystemId(revConfig);
			if (wsId == null)
				return null;
			var isCurrentIndex = revConfig == null || wsId == ReversalWritingSystemId(null);
			foreach (var id in PathDerivedPropertyTableIds(wsId, isCurrentIndex, sorterOrFilter))
			{
				if (!m_propertyTable.PropertyExists(id, PropertyTable.SettingsGroup.LocalSettings))
					continue;
				value = m_propertyTable.GetStringProperty(id, null, PropertyTable.SettingsGroup.LocalSettings);
				if (value == null)
					continue;
				m_propertyTable.SetProperty(PropertyTableIdFor(wsId, sorterOrFilter), value, PropertyTable.SettingsGroup.LocalSettings, false);
				m_propertyTable.SetProperty(id, null, PropertyTable.SettingsGroup.LocalSettings, false);
				return value;
			}
			return null;
		}

		/// <summary>
		/// The writing system id of the reversal index the settings belong to: the
		/// configuration's when one is given, otherwise that of the index the ReversalIndexGuid
		/// property names.
		/// </summary>
		private string ReversalWritingSystemId(DictionaryConfigurationModel revConfig)
		{
			if (!string.IsNullOrEmpty(revConfig?.WritingSystem))
				return revConfig.WritingSystem;
			var riGuid = ReversalIndexEntryUi.GetReversalIndexGuid(m_propertyTable);
			if (riGuid == Guid.Empty)
				return null;
			return (m_cache.ServiceLocator.GetObject(riGuid) as IReversalIndex)?.WritingSystem;
		}

		/// <summary>
		/// The keys built from the text after the first '-' of a configuration file path: from
		/// the layout property when it names this writing system (or is empty while this is the
		/// current index), and from the project's configuration file for the writing system.
		/// </summary>
		private IEnumerable<string> PathDerivedPropertyTableIds(string wsId, bool isCurrentIndex, string sorterOrFilter)
		{
			string layoutId = null;
			var layout = m_propertyTable.GetStringProperty("ReversalIndexPublicationLayout", null);
			if (layout != null && (layout.Length == 0 ? isCurrentIndex : NamesWritingSystem(layout, wsId)))
			{
				layoutId = PropertyTableIdFor(layout.Substring(layout.IndexOf('-') + 1), sorterOrFilter);
				yield return layoutId;
			}
			var projectPath = Path.Combine(LcmFileHelper.GetConfigSettingsDir(m_cache.ProjectId.ProjectFolder),
				ReversalIndexServices.RevIndexDir, wsId + DictionaryConfigurationModel.FileExtension);
			var projectId = PropertyTableIdFor(projectPath.Substring(projectPath.IndexOf('-') + 1), sorterOrFilter);
			if (projectId != layoutId)
				yield return projectId;
		}

		/// <summary>
		/// Whether the layout value is a path whose file name is the writing system id; a value
		/// that cannot be a path never is.
		/// </summary>
		private static bool NamesWritingSystem(string layout, string wsId)
		{
			return layout.IndexOfAny(Path.GetInvalidPathChars()) < 0
				&& Path.GetFileNameWithoutExtension(layout) == wsId;
		}

		private string PropertyTableIdFor(string reversalLang, string sorterOrFilter)
		{
			// Dependent lists do not have owner/property set. Rather they have class/field.
			string className = VirtualListPublisher.MetaDataCache.GetOwnClsName((int)m_flid);
			string fieldName = VirtualListPublisher.MetaDataCache.GetFieldName((int)m_flid);
			var propertyName = String.IsNullOrEmpty(PropertyName) || PropertyName == fieldName ? fieldName : PropertyName;
			return String.Format("{0}.{1}-{2}_{3}", className, propertyName, reversalLang, sorterOrFilter);
		}
	}

	public class BulkReversalEntryPosEditor : BulkPosEditorBase
	{
		protected override ICmPossibilityList List
		{
			get
			{
				ICmPossibilityList list = null;
				var riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropTable, "ReversalIndexGuid");
				if (!riGuid.Equals(Guid.Empty))
				{
					try
					{
						IReversalIndex ri = m_cache.ServiceLocator.GetObject(riGuid) as IReversalIndex;
						list = ri.PartsOfSpeechOA;
					}
					catch
					{
						// Can't get an index if we have a bad guid.
					}
				}
				return list;
			}
		}

		public override List<int> FieldPath
		{
			get
			{
				return new List<int>(new int[] { ReversalIndexEntryTags.kflidPartOfSpeech,
					CmPossibilityTags.kflidName});
			}
		}

		/// <summary>
		/// Execute the change requested by the current selection in the combo.
		/// Basically we want the PartOfSpeech indicated by m_selectedHvo, even if 0,
		/// to become the POS of each record that is appropriate to change.
		/// We do nothing to records where the check box is turned off,
		/// and nothing to ones that currently have an MSA other than an IMoStemMsa.
		/// (a) If the owning entry has an IMoStemMsa with the
		/// right POS, set the sense to use it.
		/// (b) If the sense already refers to an IMoStemMsa, and any other senses
		/// of that entry which point at it are also to be changed, change the POS
		/// of the MSA.
		/// (c) If the entry has an IMoStemMsa which is not used at all, change it to the
		/// required POS and use it.
		/// (d) Make a new IMoStemMsa in the ILexEntry with the required POS and point the sense at it.
		/// </summary>
		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			CheckDisposed();

			m_cache.DomainDataByFlid.BeginUndoTask(LexEdStrings.ksUndoBulkEditRevPOS,
				LexEdStrings.ksRedoBulkEditRevPOS);
			int i = 0;
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach (int entryId in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 80 / itemsToChange.Count() + 20;
					state.Breath();
				}
				IReversalIndexEntry entry = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(entryId);
				if (m_selectedHvo == 0)
					entry.PartOfSpeechRA = null;
				else
					entry.PartOfSpeechRA = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(m_selectedHvo);
			}
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		protected override bool CanFakeIt(int hvo)
		{
			return true;
		}
	}
}
