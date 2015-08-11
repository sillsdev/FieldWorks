// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;

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
			var filteredHvos = new HashSet<int>(rie.AllOwnedObjects.Select(obj => obj.Hvo)) { rie.Hvo }; // exclude `rie` and all of its subentries
			var wsIndex = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(rie.ReversalIndex.WritingSystem);
			mergeCandidates.AddRange(from rieInner in rie.ReversalIndex.AllEntries
									 where !filteredHvos.Contains(rieInner.Hvo)
									 select new DummyCmObject(rieInner.Hvo, rieInner.ShortName, wsIndex));
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
		public static Guid GetObjectGuidIfValid(IPropertyTable propertyTable, string key)
		{
			var sGuid = propertyTable.GetValue<string>(key);
			if (string.IsNullOrEmpty(sGuid))
				return Guid.Empty;

			Guid guid;
			if (!Guid.TryParse(sGuid, out guid))
				return Guid.Empty;

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
