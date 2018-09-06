// Copyright (c) 2002-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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

using System;
using System.IO;
using System.Management;

namespace SIL.FieldWorks.Common.FwUtils
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
		public static string BootDrive
		{
			get
			{
				return Path.GetPathRoot(Environment.GetEnvironmentVariable("windir"));
			}
		}

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
		public static string DirectoryNameOnly(string fullName)
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
		public static string FileNameOnly(string fullName)
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
		public static string FileExtensionOnly(string fullName)
		{
			FileInfo fi = new FileInfo(fullName);
			return fi.Extension;
		}

	}
}
