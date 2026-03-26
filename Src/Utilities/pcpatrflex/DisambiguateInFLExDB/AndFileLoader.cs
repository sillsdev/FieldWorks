// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDB
{
	public static class AndFileLoader
	{
		static string[] SplitOn = new string[] { "\\parse", "\\endparse" };
		static string[] pField = new string[] { "\\p " };

		public static string[] GetGuidsFromAndFile(string andfile)
		{
			var guids = new List<string>();
			using (var sr = new StreamReader(andfile, Encoding.UTF8))
			{
				var contents = sr.ReadToEnd();
				sr.Close();
				// skip any \id fields at the beginning
				int iStart = Math.Max(0, contents.IndexOf(pField[0]));
				var sections = contents
					.Substring(iStart)
					.Split(SplitOn, StringSplitOptions.RemoveEmptyEntries);
				foreach (string section in sections)
				{
					if (section.Contains("\\p"))
					{
						ProcessAna(guids, section);
					}
				}
			}
			return guids.ToArray();
		}

		private static void ProcessAna(List<string> guids, string ana)
		{
			if (ana.Contains("%"))
			{
				// still ambiguous; skip it
				guids.Add("");
			}
			else
			{
				var ps = ana.Split(pField, StringSplitOptions.RemoveEmptyEntries);
				var sb = new StringBuilder();
				foreach (string p in ps)
				{
					if (p.Length >= 2 && p[0] != '\r' && p[1] != '\n')
					{
						int i = p.IndexOf("\n");
						string start = p.Substring(0, i + 1);
						var clean = start.Replace("\r", "").Replace("\n", "");
						sb.Append(clean);
						sb.Append("\n");
					}
				}
				guids.Add(sb.ToString());
			}
		}
	}
}
