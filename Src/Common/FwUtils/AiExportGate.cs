// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// The opt-in gate for exporting a project for AI analysis. Default is off: the export option is
	/// absent from the export list until a user sets FLEX_AI_EXPORT.
	/// </summary>
	public static class AiExportGate
	{
		/// <summary>Environment variable a user sets to opt in to the AI-analysis export.</summary>
		public const string EnabledVariable = "FLEX_AI_EXPORT";

		/// <summary>Reads <see cref="EnabledVariable"/> from the current process environment.</summary>
		public static bool IsEnabled() =>
			IsEnabled(Environment.GetEnvironmentVariable(EnabledVariable));

		/// <summary>
		/// True for any <paramref name="variableValue"/> except null, blank, "0", "false", and "off"
		/// (case-insensitive), so the variable can be spelled "1", "true", or "yes" and still turn the
		/// export on, while "0" reliably turns it back off.
		/// </summary>
		internal static bool IsEnabled(string variableValue)
		{
			if (string.IsNullOrWhiteSpace(variableValue))
				return false;

			var trimmed = variableValue.Trim();
			return !string.Equals(trimmed, "0", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(trimmed, "off", StringComparison.OrdinalIgnoreCase);
		}
	}
}
