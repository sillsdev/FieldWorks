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
			return wsId == null ? null : WritingSystemPropertyTableId(wsId, sorterOrFilter);
		}

		/// <summary>
		/// Reads the setting under the writing-system key, which decides once it exists, even
		/// with no value. Until then one older key decides: its value, empty or not, moves to
		/// the writing-system key and the older key is removed.
		/// </summary>
		protected override string GetPersistedSetting(string sorterOrFilter, DictionaryConfigurationModel revConfig = null)
		{
			var wsId = ReversalWritingSystemId(revConfig);
			if (wsId == null)
				return null;
			var wsKey = WritingSystemPropertyTableId(wsId, sorterOrFilter);
			if (m_propertyTable.PropertyExists(wsKey, PropertyTable.SettingsGroup.LocalSettings))
				return m_propertyTable.GetStringProperty(wsKey, null, PropertyTable.SettingsGroup.LocalSettings);
			var isCurrentIndex = revConfig == null || wsId == ReversalWritingSystemId(null);
			var olderKey = OlderPropertyTableIds(wsId, isCurrentIndex, sorterOrFilter)
				.FirstOrDefault(id => m_propertyTable.PropertyExists(id, PropertyTable.SettingsGroup.LocalSettings));
			if (olderKey == null)
				return null;
			var value = m_propertyTable.GetStringProperty(olderKey, null, PropertyTable.SettingsGroup.LocalSettings);
			m_propertyTable.SetProperty(wsKey, value, PropertyTable.SettingsGroup.LocalSettings, false);
			m_propertyTable.RemoveProperty(olderKey, PropertyTable.SettingsGroup.LocalSettings);
			return value;
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
		/// The older keys to try, in order. When the layout is for this index, or is empty
		/// while this is the current index, only the layout's key, which holds what the view
		/// showed. Otherwise the keys of the project's configuration file named for the
		/// writing system, then of the other project files declaring it, sorted by path.
		/// </summary>
		private IEnumerable<string> OlderPropertyTableIds(string wsId, bool isCurrentIndex, string sorterOrFilter)
		{
			var layout = m_propertyTable.GetStringProperty("ReversalIndexPublicationLayout", null);
			if (layout != null && (layout.Length == 0 ? isCurrentIndex : IsConfigurationFor(layout, wsId)))
				return new[] { OlderPropertyTableId(layout, sorterOrFilter) };
			var configDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(m_cache.ProjectId.ProjectFolder),
				ReversalIndexServices.RevIndexDir);
			var paths = new List<string> { Path.Combine(configDir, wsId + DictionaryConfigurationModel.FileExtension) };
			if (Directory.Exists(configDir))
			{
				paths.AddRange(Directory.EnumerateFiles(configDir, "*" + DictionaryConfigurationModel.FileExtension)
					.Where(path => DeclaresWritingSystem(path, wsId))
					.OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
			}
			return paths.Select(path => OlderPropertyTableId(path, sorterOrFilter)).Distinct();
		}

		/// <summary>
		/// Whether the layout value is a configuration file path for the writing system: its
		/// file name is the writing system id, or the file declares that writing system. A value
		/// that cannot be a path never is.
		/// </summary>
		private static bool IsConfigurationFor(string layout, string wsId)
		{
			return layout.IndexOfAny(Path.GetInvalidPathChars()) < 0
				&& (Path.GetFileNameWithoutExtension(layout) == wsId || DeclaresWritingSystem(layout, wsId));
		}

		/// <summary>
		/// Whether a configuration file exists at the path and its root element declares the
		/// writing system. Only that attribute is read.
		/// </summary>
		private static bool DeclaresWritingSystem(string path, string wsId)
		{
			if (!File.Exists(path))
				return false;
			try
			{
				using (var stream = File.OpenRead(path))
				using (var reader = XmlReader.Create(stream))
				{
					reader.MoveToContent();
					return reader.GetAttribute("writingSystem") == wsId;
				}
			}
			catch (Exception e) when (e is XmlException || e is IOException || e is UnauthorizedAccessException)
			{
				return false;
			}
		}

		/// <summary>
		/// The settings key for a reversal index's writing system. The ':' keeps it distinct
		/// from the '-' keys a settings file may hold, even ones named by a writing system.
		/// </summary>
		private string WritingSystemPropertyTableId(string wsId, string sorterOrFilter)
		{
			return String.Format("{0}:{1}_{2}", ListSettingsName(), wsId, sorterOrFilter);
		}

		/// <summary>
		/// An older settings key, built from the text after the first '-' of a layout value
		/// or a configuration file path.
		/// </summary>
		private string OlderPropertyTableId(string layoutOrPath, string sorterOrFilter)
		{
			return String.Format("{0}-{1}_{2}", ListSettingsName(),
				layoutOrPath.Substring(layoutOrPath.IndexOf('-') + 1), sorterOrFilter);
		}

		private string ListSettingsName()
		{
			// Dependent lists do not have owner/property set. Rather they have class/field.
			string className = VirtualListPublisher.MetaDataCache.GetOwnClsName((int)m_flid);
			string fieldName = VirtualListPublisher.MetaDataCache.GetFieldName((int)m_flid);
			var propertyName = String.IsNullOrEmpty(PropertyName) || PropertyName == fieldName ? fieldName : PropertyName;
			return className + "." + propertyName;
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
