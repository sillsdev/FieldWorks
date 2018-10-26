// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.Ethnologue
{
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
			return $"Names: LangName=\"{LangName}\", CountryId=\"{CountryId}\", CountryName=\"{CountryName}\", EthnologueCode=\"{EthnologueCode}\"";
		}
	}
}
