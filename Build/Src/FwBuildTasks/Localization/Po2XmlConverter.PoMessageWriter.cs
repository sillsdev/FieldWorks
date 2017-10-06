// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	internal partial class Po2XmlConverter
	{
		private class PoMessageWriter
		{
			private readonly XmlWriter m_writer;
			private readonly bool m_roundtrip;
			private readonly string m_indent;
			private List<string> m_current;

			public PoMessageWriter(XmlWriter writer, bool roundtrip = false, string indent = "  ")
			{
				m_writer = writer;
				m_roundtrip = roundtrip;
				m_indent = indent;
				Reset();
			}

			public List<string> Current
			{
				get => m_current ?? (m_current = new List<string>());
				set => m_current = value;
			}

			public List<string> Msgid { get; set; }

			public List<string> Msgstr { get; set; }

			public List<string> UsrComment { get; private set; } = new List<string>();

			public List<string> DotComment { get; private set; } = new List<string>();

			public List<string> Reference { get; private set; } = new List<string>();

			public List<string> Flags { get; private set; } = new List<string>();

			private void Reset()
			{
				Msgid = new List<string>();
				Msgstr = new List<string>();
				UsrComment = new List<string>();
				DotComment = new List<string>();
				Reference = new List<string>();
				Flags = new List<string>();
				m_current = null;
			}

			public void Flush()
			{
				if (m_current != Msgstr)
					return;
				Write();
				Reset();
			}

			private void Write()
			{
				if (Flags != null && Flags.Contains("fuzzy"))
					return;
				m_writer.WriteWhitespace(Environment.NewLine);
				m_writer.WriteStartElement("msg");
				m_writer.WriteWhitespace(Environment.NewLine);

				if (m_roundtrip)
				{
					// Support exact round-tripping using multiple <key> and <str> child elements:
					foreach (var t in Msgid)
						WriteElement("key", t);
					foreach (var t in Msgstr)
						WriteElement("str", t);
				}
				else
				{
					// Concatenate parts into single <key> and <str> child elements:
					WriteElement("key", string.Join("", Msgid));
					WriteElement("str", string.Join("", Msgstr));
				}
				foreach (var t in UsrComment)
					WriteElement("comment", t);
				foreach (var t in DotComment)
					WriteElement("info", t);
				foreach (var t in Reference)
					WriteElement("ref", t);
				foreach (var t in Flags)
					WriteElement("flags", t);
				m_writer.WriteEndElement();
			}

			private void WriteElement(string name, string data, params string[] attrs)
			{
				m_writer.WriteWhitespace(m_indent);
				m_writer.WriteStartElement(name);
				for (var i = 0; i < attrs.Length; i++)
				{
					if (i + 1 >= attrs.Length)
					{
						throw new ArgumentException(
							$"ERROR: List of attributes for XML element {name} is missing data for attribute {attrs[i]}");
					}
					m_writer.WriteAttributeString(attrs[i], attrs[i + 1]);
				}
				m_writer.WriteRaw(data);
				m_writer.WriteEndElement();
				m_writer.WriteWhitespace(Environment.NewLine);
			}
		}
	}
}
