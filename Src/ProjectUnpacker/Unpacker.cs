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
using SIL.Utils;

namespace SIL.FieldWorks.Test.ProjectUnpacker
{
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

				if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
					m_folder += Path.DirectorySeparatorChar;

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

		static private string BaseDirectory
		{
			get
			{
				// Unfortunately Paratext 6 doesn't like long paths like "C:\Documents and Settings\eberhard\Local Settings\Temp",
				// so we have to use the root directory of the boot drive. It doesn' matter on
				// Linux since we don't have Paratext there.
				if (Environment.OSVersion.Platform == PlatformID.Unix)
					return Path.GetTempPath();
				return DriveUtil.BootDrive;
			}
		}

		static private string SfTestSettingsFolder
		{
			get { return Path.Combine(BaseDirectory, @"sf_scr~files2003.~TOB~"); }
		}
		static private string PTTestSettingsFolder
		{
			get { return Path.Combine(BaseDirectory, @"~IWTEST~"); }
		}
		static private string BadPTTestSettingsFolder
		{
			get { return Path.Combine(BaseDirectory, @"~BadPTData~"); }
		}
		static private string MissingFilesPTTestSettingsFolder
		{
			get { return Path.Combine(BaseDirectory, @"~MissingPTData~"); }
		}
		static private string ECTestSettingsFolder
		{
			get { return Path.Combine(BaseDirectory, @"~~ECTestFiles~~"); }
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
			get {return SfTestSettingsFolder + Path.DirectorySeparatorChar;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default path of the paratext projects test folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string BadPtProjectTestFolder
		{
			get {return BadPTTestSettingsFolder + Path.DirectorySeparatorChar;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default path of the Paratext projects test folder that does not have
		/// all the files referenced in the Paratext SSF.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string MissingFilePtProjectTestFolder
		{
			get { return MissingFilesPTTestSettingsFolder + Path.DirectorySeparatorChar; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default path of the ECObjects test folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string ECTestProjectTestFolder
		{
			get {return ECTestSettingsFolder + Path.DirectorySeparatorChar;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default path of the paratext projects test folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string PtProjectTestFolder
		{
			get {return PTTestSettingsFolder + Path.DirectorySeparatorChar;}
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
				if (!unpackLocation.EndsWith(Path.DirectorySeparatorChar.ToString()))
					unpackLocation += Path.DirectorySeparatorChar;

				// Create our test folder below the temp folder.
				Directory.CreateDirectory(unpackLocation);
			}
			catch
			{
			}

			try
			{
				// Read the binary data from the resource file and unpack it
				ResourceManager resources =
					new ResourceManager("SIL.FieldWorks.Test.ProjectUnpacker." + packedProject,
					System.Reflection.Assembly.GetExecutingAssembly());

				string bootDrive = DriveUtil.BootDrive;
				bool convertDrive = bootDrive != @"C:\";
				byte[] buff = (byte[])resources.GetObject(packedProject);
				using (var memStream = new MemoryStream(buff))
				{
					using (var zipStream = new ZipInputStream(memStream))
					{
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
								using (var fileStreamWriter = fi.Create())
								{
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
								fi.LastWriteTime = zipEntry.DateTime;

								if (convertDrive && (Path.GetExtension(fileName) == ".tmp"))
								{
									// Both default to UTF-8, which is what the .ssf is.
									string readerPath = Path.Combine(currDir.FullName, fileName);
									string writerpath = Path.Combine(currDir.FullName, Path.ChangeExtension(fileName, ".ssf"));
									Debug.Assert(readerPath != null);
									using (var streamReader = new StreamReader(readerPath))
									{
										using (var streamWriter = new StreamWriter(writerpath))
										{
											for (var lineIn = streamReader.ReadLine(); lineIn != null; lineIn = streamReader.ReadLine())
											{
												string lineOut = lineIn;
												if (lineIn.IndexOf(@"C:\", 0) > 0)
													lineOut = lineIn.Replace(@"C:\", bootDrive);
												streamWriter.WriteLine(lineOut);
											}
										}
									}
								}
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
			return new RegistryData(Environment.OSVersion.Platform == PlatformID.Unix ?
				Registry.CurrentUser : Registry.LocalMachine,
				kPTSettingsRegKey, string.Empty, paratextFolder);
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
				Directory.Delete(folder.TrimEnd(Path.DirectorySeparatorChar), true);
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine(
					"Got exception in Unpacker.RemoveFiles (ignored): " + e.Message);
			}
		}
	}
}
