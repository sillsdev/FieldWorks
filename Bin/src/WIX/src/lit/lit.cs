//-------------------------------------------------------------------------------------------------
// <copyright file="lit.cs" company="Microsoft">
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
// Main entry point for library tool.
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
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Xml;
	using System.Xml.Schema;

	/// <summary>
	/// Main entry point for library tool.
	/// </summary>
	public class Lit
	{
		private ArrayList objectFiles;
		private FileInfo outputFile;
		private bool showLogo;
		private bool showHelp;
		private bool useSmallTables;
		private bool suppressSchema;
		private bool suppressVersionCheck;
		private MessageHandler messageHandler;
		private StringCollection extensionList;

		/// <summary>
		/// Instantiate a new Lit class.
		/// </summary>
		private Lit()
		{
			this.objectFiles = new ArrayList();
			this.extensionList = new StringCollection();
			this.showLogo = true;
			this.messageHandler = new MessageHandler("LIT", "lit.exe");
			this.extensionList = new StringCollection();

			// set the message handler
			this.Message += new MessageEventHandler(this.messageHandler.Display);
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		public event MessageEventHandler Message;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// <param name="args">Commandline arguments for lit.</param>
		/// <returns>Returns non-zero error code in the case of an error.</returns>
		[MTAThread]
		public static int Main(string[] args)
		{
			Lit lit = new Lit();
			return lit.Run(args);
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
				Librarian librarian = null;

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
					Assembly litAssembly = Assembly.GetExecutingAssembly();

					Console.WriteLine("Microsoft (R) Windows Installer Xml Library Tool version {0}", litAssembly.GetName().Version.ToString());
					Console.WriteLine("Copyright (C) Microsoft Corporation 2003. All rights reserved.");
					Console.WriteLine();
				}

				if (this.showHelp)
				{
					Console.WriteLine(" usage:  lit.exe [-?] [-nologo] [-out outputFile] objectFile [objectFile ...]");
					Console.WriteLine();
					Console.WriteLine("   -nologo    skip printing lit logo information");
					Console.WriteLine("   -out       specify output file (default: write to current directory)");
					Console.WriteLine();
					Console.WriteLine("   -ext       extension (class, assembly), should extend SchemaExtension or BinderExtension");
					Console.WriteLine("   -ss        suppress schema validation of documents (performance boost)");
					Console.WriteLine("   -sv        suppress intermediate file version mismatch checking");
					Console.WriteLine("   -ust       use small table definitions (for backwards compatiblity)");
					Console.WriteLine("   -wx        treat warnings as errors");
					Console.WriteLine("   -w<N>      set the warning level (0: show all, 3: show none)");
					Console.WriteLine("   -sw        suppress all warnings (same as -w3)");
					Console.WriteLine("   -sw<N>     suppress warning with specific message ID");
					Console.WriteLine("   -v         verbose output (same as -v2)");
					Console.WriteLine("   -v<N>      sets the level of verbose output (0: most output, 3: none)");
					Console.WriteLine("   -?         this help information");
					Console.WriteLine();
					Console.WriteLine("Common extensions:");
					Console.WriteLine("   .wxs    - Windows installer Xml Source file");
					Console.WriteLine("   .wxi    - Windows installer Xml Include file");
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

				// create the librarian
				librarian = new Librarian(this.useSmallTables);
				librarian.Message += new MessageEventHandler(this.messageHandler.Display);

				// load any extensions
				foreach (string extension in this.extensionList)
				{
					Type extensionType = Type.GetType(extension);
					if (null == extensionType)
					{
						throw new WixInvalidExtensionException(extension);
					}

					if (extensionType.IsSubclassOf(typeof(SchemaExtension)))
					{
						librarian.AddExtension((SchemaExtension)Activator.CreateInstance(extensionType));
					}
				}

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

				// add the Intermediates to the librarian
				foreach (FileInfo objectFile in this.objectFiles)
				{
					currentFile = objectFile;

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

							Intermediate intermediate = Intermediate.Load(intermediateReader, currentFile.FullName, librarian.TableDefinitions, this.suppressVersionCheck);
							intermediates.Add(intermediate);
							continue; // next file
						}
						catch (WixNotIntermediateException)
						{
							// try another format
						}

						Library objLibrary = Library.Load(currentFile.FullName, librarian.TableDefinitions, this.suppressVersionCheck);
						intermediates.AddRange(objLibrary.Intermediates);
					}

					currentFile = null;
				}

				// and now for the fun part
				Library library = librarian.Combine((Intermediate[])intermediates.ToArray(typeof(Intermediate)));

				// save the library output if an error did not occur
				if (null != library)
				{
					library.Save(this.outputFile.FullName);
				}
			}
			catch (WixException we)
			{
				// TODO: once all WixExceptions are converted to errors, this clause
				// should be a no-op that just catches WixFatalErrorException's
				this.messageHandler.Display("lit.exe", "LIT", we);
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
					else if ("ss" == parameter)
					{
						this.suppressSchema = true;
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
					else if ("ust" == parameter)
					{
						this.useSmallTables = true;
					}
					else if ('v' == parameter[0])
					{
						if (1 < parameter.Length)
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
	}
}
