// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlNotePara.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores information about a single paragraph of text in a Scripture annotation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("para")]
	public class XmlNotePara
	{
		private string m_text;

		#region XML attributes
		/// <summary>The default language for annotation data (expessed as an ICU locale)</summary>
		[XmlAttribute("xml:lang")]
		public string IcuLocale;

		/// <summary>The paragraph style name</summary>
		[XmlAttribute("stylename")]
		public string StyleName;
		#endregion

		#region XML elements
		/// <summary>List of TSS runs contained in the paragraph</summary>
		[XmlElement(typeof(XmlTextRun), ElementName = "span")]
		[XmlElement(typeof(XmlHyperlinkRun), ElementName = "a")]
		public List<XmlTextRun> Runs = new List<XmlTextRun>();

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text. (i.e. any text that's inside the "para" tag but not in
		/// a "span" or an "a" tag.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlText]
		public string Text
		{
			get { return m_text; }
			set { m_text = (value != null ? value.Trim() : null); }
		}

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlNotePara"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlNotePara()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlNotePara"/> class based on the given
		/// StTxtPara.
		/// </summary>
		/// <param name="stTxtPara">The FDO paragraph.</param>
		/// <param name="wsDefault">The default (analysis) writing system.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// ------------------------------------------------------------------------------------
		public XmlNotePara(IStTxtPara stTxtPara, int wsDefault, ILgWritingSystemFactory lgwsf)
		{
			// REVIEW: Ask TomB about this. The only paragraph style allowed in
			// TE for notes is "Remark" so is it necessary to write it to the XML?
			// It causes a problem for the OXES validator.
			//StyleName = stTxtPara.StyleName;

			ITsString tssParaContents = stTxtPara.Contents.UnderlyingTsString;
			if (tssParaContents.RunCount == 0)
				return;

			int dummy;
			int wsFirstRun = tssParaContents.get_Properties(0).GetIntPropValues(
				(int)FwTextPropType.ktptWs, out dummy);

			//if (wsFirstRun != wsDefault)
			IcuLocale = lgwsf.GetStrFromWs(wsFirstRun);

			for (int iRun = 0; iRun < tssParaContents.RunCount; iRun++)
			{
				ITsTextProps props = tssParaContents.get_Properties(iRun);
				string text = tssParaContents.get_RunText(iRun);
				if (StringUtils.IsHyperlink(props))
					Runs.Add(new XmlHyperlinkRun(wsFirstRun, lgwsf, text, props));
				else
					Runs.Add(new XmlTextRun(wsFirstRun, lgwsf, text, props));
			}
		}

		#endregion

		#region static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a list of XmlNotePara objects representing the paragraphs of a journal text
		/// of an annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<XmlNotePara> GetParagraphList(IStJournalText stJournalText,
			int wsDefault, ILgWritingSystemFactory lgwsf)
		{
			if (stJournalText == null || stJournalText.ParagraphsOS.Count == 0 ||
				string.IsNullOrEmpty(((IStTxtPara)stJournalText.ParagraphsOS[0]).Contents.Text))
			{
				return null;
			}

			List<XmlNotePara> list = new List<XmlNotePara>();
			foreach (IStTxtPara para in stJournalText.ParagraphsOS)
				list.Add(new XmlNotePara(para, wsDefault, lgwsf));

			return list;
		}

		#endregion

		#region Methods for building and StTxtPara
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Convert each of the child paragraphs into an ITsString object, and add that object
		/// to the given list. If the paragraph's writing system cannot be found, then the
		/// default vernacular is used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public StTxtParaBldr BuildParagraph(FdoCache cache, FwStyleSheet styleSheet)
		{
			return BuildParagraph(styleSheet, cache.DefaultVernWs);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Convert each of the child paragraphs into an ITsString object, and add that object
		/// to the given list. If the paragraph's writing system cannot be found, then the
		/// specified default writing system is used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public StTxtParaBldr BuildParagraph(FwStyleSheet styleSheet, int wsDefault)
		{
			StTxtParaBldr bldr;
			int wsPara = (string.IsNullOrEmpty(IcuLocale) ?
				wsDefault : ScrNoteImportManager.GetWsForLocale(IcuLocale));

			bldr = new StTxtParaBldr(styleSheet.Cache);
			string stylename = (string.IsNullOrEmpty(StyleName) ?
				ScrStyleNames.Remark : StyleName);

			bldr.ParaStylePropsProxy = StyleProxyListManager.GetXmlParaStyleProxy(
				stylename, ContextValues.Annotation, wsDefault);

			foreach (XmlTextRun run in Runs)
			{
				int ws = (string.IsNullOrEmpty(run.IcuLocale) ?
					wsPara : ScrNoteImportManager.GetWsForLocale(run.IcuLocale));

				run.AddToParaBldr(bldr, ws, styleSheet);
			}

			// OXES supports mixed text so this is designed to handle text that
			// is found in the "para" tag but not in any "span" or "a" tag.
			if (!string.IsNullOrEmpty(m_text))
			{
				XmlTextRun extraText = new XmlTextRun();
				extraText.Text = m_text;
				extraText.AddToParaBldr(bldr, wsPara, styleSheet);
			}

			return bldr;
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			StringBuilder bldr = new StringBuilder();
			foreach (XmlTextRun run in Runs)
				bldr.AppendFormat("{0} ", run.ToString());

			if (m_text != null)
				bldr.Append(m_text);

			return bldr.ToString().TrimEnd(' ');
		}
	}
}
