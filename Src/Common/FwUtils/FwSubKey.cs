// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwSubKey.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This provides a string enumeration for subkeys of HKCU\Software\SIL\FieldWorks.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwSubKey
	{
		/// <summary>
		/// The name of the Translation Editor registry subkey (Even though this is the same as
		/// DirectoryFinder.ksTeFolderName and FwUtils.ksTeAppName, PLEASE do not use them interchangeably.
		/// Use the one that is correct for your context, in case they need to be changed later.)
		/// </summary>
		public const string TE = FwUtils.ksTeAppName;
		/// <summary>
		/// The name of the Language Explorer registry subkey (Even though this is the same as
		/// DirectoryFinder.ksFlexFolderName and FwUtils.ksFlexAppName, PLEASE do not use them interchangeably.
		/// Use the one that is correct for your context, in case they need to be changed later.)
		/// </summary>
		public const string LexText = FwUtils.ksFlexAppName;
	}
}