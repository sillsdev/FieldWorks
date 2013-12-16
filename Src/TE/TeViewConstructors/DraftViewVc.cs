// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DraftViewVc.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Implements the view constructor for the draft view (formerly DraftTextVc in file
// DraftWnd.cpp/h)
// </remarks>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils.ComTypes;
using SIL.Utils; // for check-box icons.

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
	public class DraftViewVc: TeStVc, IDisposable
	{
		#region public events
		/// <summary>Fired when an EventHandler event exists.</summary>
		// Declare the hot-link click event signature
		public delegate void HotLinkClickHandler(object sender, string strData);

		/// <summary></summary>
		[Category("Custom")]
		[Description("Occurs when the user clicks on a hot link.")]
		public event HotLinkClickHandler HotLinkClick;
		#endregion

		#region Member Variables
		//??? AfStatusBarPtr m_qstbr; // To report progress in loading data.
		/// <summary></summary>
		protected IPicture m_UnfinishedPic;
		/// <summary></summary>
		protected IPicture m_FinishedPic;
		/// <summary></summary>
		protected IPicture m_CheckedPic;
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
		public DraftViewVc(LayoutViewTarget target, int filterInstance,
			IVwStylesheet styleSheet, bool displayInTable) : base(target, filterInstance)
		{
			m_UnfinishedPic = PrepareImage(TeResourceHelper.BackTranslationUnfinishedImage);
			m_FinishedPic = PrepareImage(TeResourceHelper.BackTranslationFinishedImage);
			m_CheckedPic = PrepareImage(TeResourceHelper.BackTranslationCheckedImage);
			m_stylesheet = styleSheet;
			m_fDisplayInTable = displayInTable;
			//m_fLazy = true; // This makes the paragraphs lazy.
		}

		#endregion

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~DraftViewVc()
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
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (fDisposing)
			{
				// Dispose managed resources here.
				var disposable = m_UnfinishedPic as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				disposable = m_FinishedPic as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				disposable = m_CheckedPic as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			if (m_UnfinishedPic != null && Marshal.IsComObject(m_UnfinishedPic))
				Marshal.ReleaseComObject(m_UnfinishedPic);
			m_UnfinishedPic = null;
			if (m_FinishedPic != null && Marshal.IsComObject(m_FinishedPic))
				Marshal.ReleaseComObject(m_FinishedPic);
			m_FinishedPic = null;
			if (m_CheckedPic != null && Marshal.IsComObject(m_CheckedPic))
				Marshal.ReleaseComObject(m_CheckedPic);
			m_CheckedPic = null;

			IsDisposed = true;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare an image by replacing the transparent color with the window color.
		/// </summary>
		/// <param name="img"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static IPicture PrepareImage(Image img)
		{
			using (var bitmap = ResourceHelper.ReplaceTransparentColor(img, SystemColors.Window))
			{
				return (IPicture)OLECvt.ToOLE_IPictureDisp(bitmap);
			}
		}

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
					vwenv.NoteDependency(new[] { hvo }, new[] { ScrBookTags.kflidFootnotes }, 1);

					// This fragment should only be used on full refresh - clear the user prompt
					// flags so they will be shown again.
					ClearUserPromptUpdates();

					// We add this lazy - we will expand some of it immediately, but the non-
					// visible parts will remain lazy!
					vwenv.NoteDependency(new[]{m_cache.LanguageProject.TranslatedScriptureOA.Hvo},
						new[]{ScriptureTags.kflidScriptureBooks}, 1);
					vwenv.AddLazyVecItems(BooksTag, this,
						frag == (int)ScrFrags.kfrScripture ? (int)ScrFrags.kfrBook : (int)FootnoteFrags.kfrBook);

					// Add a 48 point gap at the bottom of the view
					if (!PrintLayout && (frag != (int)FootnoteFrags.kfrScripture))
						vwenv.AddSimpleRect((int)ColorUtil.ConvertColorToBGR(BackColor), -1, 48000, 0);
					break;
				}
				case (int)ScrFrags.kfrBook:
				{
					vwenv.OpenDiv();
					vwenv.AddObjProp(ScrBookTags.kflidTitle, this, (int)StTextFrags.kfrText);
					vwenv.AddLazyVecItems(ScrBookTags.kflidSections, this, (int)ScrFrags.kfrSection);

					// Add a 48 point gap at the bottom of the view
					if (!PrintLayout && m_fShowTailingSpace)
						vwenv.AddSimpleRect((int)ColorUtil.ConvertColorToBGR(BackColor), -1, 48000, 0);

					if (!PrintLayout)
						InsertBookSeparator(hvo, vwenv);
					vwenv.CloseDiv();
					break;
				}
				case (int)ScrFrags.kfrSection:
				{
					vwenv.OpenDiv();
					vwenv.AddObjProp(ScrSectionTags.kflidHeading, this, (int)StTextFrags.kfrText);
					vwenv.AddObjProp(ScrSectionTags.kflidContent, this, (int)StTextFrags.kfrText);
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
								IStPara para = m_cache.ServiceLocator.GetInstance<IStParaRepository>().GetObject(hvo);
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
							vwenv.AddObjVec(StTxtParaTags.kflidTranslations, this, (int)ScrFrags.kfrBtTranslationStatus);
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
						ICmTranslation trans = m_cache.ServiceLocator.GetInstance<ICmTranslationRepository>().GetObject(hvo);
						if (trans != null)
						{
							string status = trans.Status.get_String(m_wsDefault).Text;
							IPicture picture;
							if (status == BackTranslationStatus.Checked.ToString())
								picture = m_CheckedPic;
							else if (status == BackTranslationStatus.Finished.ToString())
								picture = m_FinishedPic;
							else
								picture = m_UnfinishedPic;

							vwenv.OpenDiv();
							vwenv.AddPicture(picture, -1, 0, 0);
							vwenv.NoteDependency(new [] {hvo}, new [] {CmTranslationTags.kflidStatus}, 1);
							vwenv.CloseDiv();
						}
					}
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a bar to separate this book from the following book (unless this is the last
		/// book being displayed).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void InsertBookSeparator(int hvoScrBook, IVwEnv vwenv)
		{
			FilteredScrBooks filter = m_cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(m_filterInstance);
			int hvoLastScrBook = filter.GetBook(filter.BookCount - 1).Hvo;
			if (hvoLastScrBook != hvoScrBook)
			{
				vwenv.AddSimpleRect((int)FwTextColor.kclrTransparent, -1, 20000, 0);
				vwenv.AddSimpleRect((int)ColorUtil.ConvertColorToBGR(ColorUtil.LightInverse(m_BackColor)), -1, 10000, 0);
				vwenv.AddSimpleRect((int)FwTextColor.kclrTransparent, -1, 5000, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override of DisplayVec
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void DisplayVec(IVwEnv vwenv, int paraHvo, int tag, int frag)
		{
			CheckDisposed();

			if (tag == StTxtParaTags.kflidTranslations && frag == (int)ScrFrags.kfrBtTranslationStatus)
			{
				IStTxtPara para = Cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraHvo);
				vwenv.AddObj(GetTranslationForPara(paraHvo), this, frag);
			}
			else
				base.DisplayVec(vwenv, paraHvo, tag, frag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the HotLinkClick event to indicate that the user has clicked a hot link in
		/// the view. Typically, the owning view will handle this event by setting focus to an
		/// object which is the target of the hotlink.
		/// </summary>
		/// <param name="strData"></param>
		/// <param name="sda"></param>
		/// ------------------------------------------------------------------------------------
		public override void DoHotLinkAction(string strData, ISilDataAccess sda)
		{
			CheckDisposed();

			if (HotLinkClick != null)
				HotLinkClick(this, strData);
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
		private readonly bool m_displayForFootnotes;
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
			base(LayoutViewTarget.targetStyleBar,filterInstance, null, false)
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
			int wsUser = Cache.WritingSystemFactory.UserWs;
			switch(frag)
			{
				case (int)FootnoteFrags.kfrBook:
				case (int)ScrFrags.kfrBook:
				{
					vwenv.OpenDiv();
					if (m_displayForFootnotes)
					{
						vwenv.AddObjVecItems(ScrBookTags.kflidFootnotes,
							this, (int)FootnoteFrags.kfrFootnoteStyles);
					}
					else
					{
						vwenv.AddObjProp(ScrBookTags.kflidTitle, this,
							(int)ScrFrags.kfrTextStyles);
						vwenv.AddLazyVecItems(ScrBookTags.kflidSections, this,
							(int)ScrFrags.kfrSection);
						InsertBookSeparator(hvo, vwenv);
					}
					vwenv.CloseDiv();
					break;
				}
				case (int)ScrFrags.kfrSection:
				{
					vwenv.OpenDiv();
					vwenv.AddObjProp(ScrSectionTags.kflidHeading, this,
						(int)ScrFrags.kfrTextStyles);
					vwenv.AddObjProp(ScrSectionTags.kflidContent, this,
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
						vwenv.AddLazyVecItems(StTextTags.kflidParagraphs, this,
							(int)ScrFrags.kfrParaStyles);
					}
					else
					{
						vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this,
							(int)ScrFrags.kfrParaStyles);
					}
					break;
				}
				case (int)FootnoteFrags.kfrFootnoteStyles:
				{
					if (HandleEmptyText(vwenv, hvo))
						break;
					vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this,
						(int)ScrFrags.kfrParaStyles);
					break;
				}
				case (int)ScrFrags.kfrParaStyles:
				{
					var tsTextProps = silDataAccess.get_UnknownProp(hvo, StParaTags.kflidStyleRules)
						as ITsTextProps;
					string styleName = ScrStyleNames.Normal;
					if (tsTextProps != null)
						styleName = tsTextProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);

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
					vwenv.NoteDependency(new[] {hvo}, new[] {StParaTags.kflidStyleRules}, 1);

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
