// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
		/// The name of the Language Explorer registry subkey (Even though this is the same as
		/// FwDirectoryFinder.ksFlexFolderName and FwUtils.ksFlexAppName, PLEASE do not use them interchangeably.
		/// Use the one that is correct for your context, in case they need to be changed later.)
		/// </summary>
		public const string LexText = "Language Explorer";
	}
}