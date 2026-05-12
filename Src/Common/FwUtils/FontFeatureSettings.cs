using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Parses and normalizes renderer-neutral font feature strings of the form tag=value.
	/// </summary>
	public static class FontFeatureSettings
	{
		private static readonly TraceSwitch s_traceSwitch =
			new TraceSwitch("FwUtils_FontFeatureSettings", "Font feature parsing diagnostics");

		internal static TraceSwitch DiagnosticsSwitch
		{
			get { return s_traceSwitch; }
		}

		/// <summary>
		/// Parses a comma-separated font feature string into normalized feature settings.
		/// Invalid entries are ignored so project data cannot crash render/UI paths.
		/// </summary>
		public static IReadOnlyList<FontFeatureSetting> Parse(string features)
		{
			if (string.IsNullOrWhiteSpace(features))
				return Array.Empty<FontFeatureSetting>();

			var settingsByTag = new Dictionary<string, FontFeatureSetting>(StringComparer.Ordinal);
			foreach (var rawPart in features.Split(','))
			{
				var part = rawPart.Trim();
				if (part.Length == 0)
					continue;

				var equalsIndex = part.IndexOf('=');
				if (equalsIndex <= 0 || equalsIndex == part.Length - 1)
				{
					TraceIgnoredEntry(part, "expected tag=value");
					continue;
				}

				var tag = part.Substring(0, equalsIndex).Trim();
				var valueText = part.Substring(equalsIndex + 1).Trim();
				if (!IsValidOpenTypeTag(tag))
				{
					TraceIgnoredEntry(part, "tag must contain exactly four printable ASCII characters");
					continue;
				}

				int value;
				if (!int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < 0)
				{
					TraceIgnoredEntry(part, "value must be a non-negative integer");
					continue;
				}

				settingsByTag[tag] = new FontFeatureSetting(tag, value);
			}

			return settingsByTag.Values.OrderBy(setting => setting.Tag, StringComparer.Ordinal).ToArray();
		}

		/// <summary>
		/// Returns a deterministic string representation of valid feature settings.
		/// </summary>
		public static string Normalize(string features)
		{
			return string.Join(",", Parse(features).Select(setting => setting.ToString()));
		}

		/// <summary>
		/// Returns a deterministic representation for OpenType feature strings while preserving
		/// legacy numeric Graphite feature identifiers.
		/// </summary>
		public static string NormalizePreservingLegacy(string features)
		{
			if (string.IsNullOrWhiteSpace(features))
				return string.Empty;

			var trimmed = features.Trim();
			return LooksLikeLegacyGraphiteFeatureString(trimmed) ? trimmed : Normalize(trimmed);
		}

		private static bool LooksLikeLegacyGraphiteFeatureString(string features)
		{
			var firstPart = features.Split(',').FirstOrDefault();
			if (string.IsNullOrWhiteSpace(firstPart))
				return false;

			var equalsIndex = firstPart.IndexOf('=');
			if (equalsIndex <= 0)
				return false;

			var featureId = firstPart.Substring(0, equalsIndex).Trim();
			return featureId.Length > 0 && featureId.All(char.IsDigit);
		}

		/// <summary>
		/// Returns whether a string is a valid four-character OpenType feature tag.
		/// </summary>
		public static bool IsValidOpenTypeTag(string tag)
		{
			return tag != null && tag.Length == 4 && tag.All(character => character >= 0x20 && character <= 0x7e);
		}

		private static void TraceIgnoredEntry(string part, string reason)
		{
			Trace.WriteLineIf(s_traceSwitch.TraceWarning,
				string.Format(CultureInfo.InvariantCulture,
					"Ignored invalid font feature entry '{0}': {1}.",
					part,
					reason),
				s_traceSwitch.DisplayName);
		}
	}

	/// <summary>
	/// A single renderer-neutral font feature setting.
	/// </summary>
	public sealed class FontFeatureSetting
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FontFeatureSetting"/> class.
		/// </summary>
		public FontFeatureSetting(string tag, int value)
		{
			if (!FontFeatureSettings.IsValidOpenTypeTag(tag))
				throw new ArgumentException("OpenType feature tags must contain exactly four printable ASCII characters.", nameof(tag));
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), "Feature values must be non-negative.");

			Tag = tag;
			Value = value;
		}

		/// <summary>
		/// Gets the four-character OpenType feature tag.
		/// </summary>
		public string Tag { get; }

		/// <summary>
		/// Gets the feature value.
		/// </summary>
		public int Value { get; }

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}={1}", Tag, Value);
		}
	}
}
