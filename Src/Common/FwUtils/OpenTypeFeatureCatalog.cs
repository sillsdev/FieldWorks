// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>The default applied state of an OpenType feature in typical shaping.</summary>
	public enum OpenTypeFeatureDefaultState
	{
		/// <summary>No documented default; treated as off until the user selects a value.</summary>
		Unspecified,
		/// <summary>Off unless the user turns it on.</summary>
		Off,
		/// <summary>Applied automatically by shaping engines unless the user turns it off.</summary>
		On
	}

	/// <summary>Registered-feature classification: friendly name, visibility, and default state.</summary>
	public sealed class OpenTypeFeatureCatalogEntry
	{
		/// <summary>Initializes a new instance of the <see cref="OpenTypeFeatureCatalogEntry"/> class.</summary>
		public OpenTypeFeatureCatalogEntry(string englishName, bool isHidden, OpenTypeFeatureDefaultState defaultState)
		{
			EnglishName = englishName;
			IsHidden = isHidden;
			DefaultState = defaultState;
		}

		/// <summary>Gets the English friendly name; a resx entry may override it for the UI.</summary>
		public string EnglishName { get; }

		/// <summary>Gets whether the feature is required for shaping or otherwise not user-configurable.</summary>
		public bool IsHidden { get; }

		/// <summary>Gets the feature's default applied state.</summary>
		public OpenTypeFeatureDefaultState DefaultState { get; }
	}

	/// <summary>
	/// Catalog of registered OpenType features used to classify feature tags as hidden or
	/// user-configurable and to supply a default state and friendly name. Seeded from Paratext's
	/// RegisteredFeatureCatalog and audited against the OpenType feature registry: `dlig` is
	/// user-visible (Paratext hides it), `aalt` is hidden (a glyph palette, not a toggle), and
	/// `kern` is default-on. Stylistic sets (`ssXX`) and character variants (`cvXX`) are
	/// intentionally absent; their names come from the font and their visibility is handled by the
	/// UI. See LT-22638.
	/// </summary>
	public static class OpenTypeFeatureCatalog
	{
		private static OpenTypeFeatureCatalogEntry Visible(string name, OpenTypeFeatureDefaultState state = OpenTypeFeatureDefaultState.Unspecified)
		{
			return new OpenTypeFeatureCatalogEntry(name, false, state);
		}

		private static OpenTypeFeatureCatalogEntry Hidden(string name)
		{
			return new OpenTypeFeatureCatalogEntry(name, true, OpenTypeFeatureDefaultState.Unspecified);
		}

		private static readonly Dictionary<string, OpenTypeFeatureCatalogEntry> s_entries =
			new Dictionary<string, OpenTypeFeatureCatalogEntry>(StringComparer.Ordinal)
			{
				["aalt"] = Hidden("Access All Alternates"),
				["abvf"] = Hidden("Above Base Forms"),
				["abvm"] = Hidden("Above Base Mark"),
				["abvs"] = Hidden("Above Base Substitutions"),
				["afrc"] = Visible("Vertical Fractions", OpenTypeFeatureDefaultState.Off),
				["akhn"] = Hidden("Akhand"),
				["alig"] = Visible("Ancient Ligatures"),
				["blwf"] = Hidden("Below Base Forms"),
				["blwm"] = Hidden("Below Base Mark"),
				["blws"] = Hidden("Below Base Substitutions"),
				["c2pc"] = Visible("Capitals to Petite Capitals", OpenTypeFeatureDefaultState.Off),
				["c2sc"] = Visible("Capitals to Small Capitals", OpenTypeFeatureDefaultState.Off),
				["calt"] = Visible("Contextual Alternates", OpenTypeFeatureDefaultState.On),
				["case"] = Visible("Case-Sensitive Forms"),
				["ccmp"] = Hidden("Glyph Composition/Decomposition"),
				["cfar"] = Hidden("Conjunct Form After Ro"),
				["chws"] = Visible("Contextual Half-width Spacing", OpenTypeFeatureDefaultState.On),
				["cjct"] = Hidden("Conjunct Forms"),
				["clig"] = Visible("Contextual Ligatures", OpenTypeFeatureDefaultState.On),
				["cpct"] = Visible("Centered CJK Punctuation", OpenTypeFeatureDefaultState.Off),
				["cpsp"] = Visible("Capital Spacing", OpenTypeFeatureDefaultState.On),
				["cswh"] = Visible("Contextual Swash", OpenTypeFeatureDefaultState.Off),
				["curs"] = Hidden("Cursive Attachment"),
				["dcap"] = Visible("Drop Caps"),
				["dist"] = Hidden("Distance"),
				["dlig"] = Visible("Discretionary Ligatures", OpenTypeFeatureDefaultState.Off),
				["dnom"] = Hidden("Denominators"),
				["dpng"] = Hidden("Dipthongs (Obsolete)"),
				["dtls"] = Hidden("Dotless Forms"),
				["expt"] = Visible("Expert Forms"),
				["falt"] = Visible("Final Glyph On Line"),
				["fin2"] = Hidden("Terminal Forms #2"),
				["fin3"] = Hidden("Terminal Forms #3"),
				["fina"] = Hidden("Terminal Forms"),
				["flac"] = Hidden("Flattened Accents over Capitals"),
				["frac"] = Visible("Diagonal Fractions", OpenTypeFeatureDefaultState.Off),
				["fwid"] = Visible("Full Widths", OpenTypeFeatureDefaultState.Off),
				["half"] = Hidden("Half Forms"),
				["haln"] = Hidden("Halant Forms"),
				["halt"] = Visible("Alternative Half Widths", OpenTypeFeatureDefaultState.On),
				["hist"] = Visible("Historical Forms", OpenTypeFeatureDefaultState.Off),
				["hkna"] = Visible("Horizontal Kana Alternatives", OpenTypeFeatureDefaultState.Off),
				["hlig"] = Visible("Historic Ligatures", OpenTypeFeatureDefaultState.Off),
				["hngl"] = Hidden("Hanja to Hangul"),
				["hojo"] = Visible("Hojo (JIS X 0212-1990) Kanji Forms", OpenTypeFeatureDefaultState.Off),
				["hwid"] = Visible("Half Widths", OpenTypeFeatureDefaultState.Off),
				["init"] = Hidden("Initial Forms"),
				["isol"] = Hidden("Isolated Forms"),
				["ital"] = Visible("Italics"),
				["jalt"] = Visible("Justification Alternatives"),
				["jajp"] = Hidden("Japanese Forms (Obsolete)"),
				["jp04"] = Visible("JIS2004 Forms", OpenTypeFeatureDefaultState.Off),
				["jp78"] = Visible("JIS78 Forms", OpenTypeFeatureDefaultState.Off),
				["jp83"] = Visible("JIS83 Forms", OpenTypeFeatureDefaultState.Off),
				["jp90"] = Visible("JIS90 Forms", OpenTypeFeatureDefaultState.Off),
				["kern"] = Visible("Horizontal Kerning", OpenTypeFeatureDefaultState.On),
				["lfbd"] = Visible("Left Bounds", OpenTypeFeatureDefaultState.Off),
				["liga"] = Visible("Standard Ligatures", OpenTypeFeatureDefaultState.On),
				["ljmo"] = Hidden("Leading Jamo Forms"),
				["lnum"] = Visible("Lining Figures", OpenTypeFeatureDefaultState.Off),
				["locl"] = Hidden("Localized Forms"),
				["ltra"] = Hidden("Left-to-right glyph alternates"),
				["ltrm"] = Hidden("Left-to-right mirrored forms"),
				["mark"] = Hidden("Mark Positioning"),
				["med2"] = Hidden("Medial Forms 2"),
				["medi"] = Hidden("Medial Forms"),
				["mgrk"] = Visible("Mathematical Greek", OpenTypeFeatureDefaultState.Off),
				["mkmk"] = Hidden("Mark to Mark"),
				["mset"] = Visible("Mark Positioning via Substitution"),
				["nalt"] = Visible("Alternate Annotation Forms", OpenTypeFeatureDefaultState.Off),
				["nlck"] = Visible("NLC Kanji Forms", OpenTypeFeatureDefaultState.Off),
				["nukt"] = Hidden("Nukta Forms"),
				["numr"] = Hidden("Numerators"),
				["onum"] = Visible("Oldstyle Figures", OpenTypeFeatureDefaultState.Off),
				["opbd"] = Hidden("Optical Bounds"),
				["ordn"] = Visible("Ordinals", OpenTypeFeatureDefaultState.Off),
				["ornm"] = Visible("Ornaments", OpenTypeFeatureDefaultState.Off),
				["palt"] = Visible("Proportional Alternate Metrics", OpenTypeFeatureDefaultState.Off),
				["pcap"] = Visible("Lowercase to Petite Capitals", OpenTypeFeatureDefaultState.Off),
				["pkna"] = Visible("Proportional Kana", OpenTypeFeatureDefaultState.Off),
				["pnum"] = Visible("Proportional Numbers", OpenTypeFeatureDefaultState.Off),
				["pref"] = Hidden("Pre Base Forms"),
				["pres"] = Hidden("Pre Base Substitutions"),
				["pstf"] = Hidden("Post Base Forms"),
				["psts"] = Hidden("Post Base Substitutions"),
				["pwid"] = Visible("Proportional Width"),
				["qwid"] = Visible("Quarter Widths", OpenTypeFeatureDefaultState.Off),
				["rand"] = Visible("Randomize", OpenTypeFeatureDefaultState.On),
				["rclt"] = Hidden("Required Contextual Alternates"),
				["rkrf"] = Hidden("Rakar Forms"),
				["rlig"] = Hidden("Required Ligatures"),
				["rphf"] = Hidden("Reph Form"),
				["rtbd"] = Visible("Right Bounds", OpenTypeFeatureDefaultState.Off),
				["rtla"] = Hidden("Right to Left Alternates"),
				["rtlm"] = Hidden("Right to Left mirrored forms"),
				["ruby"] = Visible("Ruby Notational Forms", OpenTypeFeatureDefaultState.Off),
				["rvrn"] = Hidden("Required Variation Alternates"),
				["salt"] = Visible("Stylistic Alternatives", OpenTypeFeatureDefaultState.Off),
				["sinf"] = Visible("Scientific Inferiors", OpenTypeFeatureDefaultState.Off),
				["smcp"] = Visible("Lowercase to Small Capitals", OpenTypeFeatureDefaultState.Off),
				["size"] = Visible("Optical size", OpenTypeFeatureDefaultState.On),
				["smpl"] = Visible("Simplified Forms", OpenTypeFeatureDefaultState.Off),
				["ssty"] = Hidden("Math script style alternates"),
				["stch"] = Hidden("Stretching Glyph Decomposition"),
				["subs"] = Visible("Subscript", OpenTypeFeatureDefaultState.Off),
				["sups"] = Visible("Superscript", OpenTypeFeatureDefaultState.Off),
				["swsh"] = Visible("Swash", OpenTypeFeatureDefaultState.Off),
				["titl"] = Visible("Titling", OpenTypeFeatureDefaultState.Off),
				["tjmo"] = Hidden("Trailing Jamo Forms"),
				["tnam"] = Visible("Traditional Name Forms", OpenTypeFeatureDefaultState.Off),
				["tnum"] = Visible("Tabular Numbers", OpenTypeFeatureDefaultState.Off),
				["trad"] = Visible("Traditional Forms", OpenTypeFeatureDefaultState.Off),
				["twid"] = Visible("Third Widths", OpenTypeFeatureDefaultState.Off),
				["unic"] = Visible("Unicase", OpenTypeFeatureDefaultState.Off),
				["valt"] = Visible("Alternate Vertical Metrics"),
				["vatu"] = Hidden("Vattu Variants"),
				["vchw"] = Visible("Vertical Contextual Half-width Spacing"),
				["vert"] = Visible("Vertical Alternates"),
				["vhal"] = Visible("Alternate Vertical Half Metrics", OpenTypeFeatureDefaultState.Off),
				["vjmo"] = Hidden("Vowel Jamo Forms"),
				["vkna"] = Visible("Vertical Kana Alternates", OpenTypeFeatureDefaultState.Off),
				["vkrn"] = Visible("Vertical Kerning"),
				["vpal"] = Visible("Proportional Alternate Vertical Metrics", OpenTypeFeatureDefaultState.Off),
				["vrt2"] = Visible("Vertical Rotation & Alternates"),
				["zero"] = Visible("Slashed Zero", OpenTypeFeatureDefaultState.Off),
			};

		/// <summary>Gets the catalog entry for a tag, or null when the tag is not registered here.</summary>
		public static OpenTypeFeatureCatalogEntry Lookup(string tag)
		{
			OpenTypeFeatureCatalogEntry entry;
			return tag != null && s_entries.TryGetValue(tag, out entry) ? entry : null;
		}

		/// <summary>Gets whether the tag is a registered feature this catalog knows about.</summary>
		public static bool IsKnown(string tag)
		{
			return tag != null && s_entries.ContainsKey(tag);
		}

		/// <summary>Gets whether the tag is a registered feature classified as hidden from users.</summary>
		public static bool IsHidden(string tag)
		{
			var entry = Lookup(tag);
			return entry != null && entry.IsHidden;
		}

		/// <summary>Gets whether the tag is a registered feature that shaping applies by default.</summary>
		public static bool IsDefaultOn(string tag)
		{
			var entry = Lookup(tag);
			return entry != null && entry.DefaultState == OpenTypeFeatureDefaultState.On;
		}

		/// <summary>Gets the English friendly name for a registered tag, or null when unknown.</summary>
		public static string GetEnglishName(string tag)
		{
			return Lookup(tag)?.EnglishName;
		}

		/// <summary>Gets every registered tag, for consistency checks and diagnostics.</summary>
		public static IEnumerable<string> AllTags => s_entries.Keys;
	}
}
