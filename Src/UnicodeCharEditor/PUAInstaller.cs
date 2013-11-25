// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PUAInstaller.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PUAInstaller
	{
		/// <summary>
		/// The index of the bidi information in the unicode data, zero based.
		/// </summary>
		private const int kiBidi = 3;

		private List<PUACharacter> m_chars;
		private readonly Dictionary<int, PUACharacter> m_dictCustomChars = new Dictionary<int, PUACharacter>();
		private string m_comment;
		string m_icuDir;
		string m_icuDataDir;

		private string IcuDir
		{
			get
			{
				if (String.IsNullOrEmpty(m_icuDir))
				{
					m_icuDir = Icu.DefaultDirectory;
					if (String.IsNullOrEmpty(m_icuDir))
						throw new Exception("ICU directory not found. Registry value for ICU not set?");
					if (!Directory.Exists(m_icuDir))
						throw new Exception("ICU directory does not exit.  Registry value for ICU set incorrectly?");
				}
				return m_icuDir;
			}
		}

		private string IcuDataDir
		{
			get
			{
				if (String.IsNullOrEmpty(m_icuDataDir))
				{
					m_icuDataDir = Icu.DefaultDataDirectory;
					if (String.IsNullOrEmpty(m_icuDataDir))
						throw new Exception("ICU data directory not found. Registry value for ICU not set?");
					if (!Directory.Exists(m_icuDataDir))
						throw new Exception("ICU data directory does not exit.  Registry value for ICU set incorrectly?");
				}
				return m_icuDataDir;
			}
		}

		#region install PUA characters

		/// <summary>
		/// Installs the PUA characters (PUADefinitions) from the given xml file
		/// We do this by:
		/// 1. Maps the XML file to a list of PUACharacter objects
		/// 2. Sorts the PUA characters
		/// 3. Opens UnicodeDataOverrides.txt for reading and writing
		/// 4. Inserts the PUA characters via their codepoints
		/// 5. Regenerate nfc.txt and nfkc.txt, the input files for gennorm2, which generates
		///    the ICU custom normalization files.
		/// 6. Run "gennorm2" to create the actual binary files used by the ICU normalization functions.
		/// </summary>
		/// <param name="filename">Our XML file containing our PUA Defintions</param>
		public void InstallPUACharacters(string filename)
		{
			try
			{
				// 0. Intro: Prepare files

				// 0.1: File names
				var unidataDir = Path.Combine(IcuDir, "data");
				var unicodeDataFilename = Path.Combine(IcuDir, "UnicodeDataOverrides.txt");
				var originalUnicodeDataFilename = Path.Combine(unidataDir, "UnicodeDataOverrides.txt");
				var nfcOverridesFileName = Path.Combine(IcuDir, "nfcOverrides.txt"); // Intermediate we will generate
				var nfkcOverridesFileName = Path.Combine(IcuDir, "nfkcOverrides.txt"); // Intermediate we will generate

				// 0.2: Create a one-time backup that will not be over written if the file exists
				BackupOrig(unicodeDataFilename);

				// 0.3: Create a stack of files to restore if we encounter and error
				//			This allows us to work with the original files
				// If we are successful we call this.RemoveBackupFiles() to clean up
				// If we are not we call this.RestoreFiles() to restore the original files
				//		and delete the backups
				var unicodeDataBackup = CreateBackupFile(unicodeDataFilename);
				AddUndoFileFrame(unicodeDataFilename, unicodeDataBackup);

				//Initialize and populate the parser if necessary
				// 1. Maps our XML file to a list of PUACharacter objects.
				ParseCustomCharsFile(filename);

				// (Step 1 has been moved before the "intro")
				// 2. Sort the PUA characters
				m_chars.Sort();

				// 3. Open the file for reading and writing
				// 4. Insert the PUA via their codepoints
				InsertCharacters(m_chars.ToArray(), originalUnicodeDataFilename, unicodeDataFilename);

				// 5. Generate the modified normalization file inputs.
				using (var reader = new StreamReader(unicodeDataFilename, Encoding.ASCII))
				{
					using (var writeNfc = new StreamWriter(nfcOverridesFileName, false, Encoding.ASCII))
					using (var writeNfkc = new StreamWriter(nfkcOverridesFileName, false, Encoding.ASCII))
					{
						reader.Peek(); // force autodetection of encoding.
						try
						{
							string line;
							while ((line = reader.ReadLine()) != null)
							{
								if (line.StartsWith("Code") || line.StartsWith("block")) // header line or special instruction
									continue;
								// Extract the first, fourth, and sixth fields.
								var match = new Regex("^([^;]*);[^;]*;[^;]*;([^;]*);[^;]*;([^;]*);").Match(line);
								if (!match.Success)
									continue;
								string codePoint = match.Groups[1].Value.Trim();
								string combiningClass = match.Groups[2].Value.Trim();
								string decomp = match.Groups[3].Value.Trim();
								if (!string.IsNullOrEmpty(combiningClass) && combiningClass != "0")
								{
									writeNfc.WriteLine(codePoint + ":" + combiningClass);
								}
								if (!string.IsNullOrEmpty(decomp))
								{
									if (decomp.StartsWith("<"))
									{
										int index = decomp.IndexOf(">", StringComparison.InvariantCulture);
										if (index < 0)
											continue; // badly formed, ignore it.
										decomp = decomp.Substring(index + 1).Trim();
									}
									// otherwise we should arguably write to nfc.txt
									// If exactly two code points write codePoint=decomp
									// otherwise write codePoint>decomp
									// However, we should not be modifying standard normalization.
									// For now treat them all as compatibility only.
									writeNfkc.WriteLine(codePoint + ">" + decomp);
								}
							}

						}
						finally
						{
							writeNfc.Close();
							writeNfkc.Close();
							reader.Close();
						}
					}
				}


				// 6. Run the "gennorm2" commands to write the actual files
				RunICUTools(unidataDir, nfcOverridesFileName, nfkcOverridesFileName);

				RemoveBackupFiles();
			}
			catch (Exception)
			{
				RestoreFiles();
				throw;
			}
			finally
			{
				RemoveTempFiles();
			}
		}

		private void ParseCustomCharsFile(string filename)
		{
			var ci = CultureInfo.CreateSpecificCulture("en-US");
			m_comment = String.Format("[SIL-Corp] {0} User Added {1}", filename, DateTime.Now.ToString("F", ci));
			m_chars = new List<PUACharacter>();
			var xd = XDocument.Load(filename, LoadOptions.None);
			foreach (var xe in xd.Descendants("CharDef"))
			{
				var xaCode = xe.Attribute("code");
				if (xaCode == null || String.IsNullOrEmpty(xaCode.Value))
					continue;
				var xaData = xe.Attribute("data");
				if (xaData == null || String.IsNullOrEmpty(xaData.Value))
					continue;
				int code;
				if (Int32.TryParse(xaCode.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code) &&
					!m_dictCustomChars.ContainsKey(code))
				{
					var spec = new PUACharacter(xaCode.Value, xaData.Value);
					m_dictCustomChars.Add(code, spec);
					m_chars.Add(spec);
				}
			}

		}

		#region Undo File Stack

		///<summary>
		///</summary>
		public struct UndoFiles
		{
			///<summary></summary>
			public string m_backupFile;
			///<summary></summary>
			public string m_originalFile;
		}

		private readonly List<UndoFiles> m_undoFileStack = new List<UndoFiles>();

		/// <summary>
		/// Create a copy of the backup and source file names for future use.
		/// </summary>
		private void AddUndoFileFrame(string srcFile, string backupFile)
		{
			LogFile.AddVerboseLine("Adding Undo File: <" + backupFile + ">");
			var frame = new UndoFiles {m_originalFile = srcFile, m_backupFile = backupFile};
			m_undoFileStack.Add(frame);
		}

		/// <summary>
		/// Remove the backup files that were created to restore original files if there
		/// were a problem.  If this method is called, there was no problem.  We're
		/// just removing the backup file - much like a temp file at this point.
		/// </summary>
		public void RemoveBackupFiles()
		{
			LogFile.AddVerboseLine("Removing Undo Files --- Start");
			for (var i = m_undoFileStack.Count; --i >= 0; )
			{
				var frame = m_undoFileStack[i];
				if (File.Exists(frame.m_backupFile))
					DeleteFile(frame.m_backupFile);
			}

			LogFile.AddVerboseLine("Removing Undo Files --- Finish");
		}


		/// <summary>
		/// Copies the backup files over the source files and then deletes the backup files.
		/// </summary>
		public void RestoreFiles()
		{
			for (var i = m_undoFileStack.Count; --i >= 0; )
			{
				var frame = m_undoFileStack[i];
				var fi = new FileInfo(frame.m_backupFile);
				if (fi.Exists)
				//				if (File.Exists(frame.backupFile))
				{
					// Use the safe versions of the methods so that the process can continue
					// even if there are errors.
					SafeFileCopyWithLogging(frame.m_backupFile, frame.m_originalFile, true);
					if (fi.Length <= 0)
					{
						// no data in the backup file, remove originalFile also
						SafeDeleteFile(frame.m_originalFile);
					}
					SafeDeleteFile(frame.m_backupFile);
				}
			}
		}
		#endregion

		/// <summary>
		/// This runs genprops and gennames in order to use UnicodeData.txt,
		/// DerivedBidiClass.txt, as well as other *.txt files in unidata to
		/// create <i>icuprefix/</i>uprops.icu and other binary data files.
		/// </summary>
		public void RunICUTools(string unidataDir, string nfcOverridesFileName, string nfkcOverridesFileName)
		{
			// run commands similar to the following (with full paths in quotes)
			//
			//    gennorm2 -o nfc.nrm nfc.txt nfcHebrew.txt nfcOverrides.txt
			//    gennorm2 -o nfkc.nrm nfc.txt nfkc.txt nfcHebrew.txt nfcOverrides.txt nfkcOverrides.txt
			//
			// Note: this compiles the input files to produce the two output files, which ICU loads to customize normalization.

			// Get the icu directory information
			var nfcTxtFileName = Path.Combine(unidataDir, "nfc.txt"); // Original from ICU, installed
			var nfcHebrewFileName = Path.Combine(unidataDir, "nfcHebrew.txt"); // Original from ICU, installed
			var nfkcTxtFileName = Path.Combine(unidataDir, "nfkc.txt"); // Original from ICU, installed

			// The exact name of this directory is built into ICU and can't be changed. It must be this subdirectory
			// relative to the IcuDir (which is the IcuDataDirectory passed to ICU).
			var uniBinaryDataDir = Path.Combine(IcuDir, "icudt50l");
			var nfcBinaryFileName = Path.Combine(uniBinaryDataDir, "nfc.nrm"); // Binary file generated by gennorm2
			var nfkcBinaryFileName = Path.Combine(uniBinaryDataDir, "nfkc.nrm"); // Binary file generated by gennorm2

			// Make a one-time original backup of the files we are about to generate.
			var nfcBackup = CreateBackupFile(nfcBinaryFileName);
			var nfkcBackup = CreateBackupFile(nfkcBinaryFileName);
			AddUndoFileFrame(nfcBinaryFileName, nfcBackup);
			AddUndoFileFrame(nfkcBinaryFileName, nfkcBackup);


			// Clean up the ICU and set the icuDatadir correctly
			Icu.Cleanup();

			var genNorm2 = GetIcuExecutable("gennorm2");

			// run it to generate the canonical binary data.
			var args = String.Format(" -o \"{0}\" \"{1}\" \"{2}\" \"{3}\"",
			   nfcBinaryFileName, nfcTxtFileName, nfcHebrewFileName, nfcOverridesFileName);
			RunProcess(genNorm2, args);

			// run it again to generate the non-canonical binary data.
			args = String.Format(" -o \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\"",
				nfkcBinaryFileName, nfcTxtFileName, nfkcTxtFileName, nfcHebrewFileName, nfcOverridesFileName, nfkcOverridesFileName);
			RunProcess(genNorm2, args);
		}

		private static void RunProcess(string executable, string args)
		{
			using (var gennormProcess = new Process())
			{
				gennormProcess.StartInfo = new ProcessStartInfo
					{
						FileName = executable,
						WorkingDirectory = Path.GetDirectoryName(executable), // lets it find its DLL
						Arguments = args,
						WindowStyle = ProcessWindowStyle.Hidden,
						CreateNoWindow = true,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true
					};

				// For some reason Hidden worked on FW6.0, but not on FW7.0+. NoWindow works!?!

				// Allows us to re-direct the std. output for logging.

				gennormProcess.Start();
				gennormProcess.WaitForExit();
				var ret = gennormProcess.ExitCode;

				// If gen props doesn't run correctly, log what it displays to the standar output
				// and throw and exception
				if (ret != 0)
				{
					if (LogFile.IsLogging())
					{
						LogFile.AddErrorLine("Error running gennorm2:");
						LogFile.AddErrorLine(gennormProcess.StandardOutput.ReadToEnd());
						LogFile.AddErrorLine(gennormProcess.StandardError.ReadToEnd());
					}
					throw new PuaException(ErrorCodes.Gennorm);
				}
			}
		}

		///<summary>
		///</summary>
		public class PuaException : Exception
		{
			readonly ErrorCodes m_ec;
			readonly string m_msg;

			///<summary>
			/// Constructor without a message.
			///</summary>
			public PuaException(ErrorCodes ec)
			{
				m_ec = ec;
			}

			///<summary>
			/// Constructor with a message.
			///</summary>
			public PuaException(ErrorCodes ec, string msg)
			{
				m_ec = ec;
				m_msg = msg;
			}

			/// <summary>
			///
			/// </summary>
			public override string ToString()
			{
				var bldr = new StringBuilder();
				switch (m_ec)
				{
					case ErrorCodes.None:
						bldr.AppendLine("No Value set yet - None");
						break;
					case ErrorCodes.Success:
						bldr.AppendLine("No Errors");
						break;
					case ErrorCodes.CommandLine:
						bldr.AppendLine("Invalid arguments on the command line");
						break;
					case ErrorCodes.CancelAccessFailure:
						bldr.AppendLine(
							"The user cancelled the install while ICU files were inaccessible due to memory mapping from another process.");
						break;
					case ErrorCodes.LdBaseLocale:
						bldr.AppendLine("Base locale ontains invalid file names");
						break;
					case ErrorCodes.LdNewLocale:
						bldr.AppendLine("Blank New Locale");
						break;
					case ErrorCodes.LdLocaleResources:
						bldr.AppendLine("Local Resources mis-matching braces");
						break;
					case ErrorCodes.LdFileName:
						bldr.AppendLine("Invalid LD Name or File");
						break;
					case ErrorCodes.LdBadData:
						bldr.AppendLine("Invalid Data");
						break;
					case ErrorCodes.LdParsingError:
						bldr.AppendLine("Not able to parse/read the XML Language Definition file.");
						break;
					case ErrorCodes.LdUsingISO3CountryName:
						bldr.AppendLine("The Country Name is already used as an ISO3 value.");
						break;
					case ErrorCodes.LdUsingISO3LanguageName:
						bldr.AppendLine("The Language Name is already used as an ISO3 value");
						break;
					case ErrorCodes.LdUsingISO3ScriptName:
						bldr.AppendLine("The Script Name is already used as an ISO3 value");
						break;
					case ErrorCodes.ResIndexFile:
						bldr.AppendLine("GENRB error creating res_index.res file");
						break;
					case ErrorCodes.RootFile:
						bldr.AppendLine("GENRB error creating root.res file");
						break;
					case ErrorCodes.NewLocaleFile:
						bldr.AppendLine("GENRB error creating new locale .res file");
						break;
					case ErrorCodes.GeneralFile:
						bldr.AppendLine("GENRB error creating a file");
						break;
					case ErrorCodes.FileRead:
						bldr.AppendLine("Error reading file");
						break;
					case ErrorCodes.FileWrite:
						bldr.AppendLine("Error writing file");
						break;
					case ErrorCodes.FileRename:
						bldr.AppendLine("Error renaming file");
						break;
					case ErrorCodes.FileNotFound:
						bldr.AppendLine("File not found");
						break;
					case ErrorCodes.RootTxtFileNotFound:
						bldr.AppendLine("root.txt file not found");
						break;
					case ErrorCodes.ResIndexTxtFileNotFound:
						bldr.AppendLine("res_index.txt file not found");
						break;
					case ErrorCodes.RootResFileNotFound:
						bldr.AppendLine("*_root.res file not found");
						break;
					case ErrorCodes.ResIndexResFileNotFound:
						bldr.AppendLine("res_index.res file not found");
						break;
					case ErrorCodes.RootTxtInvalidCustomResourceFormat:
						bldr.AppendLine("root.txt Custom resource is not in the proper format.");
						break;
					case ErrorCodes.RootTxtCustomResourceNotFound:
						bldr.AppendLine("root.txt Custom resource item was not found.");
						break;
					case ErrorCodes.RegistryIcuDir:
						bldr.AppendLine("Icu Dir not in the Registry.");
						break;
					case ErrorCodes.RegistryIcuLanguageDir:
						bldr.AppendLine("Icu Data Language not in the Registry.");
						break;
					case ErrorCodes.RegistryIcuTemplatesDir:
						bldr.AppendLine("Icu Code Templates not in the Registry.");
						break;
					case ErrorCodes.Gennames:
						bldr.AppendLine("ICU gennames exited with an error");
						break;
					case ErrorCodes.Genprops:
						bldr.AppendLine("ICU genprops exited with an error");
						break;
					case ErrorCodes.Gennorm:
						bldr.AppendLine("ICU gennorm exited with an error");
						break;
					case ErrorCodes.Genbidi:
						bldr.AppendLine("ICU genbidi exited with an error");
						break;
					case ErrorCodes.Gencase:
						bldr.AppendLine("ICU gencase exited with an error");
						break;
					case ErrorCodes.PUAOutOfRange:
						bldr.AppendLine("Cannot insert given character: not within the PUA range");
						break;
					case ErrorCodes.PUADefinitionFormat:
						bldr.AppendLine("Given an inproperly formatted character definition, either in the LDF or UnicodeData.txt");
						bldr.AppendLine("Note: we assume good PUA characters, so we don't check every detail.");
						bldr.AppendLine("We just happened to discover an error while parsing. ");
						break;
					case ErrorCodes.ICUDataParsingError:
						bldr.AppendLine("Error parsing ICU file, not within expected format");
						break;
					case ErrorCodes.ICUNodeAccessError:
						bldr.AppendLine("Error while trying to access an ICU datafile node.");
						break;
					case ErrorCodes.ProgrammingError:
						bldr.AppendLine("Programming error");
						break;
					case ErrorCodes.NonspecificError:
						bldr.AppendLine("Nonspecific error");
						break;
					default:
						bldr.AppendLine("Unknown error");
						break;
				}
				if (!String.IsNullOrEmpty(m_msg))
					bldr.Append(m_msg);
				return bldr.ToString();
			}
		}

		private string GetIcuExecutable(string exeName)
		{
#if !__MonoCS__
			string codeBaseUri = Assembly.GetExecutingAssembly().CodeBase;
			string path = Path.GetDirectoryName(FileUtils.StripFilePrefix(codeBaseUri));

			var result= Path.Combine(path, exeName + ".exe");
			if (File.Exists(result))
				return result; // typical end-user machine
			// developer machine, executing assembly is in output/debug.
			return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(path)),
				Path.Combine("DistFiles", Path.Combine("Windows", exeName + ".exe")));
#else
	// TODO-Linux: Review - is the approach of expecting execuatble location to be in PATH ok?
			return exeName;
#endif
		}

		/// <summary>
		/// Inserts the given PUADefinitions (any Unicode character) into the UnicodeData.txt file.
		///
		/// This accounts for all the cases of inserting into the "first/last" blocks.  That
		/// is, it will split the blocks into two or move the first and last tags to allow a
		/// codepoint to be inserted correctly.
		///
		/// Also, this accounts for Hexadecimal strings that are within the unicode range, not
		/// just four digit unicode files.
		///
		/// <list type="number">
		/// <listheader>Assumptions made about the format</listheader>
		/// <item>The codepoints are in order</item>
		/// <item>There first last block will always have no space between the word first and
		/// the following ">"</item>
		/// <item>No other data entries contain the word first followed by a ">"</item>
		/// <item>There will always be a "last" on the line directly after a "first".</item>
		/// </list>
		///
		/// </summary>
		/// <remarks>
		/// Pseudocode for inserting lines:
		///	if the unicodePoint	is a first tag
		///		Get	first and last uncodePoint range
		///		Stick into array all the xmlPoints that fit within the uncodePoint range
		///			Look at the next xmlPoint
		///			if there are any
		///				call WriteCodepointBlock subroutine
		///	else if the unicodePoint is greater than the last point but less than or equal to "the xmlPoint"
		///		insert the missing line or replace	the	line
		///		look at	the	next xmlPoint
		///	else
		///		do nothing except write	the	line
		///</remarks>
		/// <param name="puaDefinitions">An array of PUADefinitions to insert into UnicodeDataOverrides.txt.</param>
		/// <param name="originalOverrides">original to merge into</param>
		/// <param name="customOverrides">where to write output</param>
		private void InsertCharacters(IPuaCharacter[] puaDefinitions, string originalOverrides, string customOverrides)
		{
			// Open the file for reading and writing
			LogFile.AddVerboseLine("StreamReader on <" + originalOverrides + ">");
			using (var reader = new StreamReader(originalOverrides, Encoding.ASCII))
			{
				reader.Peek();	// force autodetection of encoding.
				using (var writer = new StreamWriter(customOverrides, false, Encoding.ASCII))
				{
					try
					{
						// Insert the PUA via their codepoints

						string line;
						var lastCode = 0;
						// Start looking at the first codepoint
						var codeIndex = 0;
						var newCode = Convert.ToInt32(puaDefinitions[codeIndex].CodePoint, 16);

						// Used to find the type for casting ArrayLists to IPuaCharacter[]
						//var factory = new PuaCharacterFactory();
						//var puaCharForType = factory.Create("");
						//var puaCharType = puaCharForType.GetType();

						//While there is a line to be read in the file
						while ((line = reader.ReadLine()) != null)
						{
							// skip entirely blank lines
							if (line.Length <= 0)
								continue;
							if (line.StartsWith("Code") || line.StartsWith("block")) // header line or special instruction
							{
								writer.WriteLine(line);
								continue;
							}

							//Grab codepoint
							var strFileCode = line.Substring(0, line.IndexOf(';')).Trim(); // current code in file
							var fileCode = Convert.ToInt32(strFileCode, 16);

							// If the new codepoint is greater than the last one processed in the file, but
							// less than or equal to the current codepoint in the file.
							if (newCode > lastCode && newCode <= fileCode)
							{
								while (newCode <= fileCode)
								{
									LogCodepoint(puaDefinitions[codeIndex].CodePoint);

									// Replace the line with the new PuaDefinition
									writer.WriteLine("{0} #{1}", puaDefinitions[codeIndex], m_comment);
									lastCode = newCode;

									// Look for the next PUA codepoint that we wish to insert, we are done
									// with this one If we are all done, push through the rest of the file.
									if (++codeIndex >= puaDefinitions.Length)
									{
										// Write out the original top of the section if it hasn't been replaced.
										if (fileCode != lastCode)
										{
											writer.WriteLine(line);
										}
										while ((line = reader.ReadLine()) != null)
											writer.WriteLine(line);
										break;
									}
									newCode = Convert.ToInt32(puaDefinitions[codeIndex].CodePoint, 16);
								}
								if (codeIndex >= puaDefinitions.Length)
									break;
								// Write out the original top of the section if it hasn't been replaced.
								if (fileCode != lastCode)
								{
									writer.WriteLine(line);
								}
							}
							//if it's not a first tag and the codepoints don't match
							else
							{
								writer.WriteLine(line);
							}
							lastCode = fileCode;
						}
						// Output any codepoints after the old end
						while (codeIndex < puaDefinitions.Length)
						{
							LogCodepoint(puaDefinitions[codeIndex].CodePoint);

							// Add a line with the new PuaDefinition
							writer.WriteLine("{0} #{1}", puaDefinitions[codeIndex], m_comment);
							codeIndex++;
						}
					}
					finally
					{
						writer.Flush();
						writer.Close();
						reader.Close();
					}
				}
			}
		}

		#region Temporary file processing

		private readonly List<string> m_tempFiles = new List<string>();

		/// <summary>
		/// Add a file to the list of temporary files.
		/// </summary>
		private void AddTempFile(string strFile)
		{
			LogFile.AddVerboseLine("Adding Temp File: <" + strFile + ">");
			m_tempFiles.Add(strFile);
		}

		/// <summary>
		/// Delete all files in the list of temporary files.
		/// </summary>
		private void RemoveTempFiles()
		{
			LogFile.AddVerboseLine("Removing Temp Files --- Start");
			foreach (var str in m_tempFiles)
				DeleteFile(str);
			LogFile.AddVerboseLine("Removing Temp Files --- Finish");
		}

		#endregion

		/// <summary>
		/// Checks whether the IPuaCharacter needs to be added to the lists, and adds if necessary.
		/// </summary>
		/// <param name="line">The line of the UnicodeData.txt that will be replaced.
		///		If a property matches, the value will not be added to the lists.</param>
		/// <param name="puaDefinition">The puaCharacter that is being inserted.</param>
		/// <param name="addToBidi"></param>
		/// <param name="addToNorm"></param>
		/// <param name="removeFromBidi"></param>
		/// <param name="removeFromNorm"></param>
		private static void AddToLists(string line, IPuaCharacter puaDefinition,
			List<IUcdCharacter> addToBidi, List<IUcdCharacter> removeFromBidi, List<IUcdCharacter> addToNorm,
			List<IUcdCharacter> removeFromNorm)
		{
#if DEBUGGING_SOMETHING
			int temp = line.IndexOf("F16F");	// junk for a debugging breakpoint...
			temp++;
#endif

			// If the bidi type doesn't match add it to the lists to replace
			var bidi = GetField(line, kiBidi + 1);
			if (!puaDefinition.Bidi.Equals(bidi))
			{
				var factory = new BidiCharacterFactory();
				removeFromBidi.Add(new BidiCharacter(line));
				addToBidi.Add(factory.Create(puaDefinition));
			}
			// If the new character doesn't match the decomposition, add it to the lists
			string decomposition = GetField(line, 5);
			string puaRawDecomp = puaDefinition.Data[5 - 1];
			if (decomposition != puaRawDecomp)
			{
				var factory = new NormalizationCharacterFactory();
				// Perform a quick attempt to remove basic decompositions
				// TODO: Extend this to actually remove more complicated entries?
				// Currently this will remove anything that we have added.
				if (decomposition.Trim() != string.Empty)
				{
					// If there is a '>' character in the decomposition field
					// then it is a compatability decomposition
					if (decomposition.IndexOf(">") != -1)
						removeFromNorm.Add(factory.Create(line, "NFKD_QC; N"));
					removeFromNorm.Add(factory.Create(line, "NFD_QC; N"));
				}
				// Add the normalization to the lists, if necessary.
				if (puaDefinition.Decomposition != string.Empty)
				{
					// Add a canonical decomposition if necessary
					if (puaDefinition.DecompositionType == string.Empty)
						addToNorm.Add(factory.Create(puaDefinition, "NFD_QC; N"));
					// Add a compatability decomposition always
					// (Apparently canonical decompositions are compatability decompositions,
					//		but not vise-versa
					addToNorm.Add(factory.Create(puaDefinition, "NFKD_QC; N"));
				}
			}
		}


		/// <summary>
		/// Retrieves the given field from the given UnicodeData.txt line
		/// </summary>
		/// <param name="line">A line in the format of the UnicodeData.txt file</param>
		/// <param name="field">The field index</param>
		/// <returns>The value of the field</returns>
		static string GetField(string line, int field)
		{
			// Find the bidi field
			return line.Split(new[] { ';' })[field];
		}

		/*
		Character Decomposition Mapping

		The tags supplied with certain decomposition mappings generally indicate formatting
		information. Where no such tag is given, the mapping is canonical. Conversely, the
		presence of a formatting tag also indicates that the mapping is a compatibility mapping
		and not a canonical mapping. In the absence of other formatting information in a
		compatibility mapping, the tag is used to distinguish it from canonical mappings.

		In some instances a canonical mapping or a compatibility mapping may consist of a single
		character. For a canonical mapping, this indicates that the character is a canonical
		equivalent of another single character. For a compatibility mapping, this indicates that
		the character is a compatibility equivalent of another single character. The compatibility
		formatting tags used are:

			Tag 		Description
			----------	-----------------------------------------
			<font>		A font variant (e.g. a blackletter form).
			<noBreak>	A no-break version of a space or hyphen.
			<initial>	An initial presentation form (Arabic).
			<medial>	A medial presentation form (Arabic).
			<final>		A final presentation form (Arabic).
			<isolated>	An isolated presentation form (Arabic).
			<circle>	An encircled form.
			<super>		A superscript form.
			<sub>		A subscript form.
			<vertical>	A vertical layout presentation form.
			<wide>		A wide (or zenkaku) compatibility character.
			<narrow>	A narrow (or hankaku) compatibility character.
			<small>		A small variant form (CNS compatibility).
			<square>	A CJK squared font variant.
			<fraction>	A vulgar fraction form.
			<compat>	Otherwise unspecified compatibility character.

		Reminder: There is a difference between decomposition and decomposition mapping. The
		decomposition mappings are defined in the UnicodeData, while the decomposition (also
		termed "full decomposition") is defined in Chapter 3 to use those mappings recursively.

			* The canonical decomposition is formed by recursively applying the canonical
			  mappings, then applying the canonical reordering algorithm.

			* The compatibility decomposition is formed by recursively applying the canonical and
			  compatibility mappings, then applying the canonical reordering algorithm.


		Decompositions and Normalization

		Decomposition is specified in Chapter 3. UAX #15: Unicode Normalization Forms [Norm]
		specifies the interaction between decomposition and normalization. That report specifies
		how the decompositions defined in UnicodeData.txt are used to derive normalized forms of
		Unicode text.

		Note that as of the 2.1.9 update of the Unicode Character Database, the decompositions in
		the UnicodeData.txt file can be used to recursively derive the full decomposition in
		canonical order, without the need to separately apply canonical reordering. However,
		canonical reordering of combining character sequences must still be applied in
		decomposition when normalizing source text which contains any combining marks.

		The QuickCheck property values are as follows:

		Value 	Property	 	Description
		-----	--------		-----------------------------------
		No		NF*_QC			Characters that cannot ever occur in the respective normalization
								form.  See Decompositions and Normalization.
		Maybe 	NFC_QC,NFKC_QC	Characters that may occur in in the respective normalization,
								depending on the context. See Decompositions and Normalization.
		Yes		n/a				All other characters. This is the default value, and is not
								explicitly listed in the file.



		*/
		/// <summary>
		/// Write a codepoint block, inserting the necessary codepoints properly.
		/// </summary>
		/// <param name="writer">UnicodeData.txt file to write lines to/</param>
		/// <param name="blockName">The name of the block (e.g. "Private Use")</param>
		/// <param name="beginning">First codepoint in the block</param>
		/// <param name="end">Last codepoint in the free block</param>
		/// <param name="puaCharacters">An array of codepoints within the block, including the ends.
		///		DO NOT pass in points external to the free block.</param>
		///	<param name="data">A string that contains all of our properties and such for the character range</param>
		///	<param name="addToBidi">A list of UCD Characters to remove from the DerivedBidiClass.txt file</param>
		///	<param name="removeFromBidi">A list of UCD Characters to add to the DerivedBidiClass.txt file</param>
		///	<param name="addToNorm">A list of UCD Characters to remove</param>
		///	<param name="removeFromNorm">A list of UCD Characters to add</param>
		private void WriteCodepointBlock(StreamWriter writer, string blockName, string beginning, string end,
			IEnumerable<IPuaCharacter> puaCharacters, string data, List<IUcdCharacter> addToBidi, List<IUcdCharacter> removeFromBidi,
			List<IUcdCharacter> addToNorm, List<IUcdCharacter> removeFromNorm)
		{
			//Write each entry
			foreach (var puaCharacter in puaCharacters)
			{
				LogCodepoint(puaCharacter.CodePoint);

				// Construct an equivelant UnicodeData.txt line
				var line = puaCharacter.CodePoint + ";" + blockName
					+ data.Substring(data.IndexOf(';'));
				AddToLists(line, puaCharacter, addToBidi, removeFromBidi, addToNorm, removeFromNorm);

				//If the current xmlCodepoint is the same as the beginning codepoint
				if (puaCharacter.CompareTo(beginning) == 0)
				{
					//Shift the beginning down one
					beginning = AddHex(beginning, 1);
					WriteUnicodeDataLine(puaCharacter, writer);
				}
				//If the current xmlCodepoint is between the beginning and end
				else if (puaCharacter.CompareTo(end) != 0)
				{
					//We're writing a range block below the current xmlCodepoint
					WriteRange(writer, beginning, AddHex(puaCharacter.CodePoint, -1), blockName, data);
					//Writes the current xmlCodepoint line
					WriteUnicodeDataLine(puaCharacter, writer);
					//Increment the beginning by one
					beginning = AddHex(puaCharacter.CodePoint, 1);
				}
				//If the current xmlCodepoint is the same as the end codepoint
				else
				{
					//Moves the end down a codepoint address
					end = AddHex(end, -1);
					//Write our range of data
					WriteRange(writer, beginning, end, blockName, data);
					//Writes the current line
					WriteUnicodeDataLine(puaCharacter, writer);
					return;
				}
			}
			//Write our range of data
			WriteRange(writer, beginning, end, blockName, data);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a number to a hex string
		/// </summary>
		/// <param name="hex">The hex string you want to add</param>
		/// <param name="num">The number you want to add to the string</param>
		/// <returns>the hex string sum</returns>
		/// ------------------------------------------------------------------------------------
		public static string AddHex(string hex, int num)
		{
			//A long because that's the return type required for ToString
			long sum = Convert.ToInt64(hex, 16) + num;
			return Convert.ToString(sum, 16).ToUpper();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subtracts two hex numbers and returns an integer value of their difference.
		/// i.e. returns an int representing hex-hex2
		/// </summary>
		/// <param name="hex">A string value representing a hexadecimal number</param>
		/// <param name="hex2">A string value representing a hexadecimal number</param>
		/// <returns>The difference between the two values.</returns>
		/// ------------------------------------------------------------------------------------
		private static int SubHex(string hex, string hex2)
		{
			return (int)(Convert.ToInt64(hex, 16) - Convert.ToInt64(hex2, 16));
		}

		/// <summary>
		/// Writes a UnicodeData.txt style line including comments.
		/// </summary>
		/// <param name="puaChar">The character to write</param>
		/// <param name="tw">The writer to write it to.</param>
		private void WriteUnicodeDataLine(IPuaCharacter puaChar, TextWriter tw)
		{
			tw.WriteLine("{0} #{1}", puaChar, m_comment);
		}

		/// <summary>
		/// Updates a UCD style file as necessary.
		/// so that the entries match the ones we just inserted into UnicodeData.txt.
		/// </summary>
		/// <remarks>
		/// A UCD file is a "Unicode Character Database" text file.
		/// The specific documentation can be found at:
		/// http://www.unicode.org/Public/UNIDATA/UCD.html#UCD_File_Format
		/// </remarks>
		private void UpdateUCDFile(List<IUcdCharacter> addToUCD, List<IUcdCharacter> removeFromUCD)
		{
			string ucdFilenameWithoutPath;

			// Get the file name we want to modify.
			if (addToUCD.Count == 0)
			{
				if (removeFromUCD.Count == 0)
					// If we aren't supposed to change anything, we are done.
					return;
				ucdFilenameWithoutPath = (removeFromUCD[0]).FileName;
			}
			else
				ucdFilenameWithoutPath = (addToUCD[0]).FileName;

			// Create a temporary file to write to and backup the original
			var unidataDir = Path.Combine(Path.Combine(IcuDir, "data"), "unidata");
			var ucdFilename = Path.Combine(unidataDir, ucdFilenameWithoutPath);
			var ucdFileTemp = CreateTempFile(ucdFilename);
			// Add the temp file to a list of files to be deleted when we are done.
			AddTempFile(ucdFileTemp);

			//All the streams necessary to read and write for the Bidi text file
			LogFile.AddVerboseLine("StreamReader on <" + ucdFilename + ">");
			using (var reader = new StreamReader(ucdFilename, Encoding.ASCII))
			{
				//These 2 streams are used to allow us to pass through the file twice w/out writing to the hard disk
				using (var stringWriter = new StringWriter())
				{
					//Writes out the final file (to a temp file that will be copied upon success)
					using (var writer = new StreamWriter(ucdFileTemp, false, Encoding.ASCII))
					{
						//Does our first pass through of the file, removing necessary lines
						ModifyUCDFile(reader, stringWriter, removeFromUCD, false);

						using (var stringReader = new StringReader(stringWriter.ToString()))
						{
							// Does the second pass through the file, adding necessary lines
							ModifyUCDFile(stringReader, writer, addToUCD, true);

							// write file
							writer.Flush();
						}
					}
				}
			}

			// If we get this far without an exception, copy the file over the original
			FileCopyWithLogging(ucdFileTemp, ucdFilename, true);
		}

		/// <summary>
		/// Makes three debug files for the DerivedBidiClass.txt:
		///
		/// A: Saves the "in-between" state (after deleting, before adding)
		/// B: Saves a list of what we are adding and deleting.
		/// C: Saves an actual file that doesn't add or delete anything.
		///		This will update the numbers, but won't do anything else
		///
		/// </summary>
		/// <param name="stringWriter">Used to get the file after deleting.</param>
		///<param name="addToBidi"></param>
		///<param name="removeFromBidi"></param>
		private void MakeDebugBidifiles(StringWriter stringWriter, IEnumerable<IUcdCharacter> addToBidi, IEnumerable<IUcdCharacter> removeFromBidi)
		{
			// TODO: Do we need to keep this debug test code?
			// This region makes some debug files

			// A: Saves the "in-between" state (after deleting, before adding)
			var unidataDir = Path.Combine(Path.Combine(IcuDir, "data"), "unidata");
			using (var middle = new StreamWriter(Path.Combine(unidataDir, "DerivedBidiClassMID.txt"),
				false, Encoding.ASCII))
			{
				middle.WriteLine(stringWriter.ToString());
				middle.Close();
			}

			// B: Saves a list of what we are adding and deleting.
			using (var lists = new StreamWriter(Path.Combine(unidataDir, "LISTS.txt"),
				false, Encoding.ASCII))
			{
				lists.WriteLine("Add:");
				foreach (var ch in addToBidi)
				{
					lists.WriteLine(ch.ToBidiString());
				}
				lists.WriteLine("Remove:");
				foreach (var ch in removeFromBidi)
				{
					lists.WriteLine(ch.ToBidiString());
				}
				lists.Close();
			}

			// C: Saves an actual file that doesn't add or delete anything.
			//This will update the numbers, but won't do anything else
			LogFile.AddVerboseLine("StreamReader on <" + "DerivedBidiClass.txt" + ">");
			using (var countReader = new StreamReader(Path.Combine(unidataDir, "DerivedBidiClass.txt"), true))
			{
				using (var countWriter = new StreamWriter(Path.Combine(unidataDir, "DerivedBidiClassCOUNT.txt"),
					false, Encoding.ASCII))
				{
					ModifyUCDFile(countReader, countWriter, new List<IUcdCharacter>(), true);
				}
			}
			// End of making debug files
		}

		/// <summary>
		/// This function will add or remove the given PUA characters from the given
		/// DerivedBidiClass.txt file by either inserting or not inserting them as necessary
		/// as it reads through the file in a single pass from <code>tr</code> to <code>tw</code>.
		/// </summary>
		/// <remarks>
		/// <list type="number">
		/// <listheader><description>Assumptions</description></listheader>
		/// <item><description>The like Bidi values are grouped together</description></item>
		/// </list>
		///
		/// <list type="number">Non Assumptions:
		/// <item><description>That comments add any information to the file.  We don't use comments for parsing.</description></item>
		/// <item><description>That "blank" lines appear only between different bidi value sections.</description></item>
		/// <item><description>That the comments should be in the format:
		///		# field2 [length of range]  Name_of_First_Character..Name_of_Last_Charachter
		///		(If it is not, we'll just ignore.</description></item>
		/// </list>
		/// </remarks>
		/// <param name="tr">A reader with information DerivedBidiData.txt</param>
		/// <param name="tw">A writer with information to write to DerivedBidiData.txt</param>
		/// <param name="ucdCharacters">A list of PUACharacters to either add or remove from the file</param>
		/// <param name="add">Whether to add or remove the given characters</param>
		private void ModifyUCDFile(TextReader tr, TextWriter tw,
			List<IUcdCharacter> ucdCharacters, bool add)
		{
			if (ucdCharacters.Count == 0)
			{
				// There is no point in processing this file if we aren't going to do anything to it.
				tw.Write(tr.ReadToEnd());
				// Allows us to know that there will be at least on ucdCharacter that we can access to get some properties
				return;
			}

			//contains our current line
			// not null so that we get into the while loop
			var line = "unused value";
			//Bidi class value from the previous line
			var lastProperty = "blank";
			//Bidi class value from the current line

			// the index of the IPuaCharacter the we are currently trying to insert
			// Note, the initial value will never be used, because there will be no
			//		bidi class value called "blank" in the file, thus it will be initialized before it is every used
			//		but VS requires an initialization value "just in case"
			int codeIndex = -1;
			// If we have read in the line already we want to use this line for the loop, set this to be true.
			bool dontRead = false;

			//Count the number of characters in each range
			int rangeCount = 0;

			//While there is a line to be read in the file

			while ((dontRead && line != null) || (line = tr.ReadLine()) != null)
			{
				dontRead = false;
				if (HasBidiData(line))
				{
					// We found another valid codepoint, increment the count
					IncrementCount(ref rangeCount, line);

					var currentProperty = GetProperty(line);

					// If this is a new section of bidi class values
					if (!ucdCharacters[0].SameRegion(currentProperty, lastProperty))
					{
						lastProperty = currentProperty;
						// Find one of the ucdCharacters in this range in the list of ucdCharacters to add.
						var fFound = false;
						for (codeIndex = 0; codeIndex < ucdCharacters.Count; codeIndex++)
						{
							var ch = ucdCharacters[codeIndex];
							if (ch != null && ch.CompareTo(currentProperty) == 0)
							{
								fFound = true;
								break;
							}
						}

						// if we don't have any characters to put in this section
						if (!fFound)
						{
							tw.WriteLine(line);
							line = ReadToEndOfSection(tr, tw, lastProperty, rangeCount, ucdCharacters[0]);
							rangeCount = 0;
							dontRead = true;
							continue;
						}
					}

					#region insert_the_PUACharacter
					//Grab codepoint
					string code = line.Substring(0, line.IndexOf(';')).Trim();

					//If it's a range of codepoints
					if (code.IndexOf('.') != -1)
					{
						#region if_range
						//Grabs the end codepoint
						string endCode = code.Substring(code.IndexOf("..") + 2).Trim();
						code = code.Substring(0, code.IndexOf("..")).Trim();

						//A dynamic array that contains our range of codepoints and the properties to go with it
						var codepointsWithinRange = new List<IUcdCharacter>();

						// If the IPuaCharacter we want to insert is before the range
						while (
							//If this is the last one stop looking for more
							StillInRange(codeIndex, ucdCharacters, currentProperty) &&
							// For every character before the given value
							(ucdCharacters[codeIndex]).CompareCodePoint(code) < 0
							)
						{
							//Insert characters before the code
							AddUCDLine(tw, ucdCharacters[codeIndex], add);
							codeIndex++;
						}
						while (
							//If this is the last one stop looking for more
							StillInRange(codeIndex, ucdCharacters, currentProperty) &&
							// While our xmlCodepoint satisfies: code <= xmlCodepoint <= endCode
							(ucdCharacters[codeIndex]).CompareCodePoint(endCode) < 1
							)
						{
							//Adds the puaCharacter to the list of codepoints that are in range
							codepointsWithinRange.Add(ucdCharacters[codeIndex]);
							codeIndex++;
						}
						//If we found any codepoints in the range to insert
						if (codepointsWithinRange.Count > 0)
						{
							#region parse_comments
							//Do lots of smart stuff to insert the PUA characters into the block
							string generalCategory = "";
							//Contains the beginning and ending range names
							string firstName = "";
							string lastName = "";

							//If a comment exists on the line in the proper format
							// e.g.   ---  # --- [ --- ] --- ... ---
							if (line.IndexOf('#') != -1 && line.IndexOf('[') != -1
								&& (line.IndexOf('#') <= line.IndexOf('[')))
							{
								//Grabs the general category
								generalCategory = line.Substring(line.IndexOf('#') + 1, line.IndexOf('[') - line.IndexOf('#') - 1).Trim();
							}
							//find the index of the second ".." in the line
							int indexDotDot = line.Substring(line.IndexOf(']')).IndexOf("..");
							if (indexDotDot != -1)
								indexDotDot += line.IndexOf(']');

							//int cat = line.IndexOf(']') ;

							if (line.IndexOf('#') != -1 && line.IndexOf('[') != -1 && line.IndexOf(']') != -1 && indexDotDot != -1
								&& (line.IndexOf('#') < line.IndexOf('['))
								&& (line.IndexOf('[') < line.IndexOf(']'))
								&& (line.IndexOf(']') < indexDotDot)
								)
							{
								//Grab the name of the first character in the range
								firstName = line.Substring(line.IndexOf(']') + 1, indexDotDot - line.IndexOf(']') - 1).Trim();
								//Grab the name of the last character in the range
								lastName = line.Substring(indexDotDot + 2).Trim();
							}
							#endregion
							WriteBidiCodepointBlock(tw, code, endCode, codepointsWithinRange,
								generalCategory, firstName, lastName, add);
						}
						else
						{
							tw.WriteLine(line);
						}
						#endregion
					}
					//if the codepoint in the file is equal to the codepoint that we want to insert
					else
					{
						if (MiscUtils.CompareHex(code, ucdCharacters[codeIndex].CodePoint) > 0)
						{
							// Insert the new PuaDefinition before the line (as well as any others that might be)
							while (
								//If this is the last one stop looking for more
								StillInRange(codeIndex, ucdCharacters, currentProperty) &&
								// For every character before the given value
								ucdCharacters[codeIndex].CompareCodePoint(code) < 0
								)
							{
								//Insert characters before the code
								AddUCDLine(tw, ucdCharacters[codeIndex], add);
								codeIndex++;
							}
						}
						//if the codepoint in the file is equal to the codepoint that we want to insert
						if (StillInRange(codeIndex, ucdCharacters, currentProperty) &&
							(code == ucdCharacters[codeIndex].CodePoint))
						{
							// Replace the line with the new PuaDefinition
							AddUCDLine(tw, ucdCharacters[codeIndex], add);
							// Look for the next PUA codepoint that we wish to insert
							codeIndex++;
						}
						//if it's not a first tag and the codepoints don't match
						else
						{
							tw.WriteLine(line);
						}
					}

					//If we have no more codepoints to insert in this section, then just finish writing this section
					if (!StillInRange(codeIndex, ucdCharacters, currentProperty))
					{
						line = ReadToEndOfSection(tr, tw, lastProperty, rangeCount, ucdCharacters[0]);
						rangeCount = 0;
						dontRead = true;
						continue;
					}
					#endregion
				}
				//If it's a comment, simply write it out
				else
				{
					// find the total count comment and replace it with the current count.
					if (line.ToLowerInvariant().IndexOf("total code points") != -1)
					{
						line = "# Total code points:" + rangeCount;
						rangeCount = 0;
					}
					tw.WriteLine(line);
				}
			}
		}

		#region ModifyUCDFile_helper_methods

		/// <summary>
		/// Read to the end of a given section of matching bidi class values.
		/// Passes everything read through to the writer.
		/// If this finds a section count comment, it will replace it with the current count.
		/// </summary>
		/// <param name="reader">The reader to read through.</param>
		///<param name="writer"></param>
		///<param name="lastProperty">The section we need to reed to the end of.</param>
		/// <param name="currentCount">The count of characters found before reading to the end of the section.</param>
		/// <param name="ucdCharacter">UCD Character used to know what kind of UCD file we are parsing.
		///		The actual contencts of the UCD Character are ignored.</param>
		/// <returns>The first line of the next section.</returns>
		private static string ReadToEndOfSection(TextReader reader, TextWriter writer, string lastProperty, int currentCount, IUcdCharacter ucdCharacter)
		{
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				// if there is a bidi class value to read
				if (HasBidiData(line))
				{
					// increments the current count of codepoints found so far.
					IncrementCount(ref currentCount, line);

					// read the bidi value from the line
					var currentProperty = GetProperty(line);

					// if it isn't in the current section we are done with section.
					if (!ucdCharacter.SameRegion(currentProperty, lastProperty))
						break;
				}
				// if its a comment, find the total count comment and replace it with the current count.
				else if (line.ToLowerInvariant().IndexOf("total code points") != -1)
				{
					line = "# Total code points: " + currentCount;
					currentCount = 0;
				}
				// Write through all lines except the first line of the next section
				writer.WriteLine(line);
			}

			// Return the last line that we read
			// This is the first line of the next section, so someone will probably want to parse it.
			return line;
		}



		/// <summary>
		/// Prints a message to the console when storing a Unicode character.
		/// </summary>
		static void LogCodepoint(string code)
		{
			if (LogFile.IsLogging())
				LogFile.AddErrorLine("Storing definition for Unicode character: " + code);
		}

		/// <summary>
		/// Given a line containing a valid code or code range, increments the count to include the code or code range.
		/// Uses XXXX..YYYY style range.
		/// </summary>
		/// <param name="currentCount">The current count to increment</param>
		/// <param name="line">The DerivedBidiClass.txt style line to use to increment.</param>
		/// <returns></returns>
		private static void IncrementCount(ref int currentCount, string line)
		{
			//Grab codepoint
			var code = line.Substring(0, line.IndexOf(';')).Trim();
			if (code.IndexOf('.') != -1)
			{
				//Grabs the end codepoint
				var endCode = code.Substring(code.IndexOf("..") + 2).Trim();
				code = code.Substring(0, code.IndexOf("..")).Trim();
				// Add all the characters in the range.
				currentCount += SubHex(endCode, code) + 1;
			}
			// we found another valid codepoint
			else
				currentCount++;
		}

		/// <summary>		returns true if the line is not just a comment
		/// i.e if it is of either of the following forms
		/// ------- ; ------ # ------
		/// ------- ; -------
		/// NOT ----- # ----- ; ------
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		private static bool HasBidiData(string line)
		{
			return (line.IndexOf('#') > line.IndexOf(';') && line.IndexOf('#') != -1 && line.IndexOf(';') != -1) ||
				(line.IndexOf('#') == -1 && line.IndexOf(';') != -1);
		}
		/// <summary>
		/// Reads the Property value from the given line of a UCD text file.
		/// (For example, if the text file is DerivedBidiClass.txt,
		///		this reads the bidi class value from the given line of a DerivedBidiClass.txt file.)
		/// </summary>
		/// <param name="line">A line from a UCD file in the following format:
		///		<code>code;Property[; other] [ # other]</code></param>
		/// <returns>The bidi class value, or bidi class value range </returns>
		public static string GetProperty(string line)
		{
			// (Note, by doing it in two steps, we are assured that even in strange cases like:
			//	code ; property # comment ; comment
			// it will stll work

			// Grab from the ; to the #, or the end of the line
			//If a comment is not on the line
			var propertyWithValue = line.IndexOf('#') == -1 ?
				line.Substring(line.IndexOf(';') + 1).Trim() :
				line.Substring(line.IndexOf(';') + 1, line.IndexOf('#') - line.IndexOf(';') - 1).Trim();

			// Return only from the first ';' to the second ';'
			return propertyWithValue.IndexOf(';') != -1 ?
				propertyWithValue.Substring(0, propertyWithValue.IndexOf(';')).Trim() :
				propertyWithValue;
		}

		/// <summary>
		/// Adds/"deletes" a given puaCharacter to the given TextWriter
		/// Will write a DerivedBidiClass.txt style line.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="ucdCharacter">The character to add or delete</param>
		/// <param name="add"></param>
		public void AddUCDLine(TextWriter writer, IUcdCharacter ucdCharacter, bool add)
		{
			if (add)
				writer.WriteLine(ucdCharacter + " " + m_comment);
			// Uncomment this line to replace lines with a comment indicating that the line was removed.
			//			else
			//				writer.WriteLine("# DELETED LINE: {0}",puaCharacter.ToBidiString());
		}
		/// <summary>
		/// Checks to make sure the given codeIndex is within the current bidi section.
		/// Performs bounds checking as well.
		/// </summary>
		/// <param name="codeIndex">The index of a PUA character in puaCharacters</param>
		/// <param name="ucdCharacters">The list of PUA characters</param>
		/// <param name="currentBidiClass">The current bidi class value.  If puaCharacters[codeIndex] doesn't match this we aren't in range.</param>
		/// <returns></returns>
		public bool StillInRange(int codeIndex, List<IUcdCharacter> ucdCharacters, string currentBidiClass)
		{
			return codeIndex < ucdCharacters.Count && ucdCharacters[codeIndex].SameRegion(currentBidiClass);
		}
		#endregion

		/// <summary>
		/// Write a codepoint block, inserting the necessary codepoints properly.
		/// </summary>
		/// <param name="writer">DerivedBidiClass.txt file to write lines to.</param>
		/// <param name="originalBeginningCode">First codepoint in the block</param>
		/// <param name="originalEndCode">Last codepoint in the free block</param>
		/// <param name="codepointsWithinRange">An array of codepoints within the block, including the ends.
		///		DO NOT pass in points external to the free block.</param>
		///	<param name="generalCategory">The field that appears directly after the name in UnicodeData.txt.
		///		This will appear as the first element in a comment.</param>
		///	<param name="firstName">The name of the first letter in the range, for the comment</param>
		///	<param name="lastName">The name of the last letter in the range, for the comment</param>
		///	<param name="add"><code>true</code> for add <code>false</code> for delete.</param>
		private void WriteBidiCodepointBlock(TextWriter writer, string originalBeginningCode, string originalEndCode,
			List<IUcdCharacter> codepointsWithinRange,
			string generalCategory, string firstName, string lastName, bool add)
		{
			//Allows us to store the original end and beginning code points while looping
			//through them
			var beginningCode = originalBeginningCode;
			var endCode = originalEndCode;

			//Write each entry
			foreach (var ucdCharacter in codepointsWithinRange)
			{
				//If the current xmlCodepoint is the same as the beginning codepoint
				if (ucdCharacter.CompareTo(beginningCode) == 0)
				{
					//Shift the beginning down one
					beginningCode = AddHex(beginningCode, 1);
					//Add or delete the character
					AddUCDLine(writer, ucdCharacter, add);
				}
				//If the current xmlCodepoint is between the beginning and end
				else if (ucdCharacter.CompareTo(endCode) != 0)
				{
					if (originalBeginningCode == beginningCode)
					{
						//We're writing a range block below the current xmlCodepoint
						WriteBidiRange(writer, beginningCode,
							AddHex(ucdCharacter.CodePoint, -1),
							generalCategory, firstName, "???", ucdCharacter.Property);
					}
					else
					{
						//We're writing a range block below the current xmlCodepoint
						WriteBidiRange(writer, beginningCode,
							AddHex(ucdCharacter.CodePoint, -1),
							generalCategory, "???", "???", ucdCharacter.Property);
					}
					AddUCDLine(writer, ucdCharacter, add);
					//Set the beginning code to be right after the ucdCharacterthat we just added
					beginningCode = AddHex(ucdCharacter.CodePoint, 1);
				}
				//If the current xmlCodepoint is the same as the end codepoint
				else
				{
					//Moves the end down a codepoint address
					endCode = AddHex(endCode, -1);
					//Write our range of data
					WriteBidiRange(writer, beginningCode, endCode, generalCategory, "???", "???",
						ucdCharacter.Property);
					//Writes the current line
					AddUCDLine(writer, ucdCharacter, add);
					return;
				}
			}
			//Write our range of data
			WriteBidiRange(writer, beginningCode, endCode, generalCategory, "???", lastName,
				codepointsWithinRange[0].Property);
		}

		/// <summary>
		/// Writes a block representing a range given first and last.
		/// If first is not before last, it will do the appropriate printing.
		/// </summary>
		/// <example>
		/// <code>
		/// ( { } are &lt; &gt; because this will interpret them as flags )
		///	E000;{Private Use, First};Co;0;L;;;;;N;;;;;
		/// F12F;{Private Use, Last};Co;0;L;;;;;N;;;;;   # [SIL-Corp] Added Feb 2004
		/// </code>
		/// -or-
		/// <code>
		///	E000;{Private Use};Co;0;L;;;;;N;;;;;
		/// </code>
		/// -or-
		/// <i>Nothing, since last was before beginning.</i>
		/// </example>
		///<param name="writer">The StreamWriter to print to</param>
		///<param name="beginning">A string hexadecimal representing the beginning.</param>
		///<param name="end">A string hexadecimal representing the end.</param>
		///<param name="generalCategory"></param>
		///<param name="firstName"></param>
		///<param name="lastName"></param>
		///<param name="bidiValue"></param>
		private static void WriteBidiRange(TextWriter writer, string beginning, string end, string generalCategory, string firstName, string lastName, string bidiValue)
		{
			if (firstName == "")
				firstName = "???";
			if (lastName == "")
				lastName = "???";

			int codeRangeCount = SubHex(end, beginning) + 1;

			switch (MiscUtils.CompareHex(end, beginning))
			{
				case -1:
					break;
				case 0:
					writer.WriteLine("{0,-14}; {1} # {2,-8} {3} OR {4}",
						beginning, bidiValue, generalCategory, firstName, lastName);
					break;
				case 1:
					string range = beginning + ".." + end;
					string codeCount = "[" + codeRangeCount + "]";
					writer.WriteLine("{0,-14}; {1} # {2} {3,5} {4}..{5}",
						range, bidiValue, generalCategory, codeCount, firstName, lastName);
					break;
			}
			return;
		}
		/// <summary>
		/// Writes a block representing a range given first and last.
		/// If first is not before last, it will do the appropriate printing.
		/// </summary>
		/// <example>
		/// <code>
		///	E000;{Private Use, First};Co;0;L;;;;;N;;;;;
		/// F12F;{Private Use, Last};Co;0;L;;;;;N;;;;;   # [SIL-Corp] Added Feb 2004
		/// </code>
		/// -or-
		/// <code>
		///	E000;{Private Use};Co;0;L;;;;;N;;;;;
		/// </code>
		/// -or-
		/// <i>Nothing, since last was before beginning.</i>
		/// </example>
		/// <param name="writer">The StreamWriter to print to</param>
		/// <param name="beginning">A string hexadecimal representing the beginning.</param>
		/// <param name="end">A string hexadecimal representing the end.</param>
		/// <param name="name">The name of the block, e.g. "Private Use"</param>
		/// <param name="data">The data to write after the block, e.g. ;Co;0;L;;;;;N;;;;;</param>
		private static void WriteRange(StreamWriter writer, string beginning, string end, string name, string data)
		{
			switch (MiscUtils.CompareHex(end, beginning))
			{
				case -1:
					break;
				case 0:
					writer.WriteLine("{0};<{1}>{2}", beginning, name, data);
					break;
				case 1:
					writer.WriteLine("{0};<{1}, First>{2}", beginning, name, data);
					writer.WriteLine("{0};<{1}, Last>{2}", end, name, data);
					break;
			}
		}
		#endregion


		const string ksOriginal = "_ORIGINAL";
		const string ksTempFileSuffix = "_TEMP";
		const string ksBackupFileSuffix = "_BAK";

		/*
				enum State { ReadingXMLFile, ICUFiles };
		*/


		///<summary>
		///</summary>
		///<param name="inName"></param>
		///<param name="outName"></param>
		///<param name="overwrite"></param>
		public static void SafeFileCopyWithLogging(string inName, string outName, bool overwrite)
		{
			try
			{
				FileCopyWithLogging(inName, outName, overwrite);
			}
			catch
			{
				LogFile.AddVerboseLine("ERROR  : unable to copy <" + inName + "> to <" + outName +
					"> <" + overwrite + ">");
			}
		}

		///<summary>
		///</summary>
		///<param name="inName"></param>
		///<param name="outName"></param>
		///<param name="overwrite"></param>
		public static void FileCopyWithLogging(string inName, string outName, bool overwrite)
		{
			var fi = new FileInfo(inName);
			if (fi.Length > 0)
			{
				if (LogFile.IsLogging())
				{
					LogFile.AddVerboseLine("Copying: <" + inName + "> to <" + outName +
						"> <" + overwrite + ">");
				}
				File.Copy(inName, outName, overwrite);
			}
			else
			{
				LogFile.AddVerboseLine("Not Copying (Zero size): <" + inName + "> to <" + outName +
					"> <" + overwrite + ">");
			}
		}


		/// <summary>
		/// Create the "original" (backup) copy of the file to be modified,
		/// if it doesn't already exist.
		/// </summary>
		/// <param name="inputFilespec">This is the file to make a copy of.</param>
		public static string BackupOrig(string inputFilespec)
		{
			if (!File.Exists(inputFilespec))
			{
				LogFile.AddVerboseLine("No Orig to back up: <" + inputFilespec);
				return null;
			}

			string outputFilespec = CreateNewFileName(inputFilespec, ksOriginal);
			if (!File.Exists(outputFilespec))
			{
				try
				{
					FileCopyWithLogging(inputFilespec, outputFilespec, true);
				}
				catch (Exception)
				{
					LogFile.AddErrorLine("Error creating " + ksOriginal + " copy: " + inputFilespec);
					throw;
				}
			}
			return outputFilespec;
		}


		///<summary>
		///</summary>
		///<param name="file"></param>
		///<returns></returns>
		public static bool SafeDeleteFile(string file)
		{
			try
			{
				return DeleteFile(file);
			}
			catch (Exception)
			{
				LogFile.AddVerboseLine("ERROR: Unable to remove file: <" + file + ">");
				return false;
			}
		}

		/// <summary>
		/// </summary>
		public static bool DeleteFile(string file)
		{
			bool rval = false;
			if (File.Exists(file))
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
				rval = true;
				if (LogFile.IsLogging())
					LogFile.AddVerboseLine("Removed file:<" + file + ">");
			}
			else
			{
				if (LogFile.IsLogging())
					LogFile.AddVerboseLine("Tried to delete file that didn't exist:<" + file + ">");
			}
			return rval;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a regular file name, this method returns the file name that is used for
		/// storing the 'origional' version of the file.
		/// </summary>
		/// <param name="inputFilespec"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string MakeOrigFileName(string inputFilespec)
		{
			return CreateNewFileName(inputFilespec, ksOriginal);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="savedName"></param>
		/// <param name="backupPortion"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string UndoCreateNewFileName(string savedName, string backupPortion)
		{
			string oldName = savedName;
			int index = savedName.LastIndexOf(backupPortion);
			if (index != -1)
			{
				oldName = savedName.Substring(0, index);
				oldName += savedName.Substring(index + backupPortion.Length);
			}
			return oldName;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="directoryName"></param>
		/// <param name="extension"></param>
		/// <param name="removeOrig"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int RestoreOrigFiles(string directoryName, string extension, bool removeOrig)
		{
			int numCopied = 0;
			var di = new DirectoryInfo(directoryName);
			string origPattern = CreateNewFileName("*" + extension, ksOriginal);
			FileInfo[] fi = di.GetFiles(origPattern);

			LogFile.AddLine("RestoreOrigFiles: " + directoryName + origPattern);

			foreach (FileInfo f in fi)
			{
				string savedName = f.FullName;
				string defName = UndoCreateNewFileName(savedName, ksOriginal);
				try
				{
					FileCopyWithLogging(savedName, defName, true);
					if (removeOrig)
					{
						// delete the orig file here...
						DeleteFile(savedName);
					}
					numCopied++;
				}
				catch
				{
					LogFile.AddErrorLine("Error restoring " + ksOriginal + " file: " + f.FullName);
					throw;
				}
			}
			if (numCopied == 0)
				LogFile.AddLine("RestoreOrigFiles: No files copied.");

			return numCopied;
		}


		/// <summary>
		/// This method will create the temporary work file copy of the original input file.
		/// </summary>
		/// <param name="inputFilespec">This is the file to make a copy of.</param>
		/// <param name="suffix"></param>
		private static string CreateXxFile(string inputFilespec, string suffix)
		{
			string outputFilespec = CreateNewFileName(inputFilespec, suffix);
			try
			{
				if (!File.Exists(inputFilespec))
				{
					// Have to save the handle in an object so that we can close it!
					using (FileStream fs = File.Create(outputFilespec))
						fs.Close();
				}
				else
				{
					FileCopyWithLogging(inputFilespec, outputFilespec, true);
				}
			}
			catch
			{
				LogFile.AddErrorLine("Error creating file with suffix: " + suffix + " from: " + inputFilespec);
				throw;
			}
			return outputFilespec;
		}


		/// <summary>
		/// This method will create the temporary work file copy of the original input file.
		/// </summary>
		/// <param name="inputFilespec">This is the file to make a copy of.</param>
		/// <returns>new file name</returns>
		public static string CreateTempFile(string inputFilespec)
		{
			return CreateXxFile(inputFilespec, ksTempFileSuffix);
		}


		/// <summary>
		/// This method will create the backup of the original input file.
		/// </summary>
		/// <param name="inputFilespec">This is the file to make a backup of.</param>
		/// <returns>new file name</returns>
		public static string CreateBackupFile(string inputFilespec)
		{
			return CreateXxFile(inputFilespec, ksBackupFileSuffix);
		}


		/// <summary>This method appends 'nameSplice' to a file ínputFilespec'.</summary>
		/// <param name="inputFilespec">Input file name to modify.</param>
		/// <param name="nameSplice">The 'text' to append to the file name before the
		/// extension.</param>
		/// <returns>The new file name.</returns>
		public static string CreateNewFileName(string inputFilespec, string nameSplice)
		{
			int index = inputFilespec.LastIndexOf('.');
			string newName;

			if (index == -1)
			{
				newName = inputFilespec + nameSplice;
			}
			else
			{
				newName = inputFilespec.Substring(0, index);
				newName += nameSplice;
				newName += inputFilespec.Substring(index);
			}
			return newName;
		}


		/// <summary>Replace the file extension with 'newExtension'.</summary>
		/// <param name="inputFilespec">Input file name to modify.</param>
		/// <param name="newExtension">The new file Extension.</param>
		/// <returns>The new file name.</returns>
		public static string ChangeFileExtension(string inputFilespec, string newExtension)
		{
			int index = inputFilespec.LastIndexOf('.');
			string newName;

			if (index == -1)
			{
				newName = inputFilespec + "." + newExtension;
			}
			else
			{
				newName = inputFilespec.Substring(0, index);
				newName += "." + newExtension;
			}
			return newName;
		}


		///<summary>
		///</summary>
		public enum CallingID
		{
			///<summary></summary>
			CID_RESTORE,
			///<summary></summary>
			CID_REMOVE,
			///<summary></summary>
			CID_INSTALL,
			///<summary></summary>
			CID_UPDATE,
			///<summary></summary>
			CID_NEW,
			///<summary></summary>
			CID_UNKNOWN
		};

		/// <summary>
		/// Check for locked ICU files and return
		/// </summary>
		/// <param name="inputLocale">the locale being modified. May just be the ICULocale name, or
		/// may be a fully specified path name to the language xml file, or null.</param>
		/// <param name="runSilent">Boolean set to true if we don't want to ask the user for info.</param>
		/// <param name="caller"></param>
		/// <returns>true if ok to continue, or false if files are locked</returns>
		internal static bool CheckForIcuLocked(string inputLocale, bool runSilent, CallingID caller)
		{
			bool fOk;
			string locale = null;
			if (inputLocale != null)
			{
				int icuName = inputLocale.LastIndexOf(Path.DirectorySeparatorChar);
				string icuPortion = inputLocale.Substring(icuName + 1);
				int iLocale = icuPortion.LastIndexOf(".");
				if (iLocale < 0)
					iLocale = icuPortion.Length;
				locale = icuPortion.Substring(0, iLocale);
			}
			do
			{
				fOk = true;
				var lockedFile = Icu.CheckIcuLocked(locale);
				if (lockedFile != null)
				{
					LogFile.AddLine(String.Format(" File Access Error: {0}. Asking user to Retry or Cancel. Caller={1}", lockedFile, caller));
					if (runSilent)
					{
						LogFile.AddLine(" Silently cancelled operation.");
						Console.WriteLine(@"Silently cancelled operation.");
						return false;
					}
					string message;	// for now
					var nl = Environment.NewLine;
					switch (caller)
					{
						case CallingID.CID_RESTORE:
							message = String.Format(Properties.Resources.ksCloseFieldWorksForRestore,
								lockedFile);
							break;
						case CallingID.CID_NEW:
							message = String.Format(Properties.Resources.ksCloseFieldWorksToInstall,
								lockedFile);
							break;
						default:
							message = String.Format(Properties.Resources.ksCannotCompleteChanges,
								lockedFile);
							break;
					}
					message = message + nl + nl + "Close Clipboard Converter" + nl + nl + "Close This FW App";

					var caption = Properties.Resources.ksMsgHeader;
					const MessageBoxButtons buttons = MessageBoxButtons.RetryCancel;
					const MessageBoxIcon icon = MessageBoxIcon.Exclamation;
					const MessageBoxDefaultButton defButton = MessageBoxDefaultButton.Button1;
					var result = MessageBox.Show(message, caption, buttons, icon, defButton);
					if (result == DialogResult.Cancel)
					{
						LogFile.AddLine(" User cancelled operation.");
						Console.WriteLine(@"User cancelled operation.");
						return false;
					}
					fOk = false;
				}
			} while (fOk == false);
			return true;
		}

		///<summary>
		///</summary>
		public enum ErrorCodes
		{
			///<summary>Not specified - uninitialized</summary>
			None = 1,

			///<summary>Success Value</summary>
			Success = 0,

#if !__MonoCS__
			///<summary>Command Line Argument Errors</summary>
			CommandLine = -1,

			///<summary>User cancellations (with indication of problem)</summary>
			CancelAccessFailure = -4,

			///<summary>Invalid Language Def file data</summary>
			LdBaseLocale = -11,
			///<summary></summary>
			LdNewLocale = -12,
			///<summary></summary>
			LdLocaleResources = -13,
			///<summary></summary>
			LdFileName = -14,
			///<summary></summary>
			LdBadData = -15,
			///<summary></summary>
			LdParsingError = -16,
			///<summary></summary>
			LdUsingISO3CountryName = -17,
			///<summary></summary>
			LdUsingISO3LanguageName = -18,
			///<summary></summary>
			LdUsingISO3ScriptName = -19,

			// Genrb errors
			///<summary></summary>
			ResIndexFile = -21,
			///<summary></summary>
			RootFile = -22,
			///<summary></summary>
			NewLocaleFile = -23,
			///<summary></summary>
			GeneralFile = -24,
			///<summary></summary>
			ExistingLocaleFile = 25,

			// General file errors
			///<summary></summary>
			FileRead = -31,
			///<summary></summary>
			FileWrite = -32,
			///<summary></summary>
			FileRename = -33,
			///<summary></summary>
			FileNotFound = -34,

			// Specific file errors
			///<summary></summary>
			RootTxtFileNotFound = -41,
			///<summary></summary>
			ResIndexTxtFileNotFound = -42,
			///<summary></summary>
			RootResFileNotFound = -43,
			///<summary></summary>
			ResIndexResFileNotFound = -44,
			///<summary></summary>
			RootTxtInvalidCustomResourceFormat = -45,
			///<summary></summary>
			RootTxtCustomResourceNotFound = -46,

			// Registry related errors
			///<summary></summary>
			RegistryIcuDir = -50,
			///<summary></summary>
			RegistryIcuLanguageDir = -51,
			///<summary></summary>
			RegistryIcuTemplatesDir = -52,

			// subprocess Error Codes
			///<summary></summary>
			Gennames = -60,
			///<summary></summary>
			Genprops = -61,
			///<summary></summary>
			Gennorm = -62,
			///<summary></summary>
			Genbidi = -63,
			///<summary></summary>
			Gencase = -64,

			// PUA Error Codes
			///<summary></summary>
			PUAOutOfRange = -70,
			// note: we assume good PUA characters, so we don't check every detail,
			// this just is if we happen to discover an error while parsing.
			///<summary></summary>
			PUADefinitionFormat = -71,

			// ICU data file related errors
			///<summary></summary>
			ICUDataParsingError = -80,
			///<summary></summary>
			ICUNodeAccessError = -81,

			// Programming error
			///<summary></summary>
			ProgrammingError = -998,

			// Unknown Error
			///<summary></summary>
			NonspecificError = -999,
#else
			// Valid linux return values are between 0-255
			// So returning -70 would return 186

			///<summary>Command Line Argument Errors</summary>
			CommandLine=2,

			///<summary>User cancellations (with indication of problem)</summary>
			CancelAccessFailure=4,

			///<summary>Invalid Language Def file data</summary>
			LdBaseLocale = 11,
			///<summary></summary>
			LdNewLocale = 12,
			///<summary></summary>
			LdLocaleResources = 13,
			///<summary></summary>
			LdFileName = 14,
			///<summary></summary>
			LdBadData = 15,
			///<summary></summary>
			LdParsingError = 16,
			///<summary></summary>
			LdUsingISO3CountryName = 17,
			///<summary></summary>
			LdUsingISO3LanguageName = 18,
			///<summary></summary>
			LdUsingISO3ScriptName = 19,

			// Genrb errors
			///<summary></summary>
			ResIndexFile = 21,
			///<summary></summary>
			RootFile = 22,
			///<summary></summary>
			NewLocaleFile = 23,
			///<summary></summary>
			GeneralFile = 24,
			///<summary></summary>
			ExistingLocaleFile = 25,

			// General file errors
			///<summary></summary>
			FileRead = 31,
			///<summary></summary>
			FileWrite = 32,
			///<summary></summary>
			FileRename = 33,
			///<summary></summary>
			FileNotFound = 34,

			// Specific file errors
			///<summary></summary>
			RootTxtFileNotFound = 41,
			///<summary></summary>
			ResIndexTxtFileNotFound = 42,
			///<summary></summary>
			RootResFileNotFound = 43,
			///<summary></summary>
			ResIndexResFileNotFound = 44,
			///<summary></summary>
			RootTxtInvalidCustomResourceFormat = 45,
			///<summary></summary>
			RootTxtCustomResourceNotFound = 46,

			// Registry related errors
			///<summary></summary>
			RegistryIcuDir = 50,
			///<summary></summary>
			RegistryIcuLanguageDir = 51,
			///<summary></summary>
			RegistryIcuTemplatesDir = 52,

			// subprocess Error Codes
			///<summary></summary>
			Gennames = 60,
			///<summary></summary>
			Genprops = 61,
			///<summary></summary>
			Gennorm = 62,
			///<summary></summary>
			Genbidi = 63,
			///<summary></summary>
			Gencase = 64,

			// PUA Error Codes
			///<summary></summary>
			PUAOutOfRange = 70,
			// note: we assume good PUA characters, so we don't check every detail,
			// this just is if we happen to discover an error while parsing.
			///<summary></summary>
			PUADefinitionFormat = 71,

			// ICU data file related errors
			///<summary></summary>
			ICUDataParsingError = 80,
			///<summary></summary>
			ICUNodeAccessError = 81,

			// Programming error
			///<summary></summary>
			ProgrammingError = 254,

			// Unknown Error
			///<summary></summary>
			NonspecificError = 255,
#endif
		};
	}
}
