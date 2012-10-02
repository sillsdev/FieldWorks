//-------------------------------------------------------------------------------------------------
// <copyright file="dark.cs" company="Microsoft">
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
// The dark decompiler application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
	using System;
	using System.IO;
	using System.Globalization;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Collections.Specialized;

	/// <summary>
	/// Entry point for decompiler
	/// </summary>
	public class Dark
	{
		private StringCollection extensionList;

		/// <summary>
		/// input file
		/// </summary>
		private string inputFile;

		/// <summary>
		/// output file
		/// </summary>
		private string outputFile;

		/// <summary>
		/// show the logo
		/// </summary>
		private bool showLogo;

		/// <summary>
		/// show help
		/// </summary>
		private bool showHelp;

		/// <summary>
		/// skip ui
		/// </summary>
		private bool skipUI;

		/// <summary>
		/// skip VSI specific elements
		/// </summary>
		private bool skipVSI;

		/// <summary>
		/// skip InstallShield specific elements
		/// </summary>
		private bool skipInstallShield;

		/// <summary>
		/// clean up the extract directory
		/// </summary>
		private bool tidy;

		/// <summary>
		/// process just the ui
		/// </summary>
		private bool processUIOnly;

		/// <summary>
		/// skip sequence tables
		/// </summary>
		private bool skipSequenceTables;

		/// <summary>
		/// explicit sequence tables
		/// </summary>
		private bool explicitSequenceTables;

		 /// <summary>
		/// skip summary info
		/// </summary>
		private bool skipSummaryInfo;

		/// <summary>
		/// export binaries
		/// </summary>
		private bool exportBinaries;

		/// <summary>
		/// processing merge module
		/// </summary>
		private bool processingModule;

		/// <summary>
		/// processing fragment
		/// </summary>
		private bool processingFragment;

		/// <summary>
		/// generate fragments
		/// </summary>
		private bool generateFragments;

		/// <summary>
		/// keep empty tables
		/// </summary>
		private bool keepEmptyTables;

		/// <summary>
		/// add comments
		/// </summary>
		private bool addComments;

		/// <summary>
		/// base path for exporting files
		/// </summary>
		private string basePath;

		/// <summary>
		/// message handling
		/// </summary>
		private MessageHandler messageHandler;

		/// <summary>
		/// Instantiate a new Dark class.
		/// </summary>
		private Dark()
		{
			this.showLogo = true;
			this.tidy = true;
			this.messageHandler = new MessageHandler("DARK", "dark.exe");
			this.extensionList = new StringCollection();

			// set the message handler
			this.Message += new MessageEventHandler(this.messageHandler.Display);
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		private event MessageEventHandler Message;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// <param name="args">Arguments to decompiler.</param>
		/// <returns>0 if sucessful, otherwise 1.</returns>
		public static int Main(string[] args)
		{
			Dark dark = new Dark();
			return dark.Run(args);
		}

		/// <summary>
		/// Main running method for the application.
		/// </summary>
		/// <param name="args">Commandline arguments to the application.</param>
		/// <returns>Returns the application error code.</returns>
		private int Run(string[] args)
		{
			try
			{
				// parse the command line
				this.ParseCommandLine(args);

				// exit if there was an error parsing the command line (otherwise the logo appears after error messages)
				if (this.messageHandler.FoundError)
				{
					return this.messageHandler.PostProcess();
				}

				if (null == this.inputFile)
				{
					this.showHelp = true;
				}

				if (this.showLogo)
				{
					Assembly darkAssembly = Assembly.GetExecutingAssembly();

					Console.WriteLine("Microsoft (R) Windows Installer Xml Decompiler Version {0}", darkAssembly.GetName().Version.ToString());
					Console.WriteLine("Copyright (C) Microsoft Corporation 2003. All rights reserved.\n");
					Console.WriteLine();
				}

				if (this.showHelp)
				{
					Console.WriteLine(" usage: dark [-?] [-x basePath] msiFileName xmlFileName");
					Console.WriteLine();
					Console.WriteLine("   --Convert a Windows Installer database to an XML installation manifest --");
					Console.WriteLine("   1st argument is path to the MSI database to query");
					Console.WriteLine("   2nd argument is path to the XML manifest to create");
					Console.WriteLine();
					Console.WriteLine("   case-insensitive option arguments may be specified in any order:");
					Console.WriteLine("     /s or -s               exclude standard sequence actions, process Custom and dialogs");
					Console.WriteLine("     /p or -p               exclude Package element generation from summary information");
					Console.WriteLine("     /n or -n               no UI elements processed");
					Console.WriteLine("     /ui or -ui             only UI elements processed");
					Console.WriteLine("     /ext                   extension (class, assembly), should extend CompilerExtension");
					Console.WriteLine("     /x <path> or -x <path> export binary streams from Binary and Icon tables to path");
					Console.WriteLine("     /m or -m               merge module");
					Console.WriteLine("     /notidy or -notidy     Do not delete temporary files (for checking results)");
					Console.WriteLine("     /e or -e               include empty tables");
					Console.WriteLine("     /f or -f               generate many fragment files via internal heuristic");
					Console.WriteLine("     /g or -g               generate single monolithic fragment");
					Console.WriteLine("     /wx or -wx             treat warnings as errors");
					Console.WriteLine("     /w<N> or -w<N>         set the warning level (0: show all, 3: show none)");
					Console.WriteLine("     /sw or -sw             suppress all warnings (same as -w3)");
					Console.WriteLine("     /sw<N> or -sw<N>       suppress warning with specific message ID");
					Console.WriteLine("     /v or -v               verbose output (same as -v2)");
					Console.WriteLine("     /v<N> or -v<N>         sets the level of verbose output (0: most output, 3: none)");
					Console.WriteLine("     /vsi or -vsi           filter out problematic Visual Studio Installer constructs");
					Console.WriteLine("     /is or -is             filter out problematic InstallShield constructs");
					Console.WriteLine("     /nologo or -nologo     skip printing dark logo information");
					Console.WriteLine("     /z or -z               write explicit sequence numbers (no relative references)");
					Console.WriteLine("     /? or -?               show this help info, same as if no arguments supplied");
					Console.WriteLine();
					Console.WriteLine();
					Console.WriteLine(" CONVERTING FROM MSI to WiX");
					Console.WriteLine();
					Console.WriteLine("    In general, WiX does a good job of validating your Xml source code.");
					Console.WriteLine("    Therefore, you will encounter logical errors -- invalid attributes, for");
					Console.WriteLine("    example -- that were ignored by MSI but will need to be fixed for WiX. ");
					Console.WriteLine();

					return this.messageHandler.PostProcess();
				}

				// print friendly message saying what file is being compiled
				Console.WriteLine(this.inputFile);

				// create the decompiler
				Decompiler decompiler = new Decompiler();

				// load any extensions
				foreach (string extension in this.extensionList)
				{
					Type extensionType = Type.GetType(extension);
					if (null == extensionType)
					{
						throw new WixInvalidExtensionException(extension);
					}

					if (extensionType.IsSubclassOf(typeof(DecompilerExtension)))
					{
						DecompilerExtension decompilerExtensionObject = Activator.CreateInstance(extensionType) as DecompilerExtension;
						decompiler.AddExtension(decompilerExtensionObject);
					}

					if (!extensionType.IsSubclassOf(typeof(DecompilerExtension)))
					{
						throw new WixInvalidExtensionException(extension);
					}
				}

				// Set the Properties
				decompiler.SkipUI = this.skipUI;
				decompiler.SkipVSI = this.skipVSI;
				decompiler.SkipInstallShield = this.skipInstallShield;
				decompiler.ProcessUIOnly = this.processUIOnly;
				decompiler.SkipSequenceTables = this.skipSequenceTables;
				decompiler.ExplicitSequenceTables = this.explicitSequenceTables;
				decompiler.SkipSummaryInfo = this.skipSummaryInfo;
				decompiler.ExportBinaries = this.exportBinaries;
				decompiler.IsMergeModule = this.processingModule;
				decompiler.IsFragmentContainer = this.processingFragment;
				decompiler.GenerateFragments = this.generateFragments;
				decompiler.KeepEmptyTables = this.keepEmptyTables;
				decompiler.AddComments = this.addComments;
				decompiler.ExportBasePath = this.basePath;
				decompiler.Tidy = this.tidy;
				decompiler.Message += new MessageEventHandler(this.messageHandler.Display);

				// and now we do what we came here to do...
				decompiler.Decompile(this.inputFile, this.outputFile);
			}
			catch (WixException we)
			{
				// TODO: once all WixExceptions are converted to errors, this clause
				// should be a no-op that just catches WixFatalErrorException's
				this.messageHandler.Display("dark.exe", "DARK", we);
				return 1;
			}
			catch (Exception e)
			{
				this.OnMessage(WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
				if (e is NullReferenceException || e is SEHException)
				{
					throw;
				}
			}

			return this.messageHandler.PostProcess();
		}

		/// <summary>
		/// Parse the commandline arguments.
		/// </summary>
		/// <param name="args">Commandline arguments.</param>
		private void ParseCommandLine(string[] args)
		{
			for (int i = 0; i < args.Length; ++i)
			{
				string arg = args[i];
				if (null == arg || 0 == arg.Length) // skip blank arguments
				{
					continue;
				}

				if ('-' == arg[0] || '/' == arg[0])
				{
					string parameter = arg.Substring(1);
					if ("o" == parameter || "out" == parameter)
					{
						if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
						{
							this.OnMessage(WixErrors.FilePathRequired(String.Concat("-", parameter)));
							return;
						}

						this.outputFile = args[i];
					}
					else if ("vsi" == parameter)
					{
						this.skipVSI = true;
					}
					else if ("is" == parameter)
					{
						this.skipInstallShield = true;
					}
					else if ("n" == parameter)
					{
						this.skipUI = true;
					}
					else if ("ui" == parameter)
					{
						this.processUIOnly = true;
					}
					else if ("s" == parameter)
					{
						this.skipSequenceTables = true;
					}
					else if ("z" == parameter)
					{
						this.explicitSequenceTables = true;
					}
					else if ("p" == parameter)
					{
						this.skipSummaryInfo = true;
					}
					else if ("x" == parameter)
					{
						this.exportBinaries = true;
						if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
						{
							this.OnMessage(WixErrors.DirectoryPathRequired(String.Concat("-", parameter)));
							return;
						}

						this.basePath = args[i];
					}
					else if ("m" == parameter)
					{
						this.processingModule = true;
					}
					else if ("f" == parameter)
					{
						this.generateFragments = true;
					}
					else if ("e" == parameter)
					{
						this.keepEmptyTables = true;
					}
					else if ("ext" == parameter)
					{
						if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
						{
							this.OnMessage(WixErrors.TypeSpecificationForExtensionRequired("-ext"));
							return;
						}

						this.extensionList.Add(args[i]);
					}
					else if ("g" == parameter)
					{
						this.processingFragment = true;
					}
					else if ("c" == parameter)
					{
						this.addComments = true;
					}
					else if ("sw" == parameter)
					{
						this.messageHandler.MinimumWarningLevel = WarningLevel.Deprecated;
					}
					else if ('s' == parameter[0] && 'w' == parameter[1])
					{
						try
						{
							int suppressWarning = Convert.ToInt32(parameter.Substring(2), CultureInfo.InvariantCulture.NumberFormat);

							if (0 >= suppressWarning)
							{
								this.OnMessage(WixErrors.IllegalSuppressWarningId(parameter.Substring(2)));
							}

							this.messageHandler.SuppressWarningMessage(suppressWarning);
						}
						catch (FormatException)
						{
							this.OnMessage(WixErrors.IllegalSuppressWarningId(parameter.Substring(2)));
						}
						catch (OverflowException)
						{
							this.OnMessage(WixErrors.IllegalSuppressWarningId(parameter.Substring(2)));
						}
					}
					else if ('v' == parameter[0])
					{
						if (parameter.Length > 1)
						{
							parameter = arg.Substring(2);

							try
							{
								int userVerbosityLevel = Convert.ToInt32(parameter, CultureInfo.InvariantCulture.NumberFormat);

								if (0 > userVerbosityLevel || 3 < userVerbosityLevel)
								{
									this.OnMessage(WixErrors.IllegalVerbosityLevel(parameter));
									continue;
								}

								this.messageHandler.MaximumVerbosityLevel = (VerboseLevel)userVerbosityLevel;
							}
							catch (FormatException)
							{
								this.OnMessage(WixErrors.IllegalVerbosityLevel(parameter));
							}
							catch (OverflowException)
							{
								this.OnMessage(WixErrors.IllegalVerbosityLevel(parameter));
							}
						}
						else // verbosity level not specified; use default
						{
							this.messageHandler.MaximumVerbosityLevel = VerboseLevel.Verbose;
						}
					}
					else if ('w' == parameter[0])
					{
						parameter = arg.Substring(2);

						if ("x" == parameter)
						{
							this.messageHandler.WarningAsError = true;
						}
						else
						{
							try
							{
								int userWarningLevel = Convert.ToInt32(parameter, CultureInfo.InvariantCulture.NumberFormat);

								if (0 > userWarningLevel || 3 < userWarningLevel)
								{
									this.OnMessage(WixErrors.IllegalWarningLevel(parameter));
									continue;
								}

								this.messageHandler.MinimumWarningLevel = (WarningLevel)userWarningLevel;
							}
							catch (FormatException)
							{
								this.OnMessage(WixErrors.IllegalWarningLevel(parameter));
							}
							catch (OverflowException)
							{
								this.OnMessage(WixErrors.IllegalWarningLevel(parameter));
							}
						}
					}
					else if ("?" == parameter || "help" == parameter)
					{
						this.showHelp = true;
					}
					else if ("nologo" == parameter)
					{
						this.showLogo = false;
					}
					else if ("notidy" == parameter)
					{
						this.tidy = false;
					}
				}
				else
				{
					if (null == this.inputFile || 0 == this.inputFile.Length)
					{
						this.inputFile = arg;
					}
					else if (null == this.outputFile || 0 == this.outputFile.Length)
					{
						this.outputFile = arg;
					}
					else
					{
						this.OnMessage(WixErrors.AdditionalArgumentUnexpected(arg));
					}
				}
			}
		}

		/// <summary>
		/// Sends a message to the message delegate if there is one.
		/// </summary>
		/// <param name="mea">Message event arguments.</param>
		private void OnMessage(MessageEventArgs mea)
		{
			if (null != this.Message)
			{
				this.Message(this, mea);
			}
		}
	}
}
