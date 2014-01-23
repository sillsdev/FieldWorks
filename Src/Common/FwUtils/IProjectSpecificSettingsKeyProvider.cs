// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
