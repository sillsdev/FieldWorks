// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
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

		public AffixRuleFormulaControl(XElement configurationNode)
			: base(configurationNode)
		{
		}

		public override bool SliceIsCurrent
		{
			set
			{
				CheckDisposed();
				if (value)
					m_view.Select();
			}
		}

		/// <summary>
		/// Indicates that a rule mapping that points to an input sequence is currently selected.
		/// </summary>
		public bool IsIndexCurrent
		{
			get
			{
				CheckDisposed();

				var obj = CurrentObject;
				if (obj.ClassID == MoCopyFromInputTags.kClassId)
				{
					var copy = (IMoCopyFromInput) obj;
					// we don't want to change a MoCopyFromInput to a MoModifyFromInput if it is pointing to
					// a variable
					return copy.ContentRA.ClassID != PhVariableTags.kClassId;
				}
				return obj.ClassID == MoModifyFromInputTags.kClassId;
			}
		}

		/// <summary>
		/// Indicates that a insert phonemes rule mapping is currently selected.
		/// </summary>
		public bool IsPhonemeCurrent
		{
			get
			{
				CheckDisposed();

				var obj = CurrentObject;
				return obj.ClassID == MoInsertPhonesTags.kClassId;
			}
		}

		/// <summary>
		/// Indicates that a modify from input rule mapping is currently selected.
		/// </summary>
		public bool IsNCIndexCurrent
		{
			get
			{
				CheckDisposed();
				var obj = CurrentObject;
				return obj.ClassID == MoModifyFromInputTags.kClassId;
			}
		}

		IMoAffixProcess Rule
		{
			get
			{
				return m_obj as IMoAffixProcess;
			}
		}

		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider,
			string displayNameProperty, string displayWs)
		{
			CheckDisposed();
			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);

			m_view.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			m_view.Init(obj.Hvo, this, new AffixRuleFormulaVc(cache, PropertyTable), AffixRuleFormulaVc.kfragRule, cache.MainCacheAccessor);

			m_view.SelectionChanged += SelectionChanged;

			m_insertionControl.AddOption(new InsertOption(RuleInsertType.Phoneme), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.NaturalClass), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.Features), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.MorphemeBoundary), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.Variable), DisplayVariableOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.Column), DisplayColumnOption);
			m_insertionControl.AddMultiOption(new InsertOption(RuleInsertType.Index), DisplayOption, DisplayIndices);
			m_insertionControl.NoOptionsMessage = DisplayNoOptsMsg;
		}

		private bool DisplayOption(object option)
		{
			RuleInsertType type = ((InsertOption) option).Type;
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
				return false;

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
						return false;
					return type != RuleInsertType.Index;
			}
		}

		private bool DisplayVariableOption(object option)
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
				return false;

			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
					return true;

				case MoAffixProcessTags.kflidOutput:
					return false;

				default:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(cellId);
					if (ctxtOrVar.ClassID == PhSequenceContextTags.kClassId)
					{
						var seqCtxt = (IPhSequenceContext) ctxtOrVar;
						if (seqCtxt.MembersRS.Count == 0)
							return true;
					}
					return false;
			}
		}

		private bool DisplayColumnOption(object option)
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			if (sel.IsRange)
				return false;

			int cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
				return false;
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
			for (int i = 0; i < indices.Length; i++)
				indices[i] = i + 1;
			return indices.Cast<object>();
		}

		private string DisplayNoOptsMsg()
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			if (cellId == -1 || cellId == 2)
				return null;
			return AreaResources.ksAffixRuleNoOptsMsg;
		}

		protected override string FeatureChooserHelpTopic
		{
			get { return "khtpChoose-LexiconEdit-PhonFeats-AffixRuleFormulaControl"; }
		}

		protected override string RuleName
		{
			get { return Rule.Form.BestVernacularAnalysisAlternative.Text; }
		}

		protected override string ContextMenuID
		{
			get { return "mnuMoAffixProcess"; }
		}

		protected override int GetCell(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (sel == null)
				return -1;

			int tag = sel.GetTextPropId(limit);
			if (tag == AffixRuleFormulaVc.ktagLeftEmpty
				|| tag == AffixRuleFormulaVc.ktagRightEmpty
				|| tag == MoAffixProcessTags.kflidOutput)
				return tag;

			foreach (SelLevInfo level in sel.GetLevelInfo(limit))
			{
				if (level.tag == MoAffixProcessTags.kflidOutput)
					return level.tag;
				if (level.tag == MoAffixProcessTags.kflidInput)
					return level.hvo;
			}

			return -1;
		}

		protected override ICmObject GetCmObject(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (sel == null)
				return null;

			foreach (SelLevInfo level in sel.GetLevelInfo(limit))
			{
				if (level.tag == MoAffixProcessTags.kflidInput
					|| level.tag == PhSequenceContextTags.kflidMembers
					|| level.tag == MoAffixProcessTags.kflidOutput)
					return m_cache.ServiceLocator.GetObject(level.hvo);
			}

			return null;
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
					if (obj.ClassID == PhSequenceContextTags.kClassId)
					{
						var seqCtxt = (IPhSequenceContext) ctxtOrVar;
						return seqCtxt.MembersRS.IndexOf(obj as IPhPhonContext);
					}
					return -1;
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
						return ((IPhSequenceContext) ctxtOrVar).MembersRS.Count;
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
					int index = ctxtOrVar.IndexInOwner;
					if (index == Rule.InputOS.Count - 1)
						return AffixRuleFormulaVc.ktagRightEmpty;
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
					int index = ctxtOrVar.IndexInOwner;
					if (index == 0)
						return AffixRuleFormulaVc.ktagLeftEmpty;
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
			int cellId = GetCell(sel);
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
			int cellId = GetCell(sel);
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

		int InsertIntoOutput(IMoRuleMapping mapping, SelectionHelper sel)
		{
			ICmObject[] mappings = Rule.OutputOS.Cast<ICmObject>().ToArray();
			int index = GetInsertionIndex(mappings, sel);
			Rule.OutputOS.Insert(index, mapping);
			if (sel.IsRange)
			{
				IEnumerable<int> indices = GetIndicesToRemove(mappings, sel);
				foreach (int idx in indices)
					Rule.OutputOS.Remove((IMoRuleMapping) mappings[idx]);
			}
			return index;
		}

		int InsertContext(IPhContextOrVar ctxtOrVar, SelectionHelper sel, out int cellIndex)
		{
			m_removeCol = null;
			int cellId = GetCell(sel);
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
						int index = cellCtxtOrVar.IndexInOwner;
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
				int index = ctxt.IndexInOwner;
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
			int index = GetColumnInsertIndex(sel);
			var seqCtxt = m_cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
			Rule.InputOS.Insert(index, seqCtxt);
			return seqCtxt.Hvo;
		}

		int GetColumnInsertIndex(SelectionHelper sel)
		{
			int hvo = GetCell(sel);
			if (hvo <= 0)
				return -1;

			ICmObject[] ctxtOrVars;
			var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(hvo);
			if (ctxtOrVar.ClassID != PhSequenceContextTags.kClassId)
			{
				ctxtOrVars = new ICmObject[] { ctxtOrVar };
			}
			else
			{
				var seqCtxt = (IPhSequenceContext) ctxtOrVar;
				ctxtOrVars = seqCtxt.MembersRS.Cast<ICmObject>().ToArray();
			}
			if (ctxtOrVars.Length == 0)
				return -1;

			int insertIndex = GetInsertionIndex(ctxtOrVars, sel);
			if (insertIndex == 0 && ctxtOrVar.IndexInOwner != 0)
			{
				var prev = Rule.InputOS[ctxtOrVar.IndexInOwner - 1];
				if (GetCellCount(prev.Hvo) > 0)
					return ctxtOrVar.IndexInOwner;
			}
			else if (insertIndex == ctxtOrVars.Length && ctxtOrVar.IndexInOwner != Rule.InputOS.Count - 1)
			{
				var next = Rule.InputOS[ctxtOrVar.IndexInOwner + 1];
				if (GetCellCount(next.Hvo) > 0)
					return next.IndexInOwner;
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

			int cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
				return -1;

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
						var seqCtxt = (IPhSequenceContext) ctxtOrVar;
						if (seqCtxt.MembersRS.Count == 0 && forward)
						{
							// remove an empty column
							int prevCellId = GetPrevCell(seqCtxt.Hvo);
							cellIndex = GetCellCount(prevCellId) - 1;
							Rule.InputOS.Remove(seqCtxt);
							return prevCellId;
						}
						bool reconstruct = RemoveContextsFrom(forward, sel, seqCtxt, false, out cellIndex);
						// if the column is empty, schedule it to be removed when the selection has changed
						if (seqCtxt.MembersRS.Count == 0)
							m_removeCol = seqCtxt;
						return reconstruct ? seqCtxt.Hvo : -1;
					}
					int idx = GetIndexToRemove(new ICmObject[] { ctxtOrVar }, sel, forward);
					if (idx > -1 && !IsLastVariable(ctxtOrVar))
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
					return -1;
			}
		}

		bool IsLastVariable(IPhContextOrVar ctxtOrVar)
		{
			if (ctxtOrVar.ClassID != PhVariableTags.kClassId)
				return false;

			int numVars = 0;
			foreach (var cur in Rule.InputOS)
			{
				if (cur.ClassID == PhVariableTags.kClassId)
					numVars++;
			}
			return numVars == 1;
		}

		bool IsLastVariableMapping(IMoRuleMapping mapping)
		{
			if (mapping.ClassID == MoCopyFromInputTags.kClassId)
			{
				var copy = (IMoCopyFromInput) mapping;
				if (IsLastVariable(copy.ContentRA))
					return true;
			}
			return false;
		}

		bool IsFinalLastVariableMapping(IMoRuleMapping mapping)
		{
			if (IsLastVariableMapping(mapping))
			{
				int numLastVarMappings = 0;
				foreach (var curMapping in Rule.OutputOS)
				{
					if (IsLastVariableMapping(curMapping))
						numLastVarMappings++;
				}
				return numLastVarMappings == 1;
			}
			return false;
		}

		/// <summary>
		/// Updates the context that the mappings point to. This is used when the context changes
		/// from a single context to a sequence context.
		/// </summary>
		/// <param name="oldCtxtOrVar"></param>
		/// <param name="newCtxtOrVar"></param>
		private void UpdateMappings(IPhContextOrVar oldCtxtOrVar, IPhContextOrVar newCtxtOrVar)
		{
			foreach (var mapping in Rule.OutputOS)
			{
				switch (mapping.ClassID)
				{
					case MoCopyFromInputTags.kClassId:
						var copy = (IMoCopyFromInput) mapping;
						if (copy.ContentRA == oldCtxtOrVar)
							copy.ContentRA = newCtxtOrVar;
						break;

					case MoModifyFromInputTags.kClassId:
						var modify = (IMoModifyFromInput) mapping;
						if (modify.ContentRA == oldCtxtOrVar)
							modify.ContentRA = newCtxtOrVar;
						break;
				}
			}
		}

		private bool RemoveFromOutput(bool forward, SelectionHelper sel, out int index)
		{
			index = -1;
			bool reconstruct = false;
			ICmObject[] mappings = Rule.OutputOS.Cast<ICmObject>().ToArray();
			if (sel.IsRange)
			{
				int[] indices = GetIndicesToRemove(mappings, sel);
				if (indices.Length > 0)
					index = indices[0] - 1;


				foreach (int idx in indices)
				{
					var mapping = (IMoRuleMapping) mappings[idx];
					if (!IsFinalLastVariableMapping(mapping))
					{
						Rule.OutputOS.Remove(mapping);
						reconstruct = true;
					}
				}
			}
			else
			{
				int idx = GetIndexToRemove(mappings, sel, forward);
				if (idx > -1)
				{
					var mapping = (IMoRuleMapping) mappings[idx];
					index = idx - 1;
					if (!IsFinalLastVariableMapping(mapping))
					{
						Rule.OutputOS.Remove(mapping);
						reconstruct = true;
					}
				}
			}
			return reconstruct;
		}

		private void SelectionChanged(object sender, EventArgs e)
		{
			if (m_removeCol != null)
			{
				// if there is a column that is scheduled to be removed, go ahead and remove it now
				SelectionHelper sel = SelectionHelper.Create(m_view);
				int cellId = GetCell(sel);
				if (m_removeCol.Hvo != cellId)
				{
					UndoableUnitOfWorkHelper.Do(AreaResources.ksRuleUndoRemove, AreaResources.ksRuleRedoRemove, Rule, () =>
					{
						m_removeCol.PreRemovalSideEffects();
						Rule.InputOS.Remove(m_removeCol);
						m_removeCol = null;
					});
					sel.RestoreSelectionAndScrollPos();
				}
			}
		}

		public void SetMappingFeatures()
		{
			SelectionHelper.Create(m_view);
			bool reconstruct = false;
			int index = -1;
			UndoableUnitOfWorkHelper.Do(AreaResources.ksAffixRuleUndoSetMappingFeatures,
				AreaResources.ksAffixRuleRedoSetMappingFeatures, m_cache.ActionHandlerAccessor, () =>
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
								featNC.Name.SetUserWritingSystem(string.Format(AreaResources.ksRuleNCFeatsName,
									Rule.Form.BestVernacularAnalysisAlternative.Text));
								featNC.FeaturesOA = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
								featChooser.FS = featNC.FeaturesOA;
								featChooser.UpdateFeatureStructure();

								var copy = (IMoCopyFromInput) obj;
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
							var modify = (IMoModifyFromInput) obj;
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

			m_view.Select();
			if (reconstruct)
				ReconstructView(MoAffixProcessTags.kflidOutput, index, true);
		}

		public void SetMappingNaturalClass()
		{
			SelectionHelper.Create(m_view);

			var natClasses = new HashSet<ICmObject>();
			foreach (var nc in m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS)
			{
				if (nc.ClassID == PhNCFeaturesTags.kClassId)
					natClasses.Add(nc);
			}
			var selectedNc = DisplayChooser(AreaResources.ksRuleNCOpt, AreaResources.ksRuleNCChooserLink,
				AreaServices.NaturalClassEditMachineName, "RuleNaturalClassFlatList", natClasses) as IPhNCFeatures;
			m_view.Select();
			if (selectedNc != null)
			{
				int index = -1;
				UndoableUnitOfWorkHelper.Do(AreaResources.ksAffixRuleUndoSetNC,
					AreaResources.ksAffixRuleRedoSetNC, m_cache.ActionHandlerAccessor, () =>
				{
					var curObj = CurrentObject;
					switch (curObj.ClassID)
					{
						case MoCopyFromInputTags.kClassId:
							var copy = (IMoCopyFromInput) curObj;
							var newModify = m_cache.ServiceLocator.GetInstance<IMoModifyFromInputFactory>().Create();
							Rule.OutputOS.Insert(copy.IndexInOwner, newModify);
							newModify.ModificationRA = selectedNc;
							newModify.ContentRA = copy.ContentRA;
							index = newModify.IndexInOwner;

							Rule.OutputOS.Remove(copy);
							break;

						case MoModifyFromInputTags.kClassId:
							var modify = (IMoModifyFromInput) curObj;
							modify.ModificationRA = selectedNc;
							index = modify.IndexInOwner;
							break;
					}
				});

				ReconstructView(MoAffixProcessTags.kflidOutput, index, true);
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (m_view != null)
			{
				int w = Width;
				m_view.Width = w > 0 ? w : 0;
			}
		}

	}
}
