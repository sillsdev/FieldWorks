// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StFootnote.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Xml;
using System.Text;
using System.Collections.Generic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The StFootnote Class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class StFootnote : StText
	{
		#region Text representation methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a footnote into a string. Formatting within the footnote string is
		/// defined as:
		///
		/// &lt;FN&gt;				= indicates string represents a footnote
		/// &lt;M&gt;				= Footnote marker
		/// &lt;P&gt; 				= Start of Para
		/// &lt;PS&gt; 				= Paragraph style
		/// &lt;RUN&gt;				= Run text
		/// &lt;WS&gt;				= Writing system of run (attribute)
		/// &lt;CS&gt;				= Character style of run (attribute)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string TextRepresentation
		{
			get
			{
				string toReturn = "<FN>";

				if (FootnoteMarker.Text != null)
					toReturn += "<M>" + FootnoteMarker.Text + "</M>";
				foreach (IStTxtPara para in ParagraphsOS)
				{
					toReturn += "<P>";
					// If the style rules are null, create the XML without a style name specified. This is bad,
					// but at least it won't crash.
					Debug.Assert(para.StyleRules != null, "StyleRules should never be null.");
					string styleName = para.StyleRules == null ? String.Empty : para.StyleRules.GetStrPropValue(
																					(int)FwTextPropType.ktptNamedStyle);
					toReturn += "<PS>" + styleName + "</PS>";
					toReturn += GetTextRepresentationOfTsString(para.Contents);
					toReturn += GetTextRepresentationOfTrans(para);
					toReturn += "</P>";
				}

				toReturn += "</FN>";

				return toReturn;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text representation of a TsString.
		/// </summary>
		/// <param name="tss">The TsString.</param>
		/// <returns>text representation of the TsString</returns>
		/// ------------------------------------------------------------------------------------
		private string GetTextRepresentationOfTsString(ITsString tss)
		{
			string tssRepresentation = string.Empty;
			for (int iRun = 0; iRun < tss.RunCount; iRun++)
			{
				tssRepresentation += "<RUN";

				// writing system of run
				int nVar;
				int ws = tss.get_Properties(iRun).GetIntPropValues(
					(int)FwTextPropType.ktptWs, out nVar);
				if (ws != -1)
				{
					IWritingSystem wsObj = Services.WritingSystemManager.Get(ws);
					tssRepresentation += " WS='" + wsObj.Id + "'";
				}

				// character style of run
				string charStyle = tss.get_Properties(iRun).GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle);
				string runText = tss.get_RunText(iRun);
				// add the style tags
				if (charStyle != null)
					tssRepresentation += " CS='" + charStyle + @"'";

				tssRepresentation += ">";

				// add the text for the tag
				if (runText != null && runText != string.Empty)
				{
					var newString = new StringBuilder(runText.Length * 2);
					for (var i = 0; i < runText.Length; i++)
					{
						// remove '<' and '>' from the text
						if (runText[i] == '<')
							newString.Append("&lt;");
						else if (runText[i] == '>')
							newString.Append("&gt;");
						else
							newString.Append(runText[i]);
					}
					tssRepresentation += newString.ToString();
				}

				// add the run end tag
				tssRepresentation += "</RUN>";
			}
			return tssRepresentation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text representation of the back translations for a footnote paragraph.
		/// </summary>
		/// <param name="footnotePara">The footnote paragraph.</param>
		/// <returns>
		/// text representation of all of the back translations for the footnote paragraph
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private string GetTextRepresentationOfTrans(IStTxtPara footnotePara)
		{
			ICmTranslation trans = footnotePara.GetBT();
			if (trans == null)
				return string.Empty;
			StringBuilder transRepresentation = new StringBuilder();

			foreach (var ws in trans.AvailableWritingSystems)
			{
				ITsString tss = trans.Translation.get_String(ws.Handle);
				if (tss != null && tss.Length > 0)
				{
					transRepresentation.Append("<TRANS WS='");
					transRepresentation.Append(ws.Id);
					transRepresentation.Append("'>");
					transRepresentation.Append(GetTextRepresentationOfTsString(tss));
					transRepresentation.Append("</TRANS>");
				}
			}

			return transRepresentation.ToString();
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the footnote.
		/// </summary>
		/// <value>The type of the footnote.</value>
		/// ------------------------------------------------------------------------------------
		public virtual FootnoteMarkerTypes MarkerType
		{
			get
			{
				string sMarker = FootnoteMarker.Text;
				if (sMarker == null)
					return FootnoteMarkerTypes.NoFootnoteMarker;
				if (Icu.IsAlphabetic((int)sMarker[0]))
					return FootnoteMarkerTypes.AutoFootnoteMarker;
				return FootnoteMarkerTypes.SymbolicFootnoteMarker;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the FootnoteMarker. Base class just does default behavior, but subclasses
		/// can (and do) override.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[ModelProperty(CellarPropertyType.String, 39001, "TsStringAccessor")]
		public virtual ITsString FootnoteMarker
		{
			get { return FootnoteMarker_Generated; }
			set { FootnoteMarker_Generated = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not to display the target reference for this footnote when it is
		/// displayed at the bottom of the page or in a separate pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool DisplayFootnoteReference
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not to display the marker (caller) for this footnote when it is
		/// displayed at the bottom of the page or in a separate pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool DisplayFootnoteMarker
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert footnote marker (i.e. Reference ORC run with the footnote GUID in the properties)
		/// into the given string builder for the translation.
		/// </summary>
		/// <param name="tsStrBldr">A builder for the translation string that is to
		/// contain the footnote reference ORC</param>
		/// <param name="ich">The 0-based character offset into the translation string
		/// at which we will insert the ORC</param>
		/// <param name="ws">The writing system id for the new ORC run</param>
		/// ------------------------------------------------------------------------------------
		public void InsertRefORCIntoTrans(ITsStrBldr tsStrBldr, int ich, int ws)
		{
			StringUtils.InsertOrcIntoPara(Guid, FwObjDataTypes.kodtNameGuidHot,
				tsStrBldr, ich, ich, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert footnote marker (i.e. Owning ORC run with the footnote GUID in the properties)
		/// into the given string builder for the paragraph.
		/// </summary>
		/// <param name="tsStrBldr">A string builder for the paragraph that is to contain the
		/// footnote owning ORC</param>
		/// <param name="ich">The 0-based character offset into the paragraph at which we will
		/// insert the ORC</param>
		/// <param name="ws">The writing system id for the new ORC run</param>
		/// ------------------------------------------------------------------------------------
		public void InsertOwningORCIntoPara(ITsStrBldr tsStrBldr, int ich, int ws)
		{
			StringUtils.InsertOrcIntoPara(Guid, FwObjDataTypes.kodtOwnNameGuidHot,
				tsStrBldr, ich, ich, ws);
		}
	}

}
