// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FootnoteVc.cs
// Responsibility: TE Team
//
// <remarks>
// Implements the view constructor for the footnote view
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE
{
	#region FootnoteVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class displays the footnote view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FootnoteVc : TeStVc, IDependentObjectsVc
	{
		#region member variables
		ITsTextProps m_ttpNoteGeneral; // TsTextProps for NoteGeneralStyle, created on demand
		ITsTextProps m_ttpNoteCrossRefs; // Note Cross-Reference Paragraph
		IVwStylesheet m_stylesheet; // for computing whether a paragraph is cross-ref.
		int m_startingFootnoteIndex = -1;
		int m_endingFootnoteIndex = -1;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the FootnoteVc class
		/// </summary>
		/// <param name="target">The target.</param>
		/// <param name="filterInstance">number used to make filters unique for each main
		/// window</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteVc(LayoutViewTarget target, int filterInstance)
			: base(target, filterInstance)
		{
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
			switch (frag)
			{
				case (int)StTextFrags.kfrFootnote:
					{
						// FWR-1640: Make the sequence of footnote paragraphs non-editable
						// since we only allow one para per footnote. This will cause
						// pasting multiple paragraphs to work correctly.
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
							(int)FwTextPropVar.ktpvEnum,
							(int)TptEditable.ktptNotEditable);
						base.Display(vwenv, hvo, frag);
						break;
					}
				case (int)FootnoteFrags.kfrScripture:
				{
					vwenv.NoteDependency(new int[] { m_cache.LanguageProject.TranslatedScriptureOA.Hvo },
						new int[] { (int)ScriptureTags.kflidScriptureBooks }, 1);
					vwenv.AddLazyVecItems(BooksTag, this, (int)FootnoteFrags.kfrBook);
					break;
				}
				case (int)FootnoteFrags.kfrRootInPageSeq:
				{
					vwenv.AddObjVec(ScrBookTags.kflidFootnotes, this, (int)FootnoteFrags.kfrAllFootnotesWithinPagePara);
					break;
				}
				case (int)FootnoteFrags.kfrFootnoteWithinPagePara:
				{
					// Insert the marker and reference
					vwenv.AddObj(hvo, this, (int)StTextFrags.kfrFootnoteMarker);
					vwenv.AddObj(hvo,  this,(int)StTextFrags.kfrFootnoteReference);

					// Insert (we hope only one) paragraph contents.
					vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this, (int)FootnoteFrags.kfrFootnoteParaWithinPagePara);
					break;
				}
				case (int) FootnoteFrags.kfrFootnoteParaWithinPagePara:
				{
					if (!InsertParaContentsUserPrompt(vwenv, hvo))
					{
						// Display the text paragraph contents, or its user prompt.
						vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					}
					break;
				}
				case (int)FootnoteFrags.kfrBook:
				{
					vwenv.OpenDiv();
					vwenv.AddObjVecItems(ScrBookTags.kflidFootnotes, this,
						(int)StTextFrags.kfrFootnote);
					vwenv.CloseDiv();
					break;
				}
				case (int)StTextFrags.kfrFootnoteReference:
				{
					DisplayFootnoteReference(vwenv, hvo);
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
			if (chvo == 0 || m_startingFootnoteIndex < 0 || m_endingFootnoteIndex < 0)
				return;
			List<int> notes = new List<int>(chvo);
			for (int ihvo = m_startingFootnoteIndex; ihvo < chvo && ihvo <= m_endingFootnoteIndex; ihvo++)
			{
				int footnoteHvo = sda.get_VecItem(hvo, tag, ihvo);
				if (sda.get_IsValidObject(footnoteHvo))
					notes.Add(footnoteHvo);
			}

			// TE-6185: Undo can cause the only footnote on the page to no longer be a valid object.
			if (notes.Count == 0)
				return;

			// Footnotes are owned by books which are owned by Scripture.
			ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(notes[0]);
			IScripture scr = obj.Owner.Owner as IScripture;
			if (scr.CrossRefsCombinedWithFootnotes)
			{
				CreateNotesParagraph(vwenv, notes, NoteGeneralParagraphStyle);
			}
			else
			{
				List<int> generalNotes = new List<int>(notes.Count);
				List<int> crNotes = new List<int>(notes.Count);
				foreach (int hvoNote in notes)
				{
					int cPara = sda.get_VecSize(hvoNote, StTextTags.kflidParagraphs);
					if (cPara != 0)
					{
						int hvoFirstPara = sda.get_VecItem(hvoNote, StTextTags.kflidParagraphs, 0);
						ITsTextProps paraStyle = (ITsTextProps)m_cache.DomainDataByFlid.get_UnknownProp(
							hvoFirstPara, StParaTags.kflidStyleRules);
						if (paraStyle != null)
						{
							string styleName = paraStyle.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
							while (!string.IsNullOrEmpty(styleName) &&
								styleName != ScrStyleNames.CrossRefFootnoteParagraph && m_stylesheet != null)
							{
								styleName = m_stylesheet.GetBasedOn(styleName);
							}
							if (styleName == ScrStyleNames.CrossRefFootnoteParagraph)
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the notes paragraph.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="notes">The notes.</param>
		/// <param name="styleProps">The style props.</param>
		/// ------------------------------------------------------------------------------------
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
					vwenv.AddString(m_cache.TsStrFactory.MakeString("  ", m_cache.DefaultVernWs));
				vwenv.AddObj(notes[ihvo], this, (int)FootnoteFrags.kfrFootnoteWithinPagePara);
			}
			vwenv.CloseParagraph();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load data needed to display the specified objects using the specified fragment.
		/// This is called before attempting to Display an item that has been listed for lazy
		/// display using AddLazyItems. It may be used to load the necessary data into the
		/// DataAccess object. We don't have to do this any more with the new FDO code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent,
			int tag, int frag, int ihvoMin)
		{
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
		protected override void DisplayFootnoteMarker(IVwEnv vwenv, IStFootnote footnote)
		{
			// The footnote marker is not editable.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
				(int)TptEditable.ktptNotEditable);

			ITsStrBldr strBldr = ((IScrFootnote)footnote).MakeFootnoteMarker(DefaultWs);
			strBldr.ReplaceTsString(strBldr.Length, strBldr.Length, OneSpaceString);
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
					ITsPropsBldr tsPropsBuilder = TsPropsBldrClass.Create();

					tsPropsBuilder.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
						ScrStyleNames.CrossRefFootnoteParagraph);
					m_ttpNoteCrossRefs = tsPropsBuilder.GetTextProps();
				}
				return m_ttpNoteCrossRefs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote marker.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetFootnoteMarker(int index)
		{
			return string.Empty + (char)((int)'a' + (index % 26));
		}
		#endregion

		#region IDependentObjectsVc Members
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the ending dependent object that will be shown (inclusive).
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public int EndingObjIndex
		{
			get { return m_endingFootnoteIndex; }
			set { m_endingFootnoteIndex = value; }
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the starting dependent object that will be shown.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public int StartingObjIndex
		{
			get { return m_startingFootnoteIndex; }
			set { m_startingFootnoteIndex = value; }
		}

		#endregion
	}
	#endregion
}
