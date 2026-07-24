// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Feature information discovered from a font's GSUB/GPOS feature lists: the four-character
	/// tag plus any font-supplied UI label and the named options a character-variant feature
	/// exposes. See LT-22638.
	/// </summary>
	public sealed class OpenTypeFontFeatureInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OpenTypeFontFeatureInfo"/> class.
		/// </summary>
		public OpenTypeFontFeatureInfo(string tag, string fontSuppliedLabel, IReadOnlyList<string> options)
		{
			Tag = tag ?? throw new ArgumentNullException(nameof(tag));
			FontSuppliedLabel = fontSuppliedLabel;
			Options = options ?? Array.Empty<string>();
		}

		/// <summary>Gets the four-character OpenType feature tag.</summary>
		public string Tag { get; }

		/// <summary>
		/// Gets the human-readable feature label supplied by the font, or null when the font
		/// provides none (registered features and stylistic/character sets without featureParams).
		/// </summary>
		public string FontSuppliedLabel { get; }

		/// <summary>
		/// Gets the ordered named options for a character-variant feature; empty for binary
		/// features. Option index i (zero-based) corresponds to feature value i+1.
		/// </summary>
		public IReadOnlyList<string> Options { get; }
	}

	/// <summary>
	/// Reads user-facing OpenType feature information from a font's layout tables. Table bytes are
	/// supplied through a delegate so production callers can pass GDI GetFontData results while
	/// tests pass bytes sliced from a font file; every read is bounds-checked so malformed or
	/// hostile fonts degrade to tag-only records instead of throwing. Parsing is adapted from
	/// Paratext's OpenTypeFeatures.Ttf reader (LT-22638).
	/// </summary>
	public static class OpenTypeFontFeatureInfoReader
	{
		private static readonly string[] s_layoutTables = { "GSUB", "GPOS" };
		private const int MaxNamedParameters = 1024;

		/// <summary>
		/// Reads feature information for the current font. <paramref name="tableSource"/> receives a
		/// four-character table tag ("GSUB", "GPOS", "name") and returns that table's bytes, or null
		/// when the font has no such table. Records are deduplicated by tag; when a tag appears in
		/// more than one script/language the richest record (one carrying a label or options) wins.
		/// </summary>
		public static IReadOnlyList<OpenTypeFontFeatureInfo> Read(Func<string, byte[]> tableSource)
		{
			if (tableSource == null)
				throw new ArgumentNullException(nameof(tableSource));

			var names = ParseNameTable(SafeGet(tableSource, "name"));
			var byTag = new Dictionary<string, OpenTypeFontFeatureInfo>(StringComparer.Ordinal);

			foreach (var tableTag in s_layoutTables)
			{
				var table = SafeGet(tableSource, tableTag);
				if (table == null)
					continue;
				foreach (var info in ReadFeatureList(table, names))
				{
					OpenTypeFontFeatureInfo existing;
					if (!byTag.TryGetValue(info.Tag, out existing) || IsRicher(info, existing))
						byTag[info.Tag] = info;
				}
			}

			return byTag.Values.OrderBy(info => info.Tag, StringComparer.Ordinal).ToArray();
		}

		private static bool IsRicher(OpenTypeFontFeatureInfo candidate, OpenTypeFontFeatureInfo existing)
		{
			var candidateScore = (candidate.FontSuppliedLabel != null ? 1 : 0) + candidate.Options.Count;
			var existingScore = (existing.FontSuppliedLabel != null ? 1 : 0) + existing.Options.Count;
			return candidateScore > existingScore;
		}

		private static byte[] SafeGet(Func<string, byte[]> tableSource, string tag)
		{
			try
			{
				return tableSource(tag);
			}
			catch
			{
				return null;
			}
		}

		private static IEnumerable<OpenTypeFontFeatureInfo> ReadFeatureList(byte[] table, IReadOnlyDictionary<int, string> names)
		{
			int featureListOffset;
			if (!TryReadUInt16(table, 6, out featureListOffset) || featureListOffset == 0)
				yield break;

			int featureCount;
			if (!TryReadUInt16(table, featureListOffset, out featureCount))
				yield break;

			for (var i = 0; i < featureCount; i++)
			{
				var recordOffset = featureListOffset + 2 + i * 6;
				string tag;
				int featureOffset;
				if (!TryReadTag(table, recordOffset, out tag) ||
					!TryReadUInt16(table, recordOffset + 4, out featureOffset))
					yield break;

				if (!FontFeatureSettings.IsValidOpenTypeTag(tag))
					continue;

				var featureTableStart = featureListOffset + featureOffset;
				int featureParamsOffset;
				if (!TryReadUInt16(table, featureTableStart, out featureParamsOffset))
					continue;

				string label = null;
				var options = Array.Empty<string>();
				if (featureParamsOffset != 0)
				{
					var paramsStart = featureTableStart + featureParamsOffset;
					if (IsCharacterVariantTag(tag))
						ReadCharacterVariantParams(table, paramsStart, names, out label, out options);
					else if (IsStylisticSetTag(tag))
						label = ReadStylisticSetName(table, paramsStart, names);
				}

				yield return new OpenTypeFontFeatureInfo(tag, label, options);
			}
		}

		private static void ReadCharacterVariantParams(byte[] table, int paramsStart,
			IReadOnlyDictionary<int, string> names, out string label, out string[] options)
		{
			label = null;
			options = Array.Empty<string>();

			int labelNameId, numNamedParameters, firstParamNameId;
			if (!TryReadUInt16(table, paramsStart + 2, out labelNameId) ||
				!TryReadUInt16(table, paramsStart + 8, out numNamedParameters) ||
				!TryReadUInt16(table, paramsStart + 10, out firstParamNameId))
				return;

			label = LookupName(names, labelNameId);

			if (numNamedParameters == 0 || numNamedParameters > MaxNamedParameters)
				return;
			var resolved = new List<string>(numNamedParameters);
			for (var i = 0; i < numNamedParameters; i++)
			{
				var option = LookupName(names, firstParamNameId + i);
				if (!string.IsNullOrEmpty(option))
					resolved.Add(option);
			}
			options = resolved.ToArray();
		}

		private static string ReadStylisticSetName(byte[] table, int paramsStart, IReadOnlyDictionary<int, string> names)
		{
			int uiNameId;
			return TryReadUInt16(table, paramsStart + 2, out uiNameId) ? LookupName(names, uiNameId) : null;
		}

		private static string LookupName(IReadOnlyDictionary<int, string> names, int nameId)
		{
			string value;
			return nameId > 0 && names.TryGetValue(nameId, out value) && !string.IsNullOrEmpty(value) ? value : null;
		}

		private static IReadOnlyDictionary<int, string> ParseNameTable(byte[] table)
		{
			var result = new Dictionary<int, string>();
			if (table == null)
				return result;

			int count, storageOffset;
			if (!TryReadUInt16(table, 2, out count) || !TryReadUInt16(table, 4, out storageOffset))
				return result;

			var best = new Dictionary<int, int>();
			for (var i = 0; i < count; i++)
			{
				var recordOffset = 6 + i * 12;
				int platformId, languageId, nameId, length, stringOffset;
				if (!TryReadUInt16(table, recordOffset, out platformId) ||
					!TryReadUInt16(table, recordOffset + 4, out languageId) ||
					!TryReadUInt16(table, recordOffset + 6, out nameId) ||
					!TryReadUInt16(table, recordOffset + 8, out length) ||
					!TryReadUInt16(table, recordOffset + 10, out stringOffset))
					break;

				var value = DecodeName(table, platformId, storageOffset + stringOffset, length);
				if (string.IsNullOrEmpty(value))
					continue;

				var rank = GetNameRank(platformId, languageId);
				int existingRank;
				if (!best.TryGetValue(nameId, out existingRank) || rank > existingRank)
				{
					best[nameId] = rank;
					result[nameId] = value;
				}
			}

			return result;
		}

		private static string DecodeName(byte[] table, int platformId, int stringStart, int length)
		{
			if (length == 0 || stringStart < 0 || stringStart > table.Length - length)
				return null;
			try
			{
				// Platform 0 (Unicode) and 3 (Windows) store UTF-16 big-endian; platform 1 (Mac)
				// stores Mac Roman. Other platforms are not decodable here and fall back to labels.
				if (platformId == 0 || platformId == 3)
					return Encoding.BigEndianUnicode.GetString(table, stringStart, length).TrimEnd('\0');
				if (platformId == 1)
					return MacRoman.GetString(table, stringStart, length).TrimEnd('\0');
			}
			catch
			{
				// Fall through to null so the feature uses its fallback label.
			}
			return null;
		}

		// Prefers a Windows US-English record, then any Windows-English, then Unicode, then any
		// Windows record, then Mac English, then anything decodable.
		private static int GetNameRank(int platformId, int languageId)
		{
			if (platformId == 3 && languageId == 0x0409)
				return 500;
			if (platformId == 3 && (languageId & 0x03FF) == 0x0009)
				return 450;
			if (platformId == 0)
				return 400;
			if (platformId == 3)
				return 300;
			if (platformId == 1 && languageId == 0)
				return 200;
			return 100;
		}

		private static bool IsCharacterVariantTag(string tag)
		{
			return tag.Length == 4 && tag[0] == 'c' && tag[1] == 'v' &&
				tag[2] >= '0' && tag[2] <= '9' && tag[3] >= '0' && tag[3] <= '9';
		}

		private static bool IsStylisticSetTag(string tag)
		{
			return tag.Length == 4 && tag[0] == 's' && tag[1] == 's' &&
				tag[2] >= '0' && tag[2] <= '9' && tag[3] >= '0' && tag[3] <= '9';
		}

		private static bool TryReadUInt16(byte[] data, int offset, out int value)
		{
			if (data == null || offset < 0 || offset > data.Length - 2)
			{
				value = 0;
				return false;
			}
			value = (data[offset] << 8) | data[offset + 1];
			return true;
		}

		private static bool TryReadTag(byte[] data, int offset, out string tag)
		{
			if (data == null || offset < 0 || offset > data.Length - 4)
			{
				tag = null;
				return false;
			}
			tag = Encoding.ASCII.GetString(data, offset, 4);
			return true;
		}

		private static readonly Encoding MacRoman = CreateMacRoman();

		private static Encoding CreateMacRoman()
		{
			try
			{
				return Encoding.GetEncoding(10000);
			}
			catch
			{
				return Encoding.ASCII;
			}
		}
	}
}
