// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InstallLanguage.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using InstallLanguage.Errors;
using Microsoft.Win32;			// registry types
using System.Security.Permissions;
using System.Security.Policy;
using System.Security;
using System.Threading;	// mutex
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using System.Windows.Forms;


namespace InstallLanguage
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for InstallLanguage.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Generic
	{
		const string original = "_ORIGINAL";
		const string tempFileSuffix = "_TEMP";
		const string backupFileSuffix = "_BAK";

		enum State { ReadingXMLFile, ICUFiles };

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the ICU Directory from the Registry.
		/// </summary>
		/// <returns>Icu directory or throws ErrorCodes.RegistryIcuDir.</returns>
		///-------------------------------------------------------------------------------------
		public static string GetIcuDir()
		{
			string rootPath = "";
			try
			{
				// Try to find the key.
				RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\SIL\\");

				if (regKey == null)
				{
					throw new LDExceptions(ErrorCodes.RegistryIcuDir);
				}

				rootPath = (string)regKey.GetValue("Icu40Dir");
				regKey.Close();
				if (rootPath == null)
				{
					throw new LDExceptions(ErrorCodes.RegistryIcuDir);
				}
				if (rootPath[rootPath.Length - 1] != '\\')
					rootPath += '\\';
			}
			catch(Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
			}
			return rootPath;
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the ICU Data Directory from the Registry.  Prior to version ICU 3.4 this
		/// was the same as the ICU Directory.
		/// </summary>
		/// <returns>Icu directory or throws ErrorCodes.RegistryIcuDir.</returns>
		///-------------------------------------------------------------------------------------
		public static string GetIcuDataDir()
		{
			string rootPath = "";
			try
			{
				// Try to find the key.
				RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\SIL\\");

				if (regKey == null)
				{
					throw new LDExceptions(ErrorCodes.RegistryIcuDir);
				}

				rootPath = (string)regKey.GetValue("Icu40DataDir");
				regKey.Close();
				if (rootPath == null)
				{
					throw new LDExceptions(ErrorCodes.RegistryIcuDir);
				}
				if (rootPath[rootPath.Length - 1] != '\\')
					rootPath += '\\';
			}
			catch(Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
			}
			return rootPath;
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the Icu Language Directory from the Registry.
		/// </summary>
		/// <returns>Icu Language directory or throws ErrorCodes.RegistryIcuLanguageDir.</returns>
		///-------------------------------------------------------------------------------------
		public static string GetIcuLanguageDir()
		{
			string rootPath = DirectoryFinder.FWDataDirectory; ;
			try
			{
				if (rootPath[rootPath.Length - 1] != '\\')
					rootPath += '\\';
				rootPath += "Languages\\";
			}
			catch (Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
			}
			return rootPath;
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the ICU Templates Directory from the Registry.
		/// </summary>
		/// <returns>ICR Templates directory or throws ErrorCodes.RegistryIcuTemplatesDir.</returns>
		///-------------------------------------------------------------------------------------
		public static string GetIcuTemplateDir()
		{
			string rootPath = DirectoryFinder.FWCodeDirectory; ;
			try
			{
				if (rootPath[rootPath.Length - 1] != '\\')
					rootPath += '\\';
				rootPath += "Templates\\";
			}
			catch (Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
			}
			return rootPath;
		}


		/// <summary>
		/// Get the Icu data string used by the current version of Icu.  This used to be a
		/// string like "icudt26l_" which prefixed all the binary data files.  Those are now
		/// stored in a subdirectory, so we return something like "icudt40l\" instead.
		/// </summary>
		/// <returns>Icu string "icudt26l_"</returns>
		public static string GetIcuData()
		{
			string sIcuDataDir = GetIcuDataDir();
			int idxBackSlash = sIcuDataDir.LastIndexOf('\\', sIcuDataDir.Length - 2);
			if (idxBackSlash == -1)
				throw new LDExceptions(ErrorCodes.RootRes_FileNotFound);
			string icuData = sIcuDataDir.Substring(idxBackSlash + 1);
			return icuData;
		}


		public static void SafeFileCopyWithLogging(string inName, string outName, bool overwrite)
		{
			try
			{
				FileCopyWithLogging(inName, outName, overwrite);
			}
			catch
			{
				LogFile.AddVerboseLine("ERROR  : unable to copy <" + inName + "> to <" + outName +
					"> <" + overwrite.ToString() + ">");
			}
		}

		public static void FileCopyWithLogging(string inName, string outName, bool overwrite)
		{
			System.IO.FileInfo fi = new System.IO.FileInfo(inName);
			if (fi.Length > 0)
			{
				if (LogFile.IsLogging())
				{
					LogFile.AddVerboseLine("Copying: <" + inName + "> to <" + outName +
						"> <" + overwrite.ToString() + ">");
				}
				File.Copy(inName, outName, overwrite);
			}
			else
			{
				LogFile.AddVerboseLine("Not Copying (Zero size): <" + inName + "> to <" + outName +
					"> <" + overwrite.ToString() + ">");
			}
		}


		/// <summary>
		/// Create the "original" (backup) copy of the file to be modified,
		/// if it doesn't already exist.
		/// </summary>
		/// <param name="inputFilespec">This is the file to make a copy of.</param>
		public static void BackupOrig(string inputFilespec)
		{
			if (!File.Exists(inputFilespec))
			{
				LogFile.AddVerboseLine("No Orig to back up: <" + inputFilespec);
				return;
			}

			string outputFilespec = CreateNewFileName(inputFilespec, original);
			if (!File.Exists(outputFilespec))
			{
				try
				{
					FileCopyWithLogging(inputFilespec, outputFilespec, true);
				}
				catch
				{
					LogFile.AddErrorLine("Error creating " + original + " copy: " + inputFilespec);
					throw new LDExceptions(ErrorCodes.FileWrite);
				}
			}
		}


		public static bool SafeDeleteFile(string file)
		{
			try
			{
				return DeleteFile(file);
			}
			catch
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
			return CreateNewFileName(inputFilespec, original);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="savedName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string UndoCreateNewFileName(string savedName, string backupPortion)
		{
			string oldName = savedName;
			int index = savedName.LastIndexOf(backupPortion);
			if (index != -1)
			{
				oldName = savedName.Substring(0, index);
				oldName += savedName.Substring(index+backupPortion.Length);
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
			DirectoryInfo di = new DirectoryInfo(directoryName);
			string origPattern = CreateNewFileName("*" + extension, original);
			System.IO.FileInfo[] fi = di.GetFiles(origPattern);

			LogFile.AddLine("RestoreOrigFiles: " + directoryName + origPattern);

			foreach(System.IO.FileInfo f in fi)
			{
				string savedName = f.FullName;
				string defName = UndoCreateNewFileName(savedName, original);
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
					LogFile.AddErrorLine("Error restoring " + original + " file: " + f.FullName);
					throw new LDExceptions(ErrorCodes.FileWrite);
				}
			}
			if (numCopied == 0)
				LogFile.AddLine("RestoreOrigFiles: No files copied.");

			return numCopied;
		}


		/// <summary>
		/// This method will create the temporary work file copy of the original input file.
		/// </summary>
		/// <param name="inputFilespec">This is the file to make a copy of.<</param>
		private static string CreateXXFile(string inputFilespec, string suffix)
		{
			string outputFilespec = CreateNewFileName(inputFilespec, suffix);
			try
			{
				if (!File.Exists(inputFilespec))
				{
					// Have to save the handle in an object so that we can close it!
					FileStream fs = File.Create(outputFilespec);
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
				throw new LDExceptions(ErrorCodes.FileWrite);
			}
			return outputFilespec;
		}


		/// <summary>
		/// This method will create the temporary work file copy of the original input file.
		/// </summary>
		/// <param name="inputFilespec">This is the file to make a copy of.<</param>
		/// <returns>new file name</returns>
		public static string CreateTempFile(string inputFilespec)
		{
			return CreateXXFile(inputFilespec, tempFileSuffix);
		}


		/// <summary>
		/// This method will create the backup of the original input file.
		/// </summary>
		/// <param name="inputFilespec">This is the file to make a backup of.</param>
		/// <returns>new file name</returns>
		public static string CreateBackupFile(string inputFilespec)
		{
			return CreateXXFile(inputFilespec, backupFileSuffix);
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
				newName = inputFilespec.Substring(0,index);
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
				newName = inputFilespec.Substring(0,index);
				newName += "." + newExtension;
			}
			return newName;
		}

		// This class generates SecurityPermission objects using SecurityPermissionFlag enumeration values.


		public  class SecurityGenerator
		{

			private SecurityPermissionFlag[] mySecurity =
		{
			SecurityPermissionFlag.AllFlags,
			SecurityPermissionFlag.Assertion,
			SecurityPermissionFlag.ControlAppDomain,
			SecurityPermissionFlag.ControlDomainPolicy,
			SecurityPermissionFlag.ControlEvidence,
			SecurityPermissionFlag.ControlPolicy,
			SecurityPermissionFlag.ControlPrincipal,
			SecurityPermissionFlag.ControlThread,
			SecurityPermissionFlag.Execution,
			SecurityPermissionFlag.Infrastructure,
			SecurityPermissionFlag.NoFlags,
			SecurityPermissionFlag.RemotingConfiguration,
			SecurityPermissionFlag.SerializationFormatter,
			SecurityPermissionFlag.SkipVerification,
			SecurityPermissionFlag.UnmanagedCode};

			private int reflectionIndex = 0;

			public SecurityGenerator()
			{
				ResetIndex();
			}

			public void ResetIndex()
			{
				reflectionIndex = 0;
			}
			// CreateSecurity creates a SecurityPermission object.
			public bool CreateSecurity(out SecurityPermission SecurityPerm, out SecurityPermissionFlag Security)
			{

				if(reflectionIndex >= mySecurity.Length)
				{
					SecurityPerm = new SecurityPermission(PermissionState.None);
					Security=SecurityPermissionFlag.NoFlags;
					reflectionIndex++;
					return false;
				}
				Security = mySecurity[reflectionIndex++];
				try
				{
					SecurityPerm = new SecurityPermission(Security);
					return true;
				}
				catch(Exception e)
				{
					Console.WriteLine("Cannot create SecurityPermission: " + Security +" "+e);
					SecurityPerm = new SecurityPermission(PermissionState.None);
					Security=SecurityPermissionFlag.NoFlags;
					return true;
				}
			}
		} // End of SecurityGenerator.

		public enum CallingID { CID_RESTORE, CID_REMOVE, CID_INSTALL, CID_UPDATE, CID_NEW, CID_UNKNOWN };

		/// <summary>
		/// Check for locked ICU files and return
		/// </summary>
		/// <param name="localPath">the locale being modified. May just be the ICULocale name, or
		/// may be a fully specified path name to the language xml file, or null.</param>
		/// <param name="runSilent">Boolean set to true if we don't want to ask the user for info.
		/// <returns>true if ok to continue, or false if files are locked</returns>
		private static bool CheckForIcuLocked(string inputLocale, bool runSilent, CallingID caller)
		{
			bool fOk;
			string locale = null;
			if (inputLocale != null)
			{
				int icuName = inputLocale.LastIndexOf("\\");
				string icuPortion = inputLocale.Substring(icuName + 1);
				int iLocale = icuPortion.LastIndexOf(".");
				if (iLocale < 0)
					iLocale = icuPortion.Length;
				locale = icuPortion.Substring(0, iLocale);
			}
			do
			{
				fOk = true;
				string lockedFile = Icu.CheckIcuLocked(locale);
				if (lockedFile != null)
				{
					LogFile.AddLine(" File Access Error: " + lockedFile + ". Asking user to Retry or Cancel. Caller="+caller.ToString());
					if (runSilent)
					{
						LogFile.AddLine(" Silently cancelled operation.");
						System.Console.WriteLine("Silently cancelled operation.");
						return false;
					}
					string message = "";	// for now
					string nl = Environment.NewLine;
					switch (caller)
					{
						case CallingID.CID_RESTORE:
							message = String.Format(InstallLanguageStrings.ksRestore_CloseOtherFWApps,
								lockedFile);
							break;
						case CallingID.CID_NEW:
							message = String.Format(InstallLanguageStrings.ksNew_CloseOtherFWApps,
								locale, lockedFile);
							break;
						case CallingID.CID_INSTALL:
						case CallingID.CID_REMOVE:
						case CallingID.CID_UNKNOWN:
						case CallingID.CID_UPDATE:
						default:
							message = String.Format(InstallLanguageStrings.ksUpdate_CloseOtherFWApps,
								locale, lockedFile);
							break;
					}
					message = message + nl + nl +
						InstallLanguageStrings.ksCloseClipboardConverter + nl + nl +
						InstallLanguageStrings.ksCloseThisFWApp;

					string caption = InstallLanguageStrings.ksInstallLanguageMsgCaption;
					MessageBoxButtons buttons = MessageBoxButtons.RetryCancel;
					MessageBoxIcon icon = MessageBoxIcon.Exclamation;
					MessageBoxDefaultButton defButton = MessageBoxDefaultButton.Button1;
					DialogResult result = MessageBox.Show(message, caption, buttons, icon, defButton);
					if (result == DialogResult.Cancel)
					{
						LogFile.AddLine(" User cancelled operation.");
						System.Console.WriteLine("User cancelled operation.");
						return false;
					}
					else
						fOk = false;
				}
			} while (fOk == false);
			return true;
		}

		/// <summary>
		/// This is the 'Main'- it kicks the ball rolling...
		/// </summary>
		/// <param name="args">command line parameters</param>
		/// <returns>error value (non-zero)</returns>
		///
		[STAThread]
		public static int Main(string[] args)
		{
			LogFile.AddLine("================= Start of process =================");
			LogFile.AddLine("Command line: <" + Environment.CommandLine + ">");
			bool testedLock = false;

			ErrorCodes ret = ErrorCodes.None;
			try
			{
				CommandLineParser argParser = new CommandLineParser();
				CommandLineParser.ParseResult parseResult = argParser.Parse(args);
				switch (parseResult)
				{
					case CommandLineParser.ParseResult.error:
						ret = ErrorCodes.CommandLine;
						int iRet = (int)ErrorCodes.CommandLine;
						LogFile.Release();
						return iRet;
					case CommandLineParser.ParseResult.stop:
						LogFile.Release();
						// no error - success
						return (int)(ErrorCodes.Success);
				}

				// tests the command line parser to make sure we have valid data
				if (argParser.FlagUsed(CommandLineParser.Flag.dontDoAnything))
				{
					foreach (CommandLineParser.Flag flag in argParser.FlagsUsed())
					{
						Console.Write("{0}:{1}, ", flag.ToString(), argParser.FlagData(flag));
					}
					Console.WriteLine();
					return (int)ErrorCodes.Success;
				}

				// don't restrict the GUI
				bool runSilent = false;
				if (argParser.FlagUsed(CommandLineParser.Flag.q))
					runSilent = true;

				bool slow = false;
				// Use the old slower file reading method
				if (argParser.FlagUsed(CommandLineParser.Flag.slow))
				{
					slow = true;
				}

				if (argParser.FlagUsed(CommandLineParser.Flag.testMainParserRoutine))
				{
					LocaleFileClass localeObject = new LocaleFileClass();
					bool successParsing = localeObject.TestBaseLocaleParsers(false);
					if (successParsing)
						ret = ErrorCodes.Success;
					else
						ret = ErrorCodes.LDParsingError;
				}


				if (argParser.FlagUsed(CommandLineParser.Flag.testICUDataParser))
				{
					try
					{
						IcuDataNode.TestIcuDataNode(argParser.FlagData(CommandLineParser.Flag.testICUDataParser));
						ret = ErrorCodes.Success;
					}
					catch (LDExceptions e)
					{
						ret = e.ec;
						LogFile.AddErrorLine("LDException: " + e.ec.ToString() + "-" + Error.Text(e.ec));
						if (e.HasConstructorText)
							LogFile.AddErrorLine("LDException Msg: " + e.ConstructorText);
					}
					catch (System.Exception e)
					{
						ret = ErrorCodes.NonspecificError;
						LogFile.AddErrorLine(e.Message);
						LogFile.AddErrorLine(e.StackTrace);
					}
				}

				if (argParser.FlagUsed(CommandLineParser.Flag.s))
				{
					LocaleFileClass localeObject = new LocaleFileClass();
					localeObject.RunSilent = runSilent;
					ret = localeObject.ShowCustomLocales();
				}

				if (argParser.FlagUsed(CommandLineParser.Flag.customLanguages))
				{
					LocaleFileClass localeObject = new LocaleFileClass();
					localeObject.RunSilent = runSilent;
					ret = localeObject.ShowCustomLanguages();
				}

				if (argParser.FlagUsed(CommandLineParser.Flag.o))	// restore origs'
				{
					CallingID cid = CallingID.CID_RESTORE;
					if (argParser.FlagUsed(CommandLineParser.Flag.newlang))
						cid = CallingID.CID_NEW;	// internal new lang case

					if (!CheckForIcuLocked(null, runSilent, cid))
					{
						return (int)ErrorCodes.CancelAccessFailure;
					}
					testedLock = true;
					// only perform this command by itself even if given with other options
					LocaleFileClass restoreFiles = new LocaleFileClass();
					restoreFiles.RunSlow = slow;
					restoreFiles.RunSilent = runSilent;
					// Restore all the original files, installing the PUA characters if requested.
					ret = restoreFiles.RestoreOrigFiles(true);

					// LT-5374 : wasn't moving the res files from the Icu34 dir to the Icudt34l dir
					if (ret == ErrorCodes.Success)
					{
						// remove the backup files from the undo/recover file name list
						restoreFiles.RemoveBackupFiles();
					}
					restoreFiles.RemoveTempFiles();
					restoreFiles.MoveNewResFilesToSubDir();
				}

				if (argParser.FlagUsed(CommandLineParser.Flag.r))	// remove locale
				{
					if (!testedLock && !CheckForIcuLocked(argParser.FlagData(CommandLineParser.Flag.r), runSilent, CallingID.CID_REMOVE))
					{
						return (int)ErrorCodes.CancelAccessFailure;
					}
					testedLock = true;
					string deleteLocale = argParser.FlagData(CommandLineParser.Flag.r);
					argParser.RemoveFlag(CommandLineParser.Flag.r);
					bool rUsed = argParser.FlagUsed(CommandLineParser.Flag.r);

					LocaleFileClass removeLocaleObject = new LocaleFileClass();
					try
					{
						removeLocaleObject.RunSilent = runSilent;
						ret = removeLocaleObject.RemoveLocale(deleteLocale);
						removeLocaleObject.MoveNewResFilesToSubDir();
					}
					catch (System.Exception e)
					{
						ret = ErrorCodes.NonspecificError;

						LogFile.AddErrorLine(e.Message);
						LogFile.AddErrorLine(e.StackTrace);
						LogFile.AddErrorLine("Exception: " + ret.ToString() + "-" + Error.Text(ret));
						removeLocaleObject.RestoreFiles();	// copy backup files to original files.
					}
				}
				// install locale
				if ((ret == ErrorCodes.Success || ret == ErrorCodes.None) &&
					argParser.FlagUsed(CommandLineParser.Flag.i))
				{
					CallingID cid = CallingID.CID_INSTALL;
					if (argParser.FlagUsed(CommandLineParser.Flag.newlang))
						cid = CallingID.CID_NEW;	// internal new lang case

					if (!testedLock && !CheckForIcuLocked(
						argParser.FlagData(CommandLineParser.Flag.i), runSilent, cid))
					{
						return (int)ErrorCodes.CancelAccessFailure;
					}
					testedLock = true;
					LocaleFileClass localeFileObject = new LocaleFileClass();
					localeFileObject.RunSilent = runSilent;
					localeFileObject.RunSlow = slow;
					try
					{
						string addLocale = argParser.FlagData(CommandLineParser.Flag.i);
						localeFileObject.InstallLDFile(addLocale);
						ret = ErrorCodes.Success;
					}
					catch (LDExceptions e)
					{
						localeFileObject.RestoreFiles();	// copy backup files to original files.
						ret = e.ec;
						LogFile.AddErrorLine("LDException: " + e.ec.ToString() + "-" + Error.Text(e.ec));
						if (e.HasConstructorText)
							LogFile.AddErrorLine("LDException Msg: " + e.ConstructorText);
					}
					catch (System.Exception e)
					{
						ret = ErrorCodes.NonspecificError;

						LogFile.AddErrorLine(e.Message);
						LogFile.AddErrorLine(e.StackTrace);
						LogFile.AddErrorLine("Exception: " + ret.ToString() + "-" + Error.Text(ret));
						localeFileObject.RestoreFiles();	// copy backup files to original files.
					}

					if (ret == ErrorCodes.Success)
					{
						// remove the backup files from the undo/recover file name list
						localeFileObject.RemoveBackupFiles();
					}

					localeFileObject.RemoveTempFiles();
					localeFileObject.MoveNewResFilesToSubDir();
				}

				//Add PUA Character
				if ((ret == ErrorCodes.Success || ret == ErrorCodes.None) &&
					argParser.FlagUsed(CommandLineParser.Flag.c))
				{
					if (!testedLock && !CheckForIcuLocked(argParser.FlagData(CommandLineParser.Flag.c), runSilent, CallingID.CID_UNKNOWN))
					{
						return (int)ErrorCodes.CancelAccessFailure;
					}
					LocaleFileClass localeFileObject = new LocaleFileClass();
					localeFileObject.RunSilent = runSilent;
					try
					{
						string addLocale = argParser.FlagData(CommandLineParser.Flag.c);
						localeFileObject.InstallPUACharacters(addLocale);
						ret = ErrorCodes.Success;
					}
					catch (LDExceptions e)
					{
						// copy backup files to original files.
						localeFileObject.RestoreFiles();
						ret = e.ec;
						LogFile.AddErrorLine("LDException: " + e.ec.ToString() + "-" + Error.Text(e.ec));
						if (e.HasConstructorText)
							LogFile.AddErrorLine("LDException Msg: " + e.ConstructorText);
					}
					catch (System.Exception e)
					{
						ret = ErrorCodes.NonspecificError;
						LogFile.AddErrorLine("Exception: " + e.Message);
						LogFile.AddErrorLine(e.StackTrace);
						LogFile.AddErrorLine("Error Code: " + ret.ToString() + "-" + Error.Text(ret));
						localeFileObject.RestoreFiles();	// copy backup files to original files.
					}
					if (ret == ErrorCodes.Success)
					{
						// remove the backup files from the undo/recover file name list
						localeFileObject.RemoveBackupFiles();
					}

					localeFileObject.RemoveTempFiles();
				}

				if (ret == ErrorCodes.Success)
				{
					if (argParser.FlagUsed(CommandLineParser.Flag.q))
						LogFile.AddLine("--- Success ---");
					else
						LogFile.AddErrorLine("--- Success ---");
				}
			}
			catch (System.Exception e)
			{
				ret = ErrorCodes.NonspecificError;
				LogFile.AddErrorLine("Exception: " + e.Message + " " + e.StackTrace);
				LogFile.AddErrorLine("Exception: " + ret.ToString() + "-" + Error.Text(ret));
			}
			finally
			{
				LogFile.Release();
			}

			int nret = (int)ret;
			return nret;
		}
	}
}
