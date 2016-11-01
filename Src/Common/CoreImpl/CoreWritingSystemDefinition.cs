using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using SIL.Extensions;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	/// <summary>
	/// A writing system implementation based on the Palaso writing system library.
	/// </summary>
	public class CoreWritingSystemDefinition : WritingSystemDefinition, ILgWritingSystem
	{
		private WritingSystemManager m_wsManager;

		private CharacterSetDefinition m_mainCharacterSet;

		private readonly Dictionary<Tuple<string, bool, bool>, IRenderEngine> m_renderEngines = new Dictionary<Tuple<string, bool, bool>, IRenderEngine>();
		private IRenderEngine m_uniscribeEngine;
		private readonly HashSet<int> m_wordFormingOverrides = new HashSet<int>();

		private readonly object m_syncRoot = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="CoreWritingSystemDefinition"/> class.
		/// </summary>
		public CoreWritingSystemDefinition()
		{
			SetupCollectionChangeListeners();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoreWritingSystemDefinition"/> class.
		/// </summary>
		public CoreWritingSystemDefinition(string ietfLanguageTag)
			: base(ietfLanguageTag)
		{
			SetupCollectionChangeListeners();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoreWritingSystemDefinition"/> class.
		/// </summary>
		public CoreWritingSystemDefinition(CoreWritingSystemDefinition ws)
			: base(ws)
		{
			SetupCollectionChangeListeners();
		}

		private void SetupCollectionChangeListeners()
		{
			CharacterSets.CollectionChanged += CharacterSetsChanged;
		}

		private void CharacterSetsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			CharacterSetDefinition mainCharSet;
			MainCharacterSet = CharacterSets.TryGet("main", out mainCharSet) ? mainCharSet : null;
			m_wordFormingOverrides.Clear();
			if (MainCharacterSet != null)
				AddWordFormingOverrides(MainCharacterSet.Characters);
		}

		private void MainCharacterSetChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					AddWordFormingOverrides(e.NewItems.Cast<string>());
					break;
				case NotifyCollectionChangedAction.Remove:
					RemoveWordFormingOverrides(e.OldItems.Cast<string>());
					break;
				case NotifyCollectionChangedAction.Replace:
					RemoveWordFormingOverrides(e.OldItems.Cast<string>());
					AddWordFormingOverrides(e.NewItems.Cast<string>());
					break;
				case NotifyCollectionChangedAction.Reset:
					m_wordFormingOverrides.Clear();
					AddWordFormingOverrides(MainCharacterSet.Characters);
					break;
			}
		}

		private void AddWordFormingOverrides(IEnumerable<string> chars)
		{
			m_wordFormingOverrides.UnionWith(chars.Select(c => char.ConvertToUtf32(c, 0)));
		}

		private void RemoveWordFormingOverrides(IEnumerable<string> chars)
		{
			m_wordFormingOverrides.ExceptWith(chars.Select(c => char.ConvertToUtf32(c, 0)));
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

		private void SetupRenderEngine(IRenderEngine renderEngine)
		{
			renderEngine.WritingSystemFactory = WritingSystemManager;
			WritingSystemManager.RegisterRenderEngine(renderEngine);
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
				if (IsGraphiteEnabled)
				{
					renderEngine = GraphiteEngineClass.Create();

					string fontFeatures = null;
					if (realFontName == DefaultFontName)
						fontFeatures = DefaultFontFeatures;
					renderEngine.InitRenderer(vg, fontFeatures);
					// check if the font is a valid Graphite font
					if (renderEngine.FontIsValid)
					{
						SetupRenderEngine(renderEngine);
						graphiteFont = true;
					}
				}

				if (!graphiteFont)
				{
					if (!MiscUtils.IsUnix)
					{
						if (m_uniscribeEngine == null)
						{
							m_uniscribeEngine = UniscribeEngineClass.Create();
							m_uniscribeEngine.InitRenderer(vg, null);
							SetupRenderEngine(m_uniscribeEngine);
						}
						renderEngine = m_uniscribeEngine;
					}
					else
					{
						// default to the UniscribeEngine unless ROMAN environment variable is set.
						if (Environment.GetEnvironmentVariable("ROMAN") == null)
							renderEngine = UniscribeEngineClass.Create();
						else
							renderEngine = RomRenderEngineClass.Create();
						renderEngine.InitRenderer(vg, null);
						SetupRenderEngine(renderEngine);
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
		/// Returns true to pass NFC text to the keyboard, otherwise we pass NFD.
		/// </summary>
		public bool UseNfcContext
		{
			get
			{
				// Currently we use NFD only for Keyman keyboards. If the LocalKeyboard
				// property is null than for sure we don't have a keyman keyboard either,
				// so we simply return true (ie. use NFC) in that case.
				return LocalKeyboard == null || LocalKeyboard.UseNfcContext;
			}
		}

		/// <summary>
		/// Return true if character is considered to be part of a word (by default, this
		/// corresponds to Unicode general category Mc, Mn, and categories starting with L).
		/// </summary>
		public bool get_IsWordForming(int ch)
		{
			if (m_wordFormingOverrides.Contains(ch))
				return true;

			return TsStringUtils.IsWordForming(ch);
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
				if (Script != null && !IsVoice && !IetfLanguageTag.IsScriptImplied(LanguageTag))
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
		public void Copy(CoreWritingSystemDefinition source)
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
			var ldmlDataMapper = new LdmlDataMapper(new CoreWritingSystemFactory());
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
			return new CoreWritingSystemDefinition(this);
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
	public class UnknownWritingSystemException : Exception
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
		/// Initializes a new instance of the <see cref="UnknownWritingSystemException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="icuLocale">The ICU locale.</param>
		/// <param name="identifier">The RFC5646 language tag.</param>
		public UnknownWritingSystemException(string message, string icuLocale, string identifier) : base(message)
		{
			IcuLocale = icuLocale;
			WsIdentifier = identifier;
		}
	}

	/// <summary>
	/// Exception raised when the RFC5646 identifier tag for the writing system is not known on single run of text.
	/// </summary>
	public class UnknownWritingSystemRunException : UnknownWritingSystemException
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
		/// Initializes a new instance of the <see cref="UnknownWritingSystemRunException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="runText">The run of text that has an unknown writing system.</param>
		/// <param name="icuLocale">The ICU locale.</param>
		/// <param name="identifier">The RFC5646 identifier tag.</param>
		public UnknownWritingSystemRunException(string message, string runText, string icuLocale, string identifier) :
			base(message, icuLocale, identifier)
		{
			RunText = runText;
		}
	}
}
