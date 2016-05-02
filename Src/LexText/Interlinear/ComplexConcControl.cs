// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.IText
{
	public partial class ComplexConcControl : ConcordanceControlBase, IFocusablePanePortion, IPatternControl
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

		public override void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();
			base.Init(mediator, propertyTable, configurationParameters);

			var pattern = m_propertyTable.GetValue<ComplexConcGroupNode>("ComplexConcPattern");
			if (pattern == null)
			{
				pattern = new ComplexConcGroupNode();
				m_propertyTable.SetProperty("ComplexConcPattern", pattern, true);
				m_propertyTable.SetPropertyPersistence("ComplexConcPattern", false);
			}
			m_patternModel = new ComplexConcPatternModel(m_cache, pattern);

			m_view.Init(mediator, m_propertyTable, m_patternModel.Root.Hvo, this, new ComplexConcPatternVc(m_cache, propertyTable), ComplexConcPatternVc.kfragPattern,
				m_patternModel.DataAccess);

			m_view.SelectionChanged += SelectionChanged;
			m_view.RemoveItemsRequested += RemoveItemsRequested;
			m_view.ContextMenuRequested += ContextMenuRequested;

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
			ConcDecorator concDecorator = ConcDecorator;
			IStTxtPara[] needsParsing = concDecorator.InterestingTexts.SelectMany(txt => txt.ParagraphsOS).Cast<IStTxtPara>().Where(para => !para.ParseIsCurrent).ToArray();
			if (needsParsing.Length > 0)
			{
				NonUndoableUnitOfWorkHelper.DoSomehow(m_cache.ActionHandlerAccessor,
					() =>
					{
						foreach (IStTxtPara para in needsParsing)
							ParagraphParser.ParseParagraph(para);
					});
			}
		}

		protected override List<IParaFragment> SearchForMatches()
		{
			List<IParaFragment> matches = new List<IParaFragment>();
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
				ComplexConcPatternNode[] nodes = new ComplexConcPatternNode[index2 - index1 + 1];
				for (int i = index1; i <= index2; i++)
					nodes[j++] = anchorNode.Parent.Children[i];
				return nodes;
			}
		}

		private void RemoveItemsRequested(object sender, RemoveItemsRequestedEventArgs e)
		{
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
				ITsString tss = sel.GetTss(SelectionHelper.SelLimitType.Anchor);
				// if the current ich is at the end of the current string, then we can safely assume
				// we are at the end of the current item, so remove it or the next item based on what
				// key was pressed, otherwise we are in the middle in which
				// case the entire item is selected, or at the beginning, so we remove it or the previous
				// item based on what key was pressed
				if (sel.IchAnchor == tss.Length)
				{
					if (e.Forward)
					{
						if (index == n.Parent.Children.Count - 1)
							index = -1;
						else
							index++;
					}
				}
				else
				{
					if (!e.Forward)
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
			m_view.SelectAt(parent, index, initial, true, true);
		}

		private void SelectionChanged(object sender, EventArgs eventArgs)
		{
			// since the context has changed update the display options on the insertion control
			m_insertControl.UpdateOptionsDisplay();
		}

		private int GetNodeIndex(ComplexConcPatternNode node)
		{
			if (node.Parent == null)
				return 0;
			return node.Parent.Children.IndexOf(node);
		}

		private void ContextMenuRequested(object sender, ContextMenuRequestedEventArgs e)
		{
			SelectionHelper sh = SelectionHelper.Create(e.Selection, m_view);
			ComplexConcPatternNode node = GetNode(sh, SelectionHelper.SelLimitType.Anchor);
			HashSet<ComplexConcPatternNode> nodes = new HashSet<ComplexConcPatternNode>(CurrentNodes.SelectMany(GetAllNodes));
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
					using (ComplexConcMorphDlg dlg = new ComplexConcMorphDlg())
					{
						ComplexConcMorphNode morphNode = new ComplexConcMorphNode();
						dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, morphNode);
						if (dlg.ShowDialog(m_propertyTable.GetValue<XWindow>("window")) == DialogResult.OK)
							node = morphNode;
					}
					break;

				case ComplexConcordanceInsertType.Word:
					using (ComplexConcWordDlg dlg = new ComplexConcWordDlg())
					{
						ComplexConcWordNode wordNode = new ComplexConcWordNode();
						dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, wordNode);
						if (dlg.ShowDialog(m_propertyTable.GetValue<XWindow>("window")) == DialogResult.OK)
							node = wordNode;
					}
					break;

				case ComplexConcordanceInsertType.TextTag:
					using (ComplexConcTagDlg dlg = new ComplexConcTagDlg())
					{
						ComplexConcTagNode tagNode = new ComplexConcTagNode();
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
			ComplexConcGroupNode group = new ComplexConcGroupNode();
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

			ComplexConcWordNode wordNode = nodes[0] as ComplexConcWordNode;
			var xwindow = m_propertyTable.GetValue<XWindow>("window");
			if (wordNode != null)
			{
				using (ComplexConcWordDlg dlg = new ComplexConcWordDlg())
				{
					dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, wordNode);
					if (dlg.ShowDialog(xwindow) == DialogResult.Cancel)
						return true;
				}
			}
			else
			{
				ComplexConcMorphNode morphNode = nodes[0] as ComplexConcMorphNode;
				if (morphNode != null)
				{
					using (ComplexConcMorphDlg dlg = new ComplexConcMorphDlg())
					{
						dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, morphNode);
						if (dlg.ShowDialog(xwindow) == DialogResult.Cancel)
							return true;
					}
				}
				else
				{
					ComplexConcTagNode tagNode = nodes[0] as ComplexConcTagNode;
					if (tagNode != null)
					{
						using (ComplexConcTagDlg dlg = new ComplexConcTagDlg())
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

		public object GetContext(SelectionHelper sel)
		{
			return GetContext(sel, SelectionHelper.SelLimitType.Anchor);
		}

		public object GetContext(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			ComplexConcPatternNode node = GetNode(sel, limit);
			return node == null ? null : node.Parent;
		}

		public object GetItem(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			return GetNode(sel, limit);
		}

		public int GetItemContextIndex(object ctxt, object obj)
		{
			return GetNodeIndex((ComplexConcPatternNode) obj);
		}

		public SelLevInfo[] GetLevelInfo(object ctxt, int cellIndex)
		{
			List<SelLevInfo> levels = new List<SelLevInfo>();
			if (!m_patternModel.Root.IsLeaf)
			{
				ComplexConcPatternNode node = (ComplexConcPatternNode) ctxt;
				int i = cellIndex;
				while (node != null)
				{
					levels.Add(new SelLevInfo {tag = ComplexConcPatternSda.ktagChildren, ihvo = i});
					i = GetNodeIndex(node);
					node = node.Parent;
				}
			}
			return levels.ToArray();
		}

		public int GetContextCount(object ctxt)
		{
			return ((ComplexConcPatternNode) ctxt).Children.Count;
		}

		public object GetNextContext(object ctxt)
		{
			return null;
		}

		public object GetPrevContext(object ctxt)
		{
			return null;
		}

		public int GetFlid(object ctxt)
		{
			return -1;
		}
	}
}
