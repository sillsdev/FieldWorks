// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoFileExtensions.cs
// --------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Static class to hold a few constant FDO file extensions.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal static class FdoFileExtensions
	{
		/*
		 * The following extensions are also defined in FwFileExtensions
		 * as a temporary stopgap.
		 * The idea is that eventually, those will be removed
		 * and all references will use FdoFileExtensions.
		 *
		 * If a change is made here, it should be made in FwFileExtensions as well.
		 */
		/// <summary>Default extension for FieldWorks XML data files (with the period)</summary>
		internal const string ksFwDataXmlFileExtension = ".fwdata";
		/// <summary>Default extension for FieldWorks DB4o data files (with the period)</summary>
		internal const string ksFwDataDb4oFileExtension = ".fwdb";
		/// <summary>Default extension for FieldWorks backup files (with the period).</summary>
		internal const string ksFwBackupFileExtension = ".fwbackup";
		/// <summary>Default extension for FieldWorks 6.0 and earlier backup files (with the period).</summary>
		internal const string ksFw60BackupFileExtension = ".zip";
		/// <summary>Default extension for FieldWorks TEMPORARY fallback data files (with the period).</summary>
		internal const string ksFwDataFallbackFileExtension = ".bak";
	}
}
