// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	internal sealed partial class ComplexConcControl : ConcordanceControlBase, IFocusablePanePortion, IPatternControl
	{
		private const int kfragPattern = 100;
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
					return ComplexConcordanceResources.OR;
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

		internal ComplexConcControl()
		{
			InitializeComponent();
		}

		internal ComplexConcControl(MatchingConcordanceRecordList recordList)
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
			m_view.Init(PatternModel.Root.Hvo, this, new ComplexConcPatternVc(m_cache, PropertyTable), kfragPattern, PatternModel.DataAccess);
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

		private ComplexConcPatternModel PatternModel { get; set; }

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
				GetInsertionIndex(sel, out parent, out var index);
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
			GetInsertionIndex(sel, out var parent, out var index);
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
			GetInsertionIndex(sel, out var parent, out var index);
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

		private IReadOnlyList<ComplexConcPatternNode> CurrentNodes
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
				if (nodes.Any())
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
				var grandParent = parent.Parent;
				var parentIndex = GetNodeIndex(parent);
				grandParent.Children.RemoveAt(parentIndex);
				grandParent.Children.Insert(parentIndex, parent.Children[0]);
				index = index == 1 ? parentIndex + 1 : parentIndex;
			}
			else
			{
				while (parent.Parent != null && parent.IsLeaf)
				{
					var grandParent = parent.Parent;
					index = GetNodeIndex(parent);
					grandParent.Children.Remove(parent);
					parent = grandParent;
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

		private static int GetNodeIndex(ComplexConcPatternNode node)
		{
			return node.Parent?.Children.IndexOf(node) ?? 0;
		}

		private void ContextMenuRequested(object sender, ContextMenuRequestedEventArgs e)
		{
			if (_mnuComplexConcordance != null)
			{
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
				var currentNodes = CurrentNodes;
				var visible = sh.IsRange && !(currentNodes[0] is ComplexConcOrNode) && !(currentNodes[currentNodes.Count - 1] is ComplexConcOrNode);
				if (visible)
				{
					/*
					<menu id="mnuComplexConcordance">
					  <item command="CmdPatternNodeOccurOnce" />
							<command id="CmdPatternNodeOccurOnce" label="Occurs exactly once" message="PatternNodeSetOccurrence">
							  <parameters min="1" max="1" />
							</command>
					*/
					var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetOccurrence_Clicked, LanguageExplorerControls.Occurs_exactly_once);
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
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetOccurrence_Clicked, LanguageExplorerControls.Occurs_zero_or_more_times);
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
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetOccurrence_Clicked, LanguageExplorerControls.Occurs_one_or_more_times);
					menu.Tag = new Dictionary<string, int>
					{
						{ "min", 1},
						{ "max", -1}
					};

					/*
					  <item command="CmdPatternNodeSetOccur" />
							<command id="CmdPatternNodeSetOccur" label="Set occurrence (min. and max.)..." message="PatternNodeSetOccurrence" />
					*/
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetOccurrence_Clicked, LanguageExplorerControls.Set_occurrence_min_and_max);
				}

				/*
				  <item label="-" translate="do not translate" />
				*/
				if (_menuItems.Any())
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(_mnuComplexConcordance);
				}

				visible = sh.IsRange && currentNodes.Count == 1 && (currentNodes[0] is ComplexConcWordNode || currentNodes[0] is ComplexConcMorphNode || currentNodes[0] is ComplexConcTagNode);
				if (visible)
				{
					/*
					  <item command="CmdPatternNodeSetCriteria" />
							<command id="CmdPatternNodeSetCriteria" label="Set criteria..." message="PatternNodeSetCriteria" />
					*/
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(_menuItems, _mnuComplexConcordance, PatternNodeSetCriteria_Clicked, TextAndWordsResources.Set_criteria);
				}

				visible = currentNodes.Count > 1 && !(currentNodes[0] is ComplexConcOrNode) && !(currentNodes[currentNodes.Count - 1] is ComplexConcOrNode);
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

		private static IEnumerable<ComplexConcPatternNode> GetAllNodes(ComplexConcPatternNode node)
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
			GetInsertionIndex(sel, out var parent, out var index);
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
				if (nodes.Count > 1)
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
			var node = nodes.Count > 1 ? GroupNodes(nodes) : nodes[0];
			node.Minimum = min;
			node.Maximum = max;
			ReconstructView(node, false);
		}

		private static ComplexConcPatternNode GroupNodes(IReadOnlyList<ComplexConcPatternNode> nodes)
		{
			var parent = nodes[0].Parent;
			var index = GetNodeIndex(nodes[0]);
			var group = new ComplexConcGroupNode();
			parent.Children.Insert(index, @group);
			foreach (var node in nodes)
			{
				parent.Children.Remove(node);
				@group.Children.Add(node);
			}
			return @group;
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
				switch (nodes[0])
				{
					case ComplexConcMorphNode morphNode:
					{
						using (var dlg = new ComplexConcMorphDlg())
						{
							dlg.SetDlgInfo(m_cache, flexComponentParameters, morphNode);
							if (dlg.ShowDialog(fwMainWnd) == DialogResult.Cancel)
							{
								return;
							}
						}

						break;
					}
					case ComplexConcTagNode tagNode:
					{
						using (var dlg = new ComplexConcTagDlg())
						{
							dlg.SetDlgInfo(m_cache, flexComponentParameters, tagNode);
							if (dlg.ShowDialog(fwMainWnd) == DialogResult.Cancel)
							{
								return;
							}
						}

						break;
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

		private sealed class ComplexConcPatternVc : PatternVcBase
		{
			// normal frags
			private const int kfragNode = 101;
			// variant frags
			private const int kfragNodeMax = 104;
			private const int kfragNodeMin = 105;
			private const int kfragOR = 106;
			private const int kfragHash = 107;
			// fake flids
			private const int ktagType = -200;
			private const int ktagForm = -201;
			private const int ktagGloss = -202;
			private const int ktagCategory = -203;
			private const int ktagEntry = -204;
			private const int ktagTag = -205;
			private const int ktagInfl = -206;
			private readonly ITsString m_infinity;
			private readonly ITsString m_or;
			private readonly ITsString m_hash;
			private IDictionary<IFsFeatDefn, object> m_curInflFeatures;

			internal ComplexConcPatternVc(LcmCache cache, IPropertyTable propertyTable)
				: base(cache, propertyTable)
			{
				var userWs = m_cache.DefaultUserWs;
				m_infinity = TsStringUtils.MakeString("\u221e", userWs);
				m_or = TsStringUtils.MakeString("OR", userWs);
				m_hash = TsStringUtils.MakeString("#", userWs);
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
					case kfragPattern:
						VwLength tableLen;
						tableLen.nVal = 10000;
						tableLen.unit = VwUnit.kunPercent100;
						vwenv.OpenTable(1, tableLen, 0, VwAlignment.kvaCenter, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 0, false);
						VwLength patternLen;
						patternLen.nVal = 1;
						patternLen.unit = VwUnit.kunRelative;
						vwenv.MakeColumns(1, patternLen);
						vwenv.OpenTableBody();
						vwenv.OpenTableRow();
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
						vwenv.set_IntProperty((int)FwTextPropType.ktptPadBottom, (int)FwTextPropVar.ktpvMilliPoint, 2000);
						vwenv.OpenTableCell(1, 1);
						vwenv.OpenParagraph();
						if (((ComplexConcPatternSda)vwenv.DataAccess).Root.IsLeaf)
						{
							OpenSingleLinePile(vwenv, GetMaxNumLines(vwenv), false);
							vwenv.Props = m_bracketProps;
							vwenv.AddProp(ComplexConcPatternSda.ktagChildren, this, kfragEmpty);
							CloseSingleLinePile(vwenv, false);
						}
						else
						{
							vwenv.AddObjVecItems(ComplexConcPatternSda.ktagChildren, this, kfragNode);
						}
						vwenv.CloseParagraph();
						vwenv.CloseTableCell();
						vwenv.CloseTableRow();
						vwenv.CloseTableBody();
						vwenv.CloseTable();
						break;
					case kfragNode:
						var node = ((ComplexConcPatternSda)vwenv.DataAccess).Nodes[hvo];
						var maxNumLines = GetMaxNumLines(vwenv);
						switch (node)
						{
							case ComplexConcOrNode _:
								OpenSingleLinePile(vwenv, maxNumLines);
								vwenv.AddProp(ktagInnerNonBoundary, this, kfragOR);
								CloseSingleLinePile(vwenv, false);
								break;
							case ComplexConcWordBdryNode _:
								OpenSingleLinePile(vwenv, maxNumLines);
								vwenv.AddProp(ktagInnerNonBoundary, this, kfragHash);
								CloseSingleLinePile(vwenv);
								break;
							case ComplexConcGroupNode _:
								{
									var numLines = GetNumLines(node);
									var hasMinMax = node.Maximum != 1 || node.Minimum != 1;
									if (numLines == 1)
									{
										OpenSingleLinePile(vwenv, maxNumLines, false);
										// use normal parentheses for a single line group
										vwenv.AddProp(ktagLeftBoundary, this, kfragLeftParen);
										vwenv.AddObjVecItems(ComplexConcPatternSda.ktagChildren, this, kfragNode);
										vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightParen);
										if (hasMinMax)
										{
											DisplayMinMax(numLines, vwenv);
										}
										CloseSingleLinePile(vwenv, false);
									}
									else
									{
										vwenv.Props = m_bracketProps;
										vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
										vwenv.OpenInnerPile();
										AddExtraLines(maxNumLines - numLines, ktagLeftNonBoundary, vwenv);
										vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftParenUpHook);
										for (var i = 1; i < numLines - 1; i++)
										{
											vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftParenExt);
										}
										vwenv.AddProp(ktagLeftBoundary, this, kfragLeftParenLowHook);
										vwenv.CloseInnerPile();
										vwenv.AddObjVecItems(ComplexConcPatternSda.ktagChildren, this, kfragNode);
										vwenv.Props = m_bracketProps;
										vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
										vwenv.OpenInnerPile();
										AddExtraLines(maxNumLines - numLines, hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, vwenv);
										vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, this, kfragRightParenUpHook);
										for (var i = 1; i < numLines - 1; i++)
										{
											vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, this, kfragRightParenExt);
										}
										vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightParenLowHook);
										vwenv.CloseInnerPile();
										if (hasMinMax)
										{
											DisplayMinMax(numLines, vwenv);
										}
									}
									break;
								}
							default:
								{
									var hasMinMax = node.Maximum != 1 || node.Minimum != 1;
									var numLines = GetNumLines(node);
									if (numLines == 1)
									{
										OpenSingleLinePile(vwenv, maxNumLines, false);
										// use normal brackets for a single line constraint
										vwenv.AddProp(ktagLeftBoundary, this, kfragLeftBracket);
										DisplayFeatures(vwenv, node);
										vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightBracket);
										if (hasMinMax)
										{
											DisplayMinMax(numLines, vwenv);
										}
										CloseSingleLinePile(vwenv, false);
									}
									else
									{
										// left bracket pile
										vwenv.Props = m_bracketProps;
										vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
										vwenv.OpenInnerPile();
										AddExtraLines(maxNumLines - numLines, ktagLeftNonBoundary, vwenv);
										vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketUpHook);
										for (var i = 1; i < numLines - 1; i++)
										{
											vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketExt);
										}
										vwenv.AddProp(ktagLeftBoundary, this, kfragLeftBracketLowHook);
										vwenv.CloseInnerPile();
										// feature pile
										vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
										vwenv.OpenInnerPile();
										AddExtraLines(maxNumLines - numLines, ktagInnerNonBoundary, vwenv);
										DisplayFeatures(vwenv, node);
										vwenv.CloseInnerPile();
										// right bracket pile
										vwenv.Props = m_bracketProps;
										vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
										vwenv.OpenInnerPile();
										AddExtraLines(maxNumLines - numLines, hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, vwenv);
										vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, this, kfragRightBracketUpHook);
										for (var i = 1; i < numLines - 1; i++)
										{
											vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, this, kfragRightBracketExt);
										}
										vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightBracketLowHook);
										vwenv.CloseInnerPile();
										if (hasMinMax)
										{
											DisplayMinMax(numLines, vwenv);
										}
									}
									break;
								}
						}
						break;
				}
			}

			private void DisplayMinMax(int numLines, IVwEnv vwenv)
			{
				var superOffset = 0;
				if (numLines == 1)
				{
					// if the inner context is a single line, then make the min value a subscript and the max value a superscript.
					// I tried to use the Views subscript and superscript properties, but they added extra space so that it would
					// have the same line height of a normal character, which is not what I wanted, so I compute the size myself
					var superSubHeight = GetFontHeight(m_cache.DefaultUserWs) * 2 / 3;
					vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, superSubHeight);
					vwenv.set_IntProperty((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, -superSubHeight);
					superOffset = superSubHeight / 2;
				}
				else
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
				}
				vwenv.OpenInnerPile();
				if (numLines == 1)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, superOffset);
				}
				vwenv.OpenParagraph();
				vwenv.AddProp(ktagRightNonBoundary, this, kfragNodeMax);
				vwenv.CloseParagraph();
				AddExtraLines(numLines - 2, ktagRightNonBoundary, vwenv);
				vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, 0);
				vwenv.OpenParagraph();
				vwenv.AddProp(ktagRightBoundary, this, kfragNodeMin);
				vwenv.CloseParagraph();
				vwenv.CloseInnerPile();
			}

			public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
			{
				// we use display variant to display literal strings that are editable
				ITsString tss = null;
				switch (frag)
				{
					case InterlinLineChoices.kfragFeatureLine:
						var node = ((ComplexConcPatternSda)vwenv.DataAccess).Nodes[vwenv.CurrentObject()];
						switch (tag)
						{
							case ktagType:
								string typeStr = null;
								if (node is ComplexConcMorphNode)
								{
									typeStr = ComplexConcordanceResources.ksComplexConcMorph;
								}
								else if (node is ComplexConcWordNode)
								{
									typeStr = ComplexConcordanceResources.ksComplexConcWord;
								}
								else if (node is ComplexConcTagNode)
								{
									typeStr = ComplexConcordanceResources.ksComplexConcTag;
								}
								tss = CreateFeatureLine(ComplexConcordanceResources.ksComplexConcType, typeStr, m_cache.DefaultUserWs);
								break;
							case ktagForm:
								ITsString form = null;
								switch (node)
								{
									case ComplexConcMorphNode formMorphNode:
										form = formMorphNode.Form;
										break;
									case ComplexConcWordNode formWordNode:
										form = formWordNode.Form;
										break;
								}
								Debug.Assert(form != null);
								tss = CreateFeatureLine(ComplexConcordanceResources.ksComplexConcForm, form, false);
								break;
							case ktagEntry:
								ITsString entry = null;
								if (node is ComplexConcMorphNode entryMorphNode)
								{
									entry = entryMorphNode.Entry;
								}
								Debug.Assert(entry != null);
								tss = CreateFeatureLine(ComplexConcordanceResources.ksComplexConcEntry, entry, false);
								break;
							case ktagGloss:
								ITsString gloss = null;
								switch (node)
								{
									case ComplexConcMorphNode glossMorphNode:
										gloss = glossMorphNode.Gloss;
										break;
									case ComplexConcWordNode glossWordNode:
										gloss = glossWordNode.Gloss;
										break;
								}
								Debug.Assert(gloss != null);
								tss = CreateFeatureLine(ComplexConcordanceResources.ksComplexConcGloss, gloss, false);
								break;
							case ktagCategory:
								IPartOfSpeech category = null;
								var catNegated = false;
								switch (node)
								{
									case ComplexConcMorphNode catMorphNode:
										category = catMorphNode.Category;
										catNegated = catMorphNode.NegateCategory;
										break;
									case ComplexConcWordNode catWordNode:
										category = catWordNode.Category;
										catNegated = catWordNode.NegateCategory;
										break;
								}
								Debug.Assert(category != null);
								tss = CreateFeatureLine(ComplexConcordanceResources.ksComplexConcCategory, category.Abbreviation.BestAnalysisAlternative, catNegated);
								break;
							case ktagTag:
								ICmPossibility tagPoss = null;
								if (node is ComplexConcTagNode tagNode)
								{
									tagPoss = tagNode.Tag;
								}
								Debug.Assert(tagPoss != null);
								tss = CreateFeatureLine(ComplexConcordanceResources.ksComplexConcTag, tagPoss.Abbreviation.BestAnalysisAlternative, false);
								break;
							case ktagInfl:
								tss = CreateFeatureLine(ComplexConcordanceResources.ksComplexConcInflFeatures, null, false);
								break;
							default:
								var feature = m_curInflFeatures.Keys.Single(f => f.Hvo == tag);
								switch (feature)
								{
									case IFsComplexFeature _:
										tss = CreateFeatureLine(feature.Abbreviation.BestAnalysisAlternative, null, false);
										break;
									case IFsClosedFeature _:
										{
											var value = (ClosedFeatureValue)m_curInflFeatures[feature];
											tss = CreateFeatureLine(feature.Abbreviation.BestAnalysisAlternative, value.Symbol.Abbreviation.BestAnalysisAlternative, value.Negate);
											break;
										}
								}
								break;
						}
						break;
					case kfragNodeMax:
						// if the max value is -1, it indicates that it is infinite
						var node1 = ((ComplexConcPatternSda)vwenv.DataAccess).Nodes[vwenv.CurrentObject()];
						tss = node1.Maximum == -1 ? m_infinity : TsStringUtils.MakeString(node1.Maximum.ToString(CultureInfo.InvariantCulture), m_cache.DefaultUserWs);
						break;
					case kfragNodeMin:
						var node2 = ((ComplexConcPatternSda)vwenv.DataAccess).Nodes[vwenv.CurrentObject()];
						tss = TsStringUtils.MakeString(node2.Minimum.ToString(CultureInfo.InvariantCulture), m_cache.DefaultUserWs);
						break;
					case kfragOR:
						tss = m_or;
						break;
					case kfragHash:
						tss = m_hash;
						break;
					default:
						tss = base.DisplayVariant(vwenv, tag, frag);
						break;
				}
				return tss;
			}

			private void DisplayFeatures(IVwEnv vwenv, ComplexConcPatternNode node)
			{
				vwenv.AddProp(ktagType, this, InterlinLineChoices.kfragFeatureLine);
				switch (node)
				{
					case ComplexConcMorphNode morphNode:
						{
							if (morphNode.Form != null)
							{
								vwenv.AddProp(ktagForm, this, InterlinLineChoices.kfragFeatureLine);
							}
							if (morphNode.Entry != null)
							{
								vwenv.AddProp(ktagEntry, this, InterlinLineChoices.kfragFeatureLine);
							}
							if (morphNode.Category != null)
							{
								vwenv.AddProp(ktagCategory, this, InterlinLineChoices.kfragFeatureLine);
							}
							if (morphNode.Gloss != null)
							{
								vwenv.AddProp(ktagGloss, this, InterlinLineChoices.kfragFeatureLine);
							}
							if (!morphNode.InflFeatures.Any())
							{
								return;
							}
							vwenv.OpenParagraph();
							vwenv.AddProp(ktagInfl, this, InterlinLineChoices.kfragFeatureLine);
							DisplayInflFeatures(vwenv, morphNode.InflFeatures);
							vwenv.CloseParagraph();
							break;
						}
					case ComplexConcWordNode wordNode:
						{
							if (wordNode.Form != null)
							{
								vwenv.AddProp(ktagForm, this, InterlinLineChoices.kfragFeatureLine);
							}
							if (wordNode.Category != null)
							{
								vwenv.AddProp(ktagCategory, this, InterlinLineChoices.kfragFeatureLine);
							}
							if (wordNode.Gloss != null)
							{
								vwenv.AddProp(ktagGloss, this, InterlinLineChoices.kfragFeatureLine);
							}
							if (!wordNode.InflFeatures.Any())
							{
								return;
							}
							vwenv.OpenParagraph();
							vwenv.AddProp(ktagInfl, this, InterlinLineChoices.kfragFeatureLine);
							DisplayInflFeatures(vwenv, wordNode.InflFeatures);
							vwenv.CloseParagraph();
							break;
						}
					case ComplexConcTagNode tagNode when tagNode.Tag != null:
						vwenv.AddProp(ktagTag, this, InterlinLineChoices.kfragFeatureLine);
						break;
				}
			}

			private void DisplayInflFeatureLines(IVwEnv vwenv, IDictionary<IFsFeatDefn, object> inflFeatures, bool openPara)
			{
				var lastInflFeatures = m_curInflFeatures;
				m_curInflFeatures = inflFeatures;
				foreach (var kvp in inflFeatures)
				{
					if (kvp.Key is IFsComplexFeature)
					{
						if (openPara)
						{
							vwenv.OpenParagraph();
						}
						vwenv.AddProp(kvp.Key.Hvo, this, InterlinLineChoices.kfragFeatureLine);
						DisplayInflFeatures(vwenv, (IDictionary<IFsFeatDefn, object>)kvp.Value);
						if (openPara)
						{
							vwenv.CloseParagraph();
						}
					}
					else
					{
						vwenv.AddProp(kvp.Key.Hvo, this, InterlinLineChoices.kfragFeatureLine);
					}
				}
				m_curInflFeatures = lastInflFeatures;
			}

			private void DisplayInflFeatures(IVwEnv vwenv, IDictionary<IFsFeatDefn, object> inflFeatures)
			{
				var numLines = GetNumLines(inflFeatures);
				if (numLines == 1)
				{
					// use normal brackets for a single line constraint
					vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftBracket);
					DisplayInflFeatureLines(vwenv, inflFeatures, false);
					vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracket);
				}
				else
				{
					// left bracket pile
					vwenv.Props = m_bracketProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
					vwenv.OpenInnerPile();
					vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketUpHook);
					for (var i = 1; i < numLines - 1; i++)
					{
						vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketExt);
					}
					vwenv.AddProp(ktagLeftBoundary, this, kfragLeftBracketLowHook);
					vwenv.CloseInnerPile();
					// feature pile
					vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
					vwenv.OpenInnerPile();
					DisplayInflFeatureLines(vwenv, inflFeatures, true);
					vwenv.CloseInnerPile();
					// right bracket pile
					vwenv.Props = m_bracketProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
					vwenv.OpenInnerPile();
					vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracketUpHook);
					for (var i = 1; i < numLines - 1; i++)
					{
						vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracketExt);
					}
					vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracketLowHook);
					vwenv.CloseInnerPile();
				}
			}

			private ITsString CreateFeatureLine(ITsString name, ITsString value, bool negated)
			{
				var featLine = TsStringUtils.MakeIncStrBldr();
				featLine.AppendTsString(name);
				featLine.Append(": ");
				if (value != null)
				{
					if (negated)
					{
						featLine.AppendTsString(TsStringUtils.MakeString("!", m_cache.DefaultUserWs));
					}
					featLine.AppendTsString(value);
				}
				return featLine.GetString();
			}

			private ITsString CreateFeatureLine(string name, ITsString value, bool negated)
			{
				return CreateFeatureLine(TsStringUtils.MakeString(name, m_cache.DefaultUserWs), value, negated);
			}

			private ITsString CreateFeatureLine(string name, string value, int ws)
			{
				return CreateFeatureLine(name, TsStringUtils.MakeString(value, ws), false);
			}

			private static int GetMaxNumLines(IVwEnv vwenv)
			{
				return GetNumLines(((ComplexConcPatternSda)vwenv.DataAccess).Root);
			}

			private static int GetNumLines(ComplexConcPatternNode node)
			{
				switch (node)
				{
					case ComplexConcMorphNode morphNode:
						{
							var numLines = 1;
							if (morphNode.Form != null)
							{
								numLines++;
							}
							if (morphNode.Entry != null)
							{
								numLines++;
							}
							if (morphNode.Gloss != null)
							{
								numLines++;
							}
							if (morphNode.Category != null)
							{
								numLines++;
							}
							numLines += GetNumLines(morphNode.InflFeatures);
							return numLines;
						}
					case ComplexConcWordNode wordNode:
						{
							var numLines = 1;
							if (wordNode.Form != null)
							{
								numLines++;
							}
							if (wordNode.Gloss != null)
							{
								numLines++;
							}
							if (wordNode.Category != null)
							{
								numLines++;
							}
							numLines += GetNumLines(wordNode.InflFeatures);
							return numLines;
						}
					case ComplexConcTagNode tagNode:
						{
							var numLines = 1;
							if (tagNode.Tag != null)
							{
								numLines++;
							}
							return numLines;
						}
					default:
						return !node.IsLeaf ? node.Children.Max(GetNumLines) : 1;
				}
			}

			private static int GetNumLines(IDictionary<IFsFeatDefn, object> inflFeatures)
			{
				var num = 0;
				foreach (var kvp in inflFeatures)
				{
					if (kvp.Key is IFsComplexFeature)
					{
						num += GetNumLines((IDictionary<IFsFeatDefn, object>)kvp.Value);
					}
					else
					{
						num++;
					}
				}
				return num;
			}
		}
	}
}