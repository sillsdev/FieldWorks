// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This class represents an affix process rule formula control. An affix process
	/// is represented by a left input empty cell, a right input empty cell, a cell for
	/// each input phonological context, and a result cell. The input empty cells are simply
	/// used to insert new contexts in to a <c>MoAffixProcess</c>. The context cells represent
	/// the contexts in the <c>Input</c> field of <c>MoAffixProcess</c>. The data in the result
	/// cell consists of the rule mapping objects in the <c>Output</c> field.
	/// </summary>
	internal sealed class AffixRuleFormulaControl : RuleFormulaControl
	{
		// column that is scheduled to be removed
		private IPhContextOrVar m_removeCol;

		public AffixRuleFormulaControl(ISharedEventHandlers sharedEventHandlers, XElement configurationNode)
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

		/// <summary>
		/// Indicates that a rule mapping that points to an input sequence is currently selected.
		/// </summary>
		public bool IsIndexCurrent
		{
			get
			{
				var obj = CurrentObject;
				if (obj.ClassID != MoCopyFromInputTags.kClassId)
				{
					return obj.ClassID == MoModifyFromInputTags.kClassId;
				}
				var copy = (IMoCopyFromInput)obj;
				// we don't want to change a MoCopyFromInput to a MoModifyFromInput if it is pointing to
				// a variable
				return copy.ContentRA.ClassID != PhVariableTags.kClassId;
			}
		}

		/// <summary>
		/// Indicates that a insert phonemes rule mapping is currently selected.
		/// </summary>
		public bool IsPhonemeCurrent => CurrentObject.ClassID == MoInsertPhonesTags.kClassId;

		/// <summary>
		/// Indicates that a modify from input rule mapping is currently selected.
		/// </summary>
		public bool IsNCIndexCurrent => CurrentObject.ClassID == MoModifyFromInputTags.kClassId;

		IMoAffixProcess Rule => (IMoAffixProcess)m_obj;

		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);

			// Don't even 'think' of calling: m_view.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			// I know, I did but it crashed, since it has been done already.
			_view.Init(obj.Hvo, this, new AffixRuleFormulaVc(cache, PropertyTable), AffixRuleFormulaVc.kfragRule, cache.MainCacheAccessor);

			_view.SelectionChanged += SelectionChanged;

			InsertionControl.AddOption(new InsertOption(RuleInsertType.Phoneme), DisplayOption);
			InsertionControl.AddOption(new InsertOption(RuleInsertType.NaturalClass), DisplayOption);
			InsertionControl.AddOption(new InsertOption(RuleInsertType.Features), DisplayOption);
			InsertionControl.AddOption(new InsertOption(RuleInsertType.MorphemeBoundary), DisplayOption);
			InsertionControl.AddOption(new InsertOption(RuleInsertType.Variable), DisplayVariableOption);
			InsertionControl.AddOption(new InsertOption(RuleInsertType.Column), DisplayColumnOption);
			InsertionControl.AddMultiOption(new InsertOption(RuleInsertType.Index), DisplayOption, DisplayIndices);
			InsertionControl.NoOptionsMessage = DisplayNoOptsMsg;
		}

		private bool DisplayOption(object option)
		{
			var type = ((InsertOption)option).Type;
			var sel = SelectionHelper.Create(_view);
			var cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
			{
				return false;
			}
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
					return type != RuleInsertType.Index;
				case MoAffixProcessTags.kflidOutput:
					return type == RuleInsertType.Index || type == RuleInsertType.Phoneme || type == RuleInsertType.MorphemeBoundary;
				default:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					if (ctxtOrVar.ClassID == PhVariableTags.kClassId)
					{
						return false;
					}
					return type != RuleInsertType.Index;
			}
		}

		private bool DisplayVariableOption(object option)
		{
			var sel = SelectionHelper.Create(_view);
			var cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
			{
				return false;
			}

			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
					return true;
				case MoAffixProcessTags.kflidOutput:
					return false;
				default:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					if (ctxtOrVar.ClassID != PhSequenceContextTags.kClassId)
					{
						return false;
					}
					var seqCtxt = (IPhSequenceContext)ctxtOrVar;
					if (seqCtxt.MembersRS.Count == 0)
					{
						return true;
					}
					return false;
			}
		}

		private bool DisplayColumnOption(object option)
		{
			var sel = SelectionHelper.Create(_view);
			if (sel.IsRange)
			{
				return false;
			}
			var cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
			{
				return false;
			}
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
				case MoAffixProcessTags.kflidOutput:
					return false;
				default:
					return GetColumnInsertIndex(sel) != -1;
			}
		}

		private IEnumerable<object> DisplayIndices()
		{
			var indices = new int[Rule.InputOS.Count];
			for (var i = 0; i < indices.Length; i++)
			{
				indices[i] = i + 1;
			}
			return indices.Cast<object>();
		}

		private string DisplayNoOptsMsg()
		{
			var sel = SelectionHelper.Create(_view);
			var cellId = GetCell(sel);
			if (cellId == -1 || cellId == 2)
			{
				return null;
			}
			return AreaResources.ksAffixRuleNoOptsMsg;
		}

		protected override string FeatureChooserHelpTopic => "khtpChoose-LexiconEdit-PhonFeats-AffixRuleFormulaControl";

		protected override string RuleName => Rule.Form.BestVernacularAnalysisAlternative.Text;

		#region Overrides of RuleFormulaControl

		/// <inheritdoc />
		protected override Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateContextMenu()
		{
			// Start: <menu id="mnuMoAffixProcess">
			const string mnuMoAffixProcess = "mnuMoAffixProcess";
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuMoAffixProcess
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(8);
			if (IsFeatsNCContextCurrent)
			{
				// <command id="CmdCtxtSetFeatures" label="Set Phonological Features..." message="ContextSetFeatures" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.ContextSetFeatures), AreaResources.Set_Phonological_Features);
			}
			if (IsIndexCurrent)
			{
				// Visible & Enabled only when "IsIndexCurrent".
				// <command id="CmdMappingSetFeatures" label="Set Phonological Features..." message="MappingSetFeatures" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MappingSetFeatures_Clicked, AreaResources.Set_Phonological_Features);

				// <command id="CmdMappingSetNC" label="Set Natural Class..." message="MappingSetNaturalClass" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MappingSetNaturalClass_Clicked, LexiconResources.Set_Natural_Class);
			}
			// Need to remember where to insert the separator, if it is needed, at all.
			var separatorOneInsertIndex = menuItems.Count - 1;

			// <item label="-" translate="do not translate" /> Optionally inserted at separatorOneInsertIndex. See below.

			ToolStripMenuItem menu;
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

			// <item label="-" translate="do not translate" /> Optionally inserted at separatorTwoInsertIndex. See below.
			var separatorTwoInsertIndex = menuItems.Count - 1;

			if (IsNCIndexCurrent)
			{
				// <command id="CmdMappingJumpToNC" label="Show in Natural Classes list" message="MappingJumpToNaturalClass" />
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_in_Natural_Classes_list);
				menu.Tag = new List<object> { Publisher, AreaServices.NaturalClassEditMachineName, ((IMoModifyFromInput)CurrentObject).ModificationRA.Guid };
			}

			if (IsPhonemeCurrent)
			{
				// <command id="CmdMappingJumpToPhoneme" label="Show in Phonemes list" message="MappingJumpToPhoneme" />
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_in_Phonemes_list);
				menu.Tag = new List<object> { Publisher, AreaServices.PhonemeEditMachineName, ((IMoInsertPhones)CurrentObject).ContentRS[0].Guid };
			}

			if (separatorOneInsertIndex > 0 && separatorOneInsertIndex < menuItems.Count - 1)
			{
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorOneInsertIndex);
			}
			if (separatorTwoInsertIndex > separatorOneInsertIndex && separatorTwoInsertIndex < menuItems.Count - 1)
			{
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorTwoInsertIndex);
			}

			// End: <menu id="mnuMoAffixProcess">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void MappingSetFeatures_Clicked(object sender, EventArgs e)
		{
			SetMappingFeatures();
		}

		private void MappingSetNaturalClass_Clicked(object sender, EventArgs e)
		{
			SetMappingNaturalClass();
		}

		#endregion

		protected override int GetCell(SelectionHelper sel, SelLimitType limit)
		{
			if (sel == null)
			{
				return -1;
			}
			var tag = sel.GetTextPropId(limit);
			if (tag == AffixRuleFormulaVc.ktagLeftEmpty || tag == AffixRuleFormulaVc.ktagRightEmpty || tag == MoAffixProcessTags.kflidOutput)
			{
				return tag;
			}
			foreach (var level in sel.GetLevelInfo(limit))
			{
				switch (level.tag)
				{
					case MoAffixProcessTags.kflidOutput:
						return level.tag;
					case MoAffixProcessTags.kflidInput:
						return level.hvo;
				}
			}
			return -1;
		}

		protected override ICmObject GetCmObject(SelectionHelper sel, SelLimitType limit)
		{
			return sel?.GetLevelInfo(limit)
				.Where(level => level.tag == MoAffixProcessTags.kflidInput || level.tag == PhSequenceContextTags.kflidMembers || level.tag == MoAffixProcessTags.kflidOutput)
				.Select(level => m_cache.ServiceLocator.GetObject(level.hvo)).FirstOrDefault();
		}

		protected override int GetItemCellIndex(int cellId, ICmObject obj)
		{
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
					return -1;
				case MoAffixProcessTags.kflidOutput:
					return obj.IndexInOwner;
				default:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					if (obj.ClassID != PhSequenceContextTags.kClassId)
					{
						return -1;
					}
					var seqCtxt = (IPhSequenceContext)ctxtOrVar;
					return seqCtxt.MembersRS.IndexOf(obj as IPhPhonContext);
			}
		}

		protected override SelLevInfo[] GetLevelInfo(int cellId, int cellIndex)
		{
			SelLevInfo[] levels = null;
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
					break;
				case MoAffixProcessTags.kflidOutput:
					if (cellIndex >= 0)
					{
						levels = new SelLevInfo[1];
						levels[0].tag = cellId;
						levels[0].ihvo = cellIndex;
					}
					break;
				default:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					if (cellIndex < 0 || ctxtOrVar.ClassID != PhSequenceContextTags.kClassId)
					{
						levels = new SelLevInfo[1];
						levels[0].tag = MoAffixProcessTags.kflidInput;
						levels[0].ihvo = ctxtOrVar.IndexInOwner;
					}
					else
					{
						levels = new SelLevInfo[2];
						levels[0].tag = PhSequenceContextTags.kflidMembers;
						levels[0].ihvo = cellIndex;
						levels[1].tag = MoAffixProcessTags.kflidInput;
						levels[1].ihvo = ctxtOrVar.IndexInOwner;
					}
					break;
			}
			return levels;
		}

		protected override int GetCellCount(int cellId)
		{
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
					return 0;
				case MoAffixProcessTags.kflidOutput:
					return Rule.OutputOS.Count;
				default:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					if (ctxtOrVar.ClassID == PhSequenceContextTags.kClassId)
					{
						return ((IPhSequenceContext)ctxtOrVar).MembersRS.Count;
					}
					return 1;
			}
		}

		protected override int GetNextCell(int cellId)
		{
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
					return Rule.InputOS[0].Hvo;
				case AffixRuleFormulaVc.ktagRightEmpty:
					return MoAffixProcessTags.kflidOutput;
				case MoAffixProcessTags.kflidOutput:
					return -1;
				default:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					var index = ctxtOrVar.IndexInOwner;
					if (index == Rule.InputOS.Count - 1)
					{
						return AffixRuleFormulaVc.ktagRightEmpty;
					}
					return Rule.InputOS[index + 1].Hvo;
			}
		}

		protected override int GetPrevCell(int cellId)
		{
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
					return -1;
				case AffixRuleFormulaVc.ktagRightEmpty:
					return Rule.InputOS[Rule.InputOS.Count - 1].Hvo;
				case MoAffixProcessTags.kflidOutput:
					return AffixRuleFormulaVc.ktagRightEmpty;
				default:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					var index = ctxtOrVar.IndexInOwner;
					if (index == 0)
					{
						return AffixRuleFormulaVc.ktagLeftEmpty;
					}
					return Rule.InputOS[index - 1].Hvo;
			}
		}

		protected override int GetFlid(int cellId)
		{
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
				case MoAffixProcessTags.kflidOutput:
					return cellId;
				default:
					return MoAffixProcessTags.kflidOutput;
			}
		}

		protected override int InsertPhoneme(IPhPhoneme phoneme, SelectionHelper sel, out int cellIndex)
		{
			var cellId = GetCell(sel);
			if (cellId == MoAffixProcessTags.kflidOutput)
			{
				var insertPhones = m_cache.ServiceLocator.GetInstance<IMoInsertPhonesFactory>().Create();
				cellIndex = InsertIntoOutput(insertPhones, sel);
				insertPhones.ContentRS.Add(phoneme);
			}
			else
			{
				var seqCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
				cellId = InsertContext(seqCtxt, sel, out cellIndex);
				seqCtxt.FeatureStructureRA = phoneme;
			}
			return cellId;
		}

		protected override int InsertBdry(IPhBdryMarker bdry, SelectionHelper sel, out int cellIndex)
		{
			var cellId = GetCell(sel);
			if (cellId == MoAffixProcessTags.kflidOutput)
			{
				var insertPhones = m_cache.ServiceLocator.GetInstance<IMoInsertPhonesFactory>().Create();
				cellIndex = InsertIntoOutput(insertPhones, sel);
				insertPhones.ContentRS.Add(bdry);
			}
			else
			{
				var bdryCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
				cellId = InsertContext(bdryCtxt, sel, out cellIndex);
				bdryCtxt.FeatureStructureRA = bdry;
			}
			return cellId;
		}

		protected override int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex, out IPhSimpleContextNC ctxt)
		{
			ctxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			var cellId = InsertContext(ctxt, sel, out cellIndex);
			ctxt.FeatureStructureRA = nc;
			return cellId;
		}

		private int InsertIntoOutput(IMoRuleMapping mapping, SelectionHelper sel)
		{
			var mappings = Rule.OutputOS.Cast<ICmObject>().ToArray();
			var index = GetInsertionIndex(mappings, sel);
			Rule.OutputOS.Insert(index, mapping);
			if (!sel.IsRange)
			{
				return index;
			}
			foreach (var idx in GetIndicesToRemove(mappings, sel))
			{
				Rule.OutputOS.Remove((IMoRuleMapping)mappings[idx]);
			}
			return index;
		}

		private int InsertContext(IPhContextOrVar ctxtOrVar, SelectionHelper sel, out int cellIndex)
		{
			m_removeCol = null;
			var cellId = GetCell(sel);
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
					Rule.InputOS.Insert(0, ctxtOrVar);
					cellIndex = -1;
					return ctxtOrVar.Hvo;
				case AffixRuleFormulaVc.ktagRightEmpty:
					Rule.InputOS.Add(ctxtOrVar);
					cellIndex = -1;
					return ctxtOrVar.Hvo;
				default:
					var cellCtxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					if (ctxtOrVar.ClassID == PhVariableTags.kClassId)
					{
						var index = cellCtxtOrVar.IndexInOwner;
						Rule.InputOS.Insert(index, ctxtOrVar);
						UpdateMappings(cellCtxtOrVar, ctxtOrVar);
						Rule.InputOS.Remove(cellCtxtOrVar);
						cellIndex = -1;
						return ctxtOrVar.Hvo;
					}
					var seqCtxt = CreateSeqCtxt(cellCtxtOrVar as IPhPhonContext);
					cellIndex = InsertContextInto(ctxtOrVar as IPhSimpleContext, sel, seqCtxt);
					return seqCtxt.Hvo;
			}
		}

		private IPhSequenceContext CreateSeqCtxt(IPhPhonContext ctxt)
		{
			IPhSequenceContext seqCtxt;
			if (ctxt.ClassID != PhSequenceContextTags.kClassId)
			{
				var index = ctxt.IndexInOwner;
				m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ctxt);
				seqCtxt = m_cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
				Rule.InputOS.Insert(index, seqCtxt);
				seqCtxt.MembersRS.Add(ctxt);
				UpdateMappings(ctxt, seqCtxt);
			}
			else
			{
				seqCtxt = ctxt as IPhSequenceContext;
			}
			return seqCtxt;
		}

		protected override int InsertColumn(SelectionHelper sel)
		{
			var index = GetColumnInsertIndex(sel);
			var seqCtxt = m_cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
			Rule.InputOS.Insert(index, seqCtxt);
			return seqCtxt.Hvo;
		}

		int GetColumnInsertIndex(SelectionHelper sel)
		{
			var hvo = GetCell(sel);
			if (hvo <= 0)
			{
				return -1;
			}
			ICmObject[] ctxtOrVars;
			var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(hvo);
			if (ctxtOrVar.ClassID != PhSequenceContextTags.kClassId)
			{
				ctxtOrVars = new ICmObject[] { ctxtOrVar };
			}
			else
			{
				var seqCtxt = (IPhSequenceContext)ctxtOrVar;
				ctxtOrVars = seqCtxt.MembersRS.Cast<ICmObject>().ToArray();
			}
			if (ctxtOrVars.Length == 0)
			{
				return -1;
			}
			var insertIndex = GetInsertionIndex(ctxtOrVars, sel);
			if (insertIndex == 0 && ctxtOrVar.IndexInOwner != 0)
			{
				var prev = Rule.InputOS[ctxtOrVar.IndexInOwner - 1];
				if (GetCellCount(prev.Hvo) > 0)
				{
					return ctxtOrVar.IndexInOwner;
				}
			}
			else if (insertIndex == ctxtOrVars.Length && ctxtOrVar.IndexInOwner != Rule.InputOS.Count - 1)
			{
				var next = Rule.InputOS[ctxtOrVar.IndexInOwner + 1];
				if (GetCellCount(next.Hvo) > 0)
				{
					return next.IndexInOwner;
				}
			}
			return -1;
		}

		protected override int InsertIndex(int index, SelectionHelper sel, out int cellIndex)
		{
			var copy = m_cache.ServiceLocator.GetInstance<IMoCopyFromInputFactory>().Create();
			cellIndex = InsertIntoOutput(copy, sel);
			copy.ContentRA = Rule.InputOS[index - 1];
			return MoAffixProcessTags.kflidOutput;
		}

		protected override int InsertVariable(SelectionHelper sel, out int cellIndex)
		{
			return InsertContext(m_cache.ServiceLocator.GetInstance<IPhVariableFactory>().Create(), sel, out cellIndex);
		}

		protected override int RemoveItems(SelectionHelper sel, bool forward, out int cellIndex)
		{
			cellIndex = -1;
			var cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
			{
				return -1;
			}
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
					return -1;
				case MoAffixProcessTags.kflidOutput:
					return RemoveFromOutput(forward, sel, out cellIndex) ? cellId : -1;
				default:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					if (ctxtOrVar.ClassID == PhSequenceContextTags.kClassId)
					{
						var seqCtxt = (IPhSequenceContext)ctxtOrVar;
						if (seqCtxt.MembersRS.Count == 0 && forward)
						{
							// remove an empty column
							var prevCellId = GetPrevCell(seqCtxt.Hvo);
							cellIndex = GetCellCount(prevCellId) - 1;
							Rule.InputOS.Remove(seqCtxt);
							return prevCellId;
						}
						var reconstruct = RemoveContextsFrom(forward, sel, seqCtxt, false, out cellIndex);
						// if the column is empty, schedule it to be removed when the selection has changed
						if (seqCtxt.MembersRS.Count == 0)
						{
							m_removeCol = seqCtxt;
						}
						return reconstruct ? seqCtxt.Hvo : -1;
					}
					var idx = GetIndexToRemove(new ICmObject[] { ctxtOrVar }, sel, forward);
					if (idx <= -1 || IsLastVariable(ctxtOrVar))
					{
						return -1;
					}
					{
						var seqCtxt = m_cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
						Rule.InputOS.Insert(ctxtOrVar.IndexInOwner, seqCtxt);
						// if the column is empty, schedule it to be removed when the selection has changed
						m_removeCol = seqCtxt;
						UpdateMappings(ctxtOrVar, seqCtxt);

						ctxtOrVar.PreRemovalSideEffects();
						Rule.InputOS.Remove(ctxtOrVar);
						return seqCtxt.Hvo;
					}
			}
		}

		private bool IsLastVariable(IPhContextOrVar ctxtOrVar)
		{
			if (ctxtOrVar.ClassID != PhVariableTags.kClassId)
			{
				return false;
			}
			return Rule.InputOS.Count(cur => cur.ClassID == PhVariableTags.kClassId) == 1;
		}

		private bool IsLastVariableMapping(IMoRuleMapping mapping)
		{
			if (mapping.ClassID != MoCopyFromInputTags.kClassId)
			{
				return false;
			}
			var copy = (IMoCopyFromInput)mapping;
			return IsLastVariable(copy.ContentRA);
		}

		private bool IsFinalLastVariableMapping(IMoRuleMapping mapping)
		{
			if (!IsLastVariableMapping(mapping))
			{
				return false;
			}
			return Rule.OutputOS.Count(curMapping => IsLastVariableMapping(curMapping)) == 1;
		}

		/// <summary>
		/// Updates the context that the mappings point to. This is used when the context changes
		/// from a single context to a sequence context.
		/// </summary>
		private void UpdateMappings(IPhContextOrVar oldCtxtOrVar, IPhContextOrVar newCtxtOrVar)
		{
			foreach (var mapping in Rule.OutputOS)
			{
				switch (mapping.ClassID)
				{
					case MoCopyFromInputTags.kClassId:
						var copy = (IMoCopyFromInput)mapping;
						if (copy.ContentRA == oldCtxtOrVar)
						{
							copy.ContentRA = newCtxtOrVar;
						}
						break;
					case MoModifyFromInputTags.kClassId:
						var modify = (IMoModifyFromInput)mapping;
						if (modify.ContentRA == oldCtxtOrVar)
						{
							modify.ContentRA = newCtxtOrVar;
						}
						break;
				}
			}
		}

		private bool RemoveFromOutput(bool forward, SelectionHelper sel, out int index)
		{
			index = -1;
			var reconstruct = false;
			var mappings = Rule.OutputOS.Cast<ICmObject>().ToArray();
			if (sel.IsRange)
			{
				var indices = GetIndicesToRemove(mappings, sel);
				if (indices.Length > 0)
				{
					index = indices[0] - 1;
				}
				foreach (var idx in indices)
				{
					var mapping = (IMoRuleMapping)mappings[idx];
					if (IsFinalLastVariableMapping(mapping))
					{
						continue;
					}
					Rule.OutputOS.Remove(mapping);
					reconstruct = true;
				}
			}
			else
			{
				var idx = GetIndexToRemove(mappings, sel, forward);
				if (idx <= -1)
				{
					return false;
				}
				var mapping = (IMoRuleMapping)mappings[idx];
				index = idx - 1;
				if (IsFinalLastVariableMapping(mapping))
				{
					return false;
				}
				Rule.OutputOS.Remove(mapping);
				reconstruct = true;
			}
			return reconstruct;
		}

		private void SelectionChanged(object sender, EventArgs e)
		{
			if (m_removeCol == null)
			{
				return;
			}
			// if there is a column that is scheduled to be removed, go ahead and remove it now
			var sel = SelectionHelper.Create(_view);
			var cellId = GetCell(sel);
			if (m_removeCol.Hvo == cellId)
			{
				return;
			}
			UndoableUnitOfWorkHelper.Do(AreaResources.ksRuleUndoRemove, AreaResources.ksRuleRedoRemove, Rule, () =>
			{
				m_removeCol.PreRemovalSideEffects();
				Rule.InputOS.Remove(m_removeCol);
				m_removeCol = null;
			});
			sel.RestoreSelectionAndScrollPos();
		}

		public void SetMappingFeatures()
		{
			SelectionHelper.Create(_view);
			var reconstruct = false;
			var index = -1;
			UndoableUnitOfWorkHelper.Do(AreaResources.ksAffixRuleUndoSetMappingFeatures, AreaResources.ksAffixRuleRedoSetMappingFeatures, m_cache.ActionHandlerAccessor, () =>
			{
				using (var featChooser = new PhonologicalFeatureChooserDlg())
				{
					var obj = CurrentObject;
					switch (obj.ClassID)
					{
						case MoCopyFromInputTags.kClassId:
							featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher);
							if (featChooser.ShowDialog() == DialogResult.OK)
							{
								// create a new natural class behind the scenes
								var featNC = m_cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
								m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(featNC);
								featNC.Name.SetUserWritingSystem(string.Format(AreaResources.ksRuleNCFeatsName, Rule.Form.BestVernacularAnalysisAlternative.Text));
								featNC.FeaturesOA = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
								featChooser.FS = featNC.FeaturesOA;
								featChooser.UpdateFeatureStructure();

								var copy = (IMoCopyFromInput)obj;
								var newModify = m_cache.ServiceLocator.GetInstance<IMoModifyFromInputFactory>().Create();
								Rule.OutputOS.Insert(copy.IndexInOwner, newModify);
								newModify.ModificationRA = featNC;
								newModify.ContentRA = copy.ContentRA;
								index = newModify.IndexInOwner;

								Rule.OutputOS.Remove(copy);
								reconstruct = true;
							}
							break;
						case MoModifyFromInputTags.kClassId:
							var modify = (IMoModifyFromInput)obj;
							featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher, modify.ModificationRA.FeaturesOA);
							if (featChooser.ShowDialog() == DialogResult.OK)
							{
								if (modify.ModificationRA.FeaturesOA.FeatureSpecsOC.Count == 0)
								{
									var newCopy = m_cache.ServiceLocator.GetInstance<IMoCopyFromInputFactory>().Create();
									Rule.OutputOS.Insert(modify.IndexInOwner, newCopy);
									newCopy.ContentRA = modify.ContentRA;
									index = newCopy.IndexInOwner;

									Rule.OutputOS.Remove(modify);
								}
								else
								{
									index = modify.IndexInOwner;
								}
								reconstruct = true;
							}
							break;
					}
				}
			});

			_view.Select();
			if (reconstruct)
			{
				ReconstructView(MoAffixProcessTags.kflidOutput, index, true);
			}
		}

		public void SetMappingNaturalClass()
		{
			SelectionHelper.Create(_view);

			var natClasses = new HashSet<ICmObject>();
			foreach (var nc in m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS)
			{
				if (nc.ClassID == PhNCFeaturesTags.kClassId)
				{
					natClasses.Add(nc);
				}
			}
			var selectedNc = DisplayChooser(AreaResources.ksRuleNCOpt, AreaResources.ksRuleNCChooserLink, AreaServices.NaturalClassEditMachineName, "RuleNaturalClassFlatList", natClasses) as IPhNCFeatures;
			_view.Select();
			if (selectedNc == null)
			{
				return;
			}
			var index = -1;
			UndoableUnitOfWorkHelper.Do(AreaResources.ksAffixRuleUndoSetNC, AreaResources.ksAffixRuleRedoSetNC, m_cache.ActionHandlerAccessor, () =>
			{
				var curObj = CurrentObject;
				switch (curObj.ClassID)
				{
					case MoCopyFromInputTags.kClassId:
						var copy = (IMoCopyFromInput)curObj;
						var newModify = m_cache.ServiceLocator.GetInstance<IMoModifyFromInputFactory>().Create();
						Rule.OutputOS.Insert(copy.IndexInOwner, newModify);
						newModify.ModificationRA = selectedNc;
						newModify.ContentRA = copy.ContentRA;
						index = newModify.IndexInOwner;
						Rule.OutputOS.Remove(copy);
						break;
					case MoModifyFromInputTags.kClassId:
						var modify = (IMoModifyFromInput)curObj;
						modify.ModificationRA = selectedNc;
						index = modify.IndexInOwner;
						break;
				}
			});
			ReconstructView(MoAffixProcessTags.kflidOutput, index, true);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (_view != null)
			{
				int w = Width;
				_view.Width = w > 0 ? w : 0;
			}
		}
	}
}