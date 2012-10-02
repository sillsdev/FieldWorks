//-------------------------------------------------------------------------------------------------
// <copyright file="tallow.cs" company="Microsoft">
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
// The tallow codegen tool application.
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
	using System.Reflection.Emit;
	using System.Runtime.InteropServices;
	using System.Security;
	using System.Text;
	using System.Xml;
	using System.Diagnostics;

	using Microsoft.Win32;

	/// <summary>
	/// The main entry point for tallow.
	/// </summary>
	public class Tallow
	{
		/// <summary>
		/// The main entry point for candle.
		/// </summary>
		/// <param name="args">Commandline arguments for the application.</param>
		/// <returns>Returns the application error code.</returns>
		[MTAThread]
		public static int Main(string[] args)
		{
			try
			{
				TallowMain tallow = new TallowMain(args);
			}
			catch (WixException we)
			{
				string errorFileName = "tallow.exe";
				Console.Error.WriteLine("\r\n\r\n{0} : fatal error TLLW{1:0000}: {2}", errorFileName, (int)we.Type, we.Message);
				Console.Error.WriteLine();

				return 1;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("\r\n\r\ntallow.exe : fatal error TLLW0001: {0}\r\n\r\nStack Trace:\r\n{1}", e.Message, e.StackTrace);
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// Main class for tallow.
		/// </summary>
		internal class TallowMain
		{
			private const int MaxPath = 255;

			private static readonly UIntPtr HkeyClassesRoot = (UIntPtr)0x80000000;
			private static readonly UIntPtr HkeyCurrentUser = (UIntPtr)0x80000001;
			private static readonly UIntPtr HkeyLocalMachine = (UIntPtr)0x80000002;
			private static readonly UIntPtr HkeyUsers = (UIntPtr)0x80000003;

			private const uint Delete = 0x00010000;
			private const uint ReadOnly = 0x00020000;
			private const uint WriteDac = 0x00040000;
			private const uint WriteOwner = 0x00080000;
			private const uint Synchronize = 0x00100000;
			private const uint StandardRightsRequired = 0x000F0000;
			private const uint StandardRightsAll = 0x001F0000;

			private const uint GenericRead = 0x80000000;
			private const uint GenericWrite = 0x40000000;
			private const uint GenericExecute = 0x20000000;
			private const uint GenericAll = 0x10000000;

			private bool showLogo;
			private bool showHelp;

			private StringCollection comregAssemblies;
			private StringCollection dirwalkDirectories;
			private StringCollection dirwalkDirectoryFilters;
			private StringCollection dirwalkFileFilters;
			private StringCollection selfregFiles;
			private StringCollection selfRegExes;
			private StringCollection selfRegExeArguments;
			private StringCollection selfregTlbs;
			private StringCollection resourceFiles;
			private StringCollection registryFiles;

			private bool generateGuids;

			private bool oneResourcePerComponent;

			private bool dirwalkSetReadOnly;
			private bool dirwalkSetHidden;
			private bool dirwalkSetSystem;
			private bool dirwalkSetVital;
			private bool dirwalkSetChecksum;
			private bool dirwalkSetCompressed;

			private int componentCount;
			private int directoryCount;
			private int fileCount;

			private const UInt32 LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

			[DllImport ("kernel32.dll", CallingConvention=CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
			private static extern IntPtr LoadLibraryEx(string DllPath, IntPtr File, UInt32 Flags);

			[DllImport("Oleaut32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
			private static extern UInt32 LoadTypeLibEx(string TlbPath, UInt32 regkind, out IntPtr pptlib);

			/// <summary>
			/// Main method for the tallow application within the TallowMain class.
			/// </summary>
			/// <param name="args">Commandline arguments to the application.</param>
			public TallowMain(string[] args)
			{
				this.showLogo = true;
				this.showHelp = false;
				this.comregAssemblies = new StringCollection();
				this.dirwalkDirectories = new StringCollection();
				this.dirwalkDirectoryFilters = new StringCollection();
				this.dirwalkFileFilters = new StringCollection();
				this.selfregFiles = new StringCollection();
				this.selfRegExes = new StringCollection();
				this.selfRegExeArguments = new StringCollection();
				this.selfregTlbs = new StringCollection();
				this.resourceFiles = new StringCollection();
				this.registryFiles = new StringCollection();

				// parse the command line
				this.ParseCommandLine(args);

				if (0 == this.comregAssemblies.Count && 0 == this.dirwalkDirectories.Count && 0 == this.selfregFiles.Count && 0 == this.selfRegExes.Count && 0 == this.selfregTlbs.Count && 0 == this.resourceFiles.Count && 0 == this.registryFiles.Count)
				{
					this.showHelp = true;
				}

				// get the assemblies
				Assembly tallowAssembly = Assembly.GetExecutingAssembly();

				if (this.showLogo)
				{
					Console.WriteLine("Microsoft (R) Windows Installer Xml Tool version {0}", tallowAssembly.GetName().Version.ToString());
					Console.WriteLine("Copyright (C) Microsoft Corporation 2003. All rights reserved.");
					Console.WriteLine();
				}
				if (this.showHelp)
				{
					Console.WriteLine(" usage:  tallow.exe [-?] [-nologo] [-1] [-c assembly] [-s file] [-e .exe-file [#arguments]] [-t file]");
					Console.WriteLine("                    [-d directory [-da?] [-dd filter] [-df filter]]");
					Console.WriteLine();
					Console.WriteLine("   -1       one resource per component");
					Console.WriteLine("   -c       extracts the COM Interop registration from an assembly");
					Console.WriteLine("   -d       walks a directory tree creating components for all the files");
					Console.WriteLine("   -da?     adds one or more attributes to all files recursed");
					Console.WriteLine("      c - checksum");
					Console.WriteLine("      h - hidden");
					Console.WriteLine("      p - compressed");
					Console.WriteLine("      r - read only");
					Console.WriteLine("      s - system");
					Console.WriteLine("      v - vital");
					Console.WriteLine("   -dd      provides a filter for directories recursed");
					Console.WriteLine("   -df      provides a filter for files found in a directory");
					Console.WriteLine("   -e       extracts the registry keys written by running the .exe file with optional command-line arguments. The # introduces the first argument; multiple args must be quoted (outside the #).");
					Console.WriteLine("   -s       extracts the registry keys written by DllRegisterServer from file");
					Console.WriteLine("   -t       extracts the registry keys needed to register type library (.tlb) file");
					Console.WriteLine("   -r       processes a .rc file into a WiX UI fragment");
					Console.WriteLine("   -reg     processes a .reg file into a WiX fragment");
					Console.WriteLine("   -nologo  skip printing tallow logo information");
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
					return; // exit
				}

				if (this.generateGuids)
				{
					Console.WriteLine("WARNING: AUTOMATICALLY GENERATING GUIDS IS A DANGEROUS FEATURE BECAUSE IT MAY CAUSE COMPONENT RULES TO BE BROKEN.  PLEASE READ THE FOLLOWING ARTICLES TO LEARN MORE ABOUT THE COMPONENT RULES.");
					Console.WriteLine("Organizing Applications into Components: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/msi/setup/organizing_applications_into_components.asp");
					Console.WriteLine("What happens if the component rules are broken?: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/msi/setup/what_happens_if_the_component_rules_are_broken.asp");
					Console.WriteLine("Component Rules 101: http://blogs.msdn.com/robmen/archive/2003/10/18/56497.aspx");
				}

				// dump XML to the screen
				XmlTextWriter writer = new XmlTextWriter(Console.Out);
				writer.Formatting = Formatting.Indented;

				// okay, here we go
				writer.WriteStartElement("Wix");
				writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/wix/2003/01/wi");
				writer.WriteStartElement("Fragment");

				if (0 < this.comregAssemblies.Count || 0 < this.dirwalkDirectories.Count || 0 < this.selfregFiles.Count || 0 < this.selfRegExes.Count || 0 < this.selfregTlbs.Count)
				{
					writer.WriteStartElement("DirectoryRef");
					writer.WriteAttributeString("Id", "TARGETDIR");

					foreach (string assembly in this.comregAssemblies)
					{
						bool success = false;

						if (!this.oneResourcePerComponent)
						{
							this.StartComponentElement(writer);
						}

						this.WriteFileElement(writer, assembly);

						try
						{
							RegistrationServices regSvcs = new RegistrationServices();
							Assembly a = Assembly.LoadFrom(assembly);

							// Must call this before overriding registry hives to prevent binding failures
							// on exported types during RegisterAssembly.
							a.GetExportedTypes();

							try
							{
								this.StartOverridingRegHives();
								success = regSvcs.RegisterAssembly(a, AssemblyRegistrationFlags.SetCodeBase);
							}
							finally
							{
								this.EndOverridingRegHives(success ? writer : null);
							}
						}
						catch (FileNotFoundException e)
						{
							Console.Error.Write("Failed to load Assembly: {0}\r\n {1}", assembly, e.ToString());
							writer.WriteStartElement("Error");
							writer.WriteAttributeString("Tallow", "File not found");
							writer.WriteEndElement();
						}
						catch (BadImageFormatException e)
						{
							Console.Error.Write("Failed to load Assembly: {0}\r\n {1}", assembly, e.ToString());
						}
						catch (SecurityException e)
						{
							Console.Error.Write("Failed to load Assembly: {0}\r\n {1}", assembly, e.ToString());
							writer.WriteStartElement("Error");
							writer.WriteAttributeString("Tallow", "Security exception");
							writer.WriteEndElement();
						}
						catch (PathTooLongException e)
						{
							Console.Error.Write("Failed to load Assembly: {0}\r\n {1}", assembly, e.ToString());
							writer.WriteStartElement("Error");
							writer.WriteAttributeString("Tallow", "Path too long");
							writer.WriteEndElement();
						}

						if (!this.oneResourcePerComponent)
						{
							this.EndComponentElement(writer); // </Component>
						}
					}

					// process SelfReg files
					if (0 < this.selfregFiles.Count)
					{
						foreach (string file in this.selfregFiles)
						{
							bool success = false;

							if (!this.oneResourcePerComponent)
							{
								this.StartComponentElement(writer);
							}

							this.WriteFileElement(writer, file);

							IntPtr dllHandle = LoadLibraryEx(file, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
							if (IntPtr.Zero == dllHandle)
							{
								int lastError = Marshal.GetLastWin32Error();
								throw new ApplicationException(String.Format("Unable to load file: {1}, error: {2}", file, lastError));
							}

							try
							{
								this.StartOverridingRegHives();

								try
								{
									DynamicPInvoke(file, "DllRegisterServer", typeof(int), null, null);
								}
								catch (TargetInvocationException e)
								{
									Console.Error.WriteLine("Failed to SelfReg: {0}\r\n {1}", file, e.ToString());
								}

								success = true;
							}
							finally
							{
								this.EndOverridingRegHives(success ? writer : null);
							}

							if (!this.oneResourcePerComponent)
							{
								this.EndComponentElement(writer); // </Component>
							}
						}
					}

					// process .exe files
					if (0 < this.selfRegExes.Count)
					{
						for (int i = 0; i < this.selfRegExes.Count; i++)
						{
							string file = this.selfRegExes[i];
							string arguments = this.selfRegExeArguments[i];
							bool success = false;

							if (!this.oneResourcePerComponent)
							{
								this.StartComponentElement(writer);
							}

							this.WriteFileElement(writer, file);

							System.Diagnostics.Process proc = new Process();
							proc.StartInfo.FileName = file;
							proc.StartInfo.Arguments = arguments;
							proc.StartInfo.UseShellExecute = false;

							try
							{
								this.StartOverridingRegHives();

								try
								{
									proc.Start();
									proc.WaitForExit();
								}
								catch
								{
									Console.Error.WriteLine("Failed to find or run: {0} {1}", file, arguments);
									writer.WriteStartElement("Error");
									writer.WriteAttributeString("Tallow", "Failed to find or run");
									writer.WriteEndElement();
								}

								success = true;
							}
							finally
							{
								this.EndOverridingRegHives(success ? writer : null);
							}

							if (!this.oneResourcePerComponent)
							{
								this.EndComponentElement(writer); // </Component>
							}
						}
					}

					// process TypeLib files
					if (0 < this.selfregTlbs.Count)
					{
						foreach (string file in this.selfregTlbs)
						{
							bool success = false;

							if (!this.oneResourcePerComponent)
							{
								this.StartComponentElement(writer);
							}

							this.WriteFileElement(writer, file);

							try
							{
								this.StartOverridingRegHives();

								try
								{
									// Call the Win32 function to load the type library and register it:
									IntPtr pptlib;
									UInt32 retVal = LoadTypeLibEx(file, 1, out pptlib);
									// Note: we are left with a pointer to the loaded library, in pptlib,
									// but there doesn't seem to be a way to release it.
								}
								catch (TargetInvocationException e)
								{
									Console.Error.WriteLine("Failed to SelfReg: {0}\r\n {1}", file, e.ToString());
								}

								success = true;
							}
							finally
							{
								this.EndOverridingRegHives(success ? writer : null);
							}

							if (!this.oneResourcePerComponent)
							{
								this.EndComponentElement(writer); // </Component>
							}
						}
					}

					// process each directory
					if (0 < this.dirwalkDirectories.Count)
					{
						if (0 == this.dirwalkDirectoryFilters.Count)
						{
							this.dirwalkDirectoryFilters.Add("*");
						}

						if (0 == this.dirwalkFileFilters.Count)
						{
							this.dirwalkFileFilters.Add("*");
						}

						foreach (string directory in this.dirwalkDirectories)
						{
							this.WriteDirectoryElement(writer, directory, true);
						}
					}

					// close up and go home
					writer.WriteEndElement(); // </DirectoryRef>
				}

				if (0 < this.resourceFiles.Count)
				{
					writer.WriteStartElement("UI");
					foreach (string resourceFile in this.resourceFiles)
					{
						TallowRCProcessing.ProcessResourceFile(writer, resourceFile);
					}
					writer.WriteEndElement();
				}

				if (0 < this.registryFiles.Count)
				{
					foreach (string resourceFile in this.registryFiles)
					{
						TallowRegProcessing.ProcessRegistryFile(writer, resourceFile);
					}
				}

				writer.WriteEndElement(); // </Fragment>
				writer.WriteEndElement(); // </Wix>

				if (this.generateGuids)
				{
					Console.WriteLine();
					Console.WriteLine("THIS IS NOT A MISTAKE, TO BE VERY CLEAR, WE ARE REPEATING THE WARNING ABOUT BREAKING COMPONENT RULES");
					Console.WriteLine("WARNING: AUTOMATICALLY GENERATING GUIDS IS A DANGEROUS FEATURE BECAUSE IT MAY CAUSE COMPONENT RULES TO BE BROKEN.  PLEASE READ THE FOLLOWING ARTICLES TO LEARN MORE ABOUT THE COMPONENT RULES.");
					Console.WriteLine("Organizing Applications into Components: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/msi/setup/organizing_applications_into_components.asp");
					Console.WriteLine("What happens if the component rules are broken?: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/msi/setup/what_happens_if_the_component_rules_are_broken.asp");
					Console.WriteLine("Component Rules 101: http://blogs.msdn.com/robmen/archive/2003/10/18/56497.aspx");
				}
			}

			/// <summary>
			/// Dynamically PInvokes into a DLL.
			/// </summary>
			/// <param name="dll">Dynamic link library containing the entry point.</param>
			/// <param name="entryPoint">Entry point into dynamic link library.</param>
			/// <param name="returnType">Return type of entry point.</param>
			/// <param name="parameterTypes">Type of parameters to entry point.</param>
			/// <param name="parameterValues">Value of parameters to entry point.</param>
			/// <returns>Value from invoked code.</returns>
			private static object DynamicPInvoke(string dll, string entryPoint, Type returnType, Type[] parameterTypes, object[] parameterValues)
			{
				AssemblyName assemblyName = new AssemblyName();
				assemblyName.Name = "tallowTempAssembly";

				AssemblyBuilder dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
				ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule("tallowTempModule");

				MethodBuilder dynamicMethod = dynamicModule.DefinePInvokeMethod(entryPoint, dll, MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.PinvokeImpl, CallingConventions.Standard, returnType, parameterTypes, CallingConvention.Winapi, CharSet.Ansi);
				dynamicModule.CreateGlobalFunctions();

				MethodInfo methodInfo = dynamicModule.GetMethod(entryPoint);
				return methodInfo.Invoke(null, parameterValues);
			}

			/// <summary>
			/// Writes the beginning of a Component element to the writer.
			/// </summary>
			/// <param name="writer">Writer to output to.</param>
			private void StartComponentElement(XmlWriter writer)
			{
				writer.WriteStartElement("Component");
				writer.WriteAttributeString("Id", String.Format("component{0}", this.componentCount++));
				writer.WriteAttributeString("DiskId", "1");
				if (this.generateGuids)
				{
					writer.WriteAttributeString("Guid", Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture).ToUpper(CultureInfo.InvariantCulture));
				}
				else
				{
					writer.WriteAttributeString("Guid", "PUT-GUID-HERE");
				}
			}

			/// <summary>
			/// Writes the end of a Component element to the writer.
			/// </summary>
			/// <param name="writer">Writer to output to.</param>
			private void EndComponentElement(XmlWriter writer)
			{
				writer.WriteEndElement();
			}

			/// <summary>
			/// Writes a Directory element to the writer.
			/// </summary>
			/// <param name="writer">Writer to output to.</param>
			/// <param name="path">Path to directory to write to XML</param>
			/// <param name="recurse">Recurse into sub directories</param>
			private void WriteDirectoryElement(XmlWriter writer, string path, bool recurse)
			{
				DirectoryInfo directory = new DirectoryInfo(path);
				if (!directory.Exists)
				{
					throw new WixFileNotFoundException(path, "Directory");
				}

				string dirName = directory.Name;
				string dirShortName = Path.GetFileName(NativeMethods.GetShortPathName(directory.FullName));

				writer.WriteStartElement("Directory");
				writer.WriteAttributeString("Id", String.Format("directory{0}", this.directoryCount++));
				writer.WriteAttributeString("Name", dirShortName);
				if (dirShortName != dirName)
				{
					writer.WriteAttributeString("LongName", dirName);
				}

				// dump any files into a Component
				foreach (string fileFilter in this.dirwalkFileFilters)
				{
					FileInfo[] files = directory.GetFiles(fileFilter);
					if (0 < files.Length)
					{
						if (!this.oneResourcePerComponent)
						{
							this.StartComponentElement(writer);
						}

						for (int i = 0; i < files.Length; ++i)
						{
							this.WriteFileElement(writer, files[i].FullName);
						}

						if (!this.oneResourcePerComponent)
						{
							this.EndComponentElement(writer); // </Component>
						}
					}
				}

				if (recurse)
				{
					foreach (string directoryFilter in this.dirwalkDirectoryFilters)
					{
						DirectoryInfo[] directories = directory.GetDirectories(directoryFilter);
						for (int i = 0; i < directories.Length; ++i)
						{
							this.WriteDirectoryElement(writer, directories[i].FullName, recurse);
						}
					}
				}
				writer.WriteEndElement(); // </Directory>
			}

			/// <summary>
			/// Writes a File element to the writer.
			/// </summary>
			/// <param name="writer">Writer to output to.</param>
			/// <param name="path">Path to file to write to XML.</param>
			private void WriteFileElement(XmlWriter writer, string path)
			{
				if (!File.Exists(path))
				{
					throw new WixFileNotFoundException(path, "File");
				}

				string fileName = Path.GetFileName(path);
				string shortFileName = Path.GetFileName(NativeMethods.GetShortPathName(Path.GetFullPath(path)));

				if (this.oneResourcePerComponent)
				{
					this.StartComponentElement(writer);
				}

				writer.WriteStartElement("File");
				writer.WriteAttributeString("Id", String.Format("file{0}", this.fileCount++));
				writer.WriteAttributeString("Name", shortFileName);
				if (shortFileName != fileName)
				{
					writer.WriteAttributeString("LongName", fileName);
				}

				if (this.dirwalkSetChecksum)
				{
					writer.WriteAttributeString("Checksum", "yes");
				}

				if (this.dirwalkSetCompressed)
				{
					writer.WriteAttributeString("Compressed", "yes");
				}

				if (this.dirwalkSetHidden)
				{
					writer.WriteAttributeString("Hidden", "yes");
				}

				if (this.dirwalkSetReadOnly)
				{
					writer.WriteAttributeString("ReadOnly", "yes");
				}

				if (this.dirwalkSetSystem)
				{
					writer.WriteAttributeString("System", "yes");
				}

				if (this.dirwalkSetVital)
				{
					writer.WriteAttributeString("Vital", "yes");
				}

				writer.WriteAttributeString("src", Path.GetFullPath(path));
				writer.WriteEndElement(); // </File>

				if (this.oneResourcePerComponent)
				{
					this.EndComponentElement(writer);
				}
			}

			/// <summary>
			/// Writes subkeys of this registry key to the writer.
			/// </summary>
			/// <param name="writer">Writer to output to.</param>
			/// <param name="key">Key to have sub keys written</param>
			private void WriteSubKeys(XmlWriter writer, RegistryKey key)
			{
				string root;
				string path = key.Name.Substring(32);
				string[] names = key.GetValueNames();
				string[] subkeys = key.GetSubKeyNames();

				int slash = path.IndexOf('\\');
				if (-1 == slash)
				{
					root = path;
					path = String.Empty;
				}
				else
				{
					root = path.Substring(0, slash);
					path = path.Substring(slash + 1, path.Length - slash - 1);
				}

				// make sure the root is valid
				switch (root)
				{
					case "HKLM":
					case "HKCR":
					case "HKCU":
					case "HKU":
						break;
					default:
						throw new InvalidOperationException(String.Concat("Unknown key: ", key.Name));
				}

				// registry names
				for (int i = 0; i < names.Length; ++i)
				{
					object value = key.GetValue(names[i]);

					if (this.oneResourcePerComponent)
					{
						this.StartComponentElement(writer);
					}

					this.WriteRegistryElement(writer, root, path, names[i], value);

					if (this.oneResourcePerComponent)
					{
						this.EndComponentElement(writer);
					}
				}

				// do subkeys
				for (int i = 0; i < subkeys.Length; ++i)
				{
					RegistryKey subkey = key.OpenSubKey(subkeys[i]);
					this.WriteSubKeys(writer, subkey);
				}

				// if there were no names or subkeys, make sure the path is in
				if (0 < path.Length && 0 == names.Length && 0 == subkeys.Length)
				{
					this.WriteRegistryElement(writer, root, path, null, null);
				}
			}

			/// <summary>
			/// Writes a Registry element to the writer.
			/// </summary>
			/// <param name="writer">Writer to output to.</param>
			/// <param name="root">Root for the key.</param>
			/// <param name="path">Path to the key.</param>
			/// <param name="name">Name of the key.</param>
			/// <param name="value">Value for the key.</param>
			private void WriteRegistryElement(XmlWriter writer, string root, string path, string name, object value)
			{
				writer.WriteStartElement("Registry");
				writer.WriteAttributeString("Root", root);
				writer.WriteAttributeString("Key", path);

				if (null != name && 0 != name.Length)
				{
					writer.WriteAttributeString("Name", name);
				}

				if (null != value)
				{
					string stringValue = value as String;

					if (null != stringValue)
					{
						if (0 != stringValue.Length)
						{
							writer.WriteAttributeString("Value", stringValue);
							writer.WriteAttributeString("Type", "string");
						}
					}
					else
					{
						if (value is string)
						{
							writer.WriteAttributeString("Value", value.ToString());
						}
						else if (value is int || value is long || value is uint || value is ulong)
						{
							writer.WriteAttributeString("Value", value.ToString());
							writer.WriteAttributeString("Type", "integer");
						}
						else if (value is byte[])
						{
							StringBuilder hexadecimalValue = new StringBuilder();

							// convert the byte array to hexadecimal
							foreach (byte byteValue in (byte[])value)
							{
								hexadecimalValue.Append(byteValue.ToString("X2", CultureInfo.InvariantCulture.NumberFormat));
							}

							writer.WriteAttributeString("Value", hexadecimalValue.ToString());
							writer.WriteAttributeString("Type", "binary");
						}
						else // expandable and multistring are not currently handled properly
						{
							writer.WriteAttributeString("Value", value.ToString());
							writer.WriteAttributeString("Type", "unknown");
						}
					}
				}

				writer.WriteEndElement();
			}

			/// <summary>
			/// Begins overriding a registry hive with a key under HKLM.
			/// </summary>
			/// <param name="hive">Hive to override.</param>
			/// <param name="regOverride">Registry key to use for overide.</param>
			private void StartOverridingRegHive(UIntPtr hive, string regOverride)
			{
				IntPtr key = IntPtr.Zero;

				try
				{
					key = NativeMethods.OpenRegistryKey(HkeyLocalMachine, regOverride);
					NativeMethods.OverrideRegistryKey(hive, key);
				}
				finally
				{
					if (IntPtr.Zero != key)
					{
						NativeMethods.CloseRegistryKey(key);
					}
				}
			}

			/// <summary>
			/// Begins overriding all registry hives with WiX specific registry keys.
			/// </summary>
			private void StartOverridingRegHives()
			{
				// delete the "private registry" key if it currently exists
				this.RemovePrivateRegKey();

				// override all of the root registry hives
				this.StartOverridingRegHive(HkeyClassesRoot, @"SOFTWARE\WiX\HKCR");

				// Create a dummy CLSID subkey in the new HKCR, as it will likely be read during a DLL's registration:
				RegistryKey regKey = Registry.ClassesRoot.CreateSubKey("CLSID");

				this.StartOverridingRegHive(HkeyCurrentUser, @"SOFTWARE\WiX\HKCU");
				this.StartOverridingRegHive(HkeyUsers, @"SOFTWARE\WiX\HKU");
				this.StartOverridingRegHive(HkeyLocalMachine, @"SOFTWARE\WiX\HKLM");
			}

			/// <summary>
			/// Stops overriding registry hive and optionally writes the overriden keys to a writer.
			/// </summary>
			/// <param name="hive">Have to end overriding.</param>
			/// <param name="writer">Optional writer to have override registry keys written to.</param>
			/// <param name="regOverride">Registry key to write to writer.</param>
			private void EndOverridingRegHive(UIntPtr hive, XmlTextWriter writer, string regOverride)
			{
				NativeMethods.OverrideRegistryKey(hive, IntPtr.Zero);

				if (null != writer)
				{
					// read any generated registry keys
					using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(regOverride))
					{
						this.WriteSubKeys(writer, regKey);
					}
				}
			}

			/// <summary>
			/// Stops overriding all of the registry hives and optionally writes the overriden keys to a writer.
			/// </summary>
			/// <param name="writer">Optional writer to have override registry keys written to.</param>
			private void EndOverridingRegHives(XmlTextWriter writer)
			{
				// First check if the only CLSID node in the aliased HKCR is the blank one we made:
				RegistryKey regKey = Registry.ClassesRoot.OpenSubKey("CLSID");
				string [] subKeys = regKey.GetSubKeyNames();
				if (subKeys.Length == 0)
				{
					// No sub keys were written to our dummy CLSID node, so delete it:
					Registry.ClassesRoot.DeleteSubKey("CLSID");
				}

				// quit overriding
				EndOverridingRegHive(HkeyLocalMachine, writer, @"SOFTWARE\WiX\HKLM"); // must end override first
				EndOverridingRegHive(HkeyClassesRoot, writer, @"SOFTWARE\WiX\HKCR");
				EndOverridingRegHive(HkeyCurrentUser, writer, @"SOFTWARE\WiX\HKCU");
				EndOverridingRegHive(HkeyUsers, writer, @"SOFTWARE\WiX\HKU");

				// delete the "private registry" key
				this.RemovePrivateRegKey();
			}

			/// <summary>
			/// Deletes the "private registry" key that is created for redirections.
			/// </summary>
			private void RemovePrivateRegKey()
			{
				try
				{
					Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\\WiX");
				}
				catch (ArgumentException)
				{
					// ignore the error case where "HKLM\SOFTWARE\WiX" does not exist
				}
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
					if (null == arg || "" == arg)   // skip blank arguments
					{
						continue;
					}

					//Console.WriteLine("arg: {0}, length: {1}", arg, arg.Length);
					if ('-' == arg[0] || '/' == arg[0])
					{
						string parameter = arg.Substring(1);
						if ("1" == parameter)
						{
							this.oneResourcePerComponent = true;
						}
						else if ("c" == parameter || "comreg" == parameter)
						{
							if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify an assembly for parameter", "-comreg");
							}

							this.comregAssemblies.Add(args[i]);
						}
						else if ("d" == parameter || "dirwalk" == parameter)
						{
							if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify a directory for parameter", "-dirwalk");
							}

							this.dirwalkDirectories.Add(args[i]);
						}
						else if ("dd" == parameter)
						{
							if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify a filter for the directories to find in the directory.  For example:  *.exe", "-de");
							}

							this.dirwalkDirectoryFilters.Add(args[i]);
						}
						else if ("df" == parameter)
						{
							if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify a filter for the files to find in the directory.  For example:  *.exe", "-de");
							}

							this.dirwalkFileFilters.Add(args[i]);
						}
						else if (1 < parameter.Length && "da" == parameter.Substring(0, 2))
						{
							if (2 == parameter.Length)
							{
								throw new ArgumentException("must specify at least one attribute to set.", "-da");
							}

							for (int j = 2; j < parameter.Length; ++j)
							{
								switch (parameter[j])
								{
									case 'c':
										this.dirwalkSetChecksum = true;
										break;
									case 'h':
										this.dirwalkSetHidden = true;
										break;
									case 'p':
										this.dirwalkSetCompressed = true;
										break;
									case 'r':
										this.dirwalkSetReadOnly = true;
										break;
									case 's':
										this.dirwalkSetSystem = true;
										break;
									case 'v':
										this.dirwalkSetVital = true;
										break;
									default:
										throw new ArgumentException(String.Format("unknown file attribute: '{0}'.", parameter[j]), "-da");
								}
							}
						}
						else if ("e" == parameter || "exeselfreg" == parameter)
						{
							if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify an .exe file (with optional arguments) for parameter", "-exeselfreg");
							}

							this.selfRegExes.Add(args[i]);

							if (args.Length >= (i + 1) && '#' == args[i + 1][0])
								this.selfRegExeArguments.Add(args[++i].Substring(1)); // Add arguments without introductory '#'.
							else
								this.selfRegExeArguments.Add("");
						}
						else if ("gg" == parameter)
						{
							this.generateGuids = true;
						}
						else if ("s" == parameter || "selfreg" == parameter)
						{
							if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify a file for parameter", "-selfreg");
							}

							this.selfregFiles.Add(args[i]);
						}
						else if ("t" == parameter || "typelib" == parameter)
						{
							if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify a .tlb file for parameter", "-typelib");
							}

							this.selfregTlbs.Add(args[i]);
						}
						else if ("r" == parameter)
						{
							if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify a file for parameter", "-r");
							}

							this.resourceFiles.Add(args[i]);
						}
						else if ("reg" == parameter)
						{
							if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
							{
								throw new ArgumentException("must specify a file for parameter", "-reg");
							}

							this.registryFiles.Add(args[i]);
						}
						else if ("nologo" == parameter)
						{
							this.showLogo = false;
						}
						else if ("?" == parameter || "help" == parameter)
						{
							this.showHelp = true;
						}
						else
						{
							throw new ArgumentException("unknown parameter", String.Concat("-", parameter));
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
									if ('\\' == line[j] && j < line.Length - 1 && '"' == line[j+1])
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
						throw new ArgumentException(String.Concat("unexpected argument on command line: ", arg));
					}
				}
			}

			/// <summary>
			/// Private class to do interop.
			/// </summary>
			private class NativeMethods
			{
				/// <summary>
				/// Gets the short name for a file.
				/// </summary>
				/// <param name="fullPath">Fullpath to file on disk.</param>
				/// <returns>Short name for file.</returns>
				internal static string GetShortPathName(string fullPath)
				{
					StringBuilder shortPath = new StringBuilder(MaxPath, MaxPath);

					// get the short file name
					GetShortPathName(fullPath, shortPath, MaxPath);

					// remove the tildes
					shortPath.Replace('~', '_');

					return shortPath.ToString();
				}

				/// <summary>
				/// Opens a registry key.
				/// </summary>
				/// <param name="key">Base key to open.</param>
				/// <param name="path">Path to subkey to open.</param>
				/// <returns>Handle to new key.</returns>
				internal static IntPtr OpenRegistryKey(UIntPtr key, string path)
				{
					IntPtr newKey = IntPtr.Zero;
					uint disposition = 0;
					uint sam = StandardRightsAll | GenericRead | GenericWrite | GenericExecute | GenericAll;

					int er = RegCreateKeyEx(key, path, 0, null, 0, sam, 0, out newKey, out disposition);

					return newKey;
				}

				/// <summary>
				/// Overrides a registry key.
				/// </summary>
				/// <param name="key">Handle to key to override.</param>
				/// <param name="newKey">Handle to override key.</param>
				internal static void OverrideRegistryKey(UIntPtr key, IntPtr newKey)
				{
					int er = RegOverridePredefKey(key, newKey);
				}

				/// <summary>
				/// Closes a previously open registry key.
				/// </summary>
				/// <param name="key">Handle to key to close.</param>
				internal static void CloseRegistryKey(IntPtr key)
				{
					int er = RegCloseKey(key);
				}

				/// <summary>
				/// Gets the short name for a file.
				/// </summary>
				/// <param name="longPath">Long path to convert to short path.</param>
				/// <param name="shortPath">Short path from long path.</param>
				/// <param name="buffer">Size of short path.</param>
				/// <returns>zero if success.</returns>
				[DllImport("kernel32.dll", EntryPoint="GetShortPathNameW", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
				internal static extern uint GetShortPathName(string longPath, StringBuilder shortPath, [MarshalAs(UnmanagedType.U4)]int buffer);

				/// <summary>
				/// Interop to RegCreateKeyW
				/// </summary>
				/// <param name="key">Handle to base key.</param>
				/// <param name="subkey">Subkey to create.</param>
				/// <param name="reserved">Always 0</param>
				/// <param name="className">Just pass null.</param>
				/// <param name="options">Just pass 0.</param>
				/// <param name="desiredSam">Rights to registry key.</param>
				/// <param name="securityAttributes">Just pass null.</param>
				/// <param name="openedKey">Opened key.</param>
				/// <param name="disposition">Whether key was opened or created.</param>
				/// <returns>Handle to registry key.</returns>
				[DllImport("advapi32.dll", EntryPoint="RegCreateKeyExW", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
				internal static extern int RegCreateKeyEx(UIntPtr key, string subkey, uint reserved, string className, uint options, uint desiredSam, uint securityAttributes, out IntPtr openedKey, out uint disposition);

				/// <summary>
				/// Interop to RegOverridePredefKey
				/// </summary>
				/// <param name="key">Handle to key to override.</param>
				/// <param name="newKey">Handle to override key.</param>
				/// <returns>0 if success.</returns>
				[DllImport("advapi32.dll", EntryPoint="RegOverridePredefKey", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
				internal static extern int RegOverridePredefKey(UIntPtr key, IntPtr newKey);

				/// <summary>
				/// Interop to RegCloseKey
				/// </summary>
				/// <param name="key">Handle to key to close.</param>
				/// <returns>0 if success.</returns>
				[DllImport("advapi32.dll", EntryPoint="RegCloseKey", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
				internal static extern int RegCloseKey(IntPtr key);
			}
		}
	}
}
