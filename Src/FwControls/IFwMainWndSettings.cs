// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
