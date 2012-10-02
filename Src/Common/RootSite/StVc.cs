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
// Last reviewed: Never
//
// <remarks>
// Implementation of StVc, a standard view constructor for displaying StText's
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

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
	public class StVc : VwBaseVc
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
		/// <summary>The view construtor's cache.</summary>
		protected FdoCache m_cache = null;
		/// <summary>The view construtor's height estimator</summary>
		protected IHeightEstimator m_heightEstimator;
		/// <summary>State controlling if view is editable or read-only</summary>
		private bool m_editable;
		private ContentTypes m_contentType = ContentTypes.kctNormal;
		private bool m_printLayout = false;
		#endregion

		#region Constructors
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor for StVc
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public StVc() : base()
		{
			m_editable = true;	// the view is editable by default
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for StVc that takes a paragraph style name and default writing system
		/// for empty paragraphs.
		/// </summary>
		/// <param name="sStyleName">paragraph style name</param>
		/// <param name="wsDefault">default writing system for empty paragraphs</param>
		/// -----------------------------------------------------------------------------------
		public StVc(string sStyleName, int wsDefault) : this()
		{
			m_wsDefault = wsDefault;
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

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
			m_sDefaultParaStyle = null;
			if (m_ttpNormal != null)
			{
				Marshal.ReleaseComObject(m_ttpNormal);
				m_ttpNormal = null;
			}
			m_tssLabel = null; // Whoever set it owns it, and needs to clear it.
			m_heightEstimator = null; // Whoever set it owns it, and needs to clear it.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the back translation writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int BackTranslationWS
		{
			get
			{
				CheckDisposed();
				return DefaultWs;
			}
			set
			{
				CheckDisposed();
				DefaultWs = value;
			}
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
			get
			{
				CheckDisposed();
				return m_heightEstimator;
			}
			set
			{
				CheckDisposed();
				m_heightEstimator = value;
			}
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
						(int)FwTextPropType.ktptNamedStyle,	StyleNames.ksNormal);
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
			get
			{
				CheckDisposed();
				return (m_sDefaultParaStyle == null) ? string.Empty : m_sDefaultParaStyle;
			}
			set
			{
				CheckDisposed();
				m_sDefaultParaStyle = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the background color for the StText
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Color BackColor
		{
			get
			{
				CheckDisposed();
				return m_BackColor;
			}
			set
			{
				CheckDisposed();
				m_BackColor = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the label to be stuck at start of first paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ITsString Label
		{
			get
			{
				CheckDisposed();
				return m_tssLabel;
			}
			set
			{
				CheckDisposed();
				m_tssLabel = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the overall text direction.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public bool RightToLeft
		{
			get
			{
				CheckDisposed();
				return m_fRtl;
			}
			set
			{
				CheckDisposed();
				m_fRtl = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets if data should be loaded lazily.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public bool Lazy
		{
			get
			{
				CheckDisposed();
				return m_fLazy;
			}
			set
			{
				CheckDisposed();
				m_fLazy = value;
			}
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
				CheckDisposed();
				if (Cache != null && value > 0 && DefaultWs != value)
				{
					LgWritingSystem defWs = new LgWritingSystem(m_cache, value);
					RightToLeft = defWs.RightToLeft;
				}
				base.DefaultWs = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the FDO cache for the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
			set
			{
				CheckDisposed();

				m_cache = value;
				if (m_wsDefault <= 0)
					m_wsDefault = m_cache.DefaultVernWs;
				if (m_wsDefault > 0)
				{
					LgWritingSystem defWs = new LgWritingSystem(m_cache, m_wsDefault);
					RightToLeft = defWs.RightToLeft;
				}
				this.LangProjectHvo = m_cache.LangProject.Hvo;
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
			get
			{
				CheckDisposed();
				return m_editable;
			}
			set
			{
				CheckDisposed();
				m_editable = value;
			}
		}

		/// <summary>
		/// Controls which of the three kinds of paragraph content is displayed.
		/// </summary>
		public virtual ContentTypes ContentType
		{
			get
			{
				CheckDisposed();
				return m_contentType;
			}
			set
			{
				CheckDisposed();
				m_contentType = value;
			}
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
			get
			{
				return ContentType != ContentTypes.kctNormal;
			}
			set
			{
				ContentType = value ? ContentTypes.kctSimpleBT : ContentTypes.kctNormal;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets an indicator that the view is for a print layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool PrintLayout
		{
			get
			{
				CheckDisposed();
				return m_printLayout;
			}
			set
			{
				CheckDisposed();
				m_printLayout = value;
			}
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// We need to show something, since the current view code can't handle a property
		/// containing no boxes.  Check to see if the StText is missing or if there are
		/// no paragraphs.  If so, then put in an empty string placeholder.
		/// REVIEW: Should we prevent the occurrence of texts with no paragraphs?
		/// </summary>
		/// <param name="vwenv">view environment</param>
		/// <param name="hvo">id of object to be displayed</param>
		/// <returns>true if an empty string was substituted for missing/empty StText</returns>
		/// -----------------------------------------------------------------------------------
		protected bool HandleEmptyText(IVwEnv vwenv, int hvo)
		{
			int paraCount = 0;
			if (hvo != 0)
				paraCount = vwenv.DataAccess.get_VecSize(
					hvo, (int)StText.StTextTags.kflidParagraphs);
			if (paraCount == 0)
			{
				// Either we have no ST object at all, or it is empty of paragraphs. The
				// current view code can't handle either, so stick something in.
				// ENHANCE JohnT: come up with a real solution. This makes it look right,
				// but we should (a) be able to edit and have the first paragraph and
				// if necessary the text itself be created; and (b) if someone adds a real
				// paragraph and/or text in some other view, have them show up.
				ITsStrFactory tsStrFactory =
					TsStrFactoryClass.Create();
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
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a user prompt in the current paragraph if desired.
		/// </summary>
		/// <remarks>Derived classes should override this method if they want to display
		/// user prompts e.g. for empty paragraphs. The default implementation does nothing.
		/// </remarks>
		/// <param name="vwenv">view environment</param>
		/// <param name="hvo">ID of the paragraph</param>
		/// <returns><c>true</c> if user prompt inserted, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool InsertParaContentsUserPrompt(IVwEnv vwenv, int hvo)
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
		/// <param name="hvo">ID of the translation</param>
		/// <returns><c>true</c> if user prompt inserted, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool InsertTranslationUserPrompt(IVwEnv vwenv, int hvo)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert end of paragraph marks if needed.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void InsertEndOfParaMarks(IVwEnv vwenv, int hvo)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the appropriate "OpenPara" method.  This function is virtual to allow
		/// an override to create a different type of paragraph.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvoPara">the StTxtPara for which we want a paragraph</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OpenPara(IVwEnv vwenv, int hvoPara)
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
		/// <param name="hvo"></param>
		/// <param name="vc">The view constructor</param>
		/// ------------------------------------------------------------------------------------
		protected static void ApplyParagraphStyleProps(IVwEnv vwenv, int hvo, StVc vc)
		{
			ITsTextProps tsTextProps = (ITsTextProps)vwenv.DataAccess.get_UnknownProp(hvo,
				(int)StPara.StParaTags.kflidStyleRules);

			// Decide what style to apply to the paragraph.
			// Rules:
			//	1. Apply the paragraph's own style, or "Normal" if it has none.
			//	2. If the creator of the view constructor specified a default style
			//		and background color, invoke those as overrides.
			if (tsTextProps == null)
			{
				// Client didn't spec, and nothing on the para, default to normal.
				tsTextProps = vc.NormalStyle;
			}
			vwenv.Props = tsTextProps;
			if (vc.DefaultParaStyle.Length > 0)
			{
				vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle,
					vc.DefaultParaStyle);
				vwenv.set_IntProperty((int)FwTextPropType.ktptParaColor,
					(int)FwTextPropVar.ktpvDefault, vc.BackColor.ToArgb());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert the body of a paragraph. This is normally (with fApplyProps true) the body
		/// of case kfrPara and kfrFootnotePara in the Display method, but some subclasses
		/// need to separate this from applying the properties.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="fApplyProps"></param>
		/// <param name="contentType"></param>
		/// <param name="vc">The view constructor used to create the paragraphs</param>
		/// ------------------------------------------------------------------------------------
		protected void InsertParagraphBody(IVwEnv vwenv, int hvo, int frag, bool fApplyProps,
			ContentTypes contentType, StVc vc)
		{
			vc.SetupWsAndDirectionForPara(vwenv, hvo);

			if (fApplyProps)
				ApplyParagraphStyleProps(vwenv, hvo, vc);

			// This was causing assertions in the layoutmgr
			// TODO (TE-5777): Should be able to do this with an in-memory stylesheet.
			//			if (DisplayTranslation)
			//			{
			//				// display the back translation text as double spaced
			//				vwenv.set_IntProperty((int)FwTextPropType.ktptLineHeight,
			//					(int)FwTextPropVar.ktpvRelative, 20000);
			//			}
			// The body of the paragraph is either editable or not.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum,
				vc.Editable ? (int)TptEditable.ktptIsEditable
				: (int)TptEditable.ktptNotEditable);
			// Make the paragraph containing the paragraph contents.
			OpenPara(vwenv, hvo);
			// Cause a regenerate when the style changes...this is mainly used for Undo.
			vwenv.NoteDependency(new int[] {hvo},
				new int[] {(int)StPara.StParaTags.kflidStyleRules}, 1);
			// Insert the label if it is the first paragraph.
			if (vc.Label != null)
			{
				int lev = vwenv.EmbeddingLevel;
				int hvoOuter;
				int ihvoItem;
				int tagOuter;
				vwenv.GetOuterObject(lev - 1, out hvoOuter, out tagOuter, out ihvoItem);
				if (ihvoItem == 0)
					vwenv.AddObj(hvo, vc, (int)StTextFrags.kfrLabel);
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
				int[] depHvos = { hvoOuter };
				int[] depTags = { StFootnote.ktagFootnoteOptions };
				vwenv.NoteDependency(depHvos, depTags, 1);
				// If this is the 0th paragraph in the footnote...
				if (ihvoItem == 0)
				{
					vwenv.AddObj(hvoOuter, vc, (int)StTextFrags.kfrFootnoteMarker);
					vwenv.AddObj(hvoOuter, /*vc.Parent != null ? vc.Parent :*/ vc,
						(int)StTextFrags.kfrFootnoteReference);
				}
			}

			if (contentType == ContentTypes.kctSimpleBT)
			{
				// If a translation is being shown instead of the paragraph, then show it instead
				// of the text of the paragraph.
				int transHvo = GetTranslationForPara(hvo);
				vwenv.AddObj(transHvo, vc, (int)StTextFrags.kfrTranslation);
				vwenv.NoteDependency(new int[] {hvo},
					new int[] {(int)StTxtPara.StTxtParaTags.kflidContents}, 1);
			}
			else if (contentType == ContentTypes.kctSegmentBT)
			{
				InsertBtSegments(vc, vwenv, hvo);
			}
			else if (!InsertParaContentsUserPrompt(vwenv, hvo))
			{
				// Display the text paragraph contents, or its user prompt.
				vwenv.AddStringProp((int)StTxtPara.StTxtParaTags.kflidContents, null);
			}

			// Display an "end-of-paragraph" marker if needed
			InsertEndOfParaMarks(vwenv, hvo);

			vwenv.CloseParagraph();
		}

		/// <summary>
		/// Insert the back translation segments. TeStVc overrides to make sure they are loaded properly.
		/// </summary>
		protected virtual void InsertBtSegments(StVc vc, IVwEnv vwenv, int hvo)
		{
			vwenv.AddObjVecItems(StTxtPara.SegmentsFlid(Cache), vc, (int)StTextFrags.kfrSegmentFreeTranslations);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the BaseWs and RightToLeft properties for the paragraph that is being laid out.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvoPara">The hvo para (not used in base implementation).</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void SetupWsAndDirectionForPara(IVwEnv vwenv, int hvoPara)
		{
			bool fIsRightToLeftPara;
			int wsPara;
			GetWsAndDirectionForPara(hvoPara, out fIsRightToLeftPara, out wsPara);

			// This sets the current default paragraph writing system from the relevant field spec.
			// It will only be applied if the paragraph itself lacks a writing system.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBaseWs,
				(int)FwTextPropVar.ktpvDefault, wsPara);
			vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
				(int)FwTextPropVar.ktpvEnum,
				fIsRightToLeftPara ? (int)FwTextToggleVal.kttvForceOn : (int)FwTextToggleVal.kttvOff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system and direction to use for the given paragraph (depending on
		/// the value of <see cref="BaseDirectionOnParaContents"/>, this can be based on the
		/// paragraph itself or simply derived from the VC).
		/// </summary>
		/// <param name="hvoPara">The HVO of the paragraph.</param>
		/// <param name="fIsRightToLeftPara">flag indicating whether direction is right to left.
		/// </param>
		/// <param name="wsPara">The writing system.</param>
		/// ------------------------------------------------------------------------------------
		protected void GetWsAndDirectionForPara(int hvoPara, out bool fIsRightToLeftPara,
			out int wsPara)
		{
			fIsRightToLeftPara = RightToLeft;
			wsPara = DefaultWs;
			if (BaseDirectionOnParaContents)
			{
				// we'll infer the direction of the paragraph from the ws of the first run (offset 0).
				wsPara = StTxtPara.GetWsAtParaOffset(m_cache, hvoPara, 0);
				if (wsPara <= 0)
					wsPara = DefaultWs;
				else
				{
					IWritingSystem wsObj = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsPara);
					if (wsObj != null)
						fIsRightToLeftPara = wsObj.RightToLeft;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the given paragraph should be laid out as right-to-left (depending
		/// on the value of <see cref="BaseDirectionOnParaContents"/>, this can be based on the
		/// paragraph itself or simply derived from the VC).
		/// </summary>
		/// <param name="hvoPara">The HVO of the paragraph.</param>
		/// ------------------------------------------------------------------------------------
		protected bool IsParaRightToLeft(int hvoPara)
		{
			bool fIsRightToLeftPara;
			int wsPara;
			GetWsAndDirectionForPara(hvoPara, out fIsRightToLeftPara, out wsPara);
			return fIsRightToLeftPara;
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
			CheckDisposed();

			switch(frag)
			{
				case (int)StTextFrags.kfrFootnote:
				{
					if (HandleEmptyText(vwenv, hvo))
						break;
					vwenv.AddObjVecItems((int)StText.StTextTags.kflidParagraphs, this,
						(int)StTextFrags.kfrFootnotePara);
					break;
				}

				case (int)StTextFrags.kfrText:
				{
					if (HandleEmptyText(vwenv, hvo))
						break;
					if (m_fLazy)
					{
						vwenv.AddLazyVecItems((int)StText.StTextTags.kflidParagraphs, this,
							(int)StTextFrags.kfrPara);
					}
					else
					{
						vwenv.AddObjVecItems((int)StText.StTextTags.kflidParagraphs, this,
							(int)StTextFrags.kfrPara);
					}
					break;
				}
				case (int)StTextFrags.kfrFootnoteMarker:
				{
					StFootnote footnote = new StFootnote(Cache, hvo);
					if (footnote.DisplayFootnoteMarker)
						DisplayFootnoteMarker(vwenv);
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
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvEnum,
						Editable ? (int)TptEditable.ktptIsEditable
						: (int)TptEditable.ktptNotEditable);
					// Display the translation, or its user prompt
					if (!InsertTranslationUserPrompt(vwenv, hvo))
					{
						vwenv.AddStringAltMember((int)CmTranslation.CmTranslationTags.kflidTranslation,
							m_wsDefault, this);
					}
					break;
				}
				case (int)StTextFrags.kfrSegmentFreeTranslations:
					// Hvo is a CmBaseAnnotation of one segment of an StTxtPara.
					if (IsLabelSegment(hvo))
					{
						CmBaseAnnotation segment = (CmBaseAnnotation)CmBaseAnnotation.CreateFromDBObject(Cache, hvo, false);
						vwenv.AddString(GetBackTransLabelText(segment));
						vwenv.AddSimpleRect(ColorUtil.ConvertColorToBGR(this.BackColor), 1200, 0, 0); // a narrow space, font-neutral
					}
					else
					{
						vwenv.AddObjProp(StTxtPara.SegmentFreeTranslationFlid(Cache), this, (int)StTextFrags.kfrFreeTrans);
						vwenv.AddString(OneSpaceString);
					}
					break;
				case (int)StTextFrags.kfrFreeTrans:
					// Hvo is a CmIndirectAnnotation whose Contents are the free/back translation.
					vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault,
						(int)ColorUtil.ConvertColorToBGR(SystemColors.Window));
					ITsString tssVal = vwenv.DataAccess.get_MultiStringAlt(hvo,
						(int) CmAnnotation.CmAnnotationTags.kflidComment, BackTranslationWS);
					if (tssVal.Length == 0 && !SuppressPrompt(hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment))
					{
						vwenv.NoteDependency(new int[] {hvo}, new int[] {(int)CmAnnotation.CmAnnotationTags.kflidComment}, 1);
						vwenv.AddProp(SimpleRootSite.kTagUserPrompt, this, (int)CmAnnotation.CmAnnotationTags.kflidComment);
						// Almost invisibly narrow, but the Views code doesn't know it is invisible so it should prevent the prompts collapsing
						// into the margin.
						vwenv.AddSimpleRect(ColorUtil.ConvertColorToBGR(this.BackColor), 100, 0, 0);
					}
					else
					{
						ITsStrBldr bldr = tssVal.GetBldr();
						bldr.Replace(0, bldr.Length, "", null); // reduce to empty string in ws.
						// We want it to change back to the prompt if all is deleted.
						vwenv.NoteStringValDependency(hvo, (int) CmAnnotation.CmAnnotationTags.kflidComment, BackTranslationWS, bldr.GetString());
						vwenv.AddStringAltMember((int)CmAnnotation.CmAnnotationTags.kflidComment, BackTranslationWS, this);
						// This little separator is useful here, too. Temporarily the comment may be displayed this way even when empty,
						// and if there is ordinary text following, it is difficult to get an IP displayed in an empty run.
						vwenv.AddSimpleRect(ColorUtil.ConvertColorToBGR(this.BackColor), 100, 0, 0);
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
		/// <param name="segment"></param>
		/// <returns></returns>
		protected virtual ITsString GetBackTransLabelText(CmBaseAnnotation segment)
		{
			return segment.TextAnnotated;
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

		private ITsString m_oneSpaceString;
		ITsString OneSpaceString
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

		/// <summary>
		/// Returns true if the segment is a label and should display the original text uneditable.
		/// DraftViewVc overrides.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected virtual bool IsLabelSegment(int hvo)
		{
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item in pixels. The item will be
		/// one of those you have added to the environment using AddLazyItems. Note that the
		/// calling code does NOT ensure that data for displaying the item in question has been
		/// loaded. This method attempts to estimate how much vertical space is needed to
		/// display this item in the available width.
		/// Note that the number of items expanded and laid out is the window height divided by
		/// the estimated height of an item. Therefore a low estimate leads to laying out too
		/// much; a high estimate merely leads to multiple expansions of the lazy box (much
		/// less expensive). Therefore we err on the high side and guess 10 16-pixel lines per
		/// paragraph.
		///
		/// </summary>
		/// <param name="hvo">id of object for which estimate is to be done</param>
		/// <param name="frag">fragment of data</param>
		/// <param name="dxpAvailWidth">Width of data layout area in pixels</param>
		/// -----------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxpAvailWidth)
		{
			CheckDisposed();

			return (HeightEstimator == null) ? 120 :
				HeightEstimator.EstimateHeight(hvo, frag, dxpAvailWidth);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Static method to create a new structured text. It creates an StText object owned by
		/// hvoOwner in property tag, then creates an StTxtPara owned by the new StText. It
		/// sets the contents of the paragraph to be an empty string in the specified writing system,
		/// and Normal paragraph style.
		/// </summary>
		/// ENHANCE JohnT: probably we will identify styles by something other than name.
		/// REVIEW JohnT(TomB): Are we supposed to be supplying a style name rather than just
		/// using "Normal"?
		///
		/// <param name="cache">FieldWorks database access</param>
		/// <param name="hvoOwner">id of object to own the new StText</param>
		/// <param name="propTag">property (field) type of the new StText</param>
		/// <param name="ws">language writing system of empty paragraph</param>
		/// <returns>HVO of the newly created StText object</returns>
		/// -----------------------------------------------------------------------------------
		public static int MakeEmptyStText(FdoCache cache, int hvoOwner,	int propTag, int ws)
		{
			// REVIEW TomB: Lastparm should really be null if Randy changes CreateObject.

			// Response from RandyR: I changed CreateObject. Null should work for
			// everything now.
			// Most of this code could be moved into the FDO objects, if desired.
			int hvoStText = cache.CreateObject(StText.kclsidStText, hvoOwner, propTag,	0);
			int hvoPara = cache.CreateObject(StTxtPara.kclsidStTxtPara, hvoStText,
				(int)StText.StTextTags.kflidParagraphs,	0);

			// Set the style of the paragraph to Normal
			ITsTextProps ttpNormal;
			ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				StyleNames.ksNormal);
			ttpNormal = tsPropsBldr.GetTextProps();
			cache.MainCacheAccessor.SetUnknown(hvoPara,
				(int)StPara.StParaTags.kflidStyleRules, ttpNormal);

			// Set its contents to an empty string in the right writing system.
			ITsStrFactory tsFactory = TsStrFactoryClass.Create();
			cache.SetTsStringProperty(hvoPara, (int)StTxtPara.StTxtParaTags.kflidContents,
				tsFactory.MakeStringRgch("", 0, ws));

			return hvoStText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the hvo of the (for now, first) translation of a paragraph
		/// </summary>
		/// <param name="hvoPara"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected int GetTranslationForPara(int hvoPara)
		{
			StTxtPara para = new StTxtPara(Cache, hvoPara);
			return para.GetOrCreateBT().Hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the footnote marker
		/// </summary>
		/// <param name="vwenv">View environment</param>
		/// ------------------------------------------------------------------------------------
		private void DisplayFootnoteMarker(IVwEnv vwenv)
		{
			// The footnote marker is not editable.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum,
				(int)TptEditable.ktptNotEditable);
			vwenv.AddStringProp((int)StFootnote.StFootnoteTags.kflidFootnoteMarker, null);

			// add a read-only space after the footnote marker
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum,
				(int)TptEditable.ktptNotEditable);
			ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
			strBldr.Append(" ");
			vwenv.AddString(strBldr.GetString());
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
