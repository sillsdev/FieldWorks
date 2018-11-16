// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Reporting;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface for handling application settings.
	/// </summary>
	public interface IFwApplicationSettings
	{
		/// <summary />
		bool UpdateGlobalWSStore { get; set; }
		/// <summary />
		ReportingSettings Reporting { get; set; }
		/// <summary />
		string LocalKeyboards { get; set; }
		string WebonaryUser { get; set; }
		/// <summary />
		string WebonaryPass { get; set; }
		/// <summary>Upgrades the settings if necessary.</summary>
		void UpgradeIfNecessary();
		/// <summary>Saves the settings.</summary>
		void Save();
		/// <summary />
		void DeleteCorruptedSettingsFilesIfPresent();
	}
}