// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BtPrintLayoutSideBySideVc.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using System;

namespace SIL.FieldWorks.TE
{
	#region IBtPrintLayoutSideBySideVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal interface IBtPrintLayoutSideBySideVc
	{
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
		void InsertParagraphBody(IVwEnv vwenv, int hvo, int frag, bool fApplyProps,
			StVc.ContentTypes contentType, StVc vc);
	}
	#endregion

	#region class BtPrintLayoutSideBySideVcImpl
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Common implementation of the side-by-side view constructor. This class is used by the
	/// Footnote and regular side-by-side VCs.
	/// </summary>
	/// <remarks>Unfortunately generics in .NET don't allow to derive from a parameter type,
	/// so we moved all the implementation stuff for the view constructors in here and have
	/// two very simple real VC classes for footnotes and regular scripture text.</remarks>
	/// ----------------------------------------------------------------------------------------
	internal class BtPrintLayoutSideBySideVcImpl<T> : IDisposable
		where T : TeStVc, IBtPrintLayoutSideBySideVc
	{
		#region Member data
		/// <summary>View Constructor for the BT side</summary>
		private TeStVc m_btVc;
		private readonly FdoCache m_cache;
		/// <summary>The VC that holds a reference to us.</summary>
		private readonly T m_RealVc;
		private readonly int m_bookTag;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the BtPrintLayoutSideBySideVcImpl class.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="filterInstance"></param>
		/// <param name="styleSheet"></param>
		/// <param name="cache"></param>
		/// <param name="btWs">The writing system of the back translation side of the view
		/// </param>
		/// <param name="fForFootnotes">True if we are displaying footnotes, false otherwise
		/// </param>
		/// <param name="realVc">The parent VC</param>
		/// <param name="bookTag">The book tag</param>
		/// ------------------------------------------------------------------------------------
		public BtPrintLayoutSideBySideVcImpl(TeStVc.LayoutViewTarget target, int filterInstance,
			IVwStylesheet styleSheet, FdoCache cache, int btWs, bool fForFootnotes, T realVc,
			int bookTag)
		{
			m_cache = cache;
			m_RealVc = realVc;
			m_bookTag = bookTag;

			m_btVc = (fForFootnotes) ?
				(TeStVc)new FootnoteVc(TeStVc.LayoutViewTarget.targetPrint, filterInstance) :
				new DraftViewVc(target, filterInstance, styleSheet, false);
			m_btVc.DefaultWs = btWs;
			m_btVc.ContentType = Options.UseInterlinearBackTranslation ? StVc.ContentTypes.kctSegmentBT : StVc.ContentTypes.kctSimpleBT;
			m_btVc.Cache = cache;
			m_btVc.PrintLayout = true;
		}

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~BtPrintLayoutSideBySideVcImpl()
		{
			Dispose(false);
		}
#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				var disposable = m_btVc as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_btVc = null;
			IsDisposed = true;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the back translation writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BackTranslationWS
		{
			get { return m_btVc.BackTranslationWS; }
			set
			{
				m_btVc.BackTranslationWS = value;
				WritingSystem defWs = m_cache.ServiceLocator.WritingSystemManager.Get(value);
				m_btVc.RightToLeft = defWs.RightToLeftScript;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them.
		/// A Scripture is displayed by displaying its Books;
		/// and a Book is displayed by displaying its Title and Sections;
		/// and a Section is diplayed by displaying its Heading and Content;
		/// which are displayed by using the standard view constructor for StText.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <returns><c>true</c> if we dealt with the display, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case (int)FootnoteFrags.kfrScripture:
				case (int)ScrFrags.kfrScripture:
					{
						// This fragment should only be used on full refresh - clear the user prompt
						// flags so they will be shown again.
						m_RealVc.ClearUserPromptUpdates();

						// We add this lazy - we will expand some of it immediately, but the non-
						// visible parts will remain lazy!
						vwenv.NoteDependency(new int[] { m_cache.LanguageProject.TranslatedScriptureOA.Hvo },
							new int[] { (int)ScriptureTags.kflidScriptureBooks }, 1);
						vwenv.AddLazyVecItems(m_bookTag, m_RealVc,
							frag == (int)ScrFrags.kfrScripture ? (int)ScrFrags.kfrBook : (int)FootnoteFrags.kfrBook);
						break;
					}
				case (int)ScrFrags.kfrBook:
					{
						vwenv.OpenDiv();
						vwenv.AddObjProp((int)ScrBookTags.kflidTitle, m_RealVc,
							(int)StTextFrags.kfrText);
						vwenv.NoteDependency(new int[] { hvo },
							new int[] { (int)ScrBookTags.kflidSections }, 1);
						vwenv.AddLazyVecItems((int)ScrBookTags.kflidSections, m_RealVc,
							(int)ScrFrags.kfrSection);

						vwenv.CloseDiv();
						break;
					}
				case (int)FootnoteFrags.kfrBook:
					{
						vwenv.OpenDiv();
						vwenv.AddObjVecItems((int)ScrBookTags.kflidFootnotes, m_RealVc,
							(int)StTextFrags.kfrFootnote);
						vwenv.CloseDiv();
						break;
					}
				case (int)ScrFrags.kfrSection:
					{
						vwenv.OpenDiv();
						vwenv.AddObjProp((int)ScrSectionTags.kflidHeading, m_RealVc,
							(int)StTextFrags.kfrText);
						vwenv.AddObjProp((int)ScrSectionTags.kflidContent, m_RealVc,
							(int)StTextFrags.kfrText);
						vwenv.CloseDiv();
						break;
					}
				case (int)StTextFrags.kfrPara:
				case (int)StTextFrags.kfrFootnotePara:
					{
						// Open a table to display the vern para in column 1, and the BT para in column 2.
						VwLength vlTable;
						vlTable.nVal = 10000;
						vlTable.unit = VwUnit.kunPercent100;

						VwLength vlColumn;
						vlColumn.nVal = 5000;
						vlColumn.unit = VwUnit.kunPercent100;

						int nColumns = 2;

						vwenv.OpenTable(nColumns, // One or two columns.
							vlTable, // Table uses 100% of available width.
							0, // Border thickness.
							VwAlignment.kvaLeft, // Default alignment.
							VwFramePosition.kvfpVoid, // No border.
							VwRule.kvrlNone,
							0, //No space between cells.
							0, //No padding inside cells.
							true);

						// Specify column widths. The first argument is the number of columns,
						// not a column index.
						vwenv.MakeColumns(nColumns, vlColumn);
						vwenv.OpenTableBody();
						vwenv.OpenTableRow();

						if (m_RealVc.RightToLeft)
						{
							AddBtParagraph(vwenv, hvo, frag, false);
							AddVernParagraph(vwenv, hvo, frag, true);
						}
						else
						{
							AddVernParagraph(vwenv, hvo, frag, false);
							AddBtParagraph(vwenv, hvo, frag, true);
						}

						// Close table
						vwenv.CloseTableRow();
						vwenv.CloseTableBody();
						vwenv.CloseTable();
						break;
					}
				case (int)StTextFrags.kfrFootnoteReference:
					{
						m_RealVc.DisplayFootnoteReference(vwenv, hvo);
						break;
					}
				default:
					return false;
			}
			return true;
		}

		internal void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag, int frag, int ihvoMin)
		{
			m_btVc.LoadDataFor(vwenv, rghvo, chvo, hvoParent, tag, frag, ihvoMin);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		/// <param name="hvo">HVO</param>
		/// <returns>Writing system</returns>
		/// ------------------------------------------------------------------------------------
		public int GetWritingSystemForHvo(int hvo)
		{
			int classId = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetClsid(hvo);
			if (classId == CmTranslationTags.kClassId || classId == SegmentTags.kClassId)
				return m_btVc.DefaultWs;

			return m_RealVc.DefaultWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the vernacular paragraph to the table.
		/// </summary>
		/// <param name="vwenv">The IVwEnv</param>
		/// <param name="hvo">The hvo of the paragraph</param>
		/// <param name="frag">The frag for the paragraph</param>
		/// <param name="fBorderLeading">if set to <c>true</c> then the border will be on the
		/// leading edge of the paragraph, otherwise it will be on the trailing edge.</param>
		/// ------------------------------------------------------------------------------------
		private void AddVernParagraph(IVwEnv vwenv, int hvo, int frag, bool fBorderLeading)
		{
			vwenv.set_IntProperty(fBorderLeading ? (int)FwTextPropType.ktptBorderLeading :
				(int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 500);
			vwenv.set_IntProperty(fBorderLeading ? (int)FwTextPropType.ktptPadLeading :
				(int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 18000);
			vwenv.OpenTableCell(1, 1);
			m_RealVc.InsertParagraphBody(vwenv, hvo, frag, true, StVc.ContentTypes.kctNormal, m_RealVc);
			vwenv.CloseTableCell();
		}

		StVc.ContentTypes BtContentType
		{
			get { return Options.UseInterlinearBackTranslation ? StVc.ContentTypes.kctSegmentBT : StVc.ContentTypes.kctSimpleBT; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the back translation paragraph to the table.
		/// </summary>
		/// <param name="vwenv">The IVwEnv</param>
		/// <param name="hvo">The hvo of the paragraph</param>
		/// <param name="frag">The frag for the paragraph</param>
		/// <param name="fBorderLeading">if set to <c>true</c> then the border will be on the
		/// leading edge of the paragraph, otherwise it will be on the trailing edge.</param>
		/// ------------------------------------------------------------------------------------
		private void AddBtParagraph(IVwEnv vwenv, int hvo, int frag, bool fBorderLeading)
		{
			vwenv.set_IntProperty(fBorderLeading ? (int)FwTextPropType.ktptBorderLeading :
				(int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 500);
			vwenv.set_IntProperty(fBorderLeading ? (int)FwTextPropType.ktptPadLeading :
				(int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 18000);
			//if (Options.UseInterlinearBackTranslation)
			//{
			//    vwenv.set_IntProperty((int) FwTextPropType.ktptBackColor, (int) FwTextPropVar.ktpvDefault,
			//                          (int)ColorUtil.ConvertColorToBGR(TeResourceHelper.ReadOnlyTextBackgroundColor));
			//}
			vwenv.OpenTableCell(1, 1);
			m_RealVc.InsertParagraphBody(vwenv, hvo, frag, true, BtContentType, m_btVc);
			vwenv.CloseTableCell();
		}
	}
	#endregion

	#region class BtPrintLayoutSideBySideVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// View constructor for side-by-side print layout view of scripture text
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BtPrintLayoutSideBySideVc : DraftViewVc, IBtPrintLayoutSideBySideVc
	{
		#region Member data
		private BtPrintLayoutSideBySideVcImpl<BtPrintLayoutSideBySideVc> m_VcImpl;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BtPrintLayoutSideBySideVc"/> class.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="filterInstance"></param>
		/// <param name="styleSheet"></param>
		/// <param name="cache"></param>
		/// <param name="btWs">The writing system of the back translation side of the view
		/// </param>
		/// ------------------------------------------------------------------------------------
		public BtPrintLayoutSideBySideVc(TeStVc.LayoutViewTarget target, int filterInstance,
			IVwStylesheet styleSheet, FdoCache cache, int btWs) :
			base (target, filterInstance, styleSheet, false)
		{
			Cache = cache;
			PrintLayout = true;

			m_VcImpl = new BtPrintLayoutSideBySideVcImpl<BtPrintLayoutSideBySideVc>(target,
				filterInstance, styleSheet, cache, btWs, false, this, BooksTag);
		}

		/// <summary/>
		protected override void Dispose(bool fDisposing)
		{
			if (fDisposing)
			{
				m_VcImpl.Dispose();
			}
			m_VcImpl = null;
			base.Dispose(fDisposing);
		}

		#region IBtPrintLayoutSideBySideVc implementation
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
		void IBtPrintLayoutSideBySideVc.InsertParagraphBody(IVwEnv vwenv, int hvo, int frag,
			bool fApplyProps, StVc.ContentTypes contentType, StVc vc)
		{
			InsertParagraphBody(vwenv, hvo, frag, fApplyProps, contentType, vc);
		}
		#endregion

		#region Overridden stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the back translation writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int BackTranslationWS
		{
			get { return m_VcImpl.BackTranslationWS; }
			set { m_VcImpl.BackTranslationWS = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them.
		/// A Scripture is displayed by displaying its Books;
		/// and a Book is displayed by displaying its Title and Sections;
		/// and a Section is diplayed by displaying its Heading and Content;
		/// which are displayed by using the standard view constructor for StText.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			if (!m_VcImpl.Display(vwenv, hvo, frag))
				base.Display(vwenv, hvo, frag);
		}

		/// <summary>
		/// Overridden to allow the BtVc to load information required for segment-level BT.
		/// </summary>
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag, int frag, int ihvoMin)
		{
			base.LoadDataFor(vwenv, rghvo, chvo, hvoParent, tag, frag, ihvoMin);
			m_VcImpl.LoadDataFor(vwenv, rghvo, chvo, hvoParent, tag, frag, ihvoMin);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		/// <param name="hvo">HVO</param>
		/// <returns>Writing system</returns>
		/// ------------------------------------------------------------------------------------
		public int GetWritingSystemForHvo(int hvo)
		{
			return m_VcImpl.GetWritingSystemForHvo(hvo);
		}
	}
	#endregion

	#region class BtFootnotePrintLayoutSideBySideVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// View constructor for side-by-side print layout view of footnotes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BtFootnotePrintLayoutSideBySideVc : FootnoteVc, IBtPrintLayoutSideBySideVc, IDisposable
	{
		#region Member data
		private readonly BtPrintLayoutSideBySideVcImpl<BtFootnotePrintLayoutSideBySideVc> m_VcImpl;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BtFootnotePrintLayoutSideBySideVc"/> class.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="filterInstance"></param>
		/// <param name="styleSheet"></param>
		/// <param name="cache"></param>
		/// <param name="btWs">The writing system of the back translation side of the view
		/// </param>
		/// ------------------------------------------------------------------------------------
		public BtFootnotePrintLayoutSideBySideVc(LayoutViewTarget target, int filterInstance,
			IVwStylesheet styleSheet, FdoCache cache, int btWs) : base(target, filterInstance)
		{
			Cache = cache;
			PrintLayout = true;

			m_VcImpl = new BtPrintLayoutSideBySideVcImpl<BtFootnotePrintLayoutSideBySideVc>(target,
				filterInstance, styleSheet, cache, btWs, true, this, BooksTag);
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~BtFootnotePrintLayoutSideBySideVc()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_VcImpl.Dispose();
			}
			IsDisposed = true;
		}
		#endregion
		#region IBtPrintLayoutSideBySideVc implementation
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
		void IBtPrintLayoutSideBySideVc.InsertParagraphBody(IVwEnv vwenv, int hvo, int frag,
			bool fApplyProps, StVc.ContentTypes contentType, StVc vc)
		{
			InsertParagraphBody(vwenv, hvo, frag, fApplyProps, contentType, vc);
		}
		#endregion

		#region Overridden stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the back translation writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int BackTranslationWS
		{
			get { return m_VcImpl.BackTranslationWS; }
			set { m_VcImpl.BackTranslationWS = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them.
		/// A Scripture is displayed by displaying its Books;
		/// and a Book is displayed by displaying its Title and Sections;
		/// and a Section is diplayed by displaying its Heading and Content;
		/// which are displayed by using the standard view constructor for StText.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			if (!m_VcImpl.Display(vwenv, hvo, frag))
				base.Display(vwenv, hvo, frag);
		}
		/// <summary>
		/// Overridden to allow the BtVc to load information required for segment-level BT.
		/// </summary>
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag, int frag, int ihvoMin)
		{
			base.LoadDataFor(vwenv, rghvo, chvo, hvoParent, tag, frag, ihvoMin);
			m_VcImpl.LoadDataFor(vwenv, rghvo, chvo, hvoParent, tag, frag, ihvoMin);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		/// <param name="hvo">HVO</param>
		/// <returns>Writing system</returns>
		/// ------------------------------------------------------------------------------------
		public int GetWritingSystemForHvo(int hvo)
		{
			return m_VcImpl.GetWritingSystemForHvo(hvo);
		}
	}
	#endregion
}
