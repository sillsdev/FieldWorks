// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	internal partial class Po2XmlConverter
	{
		public Po2XmlConverter()
		{
			PoFilePath = null;
			XmlFilePath = null;
			Roundtrip = false;
		}

		public string PoFilePath { private get; set; }
		public string XmlFilePath { private get; set; }
		private bool Roundtrip { get; }

		public int Run()
		{
			using (var writer = XmlWriter.Create(XmlFilePath))
			{
				writer.WriteWhitespace(Environment.NewLine);
				writer.WriteProcessingInstruction("xml-stylesheet",
					"type=\"text/xsl\" href=\"format-html.xsl\"");
				writer.WriteWhitespace(Environment.NewLine);
				writer.WriteStartElement("messages");

				var msg = new PoMessageWriter(writer, Roundtrip);

				using (var fileInput = new StreamReader(PoFilePath))
				{
					while (fileInput.Peek() >= 0)
					{
						var l = fileInput.ReadLine();
						if (l == null)
							break;

						// Continuation string?
						var m = new Regex("^\\s*\"(.*)\"").Match(l);
						if (m.Success)
						{
							//Debug.Assert(msg.Current != null);
							msg.Current.Add(EscapeForXml(UnescapeBackslashes(m.Groups[1].ToString())));
							continue;
						}
						msg.Flush();
						m = new Regex("^msgid \"(.*)\"", RegexOptions.Singleline).Match(l);
						if (m.Success)
						{
							msg.Msgid = new List<string> {EscapeForXml(UnescapeBackslashes(m.Groups[1].ToString()))};
							msg.Current = msg.Msgid;
						}
						m = new Regex("^msgstr \"(.*)\"", RegexOptions.Singleline).Match(l);
						if (m.Success)
						{
							msg.Msgstr = new List<string> {EscapeForXml(UnescapeBackslashes(m.Groups[1].ToString()))};
							msg.Current = msg.Msgstr;
						}

						m = new Regex("^# \\s*(.*)").Match(l);
						if (m.Success)
						{
							msg.UsrComment.Add(EscapeForXml(m.Groups[1].ToString()));
						}

						m = new Regex("^#\\.\\s*(.*)").Match(l);
						if (m.Success)
						{
							msg.DotComment.Add(EscapeForXml(m.Groups[1].ToString()));
						}

						m = new Regex("^#:\\s*(.*)").Match(l);
						if (m.Success)
						{
							msg.Reference.Add(EscapeForXml(m.Groups[1].ToString()));
						}

						m = new Regex("^#,\\s*(.*)").Match(l);
						if (m.Success)
						{
							msg.Flags.Add(EscapeForXml(m.Groups[1].ToString()));
						}

					}
					msg.Flush();
					writer.WriteWhitespace(Environment.NewLine);
					writer.WriteEndDocument();
					writer.Close();
					fileInput.Close();
				}
				return 0;
			}
		}

		/// <summary>
		/// This escapes characters that XML may need to be escaped with &...;.
		/// </summary>
		private static string EscapeForXml(string p)
		{
			var result = System.Security.SecurityElement.Escape(p);
			if (result.IndexOfAny(new char[] {'\n', '\r'}) < 0)
				return result;

			result = result.Replace("\n", "&#x0A;");
			result = result.Replace("\r", "&#x0D;");
			return result;
		}

		/// <summary>
		/// This interprets the \ escape characters.
		/// </summary>
		private static string UnescapeBackslashes(string p)
		{
			var result = p;
			for (var idx = result.IndexOf('\\', 0); idx >= 0 && idx < result.Length; idx = result.IndexOf('\\', idx))
			{
				switch (result[idx + 1])
				{
					case 'n':
						result = result.Remove(idx, 2);
						result = result.Insert(idx, "\n");
						break;
					case 'r':
						result = result.Remove(idx, 2);
						result = result.Insert(idx, "\r");
						break;
					case 't':
						result = result.Remove(idx, 2);
						result = result.Insert(idx, "\t");
						break;
					default:
						result = result.Remove(idx, 1);
						break;
				}
				++idx;		// Move past the remaining character (might be \).
			}
			return result;
		}
	}
}
