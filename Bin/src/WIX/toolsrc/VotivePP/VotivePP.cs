//-------------------------------------------------------------------------------------------------
// <copyright file="VotivePP.cs" company="Microsoft">
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
// Entry point for the VotivePP application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Tools
{
	using System;
	using System.Collections;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Reflection;

	/// <summary>
	/// Entry point into the VotivePP application, which takes a template file and
	/// replaces directory path placeholders with actual values.
	/// </summary>
	/// <remarks>
	/// This is pretty sloppy code. It's meant to just be a build tool.
	/// </remarks>
	public sealed class VotivePP
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private string sourceFile;
		private string destinationFile;
		private Hashtable placeholderValueEntries = new Hashtable();
		private bool showHelp;
		private bool showLogo = true;
		private bool usePathBackslashes = false;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="VotivePP"/> class.
		/// </summary>
		private VotivePP()
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Main entry point for the application.
		/// </summary>
		/// <param name="args">An array of command line parameters.</param>
		/// <returns>One of the <see cref="VotivePPReturnValue"/> enumeration values.</returns>
		[STAThread]
		public static int Main(string[] args)
		{
			VotivePP application = new VotivePP();
			return (int)application.Run(args);
		}

		/// <summary>
		/// Parses the command line arguments.
		/// </summary>
		/// <param name="args">An array of command line parameters.</param>
		/// <returns>One of the <see cref="VotivePPReturnValue"/> enumeration values.</returns>
		private VotivePPReturnValue ParseCommandLine(string[] args)
		{
			foreach (string arg in args)
			{
				// Skip blank arguments.
				if (arg == null || arg.Length == 0)
				{
					continue;
				}

				if (arg[0] == '-' || arg[0] == '/')
				{
					string parameter = arg.Substring(1).ToLower(CultureInfo.InvariantCulture);
					switch (parameter)
					{
						case "?":
							this.showHelp = true;
							break;

						case "nologo":
							this.showLogo = false;
							break;

						case "bs":
							this.usePathBackslashes = true;
							break;

						default:
							this.showHelp = true;
							return VotivePPReturnValue.InvalidParameters;
					}
				}
				else
				{
					// The first non '-' or '/' parameter is the source file, the second is the destination file.
					if (this.sourceFile == null)
					{
						this.sourceFile = arg;
					}
					else if (this.destinationFile == null)
					{
						this.destinationFile = arg;
					}
					else
					{
						// Get a placeholder=value entry.
						int equalPos = arg.IndexOf("=");
						if (equalPos < 0)
						{
							StringBuilder commandLine = new StringBuilder("VotivePP.exe ", args.Length * 100);
							for (int j = 0; j < args.Length; j++)
							{
								commandLine.AppendFormat("\"{0}\"", args[j]);
							}
							Console.WriteLine("Placeholder arguments must be in the form 'placeholder=value': {0}. Command Line: {1}", arg, commandLine);
							return VotivePPReturnValue.InvalidPlaceholderParam;
						}
						string placeholder = String.Concat("%", arg.Substring(0, equalPos), "%");
						string value = arg.Substring(equalPos + 1);
						this.placeholderValueEntries[placeholder] = value;
					}
				}
			}

			// By this point the required arguments should have been supplied (parsed).
			if (this.sourceFile == null || this.sourceFile.Length == 0 ||
				this.destinationFile == null || this.destinationFile.Length == 0)
			{
				this.showHelp = true;
				return VotivePPReturnValue.InvalidParameters;
			}

			return VotivePPReturnValue.Success;
		}

		/// <summary>
		/// Prints the help and usage to the console.
		/// </summary>
		private void PrintHelp()
		{
			Console.WriteLine("Specialized build preprocessor for use in the Votive project.");
			Console.WriteLine();
			Console.WriteLine(" usage: VotivePP.exe [-?] [-nologo] [-bs] sourceFile destinationFile placehoder=value[,placeholder=value]");
			Console.WriteLine("   -nologo   skip printing ResIdGen logo information");
			Console.WriteLine("   -?        shows this help information");
			Console.WriteLine("   -bs       replace paths with double backslashes (used for a .reg file)");
		}

		/// <summary>
		/// Prints the logo information to the console.
		/// </summary>
		private void PrintLogo()
		{
			Assembly thisAssembly = Assembly.GetExecutingAssembly();

			Console.WriteLine("Microsoft (R) Votive Build Preprocessor version {0}", thisAssembly.GetName().Version.ToString());
			Console.WriteLine("Copyright (C) Microsoft Corporation 2003. All rights reserved.");
			Console.WriteLine();
		}

		private VotivePPReturnValue Run(string[] args)
		{
			VotivePPReturnValue returnValue = this.ParseCommandLine(args);

			if (this.showLogo)
			{
				this.PrintLogo();
			}
			if (this.showHelp)
			{
				this.PrintHelp();
				return returnValue;
			}

			if (returnValue != VotivePPReturnValue.Success)
			{
				return returnValue;
			}

			// Check to make sure the source exists.
			if (!File.Exists(this.sourceFile))
			{
				Console.WriteLine("Source file '{0}' does not exist.", sourceFile);
				return VotivePPReturnValue.SourceFileNotFound;
			}

			StringBuilder sourceText;
			try
			{
				// Open the source file.
				using (StreamReader reader = new StreamReader(sourceFile))
				{
					string entireSource = reader.ReadToEnd();
					// Leave some room for the replacements (about 25% extra).
					sourceText = new StringBuilder(entireSource, (int)(entireSource.Length * 1.25));
				}

				// Loop through all of the placeholders to replace the text in the source.
				foreach (DictionaryEntry entry in this.placeholderValueEntries)
				{
					string placeholder = (string)entry.Key;
					string replacement = (string)entry.Value;

					// A registry (.reg) file requires double backslashes for paths, so
					// change the replacement values to reflect that. This is kind of
					// kludgy, but this app is not meant to be general purpose. It's
					// just a build utility.
					if (this.usePathBackslashes &&
						(placeholder.IndexOf("DIR") >= 0 || placeholder.IndexOf("PATH") >= 0))
					{
						replacement = replacement.Replace(@"\", @"\\");
					}

					sourceText = sourceText.Replace(placeholder, replacement);
				}

				// Replace any special folders.
				string sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
				if (usePathBackslashes)
				{
					sysDir = sysDir.Replace(@"\", @"\\");
				}
				sourceText = sourceText.Replace("%SYSDIR%", sysDir);

				// Now it's time to write the text to the destination file.
				using (StreamWriter writer = new StreamWriter(this.destinationFile, false))
				{
					writer.Write(sourceText.ToString());
				}
			}
			catch (IOException e)
			{
				Console.WriteLine("Error in reading the source file: {0}", e.Message);
				Console.WriteLine(e);
				return VotivePPReturnValue.FileReadError;
			}
			catch (Exception e)
			{
				Console.WriteLine("Unknown error: {0}", e.Message);
				Console.WriteLine(e);
				return VotivePPReturnValue.UnknownError;
			}

			return VotivePPReturnValue.Success;
		}
		#endregion
	}
}
