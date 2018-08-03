// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
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
		protected PatternView _view;
		protected ISharedEventHandlers _sharedEventHandlers;
		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> _rightClickTuple;

		public RuleFormulaControl()
		{
			InitializeComponent();
		}

		public RuleFormulaControl(ISharedEventHandlers sharedEventHandlers, XElement configurationNode)
			:this()
		{
			Guard.AgainstNull(sharedEventHandlers, nameof(_sharedEventHandlers));
			Guard.AgainstNull(configurationNode, nameof(configurationNode));

			_sharedEventHandlers = sharedEventHandlers;
			m_configurationNode = configurationNode;
		}

		public RootSite RootSite => _view;

		public InsertionControl InsertionControl { get; protected set; }

		public override bool SliceIsCurrent
		{
			set
			{
				base.SliceIsCurrent = value;
				if (value)
				{
					_view.Select();
				}
			}
		}

		protected RuleFormulaSlice MyRuleFormulaSlice => Parent.Parent.Parent as RuleFormulaSlice;

		/// <summary>
		/// Indicates that a PhSimpleContextNC with a PhNCFeatures is currently selected.
		/// </summary>
		public bool IsFeatsNCContextCurrent
		{
			get
			{
				var ctxt = CurrentContext;
				if (ctxt  != null && ctxt.ClassID == PhSimpleContextNCTags.kClassId)
				{
					var ncCtxt = (IPhSimpleContextNC) ctxt;
					if (ncCtxt.FeatureStructureRA != null)
					{
						return ncCtxt.FeatureStructureRA.ClassID == PhNCFeaturesTags.kClassId;
					}
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
				var ctxt = CurrentContext;
				if (ctxt == null || ctxt.ClassID != PhSimpleContextNCTags.kClassId)
				{
					return false;
				}
				var ncCtxt = (IPhSimpleContextNC)ctxt;
				return ncCtxt.FeatureStructureRA != null;
			}
		}

		/// <summary>
		/// Indicates that a PhSimpleContextSeg is currently selected.
		/// </summary>
		public bool IsPhonemeContextCurrent
		{
			get
			{
				var ctxt = CurrentContext;
				if (ctxt == null || ctxt.ClassID != PhSimpleContextSegTags.kClassId)
				{
					return false;
				}
				var segCtxt = (IPhSimpleContextSeg)ctxt;
				return segCtxt.FeatureStructureRA != null;
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
				var obj = CurrentObject;
				if (obj == null)
				{
					return null;
				}

				if (obj.ClassID != PhIterationContextTags.kClassId)
				{
					return obj as IPhSimpleContext;
				}
				var iterCtxt = (IPhIterationContext)obj;
				return iterCtxt.MemberRA as IPhSimpleContext;
			}
		}

		public ICmObject CurrentObject
		{
			get
			{
				var sel = SelectionHelper.Create(_view);
				var obj = GetCmObject(sel, SelectionHelper.SelLimitType.Anchor);
				var endObj = GetCmObject(sel, SelectionHelper.SelLimitType.End);
				if (obj != endObj || obj == null || endObj == null)
				{
					return null;
				}
				return obj;
			}
		}

		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);

			m_mainControl = _view;

			_view.SelectionChanged += SelectionChanged;
			_view.RemoveItemsRequested += RemoveItemsRequested;
			_view.ContextMenuRequested += ContextMenuRequested;

			InsertionControl.Insert += m_insertionControl_Insert;
		}

		#region Overrides of ButtonLauncher
		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_view != null)
				{
					_view.SelectionChanged -= SelectionChanged;
					_view.RemoveItemsRequested -= RemoveItemsRequested;
					_view.ContextMenuRequested -= ContextMenuRequested;
					_view.Dispose();
				}
				InsertionControl.Insert -= m_insertionControl_Insert;
			}
			_view = null;
			_sharedEventHandlers = null;

			base.Dispose(disposing);
		}
		#endregion

		private static int ToCellId(object ctxt)
		{
			return (int?) ctxt ?? -1;
		}

		private static object ToContextObject(int cellId)
		{
			if (cellId == -1)
			{
				return null;
			}
			return cellId;
		}

		object IPatternControl.GetContext(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			return ToContextObject(GetCell(sel, limit));
		}

		object IPatternControl.GetItem(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			return GetCmObject(sel, limit);
		}

		int IPatternControl.GetItemContextIndex(object ctxt, object obj)
		{
			return GetItemCellIndex(ToCellId(ctxt), (ICmObject) obj);
		}

		SelLevInfo[] IPatternControl.GetLevelInfo(object ctxt, int index)
		{
			return GetLevelInfo(ToCellId(ctxt), index);
		}

		int IPatternControl.GetContextCount(object ctxt)
		{
			return GetCellCount(ToCellId(ctxt));
		}

		object IPatternControl.GetNextContext(object ctxt)
		{
			return ToContextObject(GetNextCell(ToCellId(ctxt)));
		}

		object IPatternControl.GetPrevContext(object ctxt)
		{
			return ToContextObject(GetPrevCell(ToCellId(ctxt)));
		}

		int IPatternControl.GetFlid(object ctxt)
		{
			return GetFlid(ToCellId(ctxt));
		}

		protected int GetCell(SelectionHelper sel)
		{
			if (sel == null)
			{
				return -1;
			}

			var cellId = GetCell(sel, SelectionHelper.SelLimitType.Anchor);

			if (sel.IsRange && cellId != -1)
			{
				var endCellId = GetCell(sel, SelectionHelper.SelLimitType.End);
				if (cellId != endCellId)
				{
					return -1;
				}
			}
			return cellId;
		}

		protected virtual int GetCell(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			throw new NotSupportedException();
		}

		protected virtual ICmObject GetCmObject(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			throw new NotSupportedException();
		}

		protected virtual int GetCellCount(int cellId)
		{
			throw new NotSupportedException();
		}

		protected virtual int GetItemCellIndex(int cellId, ICmObject obj)
		{
			throw new NotSupportedException();
		}

		protected virtual SelLevInfo[] GetLevelInfo(int cellId, int index)
		{
			throw new NotSupportedException();
		}

		protected virtual int GetNextCell(int cellId)
		{
			throw new NotSupportedException();
		}

		protected virtual int GetPrevCell(int cellId)
		{
			throw new NotSupportedException();
		}

		protected virtual int GetFlid(int cellId)
		{
			throw new NotSupportedException();
		}

		private int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex)
		{
			IPhSimpleContextNC ctxt;
			return InsertNC(nc, sel, out cellIndex, out ctxt);
		}

		/// <summary>
		/// Inserts an item from a natural class.
		/// </summary>
		protected virtual int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex, out IPhSimpleContextNC ctxt)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Inserts an item from a phoneme.
		/// </summary>
		protected virtual int InsertPhoneme(IPhPhoneme phoneme, SelectionHelper sel, out int cellIndex)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Inserts an item from a boundary.
		/// </summary>
		protected virtual int InsertBdry(IPhBdryMarker bdry, SelectionHelper sel, out int cellIndex)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Inserts a new column.
		/// </summary>
		protected virtual int InsertColumn(SelectionHelper sel)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Inserts an item from the specified rule mapping index.
		/// </summary>
		protected virtual int InsertIndex(int index, SelectionHelper sel, out int cellIndex)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Inserts the variable (PhVariable).
		/// </summary>
		protected virtual int InsertVariable(SelectionHelper sel, out int cellIndex)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Removes items based on the specified selection and direction.
		/// </summary>
		protected virtual int RemoveItems(SelectionHelper sel, bool forward, out int cellIndex)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Updates the environment.
		/// </summary>
		protected virtual int UpdateEnvironment(IPhEnvironment env)
		{
			throw new NotSupportedException();
		}

		protected virtual string FeatureChooserHelpTopic
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		protected virtual string RuleName
		{
			get
			{
				throw new NotSupportedException();
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
				{
					candidates.Add(env);
				}
			}

			var displayWs = "analysis vernacular";
			IPhEnvironment selectedEnv = null;
			var node = m_configurationNode?.Element("deParams");
			if (node != null)
			{
				displayWs = XmlUtils.GetOptionalAttributeValue(node, "ws", "analysis vernacular").ToLower();
			}

			var labels = ObjectLabel.CreateObjectLabels(m_cache, candidates.OrderBy(e => e.ShortName), null, displayWs);

			using (var chooser = new SimpleListChooser(m_persistProvider, labels, m_fieldName, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				chooser.Cache = m_cache;
				chooser.TextParamHvo = m_cache.LangProject.PhonologicalDataOA.Hvo;
				chooser.SetHelpTopic(Slice.GetChooserHelpTopicID(Slice.HelpTopicID));
				chooser.InitializeExtras(m_configurationNode, PropertyTable, Publisher, Subscriber);

				var res = chooser.ShowDialog();
				if (res != DialogResult.Cancel)
				{
					chooser.HandleAnyJump();

					if (chooser.ChosenOne != null)
					{
						selectedEnv = chooser.ChosenOne.Object as IPhEnvironment;
					}
				}
			}

			// return focus to the view
			_view.Select();
			if (selectedEnv == null)
			{
				return;
			}
			var cellId = -1;
			UndoableUnitOfWorkHelper.Do(AreaResources.ksRuleUndoUpdateEnv, AreaResources.ksRuleRedoUpdateEnv, selectedEnv, () =>
			{
				cellId = UpdateEnvironment(selectedEnv);
			});

			ReconstructView(cellId, -1, true);
		}

		/// <summary>
		/// Handles the Insert event of the m_insertionControl control.
		/// </summary>
		private void m_insertionControl_Insert(object sender, InsertEventArgs e)
		{
			var option = (InsertOption) e.Option;

			var undo = string.Format(LanguageExplorerResources.ksUndoInsert0, option);
			var redo = string.Format(LanguageExplorerResources.ksRedoInsert0, option);

			var sel = SelectionHelper.Create(_view);
			var cellId = -1;
			var cellIndex = -1;
			switch (option.Type)
			{
				case RuleInsertType.Phoneme:
					IEnumerable<IPhPhoneme> phonemes = m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.OrderBy(ph => ph.ShortName);
					var phonemeObj = DisplayChooser(AreaResources.ksRulePhonemeOpt, AreaResources.ksRulePhonemeChooserLink, AreaServices.PhonemeEditMachineName, "RulePhonemeFlatList", phonemes);
					var phoneme = phonemeObj as IPhPhoneme;
					if (phoneme == null)
					{
						return;
					}
					UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
						{
							cellId = InsertPhoneme(phoneme, sel, out cellIndex);
						});
					break;

				case RuleInsertType.NaturalClass:
					IEnumerable<IPhNaturalClass> natClasses = m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.OrderBy(natc => natc.ShortName);
					var ncObj = DisplayChooser(AreaResources.ksRuleNCOpt, AreaResources.ksRuleNCChooserLink, AreaServices.NaturalClassEditMachineName, "RuleNaturalClassFlatList", natClasses);
					var nc = ncObj as IPhNaturalClass;
					if (nc == null)
					{
						return;
					}
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
						var res = featChooser.ShowDialog();
						if (res == DialogResult.OK)
						{
							UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
								{
									var featNC = m_cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
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
					var wordBdry = m_cache.ServiceLocator.GetInstance<IPhBdryMarkerRepository>().GetObject(LangProjectTags.kguidPhRuleWordBdry);
					UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
						{
							cellId = InsertBdry(wordBdry, sel, out cellIndex);
						});
					break;

				case RuleInsertType.MorphemeBoundary:
					var morphBdry = m_cache.ServiceLocator.GetInstance<IPhBdryMarkerRepository>().GetObject(LangProjectTags.kguidPhRuleMorphBdry);
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

			_view.Select();
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
				var guidTextParam = m_cache.LangProject.PhonologicalDataOA.Guid;
				chooser.AddLink(linkText, LinkType.kGotoLink, new FwLinkArgs(toolName, guidTextParam));
				chooser.ReplaceTreeView(PropertyTable, Publisher, Subscriber, guiControl);
				chooser.SetHelpTopic(FeatureChooserHelpTopic);

				var res = chooser.ShowDialog();
				if (res == DialogResult.Cancel)
				{
					return null;
				}
				chooser.HandleAnyJump();

				if (chooser.ChosenOne != null)
				{
					obj = chooser.ChosenOne.Object;
				}
			}

			return obj;
		}

		protected int InsertContextInto(IPhSimpleContext ctxt, SelectionHelper sel, ILcmOwningSequence<IPhSimpleContext> seq)
		{
			var ctxts = seq.Cast<ICmObject>().ToArray();
			var index = GetInsertionIndex(ctxts, sel);
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

			var ctxts = seqCtxt.MembersRS.Cast<ICmObject>().ToArray();
			var index = GetInsertionIndex(ctxts, sel);
			seqCtxt.MembersRS.Insert(index, ctxt);
			// if the current selection is a range remove the items we are overwriting
			if (!sel.IsRange)
			{
				return index;
			}
			var indices = GetIndicesToRemove(ctxts, sel);
			foreach (var idx in indices)
			{
				var c = (IPhPhonContext) ctxts[idx];
				c.PreRemovalSideEffects();
				m_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(c);
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
			var ich = sel.GetIch(SelectionHelper.SelLimitType.Top);
			for (var i = 0; i < objs.Length; i++)
			{
				// if the current ich is 0, then we can safely assume we are at the beginning of
				// the current item, so insert before it, otherwise we are in the middle in which
				// case the entire item is selected or at the end, so we insert after
				if (objs[i] == curObj)
				{
					return ich == 0 ? i : i + 1;
				}
			}
			return objs.Length;
		}

		private void RemoveItemsRequested(object sender, RemoveItemsRequestedEventArgs e)
		{
			var sel = SelectionHelper.Create(_view);
			var cellId = -1;
			var cellIndex = -1;
			UndoableUnitOfWorkHelper.Do(AreaResources.ksRuleUndoRemove, AreaResources.ksRuleRedoRemove, m_cache.ActionHandlerAccessor, () =>
			{
				cellId = RemoveItems(sel, e.Forward, out cellIndex);
			});

			// if the no cell is returned, then do not reconstruct
			if (cellId != -1 && cellId != -2)
			{
				// if the cell index is -1 that means that we removed the first item in this cell,
				// so we move the cursor to the beginning of the first item after the removed items,
				// instead of the end of the item before the removed items.
				ReconstructView(cellId, cellIndex, cellIndex == -1);
			}
		}

		protected bool RemoveContextsFrom(bool forward, SelectionHelper sel, ILcmOwningSequence<IPhSimpleContext> seq, bool preRemovalSideEffects, out int index)
		{
			index = -1;
			var reconstruct = true;
			var ctxts = seq.Cast<ICmObject>().ToArray();
			// if the selection is a range remove all items in the selection
			if (sel.IsRange)
			{
				var indices = GetIndicesToRemove(ctxts, sel);
				// return index of the item before the removed items
				if (indices.Length > 0)
					index = indices[0] - 1;
				foreach (var idx in indices)
				{
					// Sometimes when deleting a range, DeleteUnderlyingObject() takes out
					// parts of the rule before this loop gets to it. [LT-9775]
					if (ctxts[idx].IsValidObject)
					{
						ProcessIndicesSimpleContext(seq, ctxts, preRemovalSideEffects, idx);
					}
				}
			}
			else
			{
				var idx = GetIndexToRemove(ctxts, sel, forward);
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

		private void ProcessIndicesSimpleContext(ILcmOwningSequence<IPhSimpleContext> seq, ICmObject[] ctxts, bool preRemovalSideEffects, int idx)
		{
			if (ctxts == null || idx > ctxts.Length - 1 || idx < 0)
			{
				return;
			}

			var c = (IPhSimpleContext) ctxts[idx];
			if (preRemovalSideEffects)
			{
				c.PreRemovalSideEffects();
			}
			seq.Remove(c);
		}

		protected bool RemoveContextsFrom(bool forward, SelectionHelper sel, IPhSequenceContext seqCtxt, bool preRemovalSideEffects, out int index)
		{
			index = -1;
			var reconstruct = true;
			var ctxts = seqCtxt.MembersRS.Cast<ICmObject>().ToArray();
			// if the selection is a range remove all items in the selection
			if (sel.IsRange)
			{
				var indices = GetIndicesToRemove(ctxts, sel);
				// return index of the item before the removed items
				if (indices.Length > 0)
				{
					index = indices[0] - 1;
				}

				foreach (var idx in indices)
				{
					// Sometimes when deleting a range, DeleteUnderlyingObject() takes out
					// parts of the rule before this loop gets to it. [LT-9775]
					if (ctxts[idx].IsValidObject)
					{
						ProcessIndicesSeqContext(ctxts, preRemovalSideEffects, idx);
					}
				}
			}
			else
			{
				var idx = GetIndexToRemove(ctxts, sel, forward);
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
			{
				return;
			}

			var c = (IPhPhonContext) ctxts[idx];
			if (preRemovalSideEffects)
			{
				c.PreRemovalSideEffects();
			}
			m_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(c);
		}

		protected int[] GetIndicesToRemove(ICmObject[] objs, SelectionHelper sel)
		{
			var beginObj = GetCmObject(sel, SelectionHelper.SelLimitType.Top);
			var endObj = GetCmObject(sel, SelectionHelper.SelLimitType.Bottom);

			var remove = new List<int>();
			var inRange = false;
			for (int i = 0; i < objs.Length; i++)
			{
				if (objs[i] == beginObj)
				{
					remove.Add(i);
					if (beginObj == endObj)
					{
						break;
					}
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
			for (var i = 0; i < objs.Length; i++)
			{
				if (objs[i] != obj)
				{
					continue;
				}
				var tss = sel.GetTss(SelectionHelper.SelLimitType.Anchor);
				// if the current ich is at the end of the current string, then we can safely assume
				// we are at the end of the current item, so remove it or the next item based on what
				// key was pressed, otherwise we are in the middle in which
				// case the entire item is selected, or at the beginning, so we remove it or the previous
				// item based on what key was pressed
				if (sel.IchAnchor == tss.Length)
				{
					if (forward)
					{
						return i == objs.Length ? -1 : i + 1;
					}
					return i;
				}

				if (forward)
				{
					return i == objs.Length ? -1 : i;
				}
				return i - 1;
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

			((IFlexComponent)_view).InitializeFlexComponent(flexComponentParameters);
		}

		/// <summary>
		/// Sets the phonological features for the currently selected natural class simple context with
		/// a feature-based natural class.
		/// </summary>
		public void SetContextFeatures()
		{
			var sel = SelectionHelper.Create(_view);
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
					{
						featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher, natClass, PhNCFeaturesTags.kflidFeatures);
					}
				}
				else
				{
					if (natClass.FeaturesOA != null)
					{
						featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher, natClass.FeaturesOA);
					}
					else
					{
						featChooser.SetDlgInfo(m_cache, PropertyTable, Publisher);
					}
				}
				// FWR-2405: Setting the Help topic requires that the Mediator be already set!
				featChooser.SetHelpTopic("khtpChoose-Grammar-PhonRules-SetPhonologicalFeatures");
				var res = featChooser.ShowDialog();
				if (res != DialogResult.Cancel)
				{
					featChooser.HandleJump();
				}
				reconstruct = res == DialogResult.OK;
			}

			_view.Select();
			if (reconstruct)
			{
				_view.RootBox.Reconstruct();
				sel.RestoreSelectionAndScrollPos();
			}
		}

		protected virtual void ContextMenuRequested(object sender, ContextMenuRequestedEventArgs e)
		{
			e.Selection.Install();
			// Use the local variable, since it does a lot of looking around for "CurrentObject".
			var obj = CurrentObject;
			if (obj == null)
			{
				// We only bother to display the context menu if there is a CurrentObject.
				return;
			}

			_rightClickTuple = CreateContextMenu();
			_rightClickTuple.Item1.Closed += ContextMenuStrip_Closed;

			// Show menu.
			_rightClickTuple.Item1.Show(new Point(Cursor.Position.X, Cursor.Position.Y));
		}

		private void ContextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			// Get rid of the menu.
			_rightClickTuple.Item1.Closed -= ContextMenuStrip_Closed;
			foreach (var tuple in _rightClickTuple.Item2)
			{
				tuple.Item1.Click -= tuple.Item2;
			}
			_rightClickTuple.Item1.Dispose();
			_rightClickTuple = null;
		}

		protected virtual Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateContextMenu()
		{
			throw new NotSupportedException();
		}

		private void SelectionChanged(object sender, EventArgs eventArgs)
		{
			// since the context has changed update the display options on the insertion control
			InsertionControl.UpdateOptionsDisplay();
		}

		/// <summary>
		/// Reconstructs the view and moves the cursor the specified position.
		/// </summary>
		/// <param name="cellId">The cell id.</param>
		/// <param name="cellIndex">Index of the item in the cell.</param>
		/// <param name="initial">if <c>true</c> move the cursor to the beginning of the specified item, otherwise it is moved to the end</param>
		protected void ReconstructView(int cellId, int cellIndex, bool initial)
		{
			_view.RootBox.Reconstruct();
			_view.SelectAt(cellId, cellIndex, initial, true, true);
		}

		internal static bool IsWordBoundary(IPhContextOrVar ctxt)
		{
			if (ctxt?.ClassID != PhSimpleContextBdryTags.kClassId)
			{
				return false;
			}
			return ((IPhSimpleContextBdry)ctxt).FeatureStructureRA.Guid == LangProjectTags.kguidPhRuleWordBdry;
		}

		internal static bool IsMorphBoundary(IPhContextOrVar ctxt)
		{
			if (ctxt?.ClassID != PhSimpleContextBdryTags.kClassId)
			{
				return false;
			}
			return ((IPhSimpleContextBdry)ctxt).FeatureStructureRA.Guid == LangProjectTags.kguidPhRuleMorphBdry;
		}

		#region Component Designer generated code

		private void InitializeComponent()
		{
			this._view = new LanguageExplorer.Controls.LexText.PatternView();
			this.InsertionControl = new InsertionControl();
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
			this._view.BackColor = System.Drawing.SystemColors.Window;
			this._view.Dock = System.Windows.Forms.DockStyle.Left;
			this._view.DoSpellCheck = false;
			this._view.Group = null;
			this._view.IsTextBox = false;
			this._view.Location = new System.Drawing.Point(0, 0);
			this._view.Name = "_view";
			this._view.ReadOnlyView = false;
			this._view.ScrollMinSize = new System.Drawing.Size(0, 0);
			this._view.ScrollPosition = new System.Drawing.Point(0, 0);
			this._view.ShowRangeSelAfterLostFocus = false;
			this._view.Size = new System.Drawing.Size(226, 20);
			this._view.SizeChangedSuppression = false;
			this._view.TabIndex = 3;
			this._view.WritingSystemFactory = null;
			this._view.WsPending = -1;
			this._view.Zoom = 1F;
			//
			// m_insertionControl
			//
			this.InsertionControl.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.InsertionControl.Location = new System.Drawing.Point(0, 20);
			this.InsertionControl.Name = "InsertionControl";
			this.InsertionControl.Size = new System.Drawing.Size(247, 23);
			this.InsertionControl.TabIndex = 2;
			//
			// RuleFormulaControl
			//
			this.Controls.Add(this._view);
			this.Controls.Add(this.InsertionControl);
			this.Name = "RuleFormulaControl";
			this.Size = new System.Drawing.Size(247, 43);
			this.Controls.SetChildIndex(this.InsertionControl, 0);
			this.Controls.SetChildIndex(this._view, 0);
			this.Controls.SetChildIndex(this.m_panel, 0);
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}