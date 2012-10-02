// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2005' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IFwMainWndSettings.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using Microsoft.Win32;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface allows controls to query a FW main window for and application-, project-,
	/// and main window-specific registry key for use by Persistence.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFwMainWndSettings
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a registry key where settings can be saved/loaded which are specific to the
		/// main window and the current project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		RegistryKey MainWndSettingsKey { get; }
	}
}
