// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.Ethnologue
{
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
			return $"OtherNames: IsPrimaryName={IsPrimaryName}, LangName=\"{LangName}\"";
		}
	}
}