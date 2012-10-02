// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewVc.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Implements the view constructor for the draft view (formerly DraftTextVc in file
// DraftWnd.cpp/h)
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Resources; // for check-box icons.
using stdole;

namespace SIL.FieldWorks.TE
{
	#region DraftViewVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class displays the draft view.
	/// NOTE: The DraftStyleBarVc must remain synchronized with this view
	/// down to the paragraph level.
	/// </summary>
	///  ----------------------------------------------------------------------------------------
	public class DraftViewVc: TeStVc
	{
		#region public events
		/// <summary>Fired when an EventHandler event exists.</summary>
		// Declare the hot-link click event signature
		public delegate void HotLinkClickHandler(object sender, string strData, int hvoOwner, int tag,
			ITsString tss, int ichObj);

		/// <summary></summary>
		[Category("Custom")]
		[Description("Occurs when the user clicks on a hot link.")]
		public event HotLinkClickHandler HotLinkClick;
		#endregion

		#region Member Variables
		//??? AfStatusBarPtr m_qstbr; // To report progress in loading data.
		/// <summary></summary>
		protected stdole.IPicture m_UnfinishedPic;
		/// <summary></summary>
		protected stdole.IPicture m_FinishedPic;
		/// <summary></summary>
		protected stdole.IPicture m_CheckedPic;
		/// <summary></summary>
		protected IVwStylesheet m_stylesheet;
		/// <summary></summary>
		protected bool m_fDisplayInTable;
		/// <summary></summary>
		protected bool m_fShowTailingSpace;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DraftViewVc class
		/// </summary>
		/// <param name="target">target of the view (printer or draft)</param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// <param name="styleSheet">Optional stylesheet. Null is okay if this view constructor
		/// promises never to try to display a back translation</param>
		/// <param name="displayInTable">True to display the paragraphs in a table layout,
		/// false otherwise</param>
		/// ------------------------------------------------------------------------------------
		public DraftViewVc(TeStVc.LayoutViewTarget target, int filterInstance,
			IVwStylesheet styleSheet, bool displayInTable) : base(target, filterInstance)
		{
			m_UnfinishedPic = PrepareImage(TeResourceHelper.BackTranslationUnfinishedImage);
			m_FinishedPic = PrepareImage(TeResourceHelper.BackTranslationFinishedImage);
			m_CheckedPic = PrepareImage(TeResourceHelper.BackTranslationCheckedImage);
			m_stylesheet = styleSheet;
			m_fDisplayInTable = displayInTable;
			//m_fLazy = true; // This makes the paragraphs lazy.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare an image by replacing the transparent color with the window color.
		/// </summary>
		/// <param name="img"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private stdole.IPicture PrepareImage(Image img)
		{
			return (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(
				ResourceHelper.ReplaceTransparentColor(img, SystemColors.Window));
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
			Marshal.ReleaseComObject(m_UnfinishedPic);
			m_UnfinishedPic = null;
			Marshal.ReleaseComObject(m_FinishedPic);
			m_FinishedPic = null;
			Marshal.ReleaseComObject(m_CheckedPic);
			m_CheckedPic = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Overridden methods
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
			CheckDisposed();

			switch(frag)
			{
				case (int)FootnoteFrags.kfrScripture:
				case (int)ScrFrags.kfrScripture:
				{
					// This fragment should only be used on full refresh - clear the user prompt
					// flags so they will be shown again.
					ClearUserPromptUpdates();

					// We add this lazy - we will expand some of it immediately, but the non-
					// visible parts will remain lazy!
					vwenv.NoteDependency(new int[]{m_cache.LangProject.TranslatedScriptureOAHvo},
						new int[]{(int)Scripture.ScriptureTags.kflidScriptureBooks}, 1);
					vwenv.AddLazyVecItems(BooksTag, this,
						frag == (int)ScrFrags.kfrScripture ? (int)ScrFrags.kfrBook : (int)FootnoteFrags.kfrBook);

					// Add a 48 point gap at the bottom of the view
					if (!PrintLayout && (frag != (int)FootnoteFrags.kfrScripture))
						vwenv.AddSimpleRect(ColorUtil.ConvertColorToBGR(BackColor), -1, 48000, 0);
					break;
				}
				case (int)ScrFrags.kfrBook:
				{
					vwenv.OpenDiv();
					vwenv.AddObjProp((int)ScrBook.ScrBookTags.kflidTitle, this,
						(int)StTextFrags.kfrText);
					vwenv.AddLazyVecItems((int)ScrBook.ScrBookTags.kflidSections, this,
						(int)ScrFrags.kfrSection);

					// Add a 48 point gap at the bottom of the view
					if (!PrintLayout && m_fShowTailingSpace)
						vwenv.AddSimpleRect(ColorUtil.ConvertColorToBGR(BackColor), -1, 48000, 0);

					if (!PrintLayout)
						InsertBookSeparator(hvo, vwenv);
					vwenv.CloseDiv();
					break;
				}
				case (int)ScrFrags.kfrSection:
				{
					vwenv.OpenDiv();
					vwenv.AddObjProp((int)ScrSection.ScrSectionTags.kflidHeading, this,
						(int)StTextFrags.kfrText);
					vwenv.AddObjProp((int)ScrSection.ScrSectionTags.kflidContent, this,
						(int)StTextFrags.kfrText);
					vwenv.CloseDiv();
					break;
				}
				case (int)StTextFrags.kfrPara:
					if (PrintLayout || !m_fDisplayInTable)
					{
						// We are displaying Scripture or a print layout view
						base.Display(vwenv, hvo, frag);
					}
					else
					{
						// We are displaying a back translation or Scripture in draftview in a table
						// Open a table to display the BT para in column 1, and the icon in column 2.
						VwLength vlTable; // we use this to specify that the table takes 100% of the width.
						vlTable.nVal = 10000;
						vlTable.unit = VwUnit.kunPercent100;

						VwLength vlColumn; // and this one to specify 90% for the text
						vlColumn.nVal = DisplayTranslation ? 9000 : 10000;
						vlColumn.unit = VwUnit.kunPercent100;

						int nColumns = DisplayTranslation ? 2 : 1;

						vwenv.OpenTable(nColumns, // One or two columns.
							vlTable, // Table uses 100% of available width.
							0, // Border thickness.
							VwAlignment.kvaLeft, // Default alignment.
							VwFramePosition.kvfpVoid, // No border.
							//VwFramePosition.kvfpBox,
							//VwRule.kvrlAll, // rule lines between cells
							VwRule.kvrlNone,
							0, //No space between cells.
							0, //No padding inside cells.
							false);

						// Specify column widths. The first argument is the number of columns,
						// not a column index.
						vwenv.MakeColumns(nColumns, vlColumn);
						vwenv.OpenTableBody();
						vwenv.OpenTableRow();

						// Display paragraph in the first cell
						vwenv.OpenTableCell(1, 1);
						InsertParagraphBody(vwenv, hvo, frag, true, ContentType, this);
						vwenv.CloseTableCell();

						if (DisplayTranslation)
						{
							// Stylesheet should never be null for a VC that displays BTs, but to be safe...
							Debug.Assert (m_stylesheet != null);
							if (m_stylesheet != null)
							{
								StPara para = new StPara(m_cache, hvo);
								ITsTextProps styleRules = para.StyleRules;
								if (styleRules == null)
								{
									Debug.Fail("Style Rules should not be null");
									styleRules = NormalStyle;
								}
								string paraStyleName = styleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
								ITsTextProps ttp = m_stylesheet.GetStyleRgch(0, paraStyleName);
								Debug.Assert(ttp != null);
								if (ttp != null)
								{
									int var;
									int spaceBefore = ttp.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out var);
									if (spaceBefore > 0)
										vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, var, spaceBefore);
								}
							}
							// BT status icon in the next cell, not editable
							vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
								(int)TptEditable.ktptNotEditable);
							vwenv.OpenTableCell(1, 1);
							vwenv.AddObjVec((int)StTxtPara.StTxtParaTags.kflidTranslations, this, (int)ScrFrags.kfrBtTranslationStatus);
							vwenv.CloseTableCell();
						}

						// Close table
						vwenv.CloseTableRow();
						vwenv.CloseTableBody();
						vwenv.CloseTable();
					}
					break;
				case (int)ScrFrags.kfrBtTranslationStatus:
				{
					CmTranslation trans = new CmTranslation(m_cache, hvo);
					if (trans != null)
					{
						string status = trans.Status.GetAlternative(m_wsDefault);
						stdole.IPicture picture;
						if (status == BackTranslationStatus.Checked.ToString())
							picture = m_CheckedPic;
						else if (status == BackTranslationStatus.Finished.ToString())
							picture = m_FinishedPic;
						else
							picture = m_UnfinishedPic;

						vwenv.OpenDiv();
						vwenv.AddPicture(picture, -1, 0, 0);
						vwenv.NoteDependency(new int[] {hvo},
							new int[] {(int)CmTranslation.CmTranslationTags.kflidStatus}, 1);
						vwenv.CloseDiv();

					}
				}
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		/// <summary>
		/// Here we have a reference to IText, which can reference ScrFdo, so unlike the base class
		/// we can give a meaningful definition to label segment.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override bool IsLabelSegment(int hvo)
		{
			CmBaseAnnotation seg = (CmBaseAnnotation) CmObject.CreateFromDBObject(Cache, hvo);
			StTxtPara para = seg.BeginObjectRA as StTxtPara;
			return SegmentBreaker.HasLabelText(para.Contents.UnderlyingTsString, seg.BeginOffset, seg.EndOffset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a bar to separate this book from the following book (unless this is the last
		/// book being displayed).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void InsertBookSeparator(int hvoScrBook, IVwEnv vwenv)
		{
			int hvoScr = m_cache.GetOwnerOfObject(hvoScrBook);
			int hvoLastScrBook = m_cache.GetVectorItem(hvoScr, BooksTag,
				m_cache.GetVectorSize(hvoScr, BooksTag) - 1);

			if (hvoLastScrBook != hvoScrBook)
			{
				uint transparent = 0xC0000000; // FwTextColor.kclrTransparent won't convert to uint
				vwenv.AddSimpleRect(transparent, -1, 20000, 0);
				vwenv.AddSimpleRect(ColorUtil.ConvertColorToBGR(ColorUtil.LightInverse(m_BackColor)), -1, 10000, 0);
				vwenv.AddSimpleRect(transparent, -1, 5000, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override of DisplayVec
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			CheckDisposed();

			if (tag == (int)StTxtPara.StTxtParaTags.kflidTranslations &&
				frag == (int)ScrFrags.kfrBtTranslationStatus)
			{
				vwenv.AddObj(GetTranslationForPara(hvo), this, frag);
			}

			else
				base.DisplayVec(vwenv, hvo, tag, frag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load data needed to display the specified objects using the specified fragment.
		/// This is called before attempting to Display an item that has been listed for lazy
		/// display using AddLazyItems. It may be used to load the necessary data into the
		/// DataAccess object.
		/// </summary>
		/// <param name="vwenv">view environment in the state appropriate for the subsequent call to
		/// Display for the first item</param>
		/// <param name="rghvo">the items we want to display</param>
		/// <param name="chvo">number of items we want to display</param>
		/// <param name="hvoParent">HVO of the parent</param>
		/// <param name="tag">the tag we are going to display</param>
		/// <param name="frag">the fragment argument that will be passed to Display to show
		/// each of them</param>
		/// <param name="ihvoMin">the index of the first item in prghvo, in the overall
		/// property. Ignored in this implementation.</param>
		/// ------------------------------------------------------------------------------------
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag,
			int frag, int ihvoMin)
		{
			CheckDisposed();

			base.LoadDataFor(vwenv, rghvo, chvo, hvoParent, tag, frag, ihvoMin);
#if DOTHIS
			// ENHANCE TomB: Implement progress bar
			bool fStartProgressBar = false; // m_stbr && !m_stbr.IsProgressBarActive();
			try
			{
				switch(frag)
				{
					case (int)FootnoteFrags.kfrBook:
					case (int)ScrFrags.kfrBook:
					{
						// REVIEW EberhardB: we might want some optimizations on the loading
						// (see DraftTextVc::LoadDataFor)
						ScrBook scrBook;

						foreach (int hvo in rghvo)
						{
							if (m_cache.GetClassOfObject(hvo) < 1)
								return;
							try
							{
								scrBook = new ScrBook(m_cache, hvo);
//								CmObject.LoadObjectsIntoCache(m_cache, typeof(StTxtPara),
//									scrBook.TitleOA.ParagraphsOS.HvoArray);
							}
							catch(Exception e)
							{
								Debug.WriteLine("Got exception while loading data for book: " + e.Message);
								throw;
							}
						}

						break;
					}
					case (int)ScrFrags.kfrSection:
					{
						// REVIEW EberhardB: optimize, especially implement reading +/- 3 sections
						ScrSection sect;
						foreach (int hvo in rghvo)
						{
							if (m_cache.GetClassOfObject(hvo) < 1)
								return;
							try
							{
//								sect = new ScrSection(m_cache, hvo);

//								CmObject.LoadObjectsIntoCache(m_cache, typeof(StTxtPara),
//									sect.HeadingOA.ParagraphsOS.HvoArray);
//								CmObject.LoadObjectsIntoCache(m_cache, typeof(StTxtPara),
//									sect.ContentOA.ParagraphsOS.HvoArray);
							}
							catch(Exception e)
							{
								Debug.WriteLine("Got exception while loading data for section: " + e.Message);
								throw;
							}
						}

						break;
					}
					case (int)StTextFrags.kfrPara:
					case (int)ScrFrags.kfrParaStyles:
						break;
					default:
						Debug.Assert(false);
						break;
				}
			}
			catch(Exception e)
			{
				Debug.WriteLine("Got exception in LoadDataFor: " + e.Message);
				throw;
			}

			// If we had to start a progress bar, return things to normal.
			if (fStartProgressBar)
			{
				// TODO m_qstbr->EndProgressBar();
			}
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the HotLinkClick event to indicate that the user has clicked a hot link in
		/// the view. Typically, the owning view will handle this event by setting focus to an
		/// object which is the target of the hotlink.
		/// </summary>
		/// <param name="strData"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="tag"></param>
		/// <param name="tss"></param>
		/// <param name="ichObj"></param>
		/// ------------------------------------------------------------------------------------
		public override void DoHotLinkAction(string strData, int hvoOwner, int tag,
			ITsString tss, int ichObj)
		{
			CheckDisposed();

			if (HotLinkClick != null)
				HotLinkClick(this, strData, hvoOwner, tag, tss, ichObj);
			//m_rootsite.DoHotLinkAction(strData, hvoOwner, tag, tss, ichObj);
		}
		#endregion
	}
	#endregion

	#region DraftStyleBarVc
	///  ----------------------------------------------------------------------------------------
	/// <summary>
	/// The class that displays the style bar for the draft view.
	/// </summary>
	///  ----------------------------------------------------------------------------------------
	public class DraftStyleBarVc: DraftViewVc
	{
		private bool m_displayForFootnotes;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for a DraftStyleBarVc.  This is needed to construct the base view
		/// constructor with the proper layout type.
		/// </summary>
		/// <param name="filterInstance">number used to make filters unique per main window</param>
		/// ------------------------------------------------------------------------------------
		public DraftStyleBarVc(int filterInstance) : this(filterInstance, false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for a DraftStyleBarVc.  This is needed to construct the base view
		/// constructor with the proper layout type.
		/// </summary>
		/// <param name="filterInstance">number used to make filters unique per main window</param>
		/// <param name="displayForFootnotes">Only displays the styles for the footnote
		/// paragraphs</param>
		/// ------------------------------------------------------------------------------------
		public DraftStyleBarVc(int filterInstance, bool displayForFootnotes) :
			base(TeStVc.LayoutViewTarget.targetStyleBar, filterInstance, null, false)
		{
			m_displayForFootnotes = displayForFootnotes;
		}

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them.
		/// A Scripture is displayed by displaying its Books;
		/// and a Book is displayed by displaying its Title and Sections;
		/// and a Section is diplayed by displaying its Heading and Content;
		/// which are displayed by showing the style for each paragraph in the StText.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			ISilDataAccess silDataAccess = vwenv.DataAccess;
			int wsUser = Cache.LanguageWritingSystemFactoryAccessor.UserWs;
			switch(frag)
			{
				case (int)FootnoteFrags.kfrBook:
				case (int)ScrFrags.kfrBook:
				{
					vwenv.OpenDiv();
					if (m_displayForFootnotes)
					{
						vwenv.AddObjVecItems((int)ScrBook.ScrBookTags.kflidFootnotes,
							this, (int)FootnoteFrags.kfrFootnoteStyles);
					}
					else
					{
						vwenv.AddObjProp((int)ScrBook.ScrBookTags.kflidTitle, this,
							(int)ScrFrags.kfrTextStyles);
						vwenv.AddLazyVecItems((int)ScrBook.ScrBookTags.kflidSections, this,
							(int)ScrFrags.kfrSection);
						InsertBookSeparator(hvo, vwenv);
					}
					vwenv.CloseDiv();
					break;
				}
				case (int)ScrFrags.kfrSection:
				{
					vwenv.OpenDiv();
					vwenv.AddObjProp((int)ScrSection.ScrSectionTags.kflidHeading, this,
						(int)ScrFrags.kfrTextStyles);
					vwenv.AddObjProp((int)ScrSection.ScrSectionTags.kflidContent, this,
						(int)ScrFrags.kfrTextStyles);
					vwenv.CloseDiv();
					break;
				}
				case (int)ScrFrags.kfrTextStyles:
				{
					// We need to show something, since the current view code can't handle a property
					// containing no boxes.
					if (HandleEmptyText(vwenv, hvo))
						break;
					if (m_fLazy)
					{
						vwenv.AddLazyVecItems((int)StText.StTextTags.kflidParagraphs, this,
							(int)ScrFrags.kfrParaStyles);
					}
					else
					{
						vwenv.AddObjVecItems((int)StText.StTextTags.kflidParagraphs, this,
							(int)ScrFrags.kfrParaStyles);
					}
					break;
				}
				case (int)FootnoteFrags.kfrFootnoteStyles:
				{
					if (HandleEmptyText(vwenv, hvo))
						break;
					vwenv.AddObjVecItems((int)StText.StTextTags.kflidParagraphs, this,
						(int)ScrFrags.kfrParaStyles);
					break;
				}
				case (int)ScrFrags.kfrParaStyles:
				{
					ITsTextProps tsTextProps = silDataAccess.get_UnknownProp(hvo, (int)StPara.StParaTags.kflidStyleRules)
						as ITsTextProps;
					string styleName = StyleNames.ksNormal;
					if (tsTextProps != null)
					{
						styleName = tsTextProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					}
					// To insert it into the view it has to be an ITsString
					ITsStrFactory tsStrFactory =
						TsStrFactoryClass.Create();
					// Last arg is writing system. Should we use English? UI writing system?
					// Note that when we support localization of style names this code will need
					// to be enhanced as the stylename from the TsTextProps will be a raw name or
					// GUID that the user shouldn't see.
					ITsString tssStyle = tsStrFactory.MakeStringRgch(styleName,
						styleName.Length, wsUser);

					// To make the pile align things properly, top and bottom margins for the
					// matching boxes must be the same as the original.

					// A 'concordance' paragraph is a way of preventing wrapping.
					vwenv.OpenConcPara(0, 0, 0, 0);
					vwenv.AddString(tssStyle);
					// Since we looked up this property directly rather than going through the vwenv (which
					// there seems to be no way to do for Unknown-type properties as yet), we need to tell
					// the view to update this paragraph if the properties of the paragraph are changed.
					vwenv.NoteDependency(new int[] {hvo}, new int[] {(int)StPara.StParaTags.kflidStyleRules}, 1);

					vwenv.CloseParagraph();
					break;
				}
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}
		#endregion
	}
	#endregion
}
