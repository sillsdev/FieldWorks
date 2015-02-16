using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using SIL.Extensions;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	/// <summary>
	/// A writing system implementation based on the Palaso writing system library.
	/// </summary>
	public class WritingSystem : WritingSystemDefinition, ILgWritingSystem
	{
		private WritingSystemManager m_wsManager;

		private CharacterSetDefinition m_mainCharacterSet;

		private readonly Dictionary<Tuple<string, bool, bool>, IRenderEngine> m_renderEngines = new Dictionary<Tuple<string, bool, bool>, IRenderEngine>();
		private IRenderEngine m_uniscribeEngine;
		private ILgCharacterPropertyEngine m_cpe;

		private readonly object m_syncRoot = new object();

		internal WritingSystem()
		{
			SetupCollectionChangeListeners();
		}

		private WritingSystem(WritingSystem ws)
			: base(ws)
		{
			SetupCollectionChangeListeners();
		}

		private void SetupCollectionChangeListeners()
		{
			Variants.CollectionChanged += VariantsChanged;
			CharacterSets.CollectionChanged += CharacterSetsChanged;
		}

		private void CharacterSetsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			m_cpe = null;
			CharacterSetDefinition mainCharSet;
			MainCharacterSet = CharacterSets.TryGet("main", out mainCharSet) ? mainCharSet : null;
		}

		private void MainCharacterSetChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			m_cpe = null;
		}

		private void VariantsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			m_cpe = null;
		}

		private CharacterSetDefinition MainCharacterSet
		{
			get { return m_mainCharacterSet; }
			set
			{
				if (m_mainCharacterSet != value)
				{
					if (m_mainCharacterSet != null)
						m_mainCharacterSet.Characters.CollectionChanged -= MainCharacterSetChanged;
					m_mainCharacterSet = value;
					if (m_mainCharacterSet != null)
						m_mainCharacterSet.Characters.CollectionChanged += MainCharacterSetChanged;
				}
			}
		}

		/// <summary>
		/// Gets the handle.
		/// </summary>
		/// <value>The handle.</value>
		public int Handle { get; internal set; }

		/// <summary>
		/// Get the language name.
		/// </summary>
		public string LanguageName
		{
			get { return Language.Name; }
		}

		private IRenderEngine CreateRenderEngine(Func<IRenderEngine> createFunc)
		{
			IRenderEngine renderEngine = createFunc();
			renderEngine.WritingSystemFactory = WritingSystemManager;
			if (WritingSystemManager != null)
				WritingSystemManager.RegisterRenderEngine(renderEngine);
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
				if (IsGraphiteEnabled && FontHasGraphiteTables(vg))
				{
					renderEngine = CreateRenderEngine(GraphiteEngineClass.Create);

					string fontFeatures = null;
					if (realFontName == DefaultFont.Name)
						fontFeatures = DefaultFont.Features;
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
		/// Get the default serif font; usually used for the main body of text in a document.
		/// </summary>
		public string DefaultFontName
		{
			get { return DefaultFont == null ? string.Empty : DefaultFont.Name; }
		}

		/// <summary>
		/// Get the "serif font variation" string which is used, for instance, to specify
		/// Graphite features.
		///</summary>
		/// <returns>A System.String </returns>
		public string DefaultFontFeatures
		{
			get { return DefaultFont == null ? string.Empty : DefaultFont.Features; }
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
						string language, script, region, variant;
						IetfLanguageTag.GetParts(LanguageTag, out language, out script, out region, out variant);
						LgIcuCharPropEngine cpe = LgIcuCharPropEngineClass.Create();
						cpe.Initialize(language, script, region, variant);
						if (MainCharacterSet != null)
							cpe.InitCharOverrides(string.Join("\ufffc", MainCharacterSet.Characters));
						m_cpe = cpe;
					}
					return m_cpe;
				}
			}
		}

		/// <summary>
		/// Gets the ISO 639-3 language code (or Ethnologue code).
		/// </summary>
		/// <value>The ISO 639-3 language code.</value>
		public string ISO3
		{
			get { return Language.Iso3Code; }
		}

		/// <summary>
		/// Gets the displayable name for the writing system.
		/// </summary>
		/// <value>The display label.</value>
		public override string DisplayLabel
		{
			get
			{
				var sb = new StringBuilder();

				// Enhance: Is it worth negotiating with Palaso to put this part
				// in the base class?
				if (Script != null && !IsVoice)
					sb.Append(Script.ToString());

				if (Region != null)
				{
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(Region.ToString());
				}

				foreach (VariantSubtag variantSubtag in Variants)
				{
					if (variantSubtag != WellKnownSubtags.IpaVariant
						|| !Variants.Any(v => v == WellKnownSubtags.IpaPhonemicPrivateUse || v == WellKnownSubtags.IpaPhoneticPrivateUse))
					{
						if (sb.Length > 0)
							sb.Append(", ");
						sb.Append(variantSubtag.ToString());
					}
				}

				return sb.Length > 0 ? string.Format("{0} ({1})", Language, sb) : Language.ToString();
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
				// TODO WS: what should be the default ICU locale?
				if (Language == null || Language.IsPrivateUse)
					return "root";

				return IetfLanguageTag.ToIcuLocale(LanguageTag);
			}
		}

		/// <summary>
		/// Copies all of the properties from the source writing system to this writing system.
		/// </summary>
		/// <param name="source">The source writing system.</param>
		public void Copy(WritingSystem source)
		{
			Language = source.Language;
			Script = source.Script;
			Region = source.Region;
			Variants.ReplaceAll(source.Variants);
			Abbreviation = source.Abbreviation;
			RightToLeftScript = source.RightToLeftScript;
			Fonts.ReplaceAll(source.Fonts.CloneItems());
			DefaultFont = source.DefaultFont == null ? null : Fonts[source.Fonts.IndexOf(source.DefaultFont)];
			Keyboard = source.Keyboard;
			VersionNumber = source.VersionNumber;
			VersionDescription = source.VersionDescription;
			SpellCheckDictionaries.ReplaceAll(source.SpellCheckDictionaries.CloneItems());
			SpellCheckingId = source.SpellCheckingId;
			DateModified = source.DateModified;
			LocalKeyboard = source.LocalKeyboard;
			WindowsLcid = source.WindowsLcid;
			DefaultRegion = source.DefaultRegion;
			KnownKeyboards.ReplaceAll(source.KnownKeyboards);
			MatchedPairs.Clear();
			MatchedPairs.UnionWith(source.MatchedPairs);
			PunctuationPatterns.Clear();
			PunctuationPatterns.UnionWith(source.PunctuationPatterns);
			QuotationMarks.ReplaceAll(source.QuotationMarks);
			QuotationParagraphContinueType = source.QuotationParagraphContinueType;
			Collations.ReplaceAll(source.Collations.CloneItems());
			DefaultCollation = source.DefaultCollation == null ? null : Collations[source.Collations.IndexOf(source.DefaultCollation)];
			CharacterSets.ReplaceAll(source.CharacterSets.CloneItems());
			LegacyMapping = source.LegacyMapping;
			IsGraphiteEnabled = source.IsGraphiteEnabled;
		}

		/// <summary>
		/// Gets the writing system manager.
		/// </summary>
		/// <value>The writing system manager.</value>
		public WritingSystemManager WritingSystemManager
		{
			get { return m_wsManager; }
			internal set
			{
				if (m_wsManager != value)
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
			var ldmlDataMapper = new LdmlDataMapper();
			ldmlDataMapper.Write(writer, this, null);
		}

		/// <summary>
		/// Raises the <see cref="E:PropertyChanged"/> event.
		/// </summary>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == GetPropertyName(() => DefaultFont) || e.PropertyName == GetPropertyName(() => RightToLeftScript)
				|| e.PropertyName == GetPropertyName(() => IsGraphiteEnabled))
			{
				ClearRenderers();
			}

			if (e.PropertyName == GetPropertyName(() => Language) || e.PropertyName == GetPropertyName(() => Script)
				|| e.PropertyName == GetPropertyName(() => Region))
			{
				m_cpe = null;
			}
			base.OnPropertyChanged(e);
		}

		/// <summary>
		/// Updates the language tag.
		/// </summary>
		protected override void UpdateLanguageTag()
		{
			ClearRenderers();
			base.UpdateLanguageTag();
		}

		private static bool FontHasGraphiteTables(IVwGraphics vg)
		{
			const int tagSilf = 0x53696c66;
			int tblSize = 0;
			vg.GetFontData(tagSilf, ref tblSize, null);
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
			return new WritingSystem(this);
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
