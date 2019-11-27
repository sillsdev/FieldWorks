// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class SandboxVc : FwBaseVc, IDisposable
	{
		internal int krgbBorder = (int)CmObjectUi.RGB(Color.Blue);
		internal int krgbEditable = (int)CmObjectUi.RGB(Color.White);
		//The color to use as a background indicating there are multiple options for this analysis and/or gloss
		internal int multipleAnalysisColor = (int)CmObjectUi.RGB(Control.DefaultBackColor);
		const int kmpIconMargin = 3000; // gap between pull-down icon and morph (also word gloss and boundary)
		internal const int kfragBundle = 100001;
		internal const int kfragMorph = 100002;
		internal const int kfragFirstMorph = 1000014;
		internal const int kfragMissingMorphs = 100003;
		internal const int kfragMissingEntry = 100005;
		internal const int kfragMissingMorphGloss = 100006;
		internal const int kfragMissingMorphPos = 100007;
		internal const int kfragMissingWordPos = 100008;
		// 14 is used above
		// This one needs a free range following it. It displays the name of an SbNamedObject,
		// using the writing system indicated by m_choices[frag - kfragNamedObjectNameChoices.
		internal const int kfragNamedObjectNameChoices = 1001000;
		protected int m_wsVern;
		protected int m_wsAnalysis;
		protected int m_wsUi;
		protected CachePair m_caches;
		private ITsString m_tssMissingMorphs;
		private ITsString m_tssMissingEntry;
		private ITsString m_tssMissingMorphGloss;
		private ITsString m_tssMissingMorphPos;
		private ITsString m_tssMissingWordPos;
		private ITsString m_tssEmptyAnalysis;
		private ITsString m_tssEmptyVern;
		private InterlinLineChoices m_choices;
		private ComPictureWrapper m_PulldownArrowPic;
		// width in millipoints of the arrow picture.
		private int m_dxmpArrowPicWidth;
		private bool m_fIconsForAnalysisChoices;
		private bool m_fRtl;
		private SandboxBase m_sandbox;

		public SandboxVc(CachePair caches, InterlinLineChoices choices, bool fIconsForAnalysisChoices, SandboxBase sandbox)
		{
			m_caches = caches;
			m_cache = caches.MainCache; //prior to 9-20-2011 this was not set, if we find there was a reason get rid of this.
			m_choices = choices;
			m_sandbox = sandbox;
			m_fIconsForAnalysisChoices = fIconsForAnalysisChoices;
			m_wsAnalysis = caches.MainCache.DefaultAnalWs;
			m_wsUi = caches.MainCache.LanguageWritingSystemFactoryAccessor.UserWs;
			m_tssMissingMorphs = TsStringUtils.MakeString(ITextStrings.ksStars, m_sandbox.RawWordformWs);
			m_tssEmptyAnalysis = TsStringUtils.EmptyString(m_wsAnalysis);
			m_tssEmptyVern = TsStringUtils.EmptyString(m_sandbox.RawWordformWs);
			m_tssMissingEntry = m_tssMissingMorphs;
			// It's tempting to re-use m_tssMissingMorphs, but the analysis and vernacular default
			// fonts may have different sizes, requiring differnt line heights to align things well.
			m_tssMissingMorphGloss = TsStringUtils.MakeString(ITextStrings.ksStars, m_wsAnalysis);
			m_tssMissingMorphPos = TsStringUtils.MakeString(ITextStrings.ksStars, m_wsAnalysis);
			m_tssMissingWordPos = m_tssMissingMorphPos;
			m_PulldownArrowPic = OLEConvert.ConvertImageToComPicture(ResourceHelper.InterlinPopupArrow);
			m_dxmpArrowPicWidth = ConvertPictureWidthToMillipoints(m_PulldownArrowPic.Picture);
			var wsObj = caches.MainCache.ServiceLocator.WritingSystemManager.Get(m_sandbox.RawWordformWs);
			if (wsObj != null)
			{
				m_fRtl = wsObj.RightToLeftScript;
			}
		}

		/// <summary>
		/// We want a width in millipoints (72000/inch). Value we have is in 100/mm. There are 25.4 mm/inch.
		/// </summary>
		private static int ConvertPictureWidthToMillipoints(IPicture picture)
		{
			return picture.Width * 72000 / 2540;
		}

		#region Disposable stuff

		/// <summary />
		~SandboxVc()
		{
			Dispose(false);
		}

		/// <summary />
		private bool IsDisposed { get; set; }

		/// <summary />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary />
		protected virtual void Dispose(bool fDisposing)
		{
			Debug.WriteLineIf(!fDisposing, "******* Missing Dispose() call for " + GetType() + " *******");
			if (IsDisposed)
			{
				// No need to do it more than once.
				return;
			}
			if (fDisposing)
			{
				// Dispose managed resources here.
				m_PulldownArrowPic?.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sandbox = null; // Client gave it to us, so has to deal with it.
			m_caches = null; // Client gave it to us, so has to deal with it.
			m_PulldownArrowPic = null;
			m_tssMissingEntry = null; // Same as m_tssMissingMorphs, so just null it.
			m_tssMissingWordPos = null; // Same as m_tssMissingMorphPos, so just null it.
			m_tssMissingMorphs = null;
			m_tssEmptyAnalysis = null;
			m_tssEmptyVern = null;
			m_tssMissingMorphGloss = null;
			m_tssMissingMorphPos = null;
			IsDisposed = true;
		}
		#endregion

		/// <summary>
		/// Get or set the editability for the morpheme form row.
		/// </summary>
		/// <remarks>
		/// 'False' means to not show the icon and to not make the form editable.
		/// 'True' means to show the icon under certain conditions, and to allow the form to be edited.
		/// </remarks>
		public bool IsMorphemeFormEditable { get; set; } = true;

		/// <summary>
		/// Color to use for guessing.
		/// </summary>
		internal int MultipleOptionBGColor
		{
			get
			{
				return multipleAnalysisColor;
			}
			set
			{
				if (multipleAnalysisColor == value)
				{
					return;
				}
				multipleAnalysisColor = value;
				if (m_sandbox.RootBox != null)
				{
					//refresh the m_sandbox so that the background color will change.
					m_sandbox.Refresh();
				}
			}
		}

		internal int BackColor { get; set; } = (int)CmObjectUi.RGB(235, 235, 220);

		/// <summary>
		/// Get/set whether the sandbox is RTL
		/// </summary>
		internal bool RightToLeft
		{
			get
			{
				return m_fRtl;
			}
			set
			{
				if (value == m_fRtl)
				{
					return;
				}
				m_fRtl = value;
				m_sandbox.RootBox?.Reconstruct();
			}
		}

		public bool ShowWordGlossIcon { get; private set; }

		/// <summary>
		/// Called right before adding a string or opening a flow object, sets its color.
		/// </summary>
		protected static void SetColor(IVwEnv vwenv, int color)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, color);
		}

		private void AddPullDownIcon(IVwEnv vwenv, int tag)
		{
			if (!m_fIconsForAnalysisChoices)
			{
				return;
			}
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, kmpIconMargin);
			vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, -2500);
			vwenv.AddPicture(m_PulldownArrowPic.Picture, tag, 0, 0);
		}

		/// <summary>
		/// Set the indent needed when the icon is missing.
		/// </summary>
		private void SetIndentForMissingIcon(IVwEnv vwenv)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvMilliPoint, m_dxmpArrowPicWidth + kmpIconMargin);
		}
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			try
			{
				switch (frag)
				{
					case kfragBundle: // One annotated word bundle, in this case, the whole view.
						if (hvo == 0)
						{
							return;
						}
						vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmDoNotCheck);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, BackColor);
						vwenv.OpenDiv();
						vwenv.OpenParagraph();
						// Since embedded in a pile with context, we need another layer of pile here,.
						// It's an overlay sandbox: draw a box around it.
						vwenv.OpenInnerPile();
						// Inside that division we need a paragraph which does not have any border
						// or background. This suppresses the 'infinite width' behavior for the
						// nested paragraphs that may have grey border.
						vwenv.OpenParagraph();

						// This makes a little separation between left border and arrows.
						vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, 1000);
						if (m_fRtl)
						{
							// This must not be on the outer paragraph or we get infinite width behavior.
							vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
							vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
						}
						vwenv.OpenInnerPile();
						for (var ispec = 0; ispec < m_choices.Count;)
						{
							var spec = m_choices[ispec];
							if (!spec.WordLevel)
							{
								break;
							}
							if (spec.MorphemeLevel)
							{
								DisplayMorphBundles(vwenv, hvo);
								ispec = m_choices.LastMorphemeIndex + 1;
								continue;
							}
							switch (spec.Flid)
							{
								case InterlinLineChoices.kflidWord:
									DisplayWordform(vwenv, GetActualWs(hvo, spec.StringFlid, spec.WritingSystem), ispec);
									break;
								case InterlinLineChoices.kflidWordGloss:
									DisplayWordGloss(vwenv, hvo, spec.WritingSystem, ispec);
									break;
								case InterlinLineChoices.kflidWordPos:
									DisplayWordPOS(vwenv, hvo, spec.WritingSystem, ispec);
									break;
							}
							ispec++;
						}
						vwenv.CloseInnerPile();
						vwenv.CloseParagraph();
						vwenv.CloseInnerPile();
						vwenv.CloseParagraph();
						vwenv.CloseDiv();
						break;
					case kfragFirstMorph: // first morpheme in word
					case kfragMorph: // The bundle of 4 lines representing a morpheme.
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, 10000);
						vwenv.OpenInnerPile();
						for (var ispec = m_choices.FirstMorphemeIndex; ispec <= m_choices.LastMorphemeIndex; ispec++)
						{
							var tagLexEntryIcon = 0;
							if (m_choices.FirstLexEntryIndex == ispec)
							{
								tagLexEntryIcon = SandboxBase.ktagMorphEntryIcon;
							}
							var spec = m_choices[ispec];
							switch (spec.Flid)
							{
								case InterlinLineChoices.kflidMorphemes:
									DisplayMorphForm(vwenv, hvo, frag, spec.WritingSystem, ispec);
									break;
								case InterlinLineChoices.kflidLexEntries:
									AddOptionalNamedObj(vwenv, hvo, SandboxBase.ktagSbMorphEntry, SandboxBase.ktagMissingEntry, kfragMissingEntry, tagLexEntryIcon, spec.WritingSystem, ispec);
									break;
								case InterlinLineChoices.kflidLexGloss:
									AddOptionalNamedObj(vwenv, hvo, SandboxBase.ktagSbMorphGloss, SandboxBase.ktagMissingMorphGloss, kfragMissingMorphGloss, tagLexEntryIcon, spec.WritingSystem, ispec);
									break;
								case InterlinLineChoices.kflidLexPos:
									AddOptionalNamedObj(vwenv, hvo, SandboxBase.ktagSbMorphPos, SandboxBase.ktagMissingMorphPos, kfragMissingMorphPos, tagLexEntryIcon, spec.WritingSystem, ispec);
									break;
							}
						}
						vwenv.CloseInnerPile();
						break;
					default:
						if (frag >= kfragNamedObjectNameChoices && frag < kfragNamedObjectNameChoices + m_choices.Count)
						{
							vwenv.AddStringAltMember(SandboxBase.ktagSbNamedObjName, GetActualWs(hvo, SandboxBase.ktagSbNamedObjName, m_choices[frag - kfragNamedObjectNameChoices].WritingSystem), this);
						}
						else
						{
							throw new Exception("Bad fragment ID in SandboxVc.Display");
						}
						break;
				}
			}
			catch
			{
				Debug.Assert(false, "Exception thrown in the display of the SandboxVc (About to be eaten)");
			}
		}

		private int GetActualWs(int hvo, int tag, int ws)
		{
			switch (ws)
			{
				case WritingSystemServices.kwsVernInParagraph:
					ws = m_sandbox.RawWordformWs;
					break;
				case WritingSystemServices.kwsFirstAnal:
					ws = GetBestAlt(hvo, tag, m_caches.MainCache.DefaultAnalWs, m_caches.MainCache.DefaultAnalWs,
					m_caches.MainCache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(wsObj => wsObj.Handle).ToArray());
					break;
				case WritingSystemServices.kwsFirstVern:
					// for best vernacular in Sandbox, we prefer to use the ws of the wordform
					// over the standard 'default vernacular'
					var wsPreferred = m_sandbox.RawWordformWs;
					ws = GetBestAlt(hvo, tag, wsPreferred, m_caches.MainCache.DefaultVernWs, m_caches.MainCache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(wsObj => wsObj.Handle).ToArray());
					break;
				default:
					if (ws < 0)
					{
						throw new ArgumentException($"magic ws {ws} not yet supported.");
					}
					break;
			}
			return ws;
		}

		private int GetBestAlt(int hvo, int tag, int wsPreferred, int wsDefault, int[] wsList)
		{
			var wsSet = new HashSet<int>();
			if (wsPreferred != 0)
			{
				wsSet.Add(wsPreferred);
			}
			wsSet.UnionWith(wsList);
			var wsActual = 0;
			foreach (var ws1 in wsSet.ToArray())
			{
				var tssTest = m_caches.DataAccess.get_MultiStringAlt(hvo, tag, ws1);
				if (tssTest != null && tssTest.Length != 0)
				{
					wsActual = ws1;
					break;
				}
			}
			// Enhance JohnT: to be really picky here we should do like the real InterpretWsLabel
			// and fall back to default UI language, then English.
			// But we probably aren't even copying those alternatives to the sandbox cache.
			if (wsActual == 0)
			{
				wsActual = wsDefault;
			}
			return wsActual;
		}

		private void DisplayLexGloss(IVwEnv vwenv, int hvo, int ws, int choiceIndex)
		{
			var hvoNo = vwenv.DataAccess.get_ObjectProp(hvo, SandboxBase.ktagSbMorphGloss);
			SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
			if (m_fIconsForAnalysisChoices)
			{
				// This line does not have one, but add some white space to line things up.
				vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvMilliPoint, m_dxmpArrowPicWidth + kmpIconMargin);
			}
			if (hvoNo == 0)
			{
				// One of these is enough, the regeneration will redo an outer object and get
				// all the alternatives.
				vwenv.NoteDependency(new[] { hvo }, new[] { SandboxBase.ktagSbMorphGloss }, 1);
				vwenv.AddProp(SandboxBase.ktagMissingMorphGloss, this, kfragMissingMorphGloss);
			}
			else
			{
				vwenv.AddObjProp(SandboxBase.ktagSbMorphGloss, this, kfragNamedObjectNameChoices + choiceIndex);
			}
		}

		private void DisplayMorphForm(IVwEnv vwenv, int hvo, int frag, int ws, int choiceIndex)
		{
			// Allow editing of the morpheme breakdown line.
			SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
			// On this line we want an icon only for the first column (and only if it is the first
			// occurrence of the flid).
			var fWantIcon = IsMorphemeFormEditable && (frag == kfragFirstMorph) && m_choices.IsFirstOccurrenceOfFlid(choiceIndex);
			if (!fWantIcon)
			{
				SetIndentForMissingIcon(vwenv);
			}
			vwenv.OpenParagraph();
			var fFirstMorphLine = m_choices.IndexOf(InterlinLineChoices.kflidMorphemes) == choiceIndex;
			if (fWantIcon) // Review JohnT: should we do the 'edit box' for all first columns?
			{
				AddPullDownIcon(vwenv, SandboxBase.ktagMorphFormIcon);
				// Create an edit box that stays visible when the user deletes
				// the first morpheme (like the WordGloss box).
				// This is especially useful if the user wants to
				// delete the entire MorphForm line (cf. LT-1621).
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, kmpIconMargin);
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2000);
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, 2000);
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, krgbEditable);
			}
			// Per LT-14891, morpheme form is not editable except for the default vernacular.
			if (IsMorphemeFormEditable && m_choices.IsFirstOccurrenceOfFlid(choiceIndex))
			{
				MakeNextFlowObjectEditable(vwenv);
			}
			else
			{
				MakeNextFlowObjectReadOnly(vwenv);
			}
			vwenv.OpenInnerPile();
			vwenv.OpenParagraph();
			if (fFirstMorphLine)
			{
				vwenv.AddStringProp(SandboxBase.ktagSbMorphPrefix, this);
			}
			// This is never missing, but may, or may not, be editable.
			vwenv.AddObjProp(SandboxBase.ktagSbMorphForm, this, kfragNamedObjectNameChoices + choiceIndex);
			if (fFirstMorphLine)
			{
				vwenv.AddStringProp(SandboxBase.ktagSbMorphPostfix, this);
			}
			// close the special edit box we opened for the first morpheme.
			vwenv.CloseParagraph();
			vwenv.CloseInnerPile();
			vwenv.CloseParagraph();
		}

		private void DisplayWordPOS(IVwEnv vwenv, int hvo, int ws, int choiceIndex)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			AddOptionalNamedObj(vwenv, hvo, SandboxBase.ktagSbWordPos, SandboxBase.ktagMissingWordPos, kfragMissingWordPos, SandboxBase.ktagWordPosIcon, ws, choiceIndex);
		}

		private void DisplayWordGloss(IVwEnv vwenv, int hvo, int ws, int choiceIndex)
		{
			// Count how many glosses there are for the current analysis:
			var cGlosses = 0;
			// Find a wfi analysis (existing or guess) to determine whether to provide an icon for selecting
			// multiple word glosses for IhWordGloss.SetupCombo (cf. LT-1428)
			var wa = m_sandbox.GetWfiAnalysisInUse();
			if (wa != null)
			{
				cGlosses = wa.MeaningsOC.Count;
			}
			SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
			// Icon only if we want icons at all (currently always true) and there is at least one WfiGloss to choose
			// and this is the first word gloss line.
			var fWantIcon = m_fIconsForAnalysisChoices && (cGlosses > 0 || m_sandbox.ShouldAddWordGlossToLexicon) && m_choices.IsFirstOccurrenceOfFlid(choiceIndex);
			// If there isn't going to be an icon, add an indent.
			if (!fWantIcon)
			{
				SetIndentForMissingIcon(vwenv);
			}
			vwenv.OpenParagraph();
			if (fWantIcon)
			{
				AddPullDownIcon(vwenv, SandboxBase.ktagWordGlossIcon);
				ShowWordGlossIcon = true;
			}
			else if (ShowWordGlossIcon && cGlosses == 0)
			{
				// reset
				ShowWordGlossIcon = false;
			}
			//if there is more than one gloss set the background color to give visual indication
			if (cGlosses > 1)
			{
				//set directly to the MultipleApproved color rather than the stored one
				//the state of the two could be different.
				SetBGColor(vwenv, InterlinVc.MultipleApprovedGuessColor);
				var count = TsStringUtils.MakeString("" + cGlosses, Cache.DefaultUserWs);
				//make the number black.
				SetColor(vwenv, 0);
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, kmpIconMargin);
				vwenv.AddString(count);
			}

			//Set the margin and padding values
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, kmpIconMargin);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, 2000);
			SetBGColor(vwenv, krgbEditable);
			vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmNormalCheck);
			vwenv.OpenInnerPile();

			// Set the appropriate paragraph direction for the writing system.
			var fWsRtl = m_caches.MainCache.ServiceLocator.WritingSystemManager.Get(ws).RightToLeftScript;
			if (fWsRtl != RightToLeft)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, fWsRtl ? (int)FwTextToggleVal.kttvForceOn : (int)FwTextToggleVal.kttvOff);
			}

			vwenv.AddStringAltMember(SandboxBase.ktagSbWordGloss, ws, this);
			vwenv.CloseInnerPile();
			vwenv.CloseParagraph();
		}

		//Use the given IVwEnv and set the background color for the text property in it to the one that is given.
		//method added to provide clarification
		private static void SetBGColor(IVwEnv vwenv, int guessColor)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, guessColor);
		}

		private void DisplayMorphBundles(IVwEnv vwenv, int hvo)
		{
			// Don't allow direct editing of the morph bundle lines.
			MakeNextFlowObjectReadOnly(vwenv);
			if (vwenv.DataAccess.get_VecSize(hvo, SandboxBase.ktagSbWordMorphs) == 0)
			{
				SetColor(vwenv, m_choices.LabelRGBFor(m_choices.IndexOf(InterlinLineChoices.kflidMorphemes)));
				vwenv.AddProp(SandboxBase.ktagMissingMorphs, this, kfragMissingMorphs);
				// Blank lines to fill up the gap; LexEntry line
				vwenv.AddString(m_tssEmptyVern);
				vwenv.AddString(m_tssEmptyAnalysis); // LexGloss line
				vwenv.AddString(m_tssEmptyAnalysis); // LexPos line
			}
			else
			{
				vwenv.OpenParagraph();
				vwenv.AddObjVec(SandboxBase.ktagSbWordMorphs, this, kfragMorph);
				vwenv.CloseParagraph();
			}
		}

		private void DisplayWordform(IVwEnv vwenv, int ws, int choiceIndex)
		{
			// For the wordform line we only want an icon on the first line (which is always wordform).
			var fWantIcon = m_sandbox.ShowAnalysisCombo && choiceIndex == 0;
			// This has to be BEFORE we open the paragraph, so the indent applies to the whole
			// paragraph, and not some string box inside it.
			if (!fWantIcon)
			{
				SetIndentForMissingIcon(vwenv);
			}
			vwenv.OpenParagraph();
			// The actual icon, if present, has to be INSIDE the paragraph.
			if (fWantIcon)
			{
				AddPullDownIcon(vwenv, SandboxBase.ktagAnalysisIcon);
			}
			//Set the background of the wordform to the 'WordFormBGColor' which is set when ChangeOrCreateSandbox
			//is called
			SetBGColor(vwenv, MultipleOptionBGColor);
			if (ws != m_sandbox.RawWordformWs)
			{
				// Any other Ws we can edit.
				MakeNextFlowObjectEditable(vwenv);
				vwenv.OpenInnerPile(); // So white background occupies full width
				vwenv.AddStringAltMember(SandboxBase.ktagSbWordForm, ws, this);
				vwenv.CloseInnerPile();
			}
			else
			{
				MakeNextFlowObjectReadOnly(vwenv);
				//vwenv.AddString(m_sandbox.RawWordform);
				vwenv.AddStringAltMember(SandboxBase.ktagSbWordForm, ws, this);
			}
			vwenv.CloseParagraph();
		}

		private void SetEditabilityOfNextFlowObject(IVwEnv vwenv, bool fEditable)
		{
			if (fEditable)
			{
				MakeNextFlowObjectEditable(vwenv);
			}
			else
			{
				MakeNextFlowObjectReadOnly(vwenv);
			}
		}

		private void MakeNextFlowObjectReadOnly(IVwEnv vwenv)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
		}

		// Allow the next flow object to be edited (and give it a background color that
		// makes it look editable)
		private void MakeNextFlowObjectEditable(IVwEnv vwenv)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptIsEditable);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, krgbEditable);
		}

		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			switch (frag)
			{
				case kfragMorph: // The bundle of 4 lines representing a morpheme.
					var sda = vwenv.DataAccess;
					var cmorph = sda.get_VecSize(hvo, tag);
					for (var i = 0; i < cmorph; ++i)
					{
						var hvoMorph = sda.get_VecItem(hvo, tag, i);
						vwenv.AddObj(hvoMorph, this, i == 0 ? kfragFirstMorph : kfragMorph);
					}
					break;
			}
		}

		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			switch (frag)
			{
				case kfragMissingMorphs:
					return m_tssMissingMorphs;
				case kfragMissingEntry:
					return m_tssMissingEntry;
				case kfragMissingMorphGloss:
					return m_tssMissingMorphGloss;
				case kfragMissingMorphPos:
					return m_tssMissingMorphPos;
				case kfragMissingWordPos:
					return m_tssMissingWordPos;
				default:
					throw new Exception("Bad fragment ID in SandboxVc.DisplayVariant");
			}
		}

		// Return the width of the arrow picture (in mm, unfortunately).
		internal int ArrowPicWidth => m_PulldownArrowPic.Picture.Width;

		/// <summary>
		/// Add to the vwenv a display of property tag of object hvo, which stores an
		/// SbNamedObj.  If the property is non-null, display the name of the SbNamedObj.
		/// If not, display the dummyTag 'property' using the dummyFrag.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="dummyTag"></param>
		/// <param name="dummyFrag"></param>
		/// <param name="tagIcon">If non-zero, display a pull-down icon before the item, marked with this tag.</param>
		/// <param name="ws">which alternative of the name to display</param>
		/// <param name="choiceIndex">which item in m_choices this comes from. The icon is displayed
		/// only if it is the first one for its flid.</param>
		protected void AddOptionalNamedObj(IVwEnv vwenv, int hvo, int tag, int dummyTag, int dummyFrag, int tagIcon, int ws, int choiceIndex)
		{
			var hvoNo = vwenv.DataAccess.get_ObjectProp(hvo, tag);
			SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
			var fWantIcon = tagIcon != 0 && m_choices.IsFirstOccurrenceOfFlid(choiceIndex);
			if (m_fIconsForAnalysisChoices && !fWantIcon)
			{
				// This line does not have one, but add some white space to line things up.
				vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvMilliPoint, m_dxmpArrowPicWidth + kmpIconMargin);
			}
			vwenv.OpenParagraph();
			if (fWantIcon)
			{
				AddPullDownIcon(vwenv, tagIcon);
			}
			// The NoteDependency is needed whether or not hvoNo is set, in case we update
			// to a sense which has a null MSA.  See LT-4246.
			vwenv.NoteDependency(new int[] { hvo }, new int[] { tag }, 1);
			if (hvoNo == 0)
			{
				vwenv.AddProp(dummyTag, this, dummyFrag);
			}
			else
			{
				vwenv.AddObjProp(tag, this, kfragNamedObjectNameChoices + choiceIndex);
			}
			vwenv.CloseParagraph();
		}

		internal void UpdateLineChoices(InterlinLineChoices choices)
		{
			m_choices = choices;
		}
	}
}