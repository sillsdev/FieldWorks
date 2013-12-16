// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DummyDraftViewVc.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Possible scripture fragments
	/// </summary>
	public enum ScrFrags: int
	{
		/// <summary></summary>
		kfrGroup = 100, // debug is easier if different range from tags
		/// <summary>Scripture reference</summary>
		kfrScrRef,
		/// <summary>Detail line</summary>
		kfrDetailLine,
		/// <summary>Reference paragraph</summary>
		kfrRefPara,
		/// <summary>Scripture</summary>
		kfrScripture,
		/// <summary>A book</summary>
		kfrBook,
		/// <summary>A section</summary>
		kfrSection,
		/// <summary>Context</summary>
		kfrContext,
		/// <summary>Count</summary>
		kfrCount,
	};

	/// <summary>
	/// The class that displays the draft view.
	/// </summary>
	public class DummyDraftViewVc: VwBaseVc
	{
		#region Variables
		/// <summary>The structured text view constructor that we will use</summary>
		protected StVc m_stvc;
//		/// <summary>User view used for loading data</summary>
//		protected UserView m_uvs;
		///??? AfStatusBarPtr m_qstbr; // To report progress in loading data.
		/// <summary>Data access into which to load data when LoadDataFor is called</summary>
		protected FdoCache m_fdoCache;
		#endregion

		/// <summary>
		/// Initializes a new instance of the DummyBasicViewVc class
		/// </summary>
		public DummyDraftViewVc()
		{
			m_stvc = new StVc();
		}

		#region Overridden methods
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
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			switch((ScrFrags)frag)
			{
				case ScrFrags.kfrScripture:
				{
					vwenv.AddLazyVecItems((int)Scripture.ScriptureTags.kflidScriptureBooks, this,
						(int)ScrFrags.kfrBook);
					break;
				}
				case ScrFrags.kfrBook:
				{
					vwenv.AddObjProp((int)ScrBook.ScrBookTags.kflidTitle, m_stvc,
						(int)StTextFrags.kfrText);
					vwenv.AddLazyVecItems((int)ScrBook.ScrBookTags.kflidSections, this,
						(int)ScrFrags.kfrSection);
					break;
				}
				case ScrFrags.kfrSection:
				{
					vwenv.AddObjProp((int)ScrSection.ScrSectionTags.kflidHeading, m_stvc,
						(int)StTextFrags.kfrText);
					vwenv.AddObjProp((int)ScrSection.ScrSectionTags.kflidContent, m_stvc,
						(int)StTextFrags.kfrText);
					break;
				}
				default:
					Debug.Assert(false);
					break;
			}
		}

		/// <summary>
		/// This routine is used to estimate the height of an item. The item will be one of
		/// those you have added to the environment using AddLazyItems. Note that the calling
		/// code does NOT ensure that data for displaying the item in question has been loaded.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available width.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns>Height of an item</returns>
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			switch((ScrFrags)frag)
			{
				case ScrFrags.kfrBook:
					return 2000;
				case ScrFrags.kfrSection:
					return 2000;
				default:
					Debug.Assert(false);
					return -1;
			}
		}

		/// <summary>
		/// Load data needed to display the specified objects using the specified fragment.
		/// This is called before attempting to Display an item that has been listed for lazy
		/// display using AddLazyItems. It may be used to load the necessary data into the
		/// DataAccess object.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="rghvo"></param>
		/// <param name="chvo"></param>
		/// <param name="hvoParent"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// <param name="ihvoMin"></param>
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag,
			int frag, int ihvoMin)
		{
			CheckDisposed();

			// ENHANCE TomB: Implement progress bar
			bool fStartProgressBar = false; // m_stbr && !m_stbr.IsProgressBarActive();

			switch((ScrFrags)frag)
			{
				case ScrFrags.kfrBook:
				{
					// REVIEW EberhardB: we might want some optimizations on the loading
					// (see DraftTextVc::LoadDataFor)
					ScrBook scrBook;

					foreach (int hvo in rghvo)
					{
						scrBook = new ScrBook(m_stvc.Cache, hvo);
						foreach (StTxtPara stPara in scrBook.TitleOA.ParagraphsOS)
						{
							string text = stPara.Contents.Text;
						}
					}

					break;
				}
				case ScrFrags.kfrSection:
				{
					// REVIEW EberhardB: optimize, especially implement reading +/- 3 paragraphs
					ScrSection sect;

					foreach (int hvo in rghvo)
					{
						sect = new ScrSection(m_stvc.Cache, hvo);

						foreach (StTxtPara stPara in sect.HeadingOA.ParagraphsOS)
						{
							string text = stPara.Contents.Text;
						}
						foreach (StTxtPara stPara in sect.ContentOA.ParagraphsOS)
						{
							string text = stPara.Contents.Text;
						}
					}

					break;
				}
				default:
					Debug.Assert(false);
					break;
			}

			// If we had to start a progress bar, return things to normal.
			if (fStartProgressBar)
			{
				// TODO m_qstbr->EndProgressBar();
			}

		}
		#endregion

		#region Other methods

		/// <summary>
		/// Set the data access
		/// </summary>
		/// <param name="fdoCache">FDO cache object</param>
		public void SetDa(FdoCache fdoCache)
		{
			CheckDisposed();

			if (m_stvc.Cache == null)
			{
				m_stvc.Cache = fdoCache;
				m_stvc.DefaultWs = fdoCache.DefaultUserWs;
			}
		}
		#endregion

	}
}
