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
	/// but that are not in the list for the current type (but may be in the opposite list). Allows users to show
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

		/// <summary>Hidden Writing Systems that the user has selected to show in the list for the current type</summary>
		public IEnumerable<CoreWritingSystemDefinition> ShownWritingSystems => Items.Where(i => i.WillShow).Select(i => i.WS);

		/// <summary>Hidden Writing Systems whose data the user has selected to delete permanently</summary>
		public IEnumerable<CoreWritingSystemDefinition> DeletedWritingSystems => Items.Where(i => i.WillDelete).Select(i => i.WS);

		/// <summary/>
		public ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType type, LcmCache cache = null,
			ICollection<CoreWritingSystemDefinition> alreadyShowing = null, ICollection<CoreWritingSystemDefinition> alreadyDeleted = null)
		{
			ListType = type;
			if (cache == null)
			{
				// cache is null for some tests
				Items = new List<HiddenWSListItemModel>();
				return;
			}
			m_WSMgr = cache.ServiceLocator.WritingSystemManager;
			alreadyShowing = alreadyShowing ?? (ListType == FwWritingSystemSetupModel.ListType.Analysis
				? cache.LangProject.AnalysisWritingSystems
				: cache.LangProject.VernacularWritingSystems);
			m_OppositeList = ListType == FwWritingSystemSetupModel.ListType.Analysis
				? cache.LangProject.VernacularWritingSystems
				: cache.LangProject.AnalysisWritingSystems;
			Items = new List<int>(WritingSystemServices.FindAllWritingSystemsWithText(cache))
				.Where(wsHandle => !alreadyShowing.Contains(wsHandle)).Select(IntToListItem).ToList();
			if (alreadyDeleted != null)
			{
				foreach (var deletedItem in Items.Where(i => alreadyDeleted.Any(ws => i.WS.Id == ws.Id)))
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
		internal void Show(HiddenWSListItemModel item)
		{
			item.WillShow = true;
		}

		/// <summary/>
		internal void Delete(HiddenWSListItemModel item)
		{
			if (!item.InOppositeList && ConfirmDeleteWritingSystem(item.ToString()))
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
		private enum Disposition { Hide, Show, Delete }

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

		public bool WillShow
		{
			get => m_disposition == Disposition.Show;
			set
			{
				if (value)
				{
					m_disposition = Disposition.Show;
				}
				else if (m_disposition == Disposition.Show)
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
				case Disposition.Show:
					return string.Format(FwCoreDlgs.XWillBeShown, label);
				case Disposition.Delete:
					return string.Format(FwCoreDlgs.XWillBeDeleted, label);
			}
		}
	}
}