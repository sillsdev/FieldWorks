using System;
using System.Text;
using SIL.CoreImpl;

namespace SIL.FieldWorks.XWorks.Archiving
{
	public static class ArchivingExtensions
	{
		/// <summary>
		/// Combines the functionality fo StringBuilder.AppendFormat and StringBuilder.AppendLine.
		/// Also allows for the delimiter to be specified.  If the delimiter is null, Environment.NewLine
		/// will be used.
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="format"></param>
		/// <param name="args"></param>
		/// <param name="delimiter"></param>
		public static void AppendLineFormat(this StringBuilder sb, string format, object[] args, string delimiter)
		{
			if (delimiter == null) delimiter = Environment.NewLine;
			if (sb.Length != 0) sb.Append(delimiter);
			sb.AppendFormat(format, args);
		}

		/// <summary>
		/// Finds the ISO3 code for the given writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>The ISO3 code, or <value>mis</value> if the code is not found.</returns>
		public static string GetIso3Code(this CoreWritingSystemDefinition ws)
		{
			string iso3Code = ws.Language.Iso3Code;
			if (!string.IsNullOrEmpty(iso3Code))
				return iso3Code;

			iso3Code = ws.Id;

			// split the result, the iso3 code is in the first segment
			var segments = iso3Code.Split(new[] { '-' });
			iso3Code = segments[0];

			// if the code is "Local" return uncoded code
			if ((String.Compare(iso3Code, "q", StringComparison.OrdinalIgnoreCase) > 0) && (String.Compare(iso3Code, "qu", StringComparison.OrdinalIgnoreCase) < 0))
				return "mis";

			// return "mis" for uncoded languages
			if (string.IsNullOrEmpty(iso3Code) || (iso3Code.Length != 3))
				return "mis";

			return iso3Code;
		}
	}
}
