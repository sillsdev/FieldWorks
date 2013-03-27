// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2002' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DriveUtil.cs
// Responsibility: DavidO
//
// <remarks>
// The DriveInfo class contains a number of static classes
// Using 'Win32_LogicalDisk' in a WMI query (see documentation for ManagementObject,
// ManagementObjectSearcher and ManagementObjectCollection) returns a collection of
// objects, each of which has a bunch of properties. The following list shows what
// those properties are.
//
//	Access,						Availability,		BlockSize,
//	Caption,					Compressed,			ConfigManagerErrorCode,
//	ConfigManagerUserConfig,	CreationClassName,	Description,
//	DeviceID,					DriveType,			ErrorCleared,
//	ErrorDescription,			ErrorMethodology,	FileSystem,
//	FreeSpace,					InstallDate,		LastErrorCode,
//	MaximumComponentLength,		MediaType,			Name,
//	NumberOfBlocks,				PNPDeviceID,		PowerManagementCapabilities,
//	PowerManagementSupported,	ProviderName,		Purpose,
//	QuotasDisabled,				QuotasIncomplete,	QuotasRebuilding,
//	Size,						Status,				SupportsFileBasedCompression,
//	StatusInfo,					SupportsDiskQuotas,	SystemCreationClassName,
//	SystemName,					VolumeDirty,		VolumeName,
//	VolumeSerialNumber,
//
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Management;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for DriveInfo.
	/// </summary>
	public class DriveUtil
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public enum DriveTypes: int
		{
			/// <summary>Drive type couldn't be determined.</summary>
			Invalid = 0,
			/// <summary>I'm not sure what this type is.</summary>
			NoRoot,
			/// <summary>Floppy disks, Zip disks, etc.</summary>
			Removable,
			/// <summary>Hard disks</summary>
			LocalFixed,
			/// <summary>Mapped network drives</summary>
			Network,
			/// <summary>CD, DVD type drives</summary>
			CD,
			/// <summary>RAM disks</summary>
			RAMDrive
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the boot drive, based on the location of the system directory. (Note: This
		/// includes the colon and backslash in the return value.)
		/// </summary>
		/// <remarks>This returns an empty string on Linux (unless environment variable
		/// 'windir' happens to be set).</remarks>
		/// -----------------------------------------------------------------------------------
		static public string BootDrive
		{
			get
			{
				return Path.GetPathRoot(Environment.GetEnvironmentVariable("windir"));
			}
		}

#if !__MonoCS__
		// This method is currently not used anywhere. If we'll use it we have to do something
		// on Linux since it probably won't work like this.

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// A static method returning a logical drive's type.
		/// </summary>
		/// <param name="drive">A drive specification in any one of three formats:
		/// 'x', 'x:' or 'x:\' (where 'x' is the drive letter)</param>
		/// <returns>A value indicating the type of the drive specified.</returns>
		/// -----------------------------------------------------------------------------------
		static public DriveTypes GetLogicalDriveType(string drive)
		{
			try
			{
				// This check is here because when a ManagementObject is created by getting
				// the device ID for a diskette drive, the drive sounds like it's trying to
				// be accessed. That's annoying and time consuming. I'm going to go out on
				// a limb (a safe one, I think) and assume all drive's labeled 'A' and 'B'
				// are diskette drives.
				if (drive.ToUpper().StartsWith("A") || drive.ToUpper().StartsWith("B"))
					return DriveTypes.Removable;

				// Using the ManagmentObject (WMI) model to get drive types expects
				// drive specifications to be in the form "x:" (where x is the drive
				// letter). So if the caller passes a drive letter with the trailing
				// backslash, (which is quite likely if the caller used the
				// GetLogicalDrives() .Net method to get a list of available drives),
				// strip it off.
				if (drive.EndsWith("\\"))
					drive = drive.Substring(0, 2);

				using (ManagementObject mo =
					new ManagementObject("win32_logicaldisk.DeviceID=\"" + drive + "\""))
				{
					// This is a funky cast, but that's what it took to force conformity.
					return (DriveTypes)((System.UInt32)(mo["DriveType"]));
				}
			}
			catch
			{
				return DriveTypes.Invalid;
			}
		}
#endif

		//--------------------------------------------------------------------------------------
		// Removing code that is no longer used in the src base.  It was using
		// Application.DoEvents which is being irradicated from the src.  This is
		// old code and much of it now exists in the framework.
		//--------------------------------------------------------------------------------------

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Strips off and returns the directory path portion (i.e. doesn't include file name)
		/// from a fully qualified path.
		/// </summary>
		/// <param name="fullName">Fully qualified path, including file name.</param>
		/// <returns>Just the directory path portion of a full path. (Uses the
		/// FileInfo.DirectoryName method.)</returns>
		///--------------------------------------------------------------------------------------
		static public string DirectoryNameOnly(string fullName)
		{
			FileInfo fi = new FileInfo(fullName);
			return fi.DirectoryName;
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Strips off and returns the file name portion from a fully qualified path.
		/// </summary>
		/// <param name="fullName">Fully qualified path, including file name.</param>
		/// <returns>Just the file name portion of a full path. (Uses the FileInfo.Name
		/// method.)</returns>
		///--------------------------------------------------------------------------------------
		static public string FileNameOnly(string fullName)
		{
			FileInfo fi = new FileInfo(fullName);
			return fi.Name;
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Strips off and returns the file extension portion (including the period) from a
		/// fully qualified path.
		/// </summary>
		/// <param name="fullName">Fully qualified path, including file name.</param>
		/// <returns>Just the file extension portion (including the period) of a full
		/// path. (Uses the FileInfo.Extension method.)</returns>
		///--------------------------------------------------------------------------------------
		static public string FileExtensionOnly(string fullName)
		{
			FileInfo fi = new FileInfo(fullName);
			return fi.Extension;
		}

		//--------------------------------------------------------------------------------------
		// Removing code that is no longer used in the src base.  It was using
		// Application.DoEvents which is being irradicated from the src.  This is
		// old code and much of it now exists in the framework.
		//--------------------------------------------------------------------------------------
	}
}
