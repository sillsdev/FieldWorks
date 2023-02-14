// Copyright (c) 2015-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Configuration;
using System.Diagnostics;
using SIL.Settings;

namespace LCMBrowser.Properties
{

	/// <summary>
	/// Settings class to put a custom provider in.
	/// </summary>
	internal sealed partial class Settings
	{
		/// <summary />
		public Settings()
		{
			foreach (SettingsProperty property in Properties)
			{
				Debug.Assert(property.Provider is CrossPlatformSettingsProvider, $"Property '{property.Name}' Needs the Provider string set to CrossPlatformSettingsProvider.");
			}
		}
	}
}