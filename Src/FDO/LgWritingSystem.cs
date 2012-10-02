// --------------------------------------------------------------------------------------------
// Copyright (C) 2002 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: LgWritingSystem.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Implementation of:
//		LgWritingSystem
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

using Enchant;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for LgWritingSystem.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public partial class LgWritingSystem
	{
		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="fdoCache"></param>
		/// <param name="alIDs"></param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public static LgWritingSystemCollection Load(FdoCache fdoCache, Set<int> alIDs)
		{
			LgWritingSystemCollection lec = new LgWritingSystemCollection(fdoCache);
			foreach (int hvo in alIDs)
				lec.Add(new LgWritingSystem(fdoCache, hvo));
			return lec;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Implements the ever-present ToString() method. This is used for the checked list
		/// boxes on the writing system tab of the project properties dialog. The dialog
		/// is loaded with LgWritingSystem objects and this makes sure the names displayed
		/// in the list are the correct ones. This may also be used in other places.
		/// </summary>
		/// <returns>The writing system name in the default analysis writing system.</returns>
		/// --------------------------------------------------------------------------------
		public override string ToString()
		{
			// Need short name here since this gets changed names from ILgWritingSystem
			// which has updated names, while the FDO cache hasn't been updated yet.
			// This is after a name is changed in the project properties.
			return ShortName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The shortest, non-abbreviated label for the content of this object.
		/// this is the name that you would want to show up in a chooser list.
		/// For writing systems this is the writing system name if available, otherwise
		/// the ICU display name, if available, otherwise the writing system abbreviation,
		/// otherwise the writing system ICULocale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ShortName
		{
			get
			{
				//string str = Name.GetAlternative(m_cache.DefaultUserWs);
				// For now (August 2008), always try for English first.  See LT-8574.
				ILgWritingSystemFactory qwsf = m_cache.LanguageWritingSystemFactoryAccessor;
				int ws = qwsf.GetWsFromStr("en");
				if (ws == 0)
					ws = m_cache.DefaultUserWs;
				string str = Name.GetAlternative(ws);
				if (str == null || str == string.Empty)
				{
					//ILgWritingSystemFactory qwsf = m_cache.LanguageWritingSystemFactoryAccessor;
					IWritingSystem qws = qwsf.get_EngineOrNull(Hvo);
					str = qws.get_UiName(m_cache.DefaultUserWs);
				}
				return SIL.FieldWorks.Common.FwUtils.StringUtils.Compose(str);
			}
		}

		/// <summary>
		/// The user abbreviation we'd like to see typically for this writing system.
		/// Currently this is the user writing system of the Abbr field, or if that
		/// is empty, the ICU code.
		/// We will probably change this (when everything is using it) to try
		/// the analysis writing systems in order, then the user ws.
		/// </summary>
		public static ITsString UserAbbr(FdoCache cache, int hvo)
		{
			ITsString result = cache.MainCacheAccessor.get_MultiStringAlt(hvo,
				(int)LgWritingSystem.LgWritingSystemTags.kflidAbbr, cache.DefaultUserWs);
			if (result != null && result.Length != 0)
				return result;
			ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
			IWritingSystem ws = wsf.get_EngineOrNull(hvo);
			if (ws == null)
				return cache.MakeUserTss(Strings.ksStars);
			else
				return cache.MakeUserTss(ws.get_UiName(cache.DefaultUserWs));
		}

		/// <summary>
		/// The writing system for sorting a list of ShortNames: User Interface.
		/// </summary>
		public override string SortKeyWs
		{
			get
			{
				string sWs = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(
					m_cache.DefaultUserWs);

				if (sWs == null || sWs == string.Empty)
					sWs = m_cache.FallbackUserLocale;

				if (sWs == null || sWs == string.Empty)
					sWs = "en";

				return sWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The writing system abbreviation defined by the user. If not available,
		/// then the ICULocale code for the writing system. Failing that, return
		/// UNK for unknown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Abbreviation
		{
			get
			{
				// Try first to get an abbreviation from LgWriting system.
				string abbr = Abbr.AnalysisDefaultWritingSystem;
				if (abbr != null && abbr.Length > 0)
					return abbr;

				// Failing this return the ICULocale string.
				abbr = ICULocale;
				if (abbr != null && abbr.Length > 0)
					return abbr;

				// If all else fails, return 'UNK' for 'unknown'.
				return Strings.ksUNK;
			}
		}

		#region IWritingSystem Members

		/// <summary>
		/// See http://www.rfc-editor.org/rfc/rfc4646.txt
		/// </summary>
		public string RFC4646bis
		{
			get
			{
				string sLang;
				string sScript;
				string sCountry;
				string sVariant;
				Icu.UErrorCode err = Icu.UErrorCode.U_ZERO_ERROR;
				Icu.GetLanguageCode(ICULocale, out sLang, out err);
				Icu.GetScriptCode(ICULocale, out sScript, out err);
				Icu.GetCountryCode(ICULocale, out sCountry, out err);
				Icu.GetVariantCode(ICULocale, out sVariant, out err);
				if (err != Icu.UErrorCode.U_ZERO_ERROR)
					return "x-" + Abbreviation;
				StringBuilder sb = new StringBuilder(sLang, 255);
				if (!String.IsNullOrEmpty(sScript))
					sb.AppendFormat("-{0}", sScript);
				if (!String.IsNullOrEmpty(sCountry))
					sb.AppendFormat("-{0}", sCountry);
				if (!String.IsNullOrEmpty(sVariant))
					sb.AppendFormat("-{0}", ConvertVariantToRFC(sVariant));
				if (sb[0] == 'x')
					sb.Insert(1, '-');
				return sb.ToString();
			}
		}

		private string ConvertVariantToRFC(string sVariant)
		{
			switch (sVariant)
			{
				case "IPA":
					return "fonipa";
				default:
					return "x-" + sVariant;
			}
		}
		#endregion

		/// <summary>
		/// Convert an RFC4646 language identifier into an ICU locale identifier as used by FieldWorks.
		/// </summary>
		/// <param name="sRFC"></param>
		/// <returns></returns>
		public static string ConvertRFC4646ToICU(string sRFC)
		{
			string sICU = sRFC;
			if (sICU.StartsWith("x-"))
				sICU = sICU.Remove(1, 1);
			if (sICU.EndsWith("-fonipa"))
				sICU = sICU.Replace("-fonipa", "-IPA");
			if (sICU.Contains("-x-"))
				sICU = sICU.Replace("-x-", "-");
			if (sICU.Contains("-"))
			{
				string[] rgs = sICU.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
				if (rgs.Length == 0)
				{
					sICU = sICU.Replace('-', '_');	// BOGUS VALUE!!
				}
				else if (rgs.Length == 1)
				{
					sICU = rgs[0];
				}
				else if (rgs.Length == 2)
				{
					string sSep;
					if (rgs[1].Length == 2)
					{
						sSep = "_";
					}
					else if (rgs[1].Length == 4)
					{
						if (rgs[1] == rgs[1].ToUpperInvariant())
							sSep = "__";		// scripts have initial uppercase, rest lowercase.
						else
							sSep = "_";
					}
					else
					{
						sSep = "__";			// must be a variant.
					}
					sICU = rgs[0] + sSep + rgs[1];
				}
				else
				{
					sICU = sICU.Replace('-', '_');
				}
			}
			int idx = sICU.IndexOf('_');
			string sISO = sICU;
			if (idx > 0)
				sISO = sICU.Substring(0, idx);
			// Ensure that we use 2-character codes when available.  See LT-10459.
			if (sISO.Length > 2)
			{
				string sConnection = String.Format("Server={0}; Database=Ethnologue; User ID=FWDeveloper; " +
					"Password=careful; Pooling=false;", MiscUtils.LocalServerName);
				SqlConnection dbConnection = new SqlConnection(sConnection);
				dbConnection.Open();
				SqlCommand sqlCommand = dbConnection.CreateCommand();
				sqlCommand.CommandText = String.Format("SELECT Icu FROM Ethnologue WHERE Iso6393=N'{0}'", sISO);
				SqlDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.Default);
				if (reader.HasRows)
				{
					reader.Read();
					string sEth = reader.GetString(0);
					if (!String.IsNullOrEmpty(sEth))
						sEth = sEth.Trim();
					if (!String.IsNullOrEmpty(sEth) && sEth != sISO)
					{
						if (sISO == sICU)
						{
							sICU = sEth;
						}
						else
						{
							Debug.Assert(idx > 0);
							sICU = sEth + sICU.Substring(idx);
						}
					}
				}
				reader.Close();
				dbConnection.Close();
			}
			return sICU;
		}

		/// <summary>
		/// Gets text properties used to display the abbreviation of a writing system in a multi-string editor.
		/// Currently this is mainly used in detail views in Harvest, where we don't want to use the blue color
		/// that is the default in DN and perhaps elsewhere. The ControlDarkDark color is chosen to match
		/// the color of the labels used to identify slices (see Slice.DrawLabel).
		/// </summary>
		public static ITsTextProps AbbreviationTextProperties
		{
			get
			{
				ITsPropsBldr tpb = TsPropsBldrClass.Create();
				tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
					(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.ControlDarkDark)));
//				// This is the formula (red + (blue * 256 + green) * 256) for a FW RGB color,
//				// applied to the standard FW color "light blue". This is the default defn of the
//				// "Language Code" character style in DN. We could just use this style, except
//				// I'm not sure Oyster is yet using style sheets.
//				tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
//					47 + (255 * 256 + 96) * 256);
				// And this makes it 8 point.
				tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 8000);
				tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Arial");//JH added to get sans serif
				tpb.SetIntPropValues((int)FwTextPropType.ktptBold,	//JH added so it's not bold on citation form
					(int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvOff);

				tpb.SetIntPropValues((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
				return tpb.GetTextProps();
			}
		}

		/// <summary>
		/// Ensure that there is a spell-checking dictionary for this writing system. If there is not
		/// already, create one. Then return it (whether new or not).
		/// </summary>
		/// <returns></returns>
		public Enchant.Dictionary EnsureDictionary()
		{
			// This undoes the effect of disabling it by setting the spelling dictionary name to <None>.
			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
			IWritingSystem wsEngine = wsf.get_EngineOrNull(Hvo);
			if (String.IsNullOrEmpty(wsEngine.SpellCheckDictionary) || wsEngine.SpellCheckDictionary == "<None>")
				wsEngine.SpellCheckDictionary = wsEngine.IcuLocale;

			int ws = m_cache.DefaultVernWs;
			return EnchantHelper.EnsureDictionary(ws, ICULocale, wsf);
		}

		/// <summary>
		/// Disable the dictionary (by telling the WS that it should have none; don't touch any external dictionary).
		/// </summary>
		public void DisableDictionary()
		{
			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
			wsf.get_EngineOrNull(Hvo).SpellCheckDictionary = "<None>";
		}
	}
}
