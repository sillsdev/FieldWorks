// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	public partial class Ethnologue
	{
		/// <summary>
		/// This table defines the mapping from two-letter ISO country code to
		/// the English name.  Unfortunately, it's not a 1:1 mapping!
		/// </summary>
		private struct Country
		{
			internal readonly string Id;
			internal readonly string Name;
			internal Country(string sId, string sName)
			{
				Id = sId;
				Name = sName;
			}
		}
		private static List<Country> s_tblCountry;

		// The Id is just the index into this list (after it's sorted).
		private static List<string> s_tblLanguageName;

		private struct EthnologueTbl
		{
			internal readonly string Iso6393;    // char[3]
			internal readonly string Icu;        // char[4]
			internal EthnologueTbl(string sIso6393, string sIcu)
			{
				Iso6393 = sIso6393;
				Icu = sIcu;
			}
		}
		private static List<EthnologueTbl> s_tblEthnologue;
		private static Dictionary<string, int> s_mapIso6393ToIdx;
		private static Dictionary<string, int> s_mapIcuToIdx;

		private struct LanguageLocation
		{
			internal readonly int EthnologueIdx;     // REFERENCES Ethnologue index
			internal readonly string CountryUsedInId;   // char[2] - REFERENCES Country(Id)
			internal readonly int LanguageIdx;           // REFERENCES LanguageName(Id), ie, index
			internal LanguageLocation(int nEthnologueIdx, string sCountryUsedInId, int nLanguageIdx)
			{
				EthnologueIdx = nEthnologueIdx;
				CountryUsedInId = sCountryUsedInId;
				LanguageIdx = nLanguageIdx;
			}
		}
		private static List<LanguageLocation> s_tblLanguageLocation;

		private struct EthnologueLocation
		{
			internal readonly int EthnologueIdx;         // REFERENCES Ethnologue index
			internal readonly string MainCountryUsedId; // char[2] - REFERENCES Country(Id)
			internal readonly int PrimaryNameIdx;            // REFERENCES LanguageName(Id), ie, index
			internal EthnologueLocation(int nEthnologueIdx, string sMainCountryUsedId, int nPrimaryNameIdx)
			{
				EthnologueIdx = nEthnologueIdx;
				MainCountryUsedId = sMainCountryUsedId;
				PrimaryNameIdx = nPrimaryNameIdx;
			}
		};
		private static List<EthnologueLocation> s_tblEthnologueLocation;

		/// <summary>
		/// Constructor.  Loads the data into static memory the first time it's called.
		/// </summary>
		public Ethnologue()
		{
			if (s_tblCountry == null)
			{
				var rgCountryCodes = LoadCountryCodes();
				var rgLanguageCodes = LoadLanguageCodes();
				var rgLanguageIndex = LoadLanguageIndex();
				var rgIsoData = LoadIso6393();
				FixTables(rgCountryCodes, rgLanguageCodes, rgLanguageIndex, rgIsoData);
			}
		}

		private static List<string[]> LoadCountryCodes()
		{
			var rgCountryCodes = new List<string[]>();
			var sFile = Path.Combine(InstallFolder, "CountryCodes.tab");
			using (TextReader rdr = new StreamReader(sFile, Encoding.UTF8))
			{
				var sLine = rdr.ReadLine();
				if (sLine == "CountryID	Name	Area")
				{
					sLine = rdr.ReadLine();
				}
				while (sLine != null)
				{
					var rgs = sLine.Split(new[] { '\t' }, StringSplitOptions.None);
					rgCountryCodes.Add(rgs);
					sLine = rdr.ReadLine();
				}
			}
			return rgCountryCodes;
		}

		private static List<string[]> LoadLanguageCodes()
		{
			var rgLanguageCodes = new List<string[]>();
			var sFile = Path.Combine(InstallFolder, "LanguageCodes.tab");
			using (TextReader rdr = new StreamReader(sFile, Encoding.UTF8))
			{
				var sLine = rdr.ReadLine();
				if (sLine == "LangID	CountryID	LangStatus	Name")
				{
					sLine = rdr.ReadLine();
				}
				while (sLine != null)
				{
					var rgs = sLine.Split(new[] { '\t' }, StringSplitOptions.None);
					rgLanguageCodes.Add(rgs);
					sLine = rdr.ReadLine();
				}
			}
			return rgLanguageCodes;
		}

		private static List<string[]> LoadLanguageIndex()
		{
			var rgLanguageIndex = new List<string[]>();
			var sFile = Path.Combine(InstallFolder, "LanguageIndex.tab");
			using (TextReader rdr = new StreamReader(sFile, Encoding.UTF8))
			{
				var sLine = rdr.ReadLine();
				if (sLine == "LangID	CountryID	NameType	Name")
				{
					sLine = rdr.ReadLine();
				}
				while (sLine != null)
				{
					var rgs = sLine.Split(new[] { '\t' }, StringSplitOptions.None);
					rgLanguageIndex.Add(rgs);
					sLine = rdr.ReadLine();
				}
			}
			return rgLanguageIndex;
		}

		private static List<string[]> LoadIso6393()
		{
			var rgIsoData = new List<string[]>();
			var sFile = Path.Combine(InstallFolder, "iso-639-3_20180123.tab");
			using (TextReader rdr = new StreamReader(sFile, Encoding.UTF8))
			{
				var sLine = rdr.ReadLine();
				if (sLine.Contains("Id	Part2B	Part2T	Part1	Scope	Language_Type	Ref_Name	Comment"))
				{
					sLine = rdr.ReadLine();
				}
				while (sLine != null)
				{
					var rgs = sLine.Split(new[] { '\t' }, StringSplitOptions.None);
					rgIsoData.Add(rgs);
					sLine = rdr.ReadLine();
				}
			}
			return rgIsoData;
		}

		/// <summary>
		/// This method replaces the SQL code found in NormalizeData.sql.
		/// </summary>
		private static void FixTables(IReadOnlyList<string[]> rgCountryCodes, IReadOnlyList<string[]> rgLanguageCodes, IReadOnlyList<string[]> rgLanguageIndex, IReadOnlyList<string[]> rgIsoData)
		{
			const int countryId = 0;
			const int countryName = 1;
			const int langCodeId = 0;
			const int langCodeCountryId = 1;
			const int langCodeLangName = 3;
			const int langIdxId = 0;
			const int langIdxCountryId = 1;
			const int langIdxName = 3;
			const int isoId = 0;
			const int isoPart1 = 3;
			const int isoRefName = 6;
			// Countries
			s_tblCountry = new List<Country>();
			foreach (var rgs in rgCountryCodes)
			{
				s_tblCountry.Add(new Country(rgs[countryId], rgs[countryName]));
			}

			// LanguageName (now just a complete list of language names)
			s_tblLanguageName = new List<string>();
			var setLangName = new HashSet<string>();
			foreach (var rgs in rgLanguageIndex)
			{
				var sName = rgs[langIdxName];
				if (!setLangName.Contains(sName))
				{
					setLangName.Add(sName);
					s_tblLanguageName.Add(sName);
				}
			}
			foreach (var rgs in rgLanguageCodes)
			{
				var sName = rgs[langCodeLangName];
				if (!setLangName.Contains(sName))
				{
					setLangName.Add(sName);
					s_tblLanguageName.Add(sName);
				}
			}
			foreach (var rgs in rgIsoData)
			{
				var sName = rgs[isoRefName];
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
			for (var i = 0; i < rgIsoData.Count; ++i)
			{
				var rgs = rgIsoData[i];
				var sIso6391 = rgs[isoPart1];
				var sIso6393 = rgs[isoId];
				var sIcu = string.IsNullOrEmpty(sIso6391) ? sIso6393 : sIso6391;
				switch (sIso6393)
				{
					// ISO 639-1 and -2 codes were given for Standard Arabic (ara) but not for
					// Arabic (arb). In the meantime, Arabic (ara) got missed in LanguageCodes.
					// We didn't have ara before, so we're running with arb.
					case "arb":
						sIcu = "ar";
						break;
					// Last I heard there is no good solution for Mandarin Chinese. See Jira
					// issue LT-8112 for details.
					case "cmn":
						sIcu = "zh";
						break;
					// Also no good solution for Farsi. See LT-9820 for details.
					case "pes":
						sIcu = "fa";
						break;
				}
				s_tblEthnologue.Add(new EthnologueTbl(sIso6393, sIcu));
				s_mapIso6393ToIdx.Add(sIso6393, i);
				if (s_mapIcuToIdx.ContainsKey(sIcu))
				{
					// Use our overrides as the preferred match.
					if (sIso6393 == "arb" || sIso6393 == "cmn" || sIso6393 == "pes")
					{
						s_mapIcuToIdx[sIso6393] = i;
					}
				}
				else
				{
					s_mapIcuToIdx.Add(sIcu, i);
				}
			}
			foreach (var rgs in rgLanguageCodes)
			{
				var sIso = rgs[langCodeId];
				int idx;
				if (!s_mapIso6393ToIdx.TryGetValue(sIso, out idx))
				{
					idx = s_tblEthnologue.Count;
					s_tblEthnologue.Add(new EthnologueTbl(sIso, sIso));
					s_mapIso6393ToIdx.Add(sIso, idx);
					s_mapIcuToIdx.Add(sIso, idx);
				}
			}

			// LanguageLocation
			s_tblLanguageLocation = new List<LanguageLocation>();
			foreach (var rgs in rgLanguageIndex)
			{
				int nEthnologueIdx;
				if (!s_mapIso6393ToIdx.TryGetValue(rgs[langIdxId], out nEthnologueIdx))
				{
					nEthnologueIdx = -1;
				}
				var sCountryUsedInId = rgs[langIdxCountryId];
				var nLanguageNameIdx = s_tblLanguageName.BinarySearch(rgs[langIdxName]);
				s_tblLanguageLocation.Add(new LanguageLocation(nEthnologueIdx, sCountryUsedInId, nLanguageNameIdx));
			}

			// EthnologueLocation
			s_tblEthnologueLocation = new List<EthnologueLocation>();
			foreach (var rgs in rgLanguageCodes)
			{
				int nEthnologueIdx;
				if (!s_mapIso6393ToIdx.TryGetValue(rgs[langCodeId], out nEthnologueIdx))
				{
					nEthnologueIdx = -1;
				}
				var sMainCountryUsedId = rgs[langCodeCountryId];
				var nPrimaryNameIdx = s_tblLanguageName.BinarySearch(rgs[langCodeLangName]);
				s_tblEthnologueLocation.Add(new EthnologueLocation(nEthnologueIdx, sMainCountryUsedId, nPrimaryNameIdx));
			}
		}

		/// <summary>
		/// Replaces the SQL stored procedure of the same name.
		/// </summary>
		public string GetIcuCode(string sEthnologueCode)
		{
			string sIcu = null;
			var sEthno = sEthnologueCode.Trim();
			int idx;
			if (s_mapIso6393ToIdx.TryGetValue(sEthnologueCode, out idx))
			{
				sIcu = s_tblEthnologue[idx].Icu;
			}
			if (!string.IsNullOrEmpty(sIcu))
			{
				sIcu = sIcu.Trim();
			}
			if (string.IsNullOrEmpty(sIcu))
			{
				sIcu = "x" + sEthno;
			}
			return sIcu;
		}

		/// <summary>
		/// Replaces the SQL stored procedure of the same name.
		/// </summary>
		public string GetIsoCode(string sCode)
		{
			string sIso = null;
			sCode = sCode.Trim();
			var nLenCode = sCode.Length;
			if (nLenCode == 2 || nLenCode == 3)
			{
				int idx;
				if (s_mapIcuToIdx.TryGetValue(sCode, out idx))
				{
					sIso = s_tblEthnologue[idx].Iso6393;
				}
			}
			else if (nLenCode == 4 && sCode[0] == 'e')
			{
				sIso = sCode.Substring(1);
			}
			return sIso;
		}

		/// <summary>
		/// Implements ordering by LangName, CountryName, EthnologueCode, CountryId.
		/// The latter two might not have any influence.
		/// </summary>
		private static int CompareNames(Names x, Names y)
		{
			var nComp = x.LangName.CompareTo(y.LangName);
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
			var rgNames = new List<Names>();
			var query = sNameLike.ToLowerInvariant().Normalize(NormalizationForm.FormD);
			var matchingLanguages = new List<int>();

			// For every language, match query to a substring of language.
			for (var i = 0; i < s_tblLanguageName.Count; ++i)
			{
				var language = s_tblLanguageName[i].ToLowerInvariant().Normalize(NormalizationForm.FormD);
				if (chWhichPart == 'L')
				{
					if (language.StartsWith(query))
					{
						matchingLanguages.Add(i);
					}
					else if (language.CompareTo(query) > 0)
					{
						break;      // no point looking further in a sorted list.
					}
				}
				else if (chWhichPart == 'R')
				{
					if (language.EndsWith(query))
					{
						matchingLanguages.Add(i);
					}
				}
				else
				{
					if (language.Contains(query))
					{
						matchingLanguages.Add(i);
					}
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
				var queryComponents = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				Debug.Assert(queryComponents.Length > 0);

				// For each language, match any component of query to a substring of language.
				for (var i = 0; i < s_tblLanguageName.Count; ++i)
				{
					var language = s_tblLanguageName[i].ToLowerInvariant();
					var fMatch = true;

					foreach (var queryComponent in queryComponents)
					{
						if (!language.Contains(queryComponent))
						{
							fMatch = false;
							break;
						}
					}
					if (fMatch)
					{
						matchingLanguages.Add(i);
					}
				}
			}

			// For each language location that has a language that matched query,
			// if any country uses the language then add the language and this information
			// to the return list.
			foreach (var matchedLanguage in matchingLanguages)
			{
				// For each language location
				foreach (var languageLocation in s_tblLanguageLocation)
				{
					// If the matched language is in the location
					if (languageLocation.LanguageIdx == matchedLanguage)
					{
						// For each country, if the language is used in the country, add to return list.
						for (var j = 0; j < s_tblCountry.Count; ++j)
						{
							if (s_tblCountry[j].Id == languageLocation.CountryUsedInId)
							{
								rgNames.Add(new Names(matchedLanguage, s_tblLanguageName[matchedLanguage], languageLocation.CountryUsedInId, s_tblCountry[j].Name, languageLocation.EthnologueIdx, s_tblEthnologue[languageLocation.EthnologueIdx].Iso6393));
							}
						}
					}
				}
			}

			SelectDistinctNames(rgNames);
			return rgNames;
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
				return -1;
			}
			return y.IsPrimaryName ? 1 : x.LangName.CompareTo(y.LangName);
		}

		/// <summary>
		/// Replaces the SQL stored function named fnGetOtherLanguageNames.
		/// </summary>
		public List<OtherNames> GetOtherLanguageNames(string sEthnoCode)
		{
			var rgOtherNames = new List<OtherNames>();
			int eId;
			if (s_mapIso6393ToIdx.TryGetValue(sEthnoCode, out eId))
			{
				foreach (var ll in s_tblLanguageLocation)
				{
					if (ll.EthnologueIdx == eId)
					{
						var sLanguageName = s_tblLanguageName[ll.LanguageIdx];
						var fPrimary = false;
						for (var j = 0; j < s_tblEthnologueLocation.Count; ++j)
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
			var rgNames = new List<Names>();
			if (fPrimary)
			{
				for (var i = 0; i < s_tblCountry.Count; ++i)
				{
					if (CorrectedStartsWith(s_tblCountry[i].Name, sCountryName))
					{
						foreach (var el in s_tblEthnologueLocation)
						{
							if (el.MainCountryUsedId == s_tblCountry[i].Id)
							{
								rgNames.Add(new Names(el.PrimaryNameIdx, s_tblLanguageName[el.PrimaryNameIdx], el.MainCountryUsedId, s_tblCountry[i].Name.Normalize(NormalizationForm.FormD), el.EthnologueIdx, s_tblEthnologue[el.EthnologueIdx].Iso6393));
							}
						}
						break;
					}
				}
			}
			else
			{
				for (var i = 0; i < s_tblCountry.Count; ++i)
				{
					if (CorrectedStartsWith(s_tblCountry[i].Name, sCountryName))
					{
						foreach (var ll in s_tblLanguageLocation)
						{
							if (ll.CountryUsedInId == s_tblCountry[i].Id)
							{
								rgNames.Add(new Names(ll.LanguageIdx, s_tblLanguageName[ll.LanguageIdx], ll.CountryUsedInId, s_tblCountry[i].Name.Normalize(NormalizationForm.FormD), ll.EthnologueIdx, s_tblEthnologue[ll.EthnologueIdx].Iso6393));
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
		private static bool CorrectedStartsWith(string test, string sCountryName)
		{
			var searchIn = test.ToLowerInvariant().Normalize(NormalizationForm.FormD);
			if (searchIn.Length < sCountryName.Length)
			{
				return false;
			}
			return searchIn.Substring(0, sCountryName.Length) == sCountryName;
		}

		/// <summary>
		/// Replaces the SQL stored function fnGetLanguagesForIso.
		/// </summary>
		public List<Names> GetLanguagesForIso(string sIso6393)
		{
			var rgNames = new List<Names>();
			int eId;
			if (s_mapIso6393ToIdx.TryGetValue(sIso6393, out eId))
			{
				foreach (var ll in s_tblLanguageLocation)
				{
					if (ll.EthnologueIdx == eId)
					{
						for (var j = 0; j < s_tblCountry.Count; ++j)
						{
							if (ll.CountryUsedInId == s_tblCountry[j].Id)
							{
								rgNames.Add(new Names(ll.LanguageIdx, s_tblLanguageName[ll.LanguageIdx], ll.CountryUsedInId, s_tblCountry[j].Name, eId, sIso6393));
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
			for (var i = rgOtherNames.Count - 1; i > 0; --i)
			{
				if (CompareOtherNames(rgOtherNames[i], rgOtherNames[i - 1]) == 0)
				{
					rgOtherNames.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Implements SELECT DISTINCT for Names.  Also sorts by language name, country name,
		/// ethnologue code, and country code.
		/// </summary>
		private static void SelectDistinctNames(List<Names> rgNames)
		{
			if (rgNames == null || rgNames.Count < 2)
			{
				return;
			}
			rgNames.Sort(CompareNames);
			for (var i = rgNames.Count - 1; i > 0; --i)
			{
				if (CompareNames(rgNames[i], rgNames[i - 1]) == 0 &&
					rgNames[i].EthnologueIdx == rgNames[i - 1].EthnologueIdx &&
					rgNames[i].LangIdx == rgNames[i - 1].LangIdx)
				{
					rgNames.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Gets the directory where the Ethnologue was installed, or the DistFiles/Ethnologue
		/// sub-folder of the FWROOT environment variable, if it hasn't been installed.
		/// Will not return null.
		/// </summary>
		/// <exception cref="ApplicationException">
		/// If the installation directory could not be found.
		/// </exception>
		private static string InstallFolder
		{
			get
			{
				// This allows FW developers (or any other developer who defines this environment
				// variable) to override the default behavior
				var rootDir = Environment.GetEnvironmentVariable("FWROOT");
				if (!string.IsNullOrEmpty(rootDir))
				{
					rootDir = Path.Combine(rootDir, "DistFiles");
				}
				else
				{
					rootDir = RootCodeDir;
					if (rootDir == null)
					{
						throw new ApplicationException(EthnologueStrings.kstidInvalidInstallation);
					}
				}
				var path = Path.Combine(rootDir, "Ethnologue");
				if (!Directory.Exists(path))
				{
					throw new ApplicationException(EthnologueStrings.kstidInvalidInstallation);
				}
				return path;
			}
		}

		/// <summary>
		/// Gets the RootCodeDir from registry, either from HKCU if it exists there, or
		/// otherwise from HKLM.
		/// </summary>
		private static string RootCodeDir => GetCodeDirFromRegistryKey(Registry.CurrentUser) ?? GetCodeDirFromRegistryKey(Registry.LocalMachine);

		private static string GetCodeDirFromRegistryKey(RegistryKey key)
		{
			// TODO: Change this when Ethnologue has its own install location.
			// Note. We don't want to use CreateSubKey here because it will fail on
			// non-administrator logins. The user doesn't need to modify this setting.
			// Trying to use DirectoryFinder for this causes circular dependencies.
			using (var regKey = key.OpenSubKey(RegistryPathWithVersion))
			{
				return regKey?.GetValue("RootCodeDir") as string;
			}
		}
	}
}
