// Copyright (c) 2017-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Reporting;
using SIL.Settings;

namespace FieldWorks.TestUtilities
{
	/// <summary>
	/// Test application settings class
	/// </summary>
	internal sealed class TestFwApplicationSettings : IFwApplicationSettings
	{
		/// <inheritdoc />
		bool IFwApplicationSettings.UpdateGlobalWSStore { get; set; }

		/// <inheritdoc />
		ReportingSettings IFwApplicationSettings.Reporting { get; set; }

		/// <inheritdoc />
		string IFwApplicationSettings.LocalKeyboards { get; set; }

		/// <inheritdoc />
		string IFwApplicationSettings.WebonaryUser { get; set; }

		/// <inheritdoc />
		string IFwApplicationSettings.WebonaryPass { get; set; }

		/// <inheritdoc />
		void IFwApplicationSettings.UpgradeIfNecessary()
		{
			using (var stream = new MemoryStream())
			{
				ConfigXml.Save(stream);
				stream.Position = 0;
				FwUtils.MigrateIfNecessary(stream, this);
				ConfigXml = XDocument.Load(stream);
			}
		}

		/// <inheritdoc />
		void IFwApplicationSettings.Save()
		{
			// Do nothing.
		}

		/// <inheritdoc />
		void IFwApplicationSettings.DeleteCorruptedSettingsFilesIfPresent()
		{
			// Do nothing.
		}

		public UpdateSettings Update { get; set; }

		/// <summary />
		internal XDocument ConfigXml { get; set; }
	}
}