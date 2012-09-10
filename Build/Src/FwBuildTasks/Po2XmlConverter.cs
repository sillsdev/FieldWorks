using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace FwBuildTasks
{
	class Po2XmlConverter
	{
		public string PoFilePath { get; set; }
		public string XmlFilePath { get; set; }
		public bool Roundtrip { get; set; }

		public Po2XmlConverter()
		{
			PoFilePath = null;
			XmlFilePath = null;
			Roundtrip = false;
		}

		public int Run()
		{
			var writer = XmlWriter.Create(XmlFilePath);
			writer.WriteWhitespace(Environment.NewLine);
			writer.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"format-html.xsl\"");
			writer.WriteWhitespace(Environment.NewLine);
			writer.WriteStartElement("messages");

			var msg = new PoMessageWriter(writer, Roundtrip);

			var fileInput = new StreamReader(PoFilePath);
			while (fileInput.Peek() >= 0)
			{
				var l = fileInput.ReadLine();
				if (l == null)
					break;

				// Continuation string?
				var m = (new Regex("^\\s*\"(.*)\"")).Match(l);
				if(m.Success)
				{
					//Debug.Assert(msg.Current != null);
					msg.Current.Add(EscapeForXml(UnescapeBackslashes(m.Groups[1].ToString())));
					continue;
				}
				else
				{
					msg.Flush();
				}
				m = new Regex("^msgid \"(.*)\"", RegexOptions.Singleline).Match(l);
				if(m.Success)
				{
					msg.Msgid = new List<String> { EscapeForXml(UnescapeBackslashes(m.Groups[1].ToString())) };
					msg.Current = msg.Msgid;
				}
				m = new Regex("^msgstr \"(.*)\"", RegexOptions.Singleline).Match(l);
				if (m.Success)
				{
					msg.Msgstr = new List<String> { EscapeForXml(UnescapeBackslashes(m.Groups[1].ToString())) };
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
			return 0;
		}

		/// <summary>
		/// This escapes characters that XML may need to be escaped with &...;.
		/// </summary>
		private string EscapeForXml(string p)
		{
			var result = System.Security.SecurityElement.Escape(p);
			if (result.IndexOfAny(new char[] { '\n', '\r' }) >= 0)
			{
				result = result.Replace("\n", "&#x0A;");
				result = result.Replace("\r", "&#x0D;");
			}
			return result;
		}

		/// <summary>
		/// This interprets the \ escape characters.
		/// </summary>
		private string UnescapeBackslashes(string p)
		{
			var result = p;
			for (int idx = result.IndexOf('\\', 0); idx >= 0 && idx < result.Length; idx = result.IndexOf('\\', idx))
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

		internal class PoMessageWriter
		{
			private readonly XmlWriter m_writer;
			private readonly bool m_roundtrip;
			private readonly string m_indent;
			private List<string> m_msgid;
			private List<string> m_msgstr;
			private List<string> m_usrcomment = new List<string>();
			private List<string> m_dotcomment = new List<string>();
			private List<string> m_reference = new List<string>();
			private List<string> m_flags = new List<string>();
			private List<string> m_current;

			public List<String> Current
			{
				get { return m_current ?? (m_current = new List<string>()); }
				set { m_current = value; }
			}

			public List<string> Msgid
			{
				get { return m_msgid; }
				set { m_msgid = value; }
			}

			public List<string> Msgstr
			{
				get { return m_msgstr; }
				set { m_msgstr = value; }
			}

			public List<string> UsrComment
			{
				get { return m_usrcomment; }
			}

			public List<string> DotComment
			{
				get { return m_dotcomment; }
			}

			public List<string> Reference
			{
				get { return m_reference; }
			}

			public List<string> Flags
			{
				get { return m_flags; }
			}

			public PoMessageWriter(XmlWriter writer, bool roundtrip = false, string indent = "  ")
			{
				m_writer = writer;
				m_roundtrip = roundtrip;
				m_indent = indent;
				Reset();
			}

			private void Reset()
			{
				m_msgid = new List<string>();
				m_msgstr = new List<string>();
				m_usrcomment = new List<string>();
				m_dotcomment = new List<string>();
				m_reference = new List<string>();
				m_flags = new List<string>();
				m_current = null;
			}

			public void Flush()
			{
				if (m_current != m_msgstr)
					return;
				Write();
				Reset();
			}

			private void Write()
			{
				m_writer.WriteWhitespace(Environment.NewLine);
				m_writer.WriteStartElement("msg");
				m_writer.WriteWhitespace(Environment.NewLine);

				if (m_roundtrip)
				{
					// Support exact round-tripping using multiple <key> and <str> child elements:
					foreach (var t in m_msgid)
						WriteElement("key", t);
					foreach (var t in m_msgstr)
						WriteElement("str", t);
				}
				else
				{
					// Concatenate parts into single <key> and <str> child elements:
					WriteElement("key", string.Join("", m_msgid));
					WriteElement("str", string.Join("", m_msgstr));
				}
				foreach (var t in m_usrcomment)
					WriteElement("comment", t);
				foreach (var t in m_dotcomment)
					WriteElement("info", t);
				foreach (var t in m_reference)
					WriteElement("ref", t);
				foreach (var t in m_flags)
					WriteElement("flags", t);
				m_writer.WriteEndElement();
			}

			private void WriteElement(string name, string data, params string[] attrs)
			{
				m_writer.WriteWhitespace(m_indent);
				m_writer.WriteStartElement(name);
				for (int i = 0; i < attrs.Length; i++)
				{
					if (i + 1 >= attrs.Length)
						throw new ArgumentException("ERROR: List of attributes for XML element " + name +
													" is missing data for attribute " + attrs[i]);
					m_writer.WriteAttributeString(attrs[i], attrs[i + 1]);
				}
				m_writer.WriteRaw(data);
				m_writer.WriteEndElement();
				m_writer.WriteWhitespace(Environment.NewLine);
			}
		}
	}
}
