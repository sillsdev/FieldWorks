// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Archiving
{
	/// <summary>
	/// Extensions to other classes used by FW Archiving code.
	/// </summary>
	internal static class ArchivingExtensions
	{
		/// <summary>
		/// Combines the functionality fo StringBuilder.AppendFormat and StringBuilder.AppendLine.
		/// Also allows for the delimiter to be specified.  If the delimiter is null, Environment.NewLine
		/// will be used.
		/// </summary>
		internal static void AppendLineFormat(this StringBuilder sb, string format, object[] args, string delimiter)
		{
			if (delimiter == null)
			{
				delimiter = Environment.NewLine;
			}
			if (sb.Length != 0)
			{
				sb.Append(delimiter);
			}
			sb.AppendFormat(format, args);
		}

		/// <summary>
		/// Finds the ISO3 code for the given writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The ISO3 code, or <value>mis</value> if the code is not found.</returns>
		internal static string GetIso3Code(this CoreWritingSystemDefinition ws)
		{
			var iso3Code = ws.Language.Iso3Code;
			if (!string.IsNullOrEmpty(iso3Code))
			{
				return iso3Code;
			}
			iso3Code = ws.Id;
			// split the result, the iso3 code is in the first segment
			var segments = iso3Code.Split('-');
			iso3Code = segments[0];
			// if the code is "Local" return uncoded code
			return string.Compare(iso3Code, "q", StringComparison.OrdinalIgnoreCase) > 0 && string.Compare(iso3Code, "qu", StringComparison.OrdinalIgnoreCase) < 0
				? "mis" : string.IsNullOrEmpty(iso3Code) || iso3Code.Length != 3 ? "mis" : iso3Code;
		}
	}
}