using System;
using System.Diagnostics;
using System.Drawing;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// The modification of the main class suitable for this view.
	/// </summary>
	public class InterlinPrintChild : InterlinDocChild
	{
		// Make one.
		public InterlinPrintChild()
		{
			this.BackColor = Color.FromKnownColor(KnownColor.Window);
		}
		/// <summary>
		/// Pull this out into a separate method so InterlinPrintChild can make an InterlinPrintVc.
		/// </summary>
		protected override void MakeVc()
		{
			m_vc = new InterlinPrintVc(m_fdoCache);
		}

		/// <summary>
		/// Suppress the special behavior that produces a Sandbox when a click happens.
		/// </summary>
		/// <param name="e"></param>
		protected override bool HandleClickSelection(IVwSelection vwselNew, bool fBundleOnly, bool fConfirm)
		{
			return false;
		}

		/// <summary>
		/// Selecting an annotation has no side effects in this view. Among other things
		/// this suppresses the initial positioning and display of the sandbox.
		/// </summary>
		/// <param name="hvoAnnotation"></param>
		/// <param name="hvoAnalysis"></param>
		/// <param name="fConfirm"></param>
		/// <param name="fMakeDefaultSelection"></param>
		public override void TriggerAnnotationSelected (int hvoAnnotation, int hvoAnalysis,
			bool fConfirm, bool fMakeDefaultSelection)
		{
		}

		/// <summary>
		/// Select the word indicated by the text-wordform-in-context (twfic) annotation.
		/// This ignores the Sandbox! This is 'public' because it overrides a public method.
		/// </summary>
		/// <param name="hvoAnn"></param>
		public override void SelectAnnotation(int hvoAnn)
		{
			ISilDataAccess sda = Cache.MainCacheAccessor;
			// We should assert that ann is Twfic
			int twficType = CmAnnotationDefn.Twfic(Cache).Hvo;
			int annoType = sda.get_ObjectProp(hvoAnn, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
			Debug.Assert(annoType == twficType, "Given annotation type should be twfic("
				+ twficType + ") but was " + annoType + ".");

			// The following will select the Twfic, ... I hope!
			// Scroll to selection into view
			IVwSelection sel = SelectWficInIText(hvoAnn);
			if (sel == null)
				return;
			if (!this.Focused)
				this.Focus();
			this.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoTop);
			Update();
		}

		/// <summary>
		/// Return annotation Hvo that contains the selection.
		/// </summary>
		/// <returns></returns>
		public int AnnotationContainingSelection()
		{
			Debug.Assert(m_rootb != null);
			if (m_rootb == null)
				return 0;
			IVwSelection sel = m_rootb.Selection;
			if (sel == null)
				return 0;

			// See if our selection contains a base annotation.
			int cvsli = sel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			// Out variables for AllTextSelInfo.
			int ihvoRoot, tagTextProp, cpropPrevious, ichAnchor, ichEnd, ws, ihvoEnd;
			bool fAssocPrev;
			ITsTextProps ttpBogus;
			// Main array of information retrived from sel that made combo.
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);

			return FindBaseAnnInSelLevInfo(rgvsli); // returns 0 if unsuccessful
		}

		private int FindBaseAnnInSelLevInfo(SelLevInfo[] rgvsli)
		{
			for (int i = 0; i < rgvsli.Length; i++)
			{
				int hvoAnn = rgvsli[i].hvo;
				if (hvoAnn > 0 && HvoIsRealBaseAnnotation(hvoAnn))
					return hvoAnn;
			}
			return 0;
		}

		public override void MakeRoot()
		{
			base.MakeRoot();
			// Making this ReadOnlyView helps to prevent a hang (LT-9896) when the control gets focus and
			// tries to EnsureDefaultSelection, looking for an editable field.
			// ReadOnlyView tells EnsureDefaultSelection it's okay to find noneditable text to make a selection.
			this.ReadOnlyView = true;
		}

		/// <summary>
		/// Activate() is disabled by default in ReadOnlyViews, but PrintView does want to show selections.
		/// </summary>
		protected override bool AllowDisplaySelection
		{
			get { return true; }
		}
	}

	/// <summary>
	/// Modifications of InterlinVc for printing.
	/// </summary>
	public class InterlinPrintVc : InterlinVc
	{
		internal const int kfragTextDescription = 200027;

		public InterlinPrintVc(FdoCache cache) : base(cache)
		{

		}

		protected override int LabelRGBFor(InterlinLineSpec spec)
		{
			// In the print view these colors are plain black.
			return 0;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			ITsStrFactory tsf = null;
			switch (frag)
			{
				case kfragStText: // The whole text, root object for the InterlinDocChild.
					if (hvo == 0)
						return;		// What if the user deleted all the texts?  See LT-6727.
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvDefault,
						(int)TptEditable.ktptNotEditable);
					vwenv.OpenDiv();
					StText stText = new StText(m_cache, hvo);
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 6000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
						(int)FwTextPropVar.ktpvMilliPoint, 24000);
					// Add both vernacular and analysis if we have them (LT-5561).
					bool fAddedVernacular = false;
					int wsVernTitle = 0;
					//
					if (stText.Title.TryWs(LangProject.kwsFirstVern, out wsVernTitle))
					{
						vwenv.OpenParagraph();
						vwenv.AddStringAltMember(vtagStTextTitle, wsVernTitle, this);
						vwenv.CloseParagraph();
						fAddedVernacular = true;
					}
					int wsAnalysisTitle = 0;
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 10000);
					vwenv.OpenParagraph();
					ITsString tssAnal;
					if (stText.Title.TryWs(LangProject.kwsFirstAnal, out wsAnalysisTitle, out tssAnal) &&
						!tssAnal.Equals(stText.Title.BestVernacularAlternative))
					{
						if (fAddedVernacular)
						{
							// display analysis title at smaller font size.
							vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
								(int)FwTextPropVar.ktpvMilliPoint, 12000);
						}
						vwenv.AddStringAltMember(vtagStTextTitle, wsAnalysisTitle, this);
					}
					else
					{
						// just add a blank title.
						tsf = TsStrFactoryClass.Create();
						ITsString blankTitle = tsf.MakeString("", m_wsAnalysis);
						vwenv.AddString(blankTitle);
					}
					vwenv.CloseParagraph();
					int wsSource = 0;
					ITsString tssSource = stText.SourceOfTextForWs(m_wsVernForDisplay);
					if (tssSource == null || tssSource.Length == 0)
					{
						tssSource = stText.SourceOfTextForWs(m_wsAnalysis);
						if (tssSource != null && tssSource.Length > 0)
							wsSource = m_wsAnalysis;
					}
					else
					{
						wsSource = m_wsVernForDisplay;
					}
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 10000);
					if (tssSource != null && tssSource.Length > 0)
					{
						vwenv.OpenParagraph();
						vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
							(int)FwTextPropVar.ktpvMilliPoint, 12000);
						vwenv.AddStringAltMember(vtagStTextSource, wsSource, this);
						vwenv.CloseParagraph();
					}
					else
					{
						// just add a blank source.
						tsf = TsStrFactoryClass.Create();
						ITsString tssBlank = tsf.MakeString("", m_wsAnalysis);
						vwenv.AddString(tssBlank);
					}
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 10000);
					vwenv.OpenParagraph();
					if (stText.OwningFlid == (int)Text.TextTags.kflidContents)
					{
						vwenv.AddObjProp((int)CmObjectFields.kflidCmObject_Owner, this, kfragTextDescription);
					}
					vwenv.CloseParagraph();
					base.Display(vwenv, hvo, frag);
					vwenv.CloseDiv();
					break;
				case kfragTextDescription:
					vwenv.AddStringAltMember((int)CmMajorObject.CmMajorObjectTags.kflidDescription, m_wsAnalysis, this);
					break;
				case kfragSegFf: // One freeform annotation.
					int[] wssAnalysis = m_WsList.AnalysisWsIds;
					if (wssAnalysis.Length == 0)
						break; // This is bizarre, but for the sake of paranoia...
					tsf = TsStrFactoryClass.Create();
					int hvoType = m_cache.MainCacheAccessor.get_ObjectProp(hvo, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
					string label = "";
					if (hvoType == NoteSegmentDefn)
						label = ITextStrings.ksNt;
					else if (hvoType == FtSegmentDefn)
						label = ITextStrings.ksFT;
					else if (hvoType == LtSegmentDefn)
						label = ITextStrings.ksLT;
					else
						throw new Exception("Unexpected FF annotation type");
					ITsString tssLabel = tsf.MakeString(label, m_cache.DefaultUserWs);
					ISilDataAccess sda = vwenv.DataAccess;
					if (wssAnalysis.Length == 1)
					{
						ITsString tss = sda.get_MultiStringAlt(hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment, m_cache.DefaultAnalWs);
						if (tss.Length == 0)
							break;
						vwenv.OpenParagraph();
						vwenv.AddString(tssLabel);
						vwenv.AddStringAltMember((int)CmAnnotation.CmAnnotationTags.kflidComment, m_cache.DefaultAnalWs, this);
						vwenv.CloseParagraph();
					}
					else
					{
						int labelWidth, labelHeight;
						vwenv.get_StringWidth(tssLabel, null, out labelWidth, out labelHeight);
						// This roughly corresponds to the width of the space at the end of FT.
						// The nice way to do it (here and in the base class) would be a table or 'interlinear' paragraph.
						labelWidth += 3000;
						int cNonBlank = 0;
						for (int i = 0; i < wssAnalysis.Length; i++)
						{
							ITsString tss = sda.get_MultiStringAlt(hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment, wssAnalysis[i]);
							if (tss.Length == 0)
								continue;
							if (cNonBlank != 0)
							{
								// Indent subsequent paragraphs by the width of the main label.
								vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent,
									(int) FwTextPropVar.ktpvMilliPoint, labelWidth);
							}
							vwenv.OpenParagraph();
							if (cNonBlank == 0)
								vwenv.AddString(tssLabel);
							cNonBlank++; // after tests above!
							m_WsList.AddWsLabel(vwenv, i);
							vwenv.AddStringAltMember((int)CmAnnotation.CmAnnotationTags.kflidComment, wssAnalysis[i], this);
							vwenv.CloseParagraph();
						}
					}
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}
	}
}
