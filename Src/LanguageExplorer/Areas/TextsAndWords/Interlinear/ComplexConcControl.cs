// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal partial class ComplexConcControl : ConcordanceControlBase, IFocusablePanePortion, IPatternControl
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

		private sealed class InsertOption
		{
			public InsertOption(ComplexConcordanceInsertType type)
			{
				Type = type;
			}

			public ComplexConcordanceInsertType Type { get; }

			public override string ToString()
			{
				return GetOptionString(Type);
			}
		}

		public ComplexConcControl()
		{
			InitializeComponent();
		}

		internal ComplexConcControl(MatchingConcordanceItems recordList)
			:base(recordList)
		{
			InitializeComponent();
		}

		public override string AccName => "LanguageExplorer.Areas.TextsAndWords.Interlinear.ComplexConcControl";

		#region Overrides of ConcordanceControlBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			var pattern = PropertyTable.GetValue<ComplexConcGroupNode>("ComplexConcPattern");
			if (pattern == null)
			{
				pattern = new ComplexConcGroupNode();
				PropertyTable.SetProperty("ComplexConcPattern", pattern, doBroadcastIfChanged: true);
			}
			PatternModel = new ComplexConcPatternModel(m_cache, pattern);

			m_view.InitializeFlexComponent(flexComponentParameters);
			m_view.Init(PatternModel.Root.Hvo, this, new ComplexConcPatternVc(m_cache, PropertyTable), ComplexConcPatternVc.kfragPattern, PatternModel.DataAccess);

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

		#endregion

		public ComplexConcPatternModel PatternModel { get; private set; }

		private bool CanAddMorph(object option)
		{
			if (PatternModel.Root.IsLeaf)
			{
				return true;
			}

			var sel = SelectionHelper.Create(m_view);
			if (sel.IsRange)
			{
				return true;
			}

			var anchorNode = GetNode(sel, SelectionHelper.SelLimitType.Anchor);
			var endNode = GetNode(sel, SelectionHelper.SelLimitType.End);
			return anchorNode != null && endNode != null && anchorNode.Parent == endNode.Parent;
		}

		private bool CanAddConstraint(object option)
		{
			if (PatternModel.Root.IsLeaf)
			{
				return true;
			}

			var sel = SelectionHelper.Create(m_view);
			var anchorNode = GetNode(sel, SelectionHelper.SelLimitType.Anchor);
			var endNode = GetNode(sel, SelectionHelper.SelLimitType.End);
			if (anchorNode == null || endNode == null || anchorNode.Parent != endNode.Parent)
			{
				return false;
			}

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
			var sel = SelectionHelper.Create(m_view);
			if (sel.IsRange)
			{
				return false;
			}
			ComplexConcPatternNode parent;
			int index;
			GetInsertionIndex(sel, out parent, out index);
			if (index == 0)
			{
				return false;
			}

			return (!(parent.Children[index - 1] is ComplexConcOrNode) && !(parent.Children[index - 1] is ComplexConcWordBdryNode))
				&& (index == parent.Children.Count || (!(parent.Children[index] is ComplexConcOrNode) && !(parent.Children[index] is ComplexConcWordBdryNode)));
		}

		private bool CanAddWordBoundary(object option)
		{
			if (PatternModel.Root.IsLeaf)
			{
				return true;
			}

			var sel = SelectionHelper.Create(m_view);
			if (sel.IsRange)
			{
				return false;
			}
			ComplexConcPatternNode parent;
			int index;
			GetInsertionIndex(sel, out parent, out index);
			if (parent.IsLeaf)
			{
				return false;
			}

			if (index == 0)
			{
				return parent.Children[index] is ComplexConcMorphNode;
			}

			if (index == parent.Children.Count)
			{
				return parent.Children[index - 1] is ComplexConcMorphNode;
			}

			return parent.Children[index - 1] is ComplexConcMorphNode && parent.Children[index] is ComplexConcMorphNode;
		}

		private void ParseUnparsedParagraphs()
		{
			var concDecorator = ConcDecorator;
			var needsParsing = concDecorator.InterestingTexts.SelectMany(txt => txt.ParagraphsOS).Cast<IStTxtPara>().Where(para => !para.ParseIsCurrent).ToArray();
			if (needsParsing.Length > 0)
			{
				NonUndoableUnitOfWorkHelper.DoSomehow(m_cache.ActionHandlerAccessor,
					() =>
					{
						foreach (var para in needsParsing)
						{
							ParagraphParser.ParseParagraph(para);
						}
					});
			}
		}

		protected override List<IParaFragment> SearchForMatches()
		{
			var matches = new List<IParaFragment>();
			if (PatternModel.IsPatternEmpty)
			{
				return matches;
			}

			using (new WaitCursor(this))
			{
				PatternModel.Compile();

				ParseUnparsedParagraphs();
				foreach (var text in ConcDecorator.InterestingTexts)
				{
					matches.AddRange(PatternModel.Search(text));
				}
			}

			return matches;
		}

		private ComplexConcPatternNode GetNode(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (sel == null || PatternModel.Root.IsLeaf)
			{
				return null;
			}

			var levels = sel.GetLevelInfo(limit);
			if (levels.Length == 0)
			{
				return null;
			}

			var level = levels.First(l => l.tag == ComplexConcPatternSda.ktagChildren);
			return PatternModel.GetNode(level.hvo);
		}

		public ComplexConcPatternNode[] CurrentNodes
		{
			get
			{
				var sel = SelectionHelper.Create(m_view);
				var anchorNode = GetNode(sel, SelectionHelper.SelLimitType.Anchor);
				var endNode = GetNode(sel, SelectionHelper.SelLimitType.End);
				if (anchorNode == null || endNode == null || anchorNode.Parent != endNode.Parent)
				{
					return new ComplexConcPatternNode[0];
				}

				var anchorIndex = GetNodeIndex(anchorNode);
				var endIndex = GetNodeIndex(endNode);
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

				var j = 0;
				var nodes = new ComplexConcPatternNode[index2 - index1 + 1];
				for (var i = index1; i <= index2; i++)
				{
					nodes[j++] = anchorNode.Parent.Children[i];
				}
				return nodes;
			}
		}

		private void RemoveItemsRequested(object sender, RemoveItemsRequestedEventArgs e)
		{
			if (PatternModel.Root.IsLeaf)
			{
				return;
			}

			var sel = SelectionHelper.Create(m_view);
			ComplexConcPatternNode parent = null;
			var index = -1;
			if (sel.IsRange)
			{
				var nodes = CurrentNodes;
				if (nodes.Length > 0)
				{
					parent = nodes[0].Parent;
					index = GetNodeIndex(nodes[0]);
					foreach (var node in nodes)
					{
						node.Parent.Children.Remove(node);
					}
				}
			}
			else
			{
				var n = GetNode(sel, SelectionHelper.SelLimitType.Top);
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
					if (e.Forward)
					{
						if (index == n.Parent.Children.Count - 1)
						{
							index = -1;
						}
						else
						{
							index++;
						}
					}
				}
				else
				{
					if (!e.Forward)
					{
						index--;
					}
				}

				if (index != -1)
				{
					parent.Children.RemoveAt(index);
				}
			}

			if (parent == null || index == -1)
			{
				return;
			}
			if (!parent.IsLeaf)
			{
				var isFirstBdry = parent.Children[0] is ComplexConcWordBdryNode;
				if (parent.Children.Count == 1 && isFirstBdry || (parent.Children.Count > 1 && isFirstBdry && !(parent.Children[1] is ComplexConcMorphNode)))
				{
					parent.Children.RemoveAt(0);
					if (index > 0)
					{
						index--;
					}
				}
			}
			if (parent.Children.Count > 1 && parent.Children[parent.Children.Count - 1] is ComplexConcWordBdryNode
			                              && !(parent.Children[parent.Children.Count - 2] is ComplexConcMorphNode))
			{
				parent.Children.RemoveAt(parent.Children.Count - 1);
				if (index >= parent.Children.Count)
				{
					index--;
				}
			}
			for (var i = parent.Children.Count - 1; i > 0 ; i--)
			{
				if (!(parent.Children[i] is ComplexConcWordBdryNode))
				{
					continue;
				}
				if (parent.Children[i - 1] is ComplexConcWordBdryNode || (!(parent.Children[i - 1] is ComplexConcMorphNode) || (i + 1 < parent.Children.Count && !(parent.Children[i + 1] is ComplexConcMorphNode))))
				{
					parent.Children.RemoveAt(i);
					if (index > i)
					{
						index--;
					}
				}
			}

			if (!parent.IsLeaf && parent.Children[0] is ComplexConcOrNode)
			{
				parent.Children.RemoveAt(0);
				if (index > 0)
				{
					index--;
				}
			}
			if (!parent.IsLeaf && parent.Children[parent.Children.Count - 1] is ComplexConcOrNode)
			{
				parent.Children.RemoveAt(parent.Children.Count - 1);
				if (index >= parent.Children.Count)
				{
					index--;
				}
			}
			for (var i = parent.Children.Count - 1; i > 0 ; i--)
			{
				if (parent.Children[i] is ComplexConcOrNode && parent.Children[i - 1] is ComplexConcOrNode)
				{
					parent.Children.RemoveAt(i);
					if (index > i)
					{
						index--;
					}
				}
			}

			if (parent.Parent != null && parent.Children.Count == 1)
			{
				var p = parent.Parent;
				var parentIndex = GetNodeIndex(parent);
				p.Children.RemoveAt(parentIndex);
				p.Children.Insert(parentIndex, parent.Children[0]);
				index = index == 1 ? parentIndex + 1 : parentIndex;
			}
			else
			{
				while (parent.Parent != null && parent.IsLeaf)
				{
					var p = parent.Parent;
					index = GetNodeIndex(parent);
					p.Children.Remove(parent);
					parent = p;
				}
			}

			if (index >= parent.Children.Count)
			{
				ReconstructView(parent, parent.Children.Count - 1, false);
			}
			else
			{
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
			return node.Parent?.Children.IndexOf(node) ?? 0;
		}

		private void ContextMenuRequested(object sender, ContextMenuRequestedEventArgs e)
		{
			var sh = SelectionHelper.Create(e.Selection, m_view);
			var node = GetNode(sh, SelectionHelper.SelLimitType.Anchor);
			var nodes = new HashSet<ComplexConcPatternNode>(CurrentNodes.SelectMany(GetAllNodes));
			if (!nodes.Contains(node))
			{
				sh.Selection.Install();
			}
			if (nodes.Count > 0)
			{
				// we only bother to display the context menu if an item is selected
				var window = PropertyTable.GetValue<IFwMainWnd>("window");

#if RANDYTODO
				window.ShowContextMenu("mnuComplexConcordance",
					new Point(Cursor.Position.X, Cursor.Position.Y),
					null, null);
#endif
			}
		}

		private IEnumerable<ComplexConcPatternNode> GetAllNodes(ComplexConcPatternNode node)
		{
			yield return node;
			if (node.IsLeaf)
			{
				yield break;
			}
			foreach (var child in node.Children)
			{
				foreach (var n in GetAllNodes(child))
				{
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
						dlg.SetDlgInfo(m_cache, PropertyTable, Publisher, morphNode);
						if (dlg.ShowDialog(PropertyTable.GetValue<Form>("window")) == DialogResult.OK)
						{
							node = morphNode;
						}
					}
					break;

				case ComplexConcordanceInsertType.Word:
					using (var dlg = new ComplexConcWordDlg())
					{
						var wordNode = new ComplexConcWordNode();
						dlg.SetDlgInfo(m_cache, PropertyTable, Publisher, wordNode);
						if (dlg.ShowDialog(PropertyTable.GetValue<Form>("window")) == DialogResult.OK)
						{
							node = wordNode;
						}
					}
					break;

				case ComplexConcordanceInsertType.TextTag:
					using (var dlg = new ComplexConcTagDlg())
					{
						var tagNode = new ComplexConcTagNode();
						dlg.SetDlgInfo(m_cache, PropertyTable, Publisher, tagNode);
						if (dlg.ShowDialog(PropertyTable.GetValue<Form>("window")) == DialogResult.OK)
						{
							node = tagNode;
						}
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
			{
				return;
			}

			var sel = SelectionHelper.Create(m_view);
			ComplexConcPatternNode parent;
			int index;
			GetInsertionIndex(sel, out parent, out index);
			// if the current selection is a range remove the items we are overwriting
			if (sel.IsRange)
			{
				foreach (var n in CurrentNodes)
				{
					n.Parent.Children.Remove(n);
				}
			}

			parent.Children.Insert(index, node);
			ReconstructView(parent, index, false);
		}

		private void GetInsertionIndex(SelectionHelper sel, out ComplexConcPatternNode parent, out int index)
		{
			var curNode = GetNode(sel, SelectionHelper.SelLimitType.Top);
			if (PatternModel.Root.IsLeaf || curNode == null)
			{
				parent = PatternModel.Root;
				index = 0;
				return;
			}

			parent = curNode.Parent;
			var ich = sel.GetIch(SelectionHelper.SelLimitType.Top);
			index = GetNodeIndex(curNode);
			if (ich != 0)
			{
				index++;
			}
		}

		private void m_view_LayoutSizeChanged(object sender, EventArgs e)
		{
			if (m_view.RootBox == null)
			{
				return;
			}

			UpdateViewHeight();
		}

		private void UpdateViewHeight()
		{
			var height = m_view.RootBox.Height;
			var bottom = m_view.Bottom;
			m_view.Top = bottom - height;
			m_view.Height = height;
		}

#if RANDYTODO
		public bool OnDisplayPatternNodeGroup(object commandObject, ref UIItemDisplayProperties display)
		{
			ComplexConcPatternNode[] nodes = CurrentNodes;
			bool enable = nodes.Length > 1 && !(nodes[0] is ComplexConcOrNode) && !(nodes[nodes.Length - 1] is ComplexConcOrNode);
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnPatternNodeGroup(object args)
		{
			var nodes = CurrentNodes;
			var group = GroupNodes(nodes);
			ReconstructView(group, false);
			return true;
		}

#if RANDYTODO
		public bool OnDisplayPatternNodeSetOccurrence(object commandObject, ref UIItemDisplayProperties display)
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			ComplexConcPatternNode[] nodes = CurrentNodes;
			bool enable = sel.IsRange && !(nodes[0] is ComplexConcOrNode) && !(nodes[nodes.Length - 1] is ComplexConcOrNode);
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnPatternNodeSetOccurrence(object args)
		{
			var nodes = CurrentNodes;
			int min, max;
#if RANDYTODO
			var cmd = (Command) args;
			if (cmd.Parameters.Count > 0)
			{
				string minStr = XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "min");
				string maxStr = XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "max");
				min = Int32.Parse(minStr);
				max = Int32.Parse(maxStr);
			}
			else
#endif
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
				using (var dlg = new OccurrenceDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), min, max, paren))
				{
					dlg.SetHelpTopic("khtpCtxtOccurComplexConcordance");
					if (dlg.ShowDialog(PropertyTable.GetValue<Form>("window")) == DialogResult.OK)
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

			var node = nodes.Length > 1 ? GroupNodes(nodes) : nodes[0];
			node.Minimum = min;
			node.Maximum = max;
			ReconstructView(node, false);
			return true;
		}

		private ComplexConcPatternNode GroupNodes(ComplexConcPatternNode[] nodes)
		{
			var parent = nodes[0].Parent;
			var index = GetNodeIndex(nodes[0]);
			var group = new ComplexConcGroupNode();
			parent.Children.Insert(index, group);
			foreach (var node in nodes)
			{
				parent.Children.Remove(node);
				group.Children.Add(node);
			}
			return group;
		}

#if RANDYTODO
		public bool OnDisplayPatternNodeSetCriteria(object commandObject, ref UIItemDisplayProperties display)
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			ComplexConcPatternNode[] nodes = CurrentNodes;
			bool enable = sel.IsRange && nodes.Length == 1 && (nodes[0] is ComplexConcWordNode || nodes[0] is ComplexConcMorphNode || nodes[0] is ComplexConcTagNode);
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnPatternNodeSetCriteria(object args)
		{
			var nodes = CurrentNodes;
			var wordNode = nodes[0] as ComplexConcWordNode;
			var fwMainWnd = PropertyTable.GetValue<Form>("window");
			if (wordNode != null)
			{
				using (var dlg = new ComplexConcWordDlg())
				{
					dlg.SetDlgInfo(m_cache, PropertyTable, Publisher, wordNode);
					if (dlg.ShowDialog(fwMainWnd) == DialogResult.Cancel)
					{
						return true;
					}
				}
			}
			else
			{
				var morphNode = nodes[0] as ComplexConcMorphNode;
				if (morphNode != null)
				{
					using (var dlg = new ComplexConcMorphDlg())
					{
						dlg.SetDlgInfo(m_cache, PropertyTable, Publisher, morphNode);
						if (dlg.ShowDialog(fwMainWnd) == DialogResult.Cancel)
						{
							return true;
						}
					}
				}
				else
				{
					var tagNode = nodes[0] as ComplexConcTagNode;
					if (tagNode != null)
					{
						using (var dlg = new ComplexConcTagDlg())
						{
							dlg.SetDlgInfo(m_cache, PropertyTable, Publisher, tagNode);
							if (dlg.ShowDialog(fwMainWnd) == DialogResult.Cancel)
							{
								return true;
							}
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

		object IPatternControl.GetContext(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			var node = GetNode(sel, limit);
			return node?.Parent;
		}

		object IPatternControl.GetItem(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			return GetNode(sel, limit);
		}

		int IPatternControl.GetItemContextIndex(object ctxt, object obj)
		{
			return GetNodeIndex((ComplexConcPatternNode) obj);
		}

		SelLevInfo[] IPatternControl.GetLevelInfo(object ctxt, int cellIndex)
		{
			if (PatternModel.Root.IsLeaf)
			{
				return new SelLevInfo[0];
			}
			var node = (ComplexConcPatternNode) ctxt;
			var i = cellIndex;
			var levels = new List<SelLevInfo>();
			while (node != null)
			{
				levels.Add(new SelLevInfo {tag = ComplexConcPatternSda.ktagChildren, ihvo = i});
				i = GetNodeIndex(node);
				node = node.Parent;
			}
			return levels.ToArray();
		}

		int IPatternControl.GetContextCount(object ctxt)
		{
			return ((ComplexConcPatternNode) ctxt).Children.Count;
		}

		object IPatternControl.GetNextContext(object ctxt)
		{
			return null;
		}

		object IPatternControl.GetPrevContext(object ctxt)
		{
			return null;
		}

		int IPatternControl.GetFlid(object ctxt)
		{
			return -1;
		}
	}
}