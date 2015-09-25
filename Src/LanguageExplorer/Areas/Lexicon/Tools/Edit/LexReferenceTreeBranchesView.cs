// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using System.Collections.Generic;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Summary description for LexReferenceTreeBranchesView.
	/// </summary>
	internal class LexReferenceTreeBranchesView : VectorReferenceView
	{
		/// <summary />
		public LexReferenceTreeBranchesView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary />
		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			return new LexReferenceTreeBranchesVc(m_fdoCache, m_rootFlid, m_displayNameProperty, m_displayWs);
		}

		/// <summary />
		protected override void Delete()
		{
			Delete(LanguageExplorerResources.ksUndoDeleteRef, LanguageExplorerResources.ksRedoDeleteRef);
		}

		/// <summary />
		protected override void UpdateTimeStampsIfNeeded(int[] hvos)
		{
#if WANTPORTMULTI
			for (int i = 0; i < hvos.Length; ++i)
			{
				ICmObject cmo = ICmObject.CreateFromDBObject(m_fdoCache, hvos[i]);
				(cmo as ICmObject).UpdateTimestampForVirtualChange();
			}
#endif
		}

		/// <summary>
		/// Put the hidden item back into the list of visible items to make the list that should be stored in the property.
		/// </summary>
		/// <param name="items"></param>
		protected override void AddHiddenItems(List<ICmObject> items)
		{
			var allItems = base.GetVisibleItemList();
			if (allItems.Count != 0)
				items.Insert(0, allItems[0]);
		}

		/// <summary>
		/// In a tree view the FIRST item is hidden.
		/// </summary>
		protected override List<ICmObject> GetVisibleItemList()
		{
			var result = base.GetVisibleItemList();
			result.RemoveAt(0);
			return result;
		}

		#region Component Designer generated code
		/// <summary>
		/// The Superclass handles everything except our Name property.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceTreeBranchesView";
		}
		#endregion
	}

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	internal class LexReferenceTreeBranchesVc : VectorReferenceVc
	{
		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public LexReferenceTreeBranchesVc(FdoCache cache, int flid, string displayNameProperty, string displayWs)
			: base (cache, flid, displayNameProperty, displayWs)
		{
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			ISilDataAccess da = vwenv.DataAccess;
			int count = da.get_VecSize(hvo, tag);
			// Tree Branches consist of everything FOLLOWING the first element which is the Tree root.
			for (int i = 1; i < count; ++i)
			{
				vwenv.AddObj(da.get_VecItem(hvo, tag, i), this,
					VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
		}
	}
}
