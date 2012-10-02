//-------------------------------------------------------------------------------------------------
// <copyright file="WixCop.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Wix source code style inspector and repair utility.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools
{
	using System;
	using System.Collections;
	using System.IO;
	using System.Reflection;
	using System.Xml;

	/// <summary>
	/// The WiXCop application class.
	/// </summary>
	public class WixCop
	{
		private string[] errorsAsWarnings;
		private Hashtable exemptFiles;
		private bool fixErrors;
		private Inspector inspector;
		private ArrayList searchPatterns;
		private string settingsFile1;
		private string settingsFile2;
		private bool showHelp;
		private bool showLogo;
		private bool subDirectories;
		private int indentationAmount;

		/// <summary>
		/// Instantiate a new WixCop class.
		/// </summary>
		private WixCop()
		{
			this.errorsAsWarnings = null;
			this.exemptFiles = new Hashtable();
			this.fixErrors = false;
			this.inspector = null;
			this.searchPatterns = new ArrayList();
			this.settingsFile1 = null;
			this.settingsFile2 = null;
			this.showHelp = false;
			this.showLogo = true;
			this.subDirectories = false;
			this.indentationAmount = 2;
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// <param name="args">The commandline arguments.</param>
		/// <returns>The number of errors that were found.</returns>
		[STAThread]
		public static int Main(string[] args)
		{
			WixCop wixCop = new WixCop();
			return wixCop.Run(args);
		}

		/// <summary>
		/// Get the files that match a search path pattern.
		/// </summary>
		/// <param name="baseDir">The base directory at which to begin the search.</param>
		/// <param name="searchPath">The search path pattern.</param>
		/// <returns>The files matching the pattern.</returns>
		private static string[] GetFiles(string baseDir, string searchPath)
		{
			// convert alternate directory separators to the standard one
			string filePath = searchPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			int lastSeparator = filePath.LastIndexOf(Path.DirectorySeparatorChar);
			string[] files = null;

			try
			{
				if (0 > lastSeparator)
				{
					files = Directory.GetFiles(baseDir, filePath);
				}
				else // found directory separator
				{
					string searchPattern = filePath.Substring(lastSeparator + 1);

					files = Directory.GetFiles(filePath.Substring(0, lastSeparator + 1), searchPattern);
				}
			}
			catch (DirectoryNotFoundException)
			{
				// don't let this function throw the DirectoryNotFoundException. (this exception
				// occurs for non-existant directories and invalid characters in the searchPattern)
			}

			return files;
		}

		/// <summary>
		/// Inspect sub-directories.
		/// </summary>
		/// <param name="directory">The directory whose sub-directories will be inspected.</param>
		/// <returns>The number of errors that were found.</returns>
		private int InspectSubDirectories(string directory)
		{
			int errors = 0;

			foreach (string searchPattern in this.searchPatterns)
			{
				foreach (string sourceFile in GetFiles(directory, searchPattern))
				{
					FileInfo file = new FileInfo(sourceFile);

					if (!this.exemptFiles.Contains(file.Name.ToLower()))
					{
						errors += this.inspector.InspectFile(file.FullName, this.fixErrors);
					}
				}
			}

			if (this.subDirectories)
			{
				foreach (string childDirectory in Directory.GetDirectories(directory))
				{
					errors += this.InspectSubDirectories(childDirectory);
				}
			}

			return errors;
		}

		/// <summary>
		/// Run the application with the given arguments.
		/// </summary>
		/// <param name="args">The commandline arguments.</param>
		/// <returns>The number of errors that were found.</returns>
		private int Run(string[] args)
		{
			try
			{
				this.ParseCommandLine(args);

				if (this.showLogo)
				{
					Assembly wixcopAssembly = Assembly.GetExecutingAssembly();

					Console.WriteLine("Microsoft (R) Windows Installer Xml Cop version {0}", wixcopAssembly.GetName().Version.ToString());
					Console.WriteLine("Copyright (C) Microsoft Corporation 2004. All rights reserved.");
					Console.WriteLine();
				}

				if (this.showHelp)
				{
					Console.WriteLine(" usage:  wixcop.exe sourceFile [sourceFile ...]");
					Console.WriteLine();
					Console.WriteLine("   -f       fix errors automatically for writable files");
					Console.WriteLine("   -nologo  suppress displaying the logo information");
					Console.WriteLine("   -s       search for matching files in current dir and subdirs");
					Console.WriteLine("   -set1<file> primary settings file");
					Console.WriteLine("   -set2<file> secondary settings file (overrides primary)");
					Console.WriteLine("   -indent:<n> indentation multiple (overrides default of 2)");
					Console.WriteLine("   -?       this help information");
					Console.WriteLine();
					Console.WriteLine("   sourceFile may use wildcards like *.wxs");

					return 0;
				}

				// parse the settings if any were specified
				if (null != this.settingsFile1 || null != this.settingsFile2)
				{
					this.ParseSettingsFiles(this.settingsFile1, this.settingsFile2);
				}

				this.inspector = new Inspector(this.errorsAsWarnings, this.indentationAmount);

				int errors = this.InspectSubDirectories(Path.GetFullPath("."));

				return errors != 0 ? 2 : 0;
			}
			catch (Exception e)
			{
				Console.WriteLine("wixcop.exe : fatal error WXCP0001 : {0}\r\n\n\nStack Trace:\r\n{1}", e.Message, e.StackTrace);

				return 1;
			}
		}

		/// <summary>
		/// Parse the primary and secondary settings files.
		/// </summary>
		/// <param name="settingsFile1">The primary settings file.</param>
		/// <param name="settingsFile2">The secondary settings file.</param>
		private void ParseSettingsFiles(string settingsFile1, string settingsFile2)
		{
			if (null == settingsFile1 && null != settingsFile2)
			{
				throw new ArgumentException("Cannot specify a secondary settings file (set2) without a primary settings file (set1).", "set2");
			}

			Hashtable errorsAsWarningsHash = new Hashtable();
			string settingsFile = settingsFile1;
			do
			{
				XmlTextReader reader = null;
				try
				{
					reader = new XmlTextReader(settingsFile);
					XmlDocument doc = new XmlDocument();
					doc.Load(reader);

					// get the types of tests that will have their errors displayed as warnings
					XmlNodeList testsAsWarningsElements = doc.SelectNodes("/Settings/ErrorsAsWarnings/Test");
					foreach (XmlNode test in testsAsWarningsElements)
					{
						errorsAsWarningsHash[((XmlElement)test).GetAttribute("Id")] = null;
					}

					// get the exempt files
					XmlNodeList exemptFiles = doc.SelectNodes("/Settings/ExemptFiles/File");
					foreach (XmlNode file in exemptFiles)
					{
						this.exemptFiles[((XmlElement)file).GetAttribute("Name").ToLower()] = null;
					}
				}
				finally
				{
					if (null != reader)
					{
						reader.Close();
					}
				}

				settingsFile = settingsFile2;
				settingsFile2 = null;
			}
			while (null != settingsFile);

			// copy the settings to a nice string array
			this.errorsAsWarnings = new string[errorsAsWarningsHash.Keys.Count];
			errorsAsWarningsHash.Keys.CopyTo(this.errorsAsWarnings, 0);
		}

		/// <summary>
		/// Parse the commandline arguments.
		/// </summary>
		/// <param name="args">The commandline arguments.</param>
		private void ParseCommandLine(string[] args)
		{
			for (int i = 0; i < args.Length; ++i)
			{
				string arg = args[i];
				if (null == arg || string.Empty == arg) // skip blank arguments
				{
					continue;
				}

				if ('-' == arg[0] || '/' == arg[0])
				{
					string parameter = arg.Substring(1);

					switch (parameter)
					{
						case "?":
							this.showHelp = true;
							break;
						case "f":
							this.fixErrors = true;
							break;
						case "nologo":
							this.showLogo = false;
							break;
						case "s":
							this.subDirectories = true;
							break;
						default: // other parameters
							if (parameter.StartsWith("set1"))
							{
								this.settingsFile1 = parameter.Substring(4);
							}
							else if (parameter.StartsWith("set2"))
							{
								this.settingsFile2 = parameter.Substring(4);
							}
							else if (parameter.StartsWith("indent:"))
							{
								try
								{
									this.indentationAmount = Int32.Parse(parameter.Substring(7));
								}
								catch
								{
									throw new ArgumentException("Invalid numeric argument.", parameter);
								}
							}
							else
							{
								throw new ArgumentException("Invalid argument.", parameter);
							}
							break;
					}
				}
				else if ('@' == arg[0])
				{
					using (StreamReader reader = new StreamReader(arg.Substring(1)))
					{
						string line;
						ArrayList newArgs = new ArrayList();

						while (null != (line = reader.ReadLine()))
						{
							string newArg = "";
							bool betweenQuotes = false;
							for (int j = 0; j < line.Length; ++j)
							{
								// skip whitespace
								if (!betweenQuotes && (' ' == line[j] || '\t' == line[j]))
								{
									if ("" != newArg)
									{
										newArgs.Add(newArg);
										newArg = null;
									}

									continue;
								}

								// if we're escaping a quote
								if ('\\' == line[j] && j < line.Length - 1 && '"' == line[j + 1])
								{
									++j;
								}
								else if ('"' == line[j])   // if we've hit a new quote
								{
									betweenQuotes = !betweenQuotes;
									continue;
								}

								newArg = String.Concat(newArg, line[j]);
							}

							if ("" != newArg)
							{
								newArgs.Add(newArg);
							}
						}
						string[] ar = (string[])newArgs.ToArray(typeof(string));
						this.ParseCommandLine(ar);
					}
				}
				else
				{
					this.searchPatterns.Add(arg);
				}
			}
		}
	}
}
