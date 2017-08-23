// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Areas
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
	internal class RuleFormulaControl : ButtonLauncher, IPatternControl
	{
		protected enum RuleInsertType
		{
			Phoneme,
			NaturalClass,
			WordBoundary,
			MorphemeBoundary,
			Features,
			Variable,
			Index,
			Column
		};

		private static string GetOptionString(RuleInsertType type)
		{
			switch (type)
			{
				case RuleInsertType.MorphemeBoundary:
					return AreaResources.ksRuleMorphBdryOpt;

				case RuleInsertType.NaturalClass:
					return AreaResources.ksRuleNCOpt;

				case RuleInsertType.Phoneme:
					return AreaResources.ksRulePhonemeOpt;

				case RuleInsertType.WordBoundary:
					return AreaResources.ksRuleWordBdryOpt;

				case RuleInsertType.Features:
					return AreaResources.ksRuleFeaturesOpt;

				case RuleInsertType.Variable:
					return AreaResources.ksRuleVarOpt;

				case RuleInsertType.Index:
					return AreaResources.ksRuleIndexOpt;

				case RuleInsertType.Column:
					return AreaResources.ksRuleColOpt;
			}

			return null;
		}

		protected class InsertOption
		{
			private readonly RuleInsertType m_type;

			public InsertOption(RuleInsertType type)
			{
				m_type = type;
			}

			public RuleInsertType Type
			{
				get { return m_type; }
			}

			public override string ToString()
			{
				return GetOptionString(m_type);
			}
		}

		protected InsertionControl m_insertionControl;
		protected PatternView m_view;

		public RuleFormulaControl()
		{
			InitializeComponent();
		}

		public RuleFormulaControl(XElement configurationNode)
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

		public InsertionControl InsertionControl
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
					var ncCtxt = (IPhSimpleContextNC) ctxt;
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
					var ncCtxt = (IPhSimpleContextNC) ctxt;
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
					var segCtxt = (IPhSimpleContextSeg) ctxt;
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
				if (obj == null)
					return null;
				if (obj.ClassID == PhIterationContextTags.kClassId)
				{
					var iterCtxt = (IPhIterationContext) obj;
					return iterCtxt.MemberRA as IPhSimpleContext;
				}
				return obj as IPhSimpleContext;
			}
		}

		public ICmObject CurrentObject
		{
			get
			{
				CheckDisposed();
				SelectionHelper sel = SelectionHelper.Create(m_view);
				var obj = GetCmObject(sel, SelectionHelper.SelLimitType.Anchor);
				var endObj = GetCmObject(sel, SelectionHelper.SelLimitType.End);
				if (obj != endObj || obj == null || endObj == null)
					return null;
				return obj;
			}
		}

		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName,
			IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);

			m_mainControl = m_view;

			m_view.SelectionChanged += SelectionChanged;
			m_view.RemoveItemsRequested += RemoveItemsRequested;
			m_view.ContextMenuRequested += ContextMenuRequested;

			m_insertionControl.Insert += m_insertionControl_Insert;
		}

		private static int ToCellId(object ctxt)
		{
			return (int?) ctxt ?? -1;
		}

		private static object ToContextObject(int cellId)
		{
			if (cellId == -1)
				return null;
			return cellId;
		}

		public object GetContext(SelectionHelper sel)
		{
			return ToContextObject(GetCell(sel));
		}

		public object GetContext(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			return ToContextObject(GetCell(sel, limit));
		}

		public object GetItem(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			return GetCmObject(sel, limit);
		}

		public int GetItemContextIndex(object ctxt, object obj)
		{
			return GetItemCellIndex(ToCellId(ctxt), (ICmObject) obj);
		}

		public SelLevInfo[] GetLevelInfo(object ctxt, int index)
		{
			return GetLevelInfo(ToCellId(ctxt), index);
		}

		public int GetContextCount(object ctxt)
			{
			return GetCellCount(ToCellId(ctxt));
			}

		public object GetNextContext(object ctxt)
		{
			return ToContextObject(GetNextCell(ToCellId(ctxt)));
		}

		public object GetPrevContext(object ctxt)
		{
			return ToContextObject(GetPrevCell(ToCellId(ctxt)));
		}

		public int GetFlid(object ctxt)
		{
			return GetFlid(ToCellId(ctxt));
		}

		protected int GetCell(SelectionHelper sel)
		{
			if (sel == null)
				return -1;

			int cellId = GetCell(sel, SelectionHelper.SelLimitType.Anchor);

			if (sel.IsRange && cellId != -1)
			{
				int endCellId = GetCell(sel, SelectionHelper.SelLimitType.End);
				if (cellId != endCellId)
					return -1;
			}
			return cellId;
		}

		protected virtual int GetCell(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			throw new NotImplementedException();
		}

		protected virtual ICmObject GetCmObject(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			throw new NotImplementedException();
		}

		protected virtual int GetCellCount(int cellId)
		{
			throw new NotImplementedException();
		}

		protected virtual int GetItemCellIndex(int cellId, ICmObject obj)
		{
			throw new NotImplementedException();
		}

		protected virtual SelLevInfo[] GetLevelInfo(int cellId, int index)
		{
			throw new NotImplementedException();
		}

		protected virtual int GetNextCell(int cellId)
		{
			throw new NotImplementedException();
		}

		protected virtual int GetPrevCell(int cellId)
		{
			throw new NotImplementedException();
		}

		protected virtual int GetFlid(int cellId)
		{
			throw new NotImplementedException();
		}

		private int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex)
		{
			IPhSimpleContextNC ctxt;
			return InsertNC(nc, sel, out cellIndex, out ctxt);
		}

		/// <summary>
		/// Inserts an item from a natural class.
		/// </summary>
		/// <param name="nc">The natural class.</param>
		/// <param name="sel">The selection.</param>
		/// <param name="cellIndex">Index of the new item.</param>
		/// <param name="ctxt">The new context.</param>
		/// <returns>
		/// The ID of the cell that the item was inserted into
		/// </returns>
		protected virtual int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex, out IPhSimpleContextNC ctxt)
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

		protected virtual string FeatureChooserHelpTopic
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		protected virtual string RuleName
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		protected virtual string ContextMenuID
		{
			get
			{
				throw new NotImplementedException();
			}
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
				var node = m_configurationNode.Element("deParams");
				if (node != null)
					displayWs = XmlUtils.GetOptionalAttributeValue(node, "ws", "analysis vernacular").ToLower();
			}

			var labels = ObjectLabel.CreateObjectLabels(m_cache, candidates.OrderBy(e => e.ShortName), null, displayWs);

			using (var chooser = new SimpleListChooser(m_persistProvider, labels,
				m_fieldName, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				chooser.Cache = m_cache;
				chooser.TextParamHvo = m_cache.LangProject.PhonologicalDataOA.Hvo;
				chooser.SetHelpTopic(Slice.GetChooserHelpTopicID(Slice.HelpTopicID));
				chooser.InitializeExtras(m_configurationNode, PropertyTable);

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
				UndoableUnitOfWorkHelper.Do(AreaResources.ksRuleUndoUpdateEnv, AreaResources.ksRuleRedoUpdateEnv, selectedEnv, () =>
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
		/// <param name="e">The <see cref="InsertEventArgs"/> instance containing the event data.</param>
		private void m_insertionControl_Insert(object sender, InsertEventArgs e)
		{
			var option = (InsertOption) e.Option;

			var undo = string.Format(AreaResources.ksRuleUndoInsert, option);
			var redo = string.Format(AreaResources.ksRuleRedoInsert, option);

			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = -1;
			int cellIndex = -1;
			switch (option.Type)
			{
				case RuleInsertType.Phoneme:
					IEnumerable<IPhPhoneme> phonemes = m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.OrderBy(ph => ph.ShortName);
					ICmObject phonemeObj = DisplayChooser(AreaResources.ksRulePhonemeOpt, AreaResources.ksRulePhonemeChooserLink,
						"phonemeEdit", "RulePhonemeFlatList", phonemes);
					var phoneme = phonemeObj as IPhPhoneme;
					if (phoneme == null)
						return;
					UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
						{
							cellId = InsertPhoneme(phoneme, sel, out cellIndex);
						});
					break;

				case RuleInsertType.NaturalClass:
					IEnumerable<IPhNaturalClass> natClasses = m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.OrderBy(natc => natc.ShortName);
					ICmObject ncObj = DisplayChooser(AreaResources.ksRuleNCOpt, AreaResources.ksRuleNCChooserLink,
						"naturalClassEdit", "RuleNaturalClassFlatList", natClasses);
					var nc = ncObj as IPhNaturalClass;
					if (nc == null)
						return;
					UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
						{
							cellId = InsertNC(nc, sel, out cellIndex);
						});
					break;

				case RuleInsertType.Features:
					using (var featChooser = new PhonologicalFeatureChooserDlg())
					{
						SetupPhonologicalFeatureChoooserDlg(featChooser);
						featChooser.SetHelpTopic(FeatureChooserHelpTopic);
						DialogResult res = featChooser.ShowDialog();
						if (res == DialogResult.OK)
						{
							UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
								{
									IPhNCFeatures featNC = m_cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
									m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(featNC);
									featNC.Name.SetUserWritingSystem(string.Format(AreaResources.ksRuleNCFeatsName, RuleName));
									featNC.FeaturesOA = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
									IPhSimpleContextNC ctxt;
									cellId = InsertNC(featNC, sel, out cellIndex, out ctxt);
									featChooser.Context = ctxt;
									featChooser.UpdateFeatureStructure();
								});
						}
						else if (res != DialogResult.Cancel)
						{
							featChooser.HandleJump();
						}
					}
					break;

				case RuleInsertType.WordBoundary:
					IPhBdryMarker wordBdry = m_cache.ServiceLocator.GetInstance<IPhBdryMarkerRepository>().GetObject(LangProjectTags.kguidPhRuleWordBdry);
					UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
						{
							cellId = InsertBdry(wordBdry, sel, out cellIndex);
						});
					break;

				case RuleInsertType.MorphemeBoundary:
					IPhBdryMarker morphBdry = m_cache.ServiceLocator.GetInstance<IPhBdryMarkerRepository>().GetObject(LangProjectTags.kguidPhRuleMorphBdry);
					UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
						{
							cellId = InsertBdry(morphBdry, sel, out cellIndex);
						});
					break;

				case RuleInsertType.Index:
					// put the clicked index in the data field
					UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
						{
							cellId = InsertIndex((int) e.Suboption, sel, out cellIndex);
						});
					break;

				case RuleInsertType.Column:
					UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
						{
							cellId = InsertColumn(sel);
						});
					break;

				case RuleInsertType.Variable:
					UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
						{
							cellId = InsertVariable(sel, out cellIndex);
						});
					break;
			}

			m_view.Select();
			if (cellId != -1)
			{
				// reconstruct the view and place the cursor after the newly added item
				ReconstructView(cellId, cellIndex, false);
			}
		}

		protected virtual void SetupPhonologicalFeatureChoooserDlg(PhonologicalFeatureChooserDlg featChooser)
		{
			featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher);
		}

		protected ICmObject DisplayChooser(string fieldName, string linkText, string toolName, string guiControl, IEnumerable<ICmObject> candidates)
		{
			ICmObject obj = null;

			var labels = ObjectLabel.CreateObjectLabels(m_cache, candidates);

			using (var chooser = new SimpleListChooser(m_persistProvider, labels, fieldName, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				chooser.Cache = m_cache;
				chooser.TextParamHvo = m_cache.LangProject.PhonologicalDataOA.Hvo;
				Guid guidTextParam = m_cache.LangProject.PhonologicalDataOA.Guid;
				chooser.AddLink(linkText, ReallySimpleListChooser.LinkType.kGotoLink,
					new FwLinkArgs(toolName, guidTextParam));
				chooser.ReplaceTreeView(PropertyTable, guiControl);
				chooser.SetHelpTopic(FeatureChooserHelpTopic);

				DialogResult res = chooser.ShowDialog();
				if (res != DialogResult.Cancel)
				{
					chooser.HandleAnyJump();

					if (chooser.ChosenOne != null)
						obj = chooser.ChosenOne.Object;
				}
			}

			return obj;
		}

		protected int InsertContextInto(IPhSimpleContext ctxt, SelectionHelper sel, ILcmOwningSequence<IPhSimpleContext> seq)
		{
			ICmObject[] ctxts = seq.Cast<ICmObject>().ToArray();
			int index = GetInsertionIndex(ctxts, sel);
			// if the current selection is a range remove the items we are overwriting
			if (sel.IsRange)
			{
				var indices = GetIndicesToRemove(ctxts, sel);
				foreach (int idx in indices)
				{
					var c = (IPhSimpleContext) ctxts[idx];
					c.PreRemovalSideEffects();
					seq.Remove(c);
				}
			}
			seq.Insert(index, ctxt);
			return index;
		}

		protected int InsertContextInto(IPhSimpleContext ctxt, SelectionHelper sel, IPhSequenceContext seqCtxt)
		{
			m_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ctxt);

			ICmObject[] ctxts = seqCtxt.MembersRS.Cast<ICmObject>().ToArray();
			int index = GetInsertionIndex(ctxts, sel);
			seqCtxt.MembersRS.Insert(index, ctxt);
			// if the current selection is a range remove the items we are overwriting
			if (sel.IsRange)
			{
				var indices = GetIndicesToRemove(ctxts, sel);
				foreach (int idx in indices)
				{
					var c = (IPhPhonContext) ctxts[idx];
					c.PreRemovalSideEffects();
					m_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(c);
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
			var curObj = GetCmObject(sel, SelectionHelper.SelLimitType.Top);
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

		private void RemoveItemsRequested(object sender, RemoveItemsRequestedEventArgs e)
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = -1;
			int cellIndex = -1;
			UndoableUnitOfWorkHelper.Do(AreaResources.ksRuleUndoRemove, AreaResources.ksRuleRedoRemove, m_cache.ActionHandlerAccessor, () =>
			{
				cellId = RemoveItems(sel, e.Forward, out cellIndex);
			});

			// if the no cell is returned, then do not reconstruct
			if (cellId != -1 && cellId != -2)
				// if the cell index is -1 that means that we removed the first item in this cell,
				// so we move the cursor to the beginning of the first item after the removed items,
				// instead of the end of the item before the removed items.
				ReconstructView(cellId, cellIndex, cellIndex == -1);
		}

		protected bool RemoveContextsFrom(bool forward, SelectionHelper sel, ILcmOwningSequence<IPhSimpleContext> seq,
			bool preRemovalSideEffects, out int index)
		{
			index = -1;
			bool reconstruct = true;
			ICmObject[] ctxts = seq.Cast<ICmObject>().ToArray();
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

		private void ProcessIndicesSimpleContext(ILcmOwningSequence<IPhSimpleContext> seq, ICmObject[] ctxts, bool preRemovalSideEffects,
			int idx)
		{
			if (ctxts == null || idx > ctxts.Length - 1 || idx < 0)
				return;

			var c = (IPhSimpleContext) ctxts[idx];
			if (preRemovalSideEffects)
				c.PreRemovalSideEffects();
			seq.Remove(c);
		}

		protected bool RemoveContextsFrom(bool forward, SelectionHelper sel, IPhSequenceContext seqCtxt,
			bool preRemovalSideEffects, out int index)
		{
			index = -1;
			bool reconstruct = true;
			ICmObject[] ctxts = seqCtxt.MembersRS.Cast<ICmObject>().ToArray();
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
						ProcessIndicesSeqContext(ctxts, preRemovalSideEffects, idx);
					}
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

		private void ProcessIndicesSeqContext(ICmObject[] ctxts, bool preRemovalSideEffects, int idx)
		{
			if (ctxts == null || idx > ctxts.Length - 1 || idx < 0)
				return;

			var c = (IPhPhonContext) ctxts[idx];
			if (preRemovalSideEffects)
				c.PreRemovalSideEffects();
			m_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(c);
		}

		protected int[] GetIndicesToRemove(ICmObject[] objs, SelectionHelper sel)
		{
			var beginObj = GetCmObject(sel, SelectionHelper.SelLimitType.Top);
			var endObj = GetCmObject(sel, SelectionHelper.SelLimitType.Bottom);

			var remove = new List<int>();
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
			var obj = GetCmObject(sel, SelectionHelper.SelLimitType.Top);
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
						return i;
					}
					if (forward)
						return i == objs.Length ? -1 : i;
					return i - 1;
				}
			}

			return -1;
		}

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			((IFlexComponent)m_view).InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
		}

		/// <summary>
		/// Sets the phonological features for the currently selected natural class simple context with
		/// a feature-based natural class.
		/// </summary>
		public void SetContextFeatures()
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			bool reconstruct;

			using (var featChooser = new PhonologicalFeatureChooserDlg())
			{
				var ctxt = (IPhSimpleContextNC) CurrentContext;
				var natClass = (IPhNCFeatures) ctxt.FeatureStructureRA;
				featChooser.Title = AreaResources.ksRuleFeatsChooserTitle;
				if (m_obj is IPhSegRuleRHS)
				{
					featChooser.ShowFeatureConstraintValues = true;
					if (natClass.FeaturesOA != null)
					{
						var rule = m_obj as IPhSegRuleRHS;
						featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher, rule.OwningRule, ctxt);
					}
					else
						featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher, natClass, PhNCFeaturesTags.kflidFeatures);
				}
				else
				{
					if (natClass.FeaturesOA != null)
						featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher, natClass.FeaturesOA);
					else
						featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher);
				}
				// FWR-2405: Setting the Help topic requires that the Mediator be already set!
				featChooser.SetHelpTopic("khtpChoose-Grammar-PhonRules-SetPhonologicalFeatures");
				DialogResult res = featChooser.ShowDialog();
				if (res != DialogResult.Cancel)
					featChooser.HandleJump();
				reconstruct = res == DialogResult.OK;
			}

			m_view.Select();
			if (reconstruct)
			{
				m_view.RootBox.Reconstruct();
				sel.RestoreSelectionAndScrollPos();
			}
		}

		private void ContextMenuRequested(object sender, ContextMenuRequestedEventArgs e)
		{
			e.Selection.Install();
			var obj = CurrentObject;

			if (obj != null)
			{
				// we only bother to display the context menu if an item is selected
				using (var ui = new CmObjectUi(obj))
				{
					ui.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
					e.Handled = ui.HandleRightClick(this, true, ContextMenuID);
				}
			}
		}

		private void SelectionChanged(object sender, EventArgs eventArgs)
		{
			// since the context has changed update the display options on the insertion control
			m_insertionControl.UpdateOptionsDisplay();
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
			m_view.SelectAt(cellId, cellIndex, initial, true, true);
		}

		internal static bool IsWordBoundary(IPhContextOrVar ctxt)
		{
			if (ctxt == null)
				return false;

			if (ctxt.ClassID == PhSimpleContextBdryTags.kClassId)
			{
				var bdryCtxt = (IPhSimpleContextBdry) ctxt;
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
				var bdryCtxt = (IPhSimpleContextBdry) ctxt;
				if (bdryCtxt.FeatureStructureRA.Guid == LangProjectTags.kguidPhRuleMorphBdry)
					return true;
			}
			return false;
		}

		#region Component Designer generated code

		private void InitializeComponent()
		{
			this.m_view = new LanguageExplorer.Controls.LexText.PatternView();
			this.m_insertionControl = new InsertionControl();
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
		#endregion
	}
}
