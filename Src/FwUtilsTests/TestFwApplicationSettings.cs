// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Xml.Linq;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Test application settings class
	/// </summary>
	public class TestFwApplicationSettings : IFwApplicationSettings
	{
		/// <inheritdoc />
		public bool UpdateGlobalWSStore { get; set; }

		/// <inheritdoc />
		public ReportingSettings Reporting { get; set; }

		/// <inheritdoc />
		public string LocalKeyboards { get; set; }

		/// <inheritdoc />
		public string WebonaryUser { get; set; }

		/// <inheritdoc />
		public string WebonaryPass { get; set; }

		/// <summary />
		public XDocument ConfigXml { get; set; }

		/// <inheritdoc />
		public void UpgradeIfNecessary()
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
		public void Save()
		{
			// Do nothing.
		}

		/// <inheritdoc />
		public void DeleteCorruptedSettingsFilesIfPresent()
		{
			// Do nothing.
		}
	}
}