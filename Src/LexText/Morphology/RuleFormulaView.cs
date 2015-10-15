using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class represents a Views rootsite control that is used to display a rule
	/// formula. It notifies the rule formula control about key presses and right clicks.
	/// </summary>
	public class RuleFormulaView : RootSiteControl
	{
		RuleFormulaControl m_formulaControl = null;
		ICmObject m_obj = null;
		RuleFormulaVc m_vc = null;
		int m_rootFrag = -1;

		/// <summary>
		/// We MUST inherit from this, not from just EditingHelper; otherwise, the right event isn't
		/// connected (in an overide of OnEditingHelperCreated) for us to get selection change notifications.
		/// </summary>
		class RuleFormulaEditingHelper : RootSiteEditingHelper
		{
			public RuleFormulaEditingHelper(FdoCache cache, IEditingCallbacks callbacks)
				: base(cache, callbacks)
			{
			}

			public override bool CanCopy()
			{
				return false;
			}

			public override bool CanCut()
			{
				return false;
			}

			public override bool CanPaste()
			{
				return false;
			}
		}

		public RuleFormulaView()
		{
		}

		protected override EditingHelper CreateEditingHelper()
		{
			// we can't just use the Editable property to disable copy/cut/paste, because we want
			// the view to be read only, so instead we use a custom EditingHelper
			return new RuleFormulaEditingHelper(Cache, this);
		}

		#region IDisposable override
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_formulaControl = null;
			m_obj = null;
			m_vc = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		public void Init(Mediator mediator, PropertyTable propertyTable, ICmObject obj, RuleFormulaControl formulaControl,
			RuleFormulaVc vc, int rootFrag)
		{
			CheckDisposed();
			m_formulaControl = formulaControl;
			Mediator = mediator;
			m_propertyTable = propertyTable;
			Cache = m_propertyTable.GetValue<FdoCache>("cache");
			m_obj = obj;
			m_vc = vc;
			m_rootFrag = rootFrag;
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else if (m_obj != null)
			{
				m_rootb.SetRootObject(m_obj.Hvo, m_vc, m_rootFrag, FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable));
				m_rootb.Reconstruct();
			}
		}

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			// JohnT: this notification removal was introduced by Damien in change list 25875, along with removing
			// several IgnorePropChanged wrappers in RuleFormulaControl. I don't know why we ever wanted to not see
			// (some) PropChanged messages, but ignoring them all prevents us from removing inserted items from the
			// view in Undo. (see FWR-3501)
			//m_fdoCache.MainCacheAccessor.RemoveNotification(m_rootb);
			if (m_obj != null)
				m_rootb.SetRootObject(m_obj.Hvo, m_vc, m_rootFrag, FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable));
		}

		/// <summary>
		/// override this to allow deleting an item IF the key is Delete.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				m_formulaControl.RemoveItems(true);
			}
			else
			{
				e.Handled = true;
				base.OnKeyDown(e);
			}
		}

		/// <summary>
		/// override this to allow deleting an item IF the key is Backspace.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Back)
			{
				m_formulaControl.RemoveItems(false);
			}
			e.Handled = true;
			base.OnKeyPress(e);
		}

		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			m_formulaControl.UpdateSelection(prootb, vwselNew);
		}

		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y,
				new SIL.Utils.Rect(rcSrcRoot.Left, rcSrcRoot.Top, rcSrcRoot.Right, rcSrcRoot.Bottom),
				new SIL.Utils.Rect(rcDstRoot.Left, rcDstRoot.Top, rcDstRoot.Right, rcDstRoot.Bottom),
				true);
			if (sel == null)
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot); // no object, so quit and let base handle it

			if (m_formulaControl.DisplayContextMenu(sel))
				return true;
			else
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
		}
	}

	/// <summary>
	/// This view constructor is intended to be extended by particular rule formula view
	/// constructors. It handles the display of phonological contexts, such as <c>PhSequenceContext</c>,
	/// <c>PhIterationContext</c>, <c>PhSimpleContextNC</c>, <c>PhSimpleContextSeg</c>, <c>PhSimpleContextNC</c>, etc., for
	/// rule formulas.
	/// </summary>
	public abstract class RuleFormulaVc : FwBaseVc
	{
		public const int kfragEmpty = 0;
		public const int kfragContext = 1;
		public const int kfragFeatNC = 2;
		public const int kfragFeats = 3;
		public const int kfragFeature = 4;
		public const int kfragPlusVariable = 5;
		public const int kfragMinusVariable = 6;
		public const int kfragFeatureLine = 7;
		public const int kfragPlusVariableLine = 8;
		public const int kfragMinusVariableLine = 9;
		public const int kfragIterCtxtMax = 10;
		public const int kfragIterCtxtMin = 11;
		public const int kfragNC = 12;
		public const int kfragTerminalUnit = 13;

		// variant frags
		public const int kfragLeftBracketUpHook = 14;
		public const int kfragLeftBracketExt = 15;
		public const int kfragLeftBracketLowHook = 16;
		public const int kfragRightBracketUpHook = 17;
		public const int kfragRightBracketExt = 18;
		public const int kfragRightBracketLowHook = 19;
		public const int kfragLeftBracket = 20;
		public const int kfragRightBracket = 21;
		public const int kfragQuestions = 22;
		public const int kfragLeftParen = 23;
		public const int kfragRightParen = 24;
		public const int kfragXVariable = 25;
		public const int kfragZeroWidthSpace = 26;

		// fake flids
		public const int ktagFeature = -100;
		public const int ktagVariable = -101;
		public const int ktagLeftBoundary = -102;
		public const int ktagRightBoundary = -103;
		public const int ktagLeftNonBoundary = -104;
		public const int ktagRightNonBoundary = -105;
		public const int ktagXVariable = -106;
		public const int ktagInnerNonBoundary = -107;

		// spacing between contexts
		protected const int PILE_MARGIN = 1000;

		static string[] VARIABLE_NAMES = new string[] { "α", "β", "γ", "δ", "ε", "ζ", "η", "θ", "ι", "κ", "λ", "μ", "ν", "ξ",
														"ο", "π", "ρ", "σ", "τ", "υ", "φ", "χ", "ψ", "ω" };

		protected XCore.Mediator m_mediator;
		protected PropertyTable m_propertyTable;

		protected ITsTextProps m_bracketProps;
		protected ITsTextProps m_pileProps;

		protected ITsString m_empty;
		protected ITsString m_infinity;
		protected ITsString m_leftBracketUpHook;
		protected ITsString m_leftBracketExt;
		protected ITsString m_leftBracketLowHook;
		protected ITsString m_rightBracketUpHook;
		protected ITsString m_rightBracketExt;
		protected ITsString m_rightBracketLowHook;
		protected ITsString m_leftBracket;
		protected ITsString m_rightBracket;
		protected ITsString m_questions;
		protected ITsString m_leftParen;
		protected ITsString m_rightParen;
		protected ITsString m_x;
		protected ITsString m_zwSpace;

		protected RuleFormulaVc(Mediator mediator, PropertyTable propertyTable)
		{
			Cache = propertyTable.GetValue<FdoCache>("cache");
			m_mediator = mediator;
			m_propertyTable = propertyTable;

			// use Doulos SIL because it supports the special characters that are needed for
			// multiline brackets
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Charis SIL");
			m_bracketProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PILE_MARGIN);
			tpb.SetIntPropValues((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PILE_MARGIN);
			m_pileProps = tpb.GetTextProps();

			var tsf = m_cache.TsStrFactory;
			var userWs = m_cache.DefaultUserWs;
			m_empty = tsf.MakeString("", userWs);
			m_infinity = tsf.MakeString("\u221e", userWs);
			m_leftBracketUpHook = tsf.MakeString("\u23a1", userWs);
			m_leftBracketExt = tsf.MakeString("\u23a2", userWs);
			m_leftBracketLowHook = tsf.MakeString("\u23a3", userWs);
			m_rightBracketUpHook = tsf.MakeString("\u23a4", userWs);
			m_rightBracketExt = tsf.MakeString("\u23a5", userWs);
			m_rightBracketLowHook = tsf.MakeString("\u23a6", userWs);
			m_leftBracket = tsf.MakeString("[", userWs);
			m_rightBracket = tsf.MakeString("]", userWs);
			m_questions = tsf.MakeString("???", userWs);
			m_leftParen = tsf.MakeString("(", userWs);
			m_rightParen = tsf.MakeString(")", userWs);
			m_x = tsf.MakeString("X", userWs);
			m_zwSpace = tsf.MakeString("\u200b", userWs);
		}

		/// <summary>
		/// Gets the maximum number of lines for context cells.
		/// </summary>
		/// <value>The max number of lines.</value>
		protected abstract int MaxNumLines
		{
			get;
		}

		/// <summary>
		/// Gets the index of the specified feature constraint. This is used to ensure that the same
		/// variable is used for a feature constraint across the entire rule.
		/// </summary>
		/// <param name="var">The variable.</param>
		/// <returns></returns>
		protected abstract int GetVarIndex(IPhFeatureConstraint var);

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragContext:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(hvo);
					bool isOuterIterCtxt = false;
					// are we inside an iteration context? this is important since we only open a context pile if we are not
					// in an iteration context, since an iteration context does it for us
					if (vwenv.EmbeddingLevel > 0)
					{
						int outerHvo, outerTag, outerIndex;
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out outerHvo, out outerTag, out outerIndex);
						var outerObj = m_cache.ServiceLocator.GetObject(outerHvo);
						isOuterIterCtxt = outerObj.ClassID == PhIterationContextTags.kClassId;
					}

					switch (ctxtOrVar.ClassID)
					{
						case PhSequenceContextTags.kClassId:
							var seqCtxt = ctxtOrVar as IPhSequenceContext;
							if (seqCtxt.MembersRS.Count > 0)
							{
								vwenv.AddObjVecItems(PhSequenceContextTags.kflidMembers, this, kfragContext);
							}
							else
							{
								OpenContextPile(vwenv, false);
								vwenv.Props = m_bracketProps;
								vwenv.AddProp(PhSequenceContextTags.kflidMembers, this, kfragEmpty);
								CloseContextPile(vwenv, false);
							}
							break;

						case PhSimpleContextNCTags.kClassId:
							var ncCtxt = ctxtOrVar as IPhSimpleContextNC;
							if (ncCtxt.FeatureStructureRA != null && ncCtxt.FeatureStructureRA.ClassID == PhNCFeaturesTags.kClassId)
							{
								// Natural class simple context with a feature-based natural class
								var natClass = ncCtxt.FeatureStructureRA as IPhNCFeatures;

								int numLines = GetNumLines(ncCtxt);
								if (numLines == 0)
								{
									if (!isOuterIterCtxt)
										OpenContextPile(vwenv);

									vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftBracket);
									vwenv.AddProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragQuestions);
									vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracket);

									if (!isOuterIterCtxt)
										CloseContextPile(vwenv);
								}
								else if (numLines == 1)
								{
									if (!isOuterIterCtxt)
										OpenContextPile(vwenv);

									// use normal brackets for a single line context
									vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftBracket);

									// special consonant and vowel natural classes only display the abbreviation
									if (natClass.Abbreviation.AnalysisDefaultWritingSystem.Text == "C"
										|| natClass.Abbreviation.AnalysisDefaultWritingSystem.Text == "V")
									{
										vwenv.AddObjProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragNC);
									}
									else
									{
										if (natClass.FeaturesOA != null && natClass.FeaturesOA.FeatureSpecsOC.Count > 0)
											vwenv.AddObjProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragFeatNC);
										else if (ncCtxt.PlusConstrRS.Count > 0)
											vwenv.AddObjVecItems(PhSimpleContextNCTags.kflidPlusConstr, this, kfragPlusVariable);
										else
											vwenv.AddObjVecItems(PhSimpleContextNCTags.kflidMinusConstr, this, kfragMinusVariable);
									}
									vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracket);

									if (!isOuterIterCtxt)
										CloseContextPile(vwenv);
								}
								else
								{
									// multiline context

									// left bracket pile
									int maxNumLines = MaxNumLines;
									vwenv.Props = m_bracketProps;
									vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PILE_MARGIN);
									vwenv.OpenInnerPile();
									AddExtraLines(maxNumLines - numLines, ktagLeftNonBoundary, vwenv);
									vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketUpHook);
									for (int i = 1; i < numLines - 1; i++)
										vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketExt);
									vwenv.AddProp(ktagLeftBoundary, this, kfragLeftBracketLowHook);
									vwenv.CloseInnerPile();

									// feature and variable pile
									vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
									vwenv.OpenInnerPile();
									AddExtraLines(maxNumLines - numLines, vwenv);
									vwenv.AddObjProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragFeatNC);
									vwenv.AddObjVecItems(PhSimpleContextNCTags.kflidPlusConstr, this, kfragPlusVariable);
									vwenv.AddObjVecItems(PhSimpleContextNCTags.kflidMinusConstr, this, kfragMinusVariable);
									vwenv.CloseInnerPile();

									// right bracket pile
									vwenv.Props = m_bracketProps;
									if (!isOuterIterCtxt)
										vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PILE_MARGIN);
									vwenv.OpenInnerPile();
									AddExtraLines(maxNumLines - numLines, ktagRightNonBoundary, vwenv);
									vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracketUpHook);
									for (int i = 1; i < numLines - 1; i++)
										vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracketExt);
									vwenv.AddProp(ktagRightBoundary, this, kfragRightBracketLowHook);
									vwenv.CloseInnerPile();
								}
							}
							else
							{
								// natural class context with segment-based natural class
								if (!isOuterIterCtxt)
									OpenContextPile(vwenv);

								vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftBracket);
								if (ncCtxt.FeatureStructureRA != null)
									vwenv.AddObjProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragNC);
								else
									vwenv.AddProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragQuestions);
								vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracket);

								if (!isOuterIterCtxt)
									CloseContextPile(vwenv);
							}
							break;

						case PhIterationContextTags.kClassId:
							IPhIterationContext iterCtxt = ctxtOrVar as IPhIterationContext;
							if (iterCtxt.MemberRA != null)
							{
								int numLines = GetNumLines(iterCtxt.MemberRA as IPhSimpleContext);
								if (numLines > 1)
								{
									vwenv.AddObjProp(PhIterationContextTags.kflidMember, this, kfragContext);
									DisplayIterCtxt(iterCtxt, numLines, vwenv);
								}
								else
								{
									OpenContextPile(vwenv);
									if (iterCtxt.MemberRA.ClassID == PhSimpleContextNCTags.kClassId)
									{
										vwenv.AddObjProp(PhIterationContextTags.kflidMember, this, kfragContext);
									}
									else
									{
										vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftParen);
										vwenv.AddObjProp(PhIterationContextTags.kflidMember, this, kfragContext);
										vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightParen);
									}
									DisplayIterCtxt(iterCtxt, 1, vwenv);
									// Views doesn't handle selection properly when we have an inner pile with strings on either side,
									// so we don't add a zero-width space at the end
									CloseContextPile(vwenv, false);
								}
							}
							else
							{
								OpenContextPile(vwenv);
								vwenv.AddProp(PhIterationContextTags.kflidMember, this, kfragQuestions);
								CloseContextPile(vwenv);
							}
							break;

						case PhSimpleContextSegTags.kClassId:
							if (!isOuterIterCtxt)
								OpenContextPile(vwenv);

							var segCtxt = ctxtOrVar as IPhSimpleContextSeg;
							if (segCtxt.FeatureStructureRA != null)
								vwenv.AddObjProp(PhSimpleContextSegTags.kflidFeatureStructure, this, kfragTerminalUnit);
							else
								vwenv.AddProp(PhSimpleContextSegTags.kflidFeatureStructure, this, kfragQuestions);

							if (!isOuterIterCtxt)
								CloseContextPile(vwenv);
							break;

						case PhSimpleContextBdryTags.kClassId:
							if (!isOuterIterCtxt)
								OpenContextPile(vwenv);

							var bdryCtxt = ctxtOrVar as IPhSimpleContextBdry;
							if (bdryCtxt.FeatureStructureRA != null)
								vwenv.AddObjProp(PhSimpleContextBdryTags.kflidFeatureStructure, this, kfragTerminalUnit);
							else
								vwenv.AddProp(PhSimpleContextBdryTags.kflidFeatureStructure, this, kfragQuestions);

							if (!isOuterIterCtxt)
								CloseContextPile(vwenv);
							break;

						case PhVariableTags.kClassId:
							OpenContextPile(vwenv);
							vwenv.AddProp(ktagXVariable, this, kfragXVariable);
							CloseContextPile(vwenv);
							break;
					}
					break;

				case kfragNC:
					int ncWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsFirstAnal, hvo,
						PhNaturalClassTags.kflidAbbreviation);
					if (ncWs != 0)
					{
						vwenv.AddStringAltMember(PhNaturalClassTags.kflidAbbreviation, ncWs, this);
					}
					else
					{
						ncWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsFirstAnal, hvo,
							PhNaturalClassTags.kflidName);
						if (ncWs != 0)
							vwenv.AddStringAltMember(PhNaturalClassTags.kflidName, ncWs, this);
						else
							vwenv.AddProp(PhNaturalClassTags.kflidAbbreviation, this, kfragQuestions);
					}
					break;

				case kfragTerminalUnit:
					int tuWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsFirstVern,
						hvo, PhTerminalUnitTags.kflidName);
					if (tuWs != 0)
						vwenv.AddStringAltMember(PhTerminalUnitTags.kflidName, tuWs, this);
					else
						vwenv.AddProp(PhTerminalUnitTags.kflidName, this, kfragQuestions);
					break;

				case kfragFeatNC:
					vwenv.AddObjProp(PhNCFeaturesTags.kflidFeatures, this, kfragFeats);
					break;

				case kfragFeats:
					vwenv.AddObjVecItems(FsFeatStrucTags.kflidFeatureSpecs, this, kfragFeature);
					break;

				case kfragFeature:
					vwenv.AddProp(ktagFeature, this, kfragFeatureLine);
					break;

				case kfragPlusVariable:
					vwenv.AddProp(ktagVariable, this, kfragPlusVariableLine);
					break;

				case kfragMinusVariable:
					vwenv.AddProp(ktagVariable, this, kfragMinusVariableLine);
					break;
			}
		}

		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			// we use display variant to display literal strings that are editable
			ITsString tss = null;
			switch (frag)
			{
				case kfragEmpty:
					tss = m_empty;
					break;

				case kfragFeatureLine:
					var value = m_cache.ServiceLocator.GetInstance<IFsClosedValueRepository>().GetObject(vwenv.CurrentObject());
					tss = CreateFeatureLine(value);
					break;

				case kfragPlusVariableLine:
				case kfragMinusVariableLine:
					var var = m_cache.ServiceLocator.GetInstance<IPhFeatureConstraintRepository>().GetObject(vwenv.CurrentObject());
					tss = CreateVariableLine(var, frag == kfragPlusVariableLine);
					break;

				case kfragIterCtxtMax:
					// if the max value is -1, it indicates that it is infinite
					int i = m_cache.DomainDataByFlid.get_IntProp(vwenv.CurrentObject(), tag);
					if (i == -1)
						tss = m_infinity;
					else
						tss = m_cache.TsStrFactory.MakeString(Convert.ToString(i), m_cache.DefaultUserWs);
					break;

				case kfragLeftBracketUpHook:
					tss = m_leftBracketUpHook;
					break;

				case kfragLeftBracketExt:
					tss = m_leftBracketExt;
					break;

				case kfragLeftBracketLowHook:
					tss = m_leftBracketLowHook;
					break;

				case kfragRightBracketUpHook:
					tss = m_rightBracketUpHook;
					break;

				case kfragRightBracketExt:
					tss = m_rightBracketExt;
					break;

				case kfragRightBracketLowHook:
					tss = m_rightBracketLowHook;
					break;

				case kfragLeftBracket:
					tss = m_leftBracket;
					break;

				case kfragRightBracket:
					tss = m_rightBracket;
					break;

				case kfragQuestions:
					tss = m_questions;
					break;

				case kfragLeftParen:
					tss = m_leftParen;
					break;

				case kfragRightParen:
					tss = m_rightParen;
					break;

				case kfragXVariable:
					tss = m_x;
					break;

				case kfragZeroWidthSpace:
					tss = m_zwSpace;
					break;
			}
			return tss;
		}

		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag, ITsString tssVal)
		{
			return tssVal;
		}

		int GetIndex(int[] hvos, int hvo)
		{
			for (int i = 0; i < hvos.Length; i++)
			{
				if (hvos[i] == hvo)
					return i;
			}
			return -1;
		}

		ITsString CreateFeatureLine(IFsClosedValue value)
		{
			ITsIncStrBldr featLine = TsIncStrBldrClass.Create();
			if (value.ValueRA != null)
				featLine.AppendTsString(value.ValueRA.Abbreviation.BestAnalysisAlternative);
			else
				featLine.AppendTsString(m_questions);
			featLine.Append(" ");
			if (value.FeatureRA != null)
				featLine.AppendTsString(value.FeatureRA.Abbreviation.BestAnalysisAlternative);
			else
				featLine.AppendTsString(m_questions);
			return featLine.GetString();
		}

		ITsString CreateVariableLine(IPhFeatureConstraint var, bool polarity)
		{
			int varIndex = GetVarIndex(var);
			if (varIndex == -1)
				return m_questions;

			ITsIncStrBldr varLine = TsIncStrBldrClass.Create();
			if (!polarity)
				varLine.AppendTsString(m_cache.TsStrFactory.MakeString("-", m_cache.DefaultUserWs));
			varLine.AppendTsString(m_cache.TsStrFactory.MakeString(VARIABLE_NAMES[varIndex], m_cache.DefaultUserWs));
			varLine.Append(" ");
			varLine.AppendTsString(var.FeatureRA == null ? m_questions : var.FeatureRA.Abbreviation.BestAnalysisAlternative);
			return varLine.GetString();
		}

		protected void AddExtraLines(int numLines, IVwEnv vwenv)
		{
			AddExtraLines(numLines, ktagLeftNonBoundary, vwenv);
		}

		protected void AddExtraLines(int numLines, int tag, IVwEnv vwenv)
		{
			for (int i = 0; i < numLines; i++)
			{
				vwenv.Props = m_bracketProps;
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
				vwenv.OpenParagraph();
				vwenv.AddProp(tag, this, kfragEmpty);
				vwenv.CloseParagraph();
			}
		}

		void DisplayIterCtxt(IPhIterationContext iterCtxt, int numLines, IVwEnv vwenv)
		{
			int superOffset = 0;
			if (numLines == 1)
			{
				// if the inner context is a single line, then make the min value a subscript and the max value a superscript.
				// I tried to use the Views subscript and superscript properties, but they added extra space so that it would
				// have the same line height of a normal character, which is not what I wanted, so I compute the size myself
				int fontHeight = GetFontHeight(m_cache.DefaultUserWs);
				int superSubHeight = (fontHeight * 2) / 3;
				vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, superSubHeight);
				vwenv.set_IntProperty((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, -superSubHeight);
				superOffset = superSubHeight / 2;
			}
			else
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PILE_MARGIN);
			}
			vwenv.OpenInnerPile();
			if (numLines == 1)
				vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, superOffset);
			vwenv.OpenParagraph();
			vwenv.AddProp(PhIterationContextTags.kflidMaximum, this, kfragIterCtxtMax);
			vwenv.CloseParagraph();
			AddExtraLines(numLines - 2, vwenv);
			vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, 0);
			vwenv.OpenParagraph();
			vwenv.AddIntProp(PhIterationContextTags.kflidMinimum);
			vwenv.CloseParagraph();
			vwenv.CloseInnerPile();
		}

		protected void OpenContextPile(IVwEnv vwenv)
		{
			OpenContextPile(vwenv, true);
		}

		protected void OpenContextPile(IVwEnv vwenv, bool addBoundary)
		{
			vwenv.Props = m_pileProps;
			vwenv.OpenInnerPile();
			AddExtraLines(MaxNumLines - 1, vwenv);
			vwenv.OpenParagraph();
			if (addBoundary)
			{
				vwenv.Props = m_bracketProps;
				vwenv.AddProp(ktagLeftBoundary, this, kfragZeroWidthSpace);
			}
		}

		protected void CloseContextPile(IVwEnv vwenv)
		{
			CloseContextPile(vwenv, true);
		}

		protected void CloseContextPile(IVwEnv vwenv, bool addBoundary)
		{
			if (addBoundary)
			{
				vwenv.Props = m_bracketProps;
				vwenv.AddProp(ktagRightBoundary, this, kfragZeroWidthSpace);
			}
			vwenv.CloseParagraph();
			vwenv.CloseInnerPile();
		}

		/// <summary>
		/// Gets the maximum number of lines to display the specified sequence of simple contexts.
		/// </summary>
		/// <param name="seq">The sequence.</param>
		/// <returns></returns>
		protected int GetNumLines(IEnumerable<IPhSimpleContext> seq)
		{
			int maxNumLines = 1;
			foreach (IPhSimpleContext ctxt in seq)
			{
				int numLines = GetNumLines(ctxt);
				if (numLines > maxNumLines)
					maxNumLines = numLines;
			}
			return maxNumLines;
		}

		/// <summary>
		/// Gets the number of lines needed to display the specified context or variable.
		/// </summary>
		/// <param name="ctxtOrVar">The context or variable.</param>
		/// <returns></returns>
		protected int GetNumLines(IPhContextOrVar ctxtOrVar)
		{
			if (ctxtOrVar == null)
				return 1;

			switch (ctxtOrVar.ClassID)
			{
				case PhSequenceContextTags.kClassId:
					IPhSequenceContext seqCtxt = ctxtOrVar as IPhSequenceContext;
					int maxNumLines = 1;
					foreach (IPhPhonContext cur in seqCtxt.MembersRS)
					{
						int numLines = GetNumLines(cur);
						if (numLines > maxNumLines)
							maxNumLines = numLines;
					}
					return maxNumLines;

				case PhIterationContextTags.kClassId:
					IPhIterationContext iterCtxt = ctxtOrVar as IPhIterationContext;
					return GetNumLines(iterCtxt.MemberRA);

				case PhSimpleContextNCTags.kClassId:
					int numFeats = 0;
					IPhSimpleContextNC ncCtxt = ctxtOrVar as IPhSimpleContextNC;
					if (ncCtxt.FeatureStructureRA != null && ncCtxt.FeatureStructureRA.ClassID == PhNCFeaturesTags.kClassId)
					{
						IPhNCFeatures natClass = ncCtxt.FeatureStructureRA as IPhNCFeatures;
						if (natClass.FeaturesOA != null)
							numFeats = natClass.FeaturesOA.FeatureSpecsOC.Count;
					}
					return ncCtxt.PlusConstrRS.Count + ncCtxt.MinusConstrRS.Count + numFeats;
			}
			return 1;
		}

		/// <summary>
		/// Gets the font height of the specified writing system for the normal style.
		/// </summary>
		/// <param name="ws">The ws.</param>
		/// <returns></returns>
		int GetFontHeight(int ws)
		{
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			return FontHeightAdjuster.GetFontHeightForStyle("Normal", stylesheet,
				ws, m_cache.LanguageWritingSystemFactoryAccessor);
		}

		/// <summary>
		/// Gets the width of the specified context or variable.
		/// </summary>
		/// <param name="ctxtOrVar">The context or variable.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// <returns></returns>
		protected int GetWidth(IPhContextOrVar ctxtOrVar, IVwEnv vwenv)
		{
			if (ctxtOrVar == null)
				return 0;

			switch (ctxtOrVar.ClassID)
			{
				case PhSequenceContextTags.kClassId:
					var seqCtxt = ctxtOrVar as IPhSequenceContext;
					int totalLen = 0;
					foreach (IPhPhonContext cur in seqCtxt.MembersRS)
						totalLen += GetWidth(cur, vwenv);
					return totalLen;

				case PhIterationContextTags.kClassId:
					return GetIterCtxtWidth(ctxtOrVar as IPhIterationContext, vwenv) + (PILE_MARGIN * 2);

				case PhVariableTags.kClassId:
					return GetStrWidth(m_x, null, vwenv) + (PILE_MARGIN * 2);

				default:
					return GetSimpleCtxtWidth(ctxtOrVar as IPhSimpleContext, vwenv) + (PILE_MARGIN * 2);
			}
		}

		int GetIterCtxtWidth(IPhIterationContext ctxt, IVwEnv vwenv)
		{
			if (ctxt.MemberRA != null)
			{
				int len = GetSimpleCtxtWidth(ctxt.MemberRA as IPhSimpleContext, vwenv);
				int numLines = GetNumLines(ctxt.MemberRA);
				if (numLines > 1)
				{
					len += GetMinMaxWidth(ctxt, null, vwenv);
				}
				else
				{
					if (ctxt.MemberRA.ClassID != PhSimpleContextNCTags.kClassId)
					{
						len += GetStrWidth(m_leftParen, null, vwenv);
						len += GetStrWidth(m_rightParen, null, vwenv);
					}
					int fontHeight = GetFontHeight(m_cache.DefaultUserWs);
					int superSubHeight = (fontHeight * 2) / 3;
					ITsPropsBldr tpb = TsPropsBldrClass.Create();
					tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, superSubHeight);
					len += GetMinMaxWidth(ctxt, tpb.GetTextProps(), vwenv);
				}
				return len;
			}
			else
			{
				return GetStrWidth(m_questions, null, vwenv);
			}
		}

		int GetMinMaxWidth(IPhIterationContext ctxt, ITsTextProps props, IVwEnv vwenv)
		{
			var tsf = m_cache.TsStrFactory;
			var userWs = m_cache.DefaultUserWs;
			int minWidth = GetStrWidth(tsf.MakeString(Convert.ToString(ctxt.Minimum), userWs),
				props, vwenv);
			ITsString maxStr = ctxt.Maximum == -1 ? m_infinity : tsf.MakeString(Convert.ToString(ctxt.Maximum), userWs);
			int maxWidth = GetStrWidth(maxStr, props, vwenv);
			return Math.Max(minWidth, maxWidth);
		}

		int GetSimpleCtxtWidth(IPhSimpleContext ctxt, IVwEnv vwenv)
		{
			if (ctxt == null)
				return 0;

			switch (ctxt.ClassID)
			{
				case PhSimpleContextBdryTags.kClassId:
					var bdryCtxt = ctxt as IPhSimpleContextBdry;
					return GetTermUnitWidth(bdryCtxt.FeatureStructureRA, vwenv);

				case PhSimpleContextSegTags.kClassId:
					var segCtxt = ctxt as IPhSimpleContextSeg;
					return GetTermUnitWidth(segCtxt.FeatureStructureRA, vwenv);

				case PhSimpleContextNCTags.kClassId:
					return GetNCCtxtWidth(ctxt as IPhSimpleContextNC, vwenv);
			}
			return 0;
		}

		int GetTermUnitWidth(IPhTerminalUnit tu, IVwEnv vwenv)
		{
			if (tu == null)
				return GetStrWidth(m_questions, null, vwenv);
			else
				return GetStrWidth(tu.Name.BestVernacularAlternative, null, vwenv);
		}

		int GetNCCtxtWidth(IPhSimpleContextNC ctxt, IVwEnv vwenv)
		{
			if (ctxt.FeatureStructureRA != null && ctxt.FeatureStructureRA.ClassID == PhNCFeaturesTags.kClassId)
			{
				int numLines = GetNumLines(ctxt);
				if (numLines == 1)
				{
					int len = 0;
					if (ctxt.FeatureStructureRA.Abbreviation.UserDefaultWritingSystem.Text == "C"
						|| ctxt.FeatureStructureRA.Abbreviation.UserDefaultWritingSystem.Text == "V")
					{
						len = GetStrWidth(ctxt.FeatureStructureRA.Abbreviation.BestAnalysisAlternative, null, vwenv);
					}
					else
					{
						len = GetNCFeatsWidth(ctxt, vwenv);
					}
					len += GetStrWidth(m_leftBracket, null, vwenv);
					len += GetStrWidth(m_rightBracket, null, vwenv);
					return len;
				}
				else
				{
					int len = GetNCFeatsWidth(ctxt, vwenv);
					len += GetStrWidth(m_leftBracketUpHook, m_bracketProps, vwenv);
					len += GetStrWidth(m_rightBracketUpHook, m_bracketProps, vwenv);
					return len;
				}
			}
			else
			{
				int len = 0;
				if (ctxt.FeatureStructureRA == null)
					len = GetStrWidth(m_questions, null, vwenv);
				else
					len = GetStrWidth(ctxt.FeatureStructureRA.Abbreviation.BestAnalysisAlternative, null, vwenv);
				len += GetStrWidth(m_leftBracket, null, vwenv);
				len += GetStrWidth(m_rightBracket, null, vwenv);
				return len;
			}
		}

		int GetNCFeatsWidth(IPhSimpleContextNC ctxt, IVwEnv vwenv)
		{
			int maxLen = 0;
			IPhNCFeatures natClass = ctxt.FeatureStructureRA as IPhNCFeatures;
			if (natClass != null && natClass.FeaturesOA != null)
			{
				foreach (IFsFeatureSpecification spec in natClass.FeaturesOA.FeatureSpecsOC)
				{
					IFsClosedValue curVal = spec as IFsClosedValue;
					ITsString featLine = CreateFeatureLine(curVal);
					int len = GetStrWidth(featLine, null, vwenv);
					if (len > maxLen)
						maxLen = len;
				}
			}

			int plusLen = GetVariablesWidth(ctxt, vwenv, true);
			if (plusLen > maxLen)
				maxLen = plusLen;

			int minusLen = GetVariablesWidth(ctxt, vwenv, false);
			if (minusLen > maxLen)
				maxLen = minusLen;
			return maxLen;
		}

		int GetVariablesWidth(IPhSimpleContextNC ctxt, IVwEnv vwenv, bool polarity)
		{
			var vars = polarity ? ctxt.PlusConstrRS : ctxt.MinusConstrRS;
			int maxLen = 0;
			for (int i = 0; i < vars.Count; i++)
			{
				ITsString varLine = CreateVariableLine(vars[i], polarity);
				int len = GetStrWidth(varLine, null, vwenv);
				if (len > maxLen)
					maxLen = len;
			}
			return maxLen;
		}

		protected int GetStrWidth(ITsString tss, ITsTextProps props, IVwEnv vwenv)
		{
			int dmpx, dmpy;
			vwenv.get_StringWidth(tss, props, out dmpx, out dmpy);
			return dmpx;
		}
	}
}
