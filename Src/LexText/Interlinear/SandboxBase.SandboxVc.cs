using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Resources;
using SIL.Utils.ComTypes;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	partial class SandboxBase
	{
		public class SandboxVc : FwBaseVc, IDisposable
		{
			internal int krgbBackground = (int)CmObjectUi.RGB(235, 235, 220);
			//internal int krgbBackground = (int) CmObjectUi.RGB(Color.FromKnownColor(KnownColor.ControlLight)); //235,235,220);
			internal int krgbBorder = (int)CmObjectUi.RGB(Color.Blue);
			internal int krgbEditable = (int)CmObjectUi.RGB(Color.White);
			//The color to use as a background indicating there are multiple options for this analysis and/or gloss
			internal int multipleAnalysisColor = (int)CmObjectUi.RGB(DefaultBackColor);
			const int kmpIconMargin = 3000; // gap between pull-down icon and morph (also word gloss and boundary)
			internal const int kfragBundle = 100001;
			internal const int kfragMorph = 100002;
			internal const int kfragFirstMorph = 1000014;
			internal const int kfragMissingMorphs = 100003;
			internal const int kfragMissingEntry = 100005;
			internal const int kfragMissingMorphGloss = 100006;
			internal const int kfragMissingMorphPos = 100007;
			internal const int kfragMissingWordPos = 100008;
			//internal const int kfragMlAnalysisNames = 100013;
			// 14 is used above
			// This one needs a free range following it. It displays the name of an SbNamedObject,
			// using the writing system indicated by m_choices[frag - kfragNamedObjectNameChoices.
			internal const int kfragNamedObjectNameChoices = 1001000;


			protected int m_wsVern;
			protected int m_wsAnalysis;
			protected int m_wsUi;
			protected CachePair m_caches;
			ITsString m_tssMissingMorphs;
			ITsString m_tssMissingEntry;
			ITsString m_tssMissingMorphGloss;
			ITsString m_tssMissingMorphPos;
			ITsString m_tssMissingWordPos;
			ITsString m_tssEmptyAnalysis;
			ITsString m_tssEmptyVern;
			InterlinLineChoices m_choices;
			ComPictureWrapper m_PulldownArrowPic;

			// width in millipoints of the arrow picture.
			int m_dxmpArrowPicWidth;
			bool m_fIconsForAnalysisChoices;
			bool m_fShowMorphBundles = true;
			bool m_fIconForWordGloss = false;
			bool m_fIsMorphemeFormEditable = true;
			bool m_fRtl = false;
			SandboxBase m_sandbox;

			public SandboxVc(CachePair caches, InterlinLineChoices choices, bool fIconsForAnalysisChoices, SandboxBase sandbox)
			{
				m_caches = caches;
				m_cache = caches.MainCache; //prior to 9-20-2011 this was not set, if we find there was a reason get rid of this.
				m_choices = choices;
				m_sandbox = sandbox;
				m_fIconsForAnalysisChoices = fIconsForAnalysisChoices;
				m_wsAnalysis = caches.MainCache.DefaultAnalWs;
				m_wsUi = caches.MainCache.LanguageWritingSystemFactoryAccessor.UserWs;
				m_tssMissingMorphs = m_tsf.MakeString(ITextStrings.ksStars, m_sandbox.RawWordformWs);
				m_tssEmptyAnalysis = m_tsf.MakeString("", m_wsAnalysis);
				m_tssEmptyVern = m_tsf.MakeString("", m_sandbox.RawWordformWs);
				m_tssMissingEntry = m_tssMissingMorphs;
				// It's tempting to re-use m_tssMissingMorphs, but the analysis and vernacular default
				// fonts may have different sizes, requiring differnt line heights to align things well.
				m_tssMissingMorphGloss = m_tsf.MakeString(ITextStrings.ksStars, m_wsAnalysis);
				m_tssMissingMorphPos = m_tsf.MakeString(ITextStrings.ksStars, m_wsAnalysis);
				m_tssMissingWordPos = m_tssMissingMorphPos;
				m_PulldownArrowPic = VwConstructorServices.ConvertImageToComPicture(ResourceHelper.InterlinPopupArrow);
				m_dxmpArrowPicWidth = ConvertPictureWidthToMillipoints(m_PulldownArrowPic.Picture);
				IWritingSystem wsObj = caches.MainCache.ServiceLocator.WritingSystemManager.Get(m_sandbox.RawWordformWs);
				if (wsObj != null)
					m_fRtl = wsObj.RightToLeftScript;

			}

			/// <summary>
			/// We want a width in millipoints (72000/inch). Value we have is in 100/mm. There are 25.4 mm/inch.
			/// </summary>
			/// <param name="picture"></param>
			/// <returns></returns>
			private int ConvertPictureWidthToMillipoints(IPicture picture)
			{
				const int kMillipointsPerInch = 72000 / 2540;
				return picture.Width * kMillipointsPerInch;
			}

			#region Disposable stuff
			#if DEBUG
			/// <summary/>
			~SandboxVc()
			{
				Dispose(false);
			}
			#endif

			/// <summary>
			/// Throw if the IsDisposed property is true
			/// </summary>
			public void CheckDisposed()
			{
				if (IsDisposed)
					throw new ObjectDisposedException(GetType().ToString(), "This object is being used after it has been disposed: this is an Error.");
			}

			/// <summary/>
			public bool IsDisposed
			{
				get;
				private set;
			}

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
				if (fDisposing && !IsDisposed)
				{
					// Dispose managed resources here.
					if (m_PulldownArrowPic != null)
						m_PulldownArrowPic.Dispose();
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_sandbox = null; // Client gave it to us, so has to deal with it.
				m_caches = null; // Client gave it to us, so has to deal with it.
				m_PulldownArrowPic = null;
				m_tsf = null;
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
			/// Get or set the editability for the moprhem form row.
			/// </summary>
			/// <remarks>
			/// 'False' means to not show the icon and to not make the form editable.
			/// 'True' means to show the icon under certain conditions, and to allow the form to be edited.
			/// </remarks>
			public bool IsMorphemeFormEditable
			{
				get
				{
					CheckDisposed();
					return m_fIsMorphemeFormEditable;
				}
				set
				{
					CheckDisposed();
					m_fIsMorphemeFormEditable = value;
				}
			}

			/// <summary>
			/// Color to use for guessing.
			/// </summary>
			internal int MultipleOptionBGColor
			{
				get
				{
					CheckDisposed();
					return multipleAnalysisColor;
				}
				set
				{
					CheckDisposed();
					multipleAnalysisColor = value;
					if (m_sandbox.m_rootb != null)
					{
						m_sandbox.m_rootb.Reconstruct();
					}
				}
			}

			internal int BackColor
			{
				get
				{
					CheckDisposed();
					return krgbBackground;
				}
				set
				{
					CheckDisposed();
					krgbBackground = value;
				}
			}

			/// <summary>
			/// Get/set whether the sandbox is RTL
			/// </summary>
			internal bool RightToLeft
			{
				get
				{
					CheckDisposed();
					return m_fRtl;
				}
				set
				{
					CheckDisposed();

					if (value == m_fRtl)
						return;
					m_fRtl = value;
					if (m_sandbox.RootBox != null)
						m_sandbox.RootBox.Reconstruct();
				}
			}
			// Controls whether to display the morpheme bundles.
			public bool ShowMorphBundles
			{
				get
				{
					CheckDisposed();
					return m_fShowMorphBundles;
				}
				set
				{
					CheckDisposed();
					m_fShowMorphBundles = value;
				}
			}

			public bool ShowWordGlossIcon
			{
				get
				{
					CheckDisposed();
					return m_fIconForWordGloss;
				}
			}

			/// <summary>
			/// Called right before adding a string or opening a flow object, sets its color.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="color"></param>
			protected static void SetColor(IVwEnv vwenv, int color)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
					(int)FwTextPropVar.ktpvDefault, color);
			}

			/// <summary>
			/// Add the specified string in the specified color to the display, using the UI Writing system.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="color"></param>
			/// <param name="str"></param>
			protected static void AddColoredString(IVwEnv vwenv, int color, ITsStrFactory tsf, int ws, string str)
			{
				SetColor(vwenv, color);
				vwenv.AddString(tsf.MakeString(str, ws));
			}

			/// <summary>
			/// Add to the vwenv the label(s) for a gloss line.
			/// If multiple glosses are wanted, it generates a set of labels
			/// </summary>
			public void AddGlossLabels(IVwEnv vwenv, ITsStrFactory tsf, int color, string baseLabel,
				FdoCache cache, WsListManager wsList)
			{
				if (wsList != null && wsList.AnalysisWsLabels.Length > 1)
				{
					ITsString tssBase = MakeUiElementString(baseLabel, cache.DefaultUserWs, null);
					ITsString space = tsf.MakeString(" ", cache.DefaultUserWs);
					foreach (ITsString tssLabel in wsList.AnalysisWsLabels)
					{
						SetColor(vwenv, color);
						vwenv.OpenParagraph();
						vwenv.AddString(tssBase);
						vwenv.AddString(space);
						vwenv.AddString(tssLabel);
						vwenv.CloseParagraph();
					}
				}
				else
				{
					AddColoredString(vwenv, color, tsf, cache.DefaultAnalWs, baseLabel);
				}
			}

			private void AddPullDownIcon(IVwEnv vwenv, int tag)
			{
				if (m_fIconsForAnalysisChoices)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
						(int)FwTextPropVar.ktpvMilliPoint, kmpIconMargin);
					vwenv.set_IntProperty((int)FwTextPropType.ktptOffset,
						(int)FwTextPropVar.ktpvMilliPoint, -2500);
					vwenv.AddPicture(m_PulldownArrowPic.Picture, tag, 0, 0);
				}
			}

			/// <summary>
			/// Set the indent needed when the icon is missing.
			/// </summary>
			/// <param name="vwenv"></param>
			private void SetIndentForMissingIcon(IVwEnv vwenv)
			{
				vwenv.set_IntProperty(
					(int)FwTextPropType.ktptLeadingIndent,
					(int)FwTextPropVar.ktpvMilliPoint,
					m_dxmpArrowPicWidth + kmpIconMargin);
			}

			/// <summary>
			/// If fWantIcon is true, add a pull-down icon; otherwise, set enough indent so the
			/// next thing in the paragraph will line up with things that have icons.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="fWantIcon"></param>
			private void SetIndentOrDisplayPullDown(IVwEnv vwenv, int tag, bool fWantIcon)
			{
				if (fWantIcon)
					AddPullDownIcon(vwenv, tag);
				else
					SetIndentForMissingIcon(vwenv);
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				try
				{
					switch (frag)
					{
						case kfragBundle: // One annotated word bundle, in this case, the whole view.
							if (hvo == 0)
								return;
							vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
								(int)SpellingModes.ksmDoNotCheck);
							vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
								(int)FwTextPropVar.ktpvDefault, krgbBackground);
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
							vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
								(int)FwTextPropVar.ktpvMilliPoint, 1000);
							if (m_fRtl)
							{
								// This must not be on the outer paragraph or we get infinite width behavior.
								vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
									(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
								vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
									(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
							}
							vwenv.OpenInnerPile();
							for (int ispec = 0; ispec < m_choices.Count; )
							{
								InterlinLineSpec spec = m_choices[ispec];
								if (!spec.WordLevel)
									break;
								if (spec.MorphemeLevel)
								{
									DisplayMorphBundles(vwenv, hvo);
									ispec = m_choices.LastMorphemeIndex + 1;
									continue;
								}
								switch (spec.Flid)
								{
									case InterlinLineChoices.kflidWord:
										int ws = GetActualWs(hvo, spec.StringFlid, spec.WritingSystem);
										DisplayWordform(vwenv, ws, ispec);
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
							vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
								(int)FwTextPropVar.ktpvMilliPoint, 10000);
							vwenv.OpenInnerPile();
							for (int ispec = m_choices.FirstMorphemeIndex; ispec <= m_choices.LastMorphemeIndex; ispec++)
							{
								int tagLexEntryIcon = 0;
								if (m_choices.FirstLexEntryIndex == ispec)
									tagLexEntryIcon = ktagMorphEntryIcon;
								InterlinLineSpec spec = m_choices[ispec];
								switch (spec.Flid)
								{
									case InterlinLineChoices.kflidMorphemes:
										DisplayMorphForm(vwenv, hvo, frag, spec.WritingSystem, ispec);
										break;
									case InterlinLineChoices.kflidLexEntries:
										AddOptionalNamedObj(vwenv, hvo, ktagSbMorphEntry, ktagMissingEntry,
											kfragMissingEntry, tagLexEntryIcon, spec.WritingSystem, ispec);
										break;
									case InterlinLineChoices.kflidLexGloss:
										AddOptionalNamedObj(vwenv, hvo, ktagSbMorphGloss, ktagMissingMorphGloss,
											kfragMissingMorphGloss, tagLexEntryIcon, spec.WritingSystem, ispec);
										break;
									case InterlinLineChoices.kflidLexPos:
										AddOptionalNamedObj(vwenv, hvo, ktagSbMorphPos, ktagMissingMorphPos,
											kfragMissingMorphPos, tagLexEntryIcon, spec.WritingSystem, ispec);
										break;
								}
							}
							vwenv.CloseInnerPile();

							break;
						default:
							if (frag >= kfragNamedObjectNameChoices && frag < kfragNamedObjectNameChoices + m_choices.Count)
							{
								InterlinLineSpec spec = m_choices[frag - kfragNamedObjectNameChoices];
								int wsActual = GetActualWs(hvo, ktagSbNamedObjName, spec.WritingSystem);
								vwenv.AddStringAltMember(ktagSbNamedObjName, wsActual, this);
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
						int wsPreferred = m_sandbox.RawWordformWs;
						ws = GetBestAlt(hvo, tag, wsPreferred, m_caches.MainCache.DefaultVernWs,
							m_caches.MainCache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(wsObj => wsObj.Handle).ToArray());
						break;
					default:
						if (ws < 0)
						{
							throw new ArgumentException(String.Format("magic ws {0} not yet supported.", ws));
						}
						break;
				}
				return ws;
			}

			private int GetBestAlt(int hvo, int tag, int wsPreferred, int wsDefault, int[] wsList)
			{
				Set<int> wsSet = new Set<int>();
				if (wsPreferred != 0)
					wsSet.Add(wsPreferred);
				wsSet.AddRange(wsList);
				// We're not dealing with a real cache, so can't call something like this:
				//ws = LangProject.InterpretWsLabel(m_caches.MainCache,
				//	LangProject.GetMagicWsNameFromId(ws),
				//	m_caches.MainCache.DefaultAnalWs,
				//	hvo, spec.StringFlid, null);
				int wsActual = 0;
				foreach (int ws1 in wsSet.ToArray())
				{
					ITsString tssTest = m_caches.DataAccess.get_MultiStringAlt(hvo, tag, ws1);
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
					wsActual = wsDefault;
				return wsActual;
			}

			private void DisplayLexGloss(IVwEnv vwenv, int hvo, int ws, int choiceIndex)
			{
				int hvoNo = vwenv.DataAccess.get_ObjectProp(hvo, ktagSbMorphGloss);
				SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
				if (m_fIconsForAnalysisChoices)
				{
					// This line does not have one, but add some white space to line things up.
					vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent,
						(int)FwTextPropVar.ktpvMilliPoint,
						m_dxmpArrowPicWidth + kmpIconMargin);
				}
				if (hvoNo == 0)
				{
					// One of these is enough, the regeneration will redo an outer object and get
					// all the alternatives.
					vwenv.NoteDependency(new int[] { hvo }, new int[] { ktagSbMorphGloss }, 1);
					vwenv.AddProp(ktagMissingMorphGloss, this, kfragMissingMorphGloss);
				}
				else
				{
					vwenv.AddObjProp(ktagSbMorphGloss, this, kfragNamedObjectNameChoices + choiceIndex);
				}
			}

			private void DisplayMorphForm(IVwEnv vwenv, int hvo, int frag, int ws, int choiceIndex)
			{
				int hvoMorphForm = vwenv.DataAccess.get_ObjectProp(hvo, ktagSbMorphForm);

				// Allow editing of the morpheme breakdown line.
				SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
				// On this line we want an icon only for the first column (and only if it is the first
				// occurrence of the flid).
				bool fWantIcon = m_fIsMorphemeFormEditable && (frag == kfragFirstMorph) && m_choices.IsFirstOccurrenceOfFlid(choiceIndex);
				if (!fWantIcon)
					SetIndentForMissingIcon(vwenv);
				vwenv.OpenParagraph();
				bool fFirstMorphLine = (m_choices.IndexOf(InterlinLineChoices.kflidMorphemes) == choiceIndex);
				if (fWantIcon) // Review JohnT: should we do the 'edit box' for all first columns?
				{
					AddPullDownIcon(vwenv, ktagMorphFormIcon);
					// Create an edit box that stays visible when the user deletes
					// the first morpheme (like the WordGloss box).
					// This is especially useful if the user wants to
					// delete the entire MorphForm line (cf. LT-1621).
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
						(int)FwTextPropVar.ktpvMilliPoint,
						kmpIconMargin);
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing,
						(int)FwTextPropVar.ktpvMilliPoint,
						2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
						(int)FwTextPropVar.ktpvMilliPoint,
						2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
						(int)FwTextPropVar.ktpvDefault, krgbEditable);
				}
				if (m_fIsMorphemeFormEditable)
					MakeNextFlowObjectEditable(vwenv);
				else
					MakeNextFlowObjectReadOnly(vwenv);
				vwenv.OpenInnerPile();
				vwenv.OpenParagraph();
				if (fFirstMorphLine)
					vwenv.AddStringProp(ktagSbMorphPrefix, this);
				// This is never missing, but may, or may not, be editable.
				vwenv.AddObjProp(ktagSbMorphForm, this, kfragNamedObjectNameChoices + choiceIndex);
				if (fFirstMorphLine)
					vwenv.AddStringProp(ktagSbMorphPostfix, this);
				// close the special edit box we opened for the first morpheme.
				vwenv.CloseParagraph();
				vwenv.CloseInnerPile();
				vwenv.CloseParagraph();
			}

			private void DisplayWordPOS(IVwEnv vwenv, int hvo, int ws, int choiceIndex)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
				AddOptionalNamedObj(vwenv, hvo, ktagSbWordPos, ktagMissingWordPos,
					kfragMissingWordPos, ktagWordPosIcon, ws, choiceIndex);
			}

			private void DisplayWordGloss(IVwEnv vwenv, int hvo, int ws, int choiceIndex)
			{
				// Count how many glosses there are for the current analysis:
				int cGlosses = 0;
				// Find a wfi analysis (existing or guess) to determine whether to provide an icon for selecting
				// multiple word glosses for IhWordGloss.SetupCombo (cf. LT-1428)
				IWfiAnalysis wa = m_sandbox.GetWfiAnalysisInUse();
				if (wa != null)
					cGlosses = wa.MeaningsOC.Count;

				SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));

				// Icon only if we want icons at all (currently always true) and there is at least one WfiGloss to choose
				// and this is the first word gloss line.
				bool fWantIcon = m_fIconsForAnalysisChoices &&
					(cGlosses > 0 || m_sandbox.ShouldAddWordGlossToLexicon) &&
					m_choices.IsFirstOccurrenceOfFlid(choiceIndex);
				// If there isn't going to be an icon, add an indent.
				if (!fWantIcon)
				{
					SetIndentForMissingIcon(vwenv);
				}
				vwenv.OpenParagraph();
				if (fWantIcon)
				{
					AddPullDownIcon(vwenv, ktagWordGlossIcon);
					m_fIconForWordGloss = true;
				}
				else if (m_fIconForWordGloss == true && cGlosses == 0)
				{
					// reset
					m_fIconForWordGloss = false;
				}
				//if there is more than one gloss set the background color to give visual indication
				if (cGlosses > 1)
				{
					//set directly to the MultipleApproved color rather than the stored one
					//the state of the two could be different.
					SetBGColor(vwenv, InterlinVc.MultipleApprovedGuessColor);
					ITsStrFactory fact = TsStrFactoryClass.Create();
					ITsString count = TsStringUtils.MakeTss(fact, Cache.DefaultUserWs, "" + cGlosses);
					//make the number black.
					SetColor(vwenv, 0);
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
										  (int)FwTextPropVar.ktpvMilliPoint, kmpIconMargin);
					vwenv.AddString(count);
				}

				//Set the margin and padding values
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
					(int)FwTextPropVar.ktpvMilliPoint,
					kmpIconMargin);
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing,
					(int)FwTextPropVar.ktpvMilliPoint,
					2000);
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
					(int)FwTextPropVar.ktpvMilliPoint,
					2000);
				SetBGColor(vwenv, krgbEditable);
				vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
					(int)SpellingModes.ksmNormalCheck);
				vwenv.OpenInnerPile();

				// Set the appropriate paragraph direction for the writing system.
				bool fWsRtl = m_caches.MainCache.ServiceLocator.WritingSystemManager.Get(ws).RightToLeftScript;
				if (fWsRtl != RightToLeft)
					vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
						(int)FwTextPropVar.ktpvEnum,
						fWsRtl ? (int)FwTextToggleVal.kttvForceOn : (int)FwTextToggleVal.kttvOff);

				vwenv.AddStringAltMember(ktagSbWordGloss, ws, this);
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
				if (m_fShowMorphBundles)
				{
					// Don't allow direct editing of the morph bundle lines.
					MakeNextFlowObjectReadOnly(vwenv);
					if (vwenv.DataAccess.get_VecSize(hvo, ktagSbWordMorphs) == 0)
					{
						SetColor(vwenv, m_choices.LabelRGBFor(m_choices.IndexOf(InterlinLineChoices.kflidMorphemes)));
						vwenv.AddProp(ktagMissingMorphs, this, kfragMissingMorphs);
						// Blank lines to fill up the gap; LexEntry line
						vwenv.AddString(m_tssEmptyVern);
						vwenv.AddString(m_tssEmptyAnalysis); // LexGloss line
						vwenv.AddString(m_tssEmptyAnalysis); // LexPos line
					}
					else
					{
						vwenv.OpenParagraph();
						vwenv.AddObjVec(ktagSbWordMorphs, this, kfragMorph);
						vwenv.CloseParagraph();
					}
				}
			}

			private void DisplayWordform(IVwEnv vwenv, int ws, int choiceIndex)
			{
				// For the wordform line we only want an icon on the first line (which is always wordform).
				bool fWantIcon = m_sandbox.ShowAnalysisCombo && choiceIndex == 0;
				// This has to be BEFORE we open the paragraph, so the indent applies to the whole
				// paragraph, and not some string box inside it.
				if (!fWantIcon)
					SetIndentForMissingIcon(vwenv);
				vwenv.OpenParagraph();
				// The actual icon, if present, has to be INSIDE the paragraph.
				if (fWantIcon)
					AddPullDownIcon(vwenv, ktagAnalysisIcon);
				//Set the background of the wordform to the 'WordFormBGColor' which is set when ChangeOrCreateSandbox
				//is called
				SetBGColor(vwenv, MultipleOptionBGColor);
				if (ws != m_sandbox.RawWordformWs)
				{
					// Any other Ws we can edit.
					MakeNextFlowObjectEditable(vwenv);
					vwenv.OpenInnerPile(); // So white background occupies full width
					vwenv.AddStringAltMember(ktagSbWordForm, ws, this);
					vwenv.CloseInnerPile();
				}
				else
				{
					MakeNextFlowObjectReadOnly(vwenv);
					//vwenv.AddString(m_sandbox.RawWordform);
					vwenv.AddStringAltMember(ktagSbWordForm, ws, this);
				}
				vwenv.CloseParagraph();
			}

			private void SetEditabilityOfNextFlowObject(IVwEnv vwenv, bool fEditable)
			{
				if (fEditable)
					MakeNextFlowObjectEditable(vwenv);
				else
					MakeNextFlowObjectReadOnly(vwenv);
			}

			private void MakeNextFlowObjectReadOnly(IVwEnv vwenv)
			{
				vwenv.set_IntProperty(
					(int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
			}

			// Allow the next flow object to be edited (and give it a background color that
			// makes it look editable)
			private void MakeNextFlowObjectEditable(IVwEnv vwenv)
			{
				vwenv.set_IntProperty(
					(int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptIsEditable);
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
					(int)FwTextPropVar.ktpvDefault, krgbEditable);
			}

			public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
			{
				CheckDisposed();

				switch (frag)
				{
					case kfragMorph: // The bundle of 4 lines representing a morpheme.
						ISilDataAccess sda = vwenv.DataAccess;
						int cmorph = sda.get_VecSize(hvo, tag);
						for (int i = 0; i < cmorph; ++i)
						{
							int hvoMorph = sda.get_VecItem(hvo, tag, i);
							vwenv.AddObj(hvoMorph, this, i == 0 ? kfragFirstMorph : kfragMorph);
						}
						break;
				}
			}

			public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
			{
				CheckDisposed();

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
			internal int ArrowPicWidth
			{
				get
				{
					CheckDisposed();
					return m_PulldownArrowPic.Picture.Width;
				}
			}


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
			protected void AddOptionalNamedObj(IVwEnv vwenv, int hvo, int tag, int dummyTag,
				int dummyFrag, int tagIcon, int ws, int choiceIndex)
			{
				int hvoNo = vwenv.DataAccess.get_ObjectProp(hvo, tag);
				SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
				bool fWantIcon = false;
				fWantIcon = tagIcon != 0 && m_choices.IsFirstOccurrenceOfFlid(choiceIndex);
				if (m_fIconsForAnalysisChoices && !fWantIcon)
				{
					// This line does not have one, but add some white space to line things up.
					vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent,
						(int)FwTextPropVar.ktpvMilliPoint,
						m_dxmpArrowPicWidth + kmpIconMargin);
				}
				vwenv.OpenParagraph();
				if (fWantIcon)
					AddPullDownIcon(vwenv, tagIcon);
				// The NoteDependency is needed whether or not hvoNo is set, in case we update
				// to a sense which has a null MSA.  See LT-4246.
				vwenv.NoteDependency(new int[] { hvo }, new int[] { tag }, 1);
				if (hvoNo == 0)
					vwenv.AddProp(dummyTag, this, dummyFrag);
				else
					vwenv.AddObjProp(tag, this, kfragNamedObjectNameChoices + choiceIndex);
				vwenv.CloseParagraph();
			}

			internal void UpdateLineChoices(InterlinLineChoices choices)
			{
				m_choices = choices;
			}
		}

	}
}
