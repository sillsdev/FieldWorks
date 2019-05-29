// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Makes a hierarchical tree of possibility items, *even if the record list is flattened*
	/// </summary>
	internal class PossibilityTreeBarHandler : TreeBarHandler
	{
		/// <summary />
		public PossibilityTreeBarHandler(IPropertyTable propertyTable, bool expand, bool hierarchical, bool includeAbbr, string bestWS)
			: base(propertyTable, expand, hierarchical, includeAbbr, bestWS)
		{
		}

		protected override string GetDisplayPropertyName => m_includeAbbr ? "LongName" : base.GetDisplayPropertyName;

		public override void PopulateRecordBar(IRecordList list)
		{
			base.PopulateRecordBar(list);
			UpdateHeaderVisibility();
		}

		internal void ForcePopulateRecordBar(IRecordList list)
		{
			MyRecordList = null;
			PopulateRecordBar(list);
		}

		/// <summary>
		/// It's possible that another tree bar handler recently turned over control of the RecordBar
		/// to us, if so, we want to make sure they didn't leave the optional info bar visible.
		/// </summary>
		protected virtual void UpdateHeaderVisibility()
		{
			var window = m_propertyTable.GetValue<IFwMainWnd>(FwUtils.window);
			if (window?.RecordBarControl == null)
			{
				return;
			}
			window.RecordBarControl.ShowHeaderControl = false;
		}

		/// <summary>
		/// add any subitems to the tree. Note! This assumes that the list has been preloaded
		/// (e.g., using PreLoadList), so it bypasses normal load operations for speed purposes.
		/// Without preloading, it took almost 19,000 queries to start FW showing semantic domain
		/// list. With preloading it reduced the number to 200 queries.
		/// </summary>
		protected override void AddSubNodes(ICmObject obj, TreeNodeCollection parentsCollection)
		{
			var pss = (ICmPossibility)obj;
			foreach (var subPss in pss.SubPossibilitiesOS)
			{
				AddTreeNode(subPss, parentsCollection);
			}
		}

		/// <summary />
		/// <remarks> this is overridden because we actually need to avoid adding items from the top-level if
		/// they are not top-level possibilities. They will show up under their respective parents.in other words,
		/// if the list we are given has been flattened, we need to un-flatten it.</remarks>
		protected override bool ShouldAddNode(ICmObject obj)
		{
			var possibility = (ICmPossibility)obj;
			//don't show it if it is a child of another possibility.
			return possibility.OwningFlid != CmPossibilityTags.kflidSubPossibilities;
		}

		protected override ContextMenuStrip CreateTreebarContextMenuStrip()
		{
			var menu = base.CreateTreebarContextMenuStrip();
			if (MyRecordList.OwningObject is ICmPossibilityList && !(MyRecordList.OwningObject as ICmPossibilityList).IsSorted)
			{
				// Move up and move down items make sense
				menu.Items.Add(new DisposableToolStripMenuItem(LanguageExplorerResources.MoveUp){Name = AreaServices.MoveUp2 });
				menu.Items.Add(new DisposableToolStripMenuItem(LanguageExplorerResources.MoveDown) { Name = AreaServices.MoveDown2 });
			}
			return menu;
		}

		protected override void tree_moveUp()
		{
			MoveItem(-1);
		}
		protected override void tree_moveDown()
		{
			MoveItem(1);
		}

		/// <summary>
		/// Move the clicked item the specified distance (currently +/- 1) in its owning list.
		/// </summary>
		private void MoveItem(int distance)
		{
			var hvoMove = ClickObject;
			if (hvoMove == 0)
			{
				return;
			}
			var column = m_possRepo.GetObject(hvoMove);
			using (var columnUI = CmPossibilityUi.MakeLcmModelUiObject(column))
			{
				if (columnUI.CheckAndReportProtectedChartColumn())
				{
					return;
				}
			}
			var owner = column.Owner;
			if (owner == null) // probably not possible
			{
				return;
			}
			var hvoOwner = owner.Hvo;
			var ownFlid = column.OwningFlid;
			var oldIndex = m_cache.DomainDataByFlid.GetObjIndex(hvoOwner, ownFlid, column.Hvo);
			var newIndex = oldIndex + distance;
			if (newIndex < 0)
			{
				return;
			}
			var cobj = m_cache.DomainDataByFlid.get_VecSize(hvoOwner, ownFlid);
			if (newIndex >= cobj)
			{
				return;
			}
			// Without this, we insert it before the next object, which is the one it's already before,
			// so it doesn't move.
			if (distance > 0)
			{
				newIndex++;
			}
			UndoableUnitOfWorkHelper.Do(AreaResources.UndoMoveItem, AreaResources.RedoMoveItem, m_cache.ActionHandlerAccessor,
				() => m_cache.DomainDataByFlid.MoveOwnSeq(hvoOwner, ownFlid, oldIndex, oldIndex, hvoOwner, ownFlid, newIndex));
		}
	}
}