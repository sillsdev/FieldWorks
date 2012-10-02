//-------------------------------------------------------------------------------------------------
// <copyright file="candle.cs" company="Microsoft">
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
// The candle compiler application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Resources;
	using System.Runtime.InteropServices;
	using System.Xml;

	/// <summary>
	/// The main entry point for candle.
	/// </summary>
	public class Candle
	{
		private ArrayList sourceFiles;
		private Hashtable parameters;
		private StringCollection includeSearchPaths;
		private StringCollection extensionList;
		private FileInfo outputFile;
		private DirectoryInfo outputDirectory;
		private bool showLogo;
		private bool showHelp;
		private bool suppressSchema;
		private bool useSmallTables;
		private bool schemaOnly;
		private PedanticLevel pedanticLevel;
		private bool preprocessToStdout;
		private String preprocessFile;
		private MessageHandler messageHandler;

		/// <summary>
		/// Instantiate a new Candle class.
		/// </summary>
		private Candle()
		{
			this.sourceFiles = new ArrayList();
			this.parameters = new Hashtable();
			this.includeSearchPaths = new StringCollection();
			this.extensionList = new StringCollection();
			this.showLogo = true;
			this.pedanticLevel = PedanticLevel.Easy;
			this.messageHandler = new MessageHandler("CNDL", "candle.exe");

			// set the message handler
			this.Message += new MessageEventHandler(this.messageHandler.Display);
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		private event MessageEventHandler Message;

		/// <summary>
		/// The main entry point for candle.
		/// </summary>
		/// <param name="args">Commandline arguments for the application.</param>
		/// <returns>Returns the application error code.</returns>
		[MTAThread]
		public static int Main(string[] args)
		{
			Candle candle = new Candle();
			return candle.Run(args);
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
				FileInfo currentFile = null;

				// parse the command line
				this.ParseCommandLine(args);

				// exit if there was an error parsing the command line (otherwise the logo appears after error messages)
				if (this.messageHandler.FoundError)
				{
					return this.messageHandler.PostProcess();
				}

				if (0 == this.sourceFiles.Count)
				{
					this.showHelp = true;
				}
				else if (1 < this.sourceFiles.Count && null != this.outputFile)
				{
					throw new ArgumentException("cannot specify more than one source file with single output file.  Either specify an output directory for the -out argument by ending the argument with a '\\' or remove the -out argument to have the source files compiled to the current directory.", "-out");
				}

				if (this.showLogo)
				{
					Assembly candleAssembly = Assembly.GetExecutingAssembly();

					Console.WriteLine("Microsoft (R) Windows Installer Xml Compiler version {0}", candleAssembly.GetName().Version.ToString());
					Console.WriteLine("Copyright (C) Microsoft Corporation 2003. All rights reserved.");
					Console.WriteLine();
				}
				if (this.showHelp)
				{
					Console.WriteLine(" usage:  candle.exe [-?] [-nologo] [-out outputFile] sourceFile [sourceFile ...]");
					Console.WriteLine();
					Console.WriteLine("   -d<name>=<value>  define a parameter for the preprocessor");
					Console.WriteLine("   -p<file>  preprocess to a file (or stdout if no file supplied)");
					Console.WriteLine("   -I<dir>  add to include search path");
					Console.WriteLine("   -nologo  skip printing candle logo information");
					Console.WriteLine("   -out     specify output file (default: write to current directory)");
					Console.WriteLine("   -pedantic:<level>  pedantic checks (levels: easy, heroic, legendary)");
					Console.WriteLine("   -ss      suppress schema validation of documents (performance boost)");
					Console.WriteLine("   -ust     use small table definitions (for backwards compatiblity)");
					Console.WriteLine("   -trace   show source trace for errors, warnings, and verbose messages");
					Console.WriteLine("   -ext     extension (class, assembly), should extend CompilerExtension");
					Console.WriteLine("   -zs      only do validation of documents (no output)");
					Console.WriteLine("   -wx      treat warnings as errors");
					Console.WriteLine("   -w<N>    set the warning level (0: show all, 3: show none)");
					Console.WriteLine("   -sw      suppress all warnings (same as -w3)");
					Console.WriteLine("   -sw<N>   suppress warning with specific message ID");
					Console.WriteLine("   -v       verbose output (same as -v2)");
					Console.WriteLine("   -v<N>    sets the level of verbose output (0: most output, 3: none)");
					Console.WriteLine("   -?       this help information");
					Console.WriteLine();
					Console.WriteLine("Common extensions:");
					Console.WriteLine("   .wxs    - Windows installer Xml Source file");
					Console.WriteLine("   .wxi    - Windows installer Xml Include file");
					Console.WriteLine("   .wxl    - Windows installer Xml Localization file");
					Console.WriteLine("   .wixobj - Windows installer Xml Object file (in XML format)");
					Console.WriteLine("   .wixlib - Windows installer Xml Library file (in XML format)");
					Console.WriteLine("   .wixout - Windows installer Xml Output file (in XML format)");
					Console.WriteLine();
					Console.WriteLine("   .msm - Windows installer Merge Module");
					Console.WriteLine("   .msi - Windows installer Product Database");
					Console.WriteLine("   .mst - Windows installer Transform");
					Console.WriteLine("   .pcp - Windows installer Patch Creation Package");
					Console.WriteLine();
					Console.WriteLine("For more information see: http://wix.sourceforge.net");

					return this.messageHandler.PostProcess();
				}

				// create the preprocessor and compiler
				Preprocessor preprocessor = new Preprocessor();
				preprocessor.Message += new MessageEventHandler(this.messageHandler.Display);
				for (int i = 0; i < this.includeSearchPaths.Count; ++i)
				{
					preprocessor.IncludeSearchPaths.Add(this.includeSearchPaths[i]);
				}

				Compiler compiler = new Compiler(this.useSmallTables);
				compiler.Message += new MessageEventHandler(this.messageHandler.Display);
				compiler.PedanticLevel = this.pedanticLevel;
				compiler.SuppressValidate = this.suppressSchema;

				// load any extensions
				foreach (string extension in this.extensionList)
				{
					Type extensionType = Type.GetType(extension);
					if (null == extensionType)
					{
						throw new WixInvalidExtensionException(extension);
					}

					if (extensionType.IsSubclassOf(typeof(PreprocessorExtension)))
					{
						preprocessor.AddExtension((PreprocessorExtension)Activator.CreateInstance(extensionType));
					}

					if (extensionType.IsSubclassOf(typeof(CompilerExtension)))
					{
						CompilerExtension compilerExtensionObject = Activator.CreateInstance(extensionType) as CompilerExtension;
						compiler.AddExtension(compilerExtensionObject);
					}

					if (!extensionType.IsSubclassOf(typeof(PreprocessorExtension)) && !extensionType.IsSubclassOf(typeof(CompilerExtension)))
					{
						throw new WixInvalidExtensionException(extension);
					}
				}

				// preprocess then compile each source file
				foreach (FileInfo sourceFile in this.sourceFiles)
				{
					currentFile = sourceFile; // point at the file we're working on in case a exception is thrown

					FileInfo targetFile;
					if (null != this.outputFile)
					{
						targetFile = this.outputFile;
					}
					else if (null != this.outputDirectory)
					{
						targetFile = new FileInfo(String.Concat(this.outputDirectory.FullName, Path.ChangeExtension(currentFile.Name, ".wixobj")));
					}
					else
					{
						targetFile = new FileInfo(Path.ChangeExtension(currentFile.Name, ".wixobj"));
					}

					// print friendly message saying what file is being compiled
					Console.WriteLine(currentFile.Name);

					// need to clear and re-add the commandline defines for each file
					preprocessor.ResetParameters();
					foreach (string param in this.parameters.Keys)
					{
						string name = param;
						if (!name.StartsWith("var."))
						{
							name = String.Concat("var.", name);
						}
						preprocessor.Parameters.Add(name, (string)this.parameters[param]);
					}

					// preprocess the source
					XmlDocument sourceDocument;
					try
					{
						if (this.preprocessToStdout)
						{
							preprocessor.PreprocessOut = Console.Out;
						}
						else if (null != this.preprocessFile)
						{
							preprocessor.PreprocessOut = new StreamWriter(this.preprocessFile);
						}

						sourceDocument = preprocessor.Process(currentFile.FullName);
					}
					finally
					{
						if (null != preprocessor.PreprocessOut && Console.Out != preprocessor.PreprocessOut)
						{
							preprocessor.PreprocessOut.Close();
						}
					}

					// if we're not actually going to compile anything, move on to the next file
					if (this.schemaOnly || null == sourceDocument || this.preprocessToStdout || null != this.preprocessFile)
					{
						continue;
					}

					// and now we do what we came here to do...
					Intermediate intermediate = compiler.Compile(sourceDocument, currentFile.FullName);

					// save the intermediate to disk if no errors were found for this source file
					if (null != intermediate)
					{
						intermediate.Save(targetFile.FullName);
					}

					// this file is was successful so clear the reference in case an exception gets thrown
					currentFile = null;
				}
			}
			catch (WixException we)
			{
				// TODO: once all WixExceptions are converted to errors, this clause
				// should be a no-op that just catches WixFatalErrorException's
				this.messageHandler.Display("candle.exe", "CNDL", we);
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
					if ('d' == parameter[0])
					{
						parameter = arg.Substring(2);
						string[] value = parameter.Split("=".ToCharArray(), 2);

						if (1 == value.Length)
						{
							this.parameters.Add(value[0], "");
						}
						else
						{
							this.parameters.Add(value[0], value[1]);
						}
					}
					else if ('I' == parameter[0])
					{
						this.includeSearchPaths.Add(parameter.Substring(1));
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
					else if ("nologo" == parameter)
					{
						this.showLogo = false;
					}
					else if ("o" == parameter || "out" == parameter)
					{
						if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
						{
							this.OnMessage(WixErrors.FileOrDirectoryPathRequired(String.Concat("-", parameter)));
							return;
						}

						if (0 <= args[i].IndexOf('\"'))
						{
							this.OnMessage(WixErrors.PathCannotContainQuote(args[i]));
							return;
						}
						else if (args[i].EndsWith("\\") || args[i].EndsWith("/"))
						{
							this.outputDirectory = new DirectoryInfo(args[i]);
						}
						else
						{
							this.outputFile = new FileInfo(args[i]);
						}
					}
					else if (parameter.StartsWith("pedantic"))
					{
						if ("pedantic:easy" == parameter)
						{
							this.pedanticLevel = PedanticLevel.Easy;
						}
						else if ("pedantic:heroic" == parameter)
						{
							this.pedanticLevel = PedanticLevel.Heroic;
						}
						else if ("pedantic:legendary" == parameter)
						{
							this.pedanticLevel = PedanticLevel.Legendary;
						}
						else // default is heroic
						{
							this.pedanticLevel = PedanticLevel.Heroic;
						}
					}
					else if ('p' == parameter[0])
					{
						String file = arg.Substring(2);
						this.preprocessFile = file;
						this.preprocessToStdout = (0 == file.Length);
					}
					else if ("ss" == parameter)
					{
						this.suppressSchema = true;
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
					else if ("ust" == parameter)
					{
						this.useSmallTables = true;
					}
					else if ("trace" == parameter)
					{
						this.messageHandler.SourceTrace = true;
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
					else if ("zs" == parameter)
					{
						this.schemaOnly = true;
					}
					else if ("?" == parameter || "help" == parameter)
					{
						this.showHelp = true;
					}
				}
				else if ('@' == arg[0])
				{
					this.ParseCommandLine(Common.ParseResponseFile(arg.Substring(1)));
				}
				else
				{
					this.sourceFiles.AddRange(Common.GetFiles(arg, "Source"));
				}
			}

			return;
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
