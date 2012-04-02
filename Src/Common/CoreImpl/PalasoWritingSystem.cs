using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Windows.Forms;
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
				if (!MiscUtils.IsUnix) // TODO-Linux: UniscribeEngine and GraphiteEngine(FWNX-84) not yet ported
				{
					if (m_isGraphiteEnabled && FontHasGraphiteTables(vg))
					{
						renderEngine = FwGrEngineClass.Create();
						renderEngine.WritingSystemFactory = WritingSystemManager;

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
						if (m_uniscribeEngine == null)
						{
							m_uniscribeEngine = UniscribeEngineClass.Create();
							m_uniscribeEngine.WritingSystemFactory = WritingSystemManager;
						}
						renderEngine = m_uniscribeEngine;
					}
				}
				else
				{
					// default to the UniscribeEngine unless ROMAN environment variable is set.
					if (Environment.GetEnvironmentVariable("ROMAN") == null)
					{
						renderEngine = UniscribeEngineClass.Create();
						renderEngine.WritingSystemFactory = WritingSystemManager;
					}
					else
					{
						renderEngine = RomRenderEngineClass.Create();
						renderEngine.WritingSystemFactory = WritingSystemManager;
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
						cpe.Initialize(Rfc5646Tag.Language, Rfc5646Tag.Script, Rfc5646Tag.Region, Rfc5646Tag.Variant);
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
		/// Gets or sets the Windows locale ID.
		/// </summary>
		/// <value>The LCID.</value>
		public int LCID
		{
			get
			{
				lock (m_syncRoot)
				{
					if (m_lcid == 0)
					{
						// On Linux InstalledInputLanguages or DefaultInputLanguage doesn't do anything sensible.
						// see: https://bugzilla.novell.com/show_bug.cgi?id=613014
						// so just default to en-US.
						if (MiscUtils.IsUnix)
							return new CultureInfo("en-US").LCID;
						InputLanguage defaultLang = MiscUtils.IsUnix ? null : InputLanguage.DefaultInputLanguage;

						InputLanguage inputLanguage = InputLanguage.InstalledInputLanguages.Cast<InputLanguage>().FirstOrDefault(
							lang => lang.Culture.IetfLanguageTag == Id)
													  ?? InputLanguage.DefaultInputLanguage;
						return inputLanguage.Culture.LCID;
					}
					return m_lcid;
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					if (m_lcid != value)
					{
						m_lcid = value;
						m_currentLcid = value;
						Modified = true;
					}
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
					LanguageSubtag subtag = null;
					if (!string.IsNullOrEmpty(Rfc5646Tag.Language))
					{
						subtag = Rfc5646Tag.Language.ToLowerInvariant().StartsWith("x-") ? new LanguageSubtag(Rfc5646Tag.Language.Substring(2), LanguageName, true, null)
							: new LanguageSubtag(LangTagUtils.GetLanguageSubtag(Rfc5646Tag.Language), LanguageName);
					}
					return subtag;
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					string oldISO = Rfc5646Tag.Language;
					if (value == null)
					{
						Rfc5646Tag.Language = null;
						LanguageName = null;
					}
					else
					{
						if (value.IsPrivateUse)
							Rfc5646Tag.Language = "x-" + value.Code;
						else
							Rfc5646Tag.Language = value.Code;
						LanguageName = value.Name;
					}
					if (string.IsNullOrEmpty(oldISO) != string.IsNullOrEmpty(Rfc5646Tag.Language) || oldISO != Rfc5646Tag.Language)
						m_cpe = null;
				}
			}
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
					ScriptSubtag subtag = null;
					if (!string.IsNullOrEmpty(Rfc5646Tag.Script))
					{
						if (Rfc5646Tag.Script.ToLowerInvariant().StartsWith("x-"))
						{
							subtag = new ScriptSubtag(Rfc5646Tag.Script.Substring(2), ScriptName, true);
						}
						else
						{
							subtag = LangTagUtils.GetScriptSubtag(Rfc5646Tag.Script);
							if (subtag.IsPrivateUse)
								subtag = new ScriptSubtag(Rfc5646Tag.Script, ScriptName, true);
						}
					}
					return subtag;
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					string oldScript = Rfc5646Tag.Script;
					if (value == null)
					{
						Rfc5646Tag.Script = null;
						ScriptName = null;
					}
					else if (value.IsPrivateUse)
					{
						Rfc5646Tag.Script = "x-" + value.Code;
						ScriptName = value.Name;
					}
					else
					{
						Rfc5646Tag.Script = value.Code;
						ScriptName = null;
					}
					if (string.IsNullOrEmpty(oldScript) != string.IsNullOrEmpty(Rfc5646Tag.Script) || oldScript != Rfc5646Tag.Script)
						m_cpe = null;
				}
			}
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
					if (!string.IsNullOrEmpty(Rfc5646Tag.Region))
					{
						if (Rfc5646Tag.Region.ToLowerInvariant().StartsWith("x-"))
						{
							subtag = new RegionSubtag(Rfc5646Tag.Region.Substring(2), RegionName, true);
						}
						else
						{
							subtag = LangTagUtils.GetRegionSubtag(Rfc5646Tag.Region);
							if (subtag.IsPrivateUse)
								subtag = new RegionSubtag(Rfc5646Tag.Region, RegionName, true);
						}
					}
					return subtag;
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					string oldRegion = Rfc5646Tag.Region;
					if (value == null)
					{
						Rfc5646Tag.Region = null;
						RegionName = null;
					}
					else if (value.IsPrivateUse)
					{
						if (LangTagUtils.IsPrivateUseRegionCode(value.Code))
							Rfc5646Tag.Region = value.Code;
						else
							Rfc5646Tag.Region = "x-" + value.Code;
						RegionName = value.Name;
					}
					else
					{
						Rfc5646Tag.Region = value.Code;
						RegionName = null;
					}
					if (string.IsNullOrEmpty(oldRegion) != string.IsNullOrEmpty(Rfc5646Tag.Region) || oldRegion != Rfc5646Tag.Region)
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
					VariantSubtag subtag = null;
					if (!string.IsNullOrEmpty(Rfc5646Tag.Variant))
					{
						if (Rfc5646Tag.Variant.ToLowerInvariant().StartsWith("x-"))
						{
							subtag = new VariantSubtag(Rfc5646Tag.Variant.Substring(2), VariantName, true, null);
						}
						else
						{
							subtag = LangTagUtils.GetVariantSubtag(Rfc5646Tag.Variant);
							if (subtag.IsPrivateUse)
								subtag = new VariantSubtag(Rfc5646Tag.Variant, VariantName, true, null);
						}
					}
					return subtag;
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					string oldVariant = Rfc5646Tag.Variant;
					if (value == null)
					{
						Rfc5646Tag.Variant = null;
						VariantName = null;
					}
					else if (value.IsPrivateUse)
					{
						Rfc5646Tag.Variant = "x-" + value.Code;
						VariantName = value.Name;
					}
					else
					{
						Rfc5646Tag.Variant = value.Code;
						// JohnT: an earlier version of this code set VariantName to null in this case.
						// Ken and I cannot see a reason for this, and it causes problems when creating
						// a new writing system and choosing a variant like Pinyin (the new WS gets the variant code instead
						// of the variant name as part of the WS name). If someone finds or knows of a reason why it
						// should be set to null here, please document it, and also fix the resulting problem in the
						// writing system dialog. However, it seems reasonable that if we're setting the variant
						// to something that has a name, we'd want to keep that information.
						VariantName = value.Name;
					}
					if (string.IsNullOrEmpty(oldVariant) != string.IsNullOrEmpty(Rfc5646Tag.Variant) || oldVariant != Rfc5646Tag.Variant)
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
					VariantSubtag variantSubtag = VariantSubtag;
					sb.Append(scriptSubtag);
					if (sb.Length > 0 && regionSubtag != null)
						sb.Append(", ");

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
			SortRulesType sortUsing;
			LanguageSubtag languageSubtag;
			ScriptSubtag scriptSubtag;
			RegionSubtag regionSubtag;
			VariantSubtag variantSubtag;

			var pws = (PalasoWritingSystem) source;
			lock (pws.m_syncRoot)
			{
				// ILgWritingSystem properties
				lcid = pws.LCID;
				spellCheckingId = pws.SpellCheckingId;
				rtol = pws.RightToLeftScript;
				defFontFeats = pws.DefaultFontFeatures;
				defFont = pws.DefaultFontName;
				keyboard = pws.Keyboard;

				// IWritingSystem properties
				abbr = pws.Abbreviation;
				sortUsing = pws.SortUsing;
				sortRules = pws.SortRules;
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
				LCID = lcid;
				SpellCheckingId = spellCheckingId;
				RightToLeftScript = rtol;
				DefaultFontFeatures = defFontFeats;
				DefaultFontName = defFont;
				Keyboard = keyboard;

				// IWritingSystem properties
				Abbreviation = abbr;
				SortUsing = sortUsing;
				SortRules = sortRules;
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
		/// Gets the RFC-5646 language tag.
		/// </summary>
		/// <value>The RFC-5646 language tag.</value>
		public override string RFC5646
		{
			get
			{
				lock (m_syncRoot)
					return LangTagUtils.ToLangTag(LanguageSubtag, ScriptSubtag, RegionSubtag, VariantSubtag);
			}
		}

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
			const int tag_Silf = 0x666c6953;
			int tblSize;
			vg.GetFontData(tag_Silf, out tblSize);
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
