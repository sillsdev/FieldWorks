using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class represents an affix process rule formula control. An affix process
	/// is represented by a left input empty cell, a right input empty cell, a cell for
	/// each input phonological context, and a result cell. The input emtpy cells are simply
	/// used to insert new contexts in to a <c>MoAffixProcess</c>. The context cells represent
	/// the contexts in the <c>Input</c> field of <c>MoAffixProcess</c>. The data in the result
	/// cell consists of the rule mapping objects in the <c>Output</c> field.
	/// </summary>
	public class AffixRuleFormulaControl : RuleFormulaControl
	{
		// column HVO that is scheduled to be removed
		int m_removeColHvo = 0;

		public AffixRuleFormulaControl(XmlNode configurationNode)
			: base(configurationNode)
		{
			m_menuId = "mnuMoAffixProcess";
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

				int hvo = CurrentHvo;
				int classId = m_cache.GetClassOfObject(hvo);
				if (classId == MoCopyFromInput.kclsidMoCopyFromInput)
				{
					IMoCopyFromInput copy = new MoCopyFromInput(m_cache, hvo);
					// we don't want to change a MoCopyFromInput to a MoModifyFromInput if it is pointing to
					// a variable
					return copy.ContentRA.ClassID != PhVariable.kclsidPhVariable;
				}
				else
				{
					return classId == MoModifyFromInput.kclsidMoModifyFromInput;
				}
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

				int hvo = CurrentHvo;
				return m_cache.GetClassOfObject(hvo) == MoInsertPhones.kclsidMoInsertPhones;
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
				int hvo = CurrentHvo;
				return m_cache.GetClassOfObject(hvo) == MoModifyFromInput.kclsidMoModifyFromInput;
			}
		}

		IMoAffixProcess Rule
		{
			get
			{
				return m_obj as IMoAffixProcess;
			}
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider,
			XCore.Mediator mediator, string displayNameProperty, string displayWs)
		{
			CheckDisposed();
			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);

			m_view.Init(mediator, obj, this, new AffixRuleFormulaVc(cache, mediator), AffixRuleFormulaVc.kfragRule);

			m_insertionControl.Initialize(cache, mediator, persistProvider, Rule.Form.BestVernacularAnalysisAlternative.Text);
			m_insertionControl.AddOption(RuleInsertType.PHONEME, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.NATURAL_CLASS, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.FEATURES, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.MORPHEME_BOUNDARY, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.VARIABLE, DisplayVariableOption);
			m_insertionControl.AddOption(RuleInsertType.COLUMN, DisplayColumnOption);
			m_insertionControl.AddIndexOption(DisplayOption, DisplayIndices);
			m_insertionControl.NoOptionsMessage = DisplayNoOptsMsg;
		}

		bool DisplayOption(RuleInsertType type)
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
				return false;

			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
					return type != RuleInsertType.INDEX;

				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					return type == RuleInsertType.INDEX || type == RuleInsertType.PHONEME || type == RuleInsertType.MORPHEME_BOUNDARY;

				default:
					if (m_cache.GetClassOfObject(cellId) == PhVariable.kclsidPhVariable)
						return false;
					return type != RuleInsertType.INDEX;
			}
		}

		bool DisplayVariableOption(RuleInsertType type)
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

				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					return false;

				default:
					if (m_cache.GetClassOfObject(cellId) == PhSequenceContext.kclsidPhSequenceContext)
					{
						int size = m_cache.GetVectorSize(cellId, (int)PhSequenceContext.PhSequenceContextTags.kflidMembers);
						if (size == 0)
							return true;
					}
					return false;
			}
		}

		bool DisplayColumnOption(RuleInsertType type)
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
				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					return false;

				default:
					return GetColumnInsertIndex(sel) != -1;
			}
		}

		int[] DisplayIndices()
		{
			int[] indices = new int[Rule.InputOS.Count];
			for (int i = 0; i < indices.Length; i++)
				indices[i] = i + 1;
			return indices;
		}

		string DisplayNoOptsMsg()
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			if (cellId == -1 || cellId == 2)
				return null;
			return MEStrings.ksAffixRuleNoOptsMsg;
		}

		protected override int GetCell(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (sel == null)
				return -1;

			int tag = sel.GetTextPropId(limit);
			if (tag == AffixRuleFormulaVc.ktagLeftEmpty
				|| tag == AffixRuleFormulaVc.ktagRightEmpty
				|| tag == (int)MoAffixProcess.MoAffixProcessTags.kflidOutput)
				return tag;

			foreach (SelLevInfo level in sel.GetLevelInfo(limit))
			{
				if (level.tag == (int)MoAffixProcess.MoAffixProcessTags.kflidOutput)
					return level.tag;
				else if (level.tag == (int)MoAffixProcess.MoAffixProcessTags.kflidInput)
					return level.hvo;
			}

			return -1;
		}

		protected override int GetItemHvo(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (sel == null)
				return 0;

			foreach (SelLevInfo level in sel.GetLevelInfo(limit))
			{
				if (level.tag == (int)MoAffixProcess.MoAffixProcessTags.kflidInput
					|| level.tag == (int)PhSequenceContext.PhSequenceContextTags.kflidMembers
					|| level.tag == (int)MoAffixProcess.MoAffixProcessTags.kflidOutput)
					return level.hvo;
			}

			return 0;
		}

		protected override int GetItemCellIndex(int cellId, int hvo)
		{
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
					return -1;

				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					return m_cache.GetObjIndex(Rule.Hvo, cellId, hvo);

				default:
					if (m_cache.GetClassOfObject(cellId) == PhSequenceContext.kclsidPhSequenceContext)
					{
						int[] hvos = m_cache.GetVectorProperty(cellId, (int)PhSequenceContext.PhSequenceContextTags.kflidMembers, false);
						for (int i = 0; i < hvos.Length; i++)
						{
							if (hvos[i] == hvo)
								return i;
						}
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

				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					if (cellIndex >= 0)
					{
						levels = new SelLevInfo[1];
						levels[0].tag = cellId;
						levels[0].ihvo = cellIndex;
					}
					break;

				default:
					if (cellIndex < 0 || m_cache.GetClassOfObject(cellId) != PhSequenceContext.kclsidPhSequenceContext)
					{

						levels = new SelLevInfo[1];
						levels[0].tag = (int)MoAffixProcess.MoAffixProcessTags.kflidInput;
						levels[0].ihvo = m_cache.GetObjIndex(Rule.Hvo, (int)MoAffixProcess.MoAffixProcessTags.kflidInput, cellId);
					}
					else
					{
						levels = new SelLevInfo[2];
						levels[0].tag = (int)PhSequenceContext.PhSequenceContextTags.kflidMembers;
						levels[0].ihvo = cellIndex;
						levels[1].tag = (int)MoAffixProcess.MoAffixProcessTags.kflidInput;
						levels[1].ihvo = m_cache.GetObjIndex(Rule.Hvo, (int)MoAffixProcess.MoAffixProcessTags.kflidInput, cellId);
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

				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					return Rule.OutputOS.Count;

				default:
					if (m_cache.GetClassOfObject(cellId) == PhSequenceContext.kclsidPhSequenceContext)
						return m_cache.GetVectorSize(cellId, (int)PhSequenceContext.PhSequenceContextTags.kflidMembers);
					else
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
					return (int)MoAffixProcess.MoAffixProcessTags.kflidOutput;

				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					return -1;

				default:
					int index = m_cache.GetObjIndex(Rule.Hvo, (int)MoAffixProcess.MoAffixProcessTags.kflidInput, cellId);
					if (index == Rule.InputOS.Count - 1)
						return AffixRuleFormulaVc.ktagRightEmpty;
					else
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

				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					return AffixRuleFormulaVc.ktagRightEmpty;

				default:
					int index = m_cache.GetObjIndex(Rule.Hvo, (int)MoAffixProcess.MoAffixProcessTags.kflidInput, cellId);
					if (index == 0)
						return AffixRuleFormulaVc.ktagLeftEmpty;
					else
						return Rule.InputOS[index - 1].Hvo;
			}
		}

		protected override int GetFlid(int cellId)
		{
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
				case AffixRuleFormulaVc.ktagRightEmpty:
				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					return cellId;

				default:
					return (int)MoAffixProcess.MoAffixProcessTags.kflidOutput;
			}
		}

		protected override int InsertPhoneme(int hvo, SelectionHelper sel, out int cellIndex)
		{
			int cellId = GetCell(sel);
			if (cellId == (int)MoAffixProcess.MoAffixProcessTags.kflidOutput)
			{
				IMoInsertPhones insertPhones = new MoInsertPhones();
				cellIndex = InsertIntoOutput(insertPhones, sel);
				insertPhones.ContentRS.Append(hvo);
				insertPhones.NotifyNew();
			}
			else
			{
				cellId = InsertContext(new PhSimpleContextSeg(), (int)PhSimpleContextSeg.PhSimpleContextSegTags.kflidFeatureStructure, hvo,
					sel, out cellIndex);
			}
			return cellId;
		}

		protected override int InsertBdry(int hvo, SelectionHelper sel, out int cellIndex)
		{
			int cellId = GetCell(sel);
			if (cellId == (int)MoAffixProcess.MoAffixProcessTags.kflidOutput)
			{
				IMoInsertPhones insertPhones = new MoInsertPhones();
				cellIndex = InsertIntoOutput(insertPhones, sel);
				insertPhones.ContentRS.Append(hvo);
				insertPhones.NotifyNew();
			}
			else
			{
				cellId = InsertContext(new PhSimpleContextBdry(), (int)PhSimpleContextBdry.PhSimpleContextBdryTags.kflidFeatureStructure, hvo,
					sel, out cellIndex);
			}
			return cellId;
		}

		protected override int InsertNC(int hvo, SelectionHelper sel, out int cellIndex)
		{
			return InsertContext(new PhSimpleContextNC(), (int)PhSimpleContextNC.PhSimpleContextNCTags.kflidFeatureStructure, hvo,
				sel, out cellIndex);
		}

		int InsertIntoOutput(IMoRuleMapping mapping, SelectionHelper sel)
		{
			int[] hvos = Rule.OutputOS.HvoArray;
			int index = GetInsertionIndex(hvos, sel);
			Rule.OutputOS.InsertAt(mapping, index);
			if (sel.IsRange)
			{
				IEnumerable<int> indices = GetIndicesToRemove(hvos, sel);
				foreach (int idx in indices)
				{
					IMoRuleMapping removeMapping = MoRuleMapping.CreateFromDBObject(m_cache, hvos[idx]);
					removeMapping.DeleteUnderlyingObject();
				}
			}
			return index;
		}

		int InsertContext(IPhContextOrVar ctxtOrVar, int fsFlid, int fsHvo, SelectionHelper sel, out int cellIndex)
		{
			m_removeColHvo = 0;
			int cellId = GetCell(sel);
			switch (cellId)
			{
				case AffixRuleFormulaVc.ktagLeftEmpty:
					Rule.InputOS.InsertAt(ctxtOrVar, 0);
					if (fsFlid != -1 && fsHvo != 0)
						m_cache.SetObjProperty(ctxtOrVar.Hvo, fsFlid, fsHvo);
					ctxtOrVar.NotifyNew();
					cellIndex = -1;
					return ctxtOrVar.Hvo;

				case AffixRuleFormulaVc.ktagRightEmpty:
					Rule.InputOS.Append(ctxtOrVar);
					if (fsFlid != -1 && fsHvo != 0)
						m_cache.SetObjProperty(ctxtOrVar.Hvo, fsFlid, fsHvo);
					ctxtOrVar.NotifyNew();
					cellIndex = -1;
					return ctxtOrVar.Hvo;

				default:
					if (ctxtOrVar.ClassID == PhVariable.kclsidPhVariable)
					{
						int index = m_cache.GetObjIndex(Rule.Hvo, (int)MoAffixProcess.MoAffixProcessTags.kflidInput, cellId);
						Rule.InputOS.InsertAt(ctxtOrVar, index);
						ctxtOrVar.NotifyNew();
						UpdateMappings(cellId, ctxtOrVar.Hvo);
						Rule.InputOS.Remove(cellId);
						cellIndex = -1;
						return ctxtOrVar.Hvo;
					}
					else
					{
						IPhPhonContext cellCtxt = PhPhonContext.CreateFromDBObject(m_cache, cellId);
						IPhSequenceContext seqCtxt = CreateSeqCtxt(cellCtxt);
						cellIndex = InsertContextInto(ctxtOrVar as IPhSimpleContext, fsFlid, fsHvo, sel, seqCtxt);
						return seqCtxt.Hvo;
					}
			}
		}

		IPhSequenceContext CreateSeqCtxt(IPhPhonContext ctxt)
		{
			IPhSequenceContext seqCtxt = null;
			if (ctxt.ClassID != PhSequenceContext.kclsidPhSequenceContext)
			{
				int index = ctxt.IndexInOwner;
				m_cache.LangProject.PhonologicalDataOA.ContextsOS.Append(ctxt);
				seqCtxt = new PhSequenceContext();
				Rule.InputOS.InsertAt(seqCtxt, index);
				seqCtxt.MembersRS.Append(ctxt);
				seqCtxt.NotifyNew();
				UpdateMappings(ctxt.Hvo, seqCtxt.Hvo);
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
			IPhSequenceContext seqCtxt = new PhSequenceContext();
			Rule.InputOS.InsertAt(seqCtxt, index);
			seqCtxt.NotifyNew();
			return seqCtxt.Hvo;
		}

		int GetColumnInsertIndex(SelectionHelper sel)
		{
			int hvo = GetCell(sel);
			if (hvo <= 0)
				return -1;

			int[] hvos = null;
			IPhContextOrVar ctxtOrVar = PhContextOrVar.CreateFromDBObject(m_cache, hvo);
			if (ctxtOrVar.ClassID != PhSequenceContext.kclsidPhSequenceContext)
			{
				hvos = new int[] { ctxtOrVar.Hvo };
			}
			else
			{
				IPhSequenceContext seqCtxt = ctxtOrVar as IPhSequenceContext;
				hvos = seqCtxt.MembersRS.HvoArray;
			}
			if (hvos.Length == 0)
				return -1;

			int insertIndex = GetInsertionIndex(hvos, sel);
			if (insertIndex == 0 && ctxtOrVar.IndexInOwner != 0)
			{
				IPhContextOrVar prev = Rule.InputOS[ctxtOrVar.IndexInOwner - 1];
				if (GetCellCount(prev.Hvo) > 0)
					return ctxtOrVar.IndexInOwner;
			}
			else if (insertIndex == hvos.Length && ctxtOrVar.IndexInOwner != Rule.InputOS.Count - 1)
			{
				IPhContextOrVar next = Rule.InputOS[ctxtOrVar.IndexInOwner + 1];
				if (GetCellCount(next.Hvo) > 0)
					return next.IndexInOwner;
			}

			return -1;
		}

		protected override int InsertIndex(int index, SelectionHelper sel, out int cellIndex)
		{
			IMoCopyFromInput copy = new MoCopyFromInput();
			cellIndex = InsertIntoOutput(copy, sel);
			copy.ContentRAHvo = Rule.InputOS[index - 1].Hvo;
			copy.NotifyNew();
			return (int)MoAffixProcess.MoAffixProcessTags.kflidOutput;
		}

		protected override int InsertVariable(SelectionHelper sel, out int cellIndex)
		{
			return InsertContext(new PhVariable(), -1, 0, sel, out cellIndex);
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

				case (int)MoAffixProcess.MoAffixProcessTags.kflidOutput:
					return RemoveFromOutput(forward, sel, out cellIndex) ? cellId : -1;

				default:
					IPhContextOrVar ctxtOrVar = PhContextOrVar.CreateFromDBObject(m_cache, cellId);
					if (ctxtOrVar.ClassID == PhSequenceContext.kclsidPhSequenceContext)
					{
						IPhSequenceContext seqCtxt = ctxtOrVar as IPhSequenceContext;
						if (seqCtxt.MembersRS.Count == 0 && forward)
						{
							// remove an empty column
							int prevCellId = GetPrevCell(seqCtxt.Hvo);
							cellIndex = GetCellCount(prevCellId) - 1;
							Rule.InputOS.Remove(seqCtxt);
							return prevCellId;
						}
						else
						{
							bool reconstruct = RemoveContextsFrom(forward, sel, seqCtxt, false, out cellIndex);
							// if the column is empty, schedule it to be removed when the selection has changed
							if (seqCtxt.MembersRS.Count == 0)
								m_removeColHvo = seqCtxt.Hvo;
							return reconstruct ? seqCtxt.Hvo : -1;
						}
					}
					else
					{
						int idx = GetIndexToRemove(new int[] { ctxtOrVar.Hvo }, sel, forward);
						if (idx > -1 && !IsLastVariable(ctxtOrVar))
						{
							IPhSequenceContext seqCtxt = new PhSequenceContext();
							Rule.InputOS.InsertAt(seqCtxt, ctxtOrVar.IndexInOwner);
							seqCtxt.NotifyNew();
							// if the column is empty, schedule it to be removed when the selection has changed
							m_removeColHvo = seqCtxt.Hvo;
							UpdateMappings(ctxtOrVar.Hvo, seqCtxt.Hvo);
							ctxtOrVar.DeleteUnderlyingObject();
							return seqCtxt.Hvo;
						}
						else
						{
							return -1;
						}
					}
			}
		}

		bool IsLastVariable(IPhContextOrVar ctxtOrVar)
		{
			if (ctxtOrVar.ClassID != PhVariable.kclsidPhVariable)
				return false;

			int numVars = 0;
			foreach (IPhContextOrVar cur in Rule.InputOS)
			{
				if (cur.ClassID == PhVariable.kclsidPhVariable)
					numVars++;
			}
			return numVars == 1;
		}

		bool IsLastVariableMapping(int hvo)
		{
			if (m_cache.GetClassOfObject(hvo) == MoCopyFromInput.kclsidMoCopyFromInput)
			{
				IMoCopyFromInput copy = new MoCopyFromInput(m_cache, hvo);
				if (IsLastVariable(copy.ContentRA))
					return true;
			}
			return false;
		}

		bool IsFinalLastVariableMapping(int hvo)
		{
			if (IsLastVariableMapping(hvo))
			{
				int numLastVarMappings = 0;
				foreach (IMoRuleMapping mapping in Rule.OutputOS)
				{
					if (IsLastVariableMapping(mapping.Hvo))
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
		/// <param name="oldHvo">The old hvo.</param>
		/// <param name="newHvo">The new hvo.</param>
		void UpdateMappings(int oldHvo, int newHvo)
		{
			foreach (int mappingHvo in Rule.OutputOS.HvoArray)
			{
				switch (m_cache.GetClassOfObject(mappingHvo))
				{
					case MoCopyFromInput.kclsidMoCopyFromInput:
						IMoCopyFromInput copy = new MoCopyFromInput(m_cache, mappingHvo);
						if (copy.ContentRAHvo == oldHvo)
							copy.ContentRAHvo = newHvo;
						break;

					case MoModifyFromInput.kclsidMoModifyFromInput:
						IMoModifyFromInput modify = new MoModifyFromInput(m_cache, mappingHvo);
						if (modify.ContentRAHvo == oldHvo)
							modify.ContentRAHvo = newHvo;
						break;
				}
			}
		}

		protected bool RemoveFromOutput(bool forward, SelectionHelper sel, out int index)
		{
			index = -1;
			bool reconstruct = false;
			int[] hvos = Rule.OutputOS.HvoArray;
			if (sel.IsRange)
			{
				int[] indices = GetIndicesToRemove(hvos, sel);
				if (indices.Length > 0)
					index = indices[0] - 1;


				foreach (int idx in indices)
				{
					if (!IsFinalLastVariableMapping(hvos[idx]))
					{
						IMoRuleMapping mapping = MoRuleMapping.CreateFromDBObject(m_cache, hvos[idx]);
						mapping.DeleteUnderlyingObject();
						reconstruct = true;
					}
				}
			}
			else
			{
				int idx = GetIndexToRemove(hvos, sel, forward);
				if (idx > -1)
				{
					index = idx - 1;
					if (!IsFinalLastVariableMapping(hvos[idx]))
					{
						IMoRuleMapping mapping = MoRuleMapping.CreateFromDBObject(m_cache, hvos[idx]);
						mapping.DeleteUnderlyingObject();
						reconstruct = true;
					}
				}
			}
			return reconstruct;
		}

		public override void UpdateSelection(IVwRootBox prootb, IVwSelection vwselNew)
		{
			if (m_removeColHvo != 0)
			{
				// if there is a column that is scheduled to be removed, go ahead and remove it now
				SelectionHelper sel = SelectionHelper.Create(vwselNew, m_view);
				int cellId = GetCell(sel);
				if (m_removeColHvo != cellId)
				{
					using (new UndoRedoTaskHelper(m_cache, MEStrings.ksRuleUndoRemove, MEStrings.ksRuleRedoRemove))
					{
						IPhContextOrVar ctxtOrVar = PhContextOrVar.CreateFromDBObject(m_cache, m_removeColHvo);
						ctxtOrVar.DeleteUnderlyingObject();
						m_removeColHvo = 0;
					}
					m_view.RootBox.Reconstruct();
					sel.RestoreSelectionAndScrollPos();
					return;
				}
			}

			base.UpdateSelection(prootb, vwselNew);
		}

		public void SetMappingFeatures()
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			bool reconstruct = false;
			int index = -1;
			using (new UndoRedoTaskHelper(m_cache, MEStrings.ksAffixRuleUndoSetMappingFeatures, MEStrings.ksAffixRuleRedoSetMappingFeatures))
			{
				using (PhonologicalFeatureChooserDlg featChooser = new PhonologicalFeatureChooserDlg())
				{
					int hvo = CurrentHvo;
					switch (m_cache.GetClassOfObject(hvo))
					{
						case MoCopyFromInput.kclsidMoCopyFromInput:
							featChooser.SetDlgInfo(m_cache, m_mediator);
							if (featChooser.ShowDialog() == DialogResult.OK)
							{
								// create a new natural class behind the scenes
								PhNCFeatures featNC = new PhNCFeatures();
								m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Append(featNC);
								featNC.Name.UserDefaultWritingSystem = string.Format(MEStrings.ksRuleNCFeatsName,
									Rule.Form.BestVernacularAnalysisAlternative.Text);
								featNC.FeaturesOA = new FsFeatStruc();
								featChooser.FS = featNC.FeaturesOA;
								featChooser.UpdateFeatureStructure();
								featNC.FeaturesOA.NotifyNew();
								featNC.NotifyNew();

								IMoCopyFromInput copy = new MoCopyFromInput(m_cache, hvo);
								IMoModifyFromInput newModify = new MoModifyFromInput();
								Rule.OutputOS.InsertAt(newModify, copy.IndexInOwner);
								newModify.ModificationRAHvo = featNC.Hvo;
								newModify.ContentRAHvo = copy.ContentRAHvo;
								index = newModify.IndexInOwner;
								newModify.NotifyNew();

								copy.DeleteUnderlyingObject();

								reconstruct = true;
							}
							break;

						case MoModifyFromInput.kclsidMoModifyFromInput:
							IMoModifyFromInput modify = new MoModifyFromInput(m_cache, hvo);
							featChooser.SetDlgInfo(m_cache, m_mediator, modify.ModificationRA.FeaturesOA);
							if (featChooser.ShowDialog() == DialogResult.OK)
							{
								if (modify.ModificationRA.FeaturesOA.FeatureSpecsOC.Count == 0)
								{
									IMoCopyFromInput newCopy = new MoCopyFromInput();
									Rule.OutputOS.InsertAt(newCopy, modify.IndexInOwner);
									newCopy.ContentRAHvo = modify.ContentRAHvo;
									index = newCopy.IndexInOwner;
									newCopy.NotifyNew();

									modify.DeleteUnderlyingObject();
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
			}

			m_view.Select();
			if (reconstruct)
				ReconstructView((int)MoAffixProcess.MoAffixProcessTags.kflidOutput, index, true);
		}

		public void SetMappingNaturalClass()
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);

			Set<int> natClasses = new Set<int>();
			foreach (IPhNaturalClass nc in m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS)
			{
				if (nc.ClassID == PhNCFeatures.kclsidPhNCFeatures)
					natClasses.Add(nc.Hvo);
			}
			int ncHvo = m_insertionControl.DisplayChooser(MEStrings.ksRuleNCOpt, MEStrings.ksRuleNCChooserLink,
				"naturalClassedit", "RuleNaturalClassFlatList", natClasses);
			m_view.Select();
			if (ncHvo != 0)
			{
				int index = -1;
				using (new UndoRedoTaskHelper(m_cache, MEStrings.ksAffixRuleUndoSetNC, MEStrings.ksAffixRuleRedoSetNC))
				{
					int curHvo = CurrentHvo;
					switch (m_cache.GetClassOfObject(curHvo))
					{
						case MoCopyFromInput.kclsidMoCopyFromInput:
							IMoCopyFromInput copy = new MoCopyFromInput(m_cache, curHvo);
							IMoModifyFromInput newModify = new MoModifyFromInput();
							Rule.OutputOS.InsertAt(newModify, copy.IndexInOwner);
							newModify.ModificationRAHvo = ncHvo;
							newModify.ContentRAHvo = copy.ContentRAHvo;
							index = newModify.IndexInOwner;
							newModify.NotifyNew();

							copy.DeleteUnderlyingObject();
							break;

						case MoModifyFromInput.kclsidMoModifyFromInput:
							IMoModifyFromInput modify = new MoModifyFromInput(m_cache, curHvo);
							modify.ModificationRAHvo = ncHvo;
							index = modify.IndexInOwner;
							break;
					}
				}

				ReconstructView((int)MoAffixProcess.MoAffixProcessTags.kflidOutput, index, true);
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

	class AffixRuleFormulaVc : RuleFormulaVc
	{
		public const int kfragRule = 100;
		public const int kfragInput = 101;
		public const int kfragRuleMapping = 102;

		public const int kfragSpace = 103;

		public const int ktagLeftEmpty = -200;
		public const int ktagRightEmpty = -201;
		public const int ktagIndex = -202;

		IMoAffixProcess m_rule = null;

		ITsTextProps m_headerProps;
		ITsTextProps m_arrowProps;
		ITsTextProps m_ctxtProps;
		ITsTextProps m_indexProps;
		ITsTextProps m_resultProps;

		ITsString m_inputStr;
		ITsString m_indexStr;
		ITsString m_resultStr;
		ITsString m_doubleArrow;
		ITsString m_space;

		public AffixRuleFormulaVc(FdoCache cache, XCore.Mediator mediator)
			: base(cache, mediator)
		{
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Arial");
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_headerProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 24000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Charis SIL");
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_arrowProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			m_ctxtProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			m_indexProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			m_resultProps = tpb.GetTextProps();

			m_inputStr = m_cache.MakeUserTss(MEStrings.ksAffixRuleInput);
			m_indexStr = m_cache.MakeUserTss(MEStrings.ksAffixRuleIndex);
			m_resultStr = m_cache.MakeUserTss(MEStrings.ksAffixRuleResult);
			m_doubleArrow = m_cache.MakeUserTss("\u21d2");
			m_space = m_cache.MakeUserTss(" ");
		}

		#region IDisposable override
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_rule = null;

			if (m_headerProps != null)
			{
				Marshal.ReleaseComObject(m_headerProps);
				m_headerProps = null;
			}
			if (m_arrowProps != null)
			{
				Marshal.ReleaseComObject(m_arrowProps);
				m_arrowProps = null;
			}
			if (m_ctxtProps != null)
			{
				Marshal.ReleaseComObject(m_ctxtProps);
				m_ctxtProps = null;
			}
			if (m_indexProps != null)
			{
				Marshal.ReleaseComObject(m_indexProps);
				m_indexProps = null;
			}
			if (m_resultProps != null)
			{
				Marshal.ReleaseComObject(m_resultProps);
				m_resultProps = null;
			}
			if (m_inputStr != null)
			{
				Marshal.ReleaseComObject(m_inputStr);
				m_inputStr = null;
			}
			if (m_resultStr != null)
			{
				Marshal.ReleaseComObject(m_resultStr);
				m_resultStr = null;
			}
			if (m_indexStr != null)
			{
				Marshal.ReleaseComObject(m_indexStr);
				m_indexStr = null;
			}
			if (m_doubleArrow != null)
			{
				Marshal.ReleaseComObject(m_doubleArrow);
				m_doubleArrow = null;
			}
			if (m_space != null)
			{
				Marshal.ReleaseComObject(m_space);
				m_space = null;
			}

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		protected override int MaxNumLines
		{
			get
			{
				int maxNumLines = 1;
				foreach (IPhContextOrVar ctxtOrVar in m_rule.InputOS)
				{
					int numLines = GetNumLines(ctxtOrVar);
					if (numLines > maxNumLines)
						maxNumLines = numLines;
				}

				return maxNumLines;
			}
		}

		protected override int GetVarIndex(IPhFeatureConstraint var)
		{
			return -1;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragRule:
					m_rule = new MoAffixProcess(m_cache, hvo);

					VwLength tableLen;
					tableLen.nVal = 10000;
					tableLen.unit = VwUnit.kunPercent100;
					vwenv.OpenTable(3, tableLen, 0, VwAlignment.kvaLeft, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 0, false);

					VwLength inputLen;
					inputLen.nVal = 0;
					inputLen.unit = VwUnit.kunPoint1000;

					int indexWidth = GetStrWidth(m_indexStr, m_headerProps, vwenv);
					int inputWidth = GetStrWidth(m_inputStr, m_headerProps, vwenv);
					VwLength headerLen;
					headerLen.nVal = Math.Max(indexWidth, inputWidth) + 8000;
					headerLen.unit = VwUnit.kunPoint1000;
					inputLen.nVal += headerLen.nVal;

					VwLength leftEmptyLen;
					leftEmptyLen.nVal = 8000 + (PILE_MARGIN * 2) + 2000;
					leftEmptyLen.unit = VwUnit.kunPoint1000;
					inputLen.nVal += leftEmptyLen.nVal;

					VwLength[] ctxtLens = new VwLength[m_rule.InputOS.Count];
					for (int i = 0; i < m_rule.InputOS.Count; i++)
					{
						int idxWidth = GetStrWidth(m_cache.MakeUserTss(Convert.ToString(i + 1)), m_indexProps, vwenv);
						int ctxtWidth = GetWidth(m_rule.InputOS[i], vwenv);
						ctxtLens[i].nVal = Math.Max(idxWidth, ctxtWidth) + 8000 + 1000;
						ctxtLens[i].unit = VwUnit.kunPoint1000;
						inputLen.nVal += ctxtLens[i].nVal;
					}

					VwLength rightEmptyLen;
					rightEmptyLen.nVal = 8000 + (PILE_MARGIN * 2) + 1000;
					rightEmptyLen.unit = VwUnit.kunPoint1000;
					inputLen.nVal += rightEmptyLen.nVal;

					vwenv.MakeColumns(1, inputLen);

					VwLength arrowLen;
					arrowLen.nVal = GetStrWidth(m_doubleArrow, m_arrowProps, vwenv) + 8000;
					arrowLen.unit = VwUnit.kunPoint1000;
					vwenv.MakeColumns(1, arrowLen);

					VwLength outputLen;
					outputLen.nVal = 1;
					outputLen.unit = VwUnit.kunRelative;
					vwenv.MakeColumns(1, outputLen);

					vwenv.OpenTableBody();
					vwenv.OpenTableRow();

					// input table cell
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenTable(m_rule.InputOS.Count + 3, tableLen, 0, VwAlignment.kvaCenter, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 4000, false);
					vwenv.MakeColumns(1, headerLen);
					vwenv.MakeColumns(1, leftEmptyLen);
					foreach (VwLength ctxtLen in ctxtLens)
						vwenv.MakeColumns(1, ctxtLen);
					vwenv.MakeColumns(1, rightEmptyLen);

					vwenv.OpenTableBody();
					vwenv.OpenTableRow();

					// input header cell
					vwenv.Props = m_headerProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_inputStr);
					vwenv.CloseTableCell();

					// input left empty cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					OpenContextPile(vwenv, false);
					vwenv.Props = m_bracketProps;
					vwenv.AddProp(ktagLeftEmpty, this, kfragEmpty);
					CloseContextPile(vwenv, false);
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// input context cells
					vwenv.AddObjVec((int)MoAffixProcess.MoAffixProcessTags.kflidInput, this, kfragInput);

					// input right empty cell
					vwenv.Props = m_ctxtProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					OpenContextPile(vwenv, false);
					vwenv.Props = m_bracketProps;
					vwenv.AddProp(ktagRightEmpty, this, kfragEmpty);
					CloseContextPile(vwenv, false);
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.OpenTableRow();

					// index header cell
					vwenv.Props = m_headerProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_indexStr);
					vwenv.CloseTableCell();

					// index left empty cell
					vwenv.Props = m_indexProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.CloseTableCell();

					// index cells
					for (int i = 0; i < m_rule.InputOS.Count; i++)
					{
						vwenv.Props = m_indexProps;
						vwenv.OpenTableCell(1, 1);
						vwenv.AddString(m_cache.MakeUserTss(Convert.ToString(i + 1)));
						vwenv.CloseTableCell();
					}

					// index right empty cell
					vwenv.Props = m_indexProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					vwenv.CloseTableCell();

					// double arrow cell
					vwenv.Props = m_arrowProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_doubleArrow);
					vwenv.CloseTableCell();

					// result table cell
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenTable(1, tableLen, 0, VwAlignment.kvaLeft, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 4000, false);
					vwenv.MakeColumns(1, outputLen);

					vwenv.OpenTableBody();
					vwenv.OpenTableRow();

					// result header cell
					vwenv.Props = m_headerProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_resultStr);
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.OpenTableRow();

					// result cell
					vwenv.Props = m_resultProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rule.OutputOS.Count == 0)
						vwenv.AddProp((int)MoAffixProcess.MoAffixProcessTags.kflidOutput, this, kfragEmpty);
					else
						vwenv.AddObjVecItems((int)MoAffixProcess.MoAffixProcessTags.kflidOutput, this, kfragRuleMapping);
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();

					vwenv.CloseTableCell();
					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					break;

				case kfragRuleMapping:
					IMoRuleMapping mapping = MoRuleMapping.CreateFromDBObject(m_cache, hvo);
					switch (mapping.ClassID)
					{
						case MoCopyFromInput.kclsidMoCopyFromInput:
							IMoCopyFromInput copy = mapping as IMoCopyFromInput;
							OpenIndexPile(vwenv);
							if (copy.ContentRAHvo == 0)
								vwenv.AddProp(ktagIndex, this, 0);
							else
								vwenv.AddProp(ktagIndex, this, copy.ContentRA.IndexInOwner + 1);
							CloseIndexPile(vwenv);
							break;

						case MoInsertPhones.kclsidMoInsertPhones:
							OpenIndexPile(vwenv);
							vwenv.AddObjVecItems((int)MoInsertPhones.MoInsertPhonesTags.kflidContent, this, kfragTerminalUnit);
							CloseIndexPile(vwenv);
							break;

						case MoModifyFromInput.kclsidMoModifyFromInput:
							IMoModifyFromInput modify = mapping as IMoModifyFromInput;
							int numLines = modify.ModificationRA.FeaturesOA.FeatureSpecsOC.Count;
							// left bracket pile
							vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PILE_MARGIN);
							vwenv.OpenInnerPile();

							vwenv.Props = m_bracketProps;
							vwenv.AddProp(ktagLeftBoundary, this, kfragZeroWidthSpace);

							// put index in the left bracket pile
							if (modify.ContentRAHvo == 0)
								vwenv.AddProp(ktagIndex, this, 0);
							else
								vwenv.AddProp(ktagIndex, this, modify.ContentRA.IndexInOwner + 1);
							// right align brackets in left bracket pile, since the index could have a greater width, then the bracket
							if (numLines > 1)
							{
								vwenv.Props = m_bracketProps;
								vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
								vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketUpHook);
								for (int i = 1; i < numLines - 1; i++)
								{
									vwenv.Props = m_bracketProps;
									vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
									vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketExt);
								}
								vwenv.Props = m_bracketProps;
								vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
								vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketLowHook);
							}
							else
							{
								vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
								vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracket);
							}
							vwenv.CloseInnerPile();

							// feature pile
							vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
							vwenv.OpenInnerPile();
							AddExtraLines(1, vwenv);
							if (numLines == 0)
								vwenv.AddProp((int)MoModifyFromInput.MoModifyFromInputTags.kflidModification, this, kfragQuestions);
							else
								vwenv.AddObjProp((int)MoModifyFromInput.MoModifyFromInputTags.kflidModification, this, kfragFeatNC);
							vwenv.CloseInnerPile();

							// right bracket pile
							vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PILE_MARGIN);
							vwenv.OpenInnerPile();
							vwenv.Props = m_bracketProps;
							vwenv.AddProp(ktagRightBoundary, this, kfragSpace);
							if (numLines > 1)
							{
								vwenv.Props = m_bracketProps;
								vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracketUpHook);
								for (int i = 1; i < numLines - 1; i++)
								{
									vwenv.Props = m_bracketProps;
									vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracketExt);
								}
								vwenv.Props = m_bracketProps;
								vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracketLowHook);
							}
							else
							{
								vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracket);
							}
							vwenv.CloseInnerPile();
							break;
					}
					break;

				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, object v, int frag)
		{
			if (tag == ktagIndex)
			{
				// pass the index in the frag argument
				return m_cache.MakeUserTss(Convert.ToString(frag));
			}
			else
			{
				switch (frag)
				{
					case kfragSpace:
						return m_space;

					default:
						return base.DisplayVariant(vwenv, tag, v, frag);
				}
			}
		}

		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			switch (frag)
			{
				case kfragInput:
					// input context cell
					int[] ctxtHvos = m_cache.GetVectorProperty(hvo, tag, false);
					foreach (int ctxtHvo in ctxtHvos)
					{
						vwenv.Props = m_ctxtProps;
						vwenv.OpenTableCell(1, 1);
						vwenv.OpenParagraph();
						vwenv.AddObj(ctxtHvo, this, kfragContext);
						vwenv.CloseParagraph();
						vwenv.CloseTableCell();
					}
					break;

				default:
					base.DisplayVec(vwenv, hvo, tag, frag);
					break;
			}
		}

		void OpenIndexPile(IVwEnv vwenv)
		{
			vwenv.Props = m_pileProps;
			vwenv.OpenInnerPile();
			vwenv.OpenParagraph();
			vwenv.Props = m_bracketProps;
			vwenv.AddProp(ktagLeftBoundary, this, kfragZeroWidthSpace);
		}

		void CloseIndexPile(IVwEnv vwenv)
		{
			vwenv.Props = m_bracketProps;
			vwenv.AddProp(ktagRightBoundary, this, kfragZeroWidthSpace);
			vwenv.CloseParagraph();
			vwenv.CloseInnerPile();
		}
	}
}
