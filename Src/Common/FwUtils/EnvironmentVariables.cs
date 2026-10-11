// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// The shared rule for reading an environment variable that turns a FieldWorks feature on.
	/// </summary>
	public static class EnvironmentVariables
	{
		/// <summary>
		/// True when the named environment variable is set to a value that opts in. An
		/// unset variable is off, so a feature read through this defaults to off until a
		/// user asks for it.
		/// </summary>
		public static bool IsTrue(string variableName) =>
			IsTrueValue(Environment.GetEnvironmentVariable(variableName));

		/// <summary>
		/// True for every <paramref name="value"/> except null, blank, "0", "false", and
		/// "off", compared case-insensitively and ignoring surrounding whitespace. So a
		/// variable can be spelled "1", "true", or "yes" and still read as true, while "0"
		/// reliably reads as false.
		/// </summary>
		public static bool IsTrueValue(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return false;

			var trimmed = value.Trim();
			return !string.Equals(trimmed, "0", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(trimmed, "off", StringComparison.OrdinalIgnoreCase);
		}
	}
}
