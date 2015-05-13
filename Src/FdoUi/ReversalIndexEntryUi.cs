using System;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// ReversalIndexEntryUi provides UI-specific methods for the ReversalIndexEntryUi class.
	/// </summary>
	public class ReversalIndexEntryUi : CmObjectUi
	{
		/// <summary>
		/// Create one. Argument must be a PartOfSpeech.
		/// Note that declaring it to be forces us to just do a cast in every case of MakeUi, which is
		/// passed an obj anyway.
		/// </summary>
		/// <param name="obj"></param>
		public ReversalIndexEntryUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IReversalIndexEntry);
		}

		internal ReversalIndexEntryUi() {}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = FdoUiStrings.ksMergeReversalEntry;
			wp.m_label = FdoUiStrings.ksEntries;

			var rie = (IReversalIndexEntry) Object;
			int wsIndex = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(rie.ReversalIndex.WritingSystem);
			foreach (var rieInner in rie.ReversalIndex.AllEntries)
			{
				if (rieInner.Hvo != Object.Hvo)
				{
					mergeCandidates.Add(
						new DummyCmObject(
						rieInner.Hvo,
						rieInner.ShortName,
						wsIndex));
				}
			}
			guiControl = "MergeReversalEntryList";
			helpTopic = "khtpMergeReversalEntry";
			return new DummyCmObject(m_hvo, rie.ShortName, wsIndex);
		}

		/// <summary>
		/// Fetches the GUID value of the given property, having checked it is a valid object.
		/// If it is not a valid object, the property is removed.
		/// </summary>
		/// <param name="propertyTable"></param>
		/// <param name="key">Property name</param>
		/// <returns>The ReversalIndexGuid, or empty GUID if there is a problem</returns>
		public static Guid GetObjectGuidIfValid(PropertyTable propertyTable, string key)
		{
			var sGuid = propertyTable.GetStringProperty(key, "");
			if (string.IsNullOrEmpty(sGuid))
				return Guid.Empty;

			Guid guid;
			try
			{
				guid = new Guid(sGuid);
			}
			catch
			{
				return Guid.Empty;
			}

			var cache = propertyTable.GetValue<FdoCache>("cache");
			if (!cache.ServiceLocator.ObjectRepository.IsValidObjectId(guid))
			{
				propertyTable.RemoveProperty(key);
				return Guid.Empty;
			}
			return guid;
		}

	}
}
