// --------------------------------------------------------------------------------------------
#region Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StVc.cs
// Responsibility: TomB
//
// <remarks>
// Implementation of StVc, a standard view constructor for displaying StText's
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Drawing;
using System.Runtime.InteropServices;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Possible text fragments
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public enum StTextFrags
	{
		/// <summary>The whole text</summary>
		kfrText,
		/// <summary>regular paragraph</summary>
		kfrPara,
		/// <summary>Footnote</summary>
		kfrFootnote,
		/// <summary>Footnote reference</summary>
		kfrFootnoteReference,
		/// <summary>Footnote marker</summary>
		kfrFootnoteMarker,
		/// <summary>Footnote paragraph</summary>
		kfrFootnotePara,
		/// <summary>CmTranslation instance of a paragraph</summary>
		kfrTranslation,
		/// <summary>contents of the translation text</summary>
		kfrTranslatedParaContents,
		/// <summary>Label for the beginning of the first paragraph of an StText</summary>
		kfrLabel,
		/// <summary>Segments of an StTextPara, each displaying its free translation annotation.  </summary>
		kfrSegmentFreeTranslations,
		/// <summary>Free translation of a segment. </summary>
		kfrFreeTrans,
	};

	/// ---------------------------------------------------------------------------------------
	/// <remarks>
	/// StVc is  a standard view constructor for displaying StText's.
	/// </remarks>
	/// ---------------------------------------------------------------------------------------
	public class StVc : FwBaseVc
	{
		#region Member variables

		/// <summary>A TsTextProps that invokes the named style "Normal." Created when first
		/// needed.</summary>
		protected ITsTextProps m_ttpNormal;
		/// <summary></summary>
		protected string m_sDefaultParaStyle = string.Empty;
		/// <summary>Color for paragraph background.</summary>
		/// <remarks>If we don't initialize this, we will get black borders around paragraphs
		/// unless the caller always remembers to set the bacground color.</remarks>
		protected Color m_BackColor = SystemColors.Window;
		/// <summary>Label to be stuck at start of first paragraph.</summary>
		protected ITsString m_tssLabel;
		/// <summary>overall text direction</summary>
		protected bool m_fRtl = false;
		/// <summary><c>true</c> if paragraphs are displayed lazily</summary>
		protected bool m_fLazy;
		/// <summary>The view construtor's height estimator</summary>
		protected IHeightEstimator m_heightEstimator;
		/// <summary>State controlling if view is editable or read-only</summary>
		private bool m_editable = true;
		private ContentTypes m_contentType = ContentTypes.kctNormal;
		private bool m_printLayout = false;
		private ITsString m_oneSpaceString;
		#endregion

		#region Constructors
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor for StVc
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public StVc()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for StVc that takes a paragraph style name and default writing system
		/// for empty paragraphs.
		/// </summary>
		/// <param name="wsDefault">default writing system for empty paragraphs</param>
		/// -----------------------------------------------------------------------------------
		public StVc(int wsDefault) : base(wsDefault)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for StVc that takes a paragraph style name and default writing system
		/// for empty paragraphs.
		/// </summary>
		/// <param name="sStyleName">paragraph style name</param>
		/// <param name="wsDefault">default writing system for empty paragraphs</param>
		/// -----------------------------------------------------------------------------------
		public StVc(string sStyleName, int wsDefault) : this(wsDefault)
		{
			DefaultParaStyle = sStyleName;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for StVc that takes a paragraph style name, default writing system for empty
		/// paragraphs, and a background color.
		/// </summary>
		/// <param name="sStyleName">paragraph style name</param>
		/// <param name="wsDefault">default writing system for empty paragraphs</param>
		/// <param name="backColor">background color for paragraph</param>
		/// -----------------------------------------------------------------------------------
		public StVc(string sStyleName, int wsDefault, Color backColor) :
			this (sStyleName, wsDefault)
		{
			m_BackColor = backColor;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the back translation writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int BackTranslationWS
		{
			get { return DefaultWs; }
			set { DefaultWs = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to set the base WS and direction according to the
		/// first run in the paragraph contents.
		/// </summary>
		/// <value>
		/// 	<c>true</c> to base the direction on para contents; <c>false</c> to use the
		/// 	default writing system of the view constructor.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public virtual bool BaseDirectionOnParaContents
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the height estimator for the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IHeightEstimator HeightEstimator
		{
			get { return m_heightEstimator; }
			set { m_heightEstimator = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// A TsTextProps that invokes the named style "Normal." Created when first needed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected ITsTextProps NormalStyle
		{
			get
			{
				if (m_ttpNormal == null)
				{
					ITsPropsBldr tsPropsBuilder =
						TsPropsBldrClass.Create();

					tsPropsBuilder.SetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle,	StyleServices.NormalStyleName);
					m_ttpNormal = tsPropsBuilder.GetTextProps();
				}
				return m_ttpNormal;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the default paragraph style used for displaying the StText. The string
		/// returned by the getter is always non-null.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public string DefaultParaStyle
		{
			get { return m_sDefaultParaStyle ?? string.Empty; }
			set { m_sDefaultParaStyle = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the background color for the StText
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Color BackColor
		{
			get { return m_BackColor; }
			set { m_BackColor = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the label to be stuck at start of first paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ITsString Label
		{
			get { return m_tssLabel; }
			set { m_tssLabel = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the overall text direction.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public bool RightToLeft
		{
			get { return m_fRtl; }
			set { m_fRtl = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets if data should be loaded lazily.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public bool Lazy
		{
			get { return m_fLazy; }
			set { m_fLazy = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the default writing system id for the view constructor. Only the setter
		/// is overridden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int DefaultWs
		{
			set
			{
				if (m_cache != null && value > 0 && DefaultWs != value)
				{
					IWritingSystem defWs = m_cache.ServiceLocator.WritingSystemManager.Get(value);
					RightToLeft = defWs.RightToLeftScript;
				}
				base.DefaultWs = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the FDO cache for the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
#if __MonoCS__ // TODO-Linux: Work around Mono compiler bug.
			get
			{
				return base.Cache;
			}
#endif
			set
			{
				base.Cache = value;
				if (m_wsDefault > 0)
				{
					var defWs = m_cache.ServiceLocator.WritingSystemManager.Get(m_wsDefault);
					RightToLeft = defWs.RightToLeftScript;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the editable state for the VC.  If set to false then all the
		/// text in the view will be read-only.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Editable
		{
			get { return m_editable; }
			set { m_editable = value; }
		}

		/// <summary>
		/// Controls which of the three kinds of paragraph content is displayed.
		/// </summary>
		public virtual ContentTypes ContentType
		{
			get { return m_contentType; }
			set { m_contentType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets option to display the translation of StTxtPara rather than Contents.
		/// On reading, true is ambiguous as to which kind of translation; on writing, true
		/// sets it to simple for now.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DisplayTranslation
		{
			get { return ContentType != ContentTypes.kctNormal; }
			set { ContentType = value ? ContentTypes.kctSimpleBT : ContentTypes.kctNormal; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets an indicator that the view is for a print layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool PrintLayout
		{
			get { return m_printLayout; }
			set { m_printLayout = value; }
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// We need to show something, since the current view code can't handle a property
		/// containing no boxes.  Check to see if the StText is missing or if there are
		/// no paragraphs.  If so, then put in an empty string placeholder.
		/// TODO (FWR-1688): If we prevent the occurrence of texts with no paragraphs in FDO,
		/// this code can be removed.
		/// </summary>
		/// <param name="vwenv">view environment</param>
		/// <param name="hvo">id of object to be displayed</param>
		/// <returns>true if an empty string was substituted for missing/empty StText</returns>
		/// -----------------------------------------------------------------------------------
		protected bool HandleEmptyText(IVwEnv vwenv, int hvo)
		{
			if (hvo == 0 || vwenv.DataAccess.get_VecSize(hvo, StTextTags.kflidParagraphs) > 0)
				return false;

			// Either we have no ST object at all, or it is empty of paragraphs. The
			// current view code can't handle either, so stick something in.
			// ENHANCE JohnT: come up with a real solution. This makes it look right,
			// but we should (a) be able to edit and have the first paragraph and
			// if necessary the text itself be created; and (b) if someone adds a real
			// paragraph and/or text in some other view, have them show up.
			ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
			int ws = vwenv.DataAccess.WritingSystemFactory.UserWs;
			ITsString tssMissing = tsStrFactory.MakeStringRgch("", 0, ws);
			vwenv.set_IntProperty((int)FwTextPropType.ktptParaColor,
			(int)FwTextPropVar.ktpvDefault, m_BackColor.ToArgb());
			vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle,
			m_sDefaultParaStyle);
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
			(int)FwTextPropVar.ktpvEnum,
			(int)TptEditable.ktptNotEditable);
			// This sets the current default writing system from the relevant field spec.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBaseWs,
			(int)FwTextPropVar.ktpvDefault, m_wsDefault);
			vwenv.AddString(tssMissing);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a user prompt in the current paragraph if desired.
		/// </summary>
		/// <remarks>Derived classes should override this method if they want to display
		/// user prompts e.g. for empty paragraphs. The default implementation does nothing.
		/// </remarks>
		/// <param name="vwenv">view environment</param>
		/// <param name="paraHvo">the HVO of the paragraph</param>
		/// <returns><c>true</c> if user prompt inserted, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool InsertParaContentsUserPrompt(IVwEnv vwenv, int paraHvo)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a user prompt in the current translation if desired.
		/// </summary>
		/// <remarks>Derived classes should override this method if they want to display
		/// user prompts e.g. for empty translations. The default implementation does nothing.
		/// </remarks>
		/// <param name="vwenv">view environment</param>
		/// <param name="trans">the translation</param>
		/// <returns><c>true</c> if user prompt inserted, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool InsertTranslationUserPrompt(IVwEnv vwenv, ICmTranslation trans)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert end of paragraph marks if needed.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="paraHvo"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void InsertEndOfParaMarks(IVwEnv vwenv, int paraHvo)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the appropriate "OpenPara" method.  This function is virtual to allow
		/// an override to create a different type of paragraph.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="paraHvo">the HVO of the paragraph</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OpenPara(IVwEnv vwenv, int paraHvo)
		{
			vwenv.OpenMappedTaggedPara();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invoke the paragraph's style rule if any. The properties will apply to the next
		/// flow object--typically a paragraph, but in one TE case a picture--opened or added
		/// next.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="paraHvo">The HVO of the paragraph</param>
		/// <param name="vc">The view constructor</param>
		/// ------------------------------------------------------------------------------------
		protected static void ApplyParagraphStyleProps(IVwEnv vwenv, int paraHvo, StVc vc)
		{
			// Decide what style to apply to the paragraph.
			// Rules:
			//	1. Apply the paragraph's own style, or "Normal" if it has none.
			//	2. If the creator of the view constructor specified a default style
			//		and background color, invoke those as overrides.
			ITsTextProps tsTextProps = (ITsTextProps)vwenv.DataAccess.get_UnknownProp(paraHvo,
				StParaTags.kflidStyleRules);

			if (vc.DefaultParaStyle.Length > 0)
			{
				vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle,
					vc.DefaultParaStyle);
				vwenv.set_IntProperty((int)FwTextPropType.ktptParaColor,
					(int)FwTextPropVar.ktpvDefault, vc.BackColor.ToArgb());
			}
			// The style the user has explicitly set on the paragraph should override the VC's default style.
			vwenv.Props = tsTextProps ?? vc.NormalStyle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert the body of a paragraph. This is normally (with fApplyProps true) the body
		/// of case kfrPara and kfrFootnotePara in the Display method, but some subclasses
		/// need to separate this from applying the properties.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="paraHvo"></param>
		/// <param name="frag"></param>
		/// <param name="fApplyProps"></param>
		/// <param name="contentType"></param>
		/// <param name="vc">The view constructor used to create the paragraphs</param>
		/// ------------------------------------------------------------------------------------
		protected void InsertParagraphBody(IVwEnv vwenv, int paraHvo, int frag, bool fApplyProps,
			ContentTypes contentType, StVc vc)
		{
			vc.SetupWsAndDirectionForPara(vwenv, paraHvo);

			if (fApplyProps)
				ApplyParagraphStyleProps(vwenv, paraHvo, vc);

			// This was causing assertions in the layoutmgr
			// TODO (TE-5777): Should be able to do this with an in-memory stylesheet.
			//			if (DisplayTranslation)
			//			{
			//				// display the back translation text as double spaced
			//				vwenv.set_IntProperty((int)FwTextPropType.ktptLineHeight,
			//					(int)FwTextPropVar.ktpvRelative, 20000);
			//			}
			// The body of the paragraph is either editable or not.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
				vc.Editable ? (int)TptEditable.ktptIsEditable : (int)TptEditable.ktptNotEditable);
			// Make the paragraph containing the paragraph contents.
			OpenPara(vwenv, paraHvo);
			// Cause a regenerate when the style changes...this is mainly used for Undo.
			vwenv.NoteDependency(new[] {paraHvo}, new[] {StParaTags.kflidStyleRules}, 1);
			// Insert the label if it is the first paragraph.
			if (vc.Label != null)
			{
				int lev = vwenv.EmbeddingLevel;
				int hvoOuter;
				int ihvoItem;
				int tagOuter;
				vwenv.GetOuterObject(lev - 1, out hvoOuter, out tagOuter, out ihvoItem);
				if (ihvoItem == 0)
					vwenv.AddObj(paraHvo, vc, (int)StTextFrags.kfrLabel);
			}
			if (frag == (int)StTextFrags.kfrFootnotePara)
			{
				int lev = vwenv.EmbeddingLevel;
				int hvoOuter;
				int ihvoItem;
				int tagOuter;
				vwenv.GetOuterObject(lev - 1, out hvoOuter, out tagOuter, out ihvoItem);
				// Note a dependency on the footnote options so that the footnote will
				// be refreshed when these are changed.
				// If this is the 1st paragraph in the footnote...
				if (ihvoItem == 0)
				{
					vwenv.AddObj(hvoOuter, vc, (int)StTextFrags.kfrFootnoteMarker);
					vwenv.AddObj(hvoOuter, vc, (int)StTextFrags.kfrFootnoteReference);
				}
			}

			if (contentType == ContentTypes.kctSimpleBT)
			{
				// If a translation is being shown instead of the paragraph, then show it instead
				// of the text of the paragraph.
				vwenv.AddObj(GetTranslationForPara(paraHvo), vc, (int)StTextFrags.kfrTranslation);
				if (!PrintLayout)
				{
					// This dependency is here so that the "Missing" prompt will be added to the
					// view when the first character is typed in the contents. But to solve the
					// problem with losing the IP when typing (FWR-1415), the dependency is not
					// added in print layout views. The missing prompt seems less of a problem
					// than the problem with typing.
					vwenv.NoteDependency(new[]{paraHvo}, new[]{StTxtParaTags.kflidContents}, 1);
				}
			}
			else if (contentType == ContentTypes.kctSegmentBT)
			{
				vwenv.AddObjVecItems(StTxtParaTags.kflidSegments, vc, (int)StTextFrags.kfrSegmentFreeTranslations);
			}
			else if (!InsertParaContentsUserPrompt(vwenv, paraHvo))
			{
				// Display the text paragraph contents, or its user prompt.
				vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
			}

			// Display an "end-of-paragraph" marker if needed
			InsertEndOfParaMarks(vwenv, paraHvo);

			vwenv.CloseParagraph();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the BaseWs and RightToLeft properties for the paragraph that is being laid out.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="paraHvo">The HVO of the paragraph.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void SetupWsAndDirectionForPara(IVwEnv vwenv, int paraHvo)
		{
			bool fIsRightToLeftPara;
			int wsPara;
			GetWsAndDirectionForPara(paraHvo, out fIsRightToLeftPara, out wsPara);

			// This sets the current default paragraph writing system from the relevant field spec.
			// It will only be applied if the paragraph itself lacks a writing system.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBaseWs, (int)FwTextPropVar.ktpvDefault, wsPara);
			vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum,
				fIsRightToLeftPara ? (int)FwTextToggleVal.kttvForceOn : (int)FwTextToggleVal.kttvOff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system and direction to use for the given paragraph (depending on
		/// the value of <see cref="BaseDirectionOnParaContents"/>, this can be based on the
		/// paragraph itself or simply derived from the VC).
		/// </summary>
		/// <param name="paraHvo">The HVO of the paragraph.</param>
		/// <param name="fIsRightToLeftPara">flag indicating whether direction is right to left.
		/// </param>
		/// <param name="wsPara">The writing system.</param>
		/// ------------------------------------------------------------------------------------
		protected void GetWsAndDirectionForPara(int paraHvo, out bool fIsRightToLeftPara,
			out int wsPara)
		{
			fIsRightToLeftPara = RightToLeft;
			wsPara = DefaultWs;
			if (BaseDirectionOnParaContents)
			{
				// we'll infer the direction of the paragraph from the ws of the first run (offset 0).
				IStTxtPara para = Cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraHvo);
				wsPara = StringUtils.GetWsAtOffset(para.Contents, 0);
				if (wsPara <= 0)
					wsPara = DefaultWs;
				else
				{
					fIsRightToLeftPara = m_cache.ServiceLocator.WritingSystemManager.Get(wsPara).RightToLeftScript;
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them.
		/// Here a text is displayed by displaying its paragraphs;
		/// and a paragraph is displayed by invoking its style rule, making a paragraph,
		/// and displaying its contents.
		/// </summary>
		/// <param name="vwenv">view environment</param>
		/// <param name="hvo">id of object to be displayed</param>
		/// <param name="frag">fragment of data</param>
		/// -----------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch(frag)
			{
				case (int)StTextFrags.kfrFootnote:
				{
					if (HandleEmptyText(vwenv, hvo))
						break;
					IStFootnote footnote = Cache.ServiceLocator.GetInstance<IStFootnoteRepository>().GetObject(hvo);
					if (footnote.DisplayFootnoteMarker)
					{
						// We need to note this dependency here (for the update of the footnote
						// marker) instead of in the frag for the marker because noting the
						// dependency at the frag for the marker caused some weird problems
						// with the VwNotifiers which caused the view to sometimes update
						// incorrectly. (FWR-1299) It also makes more sense for it to be here
						// since the dependency would be on the whole footnote in either case
						// anyways.
						vwenv.NoteDependency(new int[] { footnote.Owner.Hvo }, new int[] { footnote.OwningFlid }, 1);
					}
					vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this,
						(int)StTextFrags.kfrFootnotePara);
					break;
				}

				case (int)StTextFrags.kfrText:
				{
					if (HandleEmptyText(vwenv, hvo))
						break;
					if (hvo == 0)
						return; // leave view empty, better than crashing.
					if (m_fLazy)
					{
						vwenv.AddLazyVecItems(StTextTags.kflidParagraphs, this,
							(int)StTextFrags.kfrPara);
					}
					else
					{
						vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this,
							(int)StTextFrags.kfrPara);
					}
					break;
				}
				case (int)StTextFrags.kfrFootnoteMarker:
				{
					IStFootnote footnote = Cache.ServiceLocator.GetInstance<IStFootnoteRepository>().GetObject(hvo);
					if (footnote.DisplayFootnoteMarker)
						DisplayFootnoteMarker(vwenv, footnote);
					break;
				}
				case (int)StTextFrags.kfrLabel:
				{
					// The label is not editable.
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptNotEditable);
					vwenv.AddString(m_tssLabel);
					break;
				}

				case (int)StTextFrags.kfrPara:
				case (int)StTextFrags.kfrFootnotePara:
				{
					InsertParagraphBody(vwenv, hvo, frag, true, ContentType, this);
					break;
				}

				case (int)StTextFrags.kfrTranslation:
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
						Editable ? (int)TptEditable.ktptIsEditable : (int)TptEditable.ktptNotEditable);
					// Display the translation, or its user prompt
					ICmTranslation trans = Cache.ServiceLocator.GetInstance<ICmTranslationRepository>().GetObject(hvo);
					if (!InsertTranslationUserPrompt(vwenv, trans))
						vwenv.AddStringAltMember(CmTranslationTags.kflidTranslation, m_wsDefault, this);
					break;
				}
				case (int)StTextFrags.kfrSegmentFreeTranslations:
					// Hvo is one segment of an StTxtPara.
					ISegment seg = Cache.ServiceLocator.GetInstance<ISegmentRepository>().GetObject(hvo);
					if (seg.IsLabel)
					{
						// Added dependencies to get labels to update automatically (FWR-1341, FWR-1342, FWR-1417)
						vwenv.NoteStringValDependency(seg.Paragraph.Hvo, StTxtParaTags.kflidContents, 0, seg.Paragraph.Contents);
						vwenv.NoteDependency(new [] {seg.Paragraph.Hvo}, new [] {StTxtParaTags.kflidSegments}, 1);
						vwenv.AddString(GetBackTransLabelText(seg));
						vwenv.AddSimpleRect((int)ColorUtil.ConvertColorToBGR(BackColor), 1200, 0, 0); // a narrow space, font-neutral
					}
					else
					{
						// Hvo is a segment whose Contents are the free/back translation.
						vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault,
							(int)ColorUtil.ConvertColorToBGR(SystemColors.Window));
						ITsString tssVal = seg.FreeTranslation.get_String(BackTranslationWS);
						if (tssVal.Length == 0 && !SuppressPrompt(hvo, SegmentTags.kflidFreeTranslation))
						{
							vwenv.NoteDependency(new[] { hvo }, new[] { SegmentTags.kflidFreeTranslation }, 1);
							vwenv.AddProp(SimpleRootSite.kTagUserPrompt, this, SegmentTags.kflidFreeTranslation);
							// Almost invisibly narrow, but the Views code doesn't know it is invisible so it should prevent the prompts collapsing
							// into the margin.
							vwenv.AddSimpleRect((int)ColorUtil.ConvertColorToBGR(BackColor), 100, 0, 0);
						}
						else
						{
							ITsStrBldr bldr = tssVal.GetBldr();
							bldr.Replace(0, bldr.Length, "", null); // reduce to empty string in ws.
							// We want it to change back to the prompt if all is deleted.
							vwenv.NoteStringValDependency(hvo, SegmentTags.kflidFreeTranslation, BackTranslationWS, bldr.GetString());
							vwenv.AddStringAltMember(SegmentTags.kflidFreeTranslation, BackTranslationWS, this);
							// This little separator is useful here, too. Temporarily the comment may be displayed this way even when empty,
							// and if there is ordinary text following, it is difficult to get an IP displayed in an empty run.
							vwenv.AddSimpleRect((int)ColorUtil.ConvertColorToBGR(BackColor), 100, 0, 0);
						}
						vwenv.AddString(OneSpaceString);
					}
					break;
			}
		}

		/// <summary>
		/// Given that segment is a 'label' segment (typically an embedded identifying number),
		/// figure out what to display as the corresponding label in a back translation view.
		/// This is made separate so that TeStVc can override to use a different numbering scheme
		/// in the BT.
		/// </summary>
		protected virtual ITsString GetBackTransLabelText(ISegment segment)
		{
			return segment.BaselineText;
		}

		/// <summary>
		/// Suppress displaying a user prompt (typically because the user typed backspace or delete at the prompt).
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		protected virtual bool SuppressPrompt(int hvo, int tag)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a TS string that can be used to separate pieces of a paragraph that is being
		/// built up from different properties (cached for efficiency).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ITsString OneSpaceString
		{
			get
			{
				if (m_oneSpaceString == null)
				{
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					m_oneSpaceString = tsf.MakeString(" ", DefaultWs);
				}
				return m_oneSpaceString;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item in points. The item will be
		/// one of those you have added to the environment using AddLazyItems. Note that the
		/// calling code does NOT ensure that data for displaying the item in question has been
		/// loaded. This method attempts to estimate how much vertical space is needed to
		/// display this item in the available width.
		/// Note that the number of items expanded and laid out is the window height divided by
		/// the estimated height of an item. Therefore a low estimate leads to laying out too
		/// much; a high estimate merely leads to multiple expansions of the lazy box (much
		/// less expensive). Therefore we err on the high side and guess 10 16-pixel lines per
		/// paragraph.
		/// </summary>
		/// <param name="hvo">id of object for which estimate is to be done</param>
		/// <param name="frag">fragment of data</param>
		/// <param name="dxpAvailWidth">Width of data layout area in points</param>
		/// -----------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxpAvailWidth)
		{
			return (HeightEstimator == null) ? 120 :
				HeightEstimator.EstimateHeight(hvo, frag, dxpAvailWidth);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the hvo of the (for now, back translation) translation of a paragraph
		/// </summary>
		/// <param name="paraHvo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected int GetTranslationForPara(int paraHvo)
		{
			IStTxtPara para = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraHvo);
			return para.GetOrCreateBT().Hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the footnote marker
		/// </summary>
		/// <param name="vwenv">View environment</param>
		/// <param name="footnote">The footnote.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisplayFootnoteMarker(IVwEnv vwenv, IStFootnote footnote)
		{
			// The footnote marker is not editable.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			vwenv.AddStringProp(StFootnoteTags.kflidFootnoteMarker, null);

			// add a read-only space after the footnote marker
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			vwenv.AddString(OneSpaceString);
		}

		/// <summary>
		/// Possible values for the ContentType property.
		/// </summary>
		public enum ContentTypes
		{
			/// <summary> Display actual content of paragraphs.</summary>
			kctNormal,
			/// <summary> Display back translation from selected CmTranslation.</summary>
			kctSimpleBT,
			///<summary> Display back translation recorded in Comment of FreeTranslation of Segment</summary>
			kctSegmentBT
		}
	}
}
