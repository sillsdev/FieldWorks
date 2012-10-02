//-------------------------------------------------------------------------------------------------
// <copyright file="light.cs" company="Microsoft">
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
// The light linker application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Xml;
	using System.Xml.Schema;
	using System.Xml.XPath;

	/// <summary>
	/// The main entry point for light.
	/// </summary>
	public class Light
	{
		private ArrayList objectFiles;
		private FileInfo outputFile;
		private StringCollection basePaths;
		private string cabCachePath;
		private StringCollection extensionList;
		private string imagebaseOutputPath;
		private bool tidy;
		private bool reuseCabinets;
		private bool sectionIdOnTuples;
		private bool setMsiAssemblyNameFileVersion;
		private bool allowIdenticalRows;
		private bool allowUnresolvedReferences;
		private PedanticLevel pedanticLevel;
		private bool showHelp;
		private bool showLogo;
		private bool suppressAclReset;
		private bool suppressAdminSequence;
		private bool suppressAdvertiseSequence;
		private bool suppressAssemblies;
		private bool suppressFileHashAndInfo;
		private bool suppressFiles;
		private bool suppressLayout;
		private bool suppressSchema;
		private bool suppressUISequence;
		private bool outputXml;
		private bool suppressVersionCheck;
		private bool useSmallTables;
		private StringCollection sourcePaths;
		private StringCollection localizationFiles;
		private MessageHandler messageHandler;

		/// <summary>
		/// Instantiate a new Light class.
		/// </summary>
		private Light()
		{
			this.objectFiles = new ArrayList();
			this.sourcePaths = new StringCollection();
			this.basePaths = new StringCollection();
			this.extensionList = new StringCollection();
			this.showLogo = true;
			this.tidy = true;
			this.pedanticLevel = PedanticLevel.Easy;
			this.localizationFiles = new StringCollection();
			this.messageHandler = new MessageHandler("LGHT", "light.exe");

			// set the message handler
			this.Message += new MessageEventHandler(this.messageHandler.Display);
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		private event MessageEventHandler Message;

		/// <summary>
		/// The main entry point for light.
		/// </summary>
		/// <param name="args">Commandline arguments for the application.</param>
		/// <returns>Returns the application error code.</returns>
		[MTAThread]
		public static int Main(string[] args)
		{
			Light light = new Light();
			return light.Run(args);
		}

		/// <summary>
		/// Checks if an ArrayList of strings contains a specific string.
		/// </summary>
		/// <param name="array">ArrayList of strings to search.</param>
		/// <param name="searchString">string to search for.</param>
		/// <returns>Returns true if the string is found in the ArrayList, false otherwise.</returns>
		private static bool StringArrayContains(StringCollection array, string searchString)
		{
			for (int i = 0; i < array.Count; i++)
			{
				string member = (string)array[i];
				if (0 == member.CompareTo(searchString))
				{
					return true;
				}
			}
			return false;
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
				XmlSchemaCollection objectSchema = new XmlSchemaCollection();
				FileInfo currentFile = null;

				ArrayList intermediates = new ArrayList();
				Linker linker = null;
				Microsoft.Tools.WindowsInstallerXml.Binder binder = null;
				Localizer localizer = null;

				try
				{
					// parse the command line
					this.ParseCommandLine(args);

					// exit if there was an error parsing the command line (otherwise the logo appears after error messages)
					if (this.messageHandler.FoundError)
					{
						return this.messageHandler.PostProcess();
					}

					if (0 == this.objectFiles.Count)
					{
						this.showHelp = true;
					}
					else if (null == this.outputFile)
					{
						if (1 < this.objectFiles.Count)
						{
							throw new ArgumentException("must specify output file when using more than one input file", "-out");
						}

						FileInfo fi = (FileInfo)this.objectFiles[0];
						this.outputFile = new FileInfo(Path.ChangeExtension(fi.Name, ".wix"));   // we'll let the linker change the extension later
					}

					if (this.showLogo)
					{
						Assembly lightAssembly = Assembly.GetExecutingAssembly();

						Console.WriteLine("Microsoft (R) Windows Installer Xml Linker version {0}", lightAssembly.GetName().Version.ToString());
						Console.WriteLine("Copyright (C) Microsoft Corporation 2003. All rights reserved.");
						Console.WriteLine();
					}

					if (this.showHelp)
					{
						Console.WriteLine(" usage:  light.exe [-?] [-b basePath] [-nologo] [-out outputFile] objectFile [objectFile ...]");
						Console.WriteLine();
						Console.WriteLine("   -ai        allow identical rows, identical rows will be treated as a warning");
						Console.WriteLine("   -au        (experimental) allow unresolved references, will not create a valid output");
						Console.WriteLine("   -b         base path to locate all files (default: current directory)");
						Console.WriteLine("   -cc        path to cache built cabinets (will not be deleted after linking)");
						Console.WriteLine("   -ext       extension (class, assembly), should extend SchemaExtension or BinderExtension");
						Console.WriteLine("   -fv        add a 'fileVersion' entry to the MsiAssemblyName table (rarely needed)");
						Console.WriteLine("   -i         specify the base output path for uncompressed images (default: -out parameter)");
						Console.WriteLine("   -loc       read localization string sfrom .wxl file");
						Console.WriteLine("   -nologo    skip printing light logo information");
						Console.WriteLine("   -notidy    do not delete temporary files (useful for debugging)");
						Console.WriteLine("   -reusecab  reuse cabinets from cabinet cache");
						Console.WriteLine("   -out       specify output file (default: write to current directory)");
						Console.WriteLine("   -xo        output xml instead of MSI format");
						Console.WriteLine("   -pedantic:<level>  pedantic checks (levels: easy, heroic, legendary)");
						Console.WriteLine("   -reusecab  reuse cabinets from cabinet cache");
						Console.WriteLine("   -sa        suppress assemblies: do not get assembly name information for assemblies");
						Console.WriteLine("   -sacl      suppress resetting ACLs (useful when laying out image to a network share)");
						Console.WriteLine("   -sadmin    suppress default admin sequence actions");
						Console.WriteLine("   -sadv      suppress default adv sequence actions");
						Console.WriteLine("   -sa        suppress assemblys: do not get assembly name information for assemblies");
						Console.WriteLine("   -sf        suppress files: do not get any file information (equivalent to -sa and -sh)");
						Console.WriteLine("   -sh        suppress file info: do not get hash, version, language, etc");
						Console.WriteLine("   -sl        suppress layout");
						Console.WriteLine("   -ss        suppress schema validation of documents (performance boost)");
						Console.WriteLine("   -sui       suppress default UI sequence actions");
						Console.WriteLine("   -sv        suppress intermediate file version mismatch checking");
						Console.WriteLine("   -ts        tag sectionId attribute on tuples (ignored if not used with -xo)");
						Console.WriteLine("   -ust       use small table definitions (for backwards compatiblity)");
						Console.WriteLine("   -wx        treat warnings as errors");
						Console.WriteLine("   -w<N>      set the warning level (0: show all, 3: show none)");
						Console.WriteLine("   -sw        suppress all warnings (same as -w3)");
						Console.WriteLine("   -sw<N>     suppress warning with specific message ID");
						Console.WriteLine("   -v         verbose output (same as -v2)");
						Console.WriteLine("   -v<N>      sets the level of verbose output (0: most output, 3: none)");
						Console.WriteLine("   -?         this help information");
						Console.WriteLine();
						Console.WriteLine("Environment variables:");
						Console.WriteLine("   WIX_TEMP   overrides the temporary directory used for cab creation, msm exploding, ...");
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

					// create the linker and the binder
					linker = new Linker(this.useSmallTables);
					binder = new Microsoft.Tools.WindowsInstallerXml.Binder(this.useSmallTables);

					linker.AllowIdenticalRows = this.allowIdenticalRows;
					linker.AllowUnresolvedReferences = this.allowUnresolvedReferences;
					linker.PedanticLevel = this.pedanticLevel;

					// set the sequence suppression options
					linker.SuppressAdminSequence = this.suppressAdminSequence;
					linker.SuppressAdvertiseSequence = this.suppressAdvertiseSequence;
					linker.SuppressUISequence = this.suppressUISequence;

					linker.SectionIdOnTuples = this.sectionIdOnTuples;
					binder.SuppressAclReset = this.suppressAclReset;
					binder.SetMsiAssemblyNameFileVersion = this.setMsiAssemblyNameFileVersion;
					binder.SuppressAssemblies = this.suppressAssemblies;
					binder.SuppressFileHashAndInfo = this.suppressFileHashAndInfo;

					if (this.suppressFiles)
					{
						binder.SuppressAssemblies = true;
						binder.SuppressFileHashAndInfo = true;
					}

					binder.SuppressLayout = this.suppressLayout;
					binder.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");

					if (null != this.cabCachePath || this.reuseCabinets)
					{
						// ensure the cabinet cache path exists if we are going to use it
						if (null != this.cabCachePath && !Directory.Exists(this.cabCachePath))
						{
							Directory.CreateDirectory(this.cabCachePath);
						}
					}

					if (null != this.basePaths)
					{
						foreach (string basePath in this.basePaths)
						{
							this.sourcePaths.Add(basePath);
						}
					}

					// load any extensions
					bool binderExtensionLoaded = false;
					foreach (string extension in this.extensionList)
					{
						Type extensionType = Type.GetType(extension);
						if (null == extensionType)
						{
							throw new WixInvalidExtensionException(extension);
						}

						if (extensionType.IsSubclassOf(typeof(BinderExtension)))
						{
							object[] extensionArgs = new object[] { this.basePaths, this.cabCachePath, this.reuseCabinets, this.sourcePaths };
							BinderExtension binderExtension = Activator.CreateInstance(extensionType, extensionArgs) as BinderExtension;
							Debug.Assert(null != binderExtension);
							if (binderExtensionLoaded)
							{
								throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "cannot load binder extension: {0}.  light can only load one binder extension and has already loaded binder extension: {1}.", binderExtension.ToString(), binder.Extension.ToString()), "ext");
							}

							binder.Extension = binderExtension;
							binderExtensionLoaded = true;
						}
						else if (extensionType.IsSubclassOf(typeof(SchemaExtension)))
						{
							linker.AddExtension((SchemaExtension)Activator.CreateInstance(extensionType));
						}
						else
						{
							throw new WixInvalidExtensionException(extension, extensionType, typeof(BinderExtension), typeof(SchemaExtension));
						}
					}

					// if the binder extension has not been loaded yet use the built-in binder extension
					if (!binderExtensionLoaded)
					{
						binder.Extension = new LightBinderExtension(this.basePaths, this.cabCachePath, this.reuseCabinets, this.sourcePaths);
					}

					if (null != this.imagebaseOutputPath)
					{
						binder.ImageBaseOutputPath = this.imagebaseOutputPath;
					}

					// set the message handlers
					linker.Message += new MessageEventHandler(this.messageHandler.Display);
					binder.Message += new MessageEventHandler(this.messageHandler.Display);

					// load the object schema
					if (!this.suppressSchema)
					{
						Assembly wixAssembly = Assembly.Load("wix");

						using (Stream objectsSchemaStream = wixAssembly.GetManifestResourceStream("Microsoft.Tools.WindowsInstallerXml.Xsd.objects.xsd"))
						{
							XmlReader reader = new XmlTextReader(objectsSchemaStream);
							objectSchema.Add("http://schemas.microsoft.com/wix/2003/04/objects", reader);
						}
					}

					Output output = null;

					// loop through all the believed object files
					foreach (FileInfo objectFile in this.objectFiles)
					{
						currentFile = objectFile;
						string dirName = Path.GetDirectoryName(currentFile.FullName);
						if (!StringArrayContains(this.sourcePaths, dirName))
						{
							this.sourcePaths.Add(dirName);
						}

						// load the object file into an intermediate object and add it to the list to be linked
						using (Stream fileStream = new FileStream(currentFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							XmlReader fileReader = new XmlTextReader(fileStream);

							try
							{
								XmlReader intermediateReader = fileReader;
								if (!this.suppressSchema)
								{
									intermediateReader = new XmlValidatingReader(fileReader);
									((XmlValidatingReader)intermediateReader).Schemas.Add(objectSchema);
								}

								Intermediate intermediate = Intermediate.Load(intermediateReader, currentFile.FullName, linker.TableDefinitions, this.suppressVersionCheck);
								intermediates.Add(intermediate);
								continue; // next file
							}
							catch (WixNotIntermediateException)
							{
								// try another format
							}

							try
							{
								Library library = Library.Load(currentFile.FullName, linker.TableDefinitions, this.suppressVersionCheck);
								intermediates.AddRange(library.Intermediates);
								continue; // next file
							}
							catch (WixNotLibraryException)
							{
								// try another format
							}

							output = Output.Load(currentFile.FullName, this.suppressVersionCheck);
						}
					}

					// instantiate the localizer and load any wixloc files
					if (0 < this.localizationFiles.Count || !this.outputXml)
					{
						localizer = new Localizer();

						localizer.Message += new MessageEventHandler(this.messageHandler.Display);

						// load each wixloc file
						foreach (string localizationFile in this.localizationFiles)
						{
							localizer.LoadFromFile(localizationFile);
						}

						// immediately stop processing if any errors were found
						if (this.messageHandler.FoundError)
						{
							return this.messageHandler.PostProcess();
						}
					}

					// and now for the fun part
					currentFile = this.outputFile;
					if (null == output)
					{
						// tell the linker about the localizer
						linker.Localizer = localizer;
						localizer = null;

						output = linker.Link((Intermediate[])intermediates.ToArray(typeof(Intermediate)));

						// if an error occurred during linking, stop processing
						if (null == output)
						{
							return this.messageHandler.PostProcess();
						}
					}
					else if (0 != intermediates.Count)
					{
						throw new InvalidOperationException("Cannot link object files (.wixobj) files with an output file (.wixout)");
					}

					output.Path = this.outputFile.FullName;

					// only output the xml
					if (this.outputXml)
					{
						string outputExtension = Path.GetExtension(this.outputFile.FullName);
						if (null == outputExtension || 0 == outputExtension.Length || ".wix" == outputExtension)
						{
							output.Path = Path.ChangeExtension(this.outputFile.FullName, ".wixout");
						}
						output.Save();
					}
					else // finish creating the MSI/MSM
					{
						string outputExtension = Path.GetExtension(this.outputFile.FullName);
						if (null == outputExtension || 0 == outputExtension.Length || ".wix" == outputExtension)
						{
							if (OutputType.Module == output.Type)
							{
								output.Path = Path.ChangeExtension(this.outputFile.FullName, ".msm");
							}
							else if (OutputType.PatchCreation == output.Type)
							{
								output.Path = Path.ChangeExtension(this.outputFile.FullName, ".pcp");
							}
							else
							{
								output.Path = Path.ChangeExtension(this.outputFile.FullName, ".msi");
							}
						}

						// tell the binder about the localizer
						binder.Localizer = localizer;

						binder.Bind(output);
					}

					currentFile = null;
				}
				catch (WixInvalidIdtException)
				{
					this.tidy = false;   // make sure the IDT files stay around
					throw;
				}
				catch (WixMergeFailureException)
				{
					this.tidy = false; // make sure the merge.log stays around
					throw;
				}
				finally
				{
					if (null != binder)
					{
						if (this.tidy)
						{
							if (!binder.DeleteTempFiles())
							{
								Console.WriteLine("Warning, failed to delete temporary directory: {0}", binder.TempFilesLocation);
							}
						}
						else
						{
							Console.WriteLine("Temporary directory located at '{0}'.", binder.TempFilesLocation);
						}
					}
				}
			}
			catch (WixException we)
			{
				// TODO: once all WixExceptions are converted to errors, this clause
				// should be a no-op that just catches WixFatalErrorException's
				this.messageHandler.Display("light.exe", "LGHT", we);
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

						this.outputFile = new FileInfo(args[i]);
					}
					else if ("ai" == parameter)
					{
						this.allowIdenticalRows = true;
					}
					else if ("au" == parameter)
					{
						this.allowUnresolvedReferences = true;
					}
					else if ("b" == parameter)
					{
						if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
						{
							this.OnMessage(WixErrors.DirectoryPathRequired(String.Concat("-", parameter)));
							return;
						}

						this.basePaths.Add(args[i]);
					}
					else if ("cc" == parameter)
					{
						if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
						{
							this.OnMessage(WixErrors.DirectoryPathRequired(String.Concat("-", parameter)));
							return;
						}

						this.cabCachePath = args[i];
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
					else if ("fv" == parameter)
					{
						this.setMsiAssemblyNameFileVersion = true;
					}
					else if ("i" == parameter)
					{
						++i;
						if (!((args[i]).EndsWith("\\")))
						{
							this.imagebaseOutputPath = args[i] + "\\";
						}
						else
						{
							this.imagebaseOutputPath = args[i];
						}
					}
					else if ("nologo" == parameter)
					{
						this.showLogo = false;
					}
					else if ("notidy" == parameter)
					{
						this.tidy = false;
					}
					else if ("loc" == parameter)
					{
						if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
						{
							this.OnMessage(WixErrors.FilePathRequired(String.Concat("-", parameter)));
							return;
						}

						this.localizationFiles.Add(args[i]);
					}
					else if ("xo" == parameter)
					{
						this.outputXml = true;
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
					else if ("reusecab" == parameter)
					{
						this.reuseCabinets = true;
					}
					else if ("sa" == parameter)
					{
						this.suppressAssemblies = true;
					}
					else if ("sacl" == parameter)
					{
						this.suppressAclReset = true;
					}
					else if ("sadmin" == parameter)
					{
						this.suppressAdminSequence = true;
					}
					else if ("sadv" == parameter)
					{
						this.suppressAdvertiseSequence = true;
					}
					else if ("sf" == parameter)
					{
						this.suppressFiles = true;
					}
					else if ("sh" == parameter)
					{
						this.suppressFileHashAndInfo = true;
					}
					else if ("sl" == parameter)
					{
						this.suppressLayout = true;
					}
					else if ("ss" == parameter)
					{
						this.suppressSchema = true;
					}
					else if ("sui" == parameter)
					{
						this.suppressUISequence = true;
					}
					else if ("sv" == parameter)
					{
						this.suppressVersionCheck = true;
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
					else if ("ts" == parameter)
					{
						this.sectionIdOnTuples = true;
					}
					else if ("ust" == parameter)
					{
						this.useSmallTables = true;
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
				}
				else if ('@' == arg[0])
				{
					this.ParseCommandLine(Common.ParseResponseFile(arg.Substring(1)));
				}
				else
				{
					this.objectFiles.AddRange(Common.GetFiles(arg, "Source"));
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

		/// <summary>
		/// Light extensions to the binder.
		/// </summary>
		private class LightBinderExtension : BinderExtension
		{
			private StringCollection basePaths;
			private string cabCachePath;
			private bool reuseCabinets;
			private StringCollection sourcePaths;

			/// <summary>
			/// Instantiate a new LightBinderExtension.
			/// </summary>
			/// <param name="basePaths">Base paths to locate files.</param>
			/// <param name="cabCachePath">Path to cabinet cache.</param>
			/// <param name="reuseCabinets">Option to reuse cabinets in the cache.</param>
			/// <param name="sourcePaths">All source paths to intermediate files.</param>
			public LightBinderExtension(StringCollection basePaths, string cabCachePath, bool reuseCabinets, StringCollection sourcePaths)
			{
				this.basePaths = basePaths;
				this.cabCachePath = cabCachePath;
				this.reuseCabinets = reuseCabinets;
				this.sourcePaths = sourcePaths;
			}

			/// <summary>
			/// FileResolutionHandler callback that looks for a valid path based on where
			/// the sources were located.
			/// </summary>
			/// <param name="source">Original source path.</param>
			/// <param name="fileType">Type of file to look for.</param>
			/// <returns>Source path to be used when importing the file.</returns>
			public override string FileResolutionHandler(string source, FileResolutionType fileType)
			{
				string filePath = null;

				if (source.StartsWith("SourceDir\\") || source.StartsWith("SourceDir/"))
				{
					foreach (string basePath in this.basePaths)
					{
						filePath = Path.Combine(basePath, source.Substring(10));
						if (File.Exists(filePath))
						{
							return filePath;
						}
					}
				}

				if (Path.IsPathRooted(source) || File.Exists(source))
				{
					return source;
				}

				foreach (string path in this.sourcePaths)
				{
					filePath = Path.Combine(path, source);
					if (File.Exists(filePath))
					{
						return filePath;
					}

					if (source.StartsWith("SourceDir\\") || source.StartsWith("SourceDir/"))
					{
						filePath = Path.Combine(path, source.Substring(10));
						if (File.Exists(filePath))
						{
							return filePath;
						}
					}
				}

				throw new WixFileNotFoundException(source, fileType.ToString());
			}

			/// <summary>
			/// CabinetResolutionHandler callback that looks for an existing, up to date
			/// cabinet and reuses that instead of rebuilding.
			/// </summary>
			/// <param name="fileIds">Array of file identifiers that will be compressed into cabinet.</param>
			/// <param name="filePaths">Array of file paths that will be compressed.  Paired with fileIds array.</param>
			/// <param name="cabinetPath">Path to cabinet to generate.  Path may be modified by delegate.</param>
			/// <returns>The CabinetBuildOption.  By default the cabinet is built and moved to its target location.</returns>
			public override CabinetBuildOption CabinetResolutionHandler(string[] fileIds, string[] filePaths, ref string cabinetPath)
			{
				// no special behavior specified, use the default
				if (null == this.cabCachePath && !this.reuseCabinets)
				{
					return base.CabinetResolutionHandler(fileIds, filePaths, ref cabinetPath);
				}

				// if a cabinet cache path was provided, change the location for the cabinet
				// to be built to
				if (null != this.cabCachePath)
				{
					string cabinetName = Path.GetFileName(cabinetPath);
					cabinetPath = Path.Combine(this.cabCachePath, cabinetName);
				}

				// if we still think we're going to reuse the cabinet check to see if the cabinet exists first
				if (this.reuseCabinets)
				{
					bool cabinetExists = false;

					// check to see if any of the files are newer than the cabinet and
					// if so we can't reuse the cabinet
					if (File.Exists(cabinetPath))
					{
						cabinetExists = true;

						DateTime cabinetCreatedTime = File.GetCreationTime(cabinetPath);
						for (int i = 0; i < filePaths.Length; ++i)
						{
							if (cabinetCreatedTime < File.GetCreationTime(filePaths[i]))
							{
								cabinetExists = false;
							}
						}
					}

					return (cabinetExists ? CabinetBuildOption.Copy : CabinetBuildOption.BuildAndCopy);
				}
				else // by default move the built cabinet
				{
					return CabinetBuildOption.BuildAndMove;
				}
			}
		}
	}
}
