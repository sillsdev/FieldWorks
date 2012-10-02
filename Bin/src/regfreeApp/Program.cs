// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Program.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using SIL.FieldWorks.Build.Tasks;

namespace SIL.FieldWorks.src.regfreeApp
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class Program
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Creates manifest files so that exe can be run without COM " +
					"needed to be registered in the registry.");
				Console.WriteLine();
				Console.WriteLine("Usage:");
				Console.WriteLine("regfreeApp <exename>");
				return;
			}

			string executable = args[0];
			if (!File.Exists(executable))
			{
				Console.WriteLine("File {0} does not exist", executable);
				return;
			}


			string manifestFile = executable + ".manifest";
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;

			if (File.Exists(manifestFile))
			{
				using (XmlReader reader = new XmlTextReader(manifestFile))
				{
					try
					{
						reader.MoveToContent();
					}
					catch (XmlException)
					{
					}
					doc.ReadNode(reader);
				}
			}

			RegFreeCreator creator = new RegFreeCreator(doc, false);
			XmlElement root = creator.CreateExeInfo(executable);

			string directory = Path.GetDirectoryName(executable);
			if (directory.Length == 0)
				directory = Directory.GetCurrentDirectory();
			foreach (string fileName in Directory.GetFiles(directory, "*.dll"))
				creator.ProcessTypeLibrary(root, fileName);

			XmlWriterSettings settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = false;
			settings.NewLineOnAttributes = false;
			settings.NewLineChars = Environment.NewLine;
			settings.Indent = true;
			settings.IndentChars = "\t";
			using (XmlWriter writer = XmlWriter.Create(manifestFile, settings))
			{
				doc.WriteContentTo(writer);
			}
		}
	}
}
