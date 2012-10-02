// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;

namespace X11.XKlavier
{
	/// <summary>
	/// Provides access to the xklavier XKB config registry methods which provide access to
	/// the keyboard layouts.
	/// </summary>
	internal class XklConfigRegistry
	{
		/// <summary>
		/// XKB keyboard layout description
		/// </summary>
		public struct LayoutDescription
		{
			#region Alternative language codes
			// ICU uses the ISO 639-3 language codes; xkb has at least some ISO 639-2/B codes.
			// According to http://en.wikipedia.org/wiki/ISO_639-2#B_and_T_codes there are 20 languages
			// that have both B and T codes, so we need to translate those.
			private static Dictionary<string, string> s_AlternateLanguageCodes;

			private Dictionary<string, string> AlternateLanguageCodes
			{
				get
				{
					if (s_AlternateLanguageCodes == null)
					{
						s_AlternateLanguageCodes = new Dictionary<string, string>();
						s_AlternateLanguageCodes["alb"] = "sqi"; // Albanian
						s_AlternateLanguageCodes["arm"] = "hye"; // Armenian
						s_AlternateLanguageCodes["baq"] = "eus"; // Basque
						s_AlternateLanguageCodes["bur"] = "mya"; // Burmese
						s_AlternateLanguageCodes["chi"] = "zho"; // Chinese
						s_AlternateLanguageCodes["cze"] = "ces"; // Czech
						s_AlternateLanguageCodes["dut"] = "nld"; // Dutch, Flemish
						s_AlternateLanguageCodes["fre"] = "fra"; // French
						s_AlternateLanguageCodes["geo"] = "kat"; // Georgian
						s_AlternateLanguageCodes["ger"] = "deu"; // German
						s_AlternateLanguageCodes["gre"] = "ell"; // Modern Greek (1453–)
						s_AlternateLanguageCodes["ice"] = "isl"; // Icelandic
						s_AlternateLanguageCodes["mac"] = "mkd"; // Macedonian
						s_AlternateLanguageCodes["mao"] = "mri"; // Maori
						s_AlternateLanguageCodes["may"] = "msa"; // Malay
						s_AlternateLanguageCodes["per"] = "fas"; // Persian
						s_AlternateLanguageCodes["rum"] = "ron"; // Romanian
						s_AlternateLanguageCodes["slo"] = "slk"; // Slovak
						s_AlternateLanguageCodes["tib"] = "bod"; // Tibetan
						s_AlternateLanguageCodes["wel"] = "cym"; // Welsh
					}
					return s_AlternateLanguageCodes;
				}
			}
			#endregion

			/// <summary>
			/// Gets or sets the layout identifier.
			/// </summary>
			/// <remarks>The layout identifier consists of the layout name and variant, separated
			/// by a tab character. Example: "us\tintl".</remarks>
			public string LayoutId { get; internal set; }

			/// <summary>
			/// Gets or sets the description of the layout as found in XklConfigItem. It consists
			/// of the country and the variant, separated by a hyphen.
			/// Example:"USA - International".
			/// </summary>
			public string Description { get; internal set; }

			/// <summary>
			/// Gets or sets the keyboard layout variant, e.g. "International".
			/// </summary>
			public string LayoutVariant { get; internal set; }

			/// <summary>
			/// Gets the locale for the current layout
			/// </summary>
			public string Locale
			{
				get { return LanguageCode + "_" + CountryCode; }
			}

			private string m_LanguageCode;

			/// <summary>
			/// Gets or sets the 3-letter language abbreviation (mostly ISO 639-2/B).
			/// </summary>
			public string LanguageCode
			{
				get { return m_LanguageCode; }
				internal set
				{
					string langCode;
					if (AlternateLanguageCodes.TryGetValue(value, out langCode))
						m_LanguageCode = langCode;
					else
						m_LanguageCode = value;
				}
			}

			/// <summary>
			/// Gets the language name in the culture of the current thread
			/// </summary>
			public string Language
			{
				get
				{
					string language;
					Icu.UErrorCode err;
					Icu.GetDisplayLanguage(Locale, CultureInfo.CurrentUICulture.Name,
						out language, out err);
					return language;
				}
			}

			/// <summary>
			/// Gets or sets the 3-letter country abbreviation. This is taken from the short
			/// description of the layout converted to uppercase.
			/// </summary>
			public string CountryCode { get; internal set; }

			/// <summary>
			/// Gets the contry name in the culture of the current thread
			/// </summary>
			public string Country
			{
				get
				{
					string country;
					Icu.UErrorCode err;
					Icu.GetDisplayCountry(Locale, CultureInfo.CurrentUICulture.Name,
						out country, out err);
					return country;
				}
			}

			public override string ToString()
			{
				return string.Format("[LayoutDescription: LayoutId={0}, Description={1}, " +
					"LayoutVariant={2}, Locale={3}, LanguageCode={4}, Language={5}, " +
					"CountryCode={6}, Country={7}]", LayoutId, Description, LayoutVariant,
					Locale, LanguageCode, Language, CountryCode, Country);
			}
		}

		private Dictionary<string, List<LayoutDescription>> m_Layouts;

		public static XklConfigRegistry Create(XklEngine engine)
		{
			var configRegistry = xkl_config_registry_get_instance(engine.Engine);
			if (!xkl_config_registry_load(configRegistry, true))
				throw new ApplicationException("Got error trying to load config registry: " + engine.LastError);
			return new XklConfigRegistry(configRegistry);
		}

		internal IntPtr ConfigRegistry { get; private set; }

		private XklConfigRegistry(IntPtr configRegistry)
		{
			ConfigRegistry = configRegistry;
		}

		/// <summary>
		/// Gets all possible keyboard layouts defined in the system (though not necessarily
		/// installed).
		/// </summary>
		public Dictionary<string, List<LayoutDescription>> Layouts
		{
			get
			{
				if (m_Layouts == null)
				{
					m_Layouts = new Dictionary<string, List<LayoutDescription>>();
					xkl_config_registry_foreach_language(ConfigRegistry,
						ProcessLanguage, IntPtr.Zero);
				}
				return m_Layouts;
			}
		}

		private void ProcessLanguage(IntPtr configRegistry, ref XklConfigItem item, IntPtr unused)
		{
			IntPtr dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(item));
			Marshal.StructureToPtr(item, dataPtr, false);
			xkl_config_registry_foreach_language_variant(configRegistry, item.Name,
				ProcessOneLayoutForLanguage, dataPtr);
			Marshal.FreeHGlobal(dataPtr);
		}

		private void ProcessOneLayoutForLanguage(IntPtr configRegistry, ref XklConfigItem item,
			ref XklConfigItem subitem, IntPtr data)
		{
			var subitemIsNull = subitem.Parent.RefCount == IntPtr.Zero;
			XklConfigItem language = (XklConfigItem)Marshal.PtrToStructure(data, typeof(XklConfigItem));
			var description = subitemIsNull ? item.Description :
				item.Description + " - " + subitem.Description;
			List<LayoutDescription > layouts;
			if (m_Layouts.ContainsKey(description))
				layouts = m_Layouts[description];
			else
			{
				layouts = new List<LayoutDescription>();
				m_Layouts[description] = layouts;
			}

			var newLayout = new LayoutDescription {
				LayoutId = subitemIsNull ? item.Name : item.Name + "\t" + subitem.Name,
				Description = description,
				LayoutVariant = subitemIsNull ? string.Empty : subitem.Description,
				LanguageCode = language.Name };
			if (item.Short_Description.Length < 3)
			{
				// we have a two letter country code; need to find the three-letter one
				newLayout.CountryCode =
					Icu.GetISO3Country(item.Short_Description + "_" + item.Name).ToUpper();
			}
			else
				newLayout.CountryCode = item.Short_Description.ToUpper();
			layouts.Add(newLayout);
		}

		#region p/invoke related
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		private struct XklConfigItem
		{
			private const int XKL_MAX_CI_NAME_LENGTH = 32;
			private const int XKL_MAX_CI_SHORT_DESC_LENGTH = 10;
			private const int XKL_MAX_CI_DESC_LENGTH = 192;

			[StructLayout(LayoutKind.Sequential)]
			public struct GObject
			{
				public IntPtr Class;
				public IntPtr RefCount;
				public IntPtr Data;
			}

			public GObject Parent;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=XKL_MAX_CI_NAME_LENGTH)]
			public string Name;
			// Setting the length to XKL_MAX_CI_DESC_LENGTH looks like a bug in the header file
			// (/usr/include/libxklavier/xkl_config_item.h)
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=XKL_MAX_CI_DESC_LENGTH)]
			public string Short_Description;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=XKL_MAX_CI_DESC_LENGTH)]
			public string Description;
		}

		private delegate void ConfigItemProcessFunc(IntPtr configRegistry, ref XklConfigItem item, IntPtr data);
		private delegate void TwoConfigItemsProcessFunc(IntPtr configRegistry,
			ref XklConfigItem item, ref XklConfigItem subitem, IntPtr data);

		[DllImport("libxklavier")]
		private extern static IntPtr xkl_config_registry_get_instance(IntPtr engine);

		[DllImport("libxklavier")]
		private extern static bool xkl_config_registry_load(IntPtr configRegistry, bool fExtrasNeeded);

		[DllImport("libxklavier")]
		private extern static void xkl_config_registry_foreach_language(IntPtr configRegistry,
			ConfigItemProcessFunc func, IntPtr data);

		[DllImport("libxklavier")]
		private extern static void xkl_config_registry_foreach_language_variant(IntPtr configRegistry,
			string languageCode, TwoConfigItemsProcessFunc func, IntPtr data);
		#endregion
	}
}
#endif
