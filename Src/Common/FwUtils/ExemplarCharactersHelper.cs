// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExemplarCharactersHelper.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region ExemplarCharactersHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class is used to get the ExemplarCharacters from an ICU resource bundle. These can be
	/// copied into a LanguageDefinition ValidChars property.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ExemplarCharactersHelper
	{
		private static IUnicodeCharacters s_ICU;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the <see cref="ExemplarCharactersHelper"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static ExemplarCharactersHelper()
		{
			s_ICU = new ICUProxy();
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// For a given LanguageDefinition, if the ValidChars field is empty then try to get a set
		/// of ExemplarCharacters (valid characters) from ICU for this language.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="cpe">A character property engine (needed for normalization).</param>
		/// ----------------------------------------------------------------------------------------
		public static void TryLoadValidCharsIfEmpty(IWritingSystem ws,
			ILgCharacterPropertyEngine cpe)
		{
			//Try to load the ValidChars if none have been loaded yet.
			if (string.IsNullOrEmpty(ws.ValidChars))
			{
				string IcuLocale = ws.LanguageSubtag.Code;
				ws.ValidChars = GetValidCharsForLocale(IcuLocale, cpe);
			}
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// ExemplarCharacters can look like this
		/// [a à â æ b c ç {dg} e é è ê ë f-i î ï j-o ô œ p-u ù û ü v w x y ÿ z]
		/// For the above example, this method would remove  [  ]  {  } and expand f-i into f g h i.
		/// </summary>
		/// <param name="origExemplarChars"></param>
		/// <returns></returns>
		/// ----------------------------------------------------------------------------------------
		private static string ExpandExemplarCharacters(string origExemplarChars)
		{
			string sTemp = origExemplarChars.TrimStart('[').TrimEnd(']');
			StringBuilder strBuilder = new StringBuilder();
			string[] sArray = sTemp.Split('-');
			for (int i = 0; i < sArray.Length; i++)
			{
				string strA = sArray[i];
				//copy last string and break out of loop
				if (i == sArray.Length - 1)
				{
					strBuilder.Append(strA);
					break;
				}
				//string strB = sArray[i+1];
				strBuilder.Append(strA);
				//now insert all the characters that are between these two
				string strSequence = CreateStringSequence(strA[strA.Length - 1], sArray[i + 1][0]);
				strBuilder.Append(strSequence);
			}
			strBuilder.Replace("{", string.Empty);
			strBuilder.Replace("}", string.Empty);
			return strBuilder.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds a list containing both uppercase and lowercase exemplar characters based on
		/// the lowercase exemplar characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string AddUppercaseCharacters(string lowerCaseExemplarChars, string locale)
		{
			string upperCaseExemplarChars = s_ICU.ToTitle(lowerCaseExemplarChars, locale);
			StringBuilder strBuilder = new StringBuilder(lowerCaseExemplarChars);
			strBuilder.Append(" ");
			strBuilder.Append(upperCaseExemplarChars);
			return strBuilder.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds a string containing the sequence of characters between two codepoints.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string CreateStringSequence(char cStart, char cEnd)
		{
			StringBuilder str = new StringBuilder();
			for (char c = cStart; c < cEnd; c++)
			{
				str.Append(c.ToString());
				str.Append(" ");
			}

			//we actually do not want cStart in this string
			str.Remove(0, 1);
			return str.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to retrieve a set of ValidChars (ExemplarCharacters) from ICU for the language
		/// associated with the LanguageDefinition parameter.
		/// </summary>
		/// <param name="icuLocale">Code for an ICU locale</param>
		/// <param name="cpe">A character property engine (needed for normalization).</param>
		/// <returns>Space-delimited set of valid characters characters for the given locale
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static string GetValidCharsForLocale(string icuLocale,
			ILgCharacterPropertyEngine cpe)
		{
			if (icuLocale == null)
				return string.Empty;

			s_ICU.Init();
			string strValidChars = ExpandExemplarCharacters(s_ICU.GetExemplarCharacters(icuLocale));
			strValidChars = AddUppercaseCharacters(strValidChars, icuLocale);
			strValidChars = cpe.NormalizeD(strValidChars);

			List<string> lExemplarChars = StringUtils.ParseCharString(strValidChars, " ", cpe);
			StringBuilder strBuilder = new StringBuilder();
			foreach (string ch in lExemplarChars)
			{
				strBuilder.Append(ch);
				strBuilder.Append(" ");
			}
			if (strBuilder.Length > 0)
				strBuilder.Remove(strBuilder.Length - 1, 1);

			//Ensure that we start the string with two space characters so that
			//space is included in the set of valid characters.
			strBuilder.Insert(0, "  ");
			return strBuilder.ToString();
		}
	}
	#endregion

	#region ICUProxy interface (to facilitate testing)
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An interface allowing us to wrap some ICU methods so we can substitute a mock object
	/// for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IUnicodeCharacters
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this instance (must be called before any other methods are called).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Init();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the exemplar characters for the given ICU locale.
		/// </summary>
		/// <param name="icuLocale">Code for the ICU locale.</param>
		/// <returns>string containing all the exemplar characters (typically only lowercase
		/// word-forming characters)</returns>
		/// ------------------------------------------------------------------------------------
		string GetExemplarCharacters(string icuLocale);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string having characters which are title-case equivalents of the
		/// characters in the given string.
		/// </summary>
		/// <param name="s">Input string.</param>
		/// <param name="icuLocale">Code of the ICU locale whose rules are used to determine
		/// title-case equivalents.</param>
		/// ------------------------------------------------------------------------------------
		string ToTitle(string s, string icuLocale);
	}
	#endregion

	#region class ICUProxy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Default implementation of IICUWrapper that wraps the real ICU
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ICUProxy : IUnicodeCharacters
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this instance (must be called before any other methods are called).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void  Init()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the exemplar characters for the given ICU locale.
		/// </summary>
		/// <param name="icuLocale">Code for the ICU locale.</param>
		/// <returns>
		/// string containing all the exemplar characters (typically only lowercase
		/// word-forming characters)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public string  GetExemplarCharacters(string icuLocale)
		{
			ILgIcuResourceBundle rbExemplarCharacters = null;
			ILgIcuResourceBundle rbLangDef = null;
			try
			{
				rbLangDef = LgIcuResourceBundleClass.Create();
				rbLangDef.Init(null, icuLocale);

				// if the name of the resource bundle doesn't match the LocaleAbbr
				// it loaded something else as a default (e.g. "en").
				// in that case we don't want to use the resource bundle so release it.
				if (rbLangDef.Name != icuLocale)
					return string.Empty;

				rbExemplarCharacters = rbLangDef.get_GetSubsection("ExemplarCharacters");
				return rbExemplarCharacters.String;
			}
			finally
			{
				if (rbExemplarCharacters != null)
					System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rbExemplarCharacters);

				if (rbLangDef != null)
					System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rbLangDef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string having characters which are title-case equivalents of the
		/// characters in the given string.
		/// </summary>
		/// <param name="s">Input string.</param>
		/// <param name="icuLocale">Code of the ICU locale whose rules are used to determine
		/// title-case equivalents.</param>
		/// ------------------------------------------------------------------------------------
		public string  ToTitle(string s, string icuLocale)
		{
			return Icu.ToTitle(s, icuLocale);
		}
	}
	#endregion
}
