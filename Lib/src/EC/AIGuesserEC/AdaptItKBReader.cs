using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;       // for Dictionary<>
using Microsoft.Win32;                  // for RegistryKey
using System.Diagnostics;               // for Debug
using System.IO;                        // for file I/O
using System.Xml;                       // for XMLDocument
using System.Xml.XPath;                 // for XPathNavigator
using SilEncConverters31;               // for EncConverter (base-class) definition
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	/// <summary>
	/// Base class for an AdaptIt Knowledge Base EncConverter
	/// </summary>
	public abstract class AdaptItKBReader : EncConverter
	{
		#region Member Variable Definitions

		public abstract string strRegValueForConfigProgId { get; }

		protected DateTime    m_timeModifiedKB = DateTime.MinValue;
		protected DateTime    m_timeModifiedProj = DateTime.MinValue;
		protected Dictionary<int, Dictionary<string, string>> m_mapOfMaps = new Dictionary<int, Dictionary<string, string>>();
		protected Dictionary<int, Dictionary<string, string>> m_mapOfReversalMaps = null;
		protected char[] m_caDelimitersForward;
		protected char[] m_caDelimitersReverse = null;
		protected string      m_strKnowledgeBaseFileSpec = null;
		protected string      m_strProjectFileSpec = null;
		protected bool        m_bLegacy = false;
		protected bool        m_bReverseLookup = false;   // we have to keep track of the direction since it might be different than m_bForward
		protected bool m_bHasNamespace = true;

		public string cstrAdaptItXmlNamespace = "http://www.sil.org/computing/schemas/AdaptIt KB.xsd";
		public string cstrAdaptItProjectFilename = "AI-ProjectConfiguration.aic";
		public string cstrAdaptItPunctuationPairsNRSource = "PunctuationPairsSourceSet(stores space for an empty cell)	";
		public string cstrAdaptItPunctuationPairsNRTarget = "PunctuationPairsTargetSet(stores space for an empty cell)	";
		public string cstrAdaptItPunctuationPairsLegacy = "PunctuationPairs(stores space for an empty cell)	";
		public char[] caSplitChars = new char[] { '\r', '\n', '\t', ' ' };

		public char[] DelimitersForward
		{
			get { return m_caDelimitersForward; }
			set { m_caDelimitersForward = value; }
		}

		public char[] DelimitersReverse
		{
			get { return m_caDelimitersReverse; }
			set { m_caDelimitersReverse = value; }
		}

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
			m_caDelimitersForward = null;
		}

		public override void Initialize(string converterName, string converterSpec,
			ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType,
			ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
			base.Initialize(converterName, converterSpec, ref lhsEncodingID, ref rhsEncodingID, ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding );

			m_bLegacy = (EncConverter.NormalizeLhsConversionType(conversionType) == NormConversionType.eLegacy);

			// get the filespec to the project file and the knowledge base
			//  (the KB path *is* the converter spec)
			m_strKnowledgeBaseFileSpec = converterSpec;
			string strProjectFolder = Path.GetDirectoryName(converterSpec);
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

		protected virtual bool Load()
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
					int nLength = strProjectFileContents.IndexOfAny(caSplitChars, nIndex) - nIndex;
					string strPunctuation = strProjectFileContents.Substring(nIndex, nLength);
					InitializeDelimitersLegacy(strPunctuation, out m_caDelimitersForward, out m_caDelimitersReverse);
				}
				else    // NonRoman version
				{
					int nIndex = strProjectFileContents.IndexOf(cstrAdaptItPunctuationPairsNRSource) + cstrAdaptItPunctuationPairsNRSource.Length;
					int nLength = strProjectFileContents.IndexOf('\n',nIndex) - nIndex;
					this.m_caDelimitersForward = ReturnDelimiters(strProjectFileContents, nIndex, nLength);
					nIndex = strProjectFileContents.IndexOf(cstrAdaptItPunctuationPairsNRTarget, nIndex) + cstrAdaptItPunctuationPairsNRTarget.Length;
					nLength = strProjectFileContents.IndexOf('\n',nIndex) - nIndex;
					this.m_caDelimitersReverse = ReturnDelimiters(strProjectFileContents, nIndex, nLength);
				}

				m_timeModifiedProj = timeModified;
				bSomethingChanged = true;
			}

			// next check on the knowledge base... make sure it's there and get the last time it was modified
			timeModified = DateTime.Now; // don't care really, but have to initialize it.
			if( !DoesFileExist(m_strKnowledgeBaseFileSpec, ref timeModified) )
				EncConverters.ThrowError(ErrStatus.CantOpenReadMap, m_strKnowledgeBaseFileSpec);

			// if it has been modified or it's not already loaded...
			if( timeModified > m_timeModifiedKB )
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
					throw new ApplicationException("Unable to open the AdaptIt knowledge base. Contact silconverters_support@sil.org", ex);
				}
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

				// keep track of the modified date, so we can detect a new version to reload
				m_timeModifiedKB = timeModified;
				bSomethingChanged = true;
			}

			return bSomethingChanged;   // indicate whether the data was reinitialized or not
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

		public void AddEntryPair(string strSourceWord, string strTargetWord)
		{
			// first get the map for the number of words in the source string (e.g. "ke picche" would be map=2)
			strSourceWord = strSourceWord.Trim((m_caDelimitersForward != null) ? m_caDelimitersForward : caSplitChars);
			int nMapValue = strSourceWord.Split(caSplitChars, StringSplitOptions.RemoveEmptyEntries).Length;
			if (nMapValue > 10)
				throw new ApplicationException("Cannot have a source phrase with more than 10 words!");

			Dictionary<string, string> mapLookup;
			if (!m_mapOfMaps.TryGetValue(nMapValue, out mapLookup))
				mapLookup = new Dictionary<string, string>();

			// first see if this pair is already there
			string strTargetWordsInMap = null;
			if (mapLookup.TryGetValue(strSourceWord, out strTargetWordsInMap))
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

		public void ValidateKnowledgeBase(string strKBFilename)
		{
			throw new NotImplementedException();
#if false
			// see if we have ICU installed and can acquire the NFD converter
			XmlDocument doc = new XmlDocument();
			try
			{
				doc.Load(strKBFilename);
				XPathNavigator navigator = doc.CreateNavigator();
				XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
				manager.AddNamespace("aikb", cstrAdaptItXmlNamespace);

				XPathNodeIterator xpMapIterator = navigator.Select("/aikb:AdaptItKnowledgeBase/aikb:KB/aikb:MAP", manager);

				while (xpMapIterator.MoveNext())
				{
					XPathNodeIterator xpSourceWords = xpMapIterator.Current.Select("aikb:TU", manager);
					while (xpSourceWords.MoveNext())
					{
						string strSourceWord = xpSourceWords.Current.GetAttribute("k", navigator.NamespaceURI);
						string strSourceWordNFD = m_aEC.Convert(strSourceWord);

						if (m_lstSourceWords.Contains(strSourceWordNFD))
						{
							MessageBox.Show(String.Format("This knowledge base has the source word '{0}' occurring twice!?",
								strSourceWordNFD), cstrCaption);
						}
						else
							m_lstSourceWords.Add(strSourceWordNFD);

						XPathNodeIterator xpTargetWords = xpSourceWords.Current.Select("aikb:RS", manager);
						while (xpTargetWords.MoveNext())
						{
							string strTargetWord = xpTargetWords.Current.GetAttribute("a", navigator.NamespaceURI);

							dataGridView.Rows.Add(new object[] { strSourceWord, strTargetWord });

							strSourceWord = null;
						}
					}
				}
			}
			catch (Exception ex)
			{
				string strError = String.Format("Unable to read the XML file:{0}{0}'{1}'{0}{0}cause: {2}",
					Environment.NewLine, strKBFilename, ex.Message);

				if (ex.InnerException != null)
					strError += String.Format("{0}{0}cause: {1}", Environment.NewLine, ex.InnerException.Message);

				MessageBox.Show(strError, cstrCaption);
			}
#endif
		}

		// for a NonRoman AI Project, the punctuation is in adjacent rows e.g.:
		// PunctuationPairsSourceSet(stores space for an empty cell)	?.,;:"!()<>{}[]“”‘’
		// PunctuationPairsTargetSet(stores space for an empty cell)	?.,;:"!()<>{}[]“”‘’
		protected char [] ReturnDelimiters(string s, int nIndex, int nLength)
		{
			string strPunctuation = s.Substring(nIndex, nLength);

			// put the delimiter chars into a char array (for use in a later 'Split' of the input string)
			char [] aChars = new char [strPunctuation.Length];
			for(int i = 0; i < strPunctuation.Length; i++)
				aChars[i] = strPunctuation[i];
			return aChars;
		}

		// for a Legacy AI Project, the punctuation is in adjacent characters e.g.:
		//  "??..,,;;::""!!(())<<>>[[]]{{}}||““””‘‘’’"
		protected void InitializeDelimitersLegacy(string strPunctuation, out char [] aPunctForward, out char [] aPunctReverse)
		{
			// initialize the output char arrays
			int nLen = strPunctuation.Length / 2;
			aPunctForward = new char [nLen];
			aPunctReverse = new char [nLen];

			// put the delimiter chars into a char array (for use in a later 'Split' of the input string)
			int i = 0, j = 0;
			while (i < nLen)
			{
				aPunctForward[i] = strPunctuation[j++];
				aPunctReverse[i++] = strPunctuation[j++];
			}
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
			Load();
		}

		protected override string GetConfigTypeName
		{
			get { return strRegValueForConfigProgId; }
		}
		#endregion Abstract Base Class Overrides
	}
}
