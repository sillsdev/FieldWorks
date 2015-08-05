using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.IText
{
	public partial class ComplexConcControl : ConcordanceControlBase, IFocusablePanePortion
	{
		private enum ComplexConcordanceInsertType
		{
			Word,
			Morph,
			TextTag,
			Or,
			WordBoundary
		};

		private static string GetOptionString(ComplexConcordanceInsertType type)
		{
			switch (type)
			{
				case ComplexConcordanceInsertType.Morph:
					return ITextStrings.ksComplexConcMorph;

				case ComplexConcordanceInsertType.Word:
					return ITextStrings.ksComplexConcWord;

				case ComplexConcordanceInsertType.TextTag:
					return ITextStrings.ksComplexConcTag;

				case ComplexConcordanceInsertType.Or:
					return "OR";

				case ComplexConcordanceInsertType.WordBoundary:
					return string.Format("{0} (#)", ITextStrings.ksComplexConcWordBoundary);
			}

			return null;
		}

		private class InsertOption
		{
			private readonly ComplexConcordanceInsertType m_type;

			public InsertOption(ComplexConcordanceInsertType type)
			{
				m_type = type;
			}

			public ComplexConcordanceInsertType Type
			{
				get { return m_type; }
			}

			public override string ToString()
			{
				return GetOptionString(m_type);
			}
		}

		private ComplexConcPatternModel m_patternModel;

		public ComplexConcControl()
		{
			InitializeComponent();
		}

		public override string AccName
		{
			get
			{
				CheckDisposed();
				return "Common.Controls.ComplexConcControl";
			}
		}

		public override void Init(Mediator mediator, IPropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();
			base.Init(mediator, propertyTable, configurationParameters);

			var pattern = m_propertyTable.GetValue<ComplexConcGroupNode>("ComplexConcPattern");
			if (pattern == null)
			{
				pattern = new ComplexConcGroupNode();
				m_propertyTable.SetProperty("ComplexConcPattern", pattern, false, true);
			}
			m_patternModel = new ComplexConcPatternModel(m_cache, pattern);

			m_view.Init(m_mediator, m_propertyTable, this);
			m_insertControl.AddOption(new InsertOption(ComplexConcordanceInsertType.Morph), CanAddMorph);
			m_insertControl.AddOption(new InsertOption(ComplexConcordanceInsertType.Word), CanAddConstraint);
			m_insertControl.AddOption(new InsertOption(ComplexConcordanceInsertType.TextTag), CanAddConstraint);
			m_insertControl.AddOption(new InsertOption(ComplexConcordanceInsertType.Or), CanAddOr);
			m_insertControl.AddOption(new InsertOption(ComplexConcordanceInsertType.WordBoundary), CanAddWordBoundary);
			UpdateViewHeight();
			m_view.RootBox.MakeSimpleSel(false, true, false, true);
		}

		public ComplexConcPatternModel PatternModel
		{
			get { return m_patternModel; }
		}

		private bool CanAddMorph(object option)
		{
			if (m_patternModel.Root.IsLeaf)
				return true;

			SelectionHelper sel = SelectionHelper.Create(m_view);
			if (sel.IsRange)
				return true;

			ComplexConcPatternNode anchorNode = GetNode(sel, SelectionHelper.SelLimitType.Anchor);
			ComplexConcPatternNode endNode = GetNode(sel, SelectionHelper.SelLimitType.End);
			return anchorNode != null && endNode != null && anchorNode.Parent == endNode.Parent;
		}

		private bool CanAddConstraint(object option)
		{
			if (m_patternModel.Root.IsLeaf)
				return true;

			SelectionHelper sel = SelectionHelper.Create(m_view);
			ComplexConcPatternNode anchorNode = GetNode(sel, SelectionHelper.SelLimitType.Anchor);
			ComplexConcPatternNode endNode = GetNode(sel, SelectionHelper.SelLimitType.End);
			if (anchorNode == null || endNode == null || anchorNode.Parent != endNode.Parent)
				return false;

			ComplexConcPatternNode parent;
			int start, end;
			if (!sel.IsRange)
			{
				int index;
				GetInsertionIndex(sel, out parent, out index);
				start = index - 1;
				end = index;
			}
			else
			{
				parent = anchorNode.Parent;
				start = (sel.Selection.EndBeforeAnchor ? GetNodeIndex(endNode) : GetNodeIndex(anchorNode)) - 1;
				end = (sel.Selection.EndBeforeAnchor ? GetNodeIndex(anchorNode) : GetNodeIndex(endNode)) + 1;
			}

			return (start == -1 || !(parent.Children[start] is ComplexConcWordBdryNode)) && (end == parent.Children.Count || !(parent.Children[end] is ComplexConcWordBdryNode));
		}

		private bool CanAddOr(object option)
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			if (sel.IsRange)
				return false;
			ComplexConcPatternNode parent;
			int index;
			GetInsertionIndex(sel, out parent, out index);
			if (index == 0)
				return false;

			return (!(parent.Children[index - 1] is ComplexConcOrNode) && !(parent.Children[index - 1] is ComplexConcWordBdryNode))
				&& (index == parent.Children.Count || (!(parent.Children[index] is ComplexConcOrNode) && !(parent.Children[index] is ComplexConcWordBdryNode)));
		}

		private bool CanAddWordBoundary(object option)
		{
			if (m_patternModel.Root.IsLeaf)
				return true;

			SelectionHelper sel = SelectionHelper.Create(m_view);
			if (sel.IsRange)
				return false;
			ComplexConcPatternNode parent;
			int index;
			GetInsertionIndex(sel, out parent, out index);
			if (parent.IsLeaf)
				return false;

			if (index == 0)
				return parent.Children[index] is ComplexConcMorphNode;

			if (index == parent.Children.Count)
				return parent.Children[index - 1] is ComplexConcMorphNode;

			return parent.Children[index - 1] is ComplexConcMorphNode && parent.Children[index] is ComplexConcMorphNode;
		}

		private void ParseUnparsedParagraphs()
		{
			var concDecorator = ConcDecorator;
			IStTxtPara[] needsParsing = concDecorator.InterestingTexts.SelectMany(txt => txt.ParagraphsOS).Cast<IStTxtPara>().Where(para => !para.ParseIsCurrent).ToArray();
			if (needsParsing.Length > 0)
			{
				NonUndoableUnitOfWorkHelper.DoSomehow(m_cache.ActionHandlerAccessor,
					() =>
					{
						foreach (var para in needsParsing)
							ParagraphParser.ParseParagraph(para);
					});
			}
		}

		protected override List<IParaFragment> SearchForMatches()
		{
			var matches = new List<IParaFragment>();
			if (m_patternModel.IsPatternEmpty)
				return matches;

			using (new WaitCursor(this))
			{
				m_patternModel.Compile();

				ParseUnparsedParagraphs();
				foreach (IStText text in ConcDecorator.InterestingTexts)
					matches.AddRange(m_patternModel.Search(text));
			}

			return matches;
		}

		private ComplexConcPatternNode GetNode(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (sel == null || m_patternModel.Root.IsLeaf)
				return null;

			SelLevInfo[] levels = sel.GetLevelInfo(limit);
			if (levels.Length == 0)
				return null;

			SelLevInfo level = levels.First(l => l.tag == ComplexConcPatternSda.ktagChildren);
			return m_patternModel.GetNode(level.hvo);
		}

		public ComplexConcPatternNode[] CurrentNodes
		{
			get
			{
				CheckDisposed();
				SelectionHelper sel = SelectionHelper.Create(m_view);
				ComplexConcPatternNode anchorNode = GetNode(sel, SelectionHelper.SelLimitType.Anchor);
				ComplexConcPatternNode endNode = GetNode(sel, SelectionHelper.SelLimitType.End);
				if (anchorNode == null || endNode == null || anchorNode.Parent != endNode.Parent)
					return new ComplexConcPatternNode[0];

				int anchorIndex = GetNodeIndex(anchorNode);
				int endIndex = GetNodeIndex(endNode);

				int index1, index2;
				if (anchorIndex < endIndex)
				{
					index1 = anchorIndex;
					index2 = endIndex;
				}
				else
				{
					index1 = endIndex;
					index2 = anchorIndex;
				}

				int j = 0;
				var nodes = new ComplexConcPatternNode[index2 - index1 + 1];
				for (int i = index1; i <= index2; i++)
					nodes[j++] = anchorNode.Parent.Children[i];
				return nodes;
			}
		}

		/// <summary>
		/// Removes items. This is called by the view when a delete or backspace button is pressed.
		/// </summary>
		/// <param name="forward">if <c>true</c> the delete button was pressed, otherwise backspace was pressed</param>
		public void RemoveNodes(bool forward)
		{
			CheckDisposed();

			if (m_patternModel.Root.IsLeaf)
				return;

			SelectionHelper sel = SelectionHelper.Create(m_view);
			ComplexConcPatternNode parent = null;
			int index = -1;
			if (sel.IsRange)
			{
				ComplexConcPatternNode[] nodes = CurrentNodes;
				if (nodes.Length > 0)
				{
					parent = nodes[0].Parent;
					index = GetNodeIndex(nodes[0]);
					foreach (ComplexConcPatternNode node in nodes)
						node.Parent.Children.Remove(node);
				}
			}
			else
			{
				ComplexConcPatternNode n = GetNode(sel, SelectionHelper.SelLimitType.Top);
				parent = n.Parent;
				index = GetNodeIndex(n);
				var tss = sel.GetTss(SelectionHelper.SelLimitType.Anchor);
				// if the current ich is at the end of the current string, then we can safely assume
				// we are at the end of the current item, so remove it or the next item based on what
				// key was pressed, otherwise we are in the middle in which
				// case the entire item is selected, or at the beginning, so we remove it or the previous
				// item based on what key was pressed
				if (sel.IchAnchor == tss.Length)
				{
					if (forward)
					{
						if (index == n.Parent.Children.Count - 1)
							index = -1;
						else
							index++;
					}
				}
				else
				{
					if (!forward)
						index--;
				}

				if (index != -1)
					parent.Children.RemoveAt(index);
			}

			if (parent != null && index != -1)
			{
				if (!parent.IsLeaf)
				{
					bool isFirstBdry = parent.Children[0] is ComplexConcWordBdryNode;
					if ((parent.Children.Count == 1 && isFirstBdry)
						|| (parent.Children.Count > 1 && isFirstBdry && !(parent.Children[1] is ComplexConcMorphNode)))
					{
						parent.Children.RemoveAt(0);
						if (index > 0)
							index--;
					}
				}
				if (parent.Children.Count > 1 && parent.Children[parent.Children.Count - 1] is ComplexConcWordBdryNode
					&& !(parent.Children[parent.Children.Count - 2] is ComplexConcMorphNode))
				{
					parent.Children.RemoveAt(parent.Children.Count - 1);
					if (index >= parent.Children.Count)
						index--;
				}
				for (int i = parent.Children.Count - 1; i > 0 ; i--)
				{
					if (parent.Children[i] is ComplexConcWordBdryNode)
					{
						if (parent.Children[i - 1] is ComplexConcWordBdryNode
							|| (!(parent.Children[i - 1] is ComplexConcMorphNode) || (i + 1 < parent.Children.Count && !(parent.Children[i + 1] is ComplexConcMorphNode))))
						{
							parent.Children.RemoveAt(i);
							if (index > i)
								index--;
						}
					}
				}

				if (!parent.IsLeaf && parent.Children[0] is ComplexConcOrNode)
				{
					parent.Children.RemoveAt(0);
					if (index > 0)
						index--;
				}
				if (!parent.IsLeaf && parent.Children[parent.Children.Count - 1] is ComplexConcOrNode)
				{
					parent.Children.RemoveAt(parent.Children.Count - 1);
					if (index >= parent.Children.Count)
						index--;
				}
				for (int i = parent.Children.Count - 1; i > 0 ; i--)
				{
					if (parent.Children[i] is ComplexConcOrNode && parent.Children[i - 1] is ComplexConcOrNode)
					{
						parent.Children.RemoveAt(i);
						if (index > i)
							index--;
					}
				}

				if (parent.Parent != null && parent.Children.Count == 1)
				{
					ComplexConcPatternNode p = parent.Parent;
					int parentIndex = GetNodeIndex(parent);
					p.Children.RemoveAt(parentIndex);
					p.Children.Insert(parentIndex, parent.Children[0]);
					index = index == 1 ? parentIndex + 1 : parentIndex;
				}
				else
				{
					while (parent.Parent != null && parent.IsLeaf)
					{
						ComplexConcPatternNode p = parent.Parent;
						index = GetNodeIndex(parent);
						p.Children.Remove(parent);
						parent = p;
					}
				}

				if (index >= parent.Children.Count)
					ReconstructView(parent, parent.Children.Count - 1, false);
				else
					ReconstructView(parent, index, true);
			}
		}

		private void ReconstructView(ComplexConcPatternNode node, bool initial)
		{
			ReconstructView(node.Parent, GetNodeIndex(node), initial);
		}

		/// <summary>
		/// Reconstructs the view and moves the cursor the specified position.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index">Index of the item in the cell.</param>
		/// <param name="initial">if <c>true</c> move the cursor to the beginning of the specified item, otherwise it is moved to the end</param>
		private void ReconstructView(ComplexConcPatternNode parent, int index, bool initial)
		{
			m_view.RootBox.Reconstruct();
			SelectAt(parent, index, initial, true, true);
		}

		private SelLevInfo[] GetLevelInfo(ComplexConcPatternNode parent, int index)
		{
			var levels = new List<SelLevInfo>();
			if (!m_patternModel.Root.IsLeaf)
			{
				ComplexConcPatternNode node = parent;
				int i = index;
				while (node != null)
				{
					levels.Add(new SelLevInfo {tag = ComplexConcPatternSda.ktagChildren, ihvo = i});
					i = GetNodeIndex(node);
					node = node.Parent;
				}
			}
			return levels.ToArray();
		}

		private void SelectLeftBoundary(ComplexConcPatternNode parent, int index, bool install)
		{
			try
			{
				SelLevInfo[] levels = GetLevelInfo(parent, index);
				m_view.RootBox.MakeTextSelection(0, levels.Length, levels, ComplexConcPatternVc.ktagLeftBoundary, 0, 0, 0,
					0, false, -1, null, install);
			}
			catch
			{
			}
		}

		private void SelectRightBoundary(ComplexConcPatternNode parent, int index, bool install)
		{
			try
			{
				SelLevInfo[] levels = GetLevelInfo(parent, index);
				m_view.RootBox.MakeTextSelection(0, levels.Length, levels, ComplexConcPatternVc.ktagRightBoundary, 0, 1, 1,
					0, false, -1, null, install);
			}
			catch
			{
			}
		}

		/// <summary>
		/// Moves the cursor to the specified position in the specified cell.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="index">Index of the item in the cell.</param>
		/// <param name="initial">if <c>true</c> move the cursor to the beginning of the specified item, otherwise it is moved to the end</param>
		/// <param name="editable">if <c>true</c> move the cursor to the first editable position</param>
		/// <param name="install">if <c>true</c> install the selection</param>
		/// <returns>The new selection</returns>
		private IVwSelection SelectAt(ComplexConcPatternNode parent, int index, bool initial, bool editable, bool install)
		{
			SelLevInfo[] levels = GetLevelInfo(parent, index);
			return m_view.RootBox.MakeTextSelInObj(0, levels.Length, levels, 0, null, initial, editable, false, false, install);
		}

		/// <summary>
		/// Update the new selection. This is called by rule formula view when selection changes.
		/// </summary>
		/// <param name="prootb">The root box.</param>
		/// <param name="vwselNew">The new selection.</param>
		public virtual void UpdateSelection(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			SelectionHelper sel = SelectionHelper.Create(vwselNew, m_view);
			if (sel != null)
				AdjustSelection(sel);
			// since the context has changed update the display options on the insertion control
			m_insertControl.UpdateOptionsDisplay();
		}

		/// <summary>
		/// Adjusts the selection.
		/// </summary>
		/// <param name="sel">The selection.</param>
		private void AdjustSelection(SelectionHelper sel)
		{
			IVwSelection anchorSel;
			int curHvo, curIch, curTag;
			// anchor IP
			if (!GetSelectionInfo(sel, SelectionHelper.SelLimitType.Anchor, out anchorSel, out curHvo, out curIch, out curTag))
				return;

			IVwSelection endSel;
			int curEndHvo, curEndIch, curEndTag;
			// end IP
			if (!GetSelectionInfo(sel, SelectionHelper.SelLimitType.End, out endSel, out curEndHvo, out curEndIch, out curEndTag))
				return;

			// create range selection
			IVwSelection vwSel = m_view.RootBox.MakeRangeSelection(anchorSel, endSel, false);
			if (vwSel != null)
			{
				ITsString tss;
				int ws;
				bool prev;

				// only install the adjusted selection if it is different then the current selection
				int wholeHvo, wholeIch, wholeTag, wholeEndHvo, wholeEndIch, wholeEndTag;
				vwSel.TextSelInfo(false, out tss, out wholeIch, out prev, out wholeHvo, out wholeTag, out ws);
				vwSel.TextSelInfo(true, out tss, out wholeEndIch, out prev, out wholeEndHvo, out wholeEndTag, out ws);

				if (wholeHvo != curHvo || wholeEndHvo != curEndHvo || wholeIch != curIch || wholeEndIch != curEndIch
					|| wholeTag != curTag || wholeEndTag != curEndTag)
					vwSel.Install();
			}
		}

		private int GetNodeIndex(ComplexConcPatternNode node)
		{
			if (node.Parent == null)
				return 0;
			return node.Parent.Children.IndexOf(node);
		}

		/// <summary>
		/// Creates a selection IP for the specified limit.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="vwSel">The new selection.</param>
		/// <param name="curHvo">The current hvo.</param>
		/// <param name="curIch">The current ich.</param>
		/// <param name="curTag">The current tag.</param>
		/// <returns><c>true</c> if we want to create a range selection, otherwise <c>false</c></returns>
		private bool GetSelectionInfo(SelectionHelper sel, SelectionHelper.SelLimitType limit, out IVwSelection vwSel,
			out int curHvo, out int curIch, out int curTag)
		{
			vwSel = null;
			curHvo = 0;
			curIch = -1;
			curTag = -1;

			ComplexConcPatternNode node = GetNode(sel, limit);
			if (node == null)
				return false;

			ITsString curTss;
			int ws;
			bool prev;

			sel.Selection.TextSelInfo(limit == SelectionHelper.SelLimitType.End, out curTss, out curIch, out prev, out curHvo, out curTag, out ws);

			int index = GetNodeIndex(node);

			if (!sel.IsRange)
			{
				// if the current selection is an IP, check if it is in one of the off-limits areas, and move the IP
				if (curIch == 0 && curTag == ComplexConcPatternVc.ktagLeftNonBoundary)
				{
					// the cursor is at a non-selectable left edge of an item, so
					// move to the selectable left edge
					SelectLeftBoundary(node.Parent, index, true);
					return false;
				}
				if (curIch == curTss.Length && curTag == ComplexConcPatternVc.ktagLeftNonBoundary)
				{
					// the cursor has been moved to the left from the left boundary, so move the
					// cursor to the previous item in the cell or the previous cell
					if (index > 0)
					{
						SelectAt(node.Parent, index - 1, false, true, true);
					}
					else
					{
						SelectLeftBoundary(node.Parent, index, true);
					}
					return false;
				}
				if (curIch == curTss.Length && curTag == ComplexConcPatternVc.ktagRightNonBoundary)
				{
					// the cursor is at a non-selectable right edge of an item, so move to the
					// selectable right edge
					SelectRightBoundary(node.Parent, index, true);
					return false;
				}
				if (curIch == 0 && curTag == ComplexConcPatternVc.ktagRightNonBoundary)
				{
					// the cursor has been moved to the right from the right boundary, so move the
					// cursor to the next item in the cell or the next cell
					if (index < node.Parent.Children.Count - 1)
					{
						SelectAt(node.Parent, index + 1, true, true, true);
					}
					else
					{
						SelectRightBoundary(node.Parent, index, true);
					}
					return false;
				}
				if (curTss.Text == "\u200b" && curIch == 1 && curTag == ComplexConcPatternVc.ktagLeftBoundary)
				{
					SelectLeftBoundary(node.Parent, index, true);
					return false;
				}
				if (!sel.Selection.IsEditable)
				{
					//SelectAt(cellId, cellIndex, true, true, true);
					return false;
				}
			}

			// find the beginning of the currently selected item
			IVwSelection initialSel = SelectAt(node.Parent, index, true, false, false);

			ITsString tss;
			int selCellIndex = index;
			int initialHvo, initialIch, initialTag;
			if (initialSel == null)
				return false;
			initialSel.TextSelInfo(false, out tss, out initialIch, out prev, out initialHvo, out initialTag, out ws);

			// are we at the beginning of an item?
			if ((curHvo == initialHvo && curIch == initialIch && curTag == initialTag)
				|| (curIch == 0 && curTag == ComplexConcPatternVc.ktagLeftBoundary))
			{
				// if the current selection is an IP, then don't adjust anything
				if (!sel.IsRange)
					return false;

				// if we are the beginning of the current item, and the current selection is a range, and the end is before the anchor,
				// then do not include the current item in the adjusted range selection
				if (sel.Selection.EndBeforeAnchor && limit == SelectionHelper.SelLimitType.Anchor)
					selCellIndex = index - 1;
			}
			else
			{
				int finalIch, finalHvo, finalTag;
				IVwSelection finalSel = SelectAt(node.Parent, index, false, false, false);
				finalSel.TextSelInfo(false, out tss, out finalIch, out prev, out finalHvo, out finalTag, out ws);
				// are we at the end of an item?
				if ((curHvo == finalHvo && curIch == finalIch && curTag == finalTag)
					|| (curIch == curTss.Length && curTag == ComplexConcPatternVc.ktagRightBoundary))
				{
					// if the current selection is an IP, then don't adjust anything
					if (!sel.IsRange)
						return false;

					// if we are the end of the current item, and the current selection is a range, and the anchor is before the end,
					// then do not include the current item in the adjusted range selection
					if (!sel.Selection.EndBeforeAnchor && limit == SelectionHelper.SelLimitType.Anchor)
						selCellIndex = index + 1;
				}
			}

			bool initial = limit == SelectionHelper.SelLimitType.Anchor ? !sel.Selection.EndBeforeAnchor : sel.Selection.EndBeforeAnchor;
			vwSel = SelectAt(node.Parent, selCellIndex, initial, false, false);

			return vwSel != null;
		}

		public bool DisplayContextMenu(IVwSelection sel)
		{
			var sh = SelectionHelper.Create(sel, m_view);
			ComplexConcPatternNode node = GetNode(sh, SelectionHelper.SelLimitType.Anchor);
			var nodes = new HashSet<ComplexConcPatternNode>(CurrentNodes.SelectMany(n => GetAllNodes(n)));
			if (!nodes.Contains(node))
				sh.Selection.Install();
			if (nodes.Count > 0)
			{
				// we only bother to display the context menu if an item is selected
				var window = m_propertyTable.GetValue<XWindow>("window");

				window.ShowContextMenu("mnuComplexConcordance",
					new Point(Cursor.Position.X, Cursor.Position.Y),
					new TemporaryColleagueParameter(m_mediator, this, true),
					null, null);
			}

			return false;
		}

		private IEnumerable<ComplexConcPatternNode> GetAllNodes(ComplexConcPatternNode node)
		{
			yield return node;
			if (!node.IsLeaf)
			{
				foreach (ComplexConcPatternNode child in node.Children)
				{
					foreach (ComplexConcPatternNode n in GetAllNodes(child))
						yield return n;
				}
			}
		}

		private void m_insertControl_Insert(object sender, InsertEventArgs e)
		{
			ComplexConcPatternNode node = null;
			switch (((InsertOption) e.Option).Type)
			{
				case ComplexConcordanceInsertType.Morph:
					using (var dlg = new ComplexConcMorphDlg())
					{
						var morphNode = new ComplexConcMorphNode();
						dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, morphNode);
						if (dlg.ShowDialog(m_propertyTable.GetValue<XWindow>("window")) == DialogResult.OK)
							node = morphNode;
					}
					break;

				case ComplexConcordanceInsertType.Word:
					using (var dlg = new ComplexConcWordDlg())
					{
						var wordNode = new ComplexConcWordNode();
						dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, wordNode);
						if (dlg.ShowDialog(m_propertyTable.GetValue<XWindow>("window")) == DialogResult.OK)
							node = wordNode;
					}
					break;

				case ComplexConcordanceInsertType.TextTag:
					using (var dlg = new ComplexConcTagDlg())
					{
						var tagNode = new ComplexConcTagNode();
						dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, tagNode);
						if (dlg.ShowDialog(m_propertyTable.GetValue<XWindow>("window")) == DialogResult.OK)
							node = tagNode;
					}
					break;

				case ComplexConcordanceInsertType.Or:
					node = new ComplexConcOrNode();
					break;

				case ComplexConcordanceInsertType.WordBoundary:
					node = new ComplexConcWordBdryNode();
					break;
			}

			m_view.Select();

			if (node == null)
				return;

			SelectionHelper sel = SelectionHelper.Create(m_view);

			ComplexConcPatternNode parent;
			int index;
			GetInsertionIndex(sel, out parent, out index);
			// if the current selection is a range remove the items we are overwriting
			if (sel.IsRange)
			{
				foreach (ComplexConcPatternNode n in CurrentNodes)
					n.Parent.Children.Remove(n);
			}

			parent.Children.Insert(index, node);
			ReconstructView(parent, index, false);
		}

		private void GetInsertionIndex(SelectionHelper sel, out ComplexConcPatternNode parent, out int index)
		{
			ComplexConcPatternNode curNode = GetNode(sel, SelectionHelper.SelLimitType.Top);
			if (m_patternModel.Root.IsLeaf || curNode == null)
			{
				parent = m_patternModel.Root;
				index = 0;
				return;
			}

			parent = curNode.Parent;
			int ich = sel.GetIch(SelectionHelper.SelLimitType.Top);
			index = GetNodeIndex(curNode);
			if (ich != 0)
				index++;
		}

		private void m_view_LayoutSizeChanged(object sender, EventArgs e)
		{
			if (m_view.RootBox == null)
				return;

			UpdateViewHeight();
		}

		private void UpdateViewHeight()
		{
			int height = m_view.RootBox.Height;
			int bottom = m_view.Bottom;
			m_view.Top = bottom - height;
			m_view.Height = height;
		}

		public bool OnDisplayPatternNodeGroup(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			ComplexConcPatternNode[] nodes = CurrentNodes;
			bool enable = nodes.Length > 1 && !(nodes[0] is ComplexConcOrNode) && !(nodes[nodes.Length - 1] is ComplexConcOrNode);
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnPatternNodeGroup(object args)
		{
			CheckDisposed();
			ComplexConcPatternNode[] nodes = CurrentNodes;
			ComplexConcPatternNode group = GroupNodes(nodes);
			ReconstructView(group, false);
			return true;
		}

		public bool OnDisplayPatternNodeSetOccurrence(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			SelectionHelper sel = SelectionHelper.Create(m_view);
			ComplexConcPatternNode[] nodes = CurrentNodes;
			bool enable = sel.IsRange && !(nodes[0] is ComplexConcOrNode) && !(nodes[nodes.Length - 1] is ComplexConcOrNode);
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnPatternNodeSetOccurrence(object args)
		{
			CheckDisposed();

			ComplexConcPatternNode[] nodes = CurrentNodes;

			int min, max;
			var cmd = (Command) args;
			if (cmd.Parameters.Count > 0)
			{
				string minStr = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "min");
				string maxStr = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "max");
				min = Int32.Parse(minStr);
				max = Int32.Parse(maxStr);
			}
			else
			{
				bool paren;
				if (nodes.Length > 1)
				{
					min = 1;
					max = 1;
					paren = true;
				}
				else
				{
					min = nodes[0].Minimum;
					max = nodes[0].Maximum;
					paren = !nodes[0].IsLeaf;
				}
				using (var dlg = new OccurrenceDlg(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), min, max, paren))
				{
					dlg.SetHelpTopic("khtpCtxtOccurComplexConcordance");
					if (dlg.ShowDialog(m_propertyTable.GetValue<XWindow>("window")) == DialogResult.OK)
					{
						min = dlg.Minimum;
						max = dlg.Maximum;
					}
					else
					{
						return true;
					}
				}
			}

			ComplexConcPatternNode node = nodes.Length > 1 ? GroupNodes(nodes) : nodes[0];
			node.Minimum = min;
			node.Maximum = max;
			ReconstructView(node, false);
			return true;
		}

		private ComplexConcPatternNode GroupNodes(ComplexConcPatternNode[] nodes)
		{
			ComplexConcPatternNode parent = nodes[0].Parent;
			int index = GetNodeIndex(nodes[0]);
			var group = new ComplexConcGroupNode();
			parent.Children.Insert(index, group);
			foreach (ComplexConcPatternNode node in nodes)
			{
				parent.Children.Remove(node);
				group.Children.Add(node);
			}
			return group;
		}

		public bool OnDisplayPatternNodeSetCriteria(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			SelectionHelper sel = SelectionHelper.Create(m_view);
			ComplexConcPatternNode[] nodes = CurrentNodes;
			bool enable = sel.IsRange && nodes.Length == 1 && (nodes[0] is ComplexConcWordNode || nodes[0] is ComplexConcMorphNode || nodes[0] is ComplexConcTagNode);
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnPatternNodeSetCriteria(object args)
		{
			CheckDisposed();
			ComplexConcPatternNode[] nodes = CurrentNodes;

			var wordNode = nodes[0] as ComplexConcWordNode;
			var xwindow = m_propertyTable.GetValue<XWindow>("window");
			if (wordNode != null)
			{
				using (var dlg = new ComplexConcWordDlg())
				{
					dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, wordNode);
					if (dlg.ShowDialog(xwindow) == DialogResult.Cancel)
						return true;
				}
			}
			else
			{
				var morphNode = nodes[0] as ComplexConcMorphNode;
				if (morphNode != null)
				{
					using (var dlg = new ComplexConcMorphDlg())
					{
						dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, morphNode);
						if (dlg.ShowDialog(xwindow) == DialogResult.Cancel)
							return true;
					}
				}
				else
				{
					var tagNode = nodes[0] as ComplexConcTagNode;
					if (tagNode != null)
					{
						using (var dlg = new ComplexConcTagDlg())
						{
							dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, tagNode);
							if (dlg.ShowDialog(xwindow) == DialogResult.Cancel)
								return true;
						}
					}
				}
			}

			ReconstructView(nodes[0], false);
			return true;
		}

		private void m_searchButton_Click(object sender, EventArgs e)
		{
			LoadMatches(true);
		}

		public bool IsFocusedPane { get; set; }
	}
}
