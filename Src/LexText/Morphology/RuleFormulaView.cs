using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;

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

		class RuleFormulaEditingHelper : EditingHelper
		{
			public RuleFormulaEditingHelper(IEditingCallbacks callbacks)
				: base(callbacks)
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
			// we can't just use the Editable property to disable copy/cut/paste, because we want
			// the view to be read only, so instead we use a custom EditingHelper
			m_editingHelper = new RuleFormulaEditingHelper(this);
		}

		#region IDisposable override
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_vc != null)
					m_vc.Dispose();
			}

			m_formulaControl = null;
			m_obj = null;
			m_vc = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		public void Init(XCore.Mediator mediator, ICmObject obj, RuleFormulaControl formulaControl,
			RuleFormulaVc vc, int rootFrag)
		{
			CheckDisposed();
			m_formulaControl = formulaControl;
			Mediator = mediator;
			Cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			m_obj = obj;
			m_vc = vc;
			m_rootFrag = rootFrag;
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else if (m_obj != null)
			{
				m_rootb.SetRootObject(m_obj.Hvo, m_vc, m_rootFrag, FontHeightAdjuster.StyleSheetFromMediator(m_mediator));
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
			m_fdoCache.MainCacheAccessor.RemoveNotification(m_rootb);
			if (m_obj != null)
				m_rootb.SetRootObject(m_obj.Hvo, m_vc, m_rootFrag, FontHeightAdjuster.StyleSheetFromMediator(m_mediator));
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

		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			m_formulaControl.UpdateSelection(prootb, vwselNew);
		}

		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y,
				new SIL.FieldWorks.Common.Utils.Rect(rcSrcRoot.Left, rcSrcRoot.Top, rcSrcRoot.Right, rcSrcRoot.Bottom),
				new SIL.FieldWorks.Common.Utils.Rect(rcDstRoot.Left, rcDstRoot.Top, rcDstRoot.Right, rcDstRoot.Bottom),
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
	public abstract class RuleFormulaVc : VwBaseVc
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

		protected FdoCache m_cache;
		protected XCore.Mediator m_mediator;

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

		public RuleFormulaVc(FdoCache cache, XCore.Mediator mediator)
		{
			m_cache = cache;
			m_mediator = mediator;

			// use Doulos SIL because it supports the special characters that are needed for
			// multiline brackets
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Charis SIL");
			m_bracketProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PILE_MARGIN);
			tpb.SetIntPropValues((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PILE_MARGIN);
			m_pileProps = tpb.GetTextProps();

			m_empty = m_cache.MakeUserTss("");
			m_infinity = m_cache.MakeUserTss("\u221e");
			m_leftBracketUpHook = m_cache.MakeUserTss("\u23a1");
			m_leftBracketExt = m_cache.MakeUserTss("\u23a2");
			m_leftBracketLowHook = m_cache.MakeUserTss("\u23a3");
			m_rightBracketUpHook = m_cache.MakeUserTss("\u23a4");
			m_rightBracketExt = m_cache.MakeUserTss("\u23a5");
			m_rightBracketLowHook = m_cache.MakeUserTss("\u23a6");
			m_leftBracket = m_cache.MakeUserTss("[");
			m_rightBracket = m_cache.MakeUserTss("]");
			m_questions = m_cache.MakeUserTss("???");
			m_leftParen = m_cache.MakeUserTss("(");
			m_rightParen = m_cache.MakeUserTss(")");
			m_x = m_cache.MakeUserTss("X");
			m_zwSpace = m_cache.MakeUserTss("\u200b");
		}

		#region IDisposable override
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_cache = null;
			m_mediator = null;

			if (m_bracketProps != null)
			{
				Marshal.ReleaseComObject(m_bracketProps);
				m_bracketProps = null;
			}
			if (m_empty != null)
			{
				Marshal.ReleaseComObject(m_empty);
				m_empty = null;
			}
			if (m_infinity != null)
			{
				Marshal.ReleaseComObject(m_infinity);
				m_infinity = null;
			}
			if (m_leftBracketUpHook != null)
			{
				Marshal.ReleaseComObject(m_leftBracketUpHook);
				m_leftBracketUpHook = null;
			}
			if (m_leftBracketExt != null)
			{
				Marshal.ReleaseComObject(m_leftBracketExt);
				m_leftBracketExt = null;
			}
			if (m_leftBracketLowHook != null)
			{
				Marshal.ReleaseComObject(m_leftBracketLowHook);
				m_leftBracketLowHook = null;
			}
			if (m_rightBracketUpHook != null)
			{
				Marshal.ReleaseComObject(m_rightBracketUpHook);
				m_rightBracketUpHook = null;
			}
			if (m_rightBracketExt != null)
			{
				Marshal.ReleaseComObject(m_rightBracketExt);
				m_rightBracketExt = null;
			}
			if (m_rightBracketLowHook != null)
			{
				Marshal.ReleaseComObject(m_rightBracketLowHook);
				m_rightBracketLowHook = null;
			}
			if (m_leftBracket != null)
			{
				Marshal.ReleaseComObject(m_leftBracket);
				m_leftBracket = null;
			}
			if (m_rightBracket != null)
			{
				Marshal.ReleaseComObject(m_rightBracket);
				m_rightBracket = null;
			}
			if (m_questions != null)
			{
				Marshal.ReleaseComObject(m_questions);
				m_questions = null;
			}
			if (m_leftParen != null)
			{
				Marshal.ReleaseComObject(m_leftParen);
				m_leftParen = null;
			}
			if (m_rightParen != null)
			{
				Marshal.ReleaseComObject(m_rightParen);
				m_rightParen = null;
			}
			if (m_x != null)
			{
				Marshal.ReleaseComObject(m_x);
				m_x = null;
			}
			if (m_zwSpace != null)
			{
				Marshal.ReleaseComObject(m_zwSpace);
				m_zwSpace = null;
			}

			base.Dispose(disposing);
		}
		#endregion IDisposable override

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
			CheckDisposed();

			switch (frag)
			{
				case kfragContext:
					IPhContextOrVar ctxtOrVar = PhContextOrVar.CreateFromDBObject(m_cache, hvo);
					bool isOuterIterCtxt = false;
					// are we inside an iteration context? this is important since we only open a context pile if we are not
					// in an iteration context, since an iteration context does it for us
					if (vwenv.EmbeddingLevel > 0)
					{
						int outerHvo, outerTag, outerIndex;
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out outerHvo, out outerTag, out outerIndex);
						isOuterIterCtxt = m_cache.GetClassOfObject(outerHvo) == PhIterationContext.kclsidPhIterationContext;
					}

					switch (ctxtOrVar.ClassID)
					{
						case PhSequenceContext.kclsidPhSequenceContext:
							if (m_cache.GetVectorSize(hvo, (int)PhSequenceContext.PhSequenceContextTags.kflidMembers) > 0)
							{
								vwenv.AddObjVecItems((int)PhSequenceContext.PhSequenceContextTags.kflidMembers, this, kfragContext);
							}
							else
							{
								OpenContextPile(vwenv, false);
								vwenv.Props = m_bracketProps;
								vwenv.AddProp((int)PhSequenceContext.PhSequenceContextTags.kflidMembers, this, kfragEmpty);
								CloseContextPile(vwenv, false);
							}
							break;

						case PhSimpleContextNC.kclsidPhSimpleContextNC:
							IPhSimpleContextNC ncCtxt = ctxtOrVar as IPhSimpleContextNC;
							if (ncCtxt.FeatureStructureRAHvo != 0 && ncCtxt.FeatureStructureRA.ClassID == PhNCFeatures.kclsidPhNCFeatures)
							{
								// Natural class simple context with a feature-based natural class
								IPhNCFeatures natClass = ncCtxt.FeatureStructureRA as IPhNCFeatures;

								int numLines = GetNumLines(ncCtxt);
								if (numLines == 0)
								{
									if (!isOuterIterCtxt)
										OpenContextPile(vwenv);

									vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftBracket);
									vwenv.AddProp((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidFeatureStructure, this, kfragQuestions);
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
									if (natClass.Abbreviation.AnalysisDefaultWritingSystem == "C"
										|| natClass.Abbreviation.AnalysisDefaultWritingSystem == "V")
									{
										vwenv.AddObjProp((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidFeatureStructure, this, kfragNC);
									}
									else
									{
										if (natClass.FeaturesOAHvo != 0 && natClass.FeaturesOA.FeatureSpecsOC.Count > 0)
											vwenv.AddObjProp((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidFeatureStructure, this, kfragFeatNC);
										else if (ncCtxt.PlusConstrRS.Count > 0)
											vwenv.AddObjVecItems((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidPlusConstr, this, kfragPlusVariable);
										else
											vwenv.AddObjVecItems((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidMinusConstr, this, kfragMinusVariable);
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
									vwenv.AddObjProp((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidFeatureStructure, this, kfragFeatNC);
									vwenv.AddObjVecItems((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidPlusConstr, this, kfragPlusVariable);
									vwenv.AddObjVecItems((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidMinusConstr, this, kfragMinusVariable);
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
								if (ncCtxt.FeatureStructureRAHvo != 0)
									vwenv.AddObjProp((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidFeatureStructure, this, kfragNC);
								else
									vwenv.AddProp((int)PhSimpleContextNC.PhSimpleContextNCTags.kflidFeatureStructure, this, kfragQuestions);
								vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracket);

								if (!isOuterIterCtxt)
									CloseContextPile(vwenv);
							}
							break;

						case PhIterationContext.kclsidPhIterationContext:
							IPhIterationContext iterCtxt = ctxtOrVar as IPhIterationContext;
							if (iterCtxt.MemberRAHvo != 0)
							{
								int numLines = GetNumLines(iterCtxt.MemberRA as IPhSimpleContext);
								if (numLines > 1)
								{
									vwenv.AddObjProp((int)PhIterationContext.PhIterationContextTags.kflidMember, this, kfragContext);
									DisplayIterCtxt(iterCtxt, numLines, vwenv);
								}
								else
								{
									OpenContextPile(vwenv);
									if (iterCtxt.MemberRA.ClassID == PhSimpleContextNC.kclsidPhSimpleContextNC)
									{
										vwenv.AddObjProp((int)PhIterationContext.PhIterationContextTags.kflidMember, this, kfragContext);
									}
									else
									{
										vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftParen);
										vwenv.AddObjProp((int)PhIterationContext.PhIterationContextTags.kflidMember, this, kfragContext);
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
								vwenv.AddProp((int)PhIterationContext.PhIterationContextTags.kflidMember, this, kfragQuestions);
								CloseContextPile(vwenv);
							}
							break;

						case PhSimpleContextSeg.kclsidPhSimpleContextSeg:
							if (!isOuterIterCtxt)
								OpenContextPile(vwenv);

							if (m_cache.GetObjProperty(hvo, (int)PhSimpleContextSeg.PhSimpleContextSegTags.kflidFeatureStructure) != 0)
								vwenv.AddObjProp((int)PhSimpleContextSeg.PhSimpleContextSegTags.kflidFeatureStructure, this, kfragTerminalUnit);
							else
								vwenv.AddProp((int)PhSimpleContextSeg.PhSimpleContextSegTags.kflidFeatureStructure, this, kfragQuestions);

							if (!isOuterIterCtxt)
								CloseContextPile(vwenv);
							break;

						case PhSimpleContextBdry.kclsidPhSimpleContextBdry:
							if (!isOuterIterCtxt)
								OpenContextPile(vwenv);

							if (m_cache.GetObjProperty(hvo, (int)PhSimpleContextBdry.PhSimpleContextBdryTags.kflidFeatureStructure) != 0)
								vwenv.AddObjProp((int)PhSimpleContextBdry.PhSimpleContextBdryTags.kflidFeatureStructure, this, kfragTerminalUnit);
							else
								vwenv.AddProp((int)PhSimpleContextBdry.PhSimpleContextBdryTags.kflidFeatureStructure, this, kfragQuestions);

							if (!isOuterIterCtxt)
								CloseContextPile(vwenv);
							break;

						case PhVariable.kclsidPhVariable:
							OpenContextPile(vwenv);
							vwenv.AddProp(ktagXVariable, this, kfragXVariable);
							CloseContextPile(vwenv);
							break;
					}
					break;

				case kfragNC:
					int ncWs = m_cache.LangProject.ActualWs(LangProject.kwsFirstAnal, hvo, (int)PhNaturalClass.PhNaturalClassTags.kflidAbbreviation);
					if (ncWs != 0)
					{
						vwenv.AddStringAltMember((int)PhNaturalClass.PhNaturalClassTags.kflidAbbreviation, ncWs, this);
					}
					else
					{
						ncWs = m_cache.LangProject.ActualWs(LangProject.kwsFirstAnal, hvo, (int)PhNaturalClass.PhNaturalClassTags.kflidName);
						if (ncWs != 0)
							vwenv.AddStringAltMember((int)PhNaturalClass.PhNaturalClassTags.kflidName, ncWs, this);
						else
							vwenv.AddProp((int)PhNaturalClass.PhNaturalClassTags.kflidAbbreviation, this, kfragQuestions);
					}
					break;

				case kfragTerminalUnit:
					int tuWs = m_cache.LangProject.ActualWs(LangProject.kwsFirstVern, hvo, (int)PhTerminalUnit.PhTerminalUnitTags.kflidName);
					if (tuWs != 0)
						vwenv.AddStringAltMember((int)PhTerminalUnit.PhTerminalUnitTags.kflidName, tuWs, this);
					else
						vwenv.AddProp((int)PhTerminalUnit.PhTerminalUnitTags.kflidName, this, kfragQuestions);
					break;

				case kfragFeatNC:
					vwenv.AddObjProp((int)PhNCFeatures.PhNCFeaturesTags.kflidFeatures, this, kfragFeats);
					break;

				case kfragFeats:
					vwenv.AddObjVecItems((int)FsFeatStruc.FsFeatStrucTags.kflidFeatureSpecs, this, kfragFeature);
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

		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, object v, int frag)
		{
			CheckDisposed();

			// we use display variant to display literal strings that are editable
			ITsString tss = null;
			switch (frag)
			{
				case kfragEmpty:
					tss = m_empty;
					break;

				case kfragFeatureLine:
					IFsClosedValue value = new FsClosedValue(m_cache, vwenv.CurrentObject());
					tss = CreateFeatureLine(value);
					break;

				case kfragPlusVariableLine:
				case kfragMinusVariableLine:
					IPhFeatureConstraint var = new PhFeatureConstraint(m_cache, vwenv.CurrentObject());
					tss = CreateVariableLine(var, frag == kfragPlusVariableLine);
					break;

				case kfragIterCtxtMax:
					// if the max value is -1, it indicates that it is infinite
					int i = (int)v;
					if (i == -1)
						tss = m_infinity;
					else
						tss = m_cache.MakeUserTss(Convert.ToString(i));
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
			if (value.ValueRAHvo != 0)
				featLine.AppendTsString(value.ValueRA.Abbreviation.BestAnalysisAlternative);
			else
				featLine.AppendTsString(m_questions);
			featLine.Append(" ");
			if (value.FeatureRAHvo != 0)
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
				varLine.Append("-");
			varLine.AppendTsString(m_cache.MakeUserTss(VARIABLE_NAMES[varIndex]));
			varLine.Append(" ");
			varLine.AppendTsString(var.FeatureRAHvo == 0 ? m_questions : var.FeatureRA.Abbreviation.BestAnalysisAlternative);
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
			vwenv.AddProp((int)PhIterationContext.PhIterationContextTags.kflidMaximum, this, kfragIterCtxtMax);
			vwenv.CloseParagraph();
			AddExtraLines(numLines - 2, vwenv);
			vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, 0);
			vwenv.OpenParagraph();
			vwenv.AddIntProp((int)PhIterationContext.PhIterationContextTags.kflidMinimum);
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
		protected int GetNumLines(FdoSequence<IPhSimpleContext> seq)
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
				case PhSequenceContext.kclsidPhSequenceContext:
					IPhSequenceContext seqCtxt = ctxtOrVar as IPhSequenceContext;
					int maxNumLines = 1;
					foreach (IPhPhonContext cur in seqCtxt.MembersRS)
					{
						int numLines = GetNumLines(cur);
						if (numLines > maxNumLines)
							maxNumLines = numLines;
					}
					return maxNumLines;

				case PhIterationContext.kclsidPhIterationContext:
					IPhIterationContext iterCtxt = ctxtOrVar as IPhIterationContext;
					return GetNumLines(iterCtxt.MemberRA);

				case PhSimpleContextNC.kclsidPhSimpleContextNC:
					int numFeats = 0;
					IPhSimpleContextNC ncCtxt = ctxtOrVar as IPhSimpleContextNC;
					if (ncCtxt.FeatureStructureRAHvo != 0 && ncCtxt.FeatureStructureRA.ClassID == PhNCFeatures.kclsidPhNCFeatures)
					{
						IPhNCFeatures natClass = ncCtxt.FeatureStructureRA as IPhNCFeatures;
						if (natClass.FeaturesOAHvo != 0)
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
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
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
				case PhSequenceContext.kclsidPhSequenceContext:
					IPhSequenceContext seqCtxt = ctxtOrVar as IPhSequenceContext;
					int totalLen = 0;
					foreach (IPhPhonContext cur in seqCtxt.MembersRS)
						totalLen += GetWidth(cur, vwenv);
					return totalLen;

				case PhIterationContext.kclsidPhIterationContext:
					return GetIterCtxtWidth(ctxtOrVar as IPhIterationContext, vwenv) + (PILE_MARGIN * 2);

				case PhVariable.kclsidPhVariable:
					return GetStrWidth(m_x, null, vwenv) + (PILE_MARGIN * 2);

				default:
					return GetSimpleCtxtWidth(ctxtOrVar as IPhSimpleContext, vwenv) + (PILE_MARGIN * 2);
			}
		}

		int GetIterCtxtWidth(IPhIterationContext ctxt, IVwEnv vwenv)
		{
			if (ctxt.MemberRAHvo != 0)
			{
				int len = GetSimpleCtxtWidth(ctxt.MemberRA as IPhSimpleContext, vwenv);
				int numLines = GetNumLines(ctxt.MemberRA);
				if (numLines > 1)
				{
					len += GetMinMaxWidth(ctxt, null, vwenv);
				}
				else
				{
					if (ctxt.MemberRA.ClassID != PhSimpleContextNC.kclsidPhSimpleContextNC)
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
			int minWidth = GetStrWidth(m_cache.MakeUserTss(Convert.ToString(ctxt.Minimum)), props, vwenv);
			ITsString maxStr = ctxt.Maximum == -1 ? m_infinity : m_cache.MakeUserTss(Convert.ToString(ctxt.Maximum));
			int maxWidth = GetStrWidth(maxStr, props, vwenv);
			return Math.Max(minWidth, maxWidth);
		}

		int GetSimpleCtxtWidth(IPhSimpleContext ctxt, IVwEnv vwenv)
		{
			if (ctxt == null)
				return 0;

			switch (ctxt.ClassID)
			{
				case PhSimpleContextBdry.kclsidPhSimpleContextBdry:
					IPhSimpleContextBdry bdryCtxt = ctxt as IPhSimpleContextBdry;
					return GetTermUnitWidth(bdryCtxt.FeatureStructureRA, vwenv);

				case PhSimpleContextSeg.kclsidPhSimpleContextSeg:
					IPhSimpleContextSeg segCtxt = ctxt as IPhSimpleContextSeg;
					return GetTermUnitWidth(segCtxt.FeatureStructureRA, vwenv);

				case PhSimpleContextNC.kclsidPhSimpleContextNC:
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
			if (ctxt.FeatureStructureRAHvo != 0 && ctxt.FeatureStructureRA.ClassID == PhNCFeatures.kclsidPhNCFeatures)
			{
				int numLines = GetNumLines(ctxt);
				if (numLines == 1)
				{
					int len = 0;
					if (ctxt.FeatureStructureRA.Abbreviation.UserDefaultWritingSystem == "C"
						|| ctxt.FeatureStructureRA.Abbreviation.UserDefaultWritingSystem == "V")
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
				if (ctxt.FeatureStructureRAHvo == 0)
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
			foreach (IFsFeatureSpecification spec in natClass.FeaturesOA.FeatureSpecsOC)
			{
				IFsClosedValue curVal = spec as IFsClosedValue;
				ITsString featLine = CreateFeatureLine(curVal);
				int len = GetStrWidth(featLine, null, vwenv);
				if (len > maxLen)
					maxLen = len;
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
			FdoSequence<IPhFeatureConstraint> vars = polarity ? ctxt.PlusConstrRS : ctxt.MinusConstrRS;
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
