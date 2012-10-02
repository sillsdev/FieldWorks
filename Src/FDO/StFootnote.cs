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
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.Cellar
{
	#region StFootnote class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Representation of a footnote in text.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class StFootnote : StText
	{
		#region Constants
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This property is when the display options of a footnote change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public const int ktagFootnoteOptions = kclsidStFootnote * 1000 + 999;
		#endregion

		#region Additional Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new (empty) StFootnote and add it to the owner's collection.
		/// This method provides a way to create a new footnote object in the db for any generic
		/// CmObject owner.
		/// </summary>
		/// <param name="owner">The owner CmObject.</param>
		/// <param name="flid">The flid of the owner's footnote collection.</param>
		/// <param name="footnoteIndex">Index to insert the footnote at in the collection.</param>
		/// <returns>the new footnote object</returns>
		/// ------------------------------------------------------------------------------------
		public StFootnote(ICmObject owner, int flid, int footnoteIndex)
			: this()
		{
			// add this new (empty) footnote to the owner's collection
			FdoOwningSequence<IStFootnote> footnotes = new FdoOwningSequence<IStFootnote>(owner.Cache,
				owner.Hvo, flid);
			footnotes.InsertAt(this, footnoteIndex);
		}
		#endregion

		#region public methods
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
		//TODO: rename this method as InsertOwnORCIntoPara
		{
			StringUtils.InsertOrcIntoPara(Guid, FwObjDataTypes.kodtOwnNameGuidHot,
				tsStrBldr, ich, ich, ws);
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
		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make it virtual, so ScrFootnote can override it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool DisplayFootnoteReference
		{
			get { return DisplayFootnoteReference_Generated; }
			set { DisplayFootnoteReference_Generated = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make it virtual, so ScrFootnote can override it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool DisplayFootnoteMarker
		{
			get { return DisplayFootnoteMarker_Generated; }
			set { DisplayFootnoteMarker_Generated = value; }
		}
		#endregion

		#region Text representation methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new StFootnote owned by the given book created from the given string
		/// representation (Created from GetTextRepresentation())
		/// </summary>
		/// <param name="owner">The object that owns the sequence of footnotes into which the
		/// new footnote is to be inserted</param>
		/// <param name="flid">The field id of the property in which the footnotes are owned
		/// </param>
		/// <param name="sTextRepOfFootnote">The given string representation of a footnote
		/// </param>
		/// <param name="footnoteIndex">0-based index where the footnote will be inserted</param>
		/// <param name="footnoteMarkerStyleName">style name for footnote markers</param>
		/// <returns>An StFootnote with the properties set to the properties in the
		/// given string representation</returns>
		/// ------------------------------------------------------------------------------------
		public static StFootnote CreateFromStringRep(CmObject owner, int flid,
			string sTextRepOfFootnote, int footnoteIndex, string footnoteMarkerStyleName)
		{
			StFootnote createdFootnote = new StFootnote(owner, flid, footnoteIndex);

			// create an XML reader to read in the string representation
			System.IO.StringReader reader = new System.IO.StringReader(sTextRepOfFootnote);
			XmlDocument doc = new XmlDocument();
			try
			{
				doc.Load(reader);
			}
			catch (XmlException)
			{
				throw new ArgumentException("Unrecognized XML format for footnote.");
			}

			XmlNodeList tagList = doc.SelectNodes("FN");

			foreach (XmlNode bla in tagList[0].ChildNodes)
			{
				// Footnote marker
				if (bla.Name == "M")
				{
					ITsPropsBldr propBlr = TsPropsBldrClass.Create();
					propBlr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
						footnoteMarkerStyleName);
					ITsStrBldr tss = TsStrBldrClass.Create();
					tss.Replace(0, 0, bla.InnerText, propBlr.GetTextProps());
					createdFootnote.FootnoteMarker.UnderlyingTsString = tss.GetString();
				}

				// Display footnote marker
				else if (bla.Name == "ShowMarker")
					createdFootnote.DisplayFootnoteMarker = true;
				// display footnote scripture reference
				else if (bla.Name == "ShowReference")
					createdFootnote.DisplayFootnoteReference = true;
				// start of a paragraph
				else if (bla.Name == "P")
				{
					StTxtPara newPara = new StTxtPara();
					createdFootnote.ParagraphsOS.Append(newPara);
					ITsIncStrBldr paraBldr = TsIncStrBldrClass.Create();
					CmTranslation trans = null;
					//ITsStrBldr paraBldr = TsStrBldrClass.Create();
					foreach (XmlNode paraTextNode in bla.ChildNodes)
					{
						if (paraTextNode.Name == "PS")
						{
							// paragraph style
							ITsPropsBldr propBldr =
								TsPropsBldrClass.Create();
							if (!String.IsNullOrEmpty(paraTextNode.InnerText))
							{
								propBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
									paraTextNode.InnerText);
							}
							else
							{
								Debug.Fail("Attempting to create a footnote paragraph with no paragraph style specified!");
							}
							newPara.StyleRules = propBldr.GetTextProps();
						}
						else if (paraTextNode.Name == "RUN")
						{
							CreateRunFromStringRep(owner, paraBldr, paraTextNode);
							paraBldr.Append(paraTextNode.InnerText);
						}
						else if (paraTextNode.Name == "TRANS")
						{
							if (trans == null)
								trans = (CmTranslation)newPara.GetOrCreateBT();

							// Determine which writing system where the string run(s) will be added.
							string iculocale = paraTextNode.Attributes.GetNamedItem("WS").Value;
							if (iculocale == null || iculocale == string.Empty)
							{
								throw new ArgumentException(
									"Unknown ICU locale encountered: " + iculocale);
							}
							int transWS = owner.Cache.LanguageEncodings.GetWsFromIcuLocale(iculocale);
							Debug.Assert(transWS != 0, "Unable to find ws from ICU Locale");

							// Build a TsString from the run(s) description.
							ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
							foreach (XmlNode transTextNode in paraTextNode.ChildNodes)
							{
								if (transTextNode.Name != "RUN")
								{
									throw new ArgumentException("Unexpected translation element '" +
										transTextNode.Name + "' encountered for ws '" + iculocale + "'");
								}

								CreateRunFromStringRep(owner, strBldr, transTextNode);
								strBldr.Append(transTextNode.InnerText);
							}

							trans.Translation.SetAlternative(strBldr.GetString(), transWS);
						}
					}
					newPara.Contents.UnderlyingTsString = paraBldr.GetString();
				}
			}
			owner.Cache.PropChanged(null, PropChangeType.kpctNotifyAll, owner.Hvo, flid, footnoteIndex, 1, 0);
			return createdFootnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the a text run from a string representation.
		/// </summary>
		/// <param name="owner">The owner of the paragraph (book).</param>
		/// <param name="strBldr">The structured string builder.</param>
		/// <param name="textNode">The text node which describes runs to be added to the
		/// paragraph or to the translation for a particular writing system</param>
		/// ------------------------------------------------------------------------------------
		private static void CreateRunFromStringRep(CmObject owner, ITsIncStrBldr strBldr,
			XmlNode textNode)
		{
			XmlNode charStyle = textNode.Attributes.GetNamedItem("CS");
			if (charStyle != null)
			{
				strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
					charStyle.Value);
			}
			else
			{
				strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
					null);
			}

			XmlNode wsICULocale = textNode.Attributes.GetNamedItem("WS");
			if (wsICULocale != null)
			{
				ILgWritingSystemFactory wsf = owner.Cache.LanguageWritingSystemFactoryAccessor;
				int ws = wsf.GetWsFromStr(wsICULocale.Value);
				if (ws <= 0)
					throw new ArgumentException("Unknown ICU locale encountered: '" + wsICULocale.Value + "'");
				strBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, wsf.GetWsFromStr(wsICULocale.Value));
			}
			else
				throw new ArgumentException("Required attribute WS missing from RUN element.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a footnote into a string. Formatting within the footnote string is
		/// defined as:
		///
		/// &lt;FN&gt;				= indicates string represents a footnote
		/// &lt;M&gt;				= Footnote marker
		/// &lt;ShowMarker&gt;		= Show marker in footnote
		/// &lt;ShowReference&gt;	= Show reference in footnote
		/// &lt;P&gt; 				= Start of Para
		/// &lt;PS&gt; 				= Paragraph style
		/// &lt;RUN&gt;				= Run text
		/// &lt;WS&gt;				= Writing system of run (attribute)
		/// &lt;CS&gt;				= Character style of run (attribute)
		/// </summary>
		/// <returns>The generated string representation of this footnote</returns>
		/// ------------------------------------------------------------------------------------
		public string GetTextRepresentation()
		{
			string toReturn = "<FN>";

			if (FootnoteMarker.Text != null)
				toReturn += "<M>" + FootnoteMarker.Text + "</M>";
			toReturn += DisplayFootnoteMarker ? "<ShowMarker/>" : string.Empty;
			toReturn += DisplayFootnoteReference ? "<ShowReference/>" : string.Empty;
			foreach (StTxtPara para in ParagraphsOS)
			{
				toReturn += "<P>";
				// If the style rules are null, create the XML without a style name specified. This is bad,
				// but at least it won't crash.
				Debug.Assert(para.StyleRules != null, "StyleRules should never be null.");
				string styleName = para.StyleRules == null ? String.Empty : para.StyleRules.GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle);
				toReturn += "<PS>" + styleName + "</PS>";
				toReturn += GetTextRepresentationOfTsString(para.Contents.UnderlyingTsString);
				toReturn += GetTextRepresentationOfTrans(para);
				toReturn += "</P>";
			}

			toReturn += "</FN>";

			return toReturn;
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
					LgWritingSystem lgws = new LgWritingSystem(m_cache, ws);
					tssRepresentation += " WS='" + lgws.ICULocale + "'";
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
					StringBuilder newString = new StringBuilder(runText.Length * 2);
					for (int i = 0; i < runText.Length; i++)
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
		private string GetTextRepresentationOfTrans(StTxtPara footnotePara)
		{
			string transRepresentation = string.Empty;
			CmTranslation trans = (CmTranslation)footnotePara.GetBT();
			if (trans == null)
				return transRepresentation;

			List<int> transWs = m_cache.GetUsedScriptureTransWsForPara(footnotePara.Hvo);

			foreach (int ws in transWs)
			{
				ITsString tss = trans.Translation.GetAlternativeTss(ws);
				if (tss != null && tss.Length > 0)
				{
					LgWritingSystem lgws = new LgWritingSystem(m_cache, ws);
					transRepresentation += "<TRANS WS='" + lgws.ICULocale + "'>";
					transRepresentation += GetTextRepresentationOfTsString(tss);
					transRepresentation += "</TRANS>";
				}
			}

			return transRepresentation;
		}

		#endregion
	}
	#endregion

	#region FootnoteInfo struct
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Information about a footnote including the footnote and its paragraph style.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public struct FootnoteInfo
	{
		/// <summary>footnote</summary>
		public readonly StFootnote footnote;
		/// <summary>paragraph style for footnote</summary>
		public readonly string paraStylename;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for the FootnoteInfo structure
		/// </summary>
		/// <param name="stFootnote">given footnote</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteInfo(StFootnote stFootnote)
		{
			footnote = stFootnote;
			StPara para = (StPara)footnote.ParagraphsOS[0];
			if (para.StyleRules != null)
			{
				paraStylename = para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			}
			else
			{
				paraStylename = null;
				Debug.Fail("StyleRules should never be null.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for the FootnoteInfo structure
		/// </summary>
		/// <param name="stFootnote">given footnote</param>
		/// <param name="sParaStylename">paragraph style of the footnote</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteInfo(StFootnote stFootnote, string sParaStylename)
		{
			footnote = stFootnote;
			paraStylename = sParaStylename;
		}
	}
	#endregion
}
