// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FootnoteVc.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// Implements the view constructor for the footnote view
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using System.Collections.Generic;

namespace SIL.FieldWorks.TE
{
	#region FootnoteVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class displays the footnote view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FootnoteVc : TeStVc
	{
		#region member variables
		ITsTextProps m_ttpNoteGeneral; // TsTextProps for NoteGeneralStyle, created on demand
		ITsTextProps m_ttpNoteCrossRefs; // Note Cross-Reference Paragraph
		IVwStylesheet m_stylesheet; // for computing whether a paragraph is cross-ref.
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the FootnoteVc class
		/// </summary>
		/// <param name="filterInstance">number used to make filters unique for each main
		/// window</param>
		/// <param name="target">The target.</param>
		/// <param name="wsDefault">HVO of the default WS to be used for footnotes</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteVc(int filterInstance, LayoutViewTarget target, int wsDefault)
			: base(target, filterInstance)
		{
			m_wsDefault = wsDefault;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwStylesheet Stylesheet
		{
			get { return m_stylesheet; }
			set { m_stylesheet = value; }
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them.
		/// Scripture Footnotes are displayed by displaying each footnote's reference and text.
		/// The text is displayed using the standard view constructor for StText.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			switch (frag)
			{
				case (int)FootnoteFrags.kfrScripture:
				{
					vwenv.NoteDependency(new int[] { m_cache.LangProject.TranslatedScriptureOAHvo },
						new int[] { (int)Scripture.ScriptureTags.kflidScriptureBooks }, 1);
					vwenv.AddLazyVecItems(BooksTag, this, (int)FootnoteFrags.kfrBook);
					break;
				}
				case (int)FootnoteFrags.kfrRootInPageSeq:
				{
					int tag = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor,
						"Scripture", "FootnotesOnPage",
						(int)CellarModuleDefns.kcptReferenceSequence).Tag;
					// Get the list of footnotes to display
					int[] hvos = m_cache.GetVectorProperty(hvo, tag, true);
					if (hvos.Length > 0)
					{
						int ownerHvo = m_cache.GetOwnerOfObject(hvos[0]);
						// The ownerHvo should be the HVO of the book
						vwenv.NoteDependency(new int[] { ownerHvo },
							new int[] { (int)ScrBook.ScrBookTags.kflidFootnotes }, 1);
					}
					vwenv.AddObjVec(tag, this, (int)FootnoteFrags.kfrAllFootnotesWithinPagePara);
					break;
				}
				case (int)FootnoteFrags.kfrFootnoteWithinPagePara:
				{
					// Note a dependency on the footnote options so that the footnote will
					// be refreshed when these are changed.
					int[] depHvos = { hvo };
					int[] depTags = { StFootnote.ktagFootnoteOptions };
					vwenv.NoteDependency(depHvos, depTags, 1);

					// Insert the marker and reference
					vwenv.AddObj(hvo, this, (int)StTextFrags.kfrFootnoteMarker);
					vwenv.AddObj(hvo,  this,(int)StTextFrags.kfrFootnoteReference);

					// Insert (we hope only one) paragraph contents.
					vwenv.AddObjVecItems((int)StText.StTextTags.kflidParagraphs, this,
						(int)FootnoteFrags.kfrFootnoteParaWithinPagePara);
					break;
				}
				case (int) FootnoteFrags.kfrFootnoteParaWithinPagePara:
				{
					if (!InsertParaContentsUserPrompt(vwenv, hvo))
					{
						// Display the text paragraph contents, or its user prompt.
						vwenv.AddStringProp((int)StTxtPara.StTxtParaTags.kflidContents, null);
					}
					break;
				}
				case (int)FootnoteFrags.kfrBook:
				{
					vwenv.OpenDiv();
					vwenv.AddObjVecItems((int)ScrBook.ScrBookTags.kflidFootnotes, this,
						(int)StTextFrags.kfrFootnote);
					vwenv.CloseDiv();
					break;
				}
				case (int)StTextFrags.kfrFootnoteMarker:
				{
					ScrFootnote footnote = new ScrFootnote(Cache, hvo);
					if (footnote.DisplayFootnoteMarker)
						DisplayFootnoteMarker(vwenv, footnote);
					break;
				}
				case (int)StTextFrags.kfrFootnoteReference:
				{
					ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
					ITsPropsFactory tpf = TsPropsFactoryClass.Create();
					ITsTextProps ttp = tpf.MakeProps(ScrStyleNames.FootnoteTargetRef, m_wsDefault, 0);

					ScrFootnote footnote = new ScrFootnote(m_cache, hvo);
					string footnoteRef = footnote.GetReference(m_wsDefault);
					ITsString tssRef = tsStrFactory.MakeStringWithPropsRgch(footnoteRef,
						footnoteRef.Length, ttp);
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptNotEditable);
					vwenv.AddString(tssRef);
					break;
				}
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is used for displaying vectors in complex ways. Often not used.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			switch (frag)
			{
				case (int)FootnoteFrags.kfrAllFootnotesWithinPagePara:
					InsertSmushedFootnotes(vwenv, hvo, tag);
					break;
				default:
					base.DisplayVec(vwenv, hvo, tag, frag);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the smushed footnotes.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// ------------------------------------------------------------------------------------
		private void InsertSmushedFootnotes(IVwEnv vwenv, int hvo, int tag)
		{
			ISilDataAccess sda = vwenv.DataAccess;
			int chvo = sda.get_VecSize(hvo, tag);
			if (chvo == 0)
				return;
			List<int> notes = new List<int>(chvo);
			for (int ihvo = 0; ihvo < chvo; ihvo++)
			{
				int footnoteHvo = sda.get_VecItem(hvo, tag, ihvo);
				if (sda.get_IsValidObject(footnoteHvo))
					notes.Add(footnoteHvo);
			}

			// TE-6185: Undo can cause the only footnote on the page to no longer be a valid object.
			if (notes.Count == 0)
				return;

			// Footnotes are owned by books which are owned by Scripture.
			int hvoScripture = m_cache.GetOwnerOfObject(m_cache.GetOwnerOfObject(notes[0]));
			int combine = m_cache.GetIntProperty(hvoScripture, (int)Scripture.ScriptureTags.kflidCrossRefsCombinedWithFootnotes);
			if (combine != 0)
				CreateNotesParagraph(vwenv, notes, NoteGeneralParagraphStyle);
			else
			{
				List<int> generalNotes = new List<int>(notes.Count);
				List<int> crNotes = new List<int>(notes.Count);
				foreach (int hvoNote in notes)
				{
					int cPara = sda.get_VecSize(hvoNote, (int)StText.StTextTags.kflidParagraphs);
					if (cPara != 0)
					{
						int hvoFirstPara = sda.get_VecItem(hvoNote, (int)StText.StTextTags.kflidParagraphs, 0);
						ITsTextProps paraStyle = (ITsTextProps)m_cache.GetUnknown(hvoFirstPara, (int)StPara.StParaTags.kflidStyleRules);
						if (paraStyle != null)
						{
							string styleName = paraStyle.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
							while (styleName != null && styleName != "" && styleName != kstrNoteCrossRefStyleName && m_stylesheet != null)
								styleName = m_stylesheet.GetBasedOn(styleName);
							if (styleName == kstrNoteCrossRefStyleName)
							{
								crNotes.Add(hvoNote);
								continue;
							}
						}
					}
					generalNotes.Add(hvoNote);
				}
				CreateNotesParagraph(vwenv, generalNotes, NoteGeneralParagraphStyle);
				CreateNotesParagraph(vwenv, crNotes, NoteCrossRefParaStyle);
			}
		}

		private void CreateNotesParagraph(IVwEnv vwenv, List<int> notes, ITsTextProps styleProps)
		{
			// Don't create a completely empty paragraph. The way the Views system handles such paragraphs
			// doesn't work for a MappedTaggedPara. Also, we want the footnote to take up no height at all
			// if there are none.
			if (notes.Count == 0)
				return;
			vwenv.set_IntProperty((int)FwTextPropType.ktptBaseWs,
				(int)FwTextPropVar.ktpvDefault, DefaultWs);
			vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
				(int)FwTextPropVar.ktpvEnum, (RightToLeft ? -1 : 0));
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvEnum,
				RightToLeft ? (int)FwTextAlign.ktalRight : (int)FwTextAlign.ktalLeft);
			vwenv.Props = styleProps;

			// The body of the paragraph is either editable or not.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum,
				Editable ? (int)TptEditable.ktptIsEditable
				: (int)TptEditable.ktptNotEditable);

			vwenv.OpenMappedTaggedPara();
			for (int ihvo = 0; ihvo < notes.Count; ihvo++)
			{
				// Optimize JohnT: could make this string once and save it in a member variable.
				if (ihvo != 0)
					vwenv.AddString(m_cache.MakeVernTss("  "));
				vwenv.AddObj(notes[ihvo], this, (int)FootnoteFrags.kfrFootnoteWithinPagePara);
			}
			vwenv.CloseParagraph();
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent,
			int tag, int frag, int ihvoMin)
		{
			CheckDisposed();

			string text;
			try
			{
				switch ((FootnoteFrags)frag)
				{
					case FootnoteFrags.kfrBook:
					{
						ScrBook scrBook;

						// The range of hvo's are for scripture books. Loop through the list of
						// requested books (i.e. requested by the views framework).
						foreach (int hvo in rghvo)
						{
							try
							{
								scrBook = new ScrBook(m_cache, hvo);

								// Loop through all the footnotes in the book.
								foreach (StFootnote stFootnote in scrBook.FootnotesOS)
								{
									if (ContentType == ContentTypes.kctSegmentBT)
										LoadDataForStText(stFootnote);
									// This serves only to load the data from the database into the
									// cache. That's because referencing an fdo cache property forces
									// its data to be loaded if it hasn't been already.
									foreach (StTxtPara stPara in stFootnote.ParagraphsOS)
										text = stPara.Contents.Text;
								}
							}
							catch(Exception e)
							{
								Debug.WriteLine("Got exception while loading footnotes for book: " + e.Message);
								throw;
							}
						}

						break;
					}
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
		}
		#endregion

		#region Private Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the footnote marker.
		/// </summary>
		/// <param name="vwenv">View environment</param>
		/// <param name="footnote">The footnote.</param>
		/// ------------------------------------------------------------------------------------
		private void DisplayFootnoteMarker(IVwEnv vwenv, ScrFootnote footnote)
		{
			vwenv.NoteDependency(new int[] { footnote.Hvo },
				new int[] { (int)StFootnote.StFootnoteTags.kflidFootnoteMarker }, 1);
			// The footnote marker is not editable.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum,
				(int)TptEditable.ktptNotEditable);

			ITsStrBldr strBldr = footnote.MakeFootnoteMarker(DefaultWs);
			strBldr.Replace(strBldr.Length, strBldr.Length, " ", null);
			vwenv.AddString(strBldr.GetString());
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// A TsTextProps that invokes the named style "Note General Paragraph". Created when first needed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected ITsTextProps NoteGeneralParagraphStyle
		{
			get
			{
				if (m_ttpNoteGeneral == null)
				{
					ITsPropsBldr tsPropsBuilder =
						TsPropsBldrClass.Create();

					tsPropsBuilder.SetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle, "Note General Paragraph");
					m_ttpNoteGeneral = tsPropsBuilder.GetTextProps();
				}
				return m_ttpNoteGeneral;
			}
		}

		const string kstrNoteCrossRefStyleName = "Note Cross-Reference Paragraph";

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// A TsTextProps that invokes the named style "Note Cross-Reference Paragraph". Created when first needed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected ITsTextProps NoteCrossRefParaStyle
		{
			get
			{
				if (m_ttpNoteCrossRefs == null)
				{
					ITsPropsBldr tsPropsBuilder =
						TsPropsBldrClass.Create();

					tsPropsBuilder.SetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle, kstrNoteCrossRefStyleName);
					m_ttpNoteCrossRefs = tsPropsBuilder.GetTextProps();
				}
				return m_ttpNoteCrossRefs;
			}
		}		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote marker.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetFootnoteMarker(int index)
		{
			return "" + (char)((int)'a' + (index % 26));
		}
		#endregion
	}
	#endregion
}
