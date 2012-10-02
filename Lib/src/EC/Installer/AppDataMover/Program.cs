using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Win32;                  // for RegistryKey
using System.Security.AccessControl;    // for DirectorySecurity
using System.Security.Principal;        // for SecurityIdentifier
using ECInterfaces;
using SilEncConverters31;

namespace AppDataMover
{
	class Program
	{
		protected const string cstrSIL = @"\SIL";
		protected const string cstrMapsTables = @"\MapsTables";
		protected const string cstrRepository = @"\Repository";
		protected const string cstrRegPathToSil = @"SOFTWARE\SIL";
		protected const string cstrLogFilename = @"\MoveEcData.log";

		protected static bool m_bVerbose = false;  // indicates whether to display debug information or not
		protected static string m_strCommonProgramFilesSilPath = null;

		/// <summary>
		/// Main program that moves the EncConverter's related stuff from the Program Files\Common Files folders
		/// to the CommonAppData folders. If you want a different destination rather than [CommonAppData],
		/// then pass it as the command line parameter to this program
		/// </summary>
		/// <param name="args">array of strings that are the arguments on the command line.
		/// The two possible command line arguments are:
		///     "/v" (for verbose output) and
		///     an optional target destination (e.g. "C:\...\My Documents"--we'll add the '\SIL' part)
		/// </param>
		static void Main(string[] args)
		{
			try
			{
				DoMain(args);
			}
			catch (Exception ex)
			{
				string strMsg = String.Format("Unable to move data because:{0}{1}", Environment.NewLine, ex.Message);
				if (ex.InnerException != null)
					strMsg += String.Format("{0}because: {1}", Environment.NewLine, ex.InnerException.Message);

				Log(strMsg);
			}
		}

		static void DoMain(string[] args)
		{
			// deal with command line arguments
			string strTargetDir = null;
			int nArgument = 0;
			if ((args.Length > nArgument) && (args[nArgument] == "/v"))
			{
				m_bVerbose = true;
				nArgument++;
			}

			if (args.Length > nArgument)
			{
				strTargetDir = args[0];
				nArgument++;    // unnecessary, but...
			}
			else
				strTargetDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

			// set-up the paths to the source and target folders
			System.Diagnostics.Debug.Assert(strTargetDir[strTargetDir.Length - 1] != '\\', "Don't pass the target directory with a final slash");
			string strCommonApplicationDataSilPath = strTargetDir + cstrSIL;
			m_strCommonProgramFilesSilPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + cstrSIL;

			// move all the files in the following folders (here the MapsTables and Repository folders)
			Log(String.Format("Moving files from: {0} to {1}", m_strCommonProgramFilesSilPath + cstrMapsTables,
					strCommonApplicationDataSilPath + cstrMapsTables));
			MoveFolderContents(m_strCommonProgramFilesSilPath + cstrMapsTables, strCommonApplicationDataSilPath + cstrMapsTables);

			Log(String.Format("Moving files from: {0} to {1}", m_strCommonProgramFilesSilPath + cstrRepository,
					strCommonApplicationDataSilPath + cstrRepository));
			MoveFolderContents(m_strCommonProgramFilesSilPath + cstrRepository, strCommonApplicationDataSilPath + cstrRepository);

			// update the repository key for where the repository xml file is stored
			Log(String.Format("Updating RegistryKey {0} from: {1} to {2}", EncConverters.HKLM_PATH_TO_XML_FILE,
					m_strCommonProgramFilesSilPath, strCommonApplicationDataSilPath));
			ReplaceRegistryKeyValue(Registry.LocalMachine, EncConverters.HKLM_PATH_TO_XML_FILE,
				EncConverters.strRegKeyForStorePath, m_strCommonProgramFilesSilPath, strCommonApplicationDataSilPath);

			// update the paths used by the options installer (specialized function)
			Log(String.Format("Fixing up Options Installer Paths from: {0} to {1}", m_strCommonProgramFilesSilPath,
					strCommonApplicationDataSilPath));
			FixupSECConverterOptionsInstallerPaths(m_strCommonProgramFilesSilPath, strCommonApplicationDataSilPath);

			// the following should come last
			// set the key that tells EncConverter to do a fixup of the converter specs
			Log(String.Format("Setting the RegistryKeyValue: {0} to {1}", EncConverters.strRegKeyForMovingRepository,
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
			SetRegistryKeyValue(Registry.LocalMachine, EncConverters.HKLM_PATH_TO_XML_FILE, EncConverters.strRegKeyForMovingRepository,
				strTargetDir);

			// finally, instantiate the EncConverters repository object which will trigger the path fixup
			//  (while we still have administrator privilege)
			Log("Launching EncConverters to fix-up the repository paths to the new target location");
			EncConverters aECs = new EncConverters();
		}

		/// <summary>
		/// This method can be used to move all of the files in the given source path (and all sub-folders)
		/// to the given target path.
		/// </summary>
		/// <param name="strSourcePath">e.g. "C:\Program Files\Common Files\SIL\MapsTables"</param>
		/// <param name="strTargetPath">e.g. "C:\Documents and Settings\All Users\Application Data\SIL\MapsTables"</param>
		static void MoveFolderContents(string strSourcePath, string strTargetPath)
		{
			if (Directory.Exists(strSourcePath))
			{
				string[] astrFilesInSource = Directory.GetFiles(strSourcePath, "*.*", SearchOption.AllDirectories);

				foreach (string strFileInSource in astrFilesInSource)
				{
					System.Diagnostics.Debug.Assert(strFileInSource.IndexOf(strSourcePath, StringComparison.OrdinalIgnoreCase) != -1);
					string strFileInTarget = strTargetPath + strFileInSource.Substring(strSourcePath.Length);
					FileMove(strFileInSource, strFileInTarget);
				}
			}
		}

		/// <summary>
		/// This is a specialized method that knows how to fixup the internal paths to convert files used by
		/// the SILConverters' Options Installer
		/// </summary>
		/// <param name="strSourcePath">Some portion of the path spec to where the files used to be (e.g. "C:\Program Files\Common Files")</param>
		/// <param name="strTargetPath">Some portion of the path spec to where the files used to be (e.g. "C:\Documents and Settings\All Users\Application Data")</param>
		static void FixupSECConverterOptionsInstallerPaths(string strSourcePath, string strTargetPath)
		{
			// the converter installer keeps track of all the converter files its installed so that it can remove them
			//  when the user uninstalls them. They are in the subfolders of the
			//  HKEY_LOCAL_MACHINE\SOFTWARE\SIL\SilEncConverters31\Installer\MapsTables
			// First, get a list of the sub-keys (because we'll have to iterate inside each one of these)
			RegistryKey keyInstallerRoot = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\SIL\SilEncConverters31\Installer\MapsTables");
			if (keyInstallerRoot != null)
			{
				string[] astrRegistrySubKeys = keyInstallerRoot.GetSubKeyNames();
				if ((astrRegistrySubKeys != null) && (astrRegistrySubKeys.Length > 0))
				{
					// for each subfolder, potentially fixup the path
					foreach (string strRegistrySubKey in astrRegistrySubKeys)
					{
						RegistryKey keyConverter = keyInstallerRoot.OpenSubKey(strRegistrySubKey, true);
						string[] astrPaths = keyConverter.GetValueNames();  // the names *are* the paths

						foreach (string strPath in astrPaths)
						{
							int nIndex = strPath.IndexOf(strSourcePath, StringComparison.OrdinalIgnoreCase);
							if (nIndex >= 0)
							{
								string strNewValue = strTargetPath + strPath.Substring(strSourcePath.Length);
								keyConverter.DeleteValue(strPath);
								keyConverter.SetValue(strNewValue, "");
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// This method can be used to replace the initial substring of a particular registry key value,
		/// 'strOriginalValueSubstring', with a new substring, strNewValueSubstring
		/// </summary>
		/// <param name="keyRoot">the root registry key (e.g. Registry.LocalMachine)</param>
		/// <param name="strKeyPath">the subkey name to open from the root key (e.g. "SOFTWARE\SIL\EncodingConverterRepository")</param>
		/// <param name="strValueName">the key's value name (e.g. "Registry")</param>
		/// <param name="strOriginalValueSubstring">the substring of the key value's data value to replace (e.g. "C:\Program Files\Common Files")</param>
		/// <param name="strNewValueSubstring">the substring of the key value's data value to replace with (e.g. "C:\Documents and Settings\All Users\Application Data")</param>
		static void ReplaceRegistryKeyValue(RegistryKey keyRoot, string strKeyPath, string strValueName,
			string strOriginalValueSubstring, string strNewValueSubstring)
		{
			RegistryKey key = keyRoot.OpenSubKey(strKeyPath, true);
			if (key != null)
			{
				string strOriginalValue = (string)key.GetValue(strValueName);
				if (!String.IsNullOrEmpty(strOriginalValue))
				{
					int nIndex = strOriginalValue.IndexOf(strOriginalValueSubstring, StringComparison.OrdinalIgnoreCase);
					if (nIndex >= 0)
					{
						string strNewValue = strNewValueSubstring + strOriginalValue.Substring(strOriginalValueSubstring.Length);
						key.SetValue(strValueName, strNewValue);
					}
				}
			}
		}

		/// <summary>
		/// This method can be used to set a particular registry key value
		/// </summary>
		/// <param name="keyRoot">the root registry key (e.g. Registry.LocalMachine)</param>
		/// <param name="strKeyPath">the subkey name to open from the root key (e.g. "SOFTWARE\SIL\EncodingConverterRepository")</param>
		/// <param name="strValueName">the key's value name (e.g. "Registry")</param>
		/// <param name="strValue">the new value to set for the key's value's data</param>
		static void SetRegistryKeyValue(RegistryKey keyRoot, string strKeyPath, string strValueName, string strValue)
		{
			RegistryKey key = keyRoot.OpenSubKey(strKeyPath, true);
			if (key != null)
				key.SetValue(strValueName, strValue);
		}

		/// <summary>
		/// This method can be used to move a file from the source location to the target location (if it doesn't
		/// already exist in the target location)
		/// </summary>
		/// <param name="strFileInSource">file spec to move (e.g. "C:\Program Files\Common Files\MapsTables\silipa93.tec")</param>
		/// <param name="strFileInTarget">file spec of the moved file (e.g. "C:\Documents and Settings\All Users\Application Data\MapsTables\silipa93.tec")</param>
		static void FileMove(string strFileInSource, string strFileInTarget)
		{
			InsureFolderExists(Path.GetDirectoryName(strFileInTarget));
			try
			{
				if (!File.Exists(strFileInTarget))
				{
					Log(String.Format("Attempting to move file: {0} to {1}", strFileInSource, strFileInTarget));
					// Note: Moving leaves the old file security settings rather than replacing them
					// based on the target directory, thus even though we set the target to AuthenticatedUsers
					// the moved files do not inherit this same security setting. Copy, on the other hand, inherits
					// security settings from the target folder, which is what we want to do.
					File.Copy(strFileInSource, strFileInTarget);
				}
			}
			catch (Exception ex)
			{
				// this may fail if for some reason, this user can't see the target folder
				string strMsg = String.Format("Unable to copy file because:{0}{1}", Environment.NewLine, ex.Message);
				if (ex.InnerException != null)
					strMsg += String.Format("{0}because: {1}", Environment.NewLine, ex.InnerException.Message);

				Log(strMsg);
			}
			finally
			{
				// get rid of the old file
				try
				{
					File.Delete(strFileInSource);
				}
				catch { }
			}
		}

		/// <summary>
		/// This method can be used to insure that the parent folder corresponding to the given filename exists
		/// and has full permissions for authenticated users.
		/// </summary>
		/// <param name="strFileSpec">the file spec whose parent folder's existence is to be insured (e.g. "C:\Documents and Settings\All Users\Application Data\MapsTables\silipa93.tec" to insure that the folder "C:\Documents and Settings\All Users\Application Data\MapsTables" is created)</param>
		/*
		static void InsureFolderExists(string strFileSpec)
		{
			string strFolderPath = Path.GetDirectoryName(strFileSpec);
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(strFolderPath));
			if (!Directory.Exists(strFolderPath))
				Directory.CreateDirectory(strFolderPath);
		}
		*/
		static void InsureFolderExists(string strFolderPath)
		{
			  Log("Making sure folder " + strFolderPath + " exists...");
			  bool fAssignedACL = false;
			  _InsureFolderExists(strFolderPath, ref fAssignedACL);
			  Log("...Done." + (fAssignedACL ? " Assigned" : " Did not assign") + " full permissions for authenticated users.");
		}

		/// <summary>
		/// This method can be used to insure that the parent folder corresponding to the given filename exists
		/// </summary>
		/// <param name="strFileSpec">the file spec whose parent folder's existence is to be insured (e.g. "C:\Documents and Settings\All Users\Application Data\MapsTables\silipa93.tec" to insure that the folder "C:\Documents and Settings\All Users\Application Data\MapsTables" is created)</param>
		static void _InsureFolderExists(string strFolderPath, ref bool fAssignedACL)
		{
			  if (!Directory.Exists(strFolderPath))
			  {
					_InsureFolderExists(Directory .GetParent(strFolderPath).FullName, ref fAssignedACL);

					Log("Creating folder " + strFolderPath);
					Directory.CreateDirectory(strFolderPath);

					if (!fAssignedACL)
					{
						  Log("Assigning full permissions for authenticated users on folder " + strFolderPath + "...");
						  // Set permissions on the new folder(s) so that all authenticated users get full control:
						  DirectorySecurity dSecurity = Directory .GetAccessControl(strFolderPath);

						  SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
						  FileSystemAccessRule rule = new FileSystemAccessRule(sid,
								FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
								PropagationFlags.None, AccessControlType.Allow);
						  dSecurity.AddAccessRule(rule);
						  Directory.SetAccessControl(strFolderPath, dSecurity);
						  fAssignedACL = true;
						  Log("...Done.");
					}
			  }
			  else
					Log("Folder " + strFolderPath + " exists.");
		}

		/// <summary>
		/// Adds a message to the Log file
		/// </summary>
		/// <param name="Msg"></param>
		static void Log(string Msg)
		{
			if (m_bVerbose)
				Console.WriteLine(Msg);

			// insure the parent folder exists (it won't if this is a 64-bit OS and this app is not compiled explicitly for 32-bit)
			if (Directory.Exists(m_strCommonProgramFilesSilPath))
			{
				StreamWriter fsLog = File.AppendText(m_strCommonProgramFilesSilPath + cstrLogFilename);
				fsLog.WriteLine(DateTime.Now + " :  " + Msg);
				fsLog.Close();
			}
		}
	}
}
