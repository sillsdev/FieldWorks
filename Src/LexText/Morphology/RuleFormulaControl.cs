using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.Utils;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class class represents a rule formula control. It is not intended to be
	/// used directly. Specific phonological/morphological rules should extend this
	/// class. It is not abstract so that it can be modified in Windows Form designer. It
	/// is a button launcher with a rule formula view and a rule insertion control.
	///
	/// It handles selection, deletion, insertion, and cursor movement for a rule formula.
	/// Rules that extend this class override the methods that provide information about the
	/// various table cells in the rule and the data contained in each cell.
	/// </summary>
	public class RuleFormulaControl : ButtonLauncher
	{
		protected RuleInsertionControl m_insertionControl;
		protected RuleFormulaView m_view;
		protected string m_menuId;

		public RuleFormulaControl()
		{
			InitializeComponent();
		}

		public RuleFormulaControl(XmlNode configurationNode)
		{
			m_configurationNode = configurationNode;
			InitializeComponent();
		}

		public RootSite RootSite
		{
			get
			{
				CheckDisposed();
				return m_view;
			}
		}

		public RuleInsertionControl InsertionControl
		{
			get
			{
				CheckDisposed();
				return m_insertionControl;
			}
		}

		public override bool SliceIsCurrent
		{
			set
			{
				CheckDisposed();
				base.SliceIsCurrent = value;
				if (value)
					m_view.Select();
			}
		}

		protected override XCore.Mediator Mediator
		{
			get
			{
				return m_mediator;
			}
		}

		/// <summary>
		/// Indicates that a PhSimpleContextNC with a PhNCFeatures is currently selected.
		/// </summary>
		public bool IsFeatsNCContextCurrent
		{
			get
			{
				CheckDisposed();
				var ctxt = CurrentContext;
				if (ctxt  != null && ctxt.ClassID == PhSimpleContextNCTags.kClassId)
				{
					var ncCtxt = ctxt as IPhSimpleContextNC;
					if (ncCtxt.FeatureStructureRA != null)
						return ncCtxt.FeatureStructureRA.ClassID == PhNCFeaturesTags.kClassId;
				}
				return false;
			}
		}

		/// <summary>
		/// Indicates that a PhSimpleContextNC is currently selected.
		/// </summary>
		public bool IsNCContextCurrent
		{
			get
			{
				CheckDisposed();
				var ctxt = CurrentContext;
				if (ctxt != null && ctxt.ClassID == PhSimpleContextNCTags.kClassId)
				{
					var ncCtxt = ctxt as IPhSimpleContextNC;
					return ncCtxt.FeatureStructureRA != null;
				}
				return false;
			}
		}

		/// <summary>
		/// Indicates that a PhSimpleContextSeg is currently selected.
		/// </summary>
		public bool IsPhonemeContextCurrent
		{
			get
			{
				CheckDisposed();
				var ctxt = CurrentContext;
				if (ctxt != null && ctxt.ClassID == PhSimpleContextSegTags.kClassId)
				{
					var segCtxt = ctxt as IPhSimpleContextSeg;
					return segCtxt.FeatureStructureRA != null;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the currently selected simple context.
		/// </summary>
		/// <value>The current context hvo.</value>
		public IPhSimpleContext CurrentContext
		{
			get
			{
				CheckDisposed();
				var obj = CurrentObject;
				if (obj.ClassID == PhIterationContextTags.kClassId)
				{
					var iterCtxt = obj as IPhIterationContext;
					return iterCtxt.MemberRA as IPhSimpleContext;
				}
				else
				{
					return obj as IPhSimpleContext;
				}
			}
		}

		public ICmObject CurrentObject
		{
			get
			{
				CheckDisposed();
				SelectionHelper sel = SelectionHelper.Create(m_view);
				var obj = GetItem(sel, SelectionHelper.SelLimitType.Anchor);
				var endObj = GetItem(sel, SelectionHelper.SelLimitType.End);
				if (obj != endObj || obj == null || endObj == null)
					return null;
				return obj;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
				m_insertionControl.Insert -= new EventHandler<RuleInsertEventArgs>(m_insertionControl_Insert);
			}

			m_insertionControl = null;
			m_view = null;
			m_menuId = null;

			base.Dispose(disposing);
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider,
			XCore.Mediator mediator, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);

			m_mainControl = m_view;

			m_insertionControl.Insert += new EventHandler<RuleInsertEventArgs>(m_insertionControl_Insert);
		}

		/// <summary>
		/// Gets the ID of the currently selected cell. Any integer can be used as a cell ID, except
		/// <c>-1</c> and <c>-2</c>, which is used to indicate no cell.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <param name="limit">The limit.</param>
		/// <returns></returns>
		protected virtual int GetCell(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the currently selected item.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <param name="limit">The limit.</param>
		/// <returns></returns>
		protected virtual ICmObject GetItem(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the index of an item in the specified cell.
		/// </summary>
		/// <param name="cellId">The cell id.</param>
		/// <param name="obj">The object.</param>
		/// <returns></returns>
		protected virtual int GetItemCellIndex(int cellId, ICmObject obj)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the level information for selection purposes of the item at the specified
		/// index in the specified cell.
		/// </summary>
		/// <param name="cellId">The cell id.</param>
		/// <param name="cellIndex">Index of the cell.</param>
		/// <returns></returns>
		protected virtual SelLevInfo[] GetLevelInfo(int cellId, int cellIndex)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the number of items in the specified cell.
		/// </summary>
		/// <param name="cellId">The cell id.</param>
		/// <returns></returns>
		protected virtual int GetCellCount(int cellId)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the ID of the next cell from the specified cell.
		/// </summary>
		/// <param name="cellId">The cell id.</param>
		/// <returns></returns>
		protected virtual int GetNextCell(int cellId)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the ID of the previous cell from the specified cell.
		/// </summary>
		/// <param name="cellId">The cell id.</param>
		/// <returns></returns>
		protected virtual int GetPrevCell(int cellId)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Inserts an item from a natural class.
		/// </summary>
		/// <param name="nc">The natural class.</param>
		/// <param name="sel">The selection.</param>
		/// <param name="cellIndex">Index of the new item.</param>
		/// <returns>
		/// The ID of the cell that the item was inserted into
		/// </returns>
		protected virtual int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Inserts an item from a phoneme.
		/// </summary>
		/// <param name="phoneme">The phoneme.</param>
		/// <param name="sel">The selection.</param>
		/// <param name="cellIndex">Index of the new item.</param>
		/// <returns>
		/// The ID of the cell that the item was inserted into
		/// </returns>
		protected virtual int InsertPhoneme(IPhPhoneme phoneme, SelectionHelper sel, out int cellIndex)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Inserts an item from a boundary.
		/// </summary>
		/// <param name="bdry">The boundary.</param>
		/// <param name="sel">The sel.</param>
		/// <param name="cellIndex">Index of the new item.</param>
		/// <returns>
		/// The ID of the cell that the item was inserted into
		/// </returns>
		protected virtual int InsertBdry(IPhBdryMarker bdry, SelectionHelper sel, out int cellIndex)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Inserts a new column.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <returns>The ID of the new cell</returns>
		protected virtual int InsertColumn(SelectionHelper sel)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Inserts an item from the specified rule mapping index.
		/// </summary>
		/// <param name="index">The rule mapping index.</param>
		/// <param name="sel">The selection.</param>
		/// <param name="cellIndex">Index of the new item.</param>
		/// <returns>The ID of the cell that the item was inserted into</returns>
		protected virtual int InsertIndex(int index, SelectionHelper sel, out int cellIndex)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Inserts the variable (PhVariable).
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <param name="cellIndex">Index of the new item.</param>
		/// <returns>The ID of the cell that the item was inserted into</returns>
		protected virtual int InsertVariable(SelectionHelper sel, out int cellIndex)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes items based on the specified selection and direction.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <param name="forward">if <c>true</c> delete button was used, otherwise backspace was used</param>
		/// <param name="cellIndex">Index of the item before the removed items.</param>
		/// <returns>The ID of the cell that was removed from</returns>
		protected virtual int RemoveItems(SelectionHelper sel, bool forward, out int cellIndex)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Updates the environment.
		/// </summary>
		/// <param name="env">The environment.</param>
		/// <returns>The ID of the cell that the environment was updated in</returns>
		protected virtual int UpdateEnvironment(IPhEnvironment env)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the flid associated with the specified cell. It is used for selection purposes.
		/// </summary>
		/// <param name="cellId">The cell ID.</param>
		/// <returns>The flid.</returns>
		protected virtual int GetFlid(int cellId)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the ID of the selected cell.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <returns></returns>
		protected int GetCell(SelectionHelper sel)
		{
			if (sel == null)
				return -1;

			int cellId = GetCell(sel, SelectionHelper.SelLimitType.Anchor);

			if (sel.IsRange && cellId != -1 && cellId != -2)
			{
				int endCellId = GetCell(sel, SelectionHelper.SelLimitType.End);
				if (cellId != endCellId)
					return -1;
			}
			return cellId;
		}

		/// <summary>
		/// Handle launching of the environment chooser.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this method, if the SimpleListChooser is not suitable.
		/// </remarks>
		protected override void HandleChooser()
		{
			// get all valid environments
			var candidates = new HashSet<ICmObject>();
			foreach (var env in m_cache.LangProject.PhonologicalDataOA.EnvironmentsOS)
			{
				ConstraintFailure failure;
				if (env.CheckConstraints(PhEnvironmentTags.kflidStringRepresentation, false, out failure))
					candidates.Add(env);
			}

			string displayWs = "analysis vernacular";
			IPhEnvironment selectedEnv = null;
			if (m_configurationNode != null)
			{
				XmlNode node = m_configurationNode.SelectSingleNode("deParams");
				if (node != null)
					displayWs = XmlUtils.GetAttributeValue(node, "ws", "analysis vernacular").ToLower();
			}

			var labels = ObjectLabel.CreateObjectLabels(m_cache, candidates, null, displayWs);

			using (var chooser = new SimpleListChooser(m_persistProvider, labels,
				m_fieldName, m_mediator.HelpTopicProvider))
			{
				chooser.Cache = m_cache;
				chooser.TextParamHvo = m_cache.LangProject.PhonologicalDataOA.Hvo;
				chooser.SetHelpTopic(Slice.GetChooserHelpTopicID(Slice.HelpTopicID));
				chooser.InitializeExtras(m_configurationNode, m_mediator);

				DialogResult res = chooser.ShowDialog();
				if (res != DialogResult.Cancel)
				{
					chooser.HandleAnyJump();

					if (chooser.ChosenOne != null)
						selectedEnv = chooser.ChosenOne.Object as IPhEnvironment;
				}
			}

			// return focus to the view
			m_view.Select();
			if (selectedEnv != null)
			{
				int cellId = -1;
				UndoableUnitOfWorkHelper.Do(MEStrings.ksRuleUndoUpdateEnv, MEStrings.ksRuleRedoUpdateEnv, selectedEnv, () =>
				{
					cellId = UpdateEnvironment(selectedEnv);
				});

				ReconstructView(cellId, -1, true);
			}
		}

		/// <summary>
		/// Handles the Insert event of the m_insertionControl control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="SIL.FieldWorks.XWorks.MorphologyEditor.RuleInsertEventArgs"/> instance containing the event data.</param>
		void m_insertionControl_Insert(object sender, RuleInsertEventArgs e)
		{
			m_view.Select();

			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = -1;
			int cellIndex = -1;
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(e.UndoMsg, e.RedoMsg, m_cache.ActionHandlerAccessor, () =>
				{
			switch (e.Type)
			{
				case RuleInsertType.PHONEME:
					var phoneme = e.Data as IPhPhoneme;
					if (phoneme == null)
						return;
					cellId = InsertPhoneme(phoneme, sel, out cellIndex);
					break;

				case RuleInsertType.FEATURES:
				case RuleInsertType.NATURAL_CLASS:
					var nc = e.Data as IPhNaturalClass;
					if (nc == null)
						return;
					cellId = InsertNC(nc, sel, out cellIndex);
					break;

				case RuleInsertType.MORPHEME_BOUNDARY:
				case RuleInsertType.WORD_BOUNDARY:
					var bdry = e.Data as IPhBdryMarker;
					if (bdry == null)
						return;
					cellId = InsertBdry(bdry, sel, out cellIndex);
					break;

				case RuleInsertType.COLUMN:
					cellId = InsertColumn(sel);
					break;

				case RuleInsertType.INDEX:
					cellId = InsertIndex((int)e.Data, sel, out cellIndex);
					break;

				case RuleInsertType.VARIABLE:
					cellId = InsertVariable(sel, out cellIndex);
					break;
			}
				});
			if (cellId != -1)
			{
				// reconstruct the view and place the cursor after the newly added item
				ReconstructView(cellId, cellIndex, false);
			}
		}

		protected int InsertContextInto(IPhSimpleContext ctxt, SelectionHelper sel, IFdoOwningSequence<IPhSimpleContext> seq)
		{
			var ctxts = seq.ToArray();
			int index = GetInsertionIndex(ctxts, sel);
			// if the current selection is a range remove the items we are overwriting
			if (sel.IsRange)
			{
				var indices = GetIndicesToRemove(ctxts, sel);
				foreach (int idx in indices)
				{
					ctxts[idx].PreRemovalSideEffects();
					seq.Remove(ctxts[idx]);
				}
			}
			seq.Insert(index, ctxt);
			return index;
		}

		protected int InsertContextInto(IPhSimpleContext ctxt, SelectionHelper sel, IPhSequenceContext seqCtxt)
		{
			m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ctxt);

			var ctxts = seqCtxt.MembersRS.ToArray();
			int index = GetInsertionIndex(ctxts, sel);
			seqCtxt.MembersRS.Insert(index, ctxt);
			// if the current selection is a range remove the items we are overwriting
			if (sel.IsRange)
			{
				var indices = GetIndicesToRemove(ctxts, sel);
				foreach (int idx in indices)
				{
					ctxts[idx].PreRemovalSideEffects();
					m_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(ctxts[idx]);
				}
			}
			return index;
		}

		protected virtual int GetInsertionIndex(ICmObject[] objs, SelectionHelper sel)
		{
			if (objs.Length == 0)
			{
				return 0;
			}
			else
			{
				var curObj = GetItem(sel, SelectionHelper.SelLimitType.Top);
				int ich = sel.GetIch(SelectionHelper.SelLimitType.Top);
				for (int i = 0; i < objs.Length; i++)
				{
					// if the current ich is 0, then we can safely assume we are at the beginning of
					// the current item, so insert before it, otherwise we are in the middle in which
					// case the entire item is selected or at the end, so we insert after
					if (objs[i] == curObj)
						return ich == 0 ? i : i + 1;
				}
				return objs.Length;
			}
		}

		/// <summary>
		/// Removes items. This is called by the view when a delete or backspace button is pressed.
		/// </summary>
		/// <param name="forward">if <c>true</c> the delete button was pressed, otherwise backspace was pressed</param>
		public void RemoveItems(bool forward)
		{
			CheckDisposed();
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = -1;
			int cellIndex = -1;
			UndoableUnitOfWorkHelper.Do(MEStrings.ksRuleUndoRemove, MEStrings.ksRuleRedoRemove, m_cache.ActionHandlerAccessor, () =>
			{
				cellId = RemoveItems(sel, forward, out cellIndex);
			});

			// if the no cell is returned, then do not reconstruct
			if (cellId != -1 && cellId != -2)
				// if the cell index is -1 that means that we removed the first item in this cell,
				// so we move the cursor to the beginning of the first item after the removed items,
				// instead of the end of the item before the removed items.
				ReconstructView(cellId, cellIndex, cellIndex == -1);
		}

		protected bool RemoveContextsFrom(bool forward, SelectionHelper sel, IFdoOwningSequence<IPhSimpleContext> seq,
			bool preRemovalSideEffects, out int index)
		{
			index = -1;
			bool reconstruct = true;
			var ctxts = seq.ToArray();
			// if the selection is a range remove all items in the selection
			if (sel.IsRange)
			{
				int[] indices = GetIndicesToRemove(ctxts, sel);
				// return index of the item before the removed items
				if (indices.Length > 0)
					index = indices[0] - 1;
				foreach (int idx in indices)
				{
					// Sometimes when deleting a range, DeleteUnderlyingObject() takes out
					// parts of the rule before this loop gets to it. [LT-9775]
					if (ctxts[idx].IsValidObject)
						ProcessIndicesSimpleContext(seq, ctxts, preRemovalSideEffects, idx);
				}
			}
			else
			{
				int idx = GetIndexToRemove(ctxts, sel, forward);
				if (idx > -1)
				{
					// return index of the item before the removed items
					index = idx - 1;
					ProcessIndicesSimpleContext(seq, ctxts, preRemovalSideEffects, idx);
				}
				else
				{
					// if the backspace button is pressed at the beginning of a cell or the delete
					// button is pressed at the end of a cell, don't do anything
					reconstruct = false;
				}
			}
			return reconstruct;
		}

		private void ProcessIndicesSimpleContext(IFdoOwningSequence<IPhSimpleContext> seq, IPhSimpleContext[] ctxts, bool preRemovalSideEffects,
			int idx)
		{
			if (ctxts == null || idx > ctxts.Length - 1 || idx < 0)
				return;

			if (preRemovalSideEffects)
				ctxts[idx].PreRemovalSideEffects();
			seq.Remove(ctxts[idx]);
		}

		protected bool RemoveContextsFrom(bool forward, SelectionHelper sel, IPhSequenceContext seqCtxt,
			bool preRemovalSideEffects, out int index)
		{
			index = -1;
			bool reconstruct = true;
			var ctxts = seqCtxt.MembersRS.ToArray();
			// if the selection is a range remove all items in the selection
			if (sel.IsRange)
			{
				int[] indices = GetIndicesToRemove(ctxts, sel);
				// return index of the item before the removed items
				if (indices.Length > 0)
					index = indices[0] - 1;

				foreach (int idx in indices)
					// Sometimes when deleting a range, DeleteUnderlyingObject() takes out
					// parts of the rule before this loop gets to it. [LT-9775]
					if (ctxts[idx].IsValidObject)
						ProcessIndicesSeqContext(ctxts, preRemovalSideEffects, idx);
			}
			else
			{
				int idx = GetIndexToRemove(ctxts, sel, forward);
				if (idx > -1)
				{
					// return index of the item before the removed items
					index = idx - 1;
					ProcessIndicesSeqContext(ctxts, preRemovalSideEffects, idx);
				}
				else
				{
					// if the backspace button is pressed at the beginning of a cell or the delete
					// button is pressed at the end of a cell, don't do anything
					reconstruct = false;
				}
			}

			return reconstruct;
		}

		private void ProcessIndicesSeqContext(IPhPhonContext[] ctxts, bool preRemovalSideEffects, int idx)
		{
			if (ctxts == null || idx > ctxts.Length - 1 || idx < 0)
				return;

			if (preRemovalSideEffects)
				ctxts[idx].PreRemovalSideEffects();
			m_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(ctxts[idx]);
		}

		protected int[] GetIndicesToRemove(ICmObject[] objs, SelectionHelper sel)
		{
			var beginObj = GetItem(sel, SelectionHelper.SelLimitType.Top);
			var endObj = GetItem(sel, SelectionHelper.SelLimitType.Bottom);

			List<int> remove = new List<int>();
			bool inRange = false;
			for (int i = 0; i < objs.Length; i++)
			{
				if (objs[i] == beginObj)
				{
					remove.Add(i);
					if (beginObj == endObj)
						break;
					inRange = true;
				}
				else if (objs[i] == endObj)
				{
					remove.Add(i);
					inRange = false;
				}
				else if (inRange)
				{
					remove.Add(i);
				}
			}
			return remove.ToArray();
		}

		protected int GetIndexToRemove(ICmObject[] objs, SelectionHelper sel, bool forward)
		{
			var obj = GetItem(sel, SelectionHelper.SelLimitType.Top);
			for (int i = 0; i < objs.Length; i++)
			{
				if (objs[i] == obj)
				{
					var tss = sel.GetTss(SelectionHelper.SelLimitType.Anchor);
					// if the current ich is at the end of the current string, then we can safely assume
					// we are at the end of the current item, so remove it or the next item based on what
					// key was pressed, otherwise we are in the middle in which
					// case the entire item is selected, or at the beginning, so we remove it or the previous
					// item based on what key was pressed
					if (sel.IchAnchor == tss.Length)
					{
						if (forward)
							return i == objs.Length ? -1 : i + 1;
						else
							return i;
					}
					else
					{
						if (forward)
							return i == objs.Length ? -1 : i;
						else
							return i - 1;
					}
				}
			}

			return -1;
		}

		/// <summary>
		/// Sets the phonological features for the currently selected natural class simple context with
		/// a feature-based natural class.
		/// </summary>
		public void SetContextFeatures()
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			bool reconstruct = false;

			UndoableUnitOfWorkHelper.Do(MEStrings.ksRuleUndoSetFeatures, MEStrings.ksRuleRedoSetFeatures, m_cache.ActionHandlerAccessor, () =>
			{
				using (var featChooser = new PhonologicalFeatureChooserDlg())
				{
					var ctxt = CurrentContext as IPhSimpleContextNC;
					IPhNCFeatures natClass = ctxt.FeatureStructureRA as IPhNCFeatures;
					featChooser.Title = MEStrings.ksRuleFeatsChooserTitle;

					if (natClass.FeaturesOA != null)
						featChooser.SetDlgInfo(m_cache, Mediator, natClass.FeaturesOA);
					else
						featChooser.SetDlgInfo(m_cache, Mediator, natClass, PhNCFeaturesTags.kflidFeatures);

					// FWR-2405: Setting the Help topic requires that the Mediator be already set!
					featChooser.SetHelpTopic("khtpChoose-Grammar-PhonRules-SetPhonologicalFeatures");
					DialogResult res = featChooser.ShowDialog();
					if (res != DialogResult.Cancel)
						featChooser.HandleJump();
					reconstruct = res == DialogResult.OK;
				}
			});

			m_view.Select();
			if (reconstruct)
			{
				m_view.RootBox.Reconstruct();
				sel.RestoreSelectionAndScrollPos();
			}
		}

		public virtual bool DisplayContextMenu(IVwSelection vwselNew)
		{
			SelectionHelper sel = SelectionHelper.Create(vwselNew, m_view);
			var obj = CurrentObject;

			if (obj != null)
			{
				// we only bother to display the context menu if an item is selected
				using (CmObjectUi ui = new CmObjectUi(obj))
					return ui.HandleRightClick(Mediator, this, true, m_menuId);
			}
			else
			{
				return false;
			}
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
			int cellId = GetCell(sel);
			if (sel != null)
			{
				// if cell ID is -1 or -2 then we are trying to select outside of a single cell
				if (cellId == -1 || cellId == -2)
				{
					if (sel.IsRange)
					{
						// ensure that a range selection only occurs within one cell
						int topCellId = GetCell(sel, SelectionHelper.SelLimitType.Top);
						int bottomCellId = GetCell(sel, SelectionHelper.SelLimitType.Bottom);
						SelectionHelper.SelLimitType limit = SelectionHelper.SelLimitType.Top;
						if (topCellId != -1 && topCellId != -2)
						{
							limit = SelectionHelper.SelLimitType.Top;
							cellId = topCellId;
						}
						else if (bottomCellId != -1 && bottomCellId != -2)
						{
							limit = SelectionHelper.SelLimitType.Bottom;
							cellId = bottomCellId;
						}

						if (cellId != -1 && cellId != -2)
						{
							IVwSelection newSel = SelectCell(cellId, limit == SelectionHelper.SelLimitType.Bottom, false);
							SelectionHelper.SelLimitType otherLimit = limit == SelectionHelper.SelLimitType.Top
								? SelectionHelper.SelLimitType.Bottom : SelectionHelper.SelLimitType.Top;
							sel.ReduceToIp(limit);
							IVwSelection otherSel = sel.SetSelection(m_view, false, false);
							if (sel.Selection.EndBeforeAnchor)
								m_view.RootBox.MakeRangeSelection(limit == SelectionHelper.SelLimitType.Top ? newSel : otherSel,
									limit == SelectionHelper.SelLimitType.Top ? otherSel : newSel, true);
							else
								m_view.RootBox.MakeRangeSelection(limit == SelectionHelper.SelLimitType.Top ? otherSel : newSel,
									limit == SelectionHelper.SelLimitType.Top ? newSel : otherSel, true);
						}

					}
				}
				else
				{
					AdjustSelection(sel);
				}
			}
			// since the context has changed update the display options on the insertion control
			m_insertionControl.UpdateOptionsDisplay();
		}

		/// <summary>
		/// Adjusts the selection.
		/// </summary>
		/// <param name="sel">The selection.</param>
		void AdjustSelection(SelectionHelper sel)
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
		bool GetSelectionInfo(SelectionHelper sel, SelectionHelper.SelLimitType limit, out IVwSelection vwSel,
			out int curHvo, out int curIch, out int curTag)
		{
			vwSel = null;
			curHvo = 0;
			curIch = -1;
			curTag = -1;

			var obj = GetItem(sel, limit);
			if (obj == null)
				return false;

			ITsString curTss;
			int ws;
			bool prev;

			sel.Selection.TextSelInfo(limit == SelectionHelper.SelLimitType.End, out curTss, out curIch, out prev, out curHvo, out curTag, out ws);

			int cellId = GetCell(sel);
			int cellIndex = GetItemCellIndex(cellId, obj);

			if (!sel.IsRange)
			{
				// if the current selection is an IP, check if it is in one of the off-limits areas, and move the IP
				if (curIch == 0 && curTag == RuleFormulaVc.ktagLeftNonBoundary)
				{
					// the cursor is at a non-selectable left edge of an item, so
					// move to the selectable left edge
					SelectLeftBoundary(cellId, cellIndex, true);
					return false;
				}
				else if (curIch == curTss.Length && curTag == RuleFormulaVc.ktagLeftNonBoundary)
				{
					// the cursor has been moved to the left from the left boundary, so move the
					// cursor to the previous item in the cell or the previous cell
					if (cellIndex > 0)
					{
						SelectAt(cellId, cellIndex - 1, false, true, true);
					}
					else
					{
						int prevCellId = GetPrevCell(cellId);
						if (prevCellId != -1)
							SelectCell(prevCellId, false, true);
						else
							SelectLeftBoundary(cellId, cellIndex, true);

					}
					return false;
				}
				else if (curIch == curTss.Length && curTag == RuleFormulaVc.ktagRightNonBoundary)
				{
					// the cursor is at a non-selectable right edge of an item, so move to the
					// selectable right edge
					SelectRightBoundary(cellId, cellIndex, true);
					return false;
				}
				else if (curIch == 0 && curTag == RuleFormulaVc.ktagRightNonBoundary)
				{
					// the cursor has been moved to the right from the right boundary, so move the
					// cursor to the next item in the cell or the next cell
					if (cellIndex < GetCellCount(cellId) - 1)
					{
						SelectAt(cellId, cellIndex + 1, true, true, true);
					}
					else
					{
						int nextCellId = GetNextCell(cellId);
						if (nextCellId != -1)
							SelectCell(nextCellId, true, true);
						else
							SelectRightBoundary(cellId, cellIndex, true);

					}
					return false;
				}
				else if (!sel.Selection.IsEditable)
				{
					//SelectAt(cellId, cellIndex, true, true, true);
					return false;
				}
			}

			// find the beginning of the currently selected item
			IVwSelection initialSel = SelectAt(cellId, cellIndex, true, false, false);

			ITsString tss;
			int selCellIndex = cellIndex;
			int initialHvo, initialIch, initialTag;
			if (initialSel == null)
				return false;
			initialSel.TextSelInfo(false, out tss, out initialIch, out prev, out initialHvo, out initialTag, out ws);
			// are we at the beginning of an item?
			if ((curHvo == initialHvo && curIch == initialIch && curTag == initialTag)
				|| (curIch == 0 && curTag == RuleFormulaVc.ktagLeftBoundary)
				//|| (m_cache.GetClassOfObject(hvo) != PhIterationContext.kclsidPhIterationContext && curIch == 0 && curTag == (int)PhTerminalUnit.PhTerminalUnitTags.kflidName)
				|| (curIch == 0 && curTag == RuleFormulaVc.ktagXVariable))
			{
				// if the current selection is an IP, then don't adjust anything
				if (!sel.IsRange)
					return false;

				// if we are the beginning of the current item, and the current selection is a range, and the end is before the anchor,
				// then do not include the current item in the adjusted range selection
				if (sel.Selection.EndBeforeAnchor && limit == SelectionHelper.SelLimitType.Anchor)
					selCellIndex = cellIndex - 1;
			}
			else
			{
				int finalIch, finalHvo, finalTag;
				IVwSelection finalSel = SelectAt(cellId, cellIndex, false, false, false);
				finalSel.TextSelInfo(false, out tss, out finalIch, out prev, out finalHvo, out finalTag, out ws);
				// are we at the end of an item?
				if ((curHvo == finalHvo && curIch == finalIch && curTag == finalTag)
					|| (curIch == curTss.Length && curTag == RuleFormulaVc.ktagRightBoundary))
				{
					// if the current selection is an IP, then don't adjust anything
					if (!sel.IsRange)
						return false;

					// if we are the end of the current item, and the current selection is a range, and the anchor is before the end,
					// then do not include the current item in the adjusted range selection
					if (!sel.Selection.EndBeforeAnchor && limit == SelectionHelper.SelLimitType.Anchor)
						selCellIndex = cellIndex + 1;
				}
			}

			bool initial = limit == SelectionHelper.SelLimitType.Anchor ? !sel.Selection.EndBeforeAnchor : sel.Selection.EndBeforeAnchor;
			vwSel = SelectAt(cellId, selCellIndex, initial, false, false);

			return vwSel != null;
		}

		/// <summary>
		/// Reconstructs the view and moves the cursor the specified position.
		/// </summary>
		/// <param name="cellId">The cell id.</param>
		/// <param name="cellIndex">Index of the item in the cell.</param>
		/// <param name="initial">if <c>true</c> move the cursor to the beginning of the specified item, otherwise it is moved to the end</param>
		protected void ReconstructView(int cellId, int cellIndex, bool initial)
		{
			m_view.RootBox.Reconstruct();
			SelectAt(cellId, cellIndex, initial, true, true);
		}

		IVwSelection SelectLeftBoundary(int cellId, int cellIndex, bool install)
		{
			List<SelLevInfo> levels = new List<SelLevInfo>(GetLevelInfo(cellId, cellIndex));
			// if the current item is an iteration context, include the extra level
			//if (m_cache.GetClassOfObject(hvo) == PhIterationContext.kclsidPhIterationContext)
			//{
			//    SelLevInfo iterCtxtLev = new SelLevInfo();
			//    iterCtxtLev.tag = (int)PhIterationContext.PhIterationContextTags.kflidMember;
			//    levels.Insert(0, iterCtxtLev);
			//}
			try
			{
				return m_view.RootBox.MakeTextSelection(0, levels.Count, levels.ToArray(), RuleFormulaVc.ktagLeftBoundary, 0, 0, 0,
					0, false, -1, null, install);
			}
			catch (Exception)
			{
				return null;
			}
		}

		IVwSelection SelectRightBoundary(int cellId, int cellIndex, bool install)
		{
			SelLevInfo[] levels = GetLevelInfo(cellId, cellIndex);
			try
			{
				return m_view.RootBox.MakeTextSelection(0, levels.Length, levels, RuleFormulaVc.ktagRightBoundary, 0, 1, 1,
					0, false, -1, null, install);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Moves the cursor to the specified position in the specified cell.
		/// </summary>
		/// <param name="cellId">The cell id.</param>
		/// <param name="cellIndex">Index of the item in the cell.</param>
		/// <param name="initial">if <c>true</c> move the cursor to the beginning of the specified item, otherwise it is moved to the end</param>
		/// <param name="editable">if <c>true</c> move the cursor to the first editable position</param>
		/// <param name="install">if <c>true</c> install the selection</param>
		/// <returns>The new selection</returns>
		protected IVwSelection SelectAt(int cellId, int cellIndex, bool initial, bool editable, bool install)
		{
			SelLevInfo[] levels = GetLevelInfo(cellId, cellIndex);
			if (levels == null)
			{
				int count = GetCellCount(cellId);
				if (count == 0)
				{
					SelectionHelper newSel = new SelectionHelper();
					newSel.SetTextPropId(SelectionHelper.SelLimitType.Anchor, GetFlid(cellId));
					return newSel.SetSelection(m_view, install, false);
				}
				else
				{
					levels = GetLevelInfo(cellId, initial ? 0 : count - 1);
				}
			}

			return m_view.RootBox.MakeTextSelInObj(0, levels.Length, levels, 0, null, initial, editable, false, false, install);
		}

		IVwSelection SelectCell(int cellId, bool initial, bool install)
		{
			return SelectAt(cellId, -1, initial, true, install);
		}

		internal static bool IsWordBoundary(IPhContextOrVar ctxt)
		{
			if (ctxt == null)
				return false;

			if (ctxt.ClassID == PhSimpleContextBdryTags.kClassId)
			{
				var bdryCtxt = ctxt as IPhSimpleContextBdry;
				if (bdryCtxt.FeatureStructureRA.Guid == LangProjectTags.kguidPhRuleWordBdry)
					return true;
			}
			return false;
		}

		internal static bool IsMorphBoundary(IPhContextOrVar ctxt)
		{
			if (ctxt == null)
				return false;

			if (ctxt.ClassID == PhSimpleContextBdryTags.kClassId)
			{
				var bdryCtxt = ctxt as IPhSimpleContextBdry;
				if (bdryCtxt.FeatureStructureRA.Guid == LangProjectTags.kguidPhRuleMorphBdry)
					return true;
			}
			return false;
		}

		private void InitializeComponent()
		{
			this.m_view = new SIL.FieldWorks.XWorks.MorphologyEditor.RuleFormulaView();
			this.m_insertionControl = new SIL.FieldWorks.XWorks.MorphologyEditor.RuleInsertionControl();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_panel
			//
			this.m_panel.Location = new System.Drawing.Point(225, 0);
			this.m_panel.Size = new System.Drawing.Size(22, 20);
			//
			// m_btnLauncher
			//
			this.m_btnLauncher.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.m_btnLauncher.Size = new System.Drawing.Size(22, 20);
			//
			// m_view
			//
			this.m_view.BackColor = System.Drawing.SystemColors.Window;
			this.m_view.Dock = System.Windows.Forms.DockStyle.Left;
			this.m_view.DoSpellCheck = false;
			this.m_view.Group = null;
			this.m_view.IsTextBox = false;
			this.m_view.Location = new System.Drawing.Point(0, 0);
			this.m_view.Mediator = null;
			this.m_view.Name = "m_view";
			this.m_view.ReadOnlyView = false;
			this.m_view.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_view.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_view.ShowRangeSelAfterLostFocus = false;
			this.m_view.Size = new System.Drawing.Size(226, 20);
			this.m_view.SizeChangedSuppression = false;
			this.m_view.TabIndex = 3;
			this.m_view.WritingSystemFactory = null;
			this.m_view.WsPending = -1;
			this.m_view.Zoom = 1F;
			//
			// m_insertionControl
			//
			this.m_insertionControl.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.m_insertionControl.Location = new System.Drawing.Point(0, 20);
			this.m_insertionControl.Name = "m_insertionControl";
			this.m_insertionControl.Size = new System.Drawing.Size(247, 23);
			this.m_insertionControl.TabIndex = 2;
			//
			// RuleFormulaControl
			//
			this.Controls.Add(this.m_view);
			this.Controls.Add(this.m_insertionControl);
			this.Name = "RuleFormulaControl";
			this.Size = new System.Drawing.Size(247, 43);
			this.Controls.SetChildIndex(this.m_insertionControl, 0);
			this.Controls.SetChildIndex(this.m_view, 0);
			this.Controls.SetChildIndex(this.m_panel, 0);
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
	}
}
