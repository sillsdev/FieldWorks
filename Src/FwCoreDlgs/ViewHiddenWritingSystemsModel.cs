// Copyright (c) 2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.WritingSystems;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Presentation model for the ViewHiddenWritingSystems dialog. Lists Writing Systems with text in the project
	/// but that are not in the list for the current type (but may be in the opposite list). Allows users to add
	/// a Writing Systems or to delete all text in a Writing System.
	/// </summary>
	public class ViewHiddenWritingSystemsModel
	{
		/// <summary/>
		public FwWritingSystemSetupModel.ConfirmDeleteWritingSystemDelegate ConfirmDeleteWritingSystem;

		/// <summary/>
		public readonly FwWritingSystemSetupModel.ListType ListType;

		private readonly WritingSystemManager m_WSMgr;

		private readonly ICollection<CoreWritingSystemDefinition> m_OppositeList;

		/// <summary>Writing Systems with data that are not in the list for the current type</summary>
		internal readonly List<HiddenWSListItemModel> Items;

		/// <summary>Hidden Writing Systems that the user has selected to add to the list for the current type</summary>
		public IEnumerable<CoreWritingSystemDefinition> AddedWritingSystems => Items.Where(i => i.WillAdd).Select(i => i.WS);

		/// <summary>Hidden Writing Systems whose data the user has selected to delete permanently</summary>
		public IEnumerable<CoreWritingSystemDefinition> DeletedWritingSystems => Items.Where(i => i.WillDelete).Select(i => i.WS);

		/// <summary/>
		public ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType type, LcmCache cache = null,
			ICollection<CoreWritingSystemDefinition> alreadyInList = null, ICollection<string> alreadyDeletedIds = null)
		{
			ListType = type;
			if (cache == null)
			{
				// cache is null for some tests
				Items = new List<HiddenWSListItemModel>();
				return;
			}
			m_WSMgr = cache.ServiceLocator.WritingSystemManager;
			alreadyInList = alreadyInList ?? (ListType == FwWritingSystemSetupModel.ListType.Analysis
				? cache.LangProject.AnalysisWritingSystems
				: cache.LangProject.VernacularWritingSystems);
			m_OppositeList = ListType == FwWritingSystemSetupModel.ListType.Analysis
				? cache.LangProject.VernacularWritingSystems
				: cache.LangProject.AnalysisWritingSystems;
			Items = WritingSystemServices.FindAllWritingSystemsWithText(cache)
				.Where(wsHandle => !alreadyInList.Contains(wsHandle)).Select(IntToListItem).ToList();
			if (alreadyDeletedIds != null)
			{
				foreach (var deletedItem in Items.Where(i => alreadyDeletedIds.Any(wsId => i.WS.Id == wsId)))
				{
					// This will add a note to the item's display label
					deletedItem.WillDelete = true;
				}
			}
		}

		/// <summary>
		/// Converts a WS Handle to a HiddenWSListItemModel for that WS.
		/// </summary>
		internal HiddenWSListItemModel IntToListItem(int wsHandle)
		{
			return new HiddenWSListItemModel(m_WSMgr.Get(wsHandle), m_OppositeList.Contains(wsHandle));
		}

		/// <summary/>
		internal void Add(HiddenWSListItemModel item)
		{
			item.WillAdd = true;
		}

		/// <summary/>
		internal void Delete(HiddenWSListItemModel item)
		{
			if (!item.InOppositeList && ConfirmDeleteWritingSystem(item.WS.DisplayLabel))
			{
				item.WillDelete = true;
			}
		}
	}

	/// <summary>
	/// This class models a list item for a writing system (that is not in the list for the current type).
	/// </summary>
	internal class HiddenWSListItemModel
	{
		private enum Disposition { Hide, Add, Delete }

		private Disposition m_disposition = Disposition.Hide;

		/// <summary/>
		public HiddenWSListItemModel(CoreWritingSystemDefinition ws, bool inOppositeList)
		{
			WS = ws;
			InOppositeList = inOppositeList;
		}

		/// <summary/>
		public CoreWritingSystemDefinition WS { get; }

		/// <summary>
		/// Whether the Writing System is in the opposite list (e.g., if we are configuring Analysis WS's, true if this WS is in the Vernacular list)
		/// </summary>
		public bool InOppositeList { get; }

		public bool WillAdd
		{
			get => m_disposition == Disposition.Add;
			set
			{
				if (value)
				{
					m_disposition = Disposition.Add;
				}
				else if (m_disposition == Disposition.Add)
				{
					m_disposition = Disposition.Hide;
				}
			}
		}

		public bool WillDelete
		{
			get => m_disposition == Disposition.Delete;
			set
			{
				if (value)
				{
					m_disposition = Disposition.Delete;
				}
				else if (m_disposition == Disposition.Delete)
				{
					m_disposition = Disposition.Hide;
				}
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return FormatDisplayLabel("other");
		}

		public string FormatDisplayLabel(string otherListType)
		{
			var label = $"[{WS.Abbreviation}] {WS.DisplayLabel}";
			if (InOppositeList)
			{
				label = string.Format(FwCoreDlgs.XInTheXList, label, otherListType);
			}
			switch (m_disposition)
			{
				default:
					return label;
				case Disposition.Add:
					return string.Format(FwCoreDlgs.XWillBeAdded, label);
				case Disposition.Delete:
					return string.Format(FwCoreDlgs.XWillBeDeleted, label);
			}
		}
	}
}