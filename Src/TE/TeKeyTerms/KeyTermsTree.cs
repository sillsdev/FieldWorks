// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2005' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: KeyTermsTree.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;

using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for KeyTermsTree.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyTermsTree : TreeView, IMessageFilter
	{
		#region Data members
		private ICmPossibilityList m_keyTermsList;
		private FdoCache m_cache;
		private int m_wsDefault = 0;
		private Set<IChkTerm> m_chkTermsWithRefs = new Set<IChkTerm>();
		List<int> m_filteredBookIds = null;
		private string m_lastFindString = null;
		private TreeNode m_startingNodeForFind;
		private TreeNode m_lastFoundNode;
		#endregion

		/// <summary/>
		public enum FindResult
		{
			/// <summary/>
			MatchFound,
			/// <summary/>
			NoMoreMatches,
			/// <summary/>
			NoMatchFound,
		}

		#region Delegates
		internal Func<int, Font> GetFontForWs { get; set; }
		#endregion

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermsTree"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public KeyTermsTree(ICmPossibilityList keyTermsList)
		{
			BorderStyle = BorderStyle.None;
			HideSelection = false;
			m_keyTermsList = keyTermsList;
			m_cache = m_keyTermsList.Cache;
			m_wsDefault = m_cache.DefaultUserWs;

			GetFontForWs = (ws => Font); // By default, just use the control's font (from Designer)
		}
		#endregion

		#region public and internal Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the set of chkTerms with references (filtered by BookFilter).
		/// NOTE: Currently only populated after calling LoadKeyTermsTree()
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Set<IChkTerm> ChkTermsWithRefs
		{
			get { return m_chkTermsWithRefs; }
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
			Font = GetFontForWs(m_wsDefault);
			BeginUpdate();
			using (new WaitCursor())
			{
				Nodes.Clear();
				LoadKeyTerms();
			}

			Cursor = Cursors.Default;
		}

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
		private void LoadKeyTerms()
		{
			try
			{
				// preload whatever we need, whenever we need to.
				// clear our chkTermsWithRefs list
				m_chkTermsWithRefs.Clear();
				foreach (ICmPossibility poss in m_keyTermsList.PossibilitiesOS)
				{
					PopulateTreeNode(Nodes, poss);
				}
				Sort();
			}
			finally
			{
				EndUpdate();
			}

			if (Nodes.Count > 0)
				SelectedNode = Nodes[0];
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
			if (hvo < 0)
				return null;

			return AllNodes.Where(treeNode => treeNode.Tag != null &&
				treeNode.Tag.GetType() == typeof(int)).FirstOrDefault(treeNode => (int)treeNode.Tag == hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds and selects the "next" (starting from the currently selected node) node for a
		/// term whose name, description, or see also contains text matching the given string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal FindResult FindNextMatch(string s)
		{
			s = s.Normalize(NormalizationForm.FormD);

			if (m_lastFindString != s || m_lastFoundNode != SelectedNode)
			{
				m_lastFindString = s;
				m_startingNodeForFind = SelectedNode;
				m_lastFoundNode = null;
			}

			if (m_startingNodeForFind == m_lastFoundNode)
				return FindResult.NoMoreMatches;

			Func<TreeNode, bool> Matches = node =>
			{
				if (node.Text.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0)
					return true;
				IChkTerm term = node.Tag as IChkTerm;
				return (term != null && (term.Name.OccursInAnyAlternative(s) ||
					term.Description.OccursInAnyAlternative(s) ||
					term.SeeAlso.OccursInAnyAlternative(s)));
			};

			bool hitStartingNode = false;
			bool hitLastFoundNode = false;
			TreeNode foundNode = null;
			foreach (TreeNode node in AllNodes)
			{
				if (node == m_startingNodeForFind)
				{
					if (hitLastFoundNode)
						break;
					hitStartingNode = true;
				}
				else if (node == m_lastFoundNode)
				{
					if (!hitStartingNode)
						foundNode = null;
					hitLastFoundNode = true;
				}
				else if (Matches(node))
				{
					bool foundFirstMatchBeforeStart = (foundNode == null && !hitStartingNode);
					bool foundWhatWeWereLookingFor = hitLastFoundNode || (hitStartingNode && m_lastFoundNode == null);
					if (foundFirstMatchBeforeStart || foundWhatWeWereLookingFor)
					{
						foundNode = node;
						if (hitStartingNode)
							break;
					}
				}
			}
			if (foundNode == null && m_startingNodeForFind != m_lastFoundNode && Matches(m_startingNodeForFind))
				foundNode = m_startingNodeForFind;

			if (foundNode == null)
			{
				bool noMatchFound = m_lastFoundNode == null && !Matches(SelectedNode);
				m_lastFoundNode = null;
				return noMatchFound ? FindResult.NoMatchFound : FindResult.NoMoreMatches;
			}

			m_lastFoundNode = foundNode;
			SelectedNode = foundNode;
			return FindResult.MatchFound;
		}
		#endregion

		#region Other private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets every stinking tree node recursively.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected IEnumerable<TreeNode> AllNodes
		{
			get { return Nodes.Cast<TreeNode>().SelectMany(node => GetAllNodes(node)); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets every stinking tree node recursively.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IEnumerable<TreeNode> GetAllNodes(TreeNode node)
		{
			yield return node;
			foreach (TreeNode n in node.Nodes.Cast<TreeNode>().SelectMany(subNode => GetAllNodes(subNode)))
				yield return n;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively populate the nodes of the tree with the key terms
		/// </summary>
		/// <param name="nodes">collection of nodes to be populated</param>
		/// <param name="keyTerm">The ChkTerm which is part of the keyterm hierarchy</param>
		/// ------------------------------------------------------------------------------------
		private void PopulateTreeNode(TreeNodeCollection nodes, ICmPossibility keyTerm)
		{
			// ENHANCE (TE-9407): Support displaying list in two writing systems (primary and secondary)
			int ws;
			string nodeName = keyTerm.Name.GetBestAlternative(out ws, m_wsDefault,
				WritingSystemServices.kwsFirstAnal, m_cache.WritingSystemFactory.GetWsFromStr("grk"),
				m_cache.WritingSystemFactory.GetWsFromStr("hbo")).Text;

			TreeNode tn = new TreeNode(nodeName);
			if (ws != m_wsDefault)
				tn.NodeFont = GetFontForWs(ws);
			tn.Tag = keyTerm;
			IFdoOwningSequence<ICmPossibility> subKeyTerms = keyTerm.SubPossibilitiesOS;
			bool fAddKeyTermWithOccurrences = ShouldAddLeafNode(keyTerm, subKeyTerms);
			foreach (ICmPossibility subKeyTerm in subKeyTerms)
			{
				PopulateTreeNode(tn.Nodes, subKeyTerm);
			}
			// if we added children to the node, then add the node to the parent tree collection.
			if (fAddKeyTermWithOccurrences || tn.Nodes.Count > 0)
			{
				nodes.Add(tn);
				if (fAddKeyTermWithOccurrences)
					m_chkTermsWithRefs.Add((IChkTerm)keyTerm);
			}
		}

		private bool ShouldAddLeafNode(ICmPossibility keyTerm, IFdoOwningSequence<ICmPossibility> subKeyTerms)
		{
			// if we have a book filter and the keyterm doesn't have subpossibilities (ie. it's a leaf node)
			// make sure this key term has an occurrence in the books specified by the book filter.
			if (subKeyTerms.Count == 0)
			{
				if (HasBookFilter)
				{
					IChkTerm chkTerm = (IChkTerm)keyTerm;
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

		private bool HasBookFilter
		{
			get { return m_filteredBookIds != null && m_filteredBookIds.Count > 0; }
		}
		#endregion

		#region Implementation of IMessageFilter
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Catches F3 key to attempt to find the next key term using the last match string.
		/// </summary>
		/// <param name="m">The message to be dispatched. You cannot modify this message.</param>
		/// <returns>
		/// true to filter the message and stop it from being dispatched; false to allow the
		/// message to continue to the next filter or control.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message m)
		{
			// Ignore the message if we're not handling a key down message.
			if (m.Msg != (int)Win32.WinMsgs.WM_SYSKEYDOWN && m.Msg != (int)Win32.WinMsgs.WM_KEYDOWN)
				return false;

			Keys key = ((Keys)(int)m.WParam & Keys.KeyCode);

			if (key != Keys.F3 || string.IsNullOrEmpty(m_lastFindString))
				return false;

			if (FindNextMatch(m_lastFindString) != FindResult.MatchFound)
				SystemSounds.Beep.Play();
			return true;
		}
		#endregion
	}
}
