// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Ethnologue.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace SIL.Ethnologue
{
	/// <summary>
	/// This class provides access to Ethnologue data.  In earlier versions of
	/// FieldWorks, this was provided by a special SQL Server database.  We now
	/// read the raw data directly from tab delimited files, and store it in
	/// memory.
	/// </summary>
	public class Ethnologue
	{
		/// <summary>
		/// This table defines the possible language status values, which are
		/// encoded as single letters.
		/// </summary>
		struct LanguageStatus
		{
			public char Id;
			public string Type;
			public LanguageStatus(char chId, string sType)
			{
				Id = chId;
				Type = sType;
			}
		};
		static List<LanguageStatus> s_tblLanguageStatus;

		/// <summary>
		/// This table defines the possible language types, which are encoded
		/// as one- or two-letter strings.
		/// </summary>
		struct LanguageType
		{
			public string Id;	// char[2]
			public string Type;
			public LanguageType(string sId, string sType)
			{
				Id = sId;
				Type = sType;
			}
		};
		static List<LanguageType> s_tblLanguageType;

		/// <summary>
		/// This table defines the mapping from two-letter ISO country code to
		/// the English name.  Unfortunately, it's not a 1:1 mapping!
		/// </summary>
		struct Country
		{
			public readonly string Id;
			public readonly string Name;
			public Country(string sId, string sName)
			{
				Id = sId;
				Name = sName;
			}
		}
		static List<Country> s_tblCountry;

		// The Id is just the index into this list (after it's sorted).
		static List<string> s_tblLanguageName;

		struct EthnologueTbl
		{
			public string Iso6391;	// char[2]
			public string Iso6393;	// char[3]
			public string Icu;		// char[4]
			public EthnologueTbl(string sIso6391, string sIso6393, string sIcu)
			{
				Iso6391 = sIso6391;
				Iso6393 = sIso6393;
				Icu = sIcu;
			}
		}
		static List<EthnologueTbl> s_tblEthnologue;
		static Dictionary<string, int> s_mapIso6393ToIdx;
		static Dictionary<string, int> s_mapIcuToIdx;

		struct LanguageLocation
		{
			public int EthnologueIdx;		// REFERENCES Ethnologue index
			public string CountryUsedInId;	// char[2] - REFERENCES Country(Id)
			public string LanguageTypeId;	// char[2] - REFERENCES LanguageType(Id)
			public int LanguageIdx;			// REFERENCES LanguageName(Id), ie, index
			public LanguageLocation(int nEthnologueIdx, string sCountryUsedInId, String sLanguageTypeId,
				int nLanguageIdx)
			{
				EthnologueIdx = nEthnologueIdx;
				CountryUsedInId = sCountryUsedInId;
				LanguageTypeId = sLanguageTypeId;
				LanguageIdx = nLanguageIdx;
			}
		}
		static List<LanguageLocation> s_tblLanguageLocation;

		struct EthnologueLocation
		{
			public int EthnologueIdx;			// REFERENCES Ethnologue index
			public string MainCountryUsedId;	// char[2] - REFERENCES Country(Id)
			public char LanguageStatusId;		// REFERENCES LanguageStatus(Id)
			public int PrimaryNameIdx;			// REFERENCES LanguageName(Id), ie, index
			public EthnologueLocation(int nEthnologueIdx, string sMainCountryUsedId,
				char chLanguageStatusId, int nPrimaryNameIdx)
			{
				EthnologueIdx = nEthnologueIdx;
				MainCountryUsedId = sMainCountryUsedId;
				LanguageStatusId = chLanguageStatusId;
				PrimaryNameIdx = nPrimaryNameIdx;
			}
		};
		static List<EthnologueLocation> s_tblEthnologueLocation;

		/// <summary>
		/// Constructor.  Loads the data into static memory the first time it's called.
		/// </summary>
		public Ethnologue()
		{
			if (s_tblCountry == null)
			{
				List<string[]> rgCountryCodes = LoadCountryCodes();
				List<string[]> rgLanguageCodes = LoadLanguageCodes();
				List<string[]> rgLanguageIndex = LoadLanguageIndex();
				List<string[]> rgIsoData = LoadISO_639_3();
				FixTables(rgCountryCodes, rgLanguageCodes, rgLanguageIndex, rgIsoData);
			}
		}

		const int kCountryId = 0;
		const int kCountryName = 1;
		const int kCountryArea = 2;

		private List<string[]> LoadCountryCodes()
		{
			List<string[]> rgCountryCodes = new List<string[]>();
			string sFile = Path.Combine(InstallFolder, "CountryCodes.tab");
			// Bizarrely, while the other two files we get from the Ethnologue are UTF8, this one is not.
			// See e.g. Cote d'Ivoire -- the first o has an accent that doesn't come out right read as UTF8
			using (TextReader rdr = new StreamReader(sFile, Encoding.GetEncoding(1252)))
			{
				string sLine = rdr.ReadLine();
				if (sLine == "CountryID	Name	Area")
					sLine = rdr.ReadLine();
				while (sLine != null)
				{
					string[] rgs = sLine.Split(new char[] { '\t' }, StringSplitOptions.None);
					rgCountryCodes.Add(rgs);
					sLine = rdr.ReadLine();
				}
			}
			return rgCountryCodes;
		}

		const int kLangCodeId = 0;
		const int kLangCodeCountryId = 1;
		const int kLangCodeLangStatus = 2;
		const int kLangCodeLangName = 3;

		private List<string[]> LoadLanguageCodes()
		{
			List<string[]> rgLanguageCodes = new List<string[]>();
			string sFile = Path.Combine(InstallFolder, "LanguageCodes.tab");
			using (TextReader rdr = new StreamReader(sFile, Encoding.UTF8))
			{
				string sLine = rdr.ReadLine();
				if (sLine == "LangID	CountryID	LangStatus	Name")
					sLine = rdr.ReadLine();
				while (sLine != null)
				{
					string[] rgs = sLine.Split(new char[] { '\t' }, StringSplitOptions.None);
					rgLanguageCodes.Add(rgs);
					sLine = rdr.ReadLine();
				}
			}
			return rgLanguageCodes;
		}

		const int kLangIdxId = 0;
		const int kLangIdxCountryId = 1;
		const int kLangIdxNameType = 2;
		const int kLangIdxName = 3;

		private List<string[]> LoadLanguageIndex()
		{
			List<string[]> rgLanguageIndex = new List<string[]>();
			string sFile = Path.Combine(InstallFolder, "LanguageIndex.tab");
			using (TextReader rdr = new StreamReader(sFile, Encoding.UTF8))
			{
				string sLine = rdr.ReadLine();
				if (sLine == "LangID	CountryID	NameType	Name")
					sLine = rdr.ReadLine();
				while (sLine != null)
				{
					string[] rgs = sLine.Split(new char[] { '\t' }, StringSplitOptions.None);
					rgLanguageIndex.Add(rgs);
					sLine = rdr.ReadLine();
				}
			}
			return rgLanguageIndex;
		}

		const int kIsoId = 0;
		const int kIsoPart2B = 1;
		const int kIsoPart2T = 2;
		const int kIsoPart1 = 3;
		const int kIsoScope = 4;
		const int kIsoLangType = 5;
		const int kIsoRefName = 6;
		const int kIsoComment = 7;

		private List<string[]> LoadISO_639_3()
		{
			List<string[]> rgIsoData = new List<string[]>();
			string sFile = Path.Combine(InstallFolder, "iso-639-3_20090210.tab");
			using (TextReader rdr = new StreamReader(sFile, Encoding.UTF8))
			{
				string sLine = rdr.ReadLine();
				if (sLine.Contains("Id	Part2B	Part2T	Part1	Scope	Language_Type	Ref_Name	Comment"))
					sLine = rdr.ReadLine();
				while (sLine != null)
				{
					string[] rgs = sLine.Split(new char[] { '\t' }, StringSplitOptions.None);
					rgIsoData.Add(rgs);
					sLine = rdr.ReadLine();
				}
			}
			return rgIsoData;
		}

		/// <summary>
		/// This method replaces the SQL code found in NormalizeData.sql.
		/// </summary>
		private void FixTables(List<string[]> rgCountryCodes, List<string[]> rgLanguageCodes,
			List<string[]> rgLanguageIndex, List<string[]> rgIsoData)
		{
			// LanguageStatus

			s_tblLanguageStatus = new List<LanguageStatus>();
			s_tblLanguageStatus.Add(new LanguageStatus('L', "Living"));
			s_tblLanguageStatus.Add(new LanguageStatus('N', "Nearly Extinct"));
			s_tblLanguageStatus.Add(new LanguageStatus('X', "Extinct"));
			s_tblLanguageStatus.Add(new LanguageStatus('S', "Second Language Only"));

			// LanguageType

			s_tblLanguageType = new List<LanguageType>();
			s_tblLanguageType.Add(new LanguageType("L", "Language"));
			s_tblLanguageType.Add(new LanguageType("LA", "Language Alternate"));
			s_tblLanguageType.Add(new LanguageType("D", "Dialect"));
			s_tblLanguageType.Add(new LanguageType("DA", "Dialect Alternate"));
			s_tblLanguageType.Add(new LanguageType("DP", "Dialect Perjorative"));
			s_tblLanguageType.Add(new LanguageType("LP", "Language Perjorative"));

			// Countries

			s_tblCountry = new List<Country>();
			for (int i = 0; i < rgCountryCodes.Count; ++i)
			{
				string[] rgs = rgCountryCodes[i];
				s_tblCountry.Add(new Country(rgs[kCountryId], rgs[kCountryName]));
			}

			// LanguageName (now just a complete list of language names)

			s_tblLanguageName = new List<string>();
			HashSet<string> setLangName = new HashSet<string>();
			for (int i = 0; i < rgLanguageIndex.Count; ++i)
			{
				string[] rgs = rgLanguageIndex[i];
				string sName = rgs[kLangIdxName];
				if (!setLangName.Contains(sName))
				{
					setLangName.Add(sName);
					s_tblLanguageName.Add(sName);
				}
			}
			for (int i = 0; i < rgLanguageCodes.Count; ++i)
			{
				string[] rgs = rgLanguageCodes[i];
				string sName = rgs[kLangCodeLangName];
				if (!setLangName.Contains(sName))
				{
					setLangName.Add(sName);
					s_tblLanguageName.Add(sName);
				}
			}
			for (int i = 0; i < rgIsoData.Count; ++i)
			{
				string[] rgs = rgIsoData[i];
				string sName = rgs[kIsoRefName];
				if (!setLangName.Contains(sName))
				{
					setLangName.Add(sName);
					s_tblLanguageName.Add(sName);
				}

			}
			s_tblLanguageName.Sort();

			// EthnologueTbl

			s_tblEthnologue = new List<EthnologueTbl>();
			s_mapIso6393ToIdx = new Dictionary<string, int>();
			s_mapIcuToIdx = new Dictionary<string, int>();
			for (int i = 0; i < rgIsoData.Count; ++i)
			{
				string[] rgs = rgIsoData[i];
				string sIso6391 = rgs[kIsoPart1];
				string sIso6393 = rgs[kIsoId];
				string sIcu = String.IsNullOrEmpty(sIso6391) ? sIso6393 : sIso6391;
				switch (sIso6393)
				{
					// ISO 639-1 and -2 codes were given for Standard Arabic (ara) but not for
					// Arabic (arb). In the meantime, Arabic (ara) got missed in LanguageCodes.
					// We didn't have ara before, so we're running with arb.
					case "arb":
						sIso6391 = "ar";		// defined on ISO 639-1 = "ara"
						sIcu = "ar";
						break;
					// Last I heard there is no good solution for Mandarin Chinese. See Jira
					// issue LT-8112 for details.
					case "cmn":
						sIso6391 = "zh";		// defined on ISO 639-1 = "zho"
						sIcu = "zh";
						break;
					// Also no good solution for Farsi. See LT-9820 for details.
					case "pes":
						sIso6391 = "fa";		// defined on ISO 639-1 = "fas"
						sIcu = "fa";
						break;
					default:
						break;
				}
				s_tblEthnologue.Add(new EthnologueTbl(sIso6391, sIso6393, sIcu));
				s_mapIso6393ToIdx.Add(sIso6393, i);
				if (s_mapIcuToIdx.ContainsKey(sIcu))
				{
					// Use our overrides as the preferred match.
					if (sIso6393 == "arb" || sIso6393 == "cmn" || sIso6393 == "pes")
						s_mapIcuToIdx[sIso6393] = i;
				}
				else
				{
					s_mapIcuToIdx.Add(sIcu, i);
				}
			}
			for (int i = 0; i < rgLanguageCodes.Count; ++i)
			{
				string[] rgs = rgLanguageCodes[i];
				string sIso = rgs[kLangCodeId];
				int idx;
				if (!s_mapIso6393ToIdx.TryGetValue(sIso, out idx))
				{
					idx = s_tblEthnologue.Count;
					s_tblEthnologue.Add(new EthnologueTbl(null, sIso, sIso));
					s_mapIso6393ToIdx.Add(sIso, idx);
					s_mapIcuToIdx.Add(sIso, idx);
				}
			}

			// LanguageLocation

			s_tblLanguageLocation = new List<LanguageLocation>();
			for (int i = 0; i < rgLanguageIndex.Count; ++i)
			{
				string[] rgs = rgLanguageIndex[i];
				int nEthnologueIdx;
				if (!s_mapIso6393ToIdx.TryGetValue(rgs[kLangIdxId], out nEthnologueIdx))
					nEthnologueIdx = -1;
				string sCountryUsedInId = rgs[kLangIdxCountryId];
				string sLanguageTypeId = rgs[kLangIdxNameType];
				int nLanguageNameIdx = s_tblLanguageName.BinarySearch(rgs[kLangIdxName]);
				s_tblLanguageLocation.Add(new LanguageLocation(nEthnologueIdx, sCountryUsedInId,
					sLanguageTypeId, nLanguageNameIdx));
			}

			// EthnologueLocation

			s_tblEthnologueLocation = new List<EthnologueLocation>();
			for (int i = 0; i < rgLanguageCodes.Count; ++i)
			{
				string[] rgs = rgLanguageCodes[i];
				int nEthnologueIdx;
				if (!s_mapIso6393ToIdx.TryGetValue(rgs[kLangCodeId], out nEthnologueIdx))
					nEthnologueIdx = -1;
				string sMainCountryUsedId = rgs[kLangCodeCountryId];
				string sLangStatus = rgs[kLangCodeLangStatus];
				char chLanguageStatusId = String.IsNullOrEmpty(sLangStatus) ? ' ' : sLangStatus[0];
				int nPrimaryNameIdx = s_tblLanguageName.BinarySearch(rgs[kLangCodeLangName]);
				s_tblEthnologueLocation.Add(new EthnologueLocation(nEthnologueIdx, sMainCountryUsedId,
					chLanguageStatusId, nPrimaryNameIdx));

			}
		}

		/// <summary>
		/// Replaces the SQL stored procedure of the same name.
		/// </summary>
		public string GetIcuCode(string sEthnologueCode)
		{
			string sIcu = null;
			string sEthno = sEthnologueCode.Trim();
			int idx;
			if (s_mapIso6393ToIdx.TryGetValue(sEthnologueCode, out idx))
				sIcu = s_tblEthnologue[idx].Icu;
			if (!String.IsNullOrEmpty(sIcu))
				sIcu = sIcu.Trim();
			if (String.IsNullOrEmpty(sIcu))
				sIcu = "x" + sEthno;
			return sIcu;
		}

		/// <summary>
		/// Replaces the SQL stored procedure of the same name.
		/// </summary>
		public string GetIsoCode(string sCode)
		{
			string sIso = null;
			sCode = sCode.Trim();
			int nLenCode = sCode.Length;
			if (nLenCode == 2 || nLenCode == 3)
			{
				int idx;
				if (s_mapIcuToIdx.TryGetValue(sCode, out idx))
					sIso = s_tblEthnologue[idx].Iso6393;
			}
			else if (nLenCode == 4 && sCode[0] == 'e')
			{
				sIso = sCode.Substring(1);
			}
			return sIso;
		}

		/// <summary>
		/// Structure for holding return values from former stored functions.
		/// </summary>
		public struct Names
		{
			/// <summary>index into the language name list</summary>
			public readonly int LangIdx;
			/// <summary>the language name (from the list)</summary>
			public readonly string LangName;
			/// <summary>the two-letter ISO country id code</summary>
			public readonly string CountryId;
			/// <summary>the English name of the country</summary>
			public readonly string CountryName;
			/// <summary>index into the Ethnologue table</summary>
			public readonly int EthnologueIdx;
			/// <summary>the Ethnologue (or Icu) code</summary>
			public readonly string EthnologueCode;

			/// <summary>
			/// Constructor.
			/// </summary>
			public Names(int nLangIdx, string sLangName, string sCountryId, string sCountryName,
				int nEthnologueIdx, string sEthnologueCode)
			{
				LangIdx = nLangIdx;
				LangName = sLangName;
				CountryId = sCountryId;
				CountryName = sCountryName;
				EthnologueIdx = nEthnologueIdx;
				EthnologueCode = sEthnologueCode;
			}

			/// <summary>
			/// Override to help debugging.
			/// </summary>
			public override string ToString()
			{
				return String.Format(
					"Names: LangName=\"{0}\", CountryId=\"{1}\", CountryName=\"{2}\", EthnologueCode=\"{3}\"",
					LangName, CountryId, CountryName, EthnologueCode);
			}
		}

		/// <summary>
		/// Implements ordering by LangName, CountryName, EthnologueCode, CountryId.
		/// The latter two might not have any influence.
		/// </summary>
		private static int CompareNames(Names x, Names y)
		{
			int nComp = x.LangName.CompareTo(y.LangName);
			if (nComp == 0)
			{
				nComp = x.CountryName.CompareTo(y.CountryName);
				if (nComp == 0)
				{
					nComp = x.EthnologueCode.CompareTo(y.EthnologueCode);
					if (nComp == 0)
					{
						nComp = x.CountryId.CompareTo(y.CountryId);
					}
				}
			}
			return nComp;
		}

		/// <summary>
		/// Replaces the SQL stored function named fnGetLanguageNamesLike.
		/// </summary>
		public List<Names> GetLanguageNamesLike(string sNameLike, char chWhichPart)
		{
			List<Names> rgNames = new List<Names>();
			string query = sNameLike.ToLowerInvariant().Normalize(NormalizationForm.FormD);
			List<int> matchingLanguages = new List<int>();

			// For every language, match query to a substring of language.
			for (int i = 0; i < s_tblLanguageName.Count; ++i)
			{
				string language = s_tblLanguageName[i].ToLowerInvariant().Normalize(NormalizationForm.FormD);
				if (chWhichPart == 'L')
				{
					if (language.StartsWith(query))
						matchingLanguages.Add(i);
					else if (language.CompareTo(query) > 0)
						break;		// no point looking further in a sorted list.
				}
				else if (chWhichPart == 'R')
				{
					if (language.EndsWith(query))
						matchingLanguages.Add(i);
				}
				else
				{
					if (language.Contains(query))
						matchingLanguages.Add(i);
				}
			}

			// If the first attempt fails, split name apart, and
			// retry using the parts of the name.
			if (matchingLanguages.Count == 0)
			{
				// Strip out periods, commas, and percents
				query = query.Replace(".", "");
				query = query.Replace(",", "");
				query = query.Replace("%", "");
				// Split into pieces (on space)
				string[] queryComponents = query.Split(new char[] { ' ' },
					StringSplitOptions.RemoveEmptyEntries);
				Debug.Assert(queryComponents.Length > 0);

				// For each language, match any component of query to a substring of language.
				for (int i = 0; i < s_tblLanguageName.Count; ++i)
				{
					string language = s_tblLanguageName[i].ToLowerInvariant();
					bool fMatch = true;

					for (int j = 0; j < queryComponents.Length; ++j)
					{
						if (!language.Contains(queryComponents[j]))
						{
							fMatch = false;
							break;
						}
					}
					if (fMatch)
						matchingLanguages.Add(i);
				}
			}

			// For each language location that has a language that matched query,
			// if any country uses the language then add the language and this information
			// to the return list.
			foreach (int matchedLanguage in matchingLanguages)
			{
				// For each language location
				for (int i = 0; i < s_tblLanguageLocation.Count; ++i)
				{
					LanguageLocation languageLocation = s_tblLanguageLocation[i];
					// If the matched language is in the location
					if (languageLocation.LanguageIdx == matchedLanguage)
					{
						// For each country, if the language is used in the country, add to return list.
						for (int j = 0; j < s_tblCountry.Count; ++j)
						{
							if (s_tblCountry[j].Id == languageLocation.CountryUsedInId)
							{
								rgNames.Add(new Names(matchedLanguage, s_tblLanguageName[matchedLanguage],
									languageLocation.CountryUsedInId, s_tblCountry[j].Name,
									languageLocation.EthnologueIdx, s_tblEthnologue[languageLocation.EthnologueIdx].Iso6393));
							}
						}
					}
				}
			}

			SelectDistinctNames(rgNames);
			return rgNames;
		}

		/// <summary>
		/// Structure for returning values from a former stored function.
		/// </summary>
		public struct OtherNames
		{
			/// <summary>flag whether this is the primary name</summary>
			public readonly bool IsPrimaryName;
			/// <summary>name of the language</summary>
			public readonly string LangName;

			/// <summary>
			/// Constructor.
			/// </summary>
			public OtherNames(bool fIsPrimaryName, string sLangName)
			{
				IsPrimaryName = fIsPrimaryName;
				LangName = sLangName;
			}

			/// <summary>
			/// Override to help debugging.
			/// </summary>
			public override string ToString()
			{
				return String.Format("OtherNames: IsPrimaryName={0}, LangName=\"{1}\"", IsPrimaryName, LangName);
			}
		}

		/// <summary>
		/// Implements ordering by IsPrimaryName (true before false), LangName.
		/// </summary>
		private static int CompareOtherNames(OtherNames x, OtherNames y)
		{
			if (x.IsPrimaryName)
			{
				if (y.IsPrimaryName)
				{
					return x.LangName.CompareTo(y.LangName);
				}
				else
				{
					return -1;
				}
			}
			else
			{
				if (y.IsPrimaryName)
				{
					return 1;
				}
				else
				{
					return x.LangName.CompareTo(y.LangName);
				}
			}
		}

		/// <summary>
		/// Replaces the SQL stored function named fnGetOtherLanguageNames.
		/// </summary>
		public List<OtherNames> GetOtherLanguageNames(string sEthnoCode)
		{
			List<OtherNames> rgOtherNames = new List<OtherNames>();
			int eId;
			if (s_mapIso6393ToIdx.TryGetValue(sEthnoCode, out eId))
			{
				for (int i = 0; i < s_tblLanguageLocation.Count; ++i)
				{
					LanguageLocation ll = s_tblLanguageLocation[i];
					if (ll.EthnologueIdx == eId)
					{
						string sLanguageName = s_tblLanguageName[ll.LanguageIdx];
						bool fPrimary = false;
						for (int j = 0; j < s_tblEthnologueLocation.Count; ++j)
						{
							if (s_tblEthnologueLocation[j].PrimaryNameIdx == ll.LanguageIdx)
							{
								fPrimary = true;
								break;
							}
						}
						rgOtherNames.Add(new OtherNames(fPrimary, sLanguageName));
					}
				}
			}
			SelectDistinctOtherNames(rgOtherNames);
			return rgOtherNames;
		}

		/// <summary>
		/// Replaces the SQL stored function fnGetLanguagesInCountry.
		/// If fPrimary, gets languages and dialects used mainly in the given country.
		/// Otherwise, gets all dialects and languages for the country
		/// </summary>
		public List<Names> GetLanguagesInCountry(string sCountryName1, bool fPrimary)
		{
			var sCountryName = sCountryName1.ToLowerInvariant().Normalize(NormalizationForm.FormD);
			List<Names> rgNames = new List<Names>();
			if (fPrimary)
			{
				for (int i = 0; i < s_tblCountry.Count; ++i)
				{
					if (CorrectedStartsWith(s_tblCountry[i].Name, sCountryName))
					{
						for (int j = 0; j < s_tblEthnologueLocation.Count; ++j)
						{
							EthnologueLocation el = s_tblEthnologueLocation[j];
							if (el.MainCountryUsedId == s_tblCountry[i].Id)
							{
								rgNames.Add(new Names(el.PrimaryNameIdx,
									s_tblLanguageName[el.PrimaryNameIdx],
									el.MainCountryUsedId, s_tblCountry[i].Name.Normalize(NormalizationForm.FormD),
									el.EthnologueIdx, s_tblEthnologue[el.EthnologueIdx].Iso6393));
							}
						}
						break;
					}
				}
			}
			else
			{
				for (int i = 0; i < s_tblCountry.Count; ++i)
				{
					if (CorrectedStartsWith(s_tblCountry[i].Name, sCountryName))
					{
						for (int j = 0; j < s_tblLanguageLocation.Count; ++j)
						{
							LanguageLocation ll = s_tblLanguageLocation[j];
							if (ll.CountryUsedInId == s_tblCountry[i].Id)
							{
								rgNames.Add(new Names(ll.LanguageIdx,
									s_tblLanguageName[ll.LanguageIdx],
									ll.CountryUsedInId, s_tblCountry[i].Name.Normalize(NormalizationForm.FormD),
									ll.EthnologueIdx, s_tblEthnologue[ll.EthnologueIdx].Iso6393));
							}
						}
					}
				}
			}
			SelectDistinctNames(rgNames);
			return rgNames;
		}

		// All documentation indicates that the commented version should give the same results but it does not.
		// The version actually used correctly matches leading characters even if the last is followed by
		// a diacritic; the commented version does not.
		// Assumes the target string has already been converted to lower and normalized.
		// test.ToLowerInvariant().Normalize(NormalizationForm.FormD).StartsWith(sCountryName, false, CultureInfo.InvariantCulture))
		private bool CorrectedStartsWith(string test, string sCountryName)
		{
			var searchIn = test.ToLowerInvariant().Normalize(NormalizationForm.FormD);
			if (searchIn.Length < sCountryName.Length)
				return false;
			return searchIn.Substring(0, sCountryName.Length) == sCountryName;
		}

		/// <summary>
		/// Replaces the SQL stored function fnGetLanguagesForIso.
		/// </summary>
		public List<Names> GetLanguagesForIso(string sIso6393)
		{
			List<Names> rgNames = new List<Names>();
			int eId;
			if (s_mapIso6393ToIdx.TryGetValue(sIso6393, out eId))
			{
				for (int i = 0; i < s_tblLanguageLocation.Count; ++i)
				{
					LanguageLocation ll = s_tblLanguageLocation[i];
					if (ll.EthnologueIdx == eId)
					{
						for (int j = 0; j < s_tblCountry.Count; ++j)
						{
							if (ll.CountryUsedInId == s_tblCountry[j].Id)
							{
								rgNames.Add(new Names(ll.LanguageIdx,
									s_tblLanguageName[ll.LanguageIdx],
									ll.CountryUsedInId, s_tblCountry[j].Name,
									eId, sIso6393));
							}
						}
					}
				}
			}
			SelectDistinctNames(rgNames);
			return rgNames;
		}

		/// <summary>
		/// Implements SELECT DISTINCT for OtherNames.  Also sorts by IsPrimaryName (true before
		/// false) and LangName.
		/// </summary>
		/// <param name="rgOtherNames"></param>
		private static void SelectDistinctOtherNames(List<OtherNames> rgOtherNames)
		{
			rgOtherNames.Sort(CompareOtherNames);
			for (int i = rgOtherNames.Count - 1; i > 0; --i)
			{
				if (CompareOtherNames(rgOtherNames[i], rgOtherNames[i - 1]) == 0)
					rgOtherNames.RemoveAt(i);
			}
		}

		/// <summary>
		/// Implements SELECT DISTINCT for Names.  Also sorts by language name, country name,
		/// ethnologue code, and country code.
		/// </summary>
		private static void SelectDistinctNames(List<Names> rgNames)
		{
			if (rgNames == null || rgNames.Count < 2)
				return;
			rgNames.Sort(CompareNames);
			for (int i = rgNames.Count - 1; i > 0; --i)
			{
				if (CompareNames(rgNames[i], rgNames[i-1]) == 0 &&
					rgNames[i].EthnologueIdx == rgNames[i-1].EthnologueIdx &&
					rgNames[i].LangIdx == rgNames[i-1].LangIdx)
				{
					rgNames.RemoveAt(i);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory where the Ethnologue was installed, or the DistFiles/Ethnologue
		/// sub-folder of the FWROOT environment variable, if it hasn't been installed.
		/// Will not return null.
		/// </summary>
		/// <exception cref="ApplicationException">
		/// If the installation directory could not be found.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		private static string InstallFolder
		{
			get
			{
				// This allows FW developers (or any other developer who defines this environment
				// variable) to override the default behavior
				string rootDir = Environment.GetEnvironmentVariable("FWROOT");
				if (!String.IsNullOrEmpty(rootDir))
				{
					rootDir = Path.Combine(rootDir, "DistFiles");
				}
				else
				{
					rootDir = RootCodeDir;
					if (rootDir == null)
						throw new ApplicationException(EthnologueStrings.kstidInvalidInstallation);
				}
				string path = Path.Combine(rootDir, "Ethnologue");
				if (!Directory.Exists(path))
					throw new ApplicationException(EthnologueStrings.kstidInvalidInstallation);
				return path;
			}
		}

		/// <summary>
		/// Gets the RootCodeDir from registry, either from HKLM if it exists there, or
		/// otherwise from HKCU.
		/// </summary>
		private static string RootCodeDir
		{
			get
			{
				return GetCodeDirFromRegistryKey(Registry.LocalMachine) ??
					GetCodeDirFromRegistryKey(Registry.CurrentUser);
			}
		}

		private static string GetCodeDirFromRegistryKey(RegistryKey key)
		{
			// TODO: Change this when Ethnologue has its own install location.
			// Note. We don't want to use CreateSubKey here because it will fail on
			// non-administrator logins. The user doesn't need to modify this setting.
			using (var regKey = key.OpenSubKey(@"Software\SIL\FieldWorks\7.0"))
			{
				return (regKey == null) ? null : regKey.GetValue("RootCodeDir") as string;
			}
		}
	}
}
