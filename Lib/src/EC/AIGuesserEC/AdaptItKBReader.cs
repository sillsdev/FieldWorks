using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;       // for Dictionary<>
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Win32;                  // for RegistryKey
using System.Diagnostics;               // for Debug
using System.IO;                        // for file I/O
using System.Xml;                       // for XMLDocument
using System.Xml.XPath;                 // for XPathNavigator
using SilEncConverters40;               // for EncConverter (base-class) definition
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters40
{
	/// <summary>
	/// Base class for an AdaptIt Knowledge Base EncConverter
	/// </summary>
	public abstract class AdaptItKBReader : EncConverter
	{
		#region Member Variable Definitions

		public abstract string strRegValueForConfigProgId { get; }

		protected DateTime m_timeModifiedKB = DateTime.MinValue;
		protected DateTime m_timeModifiedKbMapOfMaps = DateTime.MinValue;
		protected DateTime m_timeModifiedProj = DateTime.MinValue;
		protected DateTime m_timeModifiedNorm = DateTime.MinValue;
		protected Dictionary<int, Dictionary<string, string>> m_mapOfMaps = new Dictionary<int, Dictionary<string, string>>();
		protected Dictionary<int, Dictionary<string, string>> m_mapOfReversalMaps;
		protected Dictionary<string, List<string>> m_mapNormalFormToSourcePhrases = new Dictionary<string, List<string>>();
		protected char[] m_caDelimitersForward;
		protected char[] m_caDelimitersReverse;
		protected string      m_strKnowledgeBaseFileSpec;
		protected string m_strNormalizerFileSpec;
		protected string m_strProjectFileSpec;
		protected bool        m_bLegacy;
		protected bool        m_bReverseLookup;   // we have to keep track of the direction since it might be different than m_bForward
		protected bool m_bHasNamespace = true;
		protected string m_strTargetWordFontName;
		protected string m_strSourceWordFontName;
		internal MapOfMaps MapOfMaps;   // this is a special XElement implementation used to edit the KB

		public string cstrAdaptItXmlNamespace = "http://www.sil.org/computing/schemas/AdaptIt KB.xsd";
		public string cstrAdaptItProjectFilename = "AI-ProjectConfiguration.aic";
		public string cstrAdaptItPunctuationPairsNRSource = "PunctuationPairsSourceSet(stores space for an empty cell)	";
		public string cstrAdaptItPunctuationPairsNRTarget = "PunctuationPairsTargetSet(stores space for an empty cell)	";
		public string cstrAdaptItPunctuationPairsLegacy = "PunctuationPairs(stores space for an empty cell)	";
		private const string CstrSourceFont = "SourceFont";
		private const string CstrTargetFont = "TargetFont";
		private const string CstrFaceName = "FaceName";
		public static char[] CaSplitChars = new [] { '\r', '\n', '\t', ' ' };

		protected IEncConverter theNormalizerEc;

		public bool FileHasNamespace
		{
			get { return m_bHasNamespace; }
			set { m_bHasNamespace = value; }
		}

		#endregion Member Variable Definitions

		#region Initialization
		public AdaptItKBReader(string sProgId, string sImplementType)
			: base(sProgId, sImplementType)
		{
		}

		public override void Initialize(string converterName, string converterSpec,
			ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType,
			ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
			base.Initialize(converterName, converterSpec, ref lhsEncodingID, ref rhsEncodingID, ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding );

			m_bLegacy = (EncConverter.NormalizeLhsConversionType(conversionType) == NormConversionType.eLegacy);

#if !NotUsingNormalizer
			// in the new situation, the converter spec has two pieces of information:
			//  a) the same as the original one -- the path to the KB file
			//  b) the path to a normalizing entity (e.g. cct, tec, map, or a custom
			//      XML file that has the normalizing correspondences (ala. ???)
			// these two are separated by a ';'
			string[] astrPaths = converterSpec.Split(new[] {';'});
			if (astrPaths.Length >= 1)
				m_strKnowledgeBaseFileSpec = astrPaths[0];

			if (astrPaths.Length >= 2)
				m_strNormalizerFileSpec = astrPaths[1];

#else
			// get the filespec to the project file and the knowledge base
			//  (the KB path *is* the converter spec)
			m_strKnowledgeBaseFileSpec = converterSpec;
#endif
			string strProjectFolder = Path.GetDirectoryName(m_strKnowledgeBaseFileSpec);
			m_strProjectFileSpec = String.Format(@"{0}\{1}", strProjectFolder, cstrAdaptItProjectFilename);

			if( bAdding )
			{
				// if we're supposedly adding this one, then clobber our copy of its last modified
				// (there was a problem with us instantiating lots of these things in a row and
				//  not detecting the change because the modified date was within a second of each
				//  other)
				m_timeModifiedKB = m_timeModifiedProj = DateTime.MinValue;
			}
		}
		#endregion Initialization

		#region Misc helpers
		protected string XPathToKB
		{
			get
			{
				if (FileHasNamespace)
					return "/aikb:AdaptItKnowledgeBase/aikb:KB";
				return "//KB";  // use double-slash, because AI would like to keep the AdaptItKnowledgeBase element, but without the ns
			}
		}

		protected string XPathToMAP
		{
			get
			{
				if (FileHasNamespace)
					return "/aikb:AdaptItKnowledgeBase/aikb:KB/aikb:MAP";
				return "//KB/MAP";  // use double-slash, because AI would like to keep the AdaptItKnowledgeBase element, but without the ns
			}
		}

		protected string XPathToTU
		{
			get
			{
				if (FileHasNamespace)
					return "aikb:TU";
				return "TU";
			}
		}

		protected string XPathToRS
		{
			get
			{
				if (FileHasNamespace)
					return "aikb:RS";
				return "RS";
			}
		}

		protected string XPathToSpecificMAP(int nMapValue)
		{
			string str = String.Format("MAP[@mn=\"{0}\"]", nMapValue);
			if (FileHasNamespace)
				str = "aikb:" + str;
			return str;
		}

		protected string XPathToSpecificTU(string strSourceWord)
		{
			string str = String.Format("TU[@k=\"{0}\"]", strSourceWord);
			if (FileHasNamespace)
				str = "aikb:" + str;
			return str;
		}

		protected string XPathToSpecificRS(string strTargetWord)
		{
			string str = String.Format("RS[@a=\"{0}\"]", strTargetWord);
			if (FileHasNamespace)
				str = "aikb:" + str;
			return str;
		}

		protected virtual bool Load(bool bMapOfMaps)
		{
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(m_strProjectFileSpec));
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(m_strKnowledgeBaseFileSpec));

			// see if the project file timestamp has changed (in which case, we should
			//  reload the punctuation just in case it changed);
			DateTime timeModified = DateTime.Now; // don't care really, but have to initialize it.
			if( !DoesFileExist(m_strProjectFileSpec, ref timeModified) )
				EncConverters.ThrowError(ErrStatus.CantOpenReadMap, m_strProjectFileSpec);

			bool bSomethingChanged = false;

			// if we have "old" data
			if( timeModified > m_timeModifiedProj )
			{
				// get the punctuation out of the project file.
				string strProjectFileContents = null;
				using (StreamReader sr = File.OpenText(m_strProjectFileSpec))
				{
					strProjectFileContents = sr.ReadToEnd();
				}

				if( m_bLegacy ) // legacy project file does it differently
				{
					int nIndex = strProjectFileContents.IndexOf(cstrAdaptItPunctuationPairsLegacy) + cstrAdaptItPunctuationPairsLegacy.Length;
					int nLength = strProjectFileContents.IndexOfAny(CaSplitChars, nIndex) - nIndex;
					string strPunctuation = strProjectFileContents.Substring(nIndex, nLength);
					InitializeDelimitersLegacy(strPunctuation, out m_caDelimitersForward, out m_caDelimitersReverse);
					m_strTargetWordFontName = GetTargetFormFont(strProjectFileContents);
					m_strSourceWordFontName = GetSourceFormFont(strProjectFileContents);
				}
				else    // NonRoman version
				{
					int nIndex = strProjectFileContents.IndexOf(cstrAdaptItPunctuationPairsNRSource) + cstrAdaptItPunctuationPairsNRSource.Length;
					int nLength = strProjectFileContents.IndexOf('\n',nIndex) - nIndex;
					m_caDelimitersForward = ReturnDelimiters(strProjectFileContents, nIndex, nLength);
					nIndex = strProjectFileContents.IndexOf(cstrAdaptItPunctuationPairsNRTarget, nIndex) + cstrAdaptItPunctuationPairsNRTarget.Length;
					nLength = strProjectFileContents.IndexOf('\n',nIndex) - nIndex;
					m_caDelimitersReverse = ReturnDelimiters(strProjectFileContents, nIndex, nLength);
					m_strTargetWordFontName = GetTargetFormFont(strProjectFileContents);
					m_strSourceWordFontName = GetSourceFormFont(strProjectFileContents);
				}

				m_timeModifiedProj = timeModified;
				bSomethingChanged = true;
			}

			// next check on the knowledge base... make sure it's there and get the last time it was modified
			timeModified = DateTime.Now; // don't care really, but have to initialize it.
			if( !DoesFileExist(m_strKnowledgeBaseFileSpec, ref timeModified) )
				EncConverters.ThrowError(ErrStatus.CantOpenReadMap, m_strKnowledgeBaseFileSpec);

			// if we're using the new MapOfMaps class methods (like EditKb), then
			//  load *that* (otherwise, load the other)
			if (bMapOfMaps)
			{
				// if it has been modified or it's not already loaded...
				if ((MapOfMaps == null) || (timeModified > m_timeModifiedKbMapOfMaps))
				{
					MapOfMaps = MapOfMaps.LoadXml(m_strKnowledgeBaseFileSpec);

					// keep track of the modified date, so we can detect a new version to reload
					m_timeModifiedKbMapOfMaps = timeModified;
					bSomethingChanged = true;
				}
			}
			else
			{
				// if it has been modified or it's not already loaded...
				if (timeModified > m_timeModifiedKB)
				{
					m_mapOfMaps.Clear();
					m_mapOfReversalMaps = null;

#if !NotUseSchemaGeneratedClass
					// Since AdaptIt will make different records for two words which are canonically
					//  equivalent, if we use the class object to read it in via ReadXml, that will throw
					//  an exception in such a case. So see if using XmlDocument is any less restrictive
					try
					{
						XmlDocument doc;
						XPathNavigator navigator;
						XmlNamespaceManager manager;
						GetXmlDocument(out doc, out navigator, out manager);

						XPathNodeIterator xpMapIterator = navigator.Select(XPathToMAP, manager);

						List<string> astrTargetWords = new List<string>();
						while (xpMapIterator.MoveNext())
						{
							// get the map number so we can make different maps for different size phrases
							string strMapNum = xpMapIterator.Current.GetAttribute("mn", navigator.NamespaceURI);
							int nMapNum = System.Convert.ToInt32(strMapNum, 10);
							Dictionary<string, string> mapWords = new Dictionary<string, string>();
							m_mapOfMaps.Add(nMapNum, mapWords);

							XPathNodeIterator xpSourceWords = xpMapIterator.Current.Select(XPathToTU, manager);
							while (xpSourceWords.MoveNext())
							{
								XPathNodeIterator xpTargetWords = xpSourceWords.Current.Select(XPathToRS, manager);

								astrTargetWords.Clear();
								while (xpTargetWords.MoveNext())
								{
									string strTargetWord = xpTargetWords.Current.GetAttribute("a", navigator.NamespaceURI);
									astrTargetWords.Add(strTargetWord);
								}

								// if there are multiple target words for this form, then return it in Ample-like
								//  %2%target1%target% format
								string strTargetWordFull = null;
								if (astrTargetWords.Count > 1)
								{
									strTargetWordFull = String.Format("%{0}%", astrTargetWords.Count);
									foreach (string strTargetWord in astrTargetWords)
										strTargetWordFull += String.Format("{0}%", strTargetWord);
								}
								else if (astrTargetWords.Count == 1)
								{
									strTargetWordFull = astrTargetWords[0];
									if (strTargetWordFull == "<Not In KB>")
										continue;   // skip this one so we *don't* get a match later on.
								}

								string strSourceWord = xpSourceWords.Current.GetAttribute("k", navigator.NamespaceURI);
								System.Diagnostics.Debug.Assert(!mapWords.ContainsKey(strSourceWord), String.Format("The Knowledge Base has two different source records which are canonically equivalent! See if you can merge the two KB entries for word that look like, '{0}'", strSourceWord));
								mapWords[strSourceWord] = strTargetWordFull;
							}
						}
					}
					catch (System.Data.DataException ex)
					{
						if (ex.Message == "A child row has multiple parents.")
						{
							// this happens when the knowledge base has invalid data in it (e.g. when there is two
							//  canonically equivalent words in different records). This is technically a bug in
							//  AdaptIt.
							throw new ApplicationException("The AdaptIt knowledge base has invalid data in it! Contact silconverters_support@sil.org", ex);
						}

						throw ex;
					}
					catch (Exception ex)
					{
						throw new ApplicationException(
							String.Format(Properties.Resources.IDS_CantOpenAiKb,
								Environment.NewLine, ex.Message));
					}

					// keep track of the modified date, so we can detect a new version to reload
					m_timeModifiedKB = timeModified;
					bSomethingChanged = true;

#else
				AdaptItKnowledgeBase aikb = new AdaptItKnowledgeBase();
				try
				{
					aikb.ReadXml(m_strKnowledgeBaseFileSpec);
					if (aikb.KB.Count > 0)
					{
						AdaptItKnowledgeBase.KBRow aKBRow = aikb.KB[0];
						foreach (AdaptItKnowledgeBase.MAPRow aMapRow in aKBRow.GetMAPRows())
						{
							foreach (AdaptItKnowledgeBase.TURow aTURow in aMapRow.GetTURows())
							{
								string strValue = null;
								AdaptItKnowledgeBase.RSRow[] aRSRows = aTURow.GetRSRows();
								if (aRSRows.Length > 1)
								{
									// if there is more than one mapping, then make it %count%val1%val2%...
									//  so people can use the Word Pick macro to choose it
									strValue = String.Format("%{0}%", aRSRows.Length);
									foreach (AdaptItKnowledgeBase.RSRow aRSRow in aRSRows)
										strValue += String.Format("{0}%", aRSRow.a);
								}
								else if (aRSRows.Length == 1)
								{
									AdaptItKnowledgeBase.RSRow aRSRow = aRSRows[0];
									if (aRSRow.a == "<Not In KB>")
										continue;   // skip this one so we *don't* get a match later on.
									else
										strValue = aRSRow.a;
								}

								m_mapLookup[aTURow.k] = strValue;
							}
						}
					}
				}
				catch (System.Data.DataException ex)
				{
					if (ex.Message == "A child row has multiple parents.")
					{
						// this happens when the knowledge base has invalid data in it (e.g. when there is two
						//  canonically equivalent words in different records). This is technically a bug in
						//  AdaptIt.
						throw new ApplicationException("The AdaptIt knowledge base has invalid data in it! Contact silconverters_support@sil.org", ex);
					}

					throw ex;
				}
#endif
				}

				// next check on the normalizer file... if we have one...
				if (!String.IsNullOrEmpty(m_strNormalizerFileSpec))
				{
					// make sure it's there and get the last
					//  time it was modified
					timeModified = DateTime.Now; // don't care really, but have to initialize it.
					if (!DoesFileExist(m_strNormalizerFileSpec, ref timeModified))
						EncConverters.ThrowError(ErrStatus.CantOpenReadMap, m_strNormalizerFileSpec);

					// if it has been modified or it's not already loaded...
					if (timeModified > m_timeModifiedNorm)
					{
						// this map represents the lookup between unique normalized form
						//  and source phrases (i.e. words on the lhs of one of the maps)
						//  that have that same normalized form. a tryget on this map will
						//  give a List<string> of all source phrases with the same normalized
						//  form
						m_mapNormalFormToSourcePhrases.Clear();

						if (theNormalizerEc == null)
						{
							// Get the (for now) EncConverter that does the normalizing
							int nDummy = 0;
							string strDummy = null;
							int processTypeFlags = (int)ProcessTypeFlags.SpellingFixerProject;
							ConvType ct = ConvType.Unicode_to_Unicode;
							theNormalizerEc = new CcEncConverter();
							theNormalizerEc.Initialize(m_strNormalizerFileSpec, m_strNormalizerFileSpec,
								ref strDummy, ref strDummy, ref ct,
								ref processTypeFlags, nDummy, nDummy, true);
						}

						foreach (var mapOfSourceWordsToTargetWords in m_mapOfMaps.Values)
						{
							foreach (string strSourceWords in mapOfSourceWordsToTargetWords.Keys)
							{
								string strNormalizedSourceWords = CallSafeNormalizedConvert(strSourceWords);
								List<string> lstNormalizedForms;
								if (!m_mapNormalFormToSourcePhrases.TryGetValue(strNormalizedSourceWords, out lstNormalizedForms))
								{
									lstNormalizedForms = new List<string>();
									m_mapNormalFormToSourcePhrases.Add(strNormalizedSourceWords, lstNormalizedForms);
								}
								if (!lstNormalizedForms.Contains(strSourceWords))
									lstNormalizedForms.Add(strSourceWords);
							}
						}

						// keep track of the modified date, so we can detect a new version to reload
						m_timeModifiedNorm = timeModified;
						bSomethingChanged = true;
					}
				}
			}

			return bSomethingChanged;   // indicate whether the data was reinitialized or not
		}

		private string CallSafeNormalizedConvert(string strSourceWords)
		{
			try
			{
				if (theNormalizerEc != null)
					return theNormalizerEc.Convert(strSourceWords);
			}
			catch
			{
			}
			return strSourceWords;
		}

		/// <summary>
		/// return the list of words/phrases that are similar to given source word.
		/// "similar" is defined as having the same normalized form as given in by
		/// the normalizing EncConverter whose path specification was passed in as
		/// the 2nd member of this EncConverter's ConverterIdentifier.
		/// </summary>
		/// <param name="strSourceWord"></param>
		/// <returns></returns>
		public List<string> GetSimilarWords(string strSourceWord)
		{
			if (theNormalizerEc == null)
				return null;    // not "Load"ed yet -- programmer error

			// just in case it hasn't been done yet, do the Load now.
			Load(false);

			// first normalize the given word
			string strNormalizedSourceWord = CallSafeNormalizedConvert(strSourceWord);
			List<string> lstOfSourcePhrasesWithTheSameNormalizedForm;
			if (m_mapNormalFormToSourcePhrases.TryGetValue(strNormalizedSourceWord,
				out lstOfSourcePhrasesWithTheSameNormalizedForm))
			{
				// if the KB has an identity lookup (which it shouldn't but can), then
				//  don't return that one.
				int nIndex = lstOfSourcePhrasesWithTheSameNormalizedForm.IndexOf(strSourceWord);
				if (nIndex != -1)
				{
					lstOfSourcePhrasesWithTheSameNormalizedForm.RemoveAt(nIndex);

					// if it now is empty, then just return null
					if (lstOfSourcePhrasesWithTheSameNormalizedForm.Count == 0)
						return null;
				}

				return lstOfSourcePhrasesWithTheSameNormalizedForm;
			}

			return null;
		}

		private string GetSourceFormFont(string strProjectFileContents)
		{
			int nIndex = strProjectFileContents.IndexOf(CstrSourceFont);
			if (nIndex != -1)
			{
				nIndex = strProjectFileContents.IndexOf(CstrFaceName, nIndex) + CstrFaceName.Length + 1;
				int nLength = strProjectFileContents.IndexOf('\n', nIndex) - nIndex - 1;
				return strProjectFileContents.Substring(nIndex, nLength);
			}
			return "Arial Unicode MS";
		}

		private string GetTargetFormFont(string strProjectFileContents)
		{
			int nIndex = strProjectFileContents.IndexOf(CstrTargetFont);
			if (nIndex != -1)
			{
				nIndex = strProjectFileContents.IndexOf(CstrFaceName, nIndex) + CstrFaceName.Length + 1;
				int nLength = strProjectFileContents.IndexOf('\n', nIndex) - nIndex - 1;
				return strProjectFileContents.Substring(nIndex, nLength);
			}
			return "Arial Unicode MS";
		}

		protected void GetXmlDocument(out XmlDocument doc, out XPathNavigator navigator, out XmlNamespaceManager manager)
		{
			doc = new XmlDocument();
			doc.Load(m_strKnowledgeBaseFileSpec);
			navigator = doc.CreateNavigator();
			manager = new XmlNamespaceManager(navigator.NameTable);
			FileHasNamespace = (doc.InnerXml.IndexOf(cstrAdaptItXmlNamespace) != -1);
			if (FileHasNamespace)
				manager.AddNamespace("aikb", cstrAdaptItXmlNamespace);
		}

		/// <summary>
		/// This version will add a lookup pair to the Adapt It knowledge base and
		/// save the result immediately (with the negative point that it doesn't
		/// save the new item in the proper sort order). But it should be quicker
		/// than the alternative which is "AddEntryPairNoSave" + "AddEntryPairSave"
		/// </summary>
		/// <param name="strSourceWord"></param>
		/// <param name="strTargetWord"></param>
		public void AddEntryPair(string strSourceWord, string strTargetWord)
		{
			// trim up the input words in case the user didn't
			strSourceWord = strSourceWord.Trim(m_caDelimitersForward ?? CaSplitChars);
			strTargetWord = strTargetWord.Trim(m_caDelimitersReverse ?? CaSplitChars);

			// first see if this pair is already there
			int nMapValue;
			string strTargetWordsInMap;
			Dictionary<string, string> mapLookup;
			if (TryGetTargetForm(strSourceWord, out strTargetWordsInMap, out nMapValue, out mapLookup))
			{
				if ((strTargetWordsInMap == strTargetWord) || (strTargetWordsInMap.IndexOf(String.Format("%{0}%", strTargetWord)) != -1))
					return;    // already there
			}

			// otherwise, we need to add it.
#if !NotUseSchemaGeneratedClass
			try
			{
				XmlDocument doc;
				XPathNavigator navigator;
				XmlNamespaceManager manager;
				GetXmlDocument(out doc, out navigator, out manager);

				if (doc.DocumentElement != null)
				{
					XmlNode nodeKbNode = doc.DocumentElement.SelectSingleNode(XPathToKB, manager);
					if (nodeKbNode == null)
					{
						doc.CreateElement(XPathToKB);   // no KB element, so create one
						nodeKbNode = doc.DocumentElement.SelectSingleNode(XPathToKB, manager);
					}

					// see if the proper map entry is present (so we can add it, if not)
					string strMapSelect = XPathToSpecificMAP(nMapValue);
					XmlNode nodeMapEntry = nodeKbNode.SelectSingleNode(strMapSelect, manager);
					if (nodeMapEntry == null)
					{
						// if not, then add it.
						// xpathnavs are easier to use to add child elements
						XPathNavigator xpnMap = nodeKbNode.CreateNavigator();
						xpnMap.AppendChild(String.Format("<MAP mn=\"{0}\"/>", nMapValue));

						// now try it again
						nodeMapEntry = nodeKbNode.SelectSingleNode(strMapSelect, manager);
					}

					// see if the source word exists (so we can add it if not)
					string strSourceWordSelect = XPathToSpecificTU(strSourceWord);
					XmlNode nodeSourceWordEntry = nodeMapEntry.SelectSingleNode(strSourceWordSelect, manager);
					if (nodeSourceWordEntry == null)
					{
						// add it.
						XPathNavigator xpnSourceWord = nodeMapEntry.CreateNavigator();
						xpnSourceWord.AppendChild(String.Format("<TU f=\"0\" k=\"{0}\"/>", strSourceWord));

						// now try it again
						nodeSourceWordEntry = nodeMapEntry.SelectSingleNode(strSourceWordSelect, manager);
					}

					// the target word shouldn't exist (or we wouldn't be here... unless it was *just* added
					//  but to avoid doing two loads... just be sure we're not adding it twice here
					string strTargetWordSelect = XPathToSpecificRS(strTargetWord);
					XmlNode nodeTargetWordEntry = nodeSourceWordEntry.SelectSingleNode(strTargetWordSelect, manager);
					if (nodeTargetWordEntry != null)
						return; // nothing to do, because it's already in there.

					// add it.
					XPathNavigator xpnTargetWord = nodeSourceWordEntry.CreateNavigator();
					xpnTargetWord.AppendChild(String.Format("<RS n=\"1\" a=\"{0}\"/>", strTargetWord));

					File.Copy(m_strKnowledgeBaseFileSpec, m_strKnowledgeBaseFileSpec + ".bak", true);

					XmlTextWriter writer = new XmlTextWriter(m_strKnowledgeBaseFileSpec, Encoding.UTF8);
					writer.Formatting = Formatting.Indented;
					doc.Save(writer);
					writer.Close();
				}
			}
			catch (System.Data.DataException ex)
			{
				if (ex.Message == "A child row has multiple parents.")
				{
					// this happens when the knowledge base has invalid data in it (e.g. when there is two
					//  canonically equivalent words in different records). This is technically a bug in
					//  AdaptIt.
					throw new ApplicationException("The AdaptIt knowledge base has invalid data in it! Contact silconverters_support@sil.org", ex);
				}

				throw ex;
			}
#else
			AdaptItKnowledgeBase aikb = new AdaptItKnowledgeBase();
			aikb.ReadXml(m_strKnowledgeBaseFileSpec);

			// make sure there's a KB record (if AI created it, there will be, but why not allow users to create
			//  AI KBs without AI...)
			AdaptItKnowledgeBase.KBRow aKBRow = null;
			if (aikb.KB.Count == 0)
				aKBRow = aikb.KB.AddKBRow("4", null, null, "1");
			else
				aKBRow = aikb.KB[0];

			// get the proper MAP element
			AdaptItKnowledgeBase.MAPRow aMAPRow = aikb.MAP.FindBymn(nMapValue.ToString());
			if (aMAPRow == null)
			{
				// have to add it
				System.Diagnostics.Debug.Assert(aKBRow != null);
				aMAPRow = aikb.MAP.AddMAPRow(nMapValue.ToString(), aKBRow);
			}

			// get the proper Source Word element
			AdaptItKnowledgeBase.TURow aTURow = aikb.TU.FindByk(strSourceWord);
			if (aTURow == null)
			{
				System.Diagnostics.Debug.Assert(aMAPRow != null);
				aTURow = aikb.TU.AddTURow("0", strSourceWord, aMAPRow);
			}

			// see if the particular target word entry is there...
			System.Diagnostics.Debug.Assert(aTURow != null);
			foreach (AdaptItKnowledgeBase.RSRow aRSRow in aTURow.GetRSRows())
				if (aRSRow.a == strTargetWord)
					return;

			// otherwise, add a new RS element for this word
			aikb.RS.AddRSRow("1", strTargetWord, aTURow);
			File.Copy(m_strKnowledgeBaseFileSpec, m_strKnowledgeBaseFileSpec + ".bak", true);
			aikb.WriteXml(m_strKnowledgeBaseFileSpec);
#endif
		}

		// for a NonRoman AI Project, the punctuation is in adjacent rows e.g.:
		// PunctuationPairsSourceSet(stores space for an empty cell)	?.,;:"!()<>{}[]“”‘’
		// PunctuationPairsTargetSet(stores space for an empty cell)	?.,;:"!()<>{}[]“”‘’
		protected char[] ReturnDelimiters(string s, int nIndex, int nLength)
		{
			string strPunctuation = s.Substring(nIndex, nLength);

			// put the delimiter chars into a char array (for use in a later 'Split' of the input string)
			char[] aChars = new char[strPunctuation.Length + 1];
			for (int i = 0; i < strPunctuation.Length; i++)
				aChars[i] = strPunctuation[i];
			aChars[strPunctuation.Length] = ' ';    // add a space so it does an implicit Trim()
			return aChars;
		}

		// for a Legacy AI Project, the punctuation is in adjacent characters e.g.:
		//  "??..,,;;::""!!(())<<>>[[]]{{}}||““””‘‘’’"
		protected void InitializeDelimitersLegacy(string strPunctuation, out char[] aPunctForward, out char[] aPunctReverse)
		{
			// initialize the output char arrays
			int nLen = strPunctuation.Length / 2;
			aPunctForward = new char[nLen + 1];
			aPunctReverse = new char[nLen + 1];

			// put the delimiter chars into a char array (for use in a later 'Split' of the input string)
			int i = 0, j = 0;
			while (i < nLen)
			{
				aPunctForward[i] = strPunctuation[j++];
				aPunctReverse[i++] = strPunctuation[j++];
			}

			aPunctForward[nLen] = aPunctReverse[nLen] = ' ';
		}

		private void GetAmbiguities(string str, out List<string> astrAmbiguities)
		{
			astrAmbiguities = new List<string>();
			int nIndex;
			if ((nIndex = str.IndexOf('%')) == 0)
			{
				nIndex = str.IndexOf('%', nIndex + 1);
				if (nIndex != -1)
				{
					str = str.Substring(nIndex + 1);
					string[] astrWords = str.Split(new[] { '%' }, StringSplitOptions.RemoveEmptyEntries);
					astrAmbiguities.AddRange(astrWords);
				}
				else
					astrAmbiguities.Add(str);
			}
			else
				astrAmbiguities.Add(str);
		}

		private ViewSourceFormsForm _dlgSourceFormsForm;
		/// <summary>
		/// Edit the knowledgebase and optionally return the selected source word
		/// </summary>
		/// <returns>the source word selected in the edit dialog</returns>
		public string EditKnowledgeBase(string strFilter)
		{
			strFilter = strFilter.Trim(m_caDelimitersForward ?? CaSplitChars);

			if (Load(true) || (_dlgSourceFormsForm == null))
				_dlgSourceFormsForm = new ViewSourceFormsForm(MapOfMaps, m_strSourceWordFontName,
															  m_strTargetWordFontName,
															  m_caDelimitersForward ?? CaSplitChars,
															  m_caDelimitersReverse ?? CaSplitChars)
										  {
											  Parent = this
										  };

			_dlgSourceFormsForm.SelectedWord = strFilter;

			if (_dlgSourceFormsForm.ShowDialog() == DialogResult.OK)
				return _dlgSourceFormsForm.SelectedWord;

			return null;
		}

		internal void SaveMapOfMaps(MapOfMaps mapOfMaps)
		{
			mapOfMaps.SaveFile(m_strKnowledgeBaseFileSpec);

			// let's at least avoid reloading it if/since we're writing it,
			//  so capture it's new timestamp as though we had read it in
			if (!DoesFileExist(m_strKnowledgeBaseFileSpec, ref m_timeModifiedKbMapOfMaps))
				EncConverters.ThrowError(ErrStatus.CantOpenReadMap, m_strKnowledgeBaseFileSpec);
		}

		/// <summary>
		/// This version of AddEntryPair allows an option parameter to indicate whether
		/// you want the KB saved after the pair is added (primarily in case you want
		/// to *not* save it through an iteration cycle). If false, then you should
		/// then call AddEntryPairSave() when finished to save it.
		/// Also, this flavor will save the KB sorted
		/// </summary>
		/// <param name="strSourceWord"></param>
		/// <param name="strTargetWord"></param>
		/// <param name="bSave"></param>
		public void AddEntryPair(string strSourceWord, string strTargetWord, bool bSave)
		{
			// trim up the input words in case the user didn't
			strSourceWord = strSourceWord.Trim(m_caDelimitersForward ?? CaSplitChars);
			if (String.IsNullOrEmpty(strSourceWord))
				throw new ApplicationException(Properties.Resources.IDS_CantHaveEmptySourceWord);

			strTargetWord = strTargetWord.Trim(m_caDelimitersReverse ?? CaSplitChars);
			if (String.IsNullOrEmpty(strTargetWord))
				throw new ApplicationException(Properties.Resources.IDS_CantHaveEmptyTargetWord);

			Load(true); // load MapOfMaps if needed

			MapOfMaps.AddCouplet(strSourceWord, strTargetWord);

			if (bSave)
				SaveMapOfMaps(MapOfMaps);
		}

		public void AddEntryPairSave()
		{
			if (MapOfMaps != null)
				SaveMapOfMaps(MapOfMaps);
		}

		public bool EditTargetWords(string strSourceWord)
		{
			if (String.IsNullOrEmpty(strSourceWord))
				throw new ApplicationException(Properties.Resources.IDS_CantHaveEmptySourceWord);

			// trim up the input words in case the user didn't
			strSourceWord = strSourceWord.Trim(m_caDelimitersForward ?? CaSplitChars);
			if (String.IsNullOrEmpty(strSourceWord))
				throw new ApplicationException(Properties.Resources.IDS_CantHaveEmptySourceWord);

#if !NotUsingNewClass
			Load(true); // load MapOfMaps if needed
			MapOfSourceWordElements mapOfSourceWordElements;
			if (MapOfMaps.TryGetValue(strSourceWord, out mapOfSourceWordElements))
			{
				var dlg = new ModifyTargetWordsForm(strSourceWord, mapOfSourceWordElements,
					m_strTargetWordFontName, m_caDelimitersReverse ?? CaSplitChars);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					SaveMapOfMaps(MapOfMaps);
					return true;
				}
			}
#else
			// first see if this pair is already there
			int nMapValue;
			List<string> astrTargetWords = GetTargetWords(strSourceWord, out nMapValue);

			var dlg = new ModifyTargetWordsForm(strSourceWord, astrTargetWords, m_strTargetWordFontName);
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				try
				{
					XmlDocument doc;
					XPathNavigator navigator;
					XmlNamespaceManager manager;
					GetXmlDocument(out doc, out navigator, out manager);

					if (doc.DocumentElement != null)
					{
						XmlNode nodeKbNode = doc.DocumentElement.SelectSingleNode(XPathToKB, manager);
						if (nodeKbNode == null)
						{
							doc.CreateElement(XPathToKB);   // no KB element, so create one
							nodeKbNode = doc.DocumentElement.SelectSingleNode(XPathToKB, manager);
						}

						// see if the proper map entry is present (so we can add it, if not)
						string strMapSelect = XPathToSpecificMAP(nMapValue);
						XmlNode nodeMapEntry = nodeKbNode.SelectSingleNode(strMapSelect, manager);
						if (nodeMapEntry == null)
						{
							// if not, then add it.
							// xpathnavs are easier to use to add child elements
							XPathNavigator xpnMap = nodeKbNode.CreateNavigator();
							xpnMap.AppendChild(String.Format("<MAP mn=\"{0}\"/>", nMapValue));

							// now try it again
							nodeMapEntry = nodeKbNode.SelectSingleNode(strMapSelect, manager);
						}

						// see if the source word exists (so we can delete what's there now)
						string strSourceWordSelect = XPathToSpecificTU(strSourceWord);
						XmlNode nodeSourceWordEntry = nodeMapEntry.SelectSingleNode(strSourceWordSelect, manager);

						// if it exists, then empty it
						if (nodeSourceWordEntry != null)
						{
							// get rid of the existing children
							while (nodeSourceWordEntry.ChildNodes.Count > 0)
								nodeSourceWordEntry.RemoveChild(nodeSourceWordEntry.ChildNodes.Item(0));
						}
						else
						{
							// otherwise, add it
							XPathNavigator xpnSourceWord = nodeMapEntry.CreateNavigator();
							xpnSourceWord.AppendChild(String.Format("<TU f=\"0\" k=\"{0}\"/>", strSourceWord));

							// now try it again
							nodeSourceWordEntry = nodeMapEntry.SelectSingleNode(strSourceWordSelect, manager);
						}

						XPathNavigator xpnTargetWord = nodeSourceWordEntry.CreateNavigator();
						List<string> lstNewTargetForms = dlg.NewTargetForms;
						if (lstNewTargetForms.Count == 0)
							xpnTargetWord.DeleteSelf();
						else
						{
							foreach (string strNewTargetForm in dlg.NewTargetForms)
								xpnTargetWord.AppendChild(String.Format("<RS n=\"1\" a=\"{0}\"/>", strNewTargetForm));
						}

						File.Copy(m_strKnowledgeBaseFileSpec, m_strKnowledgeBaseFileSpec + ".bak", true);

						XmlTextWriter writer = new XmlTextWriter(m_strKnowledgeBaseFileSpec, Encoding.UTF8);
						writer.Formatting = Formatting.Indented;
						doc.Save(writer);
						writer.Close();
					}
				}
				catch (System.Data.DataException ex)
				{
					if (ex.Message == "A child row has multiple parents.")
					{
						// this happens when the knowledge base has invalid data in it (e.g. when there is two
						//  canonically equivalent words in different records). This is technically a bug in
						//  AdaptIt.
						throw new ApplicationException("The AdaptIt knowledge base has invalid data in it! Contact silconverters_support@sil.org", ex);
					}

					throw ex;
				}


				return true;
			}

#endif
			return false;
		}

		internal List<string> GetTargetWords(string strSourceWord, out int nMapValue)
		{
			string strTargetWordsInMap;
			Dictionary<string, string> mapLookup;
			List<string> astrTargetWords;
			if (!TryGetTargetForm(strSourceWord, out strTargetWordsInMap, out nMapValue, out mapLookup))
			{
				// if there is no source word, then we'll just offer that as the new target
				astrTargetWords = new List<string> { strSourceWord };
			}
			else
			{
				GetAmbiguities(strTargetWordsInMap, out astrTargetWords);
			}
			return astrTargetWords;
		}

		protected bool TryGetTargetForm(string strSourceWord, out string strTargetWordsInMap,
			out int nMapValue, out Dictionary<string, string> mapLookup)
		{
			// first get the map for the number of words in the source string (e.g. "ke picche" would be map=2)
			nMapValue = strSourceWord.Split(CaSplitChars, StringSplitOptions.RemoveEmptyEntries).Length;
			if (nMapValue > 10)
				throw new ApplicationException("Cannot have a source phrase with more than 10 words!");

			// just in case it hasn't been done yet, do the Load now.
			Load(false);

			if (!m_mapOfMaps.TryGetValue(nMapValue, out mapLookup))
				mapLookup = new Dictionary<string, string>();

			// first see if this pair is already there
			return mapLookup.TryGetValue(strSourceWord, out strTargetWordsInMap);
		}

		#endregion Misc helpers

		#region Abstract Base Class Overrides
		protected override void PreConvert
			(
			EncodingForm        eInEncodingForm,
			ref EncodingForm    eInFormEngine,
			EncodingForm        eOutEncodingForm,
			ref EncodingForm    eOutFormEngine,
			ref NormalizeFlags  eNormalizeOutput,
			bool                bForward
			)
		{
			// let the base class do it's thing first
			base.PreConvert( eInEncodingForm, ref eInFormEngine,
				eOutEncodingForm, ref eOutFormEngine,
				ref eNormalizeOutput, bForward);

			// this converter only deals with 'String' flavors, so if it's
			//  Unicode_to(_from)_Unicode, then we expect UTF-16 and if it's
			//  Legacy_to(_from)_Legacy, then we expect LegacyString
			if( m_bLegacy )
				eInFormEngine = eOutFormEngine = EncodingForm.LegacyString;
			else
				eInFormEngine = eOutFormEngine = EncodingForm.UTF16;

			// the bForward that comes here might be different from the IEncConverter->DirectionForward
			//  (if it came in from a call to ConvertEx), so use *this* value to determine the direction
			//  for the forthcoming conversion (DoConvert).
			m_bReverseLookup = !bForward;

			// check to see if the file(s) need to be (re-)loaded at this point.
			Load(false);
		}

		protected override string GetConfigTypeName
		{
			get { return strRegValueForConfigProgId; }
		}
		#endregion Abstract Base Class Overrides

		public static string GetAttributeValue(XElement elem, string strAttributeLabel, string strDefaultValue)
		{
			var attr = elem.Attribute(strAttributeLabel);
			return (attr != null) ? attr.Value : strDefaultValue;
		}

		public static int GetAttributeValue(XElement elem, string strAttributeLabel, int nDefaultValue)
		{
			var attr = elem.Attribute(strAttributeLabel);
			if (attr != null)
			{
				int nValue = System.Convert.ToInt32(attr.Value);
				return nValue;
			}
			return nDefaultValue;
		}
	}
}
