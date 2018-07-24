// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Grammar.Tools.PosEdit
{
	/// <summary>
	/// This class represents a rule formula control for a metathesis rule. It consists of
	/// four editable cells: left context, right context, left switch, and right switch. The
	/// data for each cell is contained within the <c>StrucDesc</c> and <c>StrucChange</c> fields
	/// in <c>PhMetathesisRule</c> class. <c>StrucDesc</c> is an owning sequence of phonological
	/// contexts. The <c>StrucChange</c> field is a string field that contains five indices. Each
	/// index is used to represent what contexts in <c>StrucDesc</c> are associated with each of
	/// the four cells.
	/// </summary>
	internal sealed class MetaRuleFormulaControl : RuleFormulaControl
	{
		public MetaRuleFormulaControl(ISharedEventHandlers sharedEventHandlers, XElement configurationNode)
			: base(sharedEventHandlers, configurationNode)
		{
		}

		public override bool SliceIsCurrent
		{
			set
			{
				if (value)
				{
					_view.Select();
				}
			}
		}

		private IPhMetathesisRule Rule => m_obj as IPhMetathesisRule;

		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);

			_view.Init(obj.Hvo, this, new MetaRuleFormulaVc(cache, PropertyTable), MetaRuleFormulaVc.kfragRule, cache.MainCacheAccessor);

			InsertionControl.AddOption(new InsertOption(RuleInsertType.Phoneme), DisplayOption);
			InsertionControl.AddOption(new InsertOption(RuleInsertType.NaturalClass), DisplayOption);
			InsertionControl.AddOption(new InsertOption(RuleInsertType.Features), DisplayOption);
			InsertionControl.AddOption(new InsertOption(RuleInsertType.WordBoundary), DisplayOption);
			InsertionControl.AddOption(new InsertOption(RuleInsertType.MorphemeBoundary), DisplayOption);
			InsertionControl.NoOptionsMessage = DisplayNoOptsMsg;
		}

		private bool DisplayOption(object option)
		{
			var type = ((InsertOption) option).Type;
			var sel = SelectionHelper.Create(_view);
			var cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
			{
				return false;
			}

			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftSwitch:
					switch (type)
					{
						case RuleInsertType.WordBoundary:
							return false;

						case RuleInsertType.MorphemeBoundary:
							if (Rule.LeftSwitchIndex == -1 || Rule.MiddleIndex != -1 || sel.IsRange)
							{
								return false;
							}
							return GetInsertionIndex(Rule.StrucDescOS.Cast<ICmObject>().ToArray(), sel) == Rule.LeftSwitchIndex + 1;

						default:
							if (Rule.LeftSwitchIndex == -1)
							{
								return true;
							}

							if (!sel.IsRange)
							{
								return false;
							}
							var beginObj = GetCmObject(sel, SelectionHelper.SelLimitType.Top);
							var endObj = GetCmObject(sel, SelectionHelper.SelLimitType.Bottom);

							IPhSimpleContext lastCtxt;
							if (Rule.MiddleIndex != -1 && Rule.IsMiddleWithLeftSwitch)
							{
								lastCtxt = Rule.StrucDescOS[Rule.MiddleLimit - 1];
							}
							else
							{
								lastCtxt = Rule.StrucDescOS[Rule.LeftSwitchLimit - 1];
							}

							return beginObj == Rule.StrucDescOS[Rule.LeftSwitchIndex] && endObj == lastCtxt;
					}

				case PhMetathesisRuleTags.kidxRightSwitch:
					switch (type)
					{
						case RuleInsertType.WordBoundary:
							return false;

						case RuleInsertType.MorphemeBoundary:
							if (Rule.RightSwitchIndex == -1 || Rule.MiddleIndex != -1 || sel.IsRange)
							{
								return false;
							}
							return GetInsertionIndex(Rule.StrucDescOS.Cast<ICmObject>().ToArray(), sel) == Rule.RightSwitchIndex;

						default:
							if (Rule.RightSwitchIndex == -1)
							{
								return true;
							}

							if (!sel.IsRange)
							{
								return false;
							}
							var beginObj = GetCmObject(sel, SelectionHelper.SelLimitType.Top);
							var endObj = GetCmObject(sel, SelectionHelper.SelLimitType.Bottom);

							IPhSimpleContext firstCtxt;
							if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
							{
								firstCtxt = Rule.StrucDescOS[Rule.MiddleIndex];
							}
							else
							{
								firstCtxt = Rule.StrucDescOS[Rule.RightSwitchIndex];
							}

							return beginObj == firstCtxt && endObj == Rule.StrucDescOS[Rule.RightSwitchLimit - 1];
					}

				case PhMetathesisRuleTags.kidxLeftEnv:
					var leftCtxts = Rule.StrucDescOS.Cast<ICmObject>().ToArray();
					IPhSimpleContext first = null;
					if (Rule.StrucDescOS.Count > 0)
					{
						first = Rule.StrucDescOS[0];
					}

					if (type == RuleInsertType.WordBoundary)
					{
						// only display the word boundary option if we are at the beginning of the left context and
						// there is no word boundary already inserted
						if (sel.IsRange)
						{
							return GetIndicesToRemove(leftCtxts, sel)[0] == 0;
						}
						return GetInsertionIndex(leftCtxts, sel) == 0 && !IsWordBoundary(first);
					}
					// we cannot insert anything to the left of a word boundary in the left context
					return sel.IsRange || GetInsertionIndex(leftCtxts, sel) != 0 || !IsWordBoundary(first);

				case PhMetathesisRuleTags.kidxRightEnv:
					var rightCtxts = Rule.StrucDescOS.Cast<ICmObject>().ToArray();
					IPhSimpleContext last = null;
					if (Rule.StrucDescOS.Count > 0)
					{
						last = Rule.StrucDescOS[Rule.StrucDescOS.Count - 1];
					}
					if (type == RuleInsertType.WordBoundary)
					{
						// only display the word boundary option if we are at the end of the right context and
						// there is no word boundary already inserted
						if (sel.IsRange)
						{
							var indices = GetIndicesToRemove(rightCtxts, sel);
							return indices[indices.Length - 1] == rightCtxts.Length - 1;
						}
						return GetInsertionIndex(rightCtxts, sel) == rightCtxts.Length && !IsWordBoundary(last);
					}
					// we cannot insert anything to the right of a word boundary in the right context
					return sel.IsRange || GetInsertionIndex(rightCtxts, sel) != rightCtxts.Length || !IsWordBoundary(last);
			}

			return false;
		}

		private string DisplayNoOptsMsg()
		{
			var sel = SelectionHelper.Create(_view);
			var cellId = GetCell(sel);
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftSwitch:
				case PhMetathesisRuleTags.kidxRightSwitch:
					return AreaResources.ksMetaRuleNoOptsMsg;

				case PhMetathesisRuleTags.kidxLeftEnv:
				case PhMetathesisRuleTags.kidxRightEnv:
					return AreaResources.ksRuleWordBdryNoOptsMsg;
			}
			return null;
		}

		protected override string FeatureChooserHelpTopic => "khtpChoose-Grammar-PhonFeats-MetaRuleFormulaControl";

		protected override string RuleName => Rule.Name.BestAnalysisAlternative.Text;

		protected override Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateContextMenu()
		{
			// Start: <menu id="mnuPhMetathesisRule">

			const string mnuPhMetathesisRule = "mnuPhMetathesisRule";

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuPhMetathesisRule
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(4);

			ToolStripMenuItem menu;
			if (IsFeatsNCContextCurrent)
			{
				// <command id="CmdCtxtSetFeatures" label="Set Phonological Features..." message="ContextSetFeatures" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.ContextSetFeatures), AreaResources.Set_Phonological_Features);
			}
			// Need to remember where to insert the separator, if it is needed, at all.
			var separatorInsertIndex = menuItems.Count - 1;

			// <item label="-" translate="do not translate" /> Optionally inserted at separatorInsertIndex. See below.

			if (IsNCContextCurrent)
			{
				// <command id="CmdCtxtJumpToNC" label="Show in Natural Classes list" message="ContextJumpToNaturalClass" />
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_in_Natural_Classes_list);
				menu.Tag = new List<object> { Publisher, AreaServices.NaturalClassEditMachineName, ((IPhSimpleContextSeg)MyRuleFormulaSlice.RuleFormulaControl.CurrentContext).FeatureStructureRA.Guid };
			}

			if (IsPhonemeContextCurrent)
			{
				// <command id="CmdCtxtJumpToPhoneme" label="Show in Phonemes list" message="ContextJumpToPhoneme" />
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_in_Phonemes_list);
				menu.Tag = new List<object> { Publisher, AreaServices.PhonemeEditMachineName, ((IPhSimpleContextSeg)MyRuleFormulaSlice.RuleFormulaControl.CurrentContext).FeatureStructureRA.Guid };
			}

			if (separatorInsertIndex > 0 && separatorInsertIndex < menuItems.Count - 1)
			{
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertIndex);
			}

			// End: <menu id="mnuPhMetathesisRule">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		protected override int GetNextCell(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					return PhMetathesisRuleTags.kidxLeftSwitch;
				case PhMetathesisRuleTags.kidxLeftSwitch:
					return PhMetathesisRuleTags.kidxRightSwitch;
				case PhMetathesisRuleTags.kidxRightSwitch:
					return PhMetathesisRuleTags.kidxRightEnv;
				case PhMetathesisRuleTags.kidxRightEnv:
					return -1;
			}
			return -1;
		}

		protected override int GetPrevCell(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					return -1;
				case PhMetathesisRuleTags.kidxLeftSwitch:
					return PhMetathesisRuleTags.kidxLeftEnv;
				case PhMetathesisRuleTags.kidxRightSwitch:
					return PhMetathesisRuleTags.kidxLeftSwitch;
				case PhMetathesisRuleTags.kidxRightEnv:
					return PhMetathesisRuleTags.kidxRightSwitch;
			}
			return -1;
		}

		protected override int GetCell(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			var tag = sel.GetTextPropId(limit);
			switch (tag)
			{
				case MetaRuleFormulaVc.ktagLeftEnv:
					return PhMetathesisRuleTags.kidxLeftEnv;
				case MetaRuleFormulaVc.ktagLeftSwitch:
					return PhMetathesisRuleTags.kidxLeftSwitch;
				case MetaRuleFormulaVc.ktagRightSwitch:
					return PhMetathesisRuleTags.kidxRightSwitch;
				case MetaRuleFormulaVc.ktagRightEnv:
					return PhMetathesisRuleTags.kidxRightEnv;
			}

			var obj = GetCmObject(sel, limit);
			if (obj == null)
				return -1;

			return Rule.GetStrucChangeIndex((IPhSimpleContext) obj);
		}

		protected override ICmObject GetCmObject(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (Rule.StrucDescOS.Count == 0 || sel.GetNumberOfLevels(limit) == 0)
			{
				return null;
			}

			var levels = sel.GetLevelInfo(limit);
			return m_cache.ServiceLocator.GetObject(levels[levels.Length - 1].hvo);
		}

		protected override int GetItemCellIndex(int cellId, ICmObject obj)
		{
			if (obj == null)
			{
				return -1;
			}
			return ConvertToCellIndex(cellId, obj.IndexInOwner);
		}

		private int ConvertToCellIndex(int cellId, int index)
		{
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					if (Rule.LeftEnvIndex == -1)
					{
						index = -1;
					}
					break;
				case PhMetathesisRuleTags.kidxLeftSwitch:
					if (Rule.LeftSwitchIndex == -1)
					{
						index = -1;
					}
					else
					{
						index -= Rule.LeftSwitchIndex;
					}
					break;
				case PhMetathesisRuleTags.kidxRightSwitch:
					if (Rule.RightSwitchIndex == -1)
					{
						index = -1;
					}
					else if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
					{
						index -= Rule.MiddleIndex;
					}
					else
					{
						index -= Rule.RightSwitchIndex;
					}
					break;
				case PhMetathesisRuleTags.kidxRightEnv:
					if (Rule.RightEnvIndex == -1)
					{
						index = -1;
					}
					else
					{
						index -= Rule.RightEnvIndex;
					}
					break;
			}
			return index;
		}

		protected override SelLevInfo[] GetLevelInfo(int cellId, int cellIndex)
		{
			if (cellIndex <= -1)
			{
				return null;
			}

			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftSwitch:
					cellIndex += Rule.LeftSwitchIndex;
					break;
				case PhMetathesisRuleTags.kidxRightSwitch:
					if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
					{
						cellIndex += Rule.MiddleIndex;
					}
					else
					{
						cellIndex += Rule.RightSwitchIndex;
					}
					break;
				case PhMetathesisRuleTags.kidxRightEnv:
					cellIndex += Rule.RightEnvIndex;
					break;
			}

			var levels = new SelLevInfo[1];
			levels[0].cpropPrevious = cellIndex;
			levels[0].tag = -1;
			return levels;
		}

		protected override int GetCellCount(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					if (Rule.LeftEnvIndex == -1)
					{
						return 0;
					}
					return Rule.LeftEnvLimit;

				case PhMetathesisRuleTags.kidxLeftSwitch:
					if (Rule.LeftSwitchIndex == -1)
					{
						return 0;
					}
					var leftMidCount = 0;
					if (Rule.MiddleIndex != -1 && Rule.IsMiddleWithLeftSwitch)
					{
						leftMidCount = Rule.MiddleLimit - Rule.MiddleIndex;
					}
					return (Rule.LeftSwitchLimit - Rule.LeftSwitchIndex) + leftMidCount;

				case PhMetathesisRuleTags.kidxRightSwitch:
					if (Rule.RightSwitchIndex == -1)
					{
						return 0;
					}
					var rightMidCount = 0;
					if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
					{
						rightMidCount = Rule.MiddleLimit - Rule.MiddleIndex;
					}
					return (Rule.RightSwitchLimit - Rule.RightSwitchIndex) + rightMidCount;

				case PhMetathesisRuleTags.kidxRightEnv:
					if (Rule.RightEnvIndex == -1)
					{
						return 0;
					}
					return Rule.RightEnvLimit - Rule.RightEnvIndex;
			}
			return 0;
		}

		protected override int GetFlid(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					return MetaRuleFormulaVc.ktagLeftEnv;
				case PhMetathesisRuleTags.kidxLeftSwitch:
					return MetaRuleFormulaVc.ktagLeftSwitch;
				case PhMetathesisRuleTags.kidxRightSwitch:
					return MetaRuleFormulaVc.ktagRightSwitch;
				case PhMetathesisRuleTags.kidxRightEnv:
					return MetaRuleFormulaVc.ktagRightEnv;
			}
			return -1;
		}

		protected override int InsertPhoneme(IPhPhoneme phoneme, SelectionHelper sel, out int cellIndex)
		{
			var segCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			var cellId = InsertContext(segCtxt, sel, out cellIndex);
			segCtxt.FeatureStructureRA = phoneme;
			return cellId;
		}

		protected override int InsertBdry(IPhBdryMarker bdry, SelectionHelper sel, out int cellIndex)
		{
			var bdryCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
			var cellId = InsertContext(bdryCtxt, sel, out cellIndex);
			bdryCtxt.FeatureStructureRA = bdry;
			return cellId;
		}

		protected override int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex, out IPhSimpleContextNC ctxt)
		{
			ctxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			var cellId = InsertContext(ctxt, sel, out cellIndex);
			ctxt.FeatureStructureRA = nc;
			return cellId;
		}

		private int InsertContext(IPhSimpleContext ctxt, SelectionHelper sel, out int cellIndex)
		{
			var cellId = GetCell(sel);
			var index = InsertContextInto(ctxt, sel, Rule.StrucDescOS);
			Rule.UpdateStrucChange(cellId, index, true);
			cellIndex = ConvertToCellIndex(cellId, index);
			return cellId;
		}

		protected override int GetInsertionIndex(ICmObject[] objs, SelectionHelper sel)
		{
			if (sel.GetNumberOfLevels(SelectionHelper.SelLimitType.Top) != 0)
			{
				return base.GetInsertionIndex(objs, sel);
			}
			var cellId = GetCell(sel, SelectionHelper.SelLimitType.Top);
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					return 0;

				case PhMetathesisRuleTags.kidxLeftSwitch:
					if (Rule.MiddleIndex != -1)
					{
						return Rule.MiddleIndex;
					}

					if (Rule.RightSwitchIndex != -1)
					{
						return Rule.RightSwitchIndex;
					}

					if (Rule.RightEnvIndex != -1)
					{
						return Rule.RightEnvIndex;
					}
					break;

				case PhMetathesisRuleTags.kidxRightSwitch:
					if (Rule.RightEnvIndex != -1)
					{
						return Rule.RightEnvIndex;
					}
					break;
			}
			return objs.Length;
		}

		protected override int RemoveItems(SelectionHelper sel, bool forward, out int cellIndex)
		{
			cellIndex = -1;
			var cellId = GetCell(sel);
			int index;
			// PhMetathesisRule.UpdateStrucChange does not need to be called here, because it is called in
			// PhSimpleContext.DeleteObjectSideEffects
			var reconstruct = RemoveContextsFrom(forward, sel, Rule.StrucDescOS, true, out index);
			if (reconstruct)
			{
				cellIndex = ConvertToCellIndex(cellId, index);
			}
			return reconstruct ? cellId : -1;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (_view == null)
			{
				return;
			}
			var w = Width;
			_view.Width = w > 0 ? w : 0;
		}
	}
}