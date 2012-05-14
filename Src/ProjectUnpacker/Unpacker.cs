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
using NUnit.Framework;

namespace SIL.FieldWorks.Test.ProjectUnpacker
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Prepare Standard Format project test data.
	/// NOTE: When unpacking Paratext projects, the unpacker should be called in the
	/// TestFixtureSetup instead of the TestSetup (or in an individual test). The reason for
	/// this is that Paratext initializes with the current settings in the registry. These
	/// values are cached and can not be uninitialized (because everything is static).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class Unpacker
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

		private static string BaseDirectory
		{
			get
			{
				// Unfortunately Paratext 6 doesn't like long paths like "C:\Documents and Settings\eberhard\Local Settings\Temp",
				// so we have to use the root directory of the boot drive. It doesn' matter on
				// Linux since we don't have Paratext there.
				if (MiscUtils.IsUnix)
					return Path.GetTempPath();
				return DriveUtil.BootDrive;
			}
		}

		private const string kPTSettingsRegKey = @"SOFTWARE\ScrChecks\1.0\Settings_Directory";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory where Paratext projects are located.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string PTProjectDirectory
		{
			get
			{
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(kPTSettingsRegKey, false))
				{
					if (key == null)
						Assert.Ignore("This test requires Paratext to be properly installed.");
					return key.GetValue(null) as string;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path to the Scripture checks Settings_Directory registry key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string PTSettingsRegKey
		{
			get {return kPTSettingsRegKey;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default path of the paratext projects test folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string PtProjectTestFolder
		{
			get { return PTProjectDirectory + Path.DirectorySeparatorChar; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unzip the TEV and Kamwe Paratext projects to the disk.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void UnPackParatextTestProjects()
		{
			UnpackFile("ZippedParatextPrj", PTProjectDirectory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unzip a TEV Paratext project containing only Titus. This includes data and a style
		/// that cannot be mapped to a TE style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void UnpackTEVTitusWithUnmappedStyle()
		{
			UnpackFile("ZippedTEVTitusWithUnmappedStyle", PTProjectDirectory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method reads a chunk of binary data from the resource file, writes it to an
		/// .exe file and then executes the .exe. The .exe is a self-extracting executable
		/// containing two Paratext projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void UnPackMissingFileParatextTestProjects()
		{
			UnpackFile("ZippedParaPrjWithMissingFiles", PTProjectDirectory);
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
			if (!unpackLocation.EndsWith(Path.DirectorySeparatorChar.ToString()))
				unpackLocation += Path.DirectorySeparatorChar;

			try
			{
				// Read the binary data from the resource file and unpack it
				ResourceManager resources =
					new ResourceManager("SIL.FieldWorks.Test.ProjectUnpacker." + packedProject,
					System.Reflection.Assembly.GetExecutingAssembly());

				string replacePart = MiscUtils.IsUnix ? unpackLocation :
					unpackLocation.Substring(0, unpackLocation.IndexOf('\\', 4));
				using (var resourceStream = new MemoryStream((byte[])resources.GetObject(packedProject)))
				{
					using (var zipStream = new ZipInputStream(resourceStream))
					{
						ZipEntry zipEntry;
						while ((zipEntry = zipStream.GetNextEntry()) != null)
						{
							if (zipEntry.IsDirectory)
								continue; // We'll create directories for the files when they are read

							// create directory
							string directoryName = Path.GetDirectoryName(zipEntry.Name);
							DirectoryInfo currDir = Directory.CreateDirectory(Path.Combine(unpackLocation, directoryName));
							string pathname = Path.Combine(currDir.FullName, Path.GetFileName(zipEntry.Name));
							if (File.Exists(pathname))
								continue;

							using (MemoryStream entryStream = new MemoryStream((int)zipEntry.Size))
							{
								int size;
								byte[] data = new byte[2048];
								while (true)
								{
									size = zipStream.Read(data, 0, data.Length);
									if (size > 0)
										// fileStreamWriter.Write(data, 0, size);
										entryStream.Write(data, 0, size);
									else
										break;
								}

								// Because Paratext .ssf files contain a full hard-coded path
								// to the project, we need to change the path that is contained
								// in the file in the zip to be the location to which we are
								// actually unpacking.

								entryStream.Position = 0;
								using (var streamReader = new StreamReader(entryStream))
								{
									using (var streamWriter = new StreamWriter(pathname))
									{
										for (var lineIn = streamReader.ReadLine(); lineIn != null; lineIn = streamReader.ReadLine())
											streamWriter.WriteLine(lineIn.Replace(@"C:\~IWTEST~", replacePart));
									}
								}
							}
						}
					}
				}
			}
			catch(Exception e)
			{
				Console.Error.WriteLine("Got exception: {0} while unpacking {1}",
					e.Message, packedProject);
				throw;
			}
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
