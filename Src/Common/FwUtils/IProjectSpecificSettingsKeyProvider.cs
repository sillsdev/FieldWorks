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
// File: IProjectSpecificSettingsKeyProvider.cs
// Responsibility: FW Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using Microsoft.Win32;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for retrieving a registry key for the current project
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IProjectSpecificSettingsKeyProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the project specific settings key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		RegistryKey ProjectSpecificSettingsKey { get; }
	}
}
