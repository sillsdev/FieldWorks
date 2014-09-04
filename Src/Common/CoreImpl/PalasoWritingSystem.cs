using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Palaso.WritingSystems;
using Palaso.WritingSystems.Collation;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// <summary>
	/// A writing system implementation based on the Palaso writing system library.
	/// </summary>
	public class PalasoWritingSystem : WritingSystemDefinition, IWritingSystem
	{
		private int m_handle;
		private IWritingSystemManager m_wsManager;

		private string m_defaultFontFeatures;
		private string m_validChars;
		private string m_matchedPairs;
		private string m_punctuationPatterns;
		private string m_quotationMarks;
		private int m_lcid;
		private string m_regionName;
		private string m_scriptName;
		private string m_variantName;
		private string m_legacyMapping;
		private bool m_isGraphiteEnabled;

		private int m_currentLcid;

		private readonly Dictionary<Tuple<string, bool, bool>, IRenderEngine> m_renderEngines = new Dictionary<Tuple<string, bool, bool>, IRenderEngine>();
		private IRenderEngine m_uniscribeEngine;
		private ILgCharacterPropertyEngine m_cpe;

		private readonly object m_syncRoot = new object();

		internal PalasoWritingSystem()
		{
		}

		private PalasoWritingSystem(PalasoWritingSystem ws)
			: base(ws)
		{
			m_defaultFontFeatures = ws.m_defaultFontFeatures;
			m_validChars = ws.m_validChars;
			m_matchedPairs = ws.m_matchedPairs;
			m_punctuationPatterns = ws.m_punctuationPatterns;
			m_quotationMarks = ws.m_quotationMarks;
			m_lcid = ws.m_lcid;
			m_regionName = ws.m_regionName;
			m_scriptName = ws.m_scriptName;
			m_variantName = ws.m_variantName;
			m_legacyMapping = ws.m_legacyMapping;
			m_isGraphiteEnabled = ws.m_isGraphiteEnabled;
		}

		#region Implementation of ILgWritingSystem

		/// <summary>
		/// Gets the handle.
		/// </summary>
		/// <value>The handle.</value>
		public int Handle
		{
			get
			{
				lock (m_syncRoot)
					return m_handle;
			}

			internal set
			{
				lock (m_syncRoot)
					m_handle = value;
			}
		}

		private IRenderEngine CreateRenderEngine(Func<IRenderEngine> createFunc)
		{
			var renderEngine = createFunc();
			renderEngine.WritingSystemFactory = WritingSystemManager;
			var palasoWsManager = WritingSystemManager as PalasoWritingSystemManager;
			if (palasoWsManager != null)
				palasoWsManager.RegisterRenderEngine(renderEngine);
			return renderEngine;
		}

		/// <summary>
		/// Get the engine used to render text with the specified properties. At present only
		/// font, bold, and italic properties are significant.
		/// Font name may be '&lt;default serif&gt;' which produces a renderer suitable for the default
		/// serif font.
		/// </summary>
		/// <param name="vg"></param>
		/// <returns></returns>
		public IRenderEngine get_Renderer(IVwGraphics vg)
		{
			lock (m_syncRoot)
			{
				LgCharRenderProps chrp = vg.FontCharProperties;
				string fontName = MarshalEx.UShortToString(chrp.szFaceName);
				Tuple<string, bool, bool> key = Tuple.Create(fontName, chrp.ttvBold == (int) FwTextToggleVal.kttvForceOn,
															 chrp.ttvItalic == (int) FwTextToggleVal.kttvForceOn);
				IRenderEngine renderEngine;
				if (m_renderEngines.TryGetValue(key, out renderEngine))
					return renderEngine;
				Tuple<string, bool, bool> key2 = null;
				string realFontName;
				if (TryGetRealFontName(fontName, out realFontName))
				{
					MarshalEx.StringToUShort(realFontName, chrp.szFaceName);
					vg.SetupGraphics(ref chrp);
					key2 = Tuple.Create(realFontName, key.Item2, key.Item3);
					if (m_renderEngines.TryGetValue(key2, out renderEngine))
					{
						m_renderEngines[key] = renderEngine;
						return renderEngine;
					}
				}
				else
				{
					realFontName = fontName;
				}

				bool graphiteFont = false;
				if (m_isGraphiteEnabled && FontHasGraphiteTables(vg))
				{
					renderEngine = CreateRenderEngine(GraphiteEngineClass.Create);

					string fontFeatures = null;
					if (realFontName == DefaultFontName)
						fontFeatures = DefaultFontFeatures;
					try
					{
						renderEngine.InitRenderer(vg, fontFeatures);
						graphiteFont = true;
					}
					catch
					{
						graphiteFont = false;
					}
				}

				if (!graphiteFont)
				{
					if (!MiscUtils.IsUnix)
					{
						if (m_uniscribeEngine == null)
							m_uniscribeEngine = CreateRenderEngine(UniscribeEngineClass.Create);
						renderEngine = m_uniscribeEngine;
					}
					else
					{
						// default to the UniscribeEngine unless ROMAN environment variable is set.
						if (Environment.GetEnvironmentVariable("ROMAN") == null)
							renderEngine = CreateRenderEngine(UniscribeEngineClass.Create);
						else
							renderEngine = CreateRenderEngine(RomRenderEngineClass.Create);
					}
				}

				m_renderEngines[key] = renderEngine;
				if (key2 != null)
					m_renderEngines[key2] = renderEngine;
				return renderEngine;
			}
		}

		/// <summary>
		/// Get the "serif font variation" string which is used, for instance, to specify
		/// Graphite features.
		///</summary>
		/// <returns>A System.String </returns>
		public string DefaultFontFeatures
		{
			get
			{
				lock (m_syncRoot)
					return m_defaultFontFeatures;
			}

			set
			{
				lock (m_syncRoot)
				{
					if (m_defaultFontFeatures != value)
						ClearRenderers();
					UpdateString(ref m_defaultFontFeatures, value);
				}
			}
		}

		/// <summary>
		/// The current input language. By default this is derived from Locale, but it can be
		/// overridden temporarily (for one session). Note that this is not persisted.
		/// Set the current (temporary) LangId for this writing system.
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32 </returns>
		public int CurrentLCID
		{
			get
			{
				lock (m_syncRoot)
				{
					if (m_currentLcid == 0)
						return m_lcid;
					return m_currentLcid;
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					if (m_currentLcid != value)
					{
						m_currentLcid = value;
						// Do NOT set this: this is a temporary value that is not persisted,
						// we do NOT need to regard the WS as modified. Doing so can cause a
						// new modify time, which makes other projects think their definition
						// of the WS is out of date, which leads to annoying and mystifying
						// prompts to the user asking whether to update to match the 'modified'
						// WS which the user is not aware of having changed. FWR-2684.
						//Modified = true;
					}
				}
			}
		}

		/// <summary>
		/// Apply any changes to the chrp before it is used for real: currently,
		/// interpret the magic font names.
		/// </summary>
		/// <param name="chrp"></param>
		public void InterpretChrp(ref LgCharRenderProps chrp)
		{
			string fontName = MarshalEx.UShortToString(chrp.szFaceName);
			string realFontName;
			if (TryGetRealFontName(fontName, out realFontName))
				MarshalEx.StringToUShort(realFontName, chrp.szFaceName);

			if (chrp.ssv != (int) FwSuperscriptVal.kssvOff)
			{
				if (chrp.ssv == (int) FwSuperscriptVal.kssvSuper)
					chrp.dympOffset += chrp.dympHeight / 3;
				else
					chrp.dympOffset -= chrp.dympHeight / 5;
				chrp.dympHeight = (chrp.dympHeight * 2) / 3;
				// Make sure no way it can happen twice!
				chrp.ssv = (int) FwSuperscriptVal.kssvOff;
			}
		}

		/// <summary>
		/// Get the engine used to find character properties, including figuring out where line
		/// breaks are allowed.
		/// </summary>
		/// <value></value>
		/// <returns>A ILgCharacterPropertyEngine </returns>
		public ILgCharacterPropertyEngine CharPropEngine
		{
			get
			{
				lock (m_syncRoot)
				{
					if (m_cpe == null)
					{
						LgIcuCharPropEngine cpe = LgIcuCharPropEngineClass.Create();
						cpe.Initialize(Language, Script, Region, Variant);
						if (!string.IsNullOrEmpty(m_validChars))
						{
							try
							{
								XElement validCharsElem = XElement.Parse(m_validChars);
								XElement wordFormingElem = validCharsElem.Element("WordForming");
								if (wordFormingElem != null)
									cpe.InitCharOverrides((string) wordFormingElem);
							}
							catch (XmlException)
							{
								// the valid chars isn't XML, so just ignore it
							}
						}
						m_cpe = cpe;
					}
					return m_cpe;
				}
			}
		}

		/// <summary>
		/// Gets or sets the default font.
		/// </summary>
		/// <value>The default name of the font.</value>
		public override string DefaultFontName
		{
			get
			{
				lock (m_syncRoot)
				{
					if (string.IsNullOrEmpty(base.DefaultFontName))
						return "Charis SIL";
					return base.DefaultFontName;
				}
			}
			set
			{
				lock (m_syncRoot)
				{
					if (base.DefaultFontName != value)
						ClearRenderers();
					base.DefaultFontName = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the script is right-to-left.
		/// </summary>
		/// <value><c>true</c> if the script is right-to-left.</value>
		public override bool RightToLeftScript
		{
			get
			{
				lock (m_syncRoot)
					return base.RightToLeftScript;
			}
			set
			{
				lock (m_syncRoot)
				{
					if (base.RightToLeftScript != value)
						ClearRenderers();
					base.RightToLeftScript = value;
				}
			}
		}

		/// <summary>
		/// Gets the ISO 639-3 language code (or Ethnologue code).
		/// </summary>
		/// <value>The ISO 639-3 language code.</value>
		public string ISO3
		{
			get
			{
				return LanguageSubtag.ISO3Code;
			}
		}

		/// <summary>
		/// Gets or sets the keyboard.
		/// </summary>
		/// <value>The keyboard.</value>
		public override string Keyboard
		{
			get
			{
				lock (m_syncRoot)
					return base.Keyboard;
			}
			set
			{
				lock (m_syncRoot)
					base.Keyboard = value;
			}
		}

		/// <summary>
		/// Gets or sets the name of the language.
		/// </summary>
		/// <value>The name of the language.</value>
		public override string LanguageName
		{
			get
			{
				lock (m_syncRoot)
					return base.LanguageName;
			}
			set
			{
				lock (m_syncRoot)
					base.LanguageName = value;
			}
		}

		/// <summary>
		/// Gets or sets the spell checking id.
		/// </summary>
		/// <value>The spell checking id.</value>
		public override string SpellCheckingId
		{
			get
			{
				lock (m_syncRoot)
					return base.SpellCheckingId;
			}
			set
			{
				lock (m_syncRoot)
					base.SpellCheckingId = value;
			}
		}

		#endregion

		#region Implementation of IWritingSystem

		/// <summary>
		/// Gets or sets the abbreviation.
		/// </summary>
		/// <value>The abbreviation.</value>
		public override string Abbreviation
		{
			get
			{
				lock (m_syncRoot)
					return base.Abbreviation;
			}
			set
			{
				lock (m_syncRoot)
					base.Abbreviation = value;
			}
		}

		/// <summary>
		/// Gets or sets the sort method.
		/// </summary>
		/// <value>The sort method.</value>
		public override SortRulesType SortUsing
		{
			get
			{
				lock (m_syncRoot)
					return base.SortUsing;
			}
			set
			{
				lock (m_syncRoot)
					base.SortUsing = value;
			}
		}

		/// <summary>
		/// Gets or sets the sort rules.
		/// </summary>
		/// <value>The sort rules.</value>
		public override string SortRules
		{
			get
			{
				lock (m_syncRoot)
					return base.SortRules;
			}
			set
			{
				lock (m_syncRoot)
					base.SortRules = value;
			}
		}

		/// <summary>
		/// Gets the collator.
		/// </summary>
		/// <value>The collator.</value>
		public override ICollator Collator
		{
			get
			{
				lock (m_syncRoot)
					return base.Collator;
			}
		}

		/// <summary>
		/// Gets or sets the language subtag.
		/// </summary>
		/// <value>The language.</value>
		public LanguageSubtag LanguageSubtag
		{
			get
			{
				lock (m_syncRoot)
				{
					if (string.IsNullOrEmpty(Language))
						return null;
					if (Language.ToLowerInvariant() == "qaa")
					{
						// These will generally be private-use language codes, but the special case of plain "qaa" is not.
						var languageCode = NthPartOfVariant(0);
						if (languageCode == "")
							languageCode = "qaa"; // special case for default language with just that language code, no variant.
						return new LanguageSubtag(languageCode, LanguageName, !languageCode.Equals("qaa", StringComparison.OrdinalIgnoreCase), null);
					}
					return new LanguageSubtag(LangTagUtils.GetLanguageSubtag(Language), LanguageName);
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					string oldISO = Language;
					string oldVariant = Variant;
					if (value == null)
					{
						Language = null;
						LanguageName = null;
					}
					else
					{
						if (value.IsPrivateUse)
						{
							Language = "qaa";
							UpdateVariantPart(0, oldISO, value.Code, "qaa", true);
						}
						else // normal case, not private use language code.
						{
							Language = value.Code;
							UpdateVariantPart(0, oldISO, "", "qaa", false);
						}
						LanguageName = value.Name;
					}
					if (!SameString(oldISO, Language) || !SameString(oldVariant, Variant))
						m_cpe = null;
				}
			}
		}

		/// <summary>
		/// Tests whether two strings are equal. Differs from normal equality in treating null as equal to ""
		/// </summary>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		/// <returns></returns>
		bool SameString(string arg1, string arg2)
		{
			if (arg1 == arg2)
				return true;
			return String.IsNullOrEmpty(arg1) && String.IsNullOrEmpty(arg2);
		}

		/// <summary>
		/// Gets or sets the script subtag.
		/// </summary>
		/// <value>The script.</value>
		public ScriptSubtag ScriptSubtag
		{
			get
			{
				lock (m_syncRoot)
				{
					if (string.IsNullOrEmpty(Script))
						return null;
					if (Script.Equals("Qaaa", StringComparison.OrdinalIgnoreCase))
						return new ScriptSubtag(NthPartOfVariant(1), ScriptName, true);
					// Enhance JohnT: Make this true?
					// It is private-use in OUR sense (user can edit code) if it is anything but a STANDARD code.
					// Valid but private-use codes (in the sense defined by the standard) are also private-use to us, since the user may edit.
					return new ScriptSubtag(Script, ScriptName, !ScriptSubtag.IsValidIso15924ScriptCode(Script));
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					string oldScript = Script;
					if (value == null)
					{
						Script = null;
						ScriptName = null;
						UpdateVariantPart(1, oldScript, "", "Qaaa", false);
					}
					else
					{
						ScriptName = value.Name;
						Script = UpdateVariantPart(1, oldScript, value.Code, "Qaaa", !ScriptSubtag.IsValidIso15924ScriptCode(value.Code));
					}
					if (string.IsNullOrEmpty(oldScript) != string.IsNullOrEmpty(Script) || oldScript != Script)
						m_cpe = null;
				}
			}
		}

		/// <summary>
		/// Combine left and right using the specified separator, unless one of them is null or empty; then just return the
		/// non-empty one.
		/// </summary>
		public static string Combine (string left, string sep, string right)
		{
			if (string.IsNullOrEmpty(left))
				return right ?? "";
			if (string.IsNullOrEmpty(right))
				return left; // not null
			return left + sep + right;
		}

		// Update the nth custom part of the variant, where position = 0 for language, 1 for script, 2 for region.
		// The old value stored in the property (not necessarily the old region code) is passed in oldValue.
		// The property used to be custom if this is equal to markerValue.
		// Return the value that should actually be stored in the appropriate property.
		string UpdateVariantPart(int position, string oldValue, string newValue, string markerValue, bool isNewCustom)
		{
			var isOldCustom = oldValue == markerValue;
			if (!isOldCustom && !isNewCustom)
				return newValue; // nothing to do, no change to variant.
			string leadIn;
			string variant;
			string suffix = GetPartsOfVariant(position, out leadIn, out variant);
			// remove oldValue if any
			if (isOldCustom)
			{
				int hyphen = suffix.IndexOf("-");
				if (hyphen >= 0)
					suffix = suffix.Substring(hyphen + 1);
				else
					suffix = ""; // remove it all, it's the only part left.
			}
			// append new value if any
			if (isNewCustom)
				suffix = newValue + "-" + suffix;
			// Now re-assemble the variant.
			var privateUse = Combine(leadIn, "-", suffix);
			string newVariant = BuildVariant(variant, privateUse);
			if (newVariant.Equals(Variant ?? "", StringComparison.OrdinalIgnoreCase))
				return isNewCustom ? markerValue : newValue;
			m_cpe = null;
			Variant = newVariant;
			return isNewCustom ? markerValue : newValue;
		}

		private string BuildVariant(string standardVariant, string privateUse)
		{
			var newVariant = standardVariant;
			if (privateUse.Length > 0)
			{
				if (standardVariant.Length > 0)
					newVariant = standardVariant + "-x-" + privateUse;
				else
					newVariant = "x-" + privateUse;
			}
			return newVariant;
		}

		internal string NthPartOfVariant(int position)
		{
			string leadIn, variant;
			var suffix = GetPartsOfVariant(position, out leadIn, out variant);
			int hyphen = suffix.IndexOf("-");
			if (hyphen >= 0)
				return suffix.Substring(0, hyphen);
			return suffix;

		}

		/// <summary>
		/// Split up the variant into the true (standard) variant(s), returned in variant;
		/// the part the comes (or should come) before the position'th custom item, where
		/// position is 0 for language, 1 for script, 2 for region, 4 for variant, returned as the main output;
		/// and the private use part before the indicated item, in leadIn.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="leadIn"></param>
		/// <param name="variant"></param>
		/// <returns></returns>
		internal string GetPartsOfVariant(int position, out string leadIn, out string variant)
		{
			int index = position;
			if (position > 2 && !Region.Equals("QM", StringComparison.OrdinalIgnoreCase))
				position--;
			if (position > 1 && !Script.Equals("Qaaa", StringComparison.OrdinalIgnoreCase))
				position--;
			if (position > 0 && !(Language.Equals("qaa", StringComparison.OrdinalIgnoreCase)))
				position--;
			string suffix = Variant ?? "";
			// set variant to the non-private-use part of the variant and suffix to the private-use part (without the x).
			variant = "";
			suffix = LangTagUtils.GetPrivateUseAndStandardVariant(suffix, out variant);
			// Now set leadIn to the parts that correspond to any prior custom things, and suffix to what's left, possibly starting with oldValue
			leadIn = "";
			for (int i = 0; i < position; i++)
			{
				if (leadIn.Length > 0)
					leadIn += "-";
				int hyphen = suffix.IndexOf("-");
				if (hyphen >= 0)
				{
					leadIn += suffix.Substring(0, hyphen);
					suffix = suffix.Substring(hyphen + 1);
				}
				else if (suffix.Length > 0)
				{
					leadIn += suffix;
					suffix = "";
				}
					// missing expected part to match leading item. Fill in default.
				else if (i == 0)
					leadIn += "qaa";
				else
				{
					leadIn += "Qaaa";
				}
			}
			return suffix;
		}

		/// <summary>
		/// Gets or sets the region subtag.
		/// </summary>
		/// <value>The region.</value>
		public RegionSubtag RegionSubtag
		{
			get
			{
				lock (m_syncRoot)
				{
					RegionSubtag subtag = null;
					if (string.IsNullOrEmpty(Region))
						return null;
					if (Region.Equals("QM", StringComparison.OrdinalIgnoreCase))
						return new RegionSubtag(NthPartOfVariant(2), RegionName, true);
					// It is private-use in OUR sense (user can edit code) if it is anything but a STANDARD code.
					// Valid but private-use codes (in the sense defined by the standard) are also private-use to us, since the user may edit.
					return new RegionSubtag(Region, RegionName, !RegionSubtag.IsStandardIso3166Region(Region));
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					string oldRegion = Region;
					if (value == null)
					{
						Region = null;
						RegionName = null;
						UpdateVariantPart(2, oldRegion, "", "QM", false);
					}
					else
					{
						RegionName = value.Name;
						Region = UpdateVariantPart(2, oldRegion, value.Code, "QM", LangTagUtils.TreatAsCustomRegion(value.Code));
					}
					if (string.IsNullOrEmpty(oldRegion) != string.IsNullOrEmpty(Region) || oldRegion != Region)
						m_cpe = null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the variant subtag.
		/// </summary>
		/// <value>The variant.</value>
		public VariantSubtag VariantSubtag
		{
			get
			{
				lock (m_syncRoot)
				{
					string leadIn, mainVariant;
					var suffix = GetPartsOfVariant(3, out leadIn, out mainVariant);
					if (suffix == "" && mainVariant == "")
						return null;
					return LangTagUtils.GetVariantSubtag(BuildVariant(mainVariant, suffix), VariantName, null);
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					string oldVariant = Variant;
					string leadIn, oldMainVariant;
					var oldSuffix = GetPartsOfVariant(3, out leadIn, out oldMainVariant);
					string newMainVariant = "";
					string newSuffix = "";
					if (value == null)
					{
						VariantName = null;
					}
					else
					{
						VariantName = value.Name;
						newSuffix = LangTagUtils.GetPrivateUseAndStandardVariant(value.Code, out newMainVariant);
					}
					var privateUse = Combine(leadIn, "-", newSuffix);
					string newVariant = BuildVariant(newMainVariant, privateUse);
					if (string.IsNullOrEmpty(newVariant))
						Variant = null;
					else
						Variant = newVariant;

					if (string.IsNullOrEmpty(oldVariant) != string.IsNullOrEmpty(Variant) || oldVariant != Variant)
						m_cpe = null;
				}
			}
		}

		/// <summary>
		/// Gets the displayable name for the writing system.
		/// </summary>
		/// <value>The display label.</value>
		public override string DisplayLabel
		{
			get
			{
				lock (m_syncRoot)
				{
					var labelSb = new StringBuilder();
					labelSb.Append(LanguageSubtag);

					var sb = new StringBuilder();
					Subtag scriptSubtag = ScriptSubtag;
					Subtag regionSubtag = RegionSubtag;
					string variantSubtag = null;
					if (VariantSubtag != null)
					{
						variantSubtag = VariantSubtag.ToString();
						if (LanguageSubtag.Code == "qaa")
						{
							variantSubtag = VariantSubtag.DisplayNameWithoutPrivateLanguageName;
							if (variantSubtag == "")
								variantSubtag = null;
						}
					}

					// Enhance: Is it worth negotiating with Palaso to put this part
					// in the base class?
					if (!IsVoice)
					{
						sb.Append(scriptSubtag);
						if (sb.Length > 0 && regionSubtag != null)
							sb.Append(", ");
					}

					sb.Append(regionSubtag);
					if (sb.Length > 0 && variantSubtag != null)
						sb.Append(", ");

					sb.Append(variantSubtag);

					if (sb.Length > 0)
					{
						labelSb.Append(" (");
						labelSb.Append(sb);
						labelSb.Append(")");
					}
					return labelSb.ToString();
				}
			}
		}

		/// <summary>
		/// Gets the icu locale.
		/// </summary>
		/// <value>The icu locale.</value>
		public string IcuLocale
		{
			get
			{
				lock (m_syncRoot)
				{
					// TODO WS: what should be the default ICU locale?
					LanguageSubtag languageSubtag = LanguageSubtag;
					if (languageSubtag == null || languageSubtag.IsPrivateUse)
						return "root";

					return LangTagUtils.ToIcuLocale(LanguageSubtag, ScriptSubtag, RegionSubtag, VariantSubtag);
				}
			}
		}

		/// <summary>
		/// Gets the RFC-5646 language tag.
		/// </summary>
		public string RFC5646
		{
			get
			{
				lock (m_syncRoot)
					return LangTagUtils.ToLangTag(LanguageSubtag, ScriptSubtag, RegionSubtag, VariantSubtag);
			}
		}

		/// <summary>
		/// Gets or sets the valid chars.
		/// </summary>
		/// <value>The valid chars.</value>
		public string ValidChars
		{
			get
			{
				lock (m_syncRoot)
					return m_validChars;
			}

			set
			{
				lock (m_syncRoot)
				{
					if (m_validChars != value)
					{
						m_cpe = null;
						ClearRenderers();
					}
					UpdateString(ref m_validChars, value);
				}
			}
		}

		/// <summary>
		/// Gets or sets the matched pairs.
		/// </summary>
		/// <value>The matched pairs.</value>
		public string MatchedPairs
		{
			get
			{
				lock (m_syncRoot)
					return m_matchedPairs;
			}

			set
			{
				lock (m_syncRoot)
				{
					if (m_matchedPairs != value)
						ClearRenderers();
					UpdateString(ref m_matchedPairs, value);
				}
			}
		}

		/// <summary>
		/// Gets or sets the punctuation patterns.
		/// </summary>
		/// <value>The punctuation patterns.</value>
		public string PunctuationPatterns
		{
			get
			{
				lock (m_syncRoot)
					return m_punctuationPatterns;
			}

			set
			{
				lock (m_syncRoot)
				{
					if (m_punctuationPatterns != value)
						ClearRenderers();
					UpdateString(ref m_punctuationPatterns, value);
				}
			}
		}

		/// <summary>
		/// Gets or sets the quotation marks.
		/// </summary>
		/// <value>The quotation marks.</value>
		public string QuotationMarks
		{
			get
			{
				lock (m_syncRoot)
					return m_quotationMarks;
			}

			set
			{
				lock (m_syncRoot)
				{
					if (m_quotationMarks != value)
						ClearRenderers();
					UpdateString(ref m_quotationMarks, value);
				}
			}
		}

		/// <summary>
		/// Gets or sets the legacy mapping.
		/// </summary>
		/// <value>The legacy mapping.</value>
		public string LegacyMapping
		{
			get
			{
				lock (m_syncRoot)
					return m_legacyMapping;
			}

			set
			{
				lock (m_syncRoot)
					UpdateString(ref m_legacyMapping, value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether Graphite is enabled for this writing system.
		/// </summary>
		/// <value><c>true</c> if Graphite is enabled, otherwise <c>false</c>.</value>
		public bool IsGraphiteEnabled
		{
			get
			{
				lock (m_syncRoot)
					return m_isGraphiteEnabled;
			}

			set
			{
				lock (m_syncRoot)
				{
					if (m_isGraphiteEnabled != value)
					{
						ClearRenderers();
						Modified = true;
					}
					m_isGraphiteEnabled = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:PalasoWritingSystem"/> is modified.
		/// </summary>
		/// <value><c>true</c> if modified, otherwise <c>false</c>.</value>
		public override bool Modified
		{
			get
			{
				lock (m_syncRoot)
					return base.Modified;
			}
			set
			{
				lock (m_syncRoot)
					base.Modified = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:IWritingSystem"/> will be deleted.
		/// </summary>
		/// <value><c>true</c> if it will be deleted, otherwise <c>false</c>.</value>
		public override bool MarkedForDeletion
		{
			get
			{
				lock (m_syncRoot)
					return base.MarkedForDeletion;
			}
			set
			{
				lock (m_syncRoot)
					base.MarkedForDeletion = value;
			}
		}

		/// <summary>
		/// Copies all of the properties from the source writing system to this writing system.
		/// </summary>
		/// <param name="source">The source writing system.</param>
		public void Copy(IWritingSystem source)
		{
			DateTime dateModified;
			int lcid;
			string spellCheckingId, defFontFeats, defFont, keyboard, abbr, sortRules, validChars, matchedPairs, punctPatterns,
				quotationMarks, legacyMapping;
			bool rtol, isGraphiteEnabled;
			bool isVoice;
			SortRulesType sortUsing;
			LanguageSubtag languageSubtag;
			ScriptSubtag scriptSubtag;
			RegionSubtag regionSubtag;
			VariantSubtag variantSubtag;

			var pws = (PalasoWritingSystem) source;
			lock (pws.m_syncRoot)
			{
				// ILgWritingSystem properties
				spellCheckingId = pws.SpellCheckingId;
				rtol = pws.RightToLeftScript;
				defFontFeats = pws.DefaultFontFeatures;
				defFont = pws.DefaultFontName;
				keyboard = pws.Keyboard;
				// This will put the keyboard actually selected into the permanent WS's list.
				// We don't need to remember any others that got temporarily added to KnownKeyboards for testing.
				LocalKeyboard = pws.LocalKeyboard;

				// IWritingSystem properties
				abbr = pws.Abbreviation;
				sortUsing = pws.SortUsing;
				sortRules = pws.SortRules;
				//copy the IsVoice property, see comment in assignment below.
				isVoice = pws.IsVoice;
				languageSubtag = pws.LanguageSubtag;
				scriptSubtag = pws.ScriptSubtag;
				regionSubtag = pws.RegionSubtag;
				variantSubtag = pws.VariantSubtag;
				validChars = pws.ValidChars;
				matchedPairs = pws.MatchedPairs;
				punctPatterns = pws.PunctuationPatterns;
				quotationMarks = pws.QuotationMarks;
				legacyMapping = pws.LegacyMapping;
				isGraphiteEnabled = pws.IsGraphiteEnabled;

				dateModified = pws.DateModified;
			}

			lock (m_syncRoot)
			{
				// ILgWritingSystem properties
				SpellCheckingId = spellCheckingId;
				RightToLeftScript = rtol;
				DefaultFontFeatures = defFontFeats;
				DefaultFontName = defFont;
				Keyboard = keyboard;

				// IWritingSystem properties
				Abbreviation = abbr;
				SortUsing = sortUsing;
				SortRules = sortRules;
				//To meet the previously undocumented pre-conditions of the Script property the IsVoice must be set before the ScriptSubTag
				//the values which the IsVoice property is based on will be re-set again by other properties, rather than try and arrange
				//the other SubTags which affect those properties I'm just setting IsVoice here -naylor 8/10/2011
				IsVoice = isVoice;
				LanguageSubtag = languageSubtag;
				ScriptSubtag = scriptSubtag;
				RegionSubtag = regionSubtag;
				VariantSubtag = variantSubtag;
				ValidChars = validChars;
				MatchedPairs = matchedPairs;
				PunctuationPatterns = punctPatterns;
				QuotationMarks = quotationMarks;
				LegacyMapping = legacyMapping;
				IsGraphiteEnabled = isGraphiteEnabled;

				DateModified = dateModified;
			}
		}

		/// <summary>
		/// Validates the collation rules.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <returns></returns>
		public override bool ValidateCollationRules(out string message)
		{
			lock (m_syncRoot)
				return base.ValidateCollationRules(out message);
		}

		/// <summary>
		/// Gets the writing system manager.
		/// </summary>
		/// <value>The writing system manager.</value>
		public IWritingSystemManager WritingSystemManager
		{
			get
			{
				lock (m_syncRoot)
					return m_wsManager;
			}

			internal set
			{
				lock (m_syncRoot)
				{
					m_wsManager = value;
					ClearRenderers();
				}
			}
		}

		/// <summary>
		/// Writes an LDML representation of this writing system to the specified writer.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void WriteLdml(XmlWriter writer)
		{
			var adaptor = new FwLdmlAdaptor();
			lock (m_syncRoot)
				adaptor.Write(writer, this, null);
		}

		#endregion

		/// <summary>
		/// Gets or sets the name of the region.
		/// </summary>
		/// <value>The name of the region.</value>
		internal string RegionName
		{
			get
			{
				return m_regionName;
			}

			set
			{
				UpdateString(ref m_regionName, value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the script.
		/// </summary>
		/// <value>The name of the script.</value>
		internal string ScriptName
		{
			get
			{
				return m_scriptName;
			}

			set
			{
				UpdateString(ref m_scriptName, value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the variant.
		/// </summary>
		/// <value>The name of the variant.</value>
		internal string VariantName
		{
			get
			{
				return m_variantName;
			}

			set
			{
				UpdateString(ref m_variantName, value);
			}
		}

		private static bool FontHasGraphiteTables(IVwGraphics vg)
		{
			const int tag_Silf = 0x53696c66;
			int tblSize = 0;
			vg.GetFontData(tag_Silf, ref tblSize, null);
			return tblSize > 0;
		}

		private bool TryGetRealFontName(string fontName, out string realFontName)
		{
			if (fontName == "<default font>")
			{
				realFontName = DefaultFontName;
				return true;
			}
			realFontName = null;
			return false;
		}

		private void ClearRenderers()
		{
			m_renderEngines.Clear();
			m_uniscribeEngine = null;
		}

		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <returns></returns>
		public override WritingSystemDefinition Clone()
		{
			lock (m_syncRoot)
				return new PalasoWritingSystem(this);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return DisplayLabel;
		}
	}

	/// <summary>
	/// Exception raised when the RFC5646 identifier tag for the writing system is not known.
	/// </summary>
	public class UnknownPalasoWsException : Exception
	{
		/// <summary>
		/// Gets or sets the writing system RFC5646 identifier tag.
		/// </summary>
		public string WsIdentifier
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the ICU locale (which may be the same as the RFC5646 identifier tag)
		/// </summary>
		public string IcuLocale
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnknownPalasoWsException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="icuLocale">The ICU locale.</param>
		/// <param name="identifier">The RFC5646 language tag.</param>
		public UnknownPalasoWsException(string message, string icuLocale, string identifier) : base(message)
		{
			IcuLocale = icuLocale;
			WsIdentifier = identifier;
		}
	}

	/// <summary>
	/// Exception raised when the RFC5646 identifier tag for the writing system is not known on single run of text.
	/// </summary>
	public class UnknownPalasoWsRunException : UnknownPalasoWsException
	{
		/// <summary>
		/// Gets or sets the run text with the writing system problem.
		/// </summary>
		public string RunText
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnknownPalasoWsRunException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="runText">The run of text that has an unknown writing system.</param>
		/// <param name="icuLocale">The ICU locale.</param>
		/// <param name="identifier">The RFC5646 identifier tag.</param>
		public UnknownPalasoWsRunException(string message, string runText, string icuLocale, string identifier) :
			base(message, icuLocale, identifier)
		{
			RunText = runText;
		}
	}
}
