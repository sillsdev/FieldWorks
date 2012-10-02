// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlFormatter.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Tool to force a standardized format for XML files.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Reflection;

namespace XmlFormatter
{
	/// <summary>
	/// Summary description for XmlFormatter.
	/// </summary>
	class XmlFormatter
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				if (File.Exists(args[0]))
					FormatFile(args[0]);
				else if (Directory.Exists(args[0]))
					FormatAllFiles(args[0]);
				else
					Console.WriteLine("Could not find a file or directory named {0}!", args[0]);
			}
			else
				Console.WriteLine("No file or directory name given as an argument!  Nothing to do.");
		}

		static private void FormatFile(string sFile)
		{
			try
			{
				if ((File.GetAttributes(sFile) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
				{
					// Make sure it's an xml file
					XmlDocument doc =  new XmlDocument();
					doc.Load(sFile);
					// It is, so now process it.
					using (Process myProcess = new Process())
					{
						string workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6);
						Console.WriteLine("Formatting {0}", sFile);
						myProcess.StartInfo.FileName = Path.Combine(workingDir, @"Xmlformatter\XMLFormat");
						myProcess.StartInfo.Arguments = sFile + " " + workingDir;
						myProcess.StartInfo.CreateNoWindow = true;
						myProcess.Start();
						while (!myProcess.HasExited)
						{} // Wait until it gets done.
						myProcess.Close();
					}
				}
			}
			catch (XmlException xmlerr)
			{
				Console.WriteLine("Error message: {0}.", xmlerr.Message);
				Console.WriteLine("Error: {0} is not an xml file!", sFile);
			}
			catch (Exception err)
			{
				Console.WriteLine("Error: " + err.Message);
			}
		}

		static private void FormatAllFiles(string sDir)
		{
			string[] astrFiles;
			astrFiles = Directory.GetFiles(sDir, "*.xml");
			foreach (string strFile in astrFiles)
			{
				FormatFile(strFile);
			}
			string [] astrDirs;
			astrDirs = Directory.GetDirectories(sDir);
			foreach (string sSubDir in astrDirs)
			{
				FormatAllFiles(sSubDir);
			}
		}
	}
}
