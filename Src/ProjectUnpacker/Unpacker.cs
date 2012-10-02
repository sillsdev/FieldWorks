// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Unpacker.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Resources;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using ICSharpCode.SharpZipLib.Zip;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Test.ProjectUnpacker
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegistryData
	{
		/// <summary>Registry hive</summary>
		public enum RootKey
		{
			/// <summary>HKLM</summary>
			LocalMachine,
			/// <summary>HKCU</summary>
			CurrentUser
		};

		private string m_keyPath;			// this is the path through the reg tree
		private string m_keyName;			// this is the actual element name
		private string m_savedValue;		// this is the previous value
		private RegistryKey m_rootRegKey;	// RootKey.LocalMachine or RootKey.CurrentUser

		private bool m_FoundPath;			// the registry path
		private bool m_FoundKey;			// the registry element at 'path'
		private bool m_bHasBeenRestored;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Change or add the specified registry key with the specified value.
		/// </summary>
		/// <param name="root">Root key (i.e. should only be LocalMachine or CurrentUser)
		/// </param>
		/// <param name="keyPath">Key's path below root.</param>
		/// <param name="key">Key (or value) whose data will change. (Note: to set the
		/// Key's default value, use string.Empty)</param>
		/// <param name="desiredValue"></param>
		/// --------------------------------------------------------------------------------
		public RegistryData(RegistryKey root, string keyPath, string key, string desiredValue)
		{
			try
			{
				m_bHasBeenRestored = false;
				m_rootRegKey = root;
				m_keyPath = keyPath;
				m_keyName = key;
				RegistryKey regKey = null;

				// Try to find the key.
				regKey = m_rootRegKey.OpenSubKey(m_keyPath, true);

				if (regKey != null)
				{
					// The registry key and it's value exist so save the value for restoration later.
					const string noValueFound = "%No Value Found%";
					m_savedValue = (string)regKey.GetValue(m_keyName, noValueFound);
					regKey.SetValue(m_keyName, desiredValue);
					m_FoundPath = true;
					m_FoundKey = (m_savedValue != noValueFound);
				}
				else
				{
					// The registry key wasn't found so create it and set its value to the desired.
					m_FoundPath = false;
					m_FoundKey = false;
					regKey = m_rootRegKey.CreateSubKey( m_keyPath );
					regKey.SetValue(m_keyName, desiredValue);
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Remove the specified registry key.
		/// </summary>
		/// <param name="root">Root key (i.e. should only be LocalMachine or CurrentUser)
		/// </param>
		/// <param name="keyPath">Key's path below root.</param>
		/// <param name="key">Key (or value) whose data will change. (Note: to set the
		/// Key's default value, use string.Empty)</param>
		/// --------------------------------------------------------------------------------
		public RegistryData(RegistryKey root, string keyPath, string key)
		{
			try
			{
				m_bHasBeenRestored = false;
				m_rootRegKey = root;
				m_keyPath = keyPath;
				m_keyName = key;
				RegistryKey regKey = null;

				// Try to find the key.
				regKey = m_rootRegKey.OpenSubKey(m_keyPath, true);

				if (regKey != null)
				{
					// The registry key and it's value exist so save the value for restoration later.
					const string noValueFound = "%No Value Found%";
					m_savedValue = (string)regKey.GetValue(m_keyName, noValueFound);
					m_FoundPath = true;
					m_FoundKey = (m_savedValue != noValueFound);
					if (m_FoundKey)
						regKey.DeleteValue(m_keyName);
				}
				else
				{
					// The registry key wasn't found so there's nothing more to do.
					m_FoundPath = false;
					m_FoundKey = false;
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void RestoreRegistryData()
		{
			if (m_bHasBeenRestored)
				return;

			m_bHasBeenRestored = true;
			RegistryKey regKey = null;

			try
			{
				if (m_FoundPath == false)		// case where the path and value didn't exist at start
				{
					m_rootRegKey.DeleteSubKey(m_keyPath);
				}
				else if (m_FoundKey == false)	// case where the value didn't exist at the start
				{
					regKey = m_rootRegKey.OpenSubKey(m_keyPath,true);

					if (regKey != null)
						regKey.DeleteValue(m_keyName);
					else
						regKey = m_rootRegKey.CreateSubKey(m_keyPath);
				}
				else							// case where we have to restore the saved value
				{
					regKey = m_rootRegKey.OpenSubKey(m_keyPath, true);

					if (regKey == null)
						regKey = m_rootRegKey.CreateSubKey(m_keyPath);

					regKey.SetValue(m_keyName, m_savedValue);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="progID"></param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		static public string GetRegisteredDLLPath(string progID)	// ECObjects.ECProject[.1]
		{
			string subKey = progID + "\\CLSID";
			RegistryKey regKey = Registry.ClassesRoot.OpenSubKey( subKey );
			if (regKey == null)
			{
				return string.Empty;	// invalid or not registered progID
			}
			string guid = (string)regKey.GetValue(string.Empty);
			regKey.Close();

			subKey = "CLSID\\" + guid + "\\InprocServer32";
			regKey = Registry.ClassesRoot.OpenSubKey( subKey );
			if (regKey == null)
			{
				return string.Empty;	// registry is in a bad state [hope it's not our fault...]
			}

			string registeredPath = (string)regKey.GetValue(string.Empty);
			regKey.Close();

			return registeredPath;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Prepare Standard Format project test data.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Unpacker
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class ResourceUnpacker
		{
			private string m_folder;		// folder where the resource is unpacked

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="resource"></param>
			/// <param name="folder"></param>
			/// --------------------------------------------------------------------------------
			public ResourceUnpacker(String resource, String folder)
			{
				m_folder = folder.Trim();

				if (!folder.EndsWith("\\"))
					m_folder += "\\";

				UnpackFile(resource, m_folder);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string UnpackedDestinationPath
			{
				get {return m_folder;}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void CleanUp()
			{
				RemoveFiles(m_folder);
			}
		}


//		static public ResourceUnpacker ECImportDataResource = new ResourceUnpacker( "ECImportData", DriveUtil.BootDrive + @"~~asdfsdf_asdfas~");

		static private string SfTestSettingsFolder
		{
			get { return DriveUtil.BootDrive + @"sf_scr~files2003.~TOB~"; }
		}
		static private string PTTestSettingsFolder
		{
			get { return DriveUtil.BootDrive + @"~IWTEST~"; }
		}
		static private string BadPTTestSettingsFolder
		{
			get { return DriveUtil.BootDrive + @"~BadPTData~"; }
		}
		static private string MissingFilesPTTestSettingsFolder
		{
			get { return DriveUtil.BootDrive + @"~MissingPTData~"; }
		}
		static private string ECTestSettingsFolder
		{
			get { return DriveUtil.BootDrive + @"~~ECTestFiles~~"; }
		}
		private const string kPTSettingsRegKey = @"SOFTWARE\ScrChecks\1.0\Settings_Directory";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string RootDir
		{
			get {return DriveUtil.BootDrive + @"SIL_TEMP\RootDir";}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path to the Scripture checks Settings_Directory registry key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string PTSettingsRegKey
		{
			get {return kPTSettingsRegKey;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path of the standard format projects test folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string SfProjectTestFolder
		{
			get {return SfTestSettingsFolder + @"\";}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default path of the paratext projects test folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string BadPtProjectTestFolder
		{
			get {return BadPTTestSettingsFolder + @"\";}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default path of the Paratext projects test folder that does not have
		/// all the files referenced in the Paratext SSF.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string MissingFilePtProjectTestFolder
		{
			get { return MissingFilesPTTestSettingsFolder + @"\"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default path of the ECObjects test folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string ECTestProjectTestFolder
		{
			get {return ECTestSettingsFolder + @"\";}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default path of the paratext projects test folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string PtProjectTestFolder
		{
			get {return PTTestSettingsFolder + @"\";}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path of the folder where the TE style file was unpacked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string TEStyleFileTestFolder
		{
			get {return SfProjectTestFolder;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method reads a chunk of binary data from the resource file, writes it to an
		/// .exe file and then executes the .exe. The .exe is a self-extracting executable
		/// containing one Standard Format project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void UnPackSfTestProjects()
		{
			UnpackFile("ZippedSfPrj", SfTestSettingsFolder);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unzip the EcoSoStyles.sty file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void UnPackTEStyleFile()
		{
			UnpackFile("ZippedEcSoStyles", SfTestSettingsFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unzip the TEV and Kamwe Paratext projects to the disk.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void UnPackParatextTestProjects()
		{
			UnpackFile("ZippedParatextPrj", PTTestSettingsFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unzip a TEV Paratext project containing only Titus. This includes data and a style
		/// that cannot be mapped to a TE style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void UnpackTEVTitusWithUnmappedStyle()
		{
			UnpackFile("ZippedTEVTitusWithUnmappedStyle", PTTestSettingsFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unzip Titus in the TEV and Kamwe Paratext project. These files include data with
		/// extra markers from the original.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void UnpackParatextStyWithExtraMarkers()
		{
			UnpackFile("ZippedParatextStyWithExtraMarkers", PTTestSettingsFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method reads a chunk of binary data from the resource file, writes it to an
		/// .exe file and then executes the .exe. The .exe is a self-extracting executable
		/// containing two Paratext projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void UnPackBadParatextTestProjects()
		{
			UnpackFile("ZippedBadParatextPrj", BadPTTestSettingsFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method reads a chunk of binary data from the resource file, writes it to an
		/// .exe file and then executes the .exe. The .exe is a self-extracting executable
		/// containing two Paratext projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void UnPackMissingFileParatextTestProjects()
		{
			UnpackFile("ZippedParaPrjWithMissingFiles", MissingFilesPTTestSettingsFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method reads a chunk of binary data from the resource file, writes it to an
		/// .exe file and then executes the .exe. The .exe is a self-extracting executable
		/// containing multiple test files for the ECObjects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void UnPackECTestProjects()
		{
			UnpackFile("ACME", ECTestSettingsFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies a binary resx field to a filename and executes the file.  This assumes the
		/// binary data is the contents of a self-extracting executable zip file.
		/// </summary>
		/// <param name="packedProject">Name of resx file without the resx extension.  This is
		/// also the name of the resx field and the executable to run without the .exe
		/// extension.</param>
		/// <param name="unpackLocation">Folder where unpacking takes place. (This is only used
		/// when creating the folder.  Its up to the self-extracting zip file to know that
		/// unpacked files go here.)</param>
		/// ------------------------------------------------------------------------------------
		private static void UnpackFile(string packedProject, string unpackLocation)
		{
			try
			{
				if (!unpackLocation.EndsWith(@"\"))
					unpackLocation += @"\";

				// Create our test folder below the temp folder.
				Directory.CreateDirectory(unpackLocation);
			}
			catch
			{
			}

			MemoryStream memStream = null;
			ZipInputStream zipStream = null;
			try
			{
				// Read the binary data from the resource file and unpack it
				ResourceManager resources =
					new ResourceManager("SIL.FieldWorks.Test.ProjectUnpacker." + packedProject,
					System.Reflection.Assembly.GetExecutingAssembly());

				string bootDrive = DriveUtil.BootDrive;
				bool convertDrive = bootDrive != @"C:\";
				byte[] buff = (byte[])resources.GetObject(packedProject);
				memStream = new MemoryStream(buff);
				zipStream = new ZipInputStream(memStream);
				ZipEntry zipEntry;
				while ((zipEntry = zipStream.GetNextEntry()) != null)
				{
					string directoryName = Path.GetDirectoryName(zipEntry.Name);
					string fileName = Path.GetFileName(zipEntry.Name);
					if (convertDrive && (Path.GetExtension(fileName) == ".ssf"))
						fileName = Path.ChangeExtension(fileName, ".tmp");

					// create directory
					DirectoryInfo currDir = Directory.CreateDirectory(Path.Combine(unpackLocation, directoryName));
					if (fileName != null && fileName.Length != 0)
					{
						string pathname = Path.Combine(currDir.FullName, fileName);
						// This results in an assert being fired, in some C++ code.
						//if (File.Exists(pathname))
						//	continue;
						FileInfo fi = new FileInfo(pathname);
						Debug.Assert(fi != null);
						FileStream fileStreamWriter = null;
						try
						{
							fileStreamWriter = fi.Create();
							int size = 2048;
							byte[] data = new byte[2048];
							while (true)
							{
								size = zipStream.Read(data, 0, data.Length);
								if (size > 0)
									fileStreamWriter.Write(data, 0, size);
								else
									break;
							}
						}
						finally
						{
							if (fileStreamWriter != null)
								fileStreamWriter.Close();
							fileStreamWriter = null;
						}
						fi.LastWriteTime = zipEntry.DateTime;

						if (convertDrive && (Path.GetExtension(fileName) == ".tmp"))
						{
							// Both default to UTF-8, which is what the .ssf is.
							StreamReader streamReader = null;
							string readerPath = Path.Combine(currDir.FullName, fileName);
							StreamWriter streamWriter = null;
							string writerpath = Path.Combine(currDir.FullName, Path.ChangeExtension(fileName, ".ssf"));
							Debug.Assert(readerPath != null);
							try
							{
								streamReader = new StreamReader(readerPath);
								streamWriter = new StreamWriter(writerpath);
								string lineIn;
								while ((lineIn = streamReader.ReadLine()) != null)
								{
									string lineOut = lineIn;
									if (lineIn.IndexOf(@"C:\", 0) > 0)
										lineOut = lineIn.Replace(@"C:\", bootDrive);
									streamWriter.WriteLine(lineOut);
								}
							}
							finally
							{
								if (streamReader != null)
									streamReader.Close();
								streamReader = null;
								if (streamWriter != null)
									streamWriter.Close();
								streamWriter = null;
							}
						}
					}
				}
			}
			catch(Exception e)
			{
				System.Console.Error.WriteLine("Got exception: {0} while unpacking {1}",
					e.Message, packedProject);
				throw;
			}
			finally
			{
				if (memStream != null)
					memStream.Close();
				if (zipStream != null)
					zipStream.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set registry values so that it seems that Paratext is installed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static RegistryData PrepareRegistryForPTData()
		{
			return PrepareRegistryForPTData(PtProjectTestFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set registry values so that it seems that Paratext is installed.
		/// </summary>
		/// <param name="paratextFolder">The folder where ParaText is "installed".</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static RegistryData PrepareRegistryForPTData(string paratextFolder)
		{
			return new RegistryData(Registry.LocalMachine, kPTSettingsRegKey,
				string.Empty, paratextFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the folders and contents therein where the SF test project was unpacked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void RemoveSfTestProjects()
		{
			RemoveFiles(SfProjectTestFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the folders and contents where the TE style file was unpacked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void RemoveTEStyleFile()
		{
			RemoveFiles(SfProjectTestFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the folders and contents therein where the paratext test projects were
		/// unpacked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void RemoveBadParatextTestProject()
		{
			RemoveFiles(BadPtProjectTestFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the folders and contents therein where the paratext test projects were
		/// unpacked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void RemoveParatextMissingFileTestProject()
		{
			RemoveFiles(MissingFilePtProjectTestFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the folders and contents therein where the paratext test projects were
		/// unpacked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void RemoveParatextTestProjects()
		{
			RemoveFiles(PtProjectTestFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the folders and contents therein where the ECObjects files were unpacked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public void RemoveECTestProjects()
		{
			RemoveFiles(ECTestProjectTestFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="folder"></param>
		/// ------------------------------------------------------------------------------------
		private static void RemoveFiles(string folder)
		{
			try
			{
				Directory.SetCurrentDirectory(DriveUtil.BootDrive);
				Directory.Delete(folder.TrimEnd('\\'), true);
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine(
					"Got exception in Unpacker.RemoveFiles (ignored): " + e.Message);
			}
		}
	}
}
