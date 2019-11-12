// Copyright (c) 2013-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	internal partial class ComplexConcControl : ConcordanceControlBase, IFocusablePanePortion, IPatternControl
	{
		private ContextMenuStrip _mnuComplexConcordance;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _menuItems;

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
					return ComplexConcordanceResources.ksComplexConcMorph;
				case ComplexConcordanceInsertType.Word:
					return ComplexConcordanceResources.ksComplexConcWord;
				case ComplexConcordanceInsertType.TextTag:
					return ComplexConcordanceResources.ksComplexConcTag;
				case ComplexConcordanceInsertType.Or:
					return "OR";
				case ComplexConcordanceInsertType.WordBoundary:
					return $"{ComplexConcordanceResources.ksComplexConcWordBoundary} (#)";
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
			: base(recordList)
		{
			InitializeComponent();
		}

		public override string AccName => "LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance.ComplexConcControl";

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
			var anchorNode = GetNode(sel, SelLimitType.Anchor);
			var endNode = GetNode(sel, SelLimitType.End);
			return anchorNode != null && endNode != null && anchorNode.Parent == endNode.Parent;
		}

		private bool CanAddConstraint(object option)
		{
			if (PatternModel.Root.IsLeaf)
			{
				return true;
			}
			var sel = SelectionHelper.Create(m_view);
			var anchorNode = GetNode(sel, SelLimitType.Anchor);
			var endNode = GetNode(sel, SelLimitType.End);
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
			return !(parent.Children[index - 1] is ComplexConcOrNode) && !(parent.Children[index - 1] is ComplexConcWordBdryNode)
				&& (index == parent.Children.Count || !(parent.Children[index] is ComplexConcOrNode) && !(parent.Children[index] is ComplexConcWordBdryNode));
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
				NonUndoableUnitOfWorkHelper.DoSomehow(m_cache.ActionHandlerAccessor, () =>
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

		private ComplexConcPatternNode GetNode(SelectionHelper sel, SelLimitType limit)
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
				var anchorNode = GetNode(sel, SelLimitType.Anchor);
				var endNode = GetNode(sel, SelLimitType.End);
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
				var n = GetNode(sel, SelLimitType.Top);
				parent = n.Parent;
				index = GetNodeIndex(n);
				var tss = sel.GetTss(SelLimitType.Anchor);
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
			if (parent.Children.Count > 1 && parent.Children[parent.Children.Count - 1] is ComplexConcWordBdryNode && !(parent.Children[parent.Children.Count - 2] is ComplexConcMorphNode))
			{
				parent.Children.RemoveAt(parent.Children.Count - 1);
				if (index >= parent.Children.Count)
				{
					index--;
				}
			}
			for (var i = parent.Children.Count - 1; i > 0; i--)
			{
				if (!(parent.Children[i] is ComplexConcWordBdryNode))
				{
					continue;
				}
				if (parent.Children[i - 1] is ComplexConcWordBdryNode || !(parent.Children[i - 1] is ComplexConcMorphNode) || i + 1 < parent.Children.Count && !(parent.Children[i + 1] is ComplexConcMorphNode))
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
			for (var i = parent.Children.Count - 1; i > 0; i--)
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
			var node = GetNode(sh, SelLimitType.Anchor);
			var nodes = new HashSet<ComplexConcPatternNode>(CurrentNodes.SelectMany(GetAllNodes));
			if (!nodes.Contains(node))
			{
				sh.Selection.Install();
			}
			if (nodes.Any())
			{
				sh = SelectionHelper.Create(m_view);
				_mnuComplexConcordance = new ContextMenuStrip()
				{
					Name = "mnuComplexConcordance"
				};
				_menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(7);
				_mnuComplexConcordance.Closed += MnuComplexConcordance_Closed;
				var currentNodes = CurrentNodes;
				var visible = sh.IsRange && !(currentNodes[0] is ComplexConcOrNode) && !(currentNodes[currentNodes.Length - 1] is ComplexConcOrNode);
				if (visible)
				{
					/*
					<menu id="mnuComplexConcordance">
					  <item command="CmdPatternNodeOccurOnce" />
							<command id="CmdPatternNodeOccurOnce" label="Occurs exactly once" message="PatternNodeSetOccurrence">
							  <parameters min="1" max="1" />
							</command>
					*/
					var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetOccurrence_Clicked, TextAndWordsResources.Occurs_exactly_once);
					menu.Tag = new Dictionary<string, int>
					{
						{ "min", 1},
						{ "max", 1}
					};

					/*
					  <item command="CmdPatternNodeOccurZeroMore" />
							<command id="CmdPatternNodeOccurZeroMore" label="Occurs zero or more times" message="PatternNodeSetOccurrence">
							  <parameters min="0" max="-1" />
							</command>
					*/
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetOccurrence_Clicked, TextAndWordsResources.Occurs_zero_or_more_times);
					menu.Tag = new Dictionary<string, int>
					{
						{ "min", 0},
						{ "max", -1}
					};

					/*
					  <item command="CmdPatternNodeOccurOneMore" />
							<command id="CmdPatternNodeOccurOneMore" label="Occurs one or more times" message="PatternNodeSetOccurrence">
							  <parameters min="1" max="-1" />
							</command>
					*/
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetOccurrence_Clicked, TextAndWordsResources.Occurs_one_or_more_times);
					menu.Tag = new Dictionary<string, int>
					{
						{ "min", 1},
						{ "max", -1}
					};

					/*
					  <item command="CmdPatternNodeSetOccur" />
							<command id="CmdPatternNodeSetOccur" label="Set occurrence (min. and max.)..." message="PatternNodeSetOccurrence" />
					*/
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetOccurrence_Clicked, TextAndWordsResources.Set_occurrence_min_and_max);
				}

				/*
				  <item label="-" translate="do not translate" />
				*/
				if (_menuItems.Any())
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(_mnuComplexConcordance);
				}

				visible = sh.IsRange && currentNodes.Length == 1 && (currentNodes[0] is ComplexConcWordNode || currentNodes[0] is ComplexConcMorphNode || currentNodes[0] is ComplexConcTagNode);
				if (visible)
				{
					/*
					  <item command="CmdPatternNodeSetCriteria" />
							<command id="CmdPatternNodeSetCriteria" label="Set criteria..." message="PatternNodeSetCriteria" />
					*/
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetCriteria_Clicked, TextAndWordsResources.Set_criteria);
				}

				visible = currentNodes.Length > 1 && !(currentNodes[0] is ComplexConcOrNode) && !(currentNodes[currentNodes.Length - 1] is ComplexConcOrNode);
				if (visible)
				{
					/*
					  <item command="CmdPatternNodeGroup" />
							<command id="CmdPatternNodeGroup" label="Group" message="PatternNodeGroup" />
					</menu>
					*/
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeGroup_Clicked, TextAndWordsResources.Group);
				}

				// If last menu item is the separator, then remove it.
				var count = _mnuComplexConcordance.Items.Count;
				if (count > 0)
				{
					var lastMenuItem = _mnuComplexConcordance.Items[count - 1];
					if (lastMenuItem is ToolStripSeparator)
					{
						_mnuComplexConcordance.Items.RemoveAt(count - 1);
					}
				}
				_mnuComplexConcordance.Show(new Point(Cursor.Position.X, Cursor.Position.Y));
				e.Handled = true;
			}
		}

		private void MnuComplexConcordance_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			_mnuComplexConcordance.Closed -= MnuComplexConcordance_Closed;
			// Get rid of it all.
			foreach (var tuple in _menuItems)
			{
				tuple.Item1.Click -= tuple.Item2;
				tuple.Item1.Dispose();
			}
			_mnuComplexConcordance.Dispose();
			_mnuComplexConcordance = null;
			_menuItems = null;
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
			var flexComponentParameters = new FlexComponentParameters(PropertyTable, Publisher, Subscriber);
			switch (((InsertOption)e.Option).Type)
			{
				case ComplexConcordanceInsertType.Morph:
					using (var dlg = new ComplexConcMorphDlg())
					{
						var morphNode = new ComplexConcMorphNode();
						dlg.SetDlgInfo(m_cache, flexComponentParameters, morphNode);
						if (dlg.ShowDialog(PropertyTable.GetValue<Form>(FwUtils.window)) == DialogResult.OK)
						{
							node = morphNode;
						}
					}
					break;
				case ComplexConcordanceInsertType.Word:
					using (var dlg = new ComplexConcWordDlg())
					{
						var wordNode = new ComplexConcWordNode();
						dlg.SetDlgInfo(m_cache, flexComponentParameters, wordNode);
						if (dlg.ShowDialog(PropertyTable.GetValue<Form>(FwUtils.window)) == DialogResult.OK)
						{
							node = wordNode;
						}
					}
					break;
				case ComplexConcordanceInsertType.TextTag:
					using (var dlg = new ComplexConcTagDlg())
					{
						var tagNode = new ComplexConcTagNode();
						dlg.SetDlgInfo(m_cache, flexComponentParameters, tagNode);
						if (dlg.ShowDialog(PropertyTable.GetValue<Form>(FwUtils.window)) == DialogResult.OK)
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
			var curNode = GetNode(sel, SelLimitType.Top);
			if (PatternModel.Root.IsLeaf || curNode == null)
			{
				parent = PatternModel.Root;
				index = 0;
				return;
			}
			parent = curNode.Parent;
			var ich = sel.GetIch(SelLimitType.Top);
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

		private void PatternNodeGroup_Clicked(object sender, EventArgs e)
		{
			ReconstructView(GroupNodes(CurrentNodes), false);
		}

		private void PatternNodeSetOccurrence_Clicked(object sender, EventArgs e)
		{
			var nodes = CurrentNodes;
			int min, max;
			var menu = (ToolStripMenuItem)sender;
			if (menu.Tag != null)
			{
				var dictionary = (Dictionary<string, int>)menu.Tag;
				min = dictionary["min"];
				max = dictionary["max"];
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
				using (var dlg = new OccurrenceDlg(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), min, max, paren))
				{
					dlg.SetHelpTopic("khtpCtxtOccurComplexConcordance");
					if (dlg.ShowDialog(PropertyTable.GetValue<Form>(FwUtils.window)) == DialogResult.OK)
					{
						min = dlg.Minimum;
						max = dlg.Maximum;
					}
					else
					{
						return;
					}
				}
			}
			var node = nodes.Length > 1 ? GroupNodes(nodes) : nodes[0];
			node.Minimum = min;
			node.Maximum = max;
			ReconstructView(node, false);
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

		private void PatternNodeSetCriteria_Clicked(object sender, EventArgs e)
		{
			var nodes = CurrentNodes;
			var wordNode = nodes[0] as ComplexConcWordNode;
			var fwMainWnd = PropertyTable.GetValue<Form>(FwUtils.window);
			var flexComponentParameters = new FlexComponentParameters(PropertyTable, Publisher, Subscriber);
			if (wordNode != null)
			{
				using (var dlg = new ComplexConcWordDlg())
				{
					dlg.SetDlgInfo(m_cache, flexComponentParameters, wordNode);
					if (dlg.ShowDialog(fwMainWnd) == DialogResult.Cancel)
					{
						return;
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
						dlg.SetDlgInfo(m_cache, flexComponentParameters, morphNode);
						if (dlg.ShowDialog(fwMainWnd) == DialogResult.Cancel)
						{
							return;
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
							dlg.SetDlgInfo(m_cache, flexComponentParameters, tagNode);
							if (dlg.ShowDialog(fwMainWnd) == DialogResult.Cancel)
							{
								return;
							}
						}
					}
				}
			}
			ReconstructView(nodes[0], false);
		}

		private void m_searchButton_Click(object sender, EventArgs e)
		{
			LoadMatches(true);
		}

		public bool IsFocusedPane { get; set; }

		object IPatternControl.GetContext(SelectionHelper sel, SelLimitType limit)
		{
			var node = GetNode(sel, limit);
			return node?.Parent;
		}

		object IPatternControl.GetItem(SelectionHelper sel, SelLimitType limit)
		{
			return GetNode(sel, limit);
		}

		int IPatternControl.GetItemContextIndex(object ctxt, object obj)
		{
			return GetNodeIndex((ComplexConcPatternNode)obj);
		}

		SelLevInfo[] IPatternControl.GetLevelInfo(object ctxt, int cellIndex)
		{
			if (PatternModel.Root.IsLeaf)
			{
				return new SelLevInfo[0];
			}
			var node = (ComplexConcPatternNode)ctxt;
			var i = cellIndex;
			var levels = new List<SelLevInfo>();
			while (node != null)
			{
				levels.Add(new SelLevInfo { tag = ComplexConcPatternSda.ktagChildren, ihvo = i });
				i = GetNodeIndex(node);
				node = node.Parent;
			}
			return levels.ToArray();
		}

		int IPatternControl.GetContextCount(object ctxt)
		{
			return ((ComplexConcPatternNode)ctxt).Children.Count;
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