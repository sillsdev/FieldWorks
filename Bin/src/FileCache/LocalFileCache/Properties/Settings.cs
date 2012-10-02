// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Settings.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace SIL.FieldWorks.Tools.Properties
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extends the Settings class so that it uses LocalDllSettingsProvider class, thus
	/// reading the settings from config file with product name
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SettingsProvider(typeof(SIL.FieldWorks.Tools.LocalDllSettingsProvider))]
	internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
	{
	}
}
