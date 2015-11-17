// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PUAMigrator.cs
// Responsibility: mcconnel

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

using Microsoft.Win32;

using SIL.Utils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class supports migrating PUA definitions from the old language.xml files to the new
	/// CustomChars.xml file, and installing the character definitions.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PUAMigrator
	{
		Dictionary<int, string> m_mapPuaCodeData = new Dictionary<int, string>();
		bool m_fDirty = false;

		/// <summary>
		/// Constructor.
		/// </summary>
		public PUAMigrator()
		{
		}

		/// <summary>
		/// Migrate and/or install any custom character definitions found.
		/// </summary>
		public void Run()
		{
			// Read any existing custom character data.
			var customCharsFile = CharEditorWindow.CustomCharsFile;
			if (File.Exists(customCharsFile))
			{
				ReadCustomCharData(customCharsFile);
				m_fDirty = false;
			}

			// If there is any old custom character data (from FieldWorks 6.0 or earlier), read it
			// as well, adding any new characters found.
			string[] files = GetLangFiles();
			if (files != null)
			{
				foreach (string filename in files)
				{
					// I don't really trust Microsoft's pattern matching for files, so make sure we
					// don't get any edit backup files or the like.
					if (!filename.ToLowerInvariant().EndsWith(".xml"))
						continue;
					try
					{
						// Surprisingly, the read method is the same even though the details of
						// the two XML files are quite different apart from the CharDef elements.
						ReadCustomCharData(filename);
					}
					catch
					{
						// ignore any exceptions.  we'll just skip that file.
					}
				}
			}
			if (m_fDirty)
				WriteCustomCharData(customCharsFile);

			// Now, if we have any custom character data, install it!
			if (File.Exists(customCharsFile))
			{
				var inst = new PUAInstaller();
				inst.InstallPUACharacters(customCharsFile);
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "klm is a reference")]
		private string[] GetLangFiles()
		{
			RegistryKey klm = RegistryHelper.CompanyKeyLocalMachine;
			using (RegistryKey kprod = klm.OpenSubKey("FieldWorks"))
			{
				if (kprod == null)
					return null;
				string dataRootDir = kprod.GetValue("RootDataDir") as string;
				if (dataRootDir != null)
				{
					string langDir = Path.Combine(dataRootDir, "Languages");
					if (Directory.Exists(langDir))
						return Directory.GetFiles(langDir, "*.xml");
				}
				return null;
			}
		}

		private void WriteCustomCharData(string charsFile)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = "  ";
			using (XmlWriter writer = XmlWriter.Create(charsFile, settings))
			{
				writer.WriteStartDocument();
				writer.WriteStartElement("PuaDefinitions");
				foreach (var key in m_mapPuaCodeData.Keys)
				{
					writer.WriteStartElement("CharDef");
					writer.WriteAttributeString("code", key.ToString("X4"));
					writer.WriteAttributeString("data", m_mapPuaCodeData[key]);
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
				writer.Close();
			}
		}

		private void ReadCustomCharData(string customCharsFile)
		{
			var xdoc = XDocument.Load(customCharsFile, LoadOptions.None);
			foreach (var xeDef in xdoc.Descendants("CharDef"))
			{
				var xaCode = xeDef.Attribute("code");
				if (xaCode == null || String.IsNullOrEmpty(xaCode.Value))
					continue;
				var xaData = xeDef.Attribute("data");
				if (xaData == null || String.IsNullOrEmpty(xaData.Value))
					continue;
				int code;
				if (Int32.TryParse(xaCode.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code) &&
					!m_mapPuaCodeData.ContainsKey(code))
				{
					m_mapPuaCodeData.Add(code, xaData.Value);
					m_fDirty = true;
				}
			}
		}

	}
}
