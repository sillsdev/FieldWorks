using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Palaso.WritingSystems;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	#region Subtag base class
	/// <summary>
	/// This class represents a subtag from the IANA language subtag registry.
	/// </summary>
	public abstract class Subtag
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Subtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		protected Subtag(string code, string name, bool isPrivateUse)
		{
			Code = code;
			Name = name;
			IsPrivateUse = isPrivateUse;
		}

		/// <summary>
		/// Gets the code.
		/// </summary>
		/// <value>The code.</value>
		public string Code
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is private use.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is private use; otherwise, <c>false</c>.
		/// </value>
		public bool IsPrivateUse
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether the code is valid.
		/// </summary>
		/// <value><c>true</c> if the code is valid; otherwise, <c>false</c>.</value>
		public abstract bool IsValid
		{
			get;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with this instance.</param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="T:System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		public override bool Equals(object obj)
		{
			return Equals(obj as Subtag);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Subtag"/> is equal to this instance.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public bool Equals(Subtag other)
		{
			if (other == null)
				throw new NullReferenceException();

			return other.Code == Code;
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
		/// </returns>
		public override int GetHashCode()
		{
			return Code.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			if (!string.IsNullOrEmpty(Name))
				return Name;
			return Code;
		}

		/// <summary>
		/// Compares the language subtags by name.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		public static int CompareByName(Subtag x, Subtag y)
		{
			if (x == null)
			{
				if (y == null)
				{
					return 0;
				}
				else
				{
					return -1;
				}
			}
			else
			{
				if (y == null)
				{
					return 1;
				}
				else
				{
					return x.Name.CompareTo(y.Name);
				}
			}
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(Subtag x, Subtag y)
		{
			if (ReferenceEquals(x, y))
				return true;
			if ((object) x == null || (object) y == null)
				return false;
			return x.Equals(y);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(Subtag x, Subtag y)
		{
			return !(x == y);
		}
	}
	#endregion

	#region LanguageSubtag class
	/// <summary>
	/// This class represents a language from the IANA language subtag registry.
	/// </summary>
	public class LanguageSubtag : Subtag
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LanguageSubtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		/// <param name="iso3Code">The ISO 639-3 language code.</param>
		public LanguageSubtag(string code, string name, bool isPrivateUse, string iso3Code)
			: base(code, name, isPrivateUse)
		{
			ISO3Code = iso3Code;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:LanguageSubtag"/> class.
		/// </summary>
		/// <param name="subtag">The subtag.</param>
		/// <param name="name">The name.</param>
		public LanguageSubtag(LanguageSubtag subtag, string name)
			: this(subtag.Code, name, subtag.IsPrivateUse, subtag.ISO3Code)
		{
		}

		/// <summary>
		/// Gets a value indicating whether the code is valid.
		/// </summary>
		/// <value><c>true</c> if the code is valid; otherwise, <c>false</c>.</value>
		public override bool IsValid
		{
			get
			{
				return LangTagUtils.IsLanguageCodeValid(Code);
			}
		}

		/// <summary>
		/// Gets the ISO 639-3 language code.
		/// </summary>
		/// <value>The ISO 639-3 language code.</value>
		public string ISO3Code
		{
			get;
			private set;
		}
	}
	#endregion

	#region ScriptSubtag class
	/// <summary>
	/// This class represents a script from the IANA language subtag registry.
	/// </summary>
	public class ScriptSubtag : Subtag
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ScriptSubtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		public ScriptSubtag(string code, string name, bool isPrivateUse)
			: base(code, name, isPrivateUse)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ScriptSubtag"/> class.
		/// </summary>
		/// <param name="subtag">The subtag.</param>
		/// <param name="name">The name.</param>
		public ScriptSubtag(ScriptSubtag subtag, string name)
			: this(subtag.Code, name, subtag.IsPrivateUse)
		{
		}

		/// <summary>
		/// Gets a value indicating whether the code is valid.
		/// </summary>
		/// <value><c>true</c> if the code is valid; otherwise, <c>false</c>.</value>
		public override bool IsValid
		{
			get
			{
				return LangTagUtils.IsScriptCodeValid(Code);
			}
		}

		private static HashSet<string> s_standardScripts;

		/// <summary>
		/// Should give the same answer as StandardTags.IsValidIso15924ScriptCode, but more efficiently.
		/// </summary>
		public static bool IsValidIso15924ScriptCode(string script)
		{
			if (s_standardScripts == null)
			{
				var standardScripts =
					new HashSet<string>(from tag in StandardTags.ValidIso15924Scripts select tag.Code.ToLowerInvariant());
				s_standardScripts = standardScripts; // if two threads attempt this, one should succeed with a correctly initialized dictionary.
			}
			return s_standardScripts.Contains(script.ToLowerInvariant());
		}
	}
	#endregion

	#region RegionSubtag class
	/// <summary>
	/// This class represents a region from the IANA language subtag registry.
	/// </summary>
	public class RegionSubtag : Subtag
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RegionSubtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		public RegionSubtag(string code, string name, bool isPrivateUse)
			: base(code, name, isPrivateUse)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:RegionSubtag"/> class.
		/// </summary>
		/// <param name="subtag">The subtag.</param>
		/// <param name="name">The name.</param>
		public RegionSubtag(RegionSubtag subtag, string name)
			: this(subtag.Code, name, subtag.IsPrivateUse)
		{
		}

		/// <summary>
		/// Gets a value indicating whether the code is valid.
		/// </summary>
		/// <value><c>true</c> if the code is valid; otherwise, <c>false</c>.</value>
		public override bool IsValid
		{
			get
			{
				return LangTagUtils.IsRegionCodeValid(Code);
			}
		}

		private static HashSet<string> s_standardRegions;

		/// <summary>
		/// Should give the same answer as StandardTags.IsStandardIso3166Region, but more efficiently.
		/// </summary>
		public static bool IsStandardIso3166Region(string region)
		{
			if (s_standardRegions == null)
			{
				var standardRegions =
					new HashSet<string>(from tag in StandardTags.ValidIso3166Regions select tag.Subtag.ToLowerInvariant());
				s_standardRegions = standardRegions; // if two threads attempt this, one should succeed with a correctly initialized dictionary.
			}
			return s_standardRegions.Contains(region.ToLowerInvariant());
		}
		/// <summary>
		/// Should give the same answer as StandardTags.IsValidIso3166Region, but more efficiently.
		/// </summary>
		public static bool IsValidIso3166Region(string region)
		{
			return IsStandardIso3166Region (region) || StandardTags.IsPrivateUseRegionCode(region);
		}
	}
	#endregion

	#region VariantSubtag class
	/// <summary>
	/// This class represents a variant from the IANA language subtag registry.
	/// </summary>
	public class VariantSubtag : Subtag
	{
		private readonly HashSet<string> m_prefixes = new HashSet<string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="T:VariantSubtag"/> class.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="name">The name.</param>
		/// <param name="isPrivateUse">if set to <c>true</c> this is a private use subtag.</param>
		/// <param name="prefixes">The prefixes.</param>
		public VariantSubtag(string code, string name, bool isPrivateUse, IEnumerable<string> prefixes)
			: base(code, name, isPrivateUse)
		{
			if (prefixes != null)
				m_prefixes.UnionWith(prefixes);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:VariantSubtag"/> class.
		/// </summary>
		/// <param name="subtag">The subtag.</param>
		/// <param name="name">The name.</param>
		public VariantSubtag(VariantSubtag subtag, string name)
			: this(subtag.Code, name, subtag.IsPrivateUse, subtag.Prefixes)
		{
		}

		/// <summary>
		/// When Language is "qaa" we embed the language name as the first private-use element (after any initial x- or embedded -x-) in the variant.
		/// Extract this and return it.
		/// </summary>
		public static string ExtractLanguageCode(string variant)
		{
			if (variant == null)
				return ""; // defensive
			var result = variant;
			if (variant.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
			{
				result = result.Substring(2);
			}
			else
			{
				int index = variant.IndexOf("-x-", StringComparison.OrdinalIgnoreCase);
				if (index == -1)
					return "qaa"; // we don't seem to have an embedded language code, use our global default.
				result = variant.Substring(index + 3);
			}
			int endOfLanguage = result.IndexOf("-", StringComparison.Ordinal);
			if (endOfLanguage == -1)
				return result;
			return result.Substring(0, endOfLanguage);
		}

		/// <summary>
		/// Assuming this is the tag of a writing system with language qaa, and therefore the first
		/// private item in the variant is our language ID, return what the code would be without it.
		/// </summary>
		public string CodeWithoutPrivateLanguageName
		{
			get
			{
				if (!IsPrivateUse)
					return Code; // no private use part to remove language from
				var code = Code;
				return RemovePrivateLanguageName(code);
			}
		}

		/// <summary>
		/// Assuming the input is a variant which is or contains a private language name, remove it and return the rest.
		/// Return an empty string if there is nothing else.
		/// </summary>
		internal string RemovePrivateLanguageName(string code)
		{
			int start = code.IndexOf("-x-", StringComparison.OrdinalIgnoreCase);
			if (start == -1)
				return ""; // whole code is private language name, without it we have nothing.
			var end = code.IndexOf("-", start + 3, StringComparison.OrdinalIgnoreCase);
			if (end == -1)
				return code.Substring(0, start);
			return code.Substring(0, start + 3) + code.Substring(end + 1);
		}

		/// <summary>
		/// Return what ToString() would, except that if it is returning the code, we want it without
		/// the embedded private language name. If we know a nicer name for that return it.
		/// </summary>
		public string DisplayNameWithoutPrivateLanguageName
		{
			get {
				if (string.IsNullOrEmpty(Name) || Name == Code)
				{
					var realVariantCode = CodeWithoutPrivateLanguageName;
					if (string.IsNullOrEmpty(realVariantCode))
						return "";
					return LangTagUtils.GetVariantSubtag(realVariantCode).ToString();
				}
				return Name;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the code is valid.
		/// </summary>
		/// <value><c>true</c> if the code is valid; otherwise, <c>false</c>.</value>
		public override bool IsValid
		{
			get
			{
				return LangTagUtils.IsVariantCodeValid(Code);
			}
		}

		/// <summary>
		/// Determines whether this subtag can be used with the specified lang tag.
		/// </summary>
		/// <param name="langTag">The lang tag.</param>
		/// <returns>
		///		<c>true</c> if this subtag can be used with the specified lang tag, otherwise <c>false</c>.
		/// </returns>
		public bool IsVariantOf(string langTag)
		{
			if (m_prefixes.Count == 0)
				return true;

			return m_prefixes.Any(langTag.StartsWith);
		}

		/// <summary>
		/// True if the variant needs a leading x- to indicate that it is private-use (if nothing earlier
		/// in the ID supplied it).
		/// </summary>
		public bool NeedsPrivateUseMarker
		{
			get
			{
				if (!IsPrivateUse)
					return false;
				if (Code.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
					return false;
				if (Code.IndexOf("-x-", StringComparison.OrdinalIgnoreCase) != -1)
					return false;
				return true;
			}
		}

		/// <summary>
		/// Gets the prefixes.
		/// </summary>
		/// <value>The prefixes.</value>
		public IEnumerable<string> Prefixes
		{
			get
			{
				return m_prefixes;
			}
		}
	}
	#endregion

	#region LangTagUtils class
	/// <summary>
	/// This static utility class contains various methods for processing RFC-5646 language tags. The methods
	/// defined in this class can currently only support language tags with a single variant subtag and no
	/// extensions.
	/// </summary>
	public static class LangTagUtils
	{
		private const string PrivateUseExpr = "[xX](-[a-zA-Z0-9]{1,40})+";
		// according to RFC-5646, a primary language subtag can be anywhere from 2 to 8 characters in length,
		// at this point only ISO 639 codes are allowed, which are all 2 to 3 characters in length, so we
		// use the more practical constraint of 2 to 3 characters, which allows private use ICU locales with
		// only a language defined (i.e. "xkal") to not match the regex.
		private const string LanguageExpr = "[a-zA-Z]{2,8}(-[a-zA-Z]{3}){0,3}";
		private const string ScriptExpr = "[a-zA-Z]{4}";
		private const string RegionExpr = "[a-zA-Z]{2}|[0-9]{3}";
		private const string VariantSubExpr = "[0-9][a-zA-Z0-9]{3}|[a-zA-Z0-9]{5,8}";
		private const string VariantExpr = VariantSubExpr + "(-" + VariantSubExpr + ")*";
		private const string ExtensionExpr = "[a-wyzA-WYZ](-([a-zA-Z0-9]{2,8})+)+";
		// We are more lenient with variant subtags because legacy projects might contain
		// poorly-formed codes. We have to do something with the unknown portion of private
		// use subtags that result, so we force it to be the variant portion of the lang tag, thus
		// the need for more relaxed rules for variant codes.
		private const string FuzzyVariantSubExpr = "[a-zA-Z0-9]{1,40}";
		private const string FuzzyVariantExpr = FuzzyVariantSubExpr + "(-" + FuzzyVariantSubExpr + ")*";

		private const string IcuTagExpr = "(\\A(?'privateuse'" + PrivateUseExpr + ")\\z)"
			+ "|(\\A(?'language'" + LanguageExpr + ")"
			+ "(-(?'script'" + ScriptExpr + "))?"
			+ "(-(?'region'" + RegionExpr + "))?"
			+ "(-(?'variant'" + VariantExpr + "))?"
			+ "(-(?'extension'" + ExtensionExpr + "))?"
			+ "(-(?'privateuse'" + PrivateUseExpr + "))?\\z)";

		private const string LangTagExpr =  "(\\A(?'language'" + LanguageExpr + ")"
			+ "(-(?'script'" + ScriptExpr + "))?"
			+ "(-(?'region'" + RegionExpr + "))?"
			+ "(-(?'variant'" + VariantExpr + "))?"
			+ "(-(?'extension'" + ExtensionExpr + "))?"
			+ "(-(?'privateuse'" + PrivateUseExpr + "))?\\z)";

		private static readonly Regex s_icuTagPattern;
		private static readonly Regex s_langTagPattern;
		private static readonly Regex s_langPattern;
		private static readonly Regex s_scriptPattern;
		private static readonly Regex s_regionPattern;
		private static readonly Regex s_variantPattern;

		private static readonly Dictionary<string, LanguageSubtag> s_languageSubtags;
		private static readonly Dictionary<string, ScriptSubtag> s_scriptSubtags;
		private static readonly Dictionary<string, RegionSubtag> s_regionSubtags;
		private static readonly Dictionary<string, VariantSubtag> s_variantSubtags;

		static LangTagUtils()
		{
			s_icuTagPattern = new Regex(IcuTagExpr, RegexOptions.ExplicitCapture);
			s_langTagPattern = new Regex(LangTagExpr, RegexOptions.ExplicitCapture);
			s_langPattern = new Regex("\\A(" + LanguageExpr + ")\\z", RegexOptions.ExplicitCapture);
			s_scriptPattern = new Regex("\\A(" + ScriptExpr + ")\\z", RegexOptions.ExplicitCapture);
			s_regionPattern = new Regex("\\A(" + RegionExpr + ")\\z", RegexOptions.ExplicitCapture);
			s_variantPattern = new Regex("\\A(" + FuzzyVariantExpr + ")\\z", RegexOptions.ExplicitCapture);

			s_languageSubtags = new Dictionary<string, LanguageSubtag>();
			foreach (var langCode in StandardTags.ValidIso639LanguageCodes)
			{
				var code = langCode.Code;
				switch (code)
				{
					// ISO3Code is now only set when it differs from Code.
					case "cmn":
						code = "zh";
						langCode.ISO3Code = "cmn";
						break;
					case "pes":
						code = "fa";
						langCode.ISO3Code = "pes";
						break;
					case "arb":
						code = "ar";
						langCode.ISO3Code = "arb";
						break;
					case "zlm":
						code = "ms";
						langCode.ISO3Code = "zlm";
						break;
				}
				var languageSubtag = new LanguageSubtag(code, langCode.Name, false, langCode.ISO3Code);
				s_languageSubtags[languageSubtag.Code] = languageSubtag;
				if (!string.IsNullOrEmpty(languageSubtag.ISO3Code))
					s_languageSubtags[languageSubtag.ISO3Code] = languageSubtag;
			}

			s_scriptSubtags = new Dictionary<string, ScriptSubtag>();
			foreach (var scriptCode in StandardTags.ValidIso15924Scripts)
				s_scriptSubtags[scriptCode.Code] = new ScriptSubtag(scriptCode.Code, scriptCode.Label, false);

			s_regionSubtags = new Dictionary<string, RegionSubtag>();
			foreach (var regionCode in StandardTags.ValidIso3166Regions)
				s_regionSubtags[regionCode.Subtag] = new RegionSubtag(regionCode.Subtag, regionCode.Description, false);

			s_variantSubtags = new Dictionary<string, VariantSubtag>();
			foreach (var variantCode in StandardTags.ValidRegisteredVariants)
				s_variantSubtags[variantCode.Subtag] = new VariantSubtag(variantCode.Subtag, variantCode.Description,
					false, variantCode.Prefixes);
			// These ones are considered non-private in that the user can't edit the code, but they already contain needed X's.
			s_variantSubtags["fonipa-x-etic"] = new VariantSubtag("fonipa-x-etic", "Phonetic", false, null);
			s_variantSubtags["fonipa-x-emic"] = new VariantSubtag("fonipa-x-emic", "Phonemic", false, null);
			s_variantSubtags["x-py"] = new VariantSubtag("x-py", "Pinyin", false, null);
			s_variantSubtags["x-pyn"] = new VariantSubtag("x-pyn", "Pinyin Numbered", false, null);
			s_variantSubtags["x-audio"] = new VariantSubtag("x-audio", "Audio", false, null);
		}

		/// <summary>
		/// Gets all valid language subtags.
		/// </summary>
		/// <value>The language subtags.</value>
		public static IEnumerable<LanguageSubtag> LanguageSubtags
		{
			get
			{
				return s_languageSubtags.Values.Distinct();
			}
		}

		/// <summary>
		/// Gets all valid script subtags.
		/// </summary>
		/// <value>The script subtags.</value>
		public static IEnumerable<ScriptSubtag> ScriptSubtags
		{
			get
			{
				return s_scriptSubtags.Values;
			}
		}

		/// <summary>
		/// Gets all valid region subtags.
		/// </summary>
		/// <value>The region subtags.</value>
		public static IEnumerable<RegionSubtag> RegionSubtags
		{
			get
			{
				return s_regionSubtags.Values;
			}
		}

		/// <summary>
		/// Gets all valid variant subtags.
		/// </summary>
		/// <value>The variant subtags.</value>
		public static IEnumerable<VariantSubtag> VariantSubtags
		{
			get
			{
				return s_variantSubtags.Values;
			}
		}

		/// <summary>
		/// Gets the language subtag with the specified code.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <returns></returns>
		public static LanguageSubtag GetLanguageSubtag(string code)
		{
			if (string.IsNullOrEmpty(code))
				throw new ArgumentNullException("code");

			LanguageSubtag subtag;
			if (!s_languageSubtags.TryGetValue(code, out subtag))
				subtag = new LanguageSubtag(code, null, true, null);
			return subtag;
		}

		/// <summary>
		/// Gets the script subtag with the specified code.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <returns></returns>
		public static ScriptSubtag GetScriptSubtag(string code)
		{
			return GetScriptSubtag(code, null);
		}

		/// <summary>
		/// Gets the script subtag with the specified code. If it is not a known code, returns a private-use tag
		/// with the specified name and code; if it is known, the supplied name is ignored.
		/// </summary>
		public static ScriptSubtag GetScriptSubtag(string code, string name)
		{
			if (string.IsNullOrEmpty(code))
				throw new ArgumentNullException("code");

			ScriptSubtag subtag;
			if (!s_scriptSubtags.TryGetValue(code, out subtag))
				subtag = new ScriptSubtag(code, name, true);
			return subtag;
		}

		/// <summary>
		/// Gets the region subtag with the specified code.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <returns></returns>
		public static RegionSubtag GetRegionSubtag(string code)
		{
			return GetRegionSubtag(code, null);
		}

		/// <summary>
		/// Get a region subtag with the specified code and name. If it is a standard region,
		/// the name passed in will be ignored, and the standard name used. If not, it will have the
		/// specified name and code, and be marked private use.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static RegionSubtag GetRegionSubtag(string code, string name)
		{
			if (string.IsNullOrEmpty(code))
				throw new ArgumentNullException("code");

			RegionSubtag subtag;
			if (!s_regionSubtags.TryGetValue(code, out subtag))
				subtag = new RegionSubtag(code, name, true);
			return subtag;

		}

		/// <summary>
		/// Gets the variant subtag with the specified code.
		/// </summary>
		public static VariantSubtag GetVariantSubtag(string code)
		{
			return GetVariantSubtag(code, null, null);
		}

		/// <summary>
		/// Gets the variant subtag with the specified code. If it is not a known (non-private-use) one,
		/// make a private-use one with the specified values. Insert 'x' or move it earlier
		/// if any leading parts are not standard.
		/// </summary>
		public static VariantSubtag GetVariantSubtag(string code, string name, IEnumerable<string> prefixes)
		{
			if (string.IsNullOrEmpty(code))
				throw new ArgumentNullException("code");

			var parts = code.Split('-').Where(part => part.Length > 0);
			var fixedParts = new List<string>();
			bool gotX = false;
			foreach (var part in parts)
			{
				if (part.Equals("x", StringComparison.OrdinalIgnoreCase))
				{
					if (gotX)
						continue; // no duplicate x.
					fixedParts.Add(part);
					gotX = true;
					continue;
				}
				if (gotX)
				{
					fixedParts.Add(part); // copy the rest unchanged.
					continue;
				}
				if (!StandardTags.IsValidRegisteredVariant(part))
				{
					fixedParts.Add("x");
					gotX = true;
				}
				fixedParts.Add(part); // copy the rest unchanged.
			}
			var code2 = fixedParts.Aggregate((partialCode, part) => partialCode + "-" + part);
			VariantSubtag subtag;
			if (!s_variantSubtags.TryGetValue(code2, out subtag))
				subtag = new VariantSubtag(code2, name, true, prefixes);
			return subtag;
		}

		/// <summary>
		/// Determines whether the specified language code is valid.
		/// </summary>
		/// <param name="code">The language code.</param>
		/// <returns>
		/// 	<c>true</c> if the language code is valid; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsLanguageCodeValid(string code)
		{
			return s_langPattern.IsMatch(code);
		}

		/// <summary>
		/// Determines whether the specified script code is valid.
		/// </summary>
		/// <param name="code">The script code.</param>
		/// <returns>
		/// 	<c>true</c> if the script code is valid; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsScriptCodeValid(string code)
		{
			return s_scriptPattern.IsMatch(code);
		}

		/// <summary>
		/// Determines whether the specified region code is valid.
		/// </summary>
		/// <param name="code">The region code.</param>
		/// <returns>
		/// 	<c>true</c> if the region code is valid; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsRegionCodeValid(string code)
		{
			return s_regionPattern.IsMatch(code);
		}

		/// <summary>
		/// Determines whether the specified variant code is valid.
		/// </summary>
		/// <param name="code">The variant code.</param>
		/// <returns>
		/// 	<c>true</c> if the variant code is valid; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsVariantCodeValid(string code)
		{
			return s_variantPattern.IsMatch(code);
		}

		/// <summary>
		/// Converts the specified ICU locale to a language tag. If the ICU locale is already a valid
		/// language tag, it will return it.
		/// </summary>
		/// <param name="icuLocale">The ICU locale.</param>
		/// <returns></returns>
		public static string ToLangTag(string icuLocale)
		{
			if (string.IsNullOrEmpty(icuLocale))
				throw new ArgumentNullException("icuLocale");

			if (icuLocale.Contains("-"))
			{
				Match match = s_icuTagPattern.Match(icuLocale);
				if (match.Success)
				{
					// We need to check for mixed case in the language code portion.  This has been
					// observed in user data, and causes crashes later on.  See LT-11288.
					var rgs = icuLocale.Split('-');
					if (rgs[0].ToLowerInvariant() == rgs[0])
						return icuLocale;
					var bldr = new StringBuilder();
					bldr.Append(rgs[0].ToLowerInvariant());
					for (var i = 1; i < rgs.Length; ++i)
					{
						bldr.Append("-");
						bldr.Append(rgs[i].ToLowerInvariant());
					}
					icuLocale = bldr.ToString();
				}
			}

			Icu.UErrorCode err;
			string icuLanguageCode;
			string languageCode;
			Icu.GetLanguageCode(icuLocale, out icuLanguageCode, out err);
			if (icuLanguageCode.Length == 4 && icuLanguageCode.StartsWith("x"))
				languageCode = icuLanguageCode.Substring(1);
			else
				languageCode = icuLanguageCode;
			// Some very old projects may have codes with over-long identifiers. In desperation we truncate these.
			// 4-letter codes starting with 'e' are a special case.
			if (languageCode.Length > 3 && !(languageCode.Length == 4 && languageCode.StartsWith("e")))
				languageCode = languageCode.Substring(0, 3);
			// The ICU locale strings in FW 6.0 allowed numbers in the language tag.  The
			// standard doesn't allow this. Map numbers to letters deterministically, even
			// though the resulting code may have no relation to reality.  (It may be a valid
			// ISO 639-3 language code that is assigned to a totally unrelated language.)
			if (languageCode.Contains('0'))
				languageCode = languageCode.Replace('0', 'a');
			if (languageCode.Contains('1'))
				languageCode = languageCode.Replace('1', 'b');
			if (languageCode.Contains('2'))
				languageCode = languageCode.Replace('2', 'c');
			if (languageCode.Contains('3'))
				languageCode = languageCode.Replace('3', 'd');
			if (languageCode.Contains('4'))
				languageCode = languageCode.Replace('4', 'e');
			if (languageCode.Contains('5'))
				languageCode = languageCode.Replace('5', 'f');
			if (languageCode.Contains('6'))
				languageCode = languageCode.Replace('6', 'g');
			if (languageCode.Contains('7'))
				languageCode = languageCode.Replace('7', 'h');
			if (languageCode.Contains('8'))
				languageCode = languageCode.Replace('8', 'i');
			if (languageCode.Contains('9'))
				languageCode = languageCode.Replace('9', 'j');
			LanguageSubtag languageSubtag;
			if (languageCode == icuLanguageCode)
			{
				languageSubtag = GetLanguageSubtag(
					(languageCode.Length == 4 && languageCode.StartsWith("e")) ?
					languageCode.Substring(1) : languageCode);
			}
			else
			{
				languageSubtag = new LanguageSubtag(languageCode, null, true, null);
			}
			if (icuLanguageCode == icuLocale)
				return ToLangTag(languageSubtag, null, null, null);

			string scriptCode;
			Icu.GetScriptCode(icuLocale, out scriptCode, out err);
			ScriptSubtag scriptSubtag = null;
			if (!string.IsNullOrEmpty(scriptCode))
				scriptSubtag = GetScriptSubtag(scriptCode);

			string regionCode;
			Icu.GetCountryCode(icuLocale, out regionCode, out err);
			RegionSubtag regionSubtag = null;
			if (!string.IsNullOrEmpty(regionCode))
				regionSubtag = GetRegionSubtag(regionCode);

			string variantCode;
			Icu.GetVariantCode(icuLocale, out variantCode, out err);
			VariantSubtag variantSubtag = null;
			if (!string.IsNullOrEmpty(variantCode))
			{
				variantCode = TranslateVariantCode(variantCode, code => {
						string[] pieces = variantCode.Split(new [] { '_' }, StringSplitOptions.RemoveEmptyEntries);
						return pieces.ToString("-", item => TranslateVariantCode(item, subItem => subItem.ToLowerInvariant()));
					});
				variantSubtag = GetVariantSubtag(variantCode);
			}

			return ToLangTag(languageSubtag, scriptSubtag, regionSubtag, variantSubtag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Translates standard variant codes to their expanded (semi-human-readable) format;
		/// all others are translated using the given function.
		/// </summary>
		/// <param name="variantCode">The variant code.</param>
		/// <param name="defaultFunc">The default translation function.</param>
		/// ------------------------------------------------------------------------------------
		private static string TranslateVariantCode(string variantCode, Func<string, string> defaultFunc)
		{
			switch (variantCode)
			{
				case "IPA": return "fonipa";
				case "X_ETIC": return "fonipa-x-etic";
				case "X_EMIC":
				case "EMC": return "fonipa-x-emic";
				case "X_PY":
				case "PY": return "pinyin";
				default: return defaultFunc(variantCode);
			}
		}

		/// <summary>
		/// Standard way to split a Variant into the standard-variant part (before any -x-, or empty if it starts with x-)
		/// and the private-use part (after the x part, or empty if there is no x). Returns the private use part.
		/// </summary>
		public static string GetPrivateUseAndStandardVariant(string wholeVariant, out string standardVariant)
		{
			if (wholeVariant.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
			{
				wholeVariant = wholeVariant.Substring(2);
				standardVariant = "";
			}
			else
			{
				int start = wholeVariant.IndexOf("-x-", StringComparison.OrdinalIgnoreCase);
				if (start == -1)
				{
					standardVariant = wholeVariant; // it's all (standard) variant.
					wholeVariant = "";
				}
				else
				{
					standardVariant = wholeVariant.Substring(0, start);
					wholeVariant = wholeVariant.Substring(start + 3);
				}
			}
			return wholeVariant;
		}

		/// <summary>
		/// Generates a language tag from the specified subtags.
		/// </summary>
		/// <param name="languageSubtag">The language subtag.</param>
		/// <param name="scriptSubtag">The script subtag.</param>
		/// <param name="regionSubtag">The region subtag.</param>
		/// <param name="variantSubtag">The variant subtag.</param>
		/// <returns></returns>
		public static string ToLangTag(LanguageSubtag languageSubtag, ScriptSubtag scriptSubtag, RegionSubtag regionSubtag, VariantSubtag variantSubtag)
		{
			if (languageSubtag == null)
				throw new ArgumentNullException("languageSubtag");

			var sb = new StringBuilder();

			// Insert non-custom language, script, region into main part of code.
			if (languageSubtag.IsPrivateUse)
			{
				sb.Append("qaa");
			}
			else
			{
				sb.Append(languageSubtag.Code);
			}

			var isCustomScript = false;
			if (scriptSubtag != null)
			{
				sb.Append("-");
				// Qaaa is our flag to expect a script in private-use. If the actual value is Qaaa, we need to treat it as custom,
				// so we don't confuse some other private-use tag with a custom script.
				isCustomScript = TreatAsCustomScript(scriptSubtag.Code);
				if (isCustomScript)
					sb.Append("Qaaa");
				else
					sb.Append(scriptSubtag.Code);
			}

			var isCustomRegion = false;
			if (regionSubtag != null)
			{
				sb.Append("-");
				// QM is our flag to expect a region in private-use. If the actual value is QM, we need to treat it as custom,
				// so we don't confuse some other private-use tag with a custom region.
				isCustomRegion = TreatAsCustomRegion(regionSubtag.Code);
				if (isCustomRegion)
					sb.Append("QM");
				else
					sb.Append(regionSubtag.Code);
			}
			string standardVariant = "";
			string privateUse = "";
			if (variantSubtag != null)
				privateUse = GetPrivateUseAndStandardVariant(variantSubtag.Code, out standardVariant);
			if (standardVariant != "")
			{
				sb.Append("-");
				sb.Append(standardVariant);
			}

			// Insert custom language, script, or variant into private=use.
			bool inPrivateUse = false;
			if (languageSubtag.IsPrivateUse)
			{
				sb.Append("-");
				if (!inPrivateUse)
				{
					inPrivateUse = true;
					sb.Append("x-");
				}
				sb.Append(languageSubtag.Code);
			}
			if (isCustomScript)
			{
				sb.Append("-");
				if (!inPrivateUse)
				{
					inPrivateUse = true;
					sb.Append("x-");
				}
				sb.Append(scriptSubtag.Code);
			}
			if (isCustomRegion)
			{
				sb.Append("-");
				if (!inPrivateUse)
				{
					inPrivateUse = true;
					sb.Append("x-");
				}
				sb.Append(regionSubtag.Code);
			}
			else if (languageSubtag.Code == "zh" && languageSubtag.ISO3Code == "cmn" &&
			regionSubtag == null)
			{
				sb.Append("-CN");
			}

			if (privateUse != "")
			{
				sb.Append("-");
				if (!inPrivateUse)
					sb.Append("x-");
				sb.Append(privateUse);
			}

			return sb.ToString();
		}

		internal static bool TreatAsCustomRegion(string regionCode)
		{
			return regionCode.Equals("QM", StringComparison.OrdinalIgnoreCase) || !RegionSubtag.IsValidIso3166Region(regionCode);
		}
		internal static bool TreatAsCustomScript(string scriptCode)
		{
			return scriptCode.Equals("Qaaa", StringComparison.OrdinalIgnoreCase) || !ScriptSubtag.IsValidIso15924ScriptCode(scriptCode);
		}
		/// <summary>
		/// Generates a language tag from the specified codes.
		/// </summary>
		/// <param name="languageCode">The language code.</param>
		/// <param name="scriptCode">The script code.</param>
		/// <param name="regionCode">The region code.</param>
		/// <param name="variantCode">The variant code.</param>
		/// <returns></returns>
		public static string ToLangTag(string languageCode, string scriptCode, string regionCode, string variantCode)
		{
			if (string.IsNullOrEmpty(languageCode))
				throw new ArgumentNullException("languageCode");

			return ToLangTag(GetLanguageSubtag(languageCode), string.IsNullOrEmpty(scriptCode) ? null : GetScriptSubtag(scriptCode),
				string.IsNullOrEmpty(regionCode) ? null : GetRegionSubtag(regionCode),
				string.IsNullOrEmpty(variantCode) ? null : GetVariantSubtag(variantCode));
		}

		/// <summary>
		/// Determines whether the specified region code is private use.
		/// </summary>
		/// <param name="regionCode">The region code.</param>
		/// <returns>
		/// 	<c>true</c> if the region code is private use.
		/// </returns>
		public static bool IsPrivateUseRegionCode(string regionCode)
		{
			return regionCode == "AA" || regionCode == "ZZ"
				|| (regionCode.CompareTo("QM") >= 0 && regionCode.CompareTo("QZ") <= 0)
				|| (regionCode.CompareTo("XA") >= 0 && regionCode.CompareTo("XZ") <= 0);
		}

		/// <summary>
		/// Converts the specified language tag to an ICU locale.
		/// </summary>
		/// <param name="langTag">The language tag.</param>
		/// <returns></returns>
		public static string ToIcuLocale(string langTag)
		{
			LanguageSubtag languageSubtag;
			ScriptSubtag scriptSubtag;
			RegionSubtag regionSubtag;
			VariantSubtag variantSubtag;
			if (!GetSubtags(langTag, out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag))
				throw new ArgumentException("langTag is not a valid RFC5646 language tag.", "langTag");
			return ToIcuLocale(languageSubtag, scriptSubtag, regionSubtag, variantSubtag);
		}

		/// <summary>
		/// Generates an ICU locale from the specified language tag subtags.
		/// </summary>
		/// <param name="languageSubtag">The language subtag.</param>
		/// <param name="scriptSubtag">The script subtag.</param>
		/// <param name="regionSubtag">The region subtag.</param>
		/// <param name="variantSubtag">The variant subtag.</param>
		/// <returns></returns>
		public static string ToIcuLocale(LanguageSubtag languageSubtag, ScriptSubtag scriptSubtag, RegionSubtag regionSubtag, VariantSubtag variantSubtag)
		{
			if (languageSubtag == null)
				throw new ArgumentNullException("languageSubtag");

			var sb = new StringBuilder();
			//start with the LanguageCode
			if (languageSubtag.IsPrivateUse)
				sb.Append("x");
			sb.Append(languageSubtag.Code);

			//now add the Script if it exists
			if (scriptSubtag != null)
				sb.AppendFormat("_{0}", scriptSubtag.Code);

			//now add the Region if it exists
			if (regionSubtag != null)
				sb.AppendFormat("_{0}", regionSubtag.Code);

			// if variantCode is notNullofEmpty then add it
			// and if CountryCode is empty add two underscores instead of one.
			if (variantSubtag != null)
			{
				string icuVariant = null;
				// convert language tag variants to known ICU variants
				// TODO: are there any more ICU variants?
				switch (variantSubtag.Code)
				{
					case "fonipa":
						icuVariant = "IPA";
						break;

					case "fonipa-x-etic":
						icuVariant = "X_ETIC";
						break;

					case "fonipa-x-emic":
						icuVariant = "X_EMIC";
						break;

					case "pinyin":
						icuVariant = "X_PY";
						break;
				}
				if (!string.IsNullOrEmpty(icuVariant))
					sb.AppendFormat(regionSubtag == null ? "__{0}" : "_{0}", icuVariant);

			}
			return sb.ToString();
		}

		/// <summary>
		/// Generates an ICU locale from the specified language tag codes.
		/// </summary>
		/// <param name="languageCode">The language code.</param>
		/// <param name="scriptCode">The script code.</param>
		/// <param name="regionCode">The region code.</param>
		/// <param name="variantCode">The variant code.</param>
		/// <returns></returns>
		public static string ToIcuLocale(string languageCode, string scriptCode, string regionCode, string variantCode)
		{
			if (string.IsNullOrEmpty(languageCode))
				throw new ArgumentNullException("languageCode");

			return ToIcuLocale(GetLanguageSubtag(languageCode), string.IsNullOrEmpty(scriptCode) ? null : GetScriptSubtag(scriptCode),
				string.IsNullOrEmpty(regionCode) ? null : GetRegionSubtag(regionCode),
				string.IsNullOrEmpty(variantCode) ? null : GetVariantSubtag(variantCode));
		}

		/// <summary>
		/// Gets the codes of the specified language tag.
		/// </summary>
		/// <param name="langTag">The lang tag.</param>
		/// <param name="languageCode">The language code.</param>
		/// <param name="scriptCode">The script code.</param>
		/// <param name="regionCode">The region code.</param>
		/// <param name="variantCode">The variant code.</param>
		/// <returns></returns>
		public static bool GetCodes(string langTag, out string languageCode, out string scriptCode, out string regionCode,
			out string variantCode)
		{
			languageCode = null;
			scriptCode = null;
			regionCode = null;
			variantCode = null;

			LanguageSubtag languageSubtag;
			ScriptSubtag scriptSubtag;
			RegionSubtag regionSubtag;
			VariantSubtag variantSubtag;
			if (!GetSubtags(langTag, out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag))
				return false;

			languageCode = languageSubtag.Code;
			if (scriptSubtag != null)
				scriptCode = scriptSubtag.Code;
			if (regionSubtag != null)
				regionCode = regionSubtag.Code;
			if (variantSubtag != null)
				variantCode = variantSubtag.Code;
			return true;
		}

		/// <summary>
		/// Gets the subtags of the specified language tag.
		/// </summary>
		/// <param name="langTag">The language tag.</param>
		/// <param name="languageSubtag">The language subtag.</param>
		/// <param name="scriptSubtag">The script subtag.</param>
		/// <param name="regionSubtag">The region subtag.</param>
		/// <param name="variantSubtag">The variant subtag.</param>
		/// <returns></returns>
		public static bool GetSubtags(string langTag, out LanguageSubtag languageSubtag, out ScriptSubtag scriptSubtag,
			out RegionSubtag regionSubtag, out VariantSubtag variantSubtag)
		{
			if (string.IsNullOrEmpty(langTag))
				throw new ArgumentNullException("langTag");

			languageSubtag = null;
			scriptSubtag = null;
			regionSubtag = null;
			variantSubtag = null;

			if (langTag.Any(c => !Char.IsLetterOrDigit(c) && c != '-'))
			{
				return false;
			}

			var cleaner = new Palaso.WritingSystems.Migration.Rfc5646TagCleaner(langTag);
			cleaner.Clean();

			List<string> privateUseSubTags =
				new List<string>(cleaner.PrivateUse.Split(new char[] {'-'}, StringSplitOptions.RemoveEmptyEntries));
			int privateUseSubTagIndex = 0;
			bool privateUsePrefix = false;
			string privateUseSubTag = null;
			int part = -1;
			string languageCode = cleaner.Language;
			if (string.IsNullOrEmpty(languageCode))
				return false;
			if (languageCode.Equals("qaa", StringComparison.OrdinalIgnoreCase))
			{
				if (privateUseSubTags.Count > 0)
				{
					languageSubtag = new LanguageSubtag(privateUseSubTags[0], "", true, null);
					privateUseSubTags.RemoveAt(0);
				}
				else
					languageSubtag = GetLanguageSubtag("qaa"); // We do allow just plain qaa.
			}
			else
			{
				languageSubtag = GetLanguageSubtag(languageCode);
			}

			string scriptCode = cleaner.Script;
			if (!string.IsNullOrEmpty(scriptCode))
			{
				if (scriptCode.Equals("Qaaa", StringComparison.OrdinalIgnoreCase) && privateUseSubTags.Count > 0)
				{
					scriptSubtag = new ScriptSubtag(privateUseSubTags[0], "", true);
					privateUseSubTags.RemoveAt(0);
				}
				else
					scriptSubtag = GetScriptSubtag(scriptCode);
			}

			string regionCode = cleaner.Region;
			if (!string.IsNullOrEmpty(regionCode))
			{
				if (regionCode.Equals("QM", StringComparison.OrdinalIgnoreCase) && privateUseSubTags.Count > 0)
				{
					regionSubtag = new RegionSubtag(privateUseSubTags[0], "", true);
					privateUseSubTags.RemoveAt(0);
				}
				else
					regionSubtag = GetRegionSubtag(regionCode);
			}

			var variantSb = new StringBuilder();
			string variantCode = cleaner.Variant;
			if (!string.IsNullOrEmpty(variantCode))
			{
				variantSb.Append(variantCode);
			}
			if (privateUseSubTags.Count > 0)
			{
				if (variantSb.Length > 0)
					variantSb.Append("-");
				variantSb.Append("x");
				foreach (var item in privateUseSubTags)
				{
					variantSb.Append("-");
					variantSb.Append(item);
				}
			}

			variantCode = variantSb.ToString();
			if (!string.IsNullOrEmpty(variantCode))
			{
				variantSubtag = GetVariantSubtag(variantCode);
			}
			return true;
		}

		private static string NextSubTag(string[] subTags, ref int subTagIndex, out bool privateUsePrefix)
		{
			privateUsePrefix = false;
			if (subTagIndex < 0 || subTagIndex >= subTags.Length)
				return null;

			if (subTags[subTagIndex].ToLowerInvariant() == "x")
			{
				privateUsePrefix = true;
				subTagIndex++;
			}
			return subTags[subTagIndex++];
		}

		/// <summary>
		/// Determines whether the specified language tag is valid.
		/// </summary>
		/// <param name="langTag">The language tag.</param>
		/// <returns>
		/// 	<c>true</c> if the specified language tag is valid; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsValid(string langTag)
		{
			LanguageSubtag languageSubtag;
			ScriptSubtag scriptSubtag;
			RegionSubtag regionSubtag;
			VariantSubtag variantSubtag;
			return GetSubtags(langTag, out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag);
		}
	}
	#endregion
}
