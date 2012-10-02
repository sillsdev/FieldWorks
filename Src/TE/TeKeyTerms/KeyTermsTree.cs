// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: KeyTermsTree.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for KeyTermsTree.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyTermsTree : TreeView, IFWDisposable
	{
		#region Data members

		private ICmPossibilityList m_keyTermsList;
		private FdoCache m_cache;
		private int m_wsDefault = 0;
		private bool m_fDisplayUI;
		private Set<int> m_chkTermsWithRefs = new Set<int>();
		#endregion

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermsTree"/> class.
		/// </summary>
		/// <param name="fDisplayUI"><c>true</c> to display UI.</param>
		/// ------------------------------------------------------------------------------------
		public KeyTermsTree(bool fDisplayUI) : base()
		{
			BorderStyle = BorderStyle.None;
			this.HideSelection = false;
			m_fDisplayUI = fDisplayUI;
		}
		#endregion

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_keyTermsList = null;
			m_cache = null;

			base.Dispose(disposing);
		}

		#region public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the possibility list containing the hierarchical list of key terms
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmPossibilityList KeyTermsList
		{
			set
			{
				CheckDisposed();

				m_keyTermsList = value;
				m_cache = m_keyTermsList.Cache;
				m_wsDefault = m_cache.DefaultUserWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to display UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DisplayUI
		{
			get { return m_fDisplayUI; }
			set { m_fDisplayUI = value; }
		}
		#endregion

		#region Misc. Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the tree with key terms
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Load()
		{
			CheckDisposed();

			BeginUpdate();
			using (new WaitCursor())
			{
				Nodes.Clear();
				LoadKeyTerms();
			}

			Cursor = Cursors.Default;
		}

		List<int> m_filteredBookIds = null;
		/// <summary>
		/// The books to filter the tree by (matching on ChkRef occurrences) (TE-4500).
		/// </summary>
		internal List<int> FilteredBookIds
		{
			get { return m_filteredBookIds; }
			set { m_filteredBookIds = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the key terms.
		/// </summary>
		/// <returns>Always <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private object LoadKeyTerms()
		{
			// Pre-load all of the stuff we are going to display from the DB.
			//m_cache.LoadAllOfAnOwningVectorProp(
			//	(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities, "ChkTerm");
			//m_cache.LoadAllOfMultiUnicode((int)CmPossibility.CmPossibilityTags.kflidName,
			//	"ChkTerm");
			//m_cache.LoadAllOfMultiUnicode((int)ChkTerm.ChkTermTags.kflidSeeAlso,
			//	"ChkTerm");
			//m_cache.LoadAllOfAnIntProp((int)ChkTerm.ChkTermTags.kflidTermId);
			// see if we have a book filter enabled
			//if (HasBookFilter())
			//{
				//m_cache.LoadAllOfAnOwningVectorProp(
				//	(int)ChkTerm.ChkTermTags.kflidOccurrences, "ChkRef");
				//m_cache.LoadAllOfAnIntProp((int)ChkRef.ChkRefTags.kflidRef);

				// Alternatively, we could use the sql query below to get all the
				// the relevant key term ids, but that makes it harder for unit testing.
				// /*
				//	select distinct (occ.Src) from ChkTerm_Occurrences occ
				//	join ChkRef cr on cr.id = occ.Dst
				//	where (cr.ref >= 41000000 and cr.ref < 42000000 or -- book filters
				//	cr.ref >= 39000000 and cr.ref < 40000000)
				// */
			//}
			try
			{
				// preload whatever we need, whenever we need to.
				// clear our chkTermsWithRefs list
				m_chkTermsWithRefs.Clear();
				m_cache.EnableBulkLoadingIfPossible(true);
				foreach (int hvo in m_keyTermsList.PossibilitiesOS.HvoArray)
				{
					PopulateTreeNode(Nodes, hvo);
				}

				this.Sort();
			}
			finally
			{
				EndUpdate();
				m_cache.EnableBulkLoadingIfPossible(false);
			}


			if (Nodes.Count > 0)
				SelectedNode = Nodes[0];

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.TreeView.AfterSelect"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.TreeViewEventArgs"/> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnAfterSelect(TreeViewEventArgs e)
		{
			using (new WaitCursor(this))
			{
				base.OnAfterSelect(e);

				StringBuilder bldr = new StringBuilder();
				for (TreeNode node = e.Node; node != null; node = node.Parent)
				{
					if (bldr.Length > 0)
						bldr.Insert(0, " - ");
					bldr.Insert(0, node.Text);
				}

				Text = bldr.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.VisibleChanged"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			// After changing the parent the tooltips don't show properly. Knowledge base article
			// 241102 (http://support.microsoft.com/default.aspx?scid=kb;en-us;241102)
			// suggests the following workaround:
			int dwStyle = Win32.GetWindowLong(new HandleRef(this, Handle), Win32.GWL_STYLE);
			Win32.SetWindowLong(new HandleRef(this, Handle), Win32.GWL_STYLE,
				dwStyle | Win32.TVS_NOTOOLTIPS);
			Win32.SetWindowLong(new HandleRef(this, Handle), Win32.GWL_STYLE,
				dwStyle & ~Win32.TVS_NOTOOLTIPS);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the key terms tree node with the specified hvo.
		/// </summary>
		/// <param name="hvo">hvo of node to find.</param>
		/// <returns>The node with the specified hvo.</returns>
		/// ------------------------------------------------------------------------------------
		public TreeNode FindNodeWithHvo(int hvo)
		{
			foreach (TreeNode node in Nodes)
			{
				TreeNode tmpNode = FindNodeWithHvo(hvo, node);
				if (tmpNode != null)
					return tmpNode;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the key terms tree node with the specified hvo, starting with the specified
		/// node. This method will recurse into child nodes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private TreeNode FindNodeWithHvo(int hvoTarget, TreeNode parentNode)
		{
			if (hvoTarget < 0 || parentNode == null)
				return null;

			// Check the parent node's hvo.
			if (parentNode.Tag != null && parentNode.Tag.GetType() == typeof(int))
			{
				if ((int)parentNode.Tag == hvoTarget)
					return parentNode;
			}

			// Check the node of each child.
			foreach (TreeNode node in parentNode.Nodes)
			{
				TreeNode tmpNode = FindNodeWithHvo(hvoTarget, node);
				if (tmpNode != null)
					return tmpNode;
			}

			return null;
		}

		#endregion

		#region Other private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively populate the nodes of the tree with the key terms
		/// </summary>
		/// <param name="nodes">collection of nodes to be populated</param>
		/// <param name="keyTermHvo">Hvo of the ChkTerm which is part of the keyterm hierarchy</param>
		/// ------------------------------------------------------------------------------------
		private void PopulateTreeNode(TreeNodeCollection nodes, int keyTermHvo)
		{
			// ENHANCE: Support displaying list in different writing systems?
			int bestWs = m_wsDefault;//m_cache.LangProject.ActualWs(LangProject.kwsFirstAnalOrVern,
				//keyTermHvo, (int)CmPossibility.CmPossibilityTags.kflidName);
			string nodeName = m_cache.GetMultiUnicodeAlt(keyTermHvo,
				(int)CmPossibility.CmPossibilityTags.kflidName, bestWs, "CmPossibility_Name");

			if (string.IsNullOrEmpty(nodeName))
			{
				int wsen = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
				nodeName = m_cache.GetMultiUnicodeAlt(keyTermHvo,
					(int)CmPossibility.CmPossibilityTags.kflidName, wsen, "CmPossibility_Name");
			}

			TreeNode tn = new TreeNode(nodeName);
			tn.Tag = keyTermHvo;
			int[] subKeyTerms = m_cache.GetVectorProperty(keyTermHvo,
				(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities, false);
			bool fAddKeyTermWithOccurrences = ShouldAddLeafNode(keyTermHvo, subKeyTerms);
			foreach (int subKeyTermHvo in subKeyTerms)
			{
				PopulateTreeNode(tn.Nodes, subKeyTermHvo);
			}
			// if we added children to the node, then add the node to the parent tree collection.
			if (fAddKeyTermWithOccurrences || tn.Nodes.Count > 0)
			{
				nodes.Add(tn);
				if (fAddKeyTermWithOccurrences)
					m_chkTermsWithRefs.Add(keyTermHvo);
			}
		}

		/// <summary>
		/// Returns the set of chkTerms with references (filtered by BookFilter).
		/// NOTE: Currently only populated after calling LoadKeyTermsTree()
		/// </summary>
		internal Set<int> ChkTermsWithRefs
		{
			get { return m_chkTermsWithRefs; }
		}

		private bool ShouldAddLeafNode(int keyTermHvo, int[] subKeyTerms)
		{
			// if we have a book filter and the keyterm doesn't have subpossibilities (ie. it's a leaf node)
			// make sure this key term has an occurrence in the books specified by the book filter.
			if (subKeyTerms.Length == 0)
			{
				if (HasBookFilter())
				{
					ChkTerm chkTerm = new ChkTerm(m_cache, keyTermHvo, false, false);
					foreach (IChkRef chkRef in chkTerm.OccurrencesOS)
					{
						int bookIdOfOccurrence = ScrReference.GetBookFromBcv(chkRef.Ref);
						if (FilteredBookIds.Contains(bookIdOfOccurrence))
						{
							// the reference is in one of our filtered books
							// so add its key term to our tree.
							return true;
						}
					}
				}
				else
				{
					// no book filter to apply, so add all the key terms.
					return true;
				}
			}
			else
			{
				// return false. not a leaf-node.
			}
			return false;
		}

		private bool HasBookFilter()
		{
			return m_filteredBookIds != null && m_filteredBookIds.Count > 0;
		}
		#endregion
	}
}
