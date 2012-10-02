//-------------------------------------------------------------------------------------------------
// <copyright file="ResIdGen.cs" company="Microsoft">
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
// Entry point for the ResIdGen application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Tools
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Reflection;

	/// <summary>
	/// Entry point into the ResIdGen application, which generates an enumeration from a
	/// Resource.h header file into a predefined class which has been marked with the
	/// // ##AUTOGENERATE HERE## marker.
	/// </summary>
	public sealed class ResIdGen
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		public static readonly string StartAutoGenerateTag = "// ##START AUTOGENERATE##";
		public static readonly string EndAutoGenerateTag = "// ##END AUTOGENERATE##";

		private string resourceFile;
		private string sourceFile;
		private string destinationFile;
		private StreamReader resourceReader;
		private StreamReader sourceReader;
		private StreamWriter writer;
		private bool showHelp;
		private bool showLogo = true;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="ResIdGen"/> class.
		/// </summary>
		private ResIdGen()
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
		/// <returns>One of the <see cref="ReturnValue"/> enumeration values.</returns>
		[STAThread]
		public static int Main(string[] args)
		{
			ResIdGen application = new ResIdGen();
			return (int)application.Run(args);
		}

		/// <summary>
		/// Parses the command line arguments.
		/// </summary>
		/// <param name="args">An array of command line parameters.</param>
		/// <returns>One of the <see cref="ReturnValue"/> enumeration values.</returns>
		private ReturnValue ParseCommandLine(string[] args)
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

						default:
							this.showHelp = true;
							return ReturnValue.InvalidParameters;
					}
				}
				else
				{
					// The first non '-' or '/' parameter is the header file, the second is the source file,
					// and the third is the destination file.
					if (this.resourceFile == null)
					{
						this.resourceFile = arg;
					}
					else if (this.sourceFile == null)
					{
						this.sourceFile = arg;
					}
					else
					{
						this.destinationFile = arg;
					}
				}
			}

			// By this point the required arguments should have been supplied (parsed).
			if (this.resourceFile == null || this.resourceFile.Length == 0 ||
				this.sourceFile == null || this.sourceFile.Length == 0 ||
				this.destinationFile == null || this.destinationFile.Length == 0)
			{
				this.showHelp = true;
				return ReturnValue.InvalidParameters;
			}

			return ReturnValue.Success;
		}

		/// <summary>
		/// Prints the help and usage to the console.
		/// </summary>
		private void PrintHelp()
		{
			Console.WriteLine("Reads resources Ids from a .h file and inserts them into a .cs file.");
			Console.WriteLine("The .cs file must be marked with '{0}' and '{1}'.", StartAutoGenerateTag, EndAutoGenerateTag);
			Console.WriteLine();
			Console.WriteLine(" usage: ResIdGen.exe [-?] [-nologo] resourceFile sourceFile destinationFile");
			Console.WriteLine("   -nologo          skip printing ResIdGen logo information");
			Console.WriteLine("   -?               shows this help information");
			Console.WriteLine("   resourceFile     the Resource.h file to read");
			Console.WriteLine("   sourceFile       the source .cs file to process");
			Console.WriteLine("   destinationFile  the destination .cs file to insert the resource ids into");
			Console.WriteLine();
		}

		/// <summary>
		/// Prints the logo information to the console.
		/// </summary>
		private void PrintLogo()
		{
			Assembly thisAssembly = Assembly.GetExecutingAssembly();

			Console.WriteLine("Microsoft (R) Resource Id Generator version {0}", thisAssembly.GetName().Version.ToString());
			Console.WriteLine("Copyright (C) Microsoft Corporation 2003. All rights reserved.");
			Console.WriteLine();
		}

		/// <summary>
		/// Runs the application
		/// </summary>
		/// <param name="args">An array of command line parameters.</param>
		/// <returns>One of the <see cref="ReturnValue"/> enumeration values.</returns>
		private ReturnValue Run(string[] args)
		{
			ReturnValue returnValue = this.ParseCommandLine(args);

			if (this.showLogo)
			{
				this.PrintLogo();
			}
			if (this.showHelp)
			{
				this.PrintHelp();
				return returnValue;
			}

			if (returnValue != ReturnValue.Success)
			{
				return returnValue;
			}

			// Check to make sure the files exist.
			returnValue = this.CheckFiles();
			if (returnValue != ReturnValue.Success)
			{
				return returnValue;
			}

			// Attempt to open the files.
			returnValue = OpenFiles();
			if (returnValue != ReturnValue.Success)
			{
				return returnValue;
			}

			try
			{
				using (this.resourceReader)
				using (this.sourceReader)
				using (this.writer)
				{
					// Get the resource sourceReader in the right spot.
					string resourceLine;
					while ((resourceLine = this.resourceReader.ReadLine()) != null && !resourceLine.StartsWith("#define ID"))
					{
						// Do nothing in the loop; we've already tested the condition.
					}

					// Get the template file sourceReader in the right spot.
					string sourceLine;
					while ((sourceLine = this.sourceReader.ReadLine()) != null && sourceLine.IndexOf(StartAutoGenerateTag) < 0)
					{
						this.writer.WriteLine(sourceLine);
					}
					if (sourceLine == null)
					{
						return ReturnValue.NoStartAutoGenerateTagFound;
					}

					// Get the spaces/tabs in front of the tag so that we can properly indent.
					string spaces = sourceLine.Substring(0, sourceLine.IndexOf(StartAutoGenerateTag));

					// Write out the StartTag so we can regenerate the next time around.
					this.writer.WriteLine(sourceLine);
					// Skip over the existing id definitions.
					while ((sourceLine = this.sourceReader.ReadLine()) != null && sourceLine.IndexOf(EndAutoGenerateTag) < 0)
					{
					}
					if (sourceLine == null)
					{
						return ReturnValue.NoEndAutoGenarateTagFound;
					}

					// Ok, now we're ready to loop through each line of the resource file and define an Id
					// in the enumeration.
					do
					{
						int idStart = resourceLine.IndexOf("ID");
						int idEnd = resourceLine.IndexOf(" ", idStart + 1);
						int valStart = resourceLine.LastIndexOf(" ") + 1;
						string id = resourceLine.Substring(idStart, idEnd - idStart);
						string value = resourceLine.Substring(valStart);

						this.writer.WriteLine("{0}/// <summary>{1}</summary>", spaces, id);
						this.writer.WriteLine("{0}{1} = {2},", spaces, id, value);
						resourceLine = this.resourceReader.ReadLine();
					} while (resourceLine != null && resourceLine.StartsWith("#define ID"));

					// Write the end tag so we can regenerate on this file again.
					this.writer.WriteLine("{0}{1}", spaces, EndAutoGenerateTag);

					// Finish writing the rest of the template file to the temp file.
					this.writer.Write(this.sourceReader.ReadToEnd());
				}
			}
			catch (IOException e)
			{
				Console.WriteLine("There was an error while trying to read or write to or from the files: {0}", e.Message);
				Console.WriteLine(e);
				return ReturnValue.FileReadWriteError;
			}
			catch (Exception e)
			{
				Console.WriteLine("An unknown error occurred: {0}", e.Message);
				Console.WriteLine(e);
				return ReturnValue.UnknownError;
			}

			return ReturnValue.Success;
		}

		/// <summary>
		/// Verifies that the source and destination files exist.
		/// </summary>
		/// <returns>One of the <see cref="ReturnValue"/> enumeration values.</returns>
		private ReturnValue CheckFiles()
		{
			if (!File.Exists(this.resourceFile))
			{
				Console.WriteLine("Resource header file '{0}' does not exist.", this.resourceFile);
				return ReturnValue.ResourceFileNotFound;
			}
			if (!File.Exists(this.sourceFile))
			{
				Console.WriteLine("Source file '{0}' does not exist.", this.sourceFile);
				return ReturnValue.SourceFileNotFound;
			}
			return ReturnValue.Success;
		}

		/// <summary>
		/// Opens the source and destination files.
		/// </summary>
		/// <returns>One of the <see cref="ReturnValue"/> enumeration values.</returns>
		private ReturnValue OpenFiles()
		{
			try
			{
				this.resourceReader = new StreamReader(this.resourceFile);
				this.sourceReader = new StreamReader(this.sourceFile);
				this.writer = new StreamWriter(this.destinationFile, false);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error in opening files: {0}", e.Message);
				Console.WriteLine(e);
				return ReturnValue.FileOpenError;
			}
			return ReturnValue.Success;
		}
		#endregion
	}
}
