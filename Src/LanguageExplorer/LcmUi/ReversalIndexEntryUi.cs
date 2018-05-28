// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
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
		public ReversalIndexEntryUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IReversalIndexEntry);
		}

		internal ReversalIndexEntryUi() {}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = LcmUiStrings.ksMergeReversalEntry;
			wp.m_label = LcmUiStrings.ksEntries;

			var rie = (IReversalIndexEntry) MyCmObject;
			var filteredHvos = new HashSet<int>(rie.AllOwnedObjects.Select(obj => obj.Hvo)) { rie.Hvo }; // exclude `rie` and all of its subentries
			var wsIndex = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(rie.ReversalIndex.WritingSystem);
			mergeCandidates.AddRange(rie.ReversalIndex.AllEntries.Where(rieInner => !filteredHvos.Contains(rieInner.Hvo)).Select(rieInner => new DummyCmObject(rieInner.Hvo, rieInner.ShortName, wsIndex)));
#if RANDYTODO
			// TODO: Use this xml, instead of 'guiControl'.
/*
			<guicontrol id="MergeReversalEntryList">
				<parameters id="mergeReversalEntryList" listItemsClass="ReversalIndexEntry" filterBar="false" treeBarAvailability="NotAllowed" defaultCursor="Arrow"
					hscroll="true" editable="false" selectColumn="false">
					<columns>
						<column label="Entry" width="100%" layout="ReversalForm" ws="$ws=reversal"/>
					</columns>
				</parameters>
			</guicontrol>
*/
#endif
			guiControl = "MergeReversalEntryList";
			helpTopic = "khtpMergeReversalEntry";
			return new DummyCmObject(m_hvo, rie.ShortName, wsIndex);
		}

		/// <summary>
		/// Fetches the GUID value of the given property, having checked it is a valid object.
		/// If it is not a valid object, the property is removed.
		/// </summary>
		public static Guid GetObjectGuidIfValid(IPropertyTable propertyTable, string key)
		{
			var sGuid = propertyTable.GetValue<string>(key);
			if (string.IsNullOrEmpty(sGuid))
			{
				return Guid.Empty;
			}

			Guid guid;
			if (!Guid.TryParse(sGuid, out guid))
			{
				return Guid.Empty;
			}

			var cache = propertyTable.GetValue<LcmCache>("cache");
			if (cache.ServiceLocator.ObjectRepository.IsValidObjectId(guid))
			{
				return guid;
			}
			propertyTable.RemoveProperty(key);
			return Guid.Empty;
		}
	}
}
