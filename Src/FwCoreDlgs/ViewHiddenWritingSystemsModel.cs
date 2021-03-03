// Copyright (c) 2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.WritingSystems;
using System;
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
		public readonly List<CoreWritingSystemDefinition> ShownWritingSystems = new List<CoreWritingSystemDefinition>();

		/// <summary>Hidden Writing Systems whose data the user has selected to delete permanently</summary>
		public readonly List<CoreWritingSystemDefinition> DeletedWritingSystems = new List<CoreWritingSystemDefinition>();

		/// <summary/>
		public ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType type, LcmCache cache = null)
		{
			ListType = type;
			if (cache == null)
			{
				// cache is null for some tests
				Items = new List<HiddenWSListItemModel>();
				return;
			}
			m_WSMgr = cache.ServiceLocator.WritingSystemManager;
			var alreadyInList = ListType == FwWritingSystemSetupModel.ListType.Analysis
				? cache.LangProject.AnalysisWritingSystems
				: cache.LangProject.VernacularWritingSystems;
			m_OppositeList = ListType == FwWritingSystemSetupModel.ListType.Analysis
				? cache.LangProject.VernacularWritingSystems
				: cache.LangProject.AnalysisWritingSystems;
			Items = new List<int>(WritingSystemServices.FindAllWritingSystemsWithText(cache))
				.Where(wsHandle => !alreadyInList.Contains(wsHandle)).Select(IntToListItem).ToList();
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
			Items.Remove(item);
			ShownWritingSystems.Add(item.WS);
		}

		/// <summary/>
		internal void Delete(HiddenWSListItemModel item)
		{
			if (!item.InOppositeList && ConfirmDeleteWritingSystem(item.ToString()))
			{
				Items.Remove(item);
				DeletedWritingSystems.Add(item.WS);
			}
		}
	}

	/// <summary>
	/// This class models a list item for a writing system (that is not in the list for the current type).
	/// </summary>
	internal class HiddenWSListItemModel : Tuple<CoreWritingSystemDefinition, bool>
	{
		/// <summary/>
		public HiddenWSListItemModel(CoreWritingSystemDefinition ws, bool inOppositeList) : base(ws, inOppositeList) { }

		/// <summary/>
		public CoreWritingSystemDefinition WS => Item1;

		/// <summary>
		/// Whether the Writing System is in the opposite list (e.g., if we are configuring Analysis WS's, true if this WS is in the Vernacular list)
		/// </summary>
		public bool InOppositeList => Item2;

		/// <inheritdoc />
		public override string ToString() => WS.DisplayLabel;
	}
}