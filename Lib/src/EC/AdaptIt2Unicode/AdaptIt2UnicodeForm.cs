using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;                        // FileInfo, DirectoryInfo
using System.Xml;                       // for XMLDocument
using System.Xml.XPath;                 // for XPathNavigator
using Microsoft.Win32;                  // for Registry

using ECInterfaces;
using SilEncConverters40;

namespace AdaptIt2Unicode
{
	public partial class AdaptIt2UnicodeForm : Form
	{
		public const string cstrCaption = "Adapt It to Unicode";
		public const string cstrAdaptItProjectFolderAnsi = @"\Adapt It Work";
		public const string cstrAdaptItProjectFolderUnicode = @"\Adapt It Unicode Work";
		public const string cstrAdaptItProjectFilename = "AI-ProjectConfiguration.aic";

		public const string cstrAdaptItPunctuationPairsLegacy = "PunctuationPairs(stores space for an empty cell)	";
		public const string cstrAdaptItPunctuationPairsNRSource = "PunctuationPairsSourceSet(stores space for an empty cell)	";
		public const string cstrAdaptItPunctuationPairsNRTarget = "PunctuationPairsTargetSet(stores space for an empty cell)	";

		public const string cstrAdaptItPunctuationExtraLegacy = "PunctuationTwoCharacterPairs	";
		public const string cstrAdaptItPunctuationExtraNR = "PunctuationTwoCharacterPairsSourceSet(ditto)	\r\nPunctuationTwoCharacterPairsTargetSet(ditto)	";

		public const string cstrEncodingNameDelimiter = "encoding=\"";
		protected const string cstrNoChange = " (no change)";

		protected const string cstrSfmOff = @"*";

		public const string cstrAdaptItCharMapDelimiter = "CharSet	";
		public const string cstrAdaptItFontNameDelimiter = "FaceName	";
		public const string m_strStatusBoxDefaultFont = "Microsoft Sans Serif";
		public const string cstrUsingUsfmRegKeyValue = "UsingUSFM";

		public int m_nStatusBoxDefaultFontSize = 11;
		public string cstrAdaptItXmlNamespace = "http://www.sil.org/computing/schemas/AdaptIt KB.xsd";
		protected string CRLF = "\r\n";

		protected const int cnMaxExamples = 50;

		public char[] caSplitChars = new char[] { '\r', '\n', '\t' };
		public char[] caSfmTerminators = new char[] { ' ', '\\' };

		protected string m_strFontNameSource = null;
		protected string m_strFontNameTarget = null;

		public string m_strDefaultUnicodeFontSource = m_strStatusBoxDefaultFont;
		public string m_strDefaultUnicodeFontTarget = m_strStatusBoxDefaultFont;

		protected IEncConverter m_aEcSource = null;
		protected IEncConverter m_aEcTarget = null;
		protected IEncConverter m_aEcGloss = null;

		protected Dictionary<string, Font> m_mapFonts = new Dictionary<string,Font>();
		protected Dictionary<string, List<string>> m_mapFilteredSfms = new Dictionary<string, List<string>>();
		protected List<string> m_astrSfmsToConvert = new List<string>();
		protected List<string> m_astrSfmsToNotConvert = new List<string>();
		protected int m_nLinesWritten = 0;

		public enum AIProjectType
		{
			eLegacy,
			eUnicode
		}

		public AdaptIt2UnicodeForm()
		{
			InitializeComponent();
			InitProjectInfo();
		}

		protected void ConvertProjectFile(string strProjectFileSource, string strProjectFileTarget,
			IEncConverter aEcSource, IEncConverter aEcTarget, string strFontNameSource, string strFontNameTarget)
		{
			// convert the punctuation in the legacy project file.
			Encoding enc = Encoding.Default;
			if (aEcSource != null)
			{
				// the "symbol" code page apparently isn't valid for GetEncoding, so use the cp for ISO-8859-1
				if (aEcSource.CodePageInput == EncConverters.cnSymbolFontCodePage)
					enc = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage);
				else
					enc = Encoding.GetEncoding(aEcSource.CodePageInput);
			}
			string strProjectFileContents = File.ReadAllText(strProjectFileSource, enc);

			int nIndex;
			int nLength = 0;

			// the charset value has to be 0 in a Unicode project
			// first the 'Source language'
			nIndex = strProjectFileContents.IndexOf(cstrAdaptItCharMapDelimiter);
			if (nIndex != -1)
			{
				nIndex += cstrAdaptItCharMapDelimiter.Length;
				nLength = strProjectFileContents.IndexOfAny(caSplitChars, nIndex) - nIndex;
				strProjectFileContents = ReplaceSubstring(strProjectFileContents, nIndex, nLength, "0");

				// next the target language...
				nIndex = strProjectFileContents.IndexOf(cstrAdaptItCharMapDelimiter, nIndex);
				if (nIndex != -1)
				{
					nIndex += cstrAdaptItCharMapDelimiter.Length;
					nLength = strProjectFileContents.IndexOfAny(caSplitChars, nIndex) - nIndex;
					strProjectFileContents = ReplaceSubstring(strProjectFileContents, nIndex, nLength, "0");

					// finally do the navigation language as well.
					nIndex = strProjectFileContents.IndexOf(cstrAdaptItCharMapDelimiter, nIndex);
					if (nIndex != -1)
					{
						nIndex += cstrAdaptItCharMapDelimiter.Length;
						nLength = strProjectFileContents.IndexOfAny(caSplitChars, nIndex) - nIndex;
						strProjectFileContents = ReplaceSubstring(strProjectFileContents, nIndex, nLength, "0");
					}
				}
			}

			// see about replacing the font names
			if (m_strDefaultUnicodeFontSource != m_strStatusBoxDefaultFont)
			{
				nIndex = strProjectFileContents.IndexOf(strFontNameSource);
				nLength = strFontNameSource.Length;
				strProjectFileContents = ReplaceSubstring(strProjectFileContents, nIndex, nLength, m_strDefaultUnicodeFontSource);
			}
			else
			{
				nIndex = strProjectFileContents.IndexOf(cstrAdaptItFontNameDelimiter);
				nIndex = strProjectFileContents.IndexOf(cstrAdaptItFontNameDelimiter, nIndex);
			}

			if (m_strDefaultUnicodeFontTarget != m_strStatusBoxDefaultFont)
			{
				nIndex = strProjectFileContents.IndexOf(strFontNameTarget, nIndex + nLength);
				nLength = strFontNameTarget.Length;
				strProjectFileContents = ReplaceSubstring(strProjectFileContents, nIndex, nLength, m_strDefaultUnicodeFontTarget);
			}

			int nIndexPunctDelimiter = strProjectFileContents.IndexOf(cstrAdaptItPunctuationPairsLegacy, nIndex);
			nIndex = nIndexPunctDelimiter + cstrAdaptItPunctuationPairsLegacy.Length;
			nLength = strProjectFileContents.IndexOfAny(caSplitChars, nIndex) - nIndex;
			string strPunctuation = strProjectFileContents.Substring(nIndex, nLength);

			char[] aPunctSource, aPunctTarget;
			UnpackPunctuationLegacy(strPunctuation, out aPunctSource, out aPunctTarget);

			string strNewPunctuationSource = new string(aPunctSource);
			AppendStatusOutputNl("found: legacy source punctuation: ", m_strStatusBoxDefaultFont, true);
			AppendStatusOutput(strNewPunctuationSource, strFontNameSource, true);

			string strNewPunctuationSourceConverted = strNewPunctuationSource;
			if (aEcSource != null)
				strNewPunctuationSourceConverted = aEcSource.Convert(strNewPunctuationSource);

			if (strNewPunctuationSourceConverted != strNewPunctuationSource)
				AppendStatusOutputNl("converted to: " + strNewPunctuationSourceConverted, m_strDefaultUnicodeFontSource, true);
			else
				AppendStatusOutputNl("result: no changes", m_strStatusBoxDefaultFont, true);

			string strNewPunctuationTarget = new string(aPunctTarget);
			AppendStatusOutputNl("found: legacy target punctuation: ", m_strStatusBoxDefaultFont, true);
			AppendStatusOutput(strNewPunctuationTarget, strFontNameTarget, true);

			string strNewPunctuationTargetConverted = strNewPunctuationTarget;
			if (aEcTarget != null)
				strNewPunctuationTargetConverted = aEcTarget.Convert(strNewPunctuationTarget);

			if (strNewPunctuationTargetConverted != strNewPunctuationTarget)
				AppendStatusOutputNl("converted to: " + strNewPunctuationTargetConverted, m_strDefaultUnicodeFontTarget, true);
			else
				AppendStatusOutputNl("result: no changes", m_strStatusBoxDefaultFont, true);

			nIndex = strProjectFileContents.IndexOf(cstrAdaptItPunctuationExtraLegacy, nIndex) + cstrAdaptItPunctuationExtraLegacy.Length;
			nLength = strProjectFileContents.IndexOfAny(caSplitChars, nIndex) - nIndex;

			string strProjectFileContentsNew =
				strProjectFileContents.Substring(0, nIndexPunctDelimiter) +
				cstrAdaptItPunctuationPairsNRSource + strNewPunctuationSourceConverted + CRLF +
				cstrAdaptItPunctuationPairsNRTarget + strNewPunctuationTargetConverted + CRLF +
				cstrAdaptItPunctuationExtraNR +
				strProjectFileContents.Substring(nIndex);

			// as a special case, if the 'after-conversion' fonts are both defined and are equal to each other,
			//  then go ahead and update in the project file
			if (    (strFontNameSource == strFontNameTarget)
				&&  (m_strDefaultUnicodeFontSource != m_strStatusBoxDefaultFont)
				&&  (m_strDefaultUnicodeFontTarget != m_strStatusBoxDefaultFont)
				&&  (m_strDefaultUnicodeFontSource == m_strDefaultUnicodeFontTarget))
				strProjectFileContentsNew = strProjectFileContentsNew.Replace(strFontNameSource, m_strDefaultUnicodeFontSource);

			InsureDirectoryExists(strProjectFileTarget);

			// don't use WriteAllText (c.f. ReadAllText above), because that inserts the BOM which causes AIU grief
			byte[] abyContents = Encoding.UTF8.GetBytes(strProjectFileContentsNew);
			File.WriteAllBytes(strProjectFileTarget, abyContents);
		}

		protected void ConvertKbFile(string strKbFileSource, string strKbFileTarget,
			IEncConverter aEcSource, IEncConverter aEcTarget, string strFontNameSource, string strFontNameTarget)
		{
			// Since AdaptIt will make different records for two words which are canonically
			//  equivalent, if we use the class object to read it in via ReadXml, that will throw
			//  an exception in such a case. So see if using XmlDocument is any less restrictive
			try
			{
				AppendStatusOutputNl("Processing KB file: " + strKbFileSource, m_strStatusBoxDefaultFont, true);
				string strTempKbFileSource = strKbFileSource;
				CheckForIncorrectEncodingString(ref strTempKbFileSource);
				XmlDocument doc;
				XPathNavigator navigator;
				XmlNamespaceManager manager;
				GetXmlDocument(strTempKbFileSource, out doc, out navigator, out manager);

				XPathNodeIterator xpMapIterator = navigator.Select("/aikb:AdaptItKnowledgeBase/aikb:KB/aikb:MAP", manager);

				progressBarAdaptationFile.Visible = true;

				// can't have two source words with the same value
				List<string> astrSourceWords = new List<string>();
				while (xpMapIterator.MoveNext())
				{
					// find the first (next) source word element
					XPathNodeIterator xpSourceWords = xpMapIterator.Current.Select("aikb:TU", manager);

					progressBarAdaptationFile.Value = 0;
					progressBarAdaptationFile.Maximum = xpSourceWords.Count;
					while (xpSourceWords.MoveNext())
					{
						progressBarAdaptationFile.Value++;
						// get an iterator for the source word attribute (so we can change it later)
						XPathNodeIterator xpSourceWord = xpSourceWords.Current.Select("@k", manager);
						if (xpSourceWord.MoveNext())
						{
							// get and convert the source word value
							string strSourceWord = xpSourceWord.Current.Value;
							string strSourceWordConverted = ((aEcSource != null) && !String.IsNullOrEmpty(strSourceWord)) ? aEcSource.Convert(strSourceWord) : strSourceWord;

							// if this word has already come up before, then we have to do something else (TODO: e.g. merge the two)
							if (astrSourceWords.Contains(strSourceWordConverted))
							{
								AppendStatusOutputNl("Found two source words that have the same value: ", m_strStatusBoxDefaultFont, true);
								AppendStatusOutput(strSourceWordConverted, m_strDefaultUnicodeFontSource, true);

								while (astrSourceWords.Contains(strSourceWordConverted))
									strSourceWordConverted = "DuplicateOf:" + strSourceWordConverted;

								AppendStatusOutput(". Changing it to: ", m_strStatusBoxDefaultFont, true);
								AppendStatusOutput(strSourceWordConverted, m_strDefaultUnicodeFontSource, true);
							}

							AppendStatusOutputNl("source: ", m_strStatusBoxDefaultFont, false);
							AppendStatusOutput(strSourceWord, strFontNameSource, false);

							if (strSourceWord != strSourceWordConverted)
							{
								// otherwise, just change the value in-situ
								xpSourceWord.Current.SetValue(strSourceWordConverted);
								astrSourceWords.Add(strSourceWordConverted);

								AppendStatusOutput(", becomes: ", m_strStatusBoxDefaultFont, false);
								AppendStatusOutput(strSourceWordConverted, m_strDefaultUnicodeFontSource, false);
							}
							else
							{
								AppendStatusOutput(cstrNoChange, m_strStatusBoxDefaultFont, false);
							}

							// now get an iterator for all of the target words for the current source word
							//  (here we'll go directly to the attribute value)
							XPathNodeIterator xpTargetWords = xpSourceWords.Current.Select("aikb:RS", manager);

							// maintain a list of all target words in case the conversion merges them.
							List<string> astrTargetWords = new List<string>();
							while (xpTargetWords.MoveNext())
							{
								XPathNodeIterator xpTargetWord = xpTargetWords.Current.Select("@a", manager);
								if (xpTargetWord.MoveNext())
								{
									// get and convert the target word
									string strTargetWord = xpTargetWord.Current.Value;
									string strTargetWordConverted = ((aEcTarget != null) && !String.IsNullOrEmpty(strTargetWord)) ? aEcTarget.Convert(strTargetWord) : strTargetWord;

									// if this word has already come up before, then we have to do something else (TODO: e.g. merge the two)
									if (astrTargetWords.Contains(strTargetWordConverted))
									{
										AppendStatusOutputNl("Found two target words that have the same value: ", m_strStatusBoxDefaultFont, true);
										AppendStatusOutput(strTargetWordConverted, m_strDefaultUnicodeFontTarget, true);

										while (astrTargetWords.Contains(strTargetWordConverted))
											strTargetWordConverted = "DuplicateOf:" + strTargetWordConverted;

										AppendStatusOutput(". Changing it to: ", m_strStatusBoxDefaultFont, true);
										AppendStatusOutput(strTargetWordConverted, m_strDefaultUnicodeFontTarget, true);
									}

									AppendStatusOutputNl("target: ", m_strStatusBoxDefaultFont, false);
									AppendStatusOutput(strTargetWord, strFontNameTarget, false);

									if (strTargetWord != strTargetWordConverted)
									{
										// otherwise, just change the value in-situ
										xpTargetWord.Current.SetValue(strTargetWordConverted);
										astrTargetWords.Add(strTargetWordConverted);

										AppendStatusOutput(", becomes: ", m_strStatusBoxDefaultFont, false);
										AppendStatusOutput(strTargetWordConverted, m_strDefaultUnicodeFontTarget, false);
									}
									else
									{
										AppendStatusOutput(cstrNoChange, m_strStatusBoxDefaultFont, false);
									}
								}
							}
						}
					}
				}

				// now write it to the target folder
				XmlTextWriter writer = new XmlTextWriter(strKbFileTarget, Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				doc.Save(writer);

				progressBarAdaptationFile.Visible = false;
			}
			catch (System.Data.DataException ex)
			{
				if (ex.Message == "A child row has multiple parents.")
				{
					// this happens when the knowledge base has invalid data in it (e.g. when there is two
					//  canonically equivalent words in different records). This is technically a bug in
					//  AdaptIt.
					throw new ApplicationException(String.Format("The AdaptIt xml file '{0}' has invalid data in it! Contact silconverters_support@sil.org", strKbFileSource), ex);
				}

				throw ex;
			}
			catch (Exception ex)
			{
				throw new ApplicationException(String.Format("Unable to process the AdaptIt xml file '{0}'. Contact silconverters_support@sil.org", strKbFileSource), ex);
			}
		}

		protected void ConvertAdaptationFile(string strAdaptationFileSource, string strAdaptationFileTarget,
			IEncConverter aEcSource, IEncConverter aEcTarget, IEncConverter aEcGloss, string strFontNameSource, string strFontNameConvertedSource)
		{
			// Since AdaptIt will make different records for two words which are canonically
			//  equivalent, if we use the class object to read it in via ReadXml, that will throw
			//  an exception in such a case. So see if using XmlDocument is any less restrictive
			try
			{
				string strTempAdaptationFileSource = strAdaptationFileSource;
				CheckForIncorrectEncodingString(ref strTempAdaptationFileSource);
				AppendStatusOutputNl("Processing Adaptation file: " + strAdaptationFileSource, m_strStatusBoxDefaultFont, true);
				XmlDocument doc = new XmlDocument();
				doc.Load(strTempAdaptationFileSource);
				XmlNodeList xnlBucketIterator = doc.SelectNodes("/AdaptItDoc//S");  // use "//" since the 'S' element can be embedded

				// go thru it all looking for extra fields we'll need to convert from within the 'm', 'mm' fields.
				foreach (XmlNode xnBucket in xnlBucketIterator)
				{
					XmlAttributeCollection xacAttributes = xnBucket.Attributes;
					CheckForEmbeddedMarkers(xacAttributes, "m", aEcSource);
					CheckForEmbeddedMarkers(xacAttributes, "mm", aEcSource);
				}

				// ask the user to decide about these fields
				FilteredFieldsForm dlg = new FilteredFieldsForm(m_mapFilteredSfms, m_astrSfmsToConvert, m_astrSfmsToNotConvert,
					m_mapFonts[strFontNameSource], m_mapFonts[strFontNameConvertedSource], aEcSource);

				if (dlg.ShowDialog() == DialogResult.OK)
					dlg.DivyUpSfmsToConvert(m_astrSfmsToConvert, m_astrSfmsToNotConvert);

				progressBarAdaptationFile.Visible = true;
				progressBarAdaptationFile.Value = 0;
				progressBarAdaptationFile.Maximum = xnlBucketIterator.Count;
				foreach (XmlNode xnBucket in xnlBucketIterator)
				{
					progressBarAdaptationFile.Value++;

					// convert: <S s="I" k="I" t="v3.0" a="v3.0"
					XmlAttributeCollection	xacAttributes = xnBucket.Attributes;
					ConvertAttribute(xacAttributes, "s", aEcSource);
					ConvertAttribute(xacAttributes, "k", aEcSource);
					ConvertAttribute(xacAttributes, "pp", aEcSource);
					ConvertAttribute(xacAttributes, "fp", aEcSource);
					ConvertAttribute(xacAttributes, "mp", aEcSource);
					ConvertAttribute(xacAttributes, "t", aEcTarget);
					ConvertAttribute(xacAttributes, "a", aEcTarget);
					ConvertAttribute(xacAttributes, "g", aEcGloss);

					if (m_astrSfmsToConvert.Count > 0)
					{
						ConvertEmbeddedMarkers(xacAttributes, "m", aEcSource, m_astrSfmsToConvert);
						ConvertEmbeddedMarkers(xacAttributes, "mm", aEcSource, m_astrSfmsToConvert);
					}
				}

				// now write it to the target folder
				InsureDirectoryExists(strAdaptationFileTarget);
				XmlTextWriter writer = new XmlTextWriter(strAdaptationFileTarget, Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				doc.Save(writer);

				progressBarAdaptationFile.Visible = false;
			}
			catch (System.Data.DataException ex)
			{
				if (ex.Message == "A child row has multiple parents.")
				{
					// this happens when the knowledge base has invalid data in it (e.g. when there is two
					//  canonically equivalent words in different records). This is technically a bug in
					//  AdaptIt.
					throw new ApplicationException(String.Format("The AdaptIt xml file '{0}' has invalid data in it! Contact silconverters_support@sil.org", strAdaptationFileSource), ex);
				}

				throw ex;
			}
			catch (Exception ex)
			{
				throw new ApplicationException(String.Format("Unable to process the AdaptIt xml file '{0}'. Contact silconverters_support@sil.org", strAdaptationFileSource), ex);
			}
		}

		protected void ConvertEmbeddedMarkers(XmlAttributeCollection xnAttributes, string strAttributeName, IEncConverter aEC, List<string> astrSfmsToConvert)
		{
			XmlAttribute xa = xnAttributes[strAttributeName];
			if (xa != null)
			{
				string strValue = xa.Value;
				if (!String.IsNullOrEmpty(strValue))
				{
					// iterate thru the chunks of sfm data and convert them with the source converter if in the
					//  list of SFMs to convert
					int nIndexThat, nIndexBOC, nLengthContents, nIndexThis = strValue.IndexOf('\\');
					List<string> astrOpenedMarkers = new List<string>();
					while (nIndexThis != -1)
					{
						string strMarker, strContents;
						if (FindMarkerAndContents(strValue, nIndexThis, out nIndexThat, out nIndexBOC,
							out strMarker, out strContents, out nLengthContents, astrOpenedMarkers))
						{
							// check if it's in the list of SFMs to convert
							if (astrSfmsToConvert.Contains(strMarker))
							{
								string strContentsConverted = aEC.Convert(strContents);
								strValue = ReplaceSubstring(strValue, nIndexThis + nIndexBOC, nLengthContents, strContentsConverted);
							}
						}

						nIndexThis = nIndexThat;
					}

					xa.Value = strValue;
				}
			}
		}

		// see if the user has some hidden SFM marker which we'll ask whether they want converted or not
		// the value could be things like:
		//  "\c 1  \v 1 "
		// or
		/*
		//  "\~FILTER \note a note
a second line
a third line
it is okay to type ENTER to begin each new line
the end \note* \~FILTER* \~FILTER \free This is a bit of free translation. \free* \~FILTER* "
		*/
		protected void CheckForEmbeddedMarkers(XmlAttributeCollection xnAttributes, string strAttributeName, IEncConverter aEC)
		{
			XmlAttribute xa = xnAttributes[strAttributeName];
			if (xa != null)
			{
				string strValue = xa.Value;
				if (!String.IsNullOrEmpty(strValue))
				{
					// put all text between "\" into an array of strings
					int nIndexThat, nIndexBOC, nLengthContents, nIndexThis = strValue.IndexOf('\\');
					List<string> astrOpenedMarkers = new List<string>();
					while (nIndexThis != -1)
					{
						string strMarker, strContents;
						if (FindMarkerAndContents(strValue, nIndexThis, out nIndexThat, out nIndexBOC,
							out strMarker, out strContents, out nLengthContents, astrOpenedMarkers))
						{
							List<string> astrSfmContents;
							if (!m_mapFilteredSfms.TryGetValue(strMarker, out astrSfmContents))
							{
								astrSfmContents = new List<string>();
								m_mapFilteredSfms.Add(strMarker, astrSfmContents);
							}

							// do about 20 occurrences max (just in case there's lots!)
							if (astrSfmContents.Count < cnMaxExamples)
								astrSfmContents.Add(strContents);
						}

						// check the next chunk
						nIndexThis = nIndexThat;
					}
				}
			}
		}

		protected bool FindMarkerAndContents(string strValue, int nIndexThis, out int nIndexThat, out int nIndexBOC,
			out string strMarker, out string strContents, out int nLengthContents, List<string> astrOpenedMarkers)
		{
			nIndexThat = strValue.IndexOf('\\', nIndexThis + 1);
			string strMarkerPlusContents;
			if (nIndexThat != -1)
				strMarkerPlusContents = strValue.Substring(nIndexThis, nIndexThat - nIndexThis);
			else
				strMarkerPlusContents = strValue.Substring(nIndexThis);

			// get the marker
			nIndexBOC = strMarkerPlusContents.IndexOfAny(caSfmTerminators, 1) + 1;
			if (nIndexBOC == 0)
				nIndexBOC = strMarkerPlusContents.Length + 1;

			strMarker = strMarkerPlusContents.Substring(0, nIndexBOC - 1);
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(strMarker), "bad assumption (1) about contents in adaptation file. Send to silconverters_support@sil.org for help");

			bool bHasContents = false;
			nLengthContents = strMarkerPlusContents.Length - nIndexBOC - 1; // extra space added by AI at the end
			if (nLengthContents > 0)
			{
				strContents = strMarkerPlusContents.Substring(nIndexBOC, nLengthContents);
				bHasContents = true;
			}
			else
				strContents = null;

			// keep track of the last marker that was opened (since we may have to go back to it)
			if (strMarker.Substring(strMarker.Length - 1, 1) == cstrSfmOff)
			{
				// this is a closing marker, which by definition have no contents.
				// this assert is checking that it has a matching opening field
				System.Diagnostics.Debug.Assert(((astrOpenedMarkers.Count > 1) || !bHasContents), "bad assumption (2) about contents in adaptation file. Send to silconverters_support@sil.org for help");
				if (astrOpenedMarkers.Count > 1)
				{
					int nLastIndex = astrOpenedMarkers.Count - 1;
					astrOpenedMarkers.RemoveAt(nLastIndex--);
					strMarker = astrOpenedMarkers[nLastIndex];
				}
			}
			else
			{
				astrOpenedMarkers.Add(strMarker);
			}

			return bHasContents;
		}

		protected void ConvertAttribute(XmlAttributeCollection xnAttributes, string strAttributeName, IEncConverter aEC)
		{
			XmlAttribute xa = xnAttributes[strAttributeName];
			if (xa != null)
			{
				string strValue = xa.Value;

				if (!String.IsNullOrEmpty(strValue))
				{
					string strValueConverted = (aEC != null) ? aEC.Convert(strValue) : strValue;
					xa.Value = strValueConverted;
				}
			}
		}

		protected void Reset()
		{
			m_nLinesWritten = 0;
			m_mapFonts.Clear();
			m_mapFilteredSfms.Clear();
			m_astrSfmsToConvert.Clear();
			m_astrSfmsToNotConvert.Clear();

			// if the user is using proper USFM or PNGSIL, then Bruce doesn't want them to be presented
			//  with the FilteredFieldsForm. This can be avoided by simply loading the footnote and cross-ref
			//  fields directly into the list of fields to convert (so they won't show up in that form).
			//  But we use an installation switch to decide this: if the user checks a checkbox, then the
			//  registry key HKEY_CURRENT_USER\Software\SIL\AdaptIt2Unicode[UsingUSFM] == "Yes"
			RegistryKey keyUsingUSFM = Registry.CurrentUser.OpenSubKey(@"Software\SIL\AdaptIt2Unicode", false);
			if (keyUsingUSFM != null)
			{
				string strUsingUSFMValue = (string)keyUsingUSFM.GetValue(cstrUsingUsfmRegKeyValue);
				if (strUsingUSFMValue == "Yes")
				{
					foreach (string strSfm in Properties.Settings.Default.DefaultFieldsToConvert)
						m_astrSfmsToConvert.Add(strSfm);

					foreach (string strSfm in Properties.Settings.Default.DefaultFieldsToNotConvert)
						m_astrSfmsToNotConvert.Add(strSfm);
				}
			}
		}

		protected bool IsLegacyToUnicode
		{
			get
			{
				// return this.radioButtonLegacy.Checked;
				return true;
			}
		}

		protected AIProjectType ProjectType
		{
			get
			{
				return (IsLegacyToUnicode) ? AIProjectType.eLegacy : AIProjectType.eUnicode;
			}
		}

		protected AIProjectType OppositeProjectType
		{
			get
			{
				return (IsLegacyToUnicode) ? AIProjectType.eUnicode : AIProjectType.eLegacy;
			}
		}

		protected string ProjectsFolder(AIProjectType type)
		{
			string strProjectsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (type == AIProjectType.eLegacy)
				strProjectsFolder += cstrAdaptItProjectFolderAnsi;
			else
				strProjectsFolder += cstrAdaptItProjectFolderUnicode;
			return strProjectsFolder;
		}

		protected void InsureDirectoryExists(string strFilePath)
		{
			string strFolderPath = Path.GetDirectoryName(strFilePath);
			Directory.CreateDirectory(strFolderPath);
		}

		protected void InitProjectInfo()
		{
			listBoxProjects.Items.Clear();
			string strProjectsFolder = ProjectsFolder(ProjectType);
			if (Directory.Exists(strProjectsFolder))
			{
				string[] astrProjectFolders = Directory.GetDirectories(strProjectsFolder, "* to * adaptations", SearchOption.TopDirectoryOnly);
				foreach (string strProjectFolder in astrProjectFolders)
				{
					string strProjectName = Path.GetFileNameWithoutExtension(strProjectFolder);
					listBoxProjects.Items.Add(strProjectName);
					InitStatusBox();
				}
			}
			else
			{
				richTextBoxStatus.Text = "Unable to find any projects of the selected type!";
			}
		}

		protected void InitStatusBox()
		{
			richTextBoxStatus.Clear();

			if (!String.IsNullOrEmpty(m_strFontNameSource))
				AppendStatusOutput("Source Language Font: " + m_strFontNameSource + CRLF, m_strStatusBoxDefaultFont, true);

			if (!String.IsNullOrEmpty(m_strFontNameTarget))
				AppendStatusOutput("Target Language Font: " + m_strFontNameTarget + CRLF, m_strStatusBoxDefaultFont, true);

			if (m_strDefaultUnicodeFontSource != m_strStatusBoxDefaultFont)
				AppendStatusOutput("Source Language Font after conversion: " + m_strDefaultUnicodeFontSource + CRLF, m_strStatusBoxDefaultFont, true);

			if (m_strDefaultUnicodeFontTarget != m_strStatusBoxDefaultFont)
				AppendStatusOutput("Target Language Font after conversion: " + m_strDefaultUnicodeFontTarget + CRLF, m_strStatusBoxDefaultFont, true);

			AppendStatusOutput("Select a project, configure the Converters to use, and click 'Convert'", m_strStatusBoxDefaultFont, true);
		}

		private void radioButtonProjectName_CheckedChanged(object sender, EventArgs e)
		{
			InitProjectInfo();
		}

		private EncConverters m_aECs = new EncConverters();

		public EncConverters GetEncConverters
		{
			get { return m_aECs; }
		}

		private void buttonSourceConverter_Click(object sender, EventArgs e)
		{
			ConvType eFilter = (IsLegacyToUnicode) ? ConvType.Legacy_to_from_Unicode : ConvType.Unicode_to_from_Legacy;
			m_aEcSource = GetEncConverters.AutoSelectWithTitle(eFilter, "Choose Source Language Converter");
			if (m_aEcSource != null)
				labelSourceLanguageConverter.Text = m_aEcSource.Name;
			else
				labelSourceLanguageConverter.Text = cstrDefaultConvertLabelSource;
		}

		private void buttonTargetLanguageConverter_Click(object sender, EventArgs e)
		{
			ConvType eFilter = (IsLegacyToUnicode) ? ConvType.Legacy_to_from_Unicode : ConvType.Unicode_to_from_Legacy;
			m_aEcTarget = GetEncConverters.AutoSelectWithTitle(eFilter, "Choose Target Language Converter");
			if (m_aEcTarget != null)
				labelTargetLanguageConverter.Text = m_aEcTarget.Name;
			else
				labelTargetLanguageConverter.Text = cstrDefaultConvertLabelTarget;
		}

		void buttonSelectGlossConverter_Click(object sender, System.EventArgs e)
		{
			ConvType eFilter = (IsLegacyToUnicode) ? ConvType.Legacy_to_from_Unicode : ConvType.Unicode_to_from_Legacy;
			m_aEcGloss = GetEncConverters.AutoSelectWithTitle(eFilter, "Choose Glossing Language Converter");
			if (m_aEcGloss != null)
				labelGlossLanguageConverter.Text = m_aEcGloss.Name;
			else
				labelGlossLanguageConverter.Text = cstrDefaultConvertLabelGloss;
		}

		protected void ConvertProcessing()
		{
			// if they don't configure at least one EncConverter, then there's nothing to do:
			if ((m_aEcSource == null) && (m_aEcTarget == null) && (m_aEcGloss == null))
				throw new ApplicationException("If you don't choose a converter, there's nothing for me to do!");

			Reset();
			InitStatusBox();

			// requested project to convert
			string strSelectedProjectName = (string)listBoxProjects.SelectedItem;

			// make sure the target folder exists
			string strProjectsFolderTarget = ProjectsFolder(OppositeProjectType);
			if (!Directory.Exists(strProjectsFolderTarget))
				Directory.CreateDirectory(strProjectsFolderTarget);

			// get the directory spec for the source and target project folders
			string strProjectFolderSource = String.Format(@"{0}\{1}", ProjectsFolder(ProjectType), strSelectedProjectName);
			string strProjectFolderTarget = String.Format(@"{0}\{1}", strProjectsFolderTarget, strSelectedProjectName);

			DialogResult res;
			if (Directory.Exists(strProjectFolderTarget))
			{
				res = MessageBox.Show(String.Format("The project '{1}' already exists in the Unicode project folder.{0}Do you want to remove it?", Environment.NewLine, strSelectedProjectName), cstrCaption, MessageBoxButtons.YesNoCancel);
				if (res == DialogResult.Yes)
					DeleteFolder(strProjectFolderTarget, true);
				else if (res == DialogResult.Cancel)
					AbortProcessing();
			}

			// copy over the project file first
			string strProjectFileSource = String.Format(@"{0}\{1}",
				strProjectFolderSource, cstrAdaptItProjectFilename);
			string strProjectFileTarget = String.Format(@"{0}\{1}",
				strProjectFolderTarget, cstrAdaptItProjectFilename);

			ConvertProjectFile(strProjectFileSource, strProjectFileTarget,
				m_aEcSource, m_aEcTarget, m_strFontNameSource, m_strFontNameTarget);

			// copy over the main KB
			string strKbFileSource = String.Format(@"{0}\{1}.xml", strProjectFolderSource, strSelectedProjectName);
			string strKbFileTarget = String.Format(@"{0}\{1}.xml", strProjectFolderTarget, strSelectedProjectName);
			ConvertKbFile(strKbFileSource, strKbFileTarget, m_aEcSource, m_aEcTarget, m_strFontNameSource, m_strFontNameTarget);

			// copy over the glossing KB
			string strGlossingKbFileSource = String.Format(@"{0}\Glossing.xml", strProjectFolderSource);
			string strGlossingKbFileTarget = String.Format(@"{0}\Glossing.xml", strProjectFolderTarget);
			ConvertKbFile(strGlossingKbFileSource, strGlossingKbFileTarget, m_aEcSource, m_aEcGloss, m_strFontNameSource, m_strFontNameTarget);

			// create the Adaptations sub-folder
			string strAdaptationsFolderSource = String.Format(@"{0}\Adaptations", strProjectFolderSource);
			string strAdaptationsFolderTarget = String.Format(@"{0}\Adaptations", strProjectFolderTarget);
			Directory.CreateDirectory(strAdaptationsFolderTarget);

			// res = MessageBox.Show(String.Format(Properties.Resources.ConversionCompleteString, strSelectedProjectName), cstrCaption, MessageBoxButtons.RetryCancel);
			// if (res == DialogResult.Retry)
			{
				// copy the adaptation files
				string[] astrAdaptationFiles = Directory.GetFiles(strAdaptationsFolderSource, "*.xml", SearchOption.AllDirectories);
				int nSourceFolderLength = strAdaptationsFolderSource.Length;
				foreach (string strAdaptationFileSource in astrAdaptationFiles)
				{
					string strAdaptationFileTarget = strAdaptationsFolderTarget + strAdaptationFileSource.Substring(nSourceFolderLength);
					ConvertAdaptationFile(strAdaptationFileSource, strAdaptationFileTarget,
						m_aEcSource, m_aEcTarget, m_aEcGloss, m_strFontNameSource, m_strDefaultUnicodeFontSource);
				}
			}

			AppendStatusOutputNl(Environment.NewLine + "Conversion Complete! Next: edit the new project with Adapt It Unicode and click on 'Edit', 'Preferences', configure the fonts to use for the source and target words", m_strStatusBoxDefaultFont, true);
		}

		private void buttonConvert_Click(object sender, EventArgs e)
		{
			Cursor = Cursors.WaitCursor;
			try
			{
				ConvertProcessing();
			}
			catch (Exception ex)
			{
				string strErrorMessage = ex.Message;
				if (ex.InnerException != null)
					strErrorMessage += Environment.NewLine + Environment.NewLine + ex.InnerException.Message;
				MessageBox.Show(strErrorMessage, cstrCaption);
			}
			Cursor = Cursors.Default;
		}

		protected void DeleteFolder(string strFolder, bool bRecursive)
		{
			while (true)
			{
				try
				{
					Directory.Delete(strFolder, bRecursive);
					return;
				}
				catch (Exception ex)
				{
					DialogResult res = MessageBox.Show(String.Format("Unable to delete folder{0}{0}{1}{0}{0}because, {2}{0}{0}Try again?",
						Environment.NewLine, strFolder, ex.Message), cstrCaption, MessageBoxButtons.YesNoCancel);
					if (res != DialogResult.Yes)
						AbortProcessing();
				}
			}
		}

		protected void AbortProcessing()
		{
			throw new ApplicationException("End Processing");
		}

		// for a Legacy AI Project, the punctuation is in adjacent characters e.g.:
		//  "??..,,;;::""!!(())<<>>[[]]{{}}||““””‘‘’’"
		protected void UnpackPunctuationLegacy(string strPunctuation, out char[] aPunctSource, out char[] aPunctTarget)
		{
			// initialize the output char arrays
			int nLen = strPunctuation.Length / 2;
			aPunctSource = new char[nLen];
			aPunctTarget = new char[nLen];

			// put the delimiter chars into a char array (for use in a later 'Split' of the input string)
			int i = 0, j = 0;
			while (i < nLen)
			{
				aPunctSource[i] = strPunctuation[j++];
				aPunctTarget[i++] = strPunctuation[j++];
			}
		}

		protected void GetXmlDocument(string strKnowledgeBaseFileSpec, out XmlDocument doc, out XPathNavigator navigator, out XmlNamespaceManager manager)
		{
			doc = new XmlDocument();
			doc.Load(strKnowledgeBaseFileSpec);
			navigator = doc.CreateNavigator();
			manager = new XmlNamespaceManager(navigator.NameTable);
			manager.AddNamespace("aikb", cstrAdaptItXmlNamespace);
		}

		protected void GetProjectFonts(string strProjectFileSource, out string strFontNameSource, out string strFontNameTarget)
		{
			string strProjectFileContents = File.ReadAllText(strProjectFileSource);

			int nIndex = strProjectFileContents.IndexOf(cstrAdaptItFontNameDelimiter) + cstrAdaptItFontNameDelimiter.Length;
			int nLength = strProjectFileContents.IndexOfAny(caSplitChars, nIndex) - nIndex;
			strFontNameSource = strProjectFileContents.Substring(nIndex, nLength);

			Font font;
			if (!m_mapFonts.TryGetValue(strFontNameSource, out font))
			{
				font = new Font(strFontNameSource, m_nStatusBoxDefaultFontSize);
				m_mapFonts.Add(strFontNameSource, font);
			}

			nIndex = strProjectFileContents.IndexOf(cstrAdaptItFontNameDelimiter, nIndex) + cstrAdaptItFontNameDelimiter.Length;
			nLength = strProjectFileContents.IndexOfAny(caSplitChars, nIndex) - nIndex;
			strFontNameTarget = strProjectFileContents.Substring(nIndex, nLength);

			if (!m_mapFonts.TryGetValue(strFontNameTarget, out font))
			{
				font = new Font(strFontNameTarget, m_nStatusBoxDefaultFontSize);
				m_mapFonts.Add(strFontNameTarget, font);
			}
		}

		protected string ReplaceSubstring(string strContents, int nIndex, int nLength, string strReplacement)
		{
			string str = strContents.Substring(0, nIndex);
			str += strReplacement;
			str += strContents.Substring(nIndex + nLength);
			return str;
		}

		protected void AppendStatusOutputNl(string str, string strFontName, bool bForceOutput)
		{
			if (!bForceOutput && (m_nLinesWritten++ == cnMaxExamples))
			{
				AppendStatusOutput(Environment.NewLine + "Skipping the rest...", m_strStatusBoxDefaultFont, true);
				richTextBoxStatus.ScrollToCaret();
			}
			else if (bForceOutput || (m_nLinesWritten < cnMaxExamples))
			{
				AppendStatusOutput(Environment.NewLine + str, strFontName, bForceOutput);
				richTextBoxStatus.ScrollToCaret();
			}
		}

		protected void AppendStatusOutput(string str, string strFontName, bool bForceOutput)
		{
			if (bForceOutput || (m_nLinesWritten < cnMaxExamples))
			{
				Font font;
				if (!m_mapFonts.TryGetValue(strFontName, out font))
				{
					font = new Font(strFontName, m_nStatusBoxDefaultFontSize);
					m_mapFonts.Add(strFontName, font);
				}

				int nStart = richTextBoxStatus.TextLength;

				richTextBoxStatus.AppendText(str);
				richTextBoxStatus.SelectionStart = nStart;
				richTextBoxStatus.SelectionLength = str.Length;
				richTextBoxStatus.SelectionFont = font;
			}
		}

		private void listBoxProjects_SelectedIndexChanged(object sender, EventArgs e)
		{
			buttonConvert.Enabled = (listBoxProjects.SelectedIndex != -1);
			if (buttonConvert.Enabled)
			{
				string strSelectedProjectName = (string)listBoxProjects.SelectedItem;

				// get the file spec for the project configuration file (so we can harvest the font names)
				string strProjectFileSource = String.Format(@"{0}\{1}\{2}", ProjectsFolder(ProjectType), strSelectedProjectName, cstrAdaptItProjectFilename);

				// reset all the details
				m_aEcGloss = m_aEcSource = m_aEcTarget = null;
				m_strFontNameSource = m_strFontNameTarget = null;
				labelSourceLanguageConverter.Text = cstrDefaultConvertLabelSource;
				labelTargetLanguageConverter.Text = cstrDefaultConvertLabelTarget;
				labelGlossLanguageConverter.Text = cstrDefaultConvertLabelGloss;
				m_strDefaultUnicodeFontSource = m_strStatusBoxDefaultFont;
				m_strDefaultUnicodeFontTarget = m_strStatusBoxDefaultFont;

				// get the fonts associated with this project
				GetProjectFonts(strProjectFileSource, out m_strFontNameSource, out m_strFontNameTarget);

				// see if there's a converter assigned in the repository for these font names
				SetConverter(m_strFontNameSource, ref m_aEcSource, labelSourceLanguageConverter, ref m_strDefaultUnicodeFontSource);
				SetConverter(m_strFontNameTarget, ref m_aEcTarget, labelTargetLanguageConverter, ref m_strDefaultUnicodeFontTarget);

				InitStatusBox();
			}
		}

		protected void SetConverter(string strFontName, ref IEncConverter aEC, Label aLabel, ref string strConvertedFontName)
		{
			string strMappingName = GetEncConverters.GetMappingNameFromFont(strFontName);
			if (!String.IsNullOrEmpty(strMappingName))
			{
				aEC = GetEncConverters[strMappingName];
				if (aEC != null)
					aLabel.Text = aEC.Name;

				// also see if we have a default font for the converted text
				string[] astrFontMappings = GetEncConverters.GetFontMapping(aEC.Name, strFontName);
				if (astrFontMappings.Length > 0)
					strConvertedFontName = astrFontMappings[0];
			}

			Font font;
			if (!m_mapFonts.TryGetValue(strConvertedFontName, out font))
			{
				font = new Font(strConvertedFontName, m_nStatusBoxDefaultFontSize);
				m_mapFonts.Add(strConvertedFontName, font);
			}
		}

		protected void CheckForIncorrectEncodingString(ref string strXmlFilename)
		{
			Encoding enc = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage);   // use one that shouldn't clobber anything

			string strXmlFileContents = File.ReadAllText(strXmlFilename, enc);
			int nIndex = strXmlFileContents.IndexOf(cstrEncodingNameDelimiter) + cstrEncodingNameDelimiter.Length;
			int nLength = strXmlFileContents.IndexOf("\"", nIndex) - nIndex;
			string strEncodingName = strXmlFileContents.Substring(nIndex, nLength);

			if (strEncodingName.ToLower() == "utf-8")
			{
				AppendStatusOutputNl(String.Format("Found invalid encoding string '{0}' in legacy xml file: '{1}' ",
					strEncodingName, strXmlFilename), m_strStatusBoxDefaultFont, true);

				// if we have information from the repository, maybe we can fix this
				if (m_aEcSource != null)
				{
					// the "symbol" code page apparently isn't valid for GetEncoding, so use the cp for ISO-8859-1
					Encoding encHeader;
					if (m_aEcSource.CodePageInput == EncConverters.cnSymbolFontCodePage)
						encHeader = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage);
					else
						encHeader = Encoding.GetEncoding(m_aEcSource.CodePageInput);

					strXmlFileContents = strXmlFileContents.Replace("UTF-8", encHeader.HeaderName);
					strXmlFileContents = strXmlFileContents.Replace("utf-8", encHeader.HeaderName);

					// put it in a temporary file so we don't possibly trash the original files
					strXmlFilename = Path.GetTempFileName();
					File.WriteAllText(strXmlFilename, strXmlFileContents, enc);

					AppendStatusOutput(String.Format("copying to: '{0}' to fix for further processing", strXmlFilename), m_strStatusBoxDefaultFont, true);
				}
				else
					throw new ApplicationException(String.Format("Unable to fix bad encoding string '{0}' in xml file: '{1}'! You should upgrade your version of AdaptIt (regular) and re-edit/save all the adapted texts before trying to do this conversion", strEncodingName, strXmlFilename));
			}
		}
	}
}