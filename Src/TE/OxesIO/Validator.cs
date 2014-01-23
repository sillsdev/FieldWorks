// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Validator.cs
// Responsibility:
//
// <remarks>
// Although this was developed for use in FieldWorks, it doesn't depend on any FieldWorks
// specific classes, so it should be usable by other projects.
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using Commons.Xml.Relaxng;
using SIL.Utils;

namespace SIL.OxesIO
{
	/// <summary>
	/// This static class has methods for validating an OXES file against an embedded RelaxNG
	/// schema.
	/// </summary>
	public class Validator
	{
		/// <summary>
		/// Get the first validation error (if any) for the given file, return the error
		/// message.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		static public string GetAnyValidationErrors(string path)
		{
			using (TextReader reader = FileUtils.OpenFileForRead(path, Encoding.UTF8))
			{
				using (XmlTextReader documentReader = new XmlTextReader(reader))
					return GetAnyValidationErrors(documentReader);
			}
		}

		/// <summary>
		/// Get the first validation error for the given XML document, converting an error in
		/// xmlns (namespace) attribute to a version problem, since that's where the version
		/// number is set.  Returns null if there are no errors.
		/// </summary>
		/// <param name="documentReader"></param>
		/// <returns>string describing first validation error, or null if there are none</returns>
		static public string GetAnyValidationErrors(XmlTextReader documentReader)
		{
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIL.OxesIO.oxes.rng"))
			using (XmlTextReader rngReader = new XmlTextReader(stream))
			using (RelaxngValidatingReader reader = new RelaxngValidatingReader(documentReader, rngReader))
			{
				//RelaxngValidatingReader reader = new RelaxngValidatingReader(documentReader,
				//    new XmlTextReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("SIL.OxesIO.oxes.rng")));
				reader.ReportDetails = true;
				string lastGuy = "oxes";
				try
				{
					while (!reader.EOF)
					{
						//Debug.WriteLine(reader.Value);
						reader.Read();
						lastGuy = reader.Name;
					}
				}
				catch (Exception e)
				{
					string xmlns = null;
					if (reader.Name == "oxes")
						xmlns = reader.GetAttribute("xmlns");
					else if (reader.Name == "xmlns" && (lastGuy == "oxes" || lastGuy == ""))
						xmlns = reader.Value;
					if (String.IsNullOrEmpty(xmlns))
					{
						return String.Format(OxesIOStrings.ksErrorNear, e.Message, lastGuy, reader.Name, reader.Value);
					}
					else
					{
						string sVersion;
						int idx = xmlns.IndexOf("version_");
						if (idx >= 0)
							sVersion = xmlns.Substring(idx + 8);
						else
							sVersion = xmlns;
						return String.Format(OxesIOStrings.ksWrongVersion, sVersion, OxesVersion);
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Get the version of OXES supported by this version of the software.
		/// </summary>
		public static string OxesVersion
		{
			get
			{
				return "1.1.4";
			}
		}

		/// <summary>
		/// Get the first validation error (if any) in the given file, throwing an exception if
		/// one occurs.
		/// </summary>
		/// <param name="pathToOxesFile"></param>
		public static void CheckOxesWithPossibleThrow(string pathToOxesFile)
		{
			string errors = GetAnyValidationErrors(pathToOxesFile);
			if (!String.IsNullOrEmpty(errors))
			{
				errors = string.Format(OxesIOStrings.ksDoesNotConform, pathToOxesFile, errors);
				throw new ApplicationException(errors);
			}
		}

		/// <summary>
		/// Get the OXES version of the file by interpreting the xmlns attribute.
		/// </summary>
		/// <param name="pathToOxes"></param>
		/// <returns></returns>
		public static string GetOxesVersion(string pathToOxes)
		{
			string oxesVersionOfRequestedFile = String.Empty;
			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.ValidationType = ValidationType.None;
			readerSettings.IgnoreComments = true;
			// The first element should look like
			// <oxes xmlns="http://www.wycliffe.net/scripture/namespace/version_1.0.8">
			using (TextReader txtReader = FileUtils.OpenFileForRead(pathToOxes, Encoding.UTF8))
			using (XmlReader reader = XmlReader.Create(txtReader, readerSettings))
			{
				if (reader.IsStartElement("oxes"))
				{
					string xmlns = reader.GetAttribute("xmlns");
					if (!String.IsNullOrEmpty(xmlns))
					{
						int idx = xmlns.IndexOf("version_");
						if (idx >= 0)
							oxesVersionOfRequestedFile = xmlns.Substring(idx + 8);
					}
				}
			}
			if (String.IsNullOrEmpty(oxesVersionOfRequestedFile))
			{
				throw new ApplicationException(String.Format(OxesIOStrings.ksNotOxesFile, pathToOxes));
			}
			return oxesVersionOfRequestedFile;
		}
	}
}
