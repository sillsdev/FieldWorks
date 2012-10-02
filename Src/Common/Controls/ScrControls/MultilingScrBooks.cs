// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MultilingScrBooks.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Scripture;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for MultilingScrBooks.
	/// </summary>
	public class MultilingScrBooks : IMultilingScrBooks
	{
		#region Internal Structs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// WsNames structure holds all name info for a given writing system.
		/// Note - Book numbers are one-based (i.e. Genesis = 0)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class WsNames
		{
			/// <summary></summary>
			public int Ws;
			/// <summary></summary>
			public string[] Name;
			/// <summary></summary>
			public string[] Abbrev;

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="nEnc"></param>
			/// --------------------------------------------------------------------------------
			public WsNames(int nEnc)
			{
				Ws = nEnc;
				Name = new string[ScrReference.LastBook];
				Abbrev = new string[ScrReference.LastBook];
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="nBookNum"></param>
			/// <param name="sName"></param>
			/// <param name="sAbbrev"></param>
			/// --------------------------------------------------------------------------------
			public void Add(int nBookNum, string sName, string sAbbrev)
			{
				Name[nBookNum] = sName;
				Abbrev[nBookNum] = sAbbrev;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Search for an abbreviation or name that matches a given string.
			/// </summary>
			/// <param name="searchString">The search string.</param>
			/// <returns>Index of first matching book abbreviation or name, or -1 if no match.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public int Search(string searchString)
			{
				return Search(searchString, 0);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Search for an abbreviation or name that matches a given string.
			/// </summary>
			/// <param name="searchString">The search string.</param>
			/// <param name="startIndex">The starting index of the book to search.</param>
			/// <returns>Index of first matching book abbreviation or name, or -1 if no match.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public int Search(string searchString, int startIndex)
			{
				// ENHANCE: Make more effecient; abbrevs first, hash map, NT first, etc?)
				int iBestMatch = -1;
				for (int iBook = startIndex; iBook < ScrReference.LastBook; iBook++)
				{
					if (Abbrev[iBook].StartsWith(searchString, StringComparison.CurrentCultureIgnoreCase) ||
						Name[iBook].StartsWith(searchString, StringComparison.CurrentCultureIgnoreCase))
					{
						if (Abbrev[iBook].Equals(searchString, StringComparison.CurrentCultureIgnoreCase) ||
							Name[iBook].Equals(searchString, StringComparison.CurrentCultureIgnoreCase))
						{
							return (iBook + 1); // Return one-based book number.
						}
						else if (iBestMatch == -1)
							iBestMatch = iBook + 1;
					}
				}

				return iBestMatch; // Did not find an exact match; return the first partial match, if any.
			}
		}
		#endregion

		#region Data members
		/// <summary></summary>
		protected bool m_fProcessDeuteroCanonical = false; // Don't process deutero-canonical book names.
		/// <summary></summary>
		protected List<int> m_requestedEncodings; // Array containing the primary & secondary encodings.
		/// <summary></summary>
		protected short m_nReferenceFormat; // The specified format for the Scripture reference strings.
		/// <summary></summary>
		protected List<WsNames> m_NameSets; // One name struct per available writing system.
		/// <summary></summary>
		protected const int knWsSilCodes = 1; // Writing system number for SIL codes
		/// <summary>writing system id for English</summary>
		protected int m_wsEnglish;
		/// <summary>writing system id for Spanish</summary>
		protected int m_wsSpanish;
		/// <summary>currrent versification scheme</summary>
		protected IScrProjMetaDataProvider m_versificationProvider;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MultilingScrBooks"/> class.
		/// </summary>
		/// <param name="versificationProvider">The Scripture project meta-data provider used
		/// to get the current versification system.</param>
		/// ------------------------------------------------------------------------------------
		public MultilingScrBooks(IScrProjMetaDataProvider versificationProvider)
		{
			m_versificationProvider = versificationProvider;
			InitNameSets();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		protected void InitNameSets()
		{
			// If values exist already, depend on collecting garbage.
			m_NameSets = new List<WsNames>();
			m_NameSets.Add(new WsNames(knWsSilCodes)); // element zero is SIL codes
			m_NameSets.Add(new WsNames(m_wsEnglish)); // element one is English
			m_NameSets.Add(new WsNames(m_wsSpanish)); // element two is Spanish

			// Initialize the list of requested encodings with the built-in languages we provide
			List<int> encs = new List<int>(8); // capacity of 8
			encs.AddRange(new int[] { m_wsEnglish, m_wsSpanish });
			RequestedEncodings = encs; // Calls RequestedEncodings:Set

			//SIL codes, stored in element zero
			BuildSILNameSet(0);

			// English, stored in element one
			BuildEnglishNameSet(1);

			// Spanish, stored in element two
			BuildSpanishNameSet(2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the name set for Spanish scripture book names and abbreviations.
		/// </summary>
		/// <param name="index">Index of the name set to fill.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void BuildEnglishNameSet(int index)
		{
			m_NameSets[index].Name = new string[] {"Genesis","Exodus","Leviticus","Numbers","Deuteronomy",
				"Joshua","Judges","Ruth","1 Samuel","2 Samuel","1 Kings","2 Kings","1 Chronicles",
				"2 Chronicles", "Ezra","Nehemiah","Esther","Job","Psalms","Proverbs","Ecclesiastes",
				"Song of Solomon","Isaiah", "Jeremiah","Lamentations","Ezekiel","Daniel","Hosea","Joel",
				"Amos","Obadiah","Jonah","Micah","Nahum","Habakkuk","Zephaniah","Haggai","Zechariah",
				"Malachi",
				"Matthew","Mark","Luke","John","Acts","Romans","1 Corinthians","2 Corinthians",
				"Galatians","Ephesians","Philippians","Colossians","1 Thessalonians","2 Thessalonians",
				"1 Timothy","2 Timothy","Titus","Philemon","Hebrews","James",
				"1 Peter","2 Peter","1 John","2 John","3 John","Jude","Revelation"};

			m_NameSets[index].Abbrev = new string[] {"Gen","Exo","Lev","Num","Deu",
				"Jos","Jdg","Rut","1Sa","2Sa","1Ki","2Ki","1Ch",
				"2Ch","Ezr","Neh","Est","Job","Psa","Pro","Ecc",
				"Sng","Isa","Jer","Lam","Ezk","Dan","Hos","Jol",
				"Amo","Oba","Jon","Mic","Nah","Hab","Zep","Hag","Zec",
				"Mal",
				"Mat","Mrk","Luk","Jhn","Act","Rom","1Cor","2Cor",
				"Gal","Eph","Php","Col","1Th","2Th",
				"1Tim","2Tim","Tit","Phm","Heb","Jas",
				"1Pet","2Pet","1Jn","2Jn","3Jn","Jud","Rev"};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the name set for Spanish scripture book names and abbreviations.
		/// </summary>
		/// <param name="index">Index of the name set to fill.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void BuildSpanishNameSet(int index)
		{
			m_NameSets[index].Name = new string[] {"Génesis","Exodo","Levítico","Números","Deuteronomio",
				"Josué","Jueces","Rut","1 Samuel","2 Samuel","1 Reyes","2 Reyes","1 Crónicas",
				"2 Crónicas", "Esdras","Nehemías","Ester","Job","Salmos","Proverbios","Eclesiastés",
				"Cantares","Isaías", "Jeremías","Lamentaciones","Ezequiel","Daniel","Oseas","Joel",
				"Amós","Abdías","Jonás", "Miqueas","Nahúm","Habacuc","Sofonías","Hageo","Zacarías",
				"Malaquías",
				"Mateo","Marcos","Lucas","Juan","Hechos","Romanos","1 Corintios","2 Corintios",
				"Gálatas","Efesios","Filipenses","Colosenses","1 Tesalonicenses","2 Tesalonicenses",
				"1 Timoteo","2 Timoteo","Tito","Filemón","Hebreos","Santiago",
				"1 Pedro","2 Pedro","1 Juan","2 Juan","3 Juan","Judas","Apocalipsis"};

			m_NameSets[index].Abbrev = new string[] {"Gén","Ex","Lev","Núm","Dt",
				"Jos","Jue","Rt","1 Sam","2 Sam","1 Re","2 Re","1 Cro",
				"2 Cro","Esd","Neh","Est","Job","Sal","Prov","Ecl",
				"Cant","Is","Jer","Lam","Ez","Dan","Os","Jl",
				"Am","Abd","Jon","Miq","Nah","Hab","Sof","Hag","Zac",
				"Mal",
				"Mt","Mc","Lc","Jn","Hch","Rom","1 Cor","2 Cor",
				"Gál","Ef","Flp","Col","1 Tes","2 Tes",
				"1 Tim","2 Tim","Tit","Flm","Heb","Sant",
				"1 Pe","2 Pe","1 Jn","2 Jn","3 Jn","Jds","Ap"};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the name set for SIL scripture book names and abbreviations.
		/// </summary>
		/// <param name="index">Index of the name set to fill.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void BuildSILNameSet(int index)
		{
			m_NameSets[index].Name = SilBooks.Codes_3Letter;
			m_NameSets[index].Abbrev = SilBooks.Codes_2Letter;
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// RequestedEncodings property
		/// The first writing system in the array is called the primary
		/// writing system by the spec. The array may also optionally include a list of secondary
		/// encodings, which are used by MultilingScrBooks if the primary writing system does not
		/// suffice for some reason. The caller must ensure that the UI writing system is included
		/// in the array.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("Array of encodings requested by the caller.")]
		public List<int> RequestedEncodings
		{
			get
			{
				return m_requestedEncodings;
			}
			set
			{
				m_requestedEncodings = value;
				//m_wsPrimary = (int)m_requestedEncodings[0]; //set the primary writing system

				// Ensure that SIL codes is in the list of requested encodings
				if (!m_requestedEncodings.Contains(knWsSilCodes))
					m_requestedEncodings.Add(knWsSilCodes); // Add to the end
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// ProcessDeuteroCanonical property
		/// True if Deutero-canonical refs are to be processed. Note: an implementation may
		/// not support Deutero-canonical refs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("Always false in this implementation.")]
		public bool ProcessDeuteroCanonical
		{
			get {return m_fProcessDeuteroCanonical;}
			set {m_fProcessDeuteroCanonical = false;}	// nScrBooksLim = 66;
			// This impletation does not support deutero-canonical book names.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// ScriptureBooksLim property
		/// The maximum number of Scripture books that may be returned to the caller.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("Maximum number of Scripture books.")]
		public int ScriptureBooksLim
		{
			get {return ScrReference.LastBook;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// ReferenceFormat property
		/// The specified format for the Scripture reference strings.
		/// e.g. ReferenceFormat of 0 specifies Western format -> "Eph 3:2".
		/// Note: an implementation may not support multiple formats.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("Always zero in this implementation.")]
		public short ReferenceFormat
		{
			get {return m_nReferenceFormat;}
			set {m_nReferenceFormat = 0;}		// This implementation supports only a western format.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Writing system id for English.</summary>
		/// ------------------------------------------------------------------------------------
		[Description("Writing system id for English")]
		public int EnglishWs
		{
			get {return m_wsEnglish;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Writing system id for Spanish.</summary>
		/// ------------------------------------------------------------------------------------
		[Description("Writing system id for Spanish")]
		public int SpanishWs
		{
			get {return m_wsSpanish;}
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the id of the primary writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int PrimaryEncoding
		{
			get { return (int)m_requestedEncodings[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="nEnc"></param>
		/// <returns>The WsNames object for the given ws, if it exists</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual WsNames GetWsNames(int nEnc)
		{
			for(int i = 0; i < m_NameSets.Count; i++)
				if (m_NameSets[i].Ws == nEnc)
					return m_NameSets[i];

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns false, if hooked to a scr database and the book text does not exist.
		/// </summary>
		/// <param name="nBook">Book number used to see if book text exists in database.
		/// NOTE: book number is one based (i.e. Genesis = 1).</param>
		/// <returns>In implementation (a): always return true.
		/// In implementation (b) (hooked to a scr database): return true if book text exists.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsBookAvailableInDb(int nBook)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified book index is within the range of valid
		/// books.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>True if the book is valid. Otherwise false.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsBookValid(int nBook)
		{
			WsNames wsNames;
			wsNames = GetWsNames(PrimaryEncoding);
			return (nBook > 0 && nBook <= wsNames.Name.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the reference string is a valid canonical scripture
		/// reference.
		/// </summary>
		/// <param name="reference">Scripture reference</param>
		/// <returns>True if the reference is valid. Otherwise false... imagine that.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsReferenceValid(string reference)
		{
			try
			{
				return (ParseRefString(reference).Valid);
			}
			catch (Exception e)
			{
				// TODO: This error code needs to be checked
//				if ((e.ErrorCode & 0xf408) == 0xf408)	// versification HRESULT value
					throw new InstallationException(e);
//				else
//					return false;	// some other problem with string ...
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the book name for a given book number.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>The book name as a string.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetBookName(int nBook)
		{
			WsNames wsNames;
			wsNames = GetWsNames(PrimaryEncoding);
			Debug.Assert(nBook > 0 && nBook <= wsNames.Name.Length);
			return wsNames.Name[nBook - 1];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the SIL Code for a given book number.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>The SIL Code as a string.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetSILCode(int nBook)
		{
			WsNames wsNames;
			wsNames = GetWsNames(knWsSilCodes);
			Debug.Assert(nBook > 0 && nBook <= wsNames.Name.Length); // Needed?
			Debug.Assert(wsNames.Ws == knWsSilCodes);
			// The 3 letter SIL code is stored in the Name array
			string sCode = wsNames.Name[nBook - 1];
			Debug.Assert(sCode != null);
			return sCode;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the book abbreviation for a given book number.
		/// (get Primary writing system abbrev; if not available, get the SIL code)
		/// or should we also try secondary encodings??
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>The book abbreviation as a string.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetBookAbbrev(int nBook)
		{
			// TODO: ToddJ (BryanW)		If JohnW confirms we want the secondary Ws rather
			// than SIL code...
			// foreach (int nEnc in m_requestedEncodings)
			// Assert that knWsSilCodes is in the list
			WsNames wsNames;
			string sAbbrev;

			// Get the abbreviation for the primary writing system
			if (PrimaryEncoding != 0)
			{
				wsNames = GetWsNames(PrimaryEncoding);
				if (wsNames != null)
				{
					Debug.Assert(wsNames.Ws == PrimaryEncoding);
					sAbbrev = wsNames.Abbrev[nBook - 1];
					if (sAbbrev != null)
						return sAbbrev;
				}
			}

			// No abbreviation found in primary writing system; get SIL code instead
			wsNames = GetWsNames(knWsSilCodes); // SIL codes
			Debug.Assert (wsNames != null);
			sAbbrev = wsNames.Name[nBook - 1]; // full SIL code is the .Name
			Debug.Assert(sAbbrev != null);
			return sAbbrev;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If name of one book is not available in the first (primary) writing system,
		/// the info in the next available (secondary) writing system is substituted.
		/// If not available in any given writing system, the 3-letter SIL/UBS book code is
		/// returned.
		/// </summary>
		/// <param name="fAvailable">Normally, the output array only includes available books
		/// if fAvailable is true. Ignored in this implementation, since all names are
		/// returned.</param>
		/// <returns>An array of BookNameInfo objects in the requested primary writing system,
		/// as far as possible.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual BookLabel[] GetBookNames(bool fAvailable)
		{
			// TODO: ToddJ (BryanW)	Implement the description above
			// foreach (int nEnc in m_requestedEncodings)
			// Assert that knWsSilCodes is in the list

			BookLabel[] rgblBookNames = new BookLabel[ScriptureBooksLim];
			WsNames wsNames;

			if (PrimaryEncoding != 0)
			{
				wsNames = GetWsNames(PrimaryEncoding);
				if (wsNames != null)
				{
					for(int i = 0; i < ScriptureBooksLim; i++)
						rgblBookNames[i] = new BookLabel(wsNames.Name[i], (i + 1));

					return rgblBookNames;
				}
			}

			Debug.Assert(false);
			return rgblBookNames;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the user typed in string.  Creates and returns a ScrReference object.
		/// </summary>
		/// <param name="sTextToBeParsed">Reference string the user types in.</param>
		/// <returns>The generated scReference object.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual ScrReference ParseRefString(string sTextToBeParsed)
		{
			return ParseRefString(sTextToBeParsed, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the user typed in string. Creates and returns a ScrReference object.
		/// </summary>
		/// <param name="sTextToBeParsed">Reference string the user types in.</param>
		/// <param name="startingBook">The 0-based index of starting book to consider when
		/// parsing reference.</param>
		/// <returns>The generated scReference object.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual ScrReference ParseRefString(string sTextToBeParsed, int startingBook)
		{
			Regex regex = new Regex(@"(?<book>\w?.*[a-zA-Z])\s?((?<chapter>\d+)(\D+(?<verse>\d+))?)?");
			Match match = regex.Match(sTextToBeParsed.TrimStart());
			if (match.Success)
			{
				// Determine book number
				int nBookNumber = 0;
				string bookToken = match.Groups["book"].Value;
				foreach (int nEnc in m_requestedEncodings)
				{
					WsNames wsNames = GetWsNames(nEnc);
					if (wsNames != null)
					{
						nBookNumber = wsNames.Search(bookToken, startingBook);
						if (nBookNumber > 0)
							break;
					}
				}

				// Break out the chapter and verse numbers
				int chapter;
				if (!int.TryParse(match.Groups["chapter"].Value, out chapter))
					chapter = 1;	// it's legal to specify 0 as chapter number (i.e. intro material)

				int verse;
				int.TryParse(match.Groups["verse"].Value, out verse);

				// If there was no verse specified, then make it 1
				if (verse == 0)
					verse = 1;

				// If the book number is invalid...
				if (nBookNumber < 1 || nBookNumber > 66)
				{
					// just set the reference to GEN 1:1
					nBookNumber = 1;
					chapter = 1;
					verse = 1;
				}
				return new ScrReference(nBookNumber, chapter, verse,
					m_versificationProvider.Versification);
			}
			return new ScrReference();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a cannonical text form of the given reference, consisting of
		/// book abbreviation (in primary writing system), chapter nbr, colon, verse nbr.
		/// </summary>
		/// <param name="scRef">The given scReference object.</param>
		/// <returns>The generated text string reference.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetRefString(ScrReference scRef)
		{
			return scRef.AsString;
			// GetAbbrev, replace 1st 3 with new abbrev
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the object with the proper writing system factory, which also initializes
		/// some standard writing system ids.
		/// </summary>
		/// <param name="wsf">the writing system factory</param>
		/// <returns>nothing</returns>
		/// ------------------------------------------------------------------------------------
		public virtual void InitializeWritingSystems(ILgWritingSystemFactory wsf)
		{
			m_wsEnglish = wsf.GetWsFromStr("en");
			m_wsSpanish = wsf.GetWsFromStr("es");
			// Rebuild the namesets with the new writing system ids.
			InitNameSets();
		}

		#endregion
	}
	//TODO BryanW(ToddJ): Do whitebox texting
}
