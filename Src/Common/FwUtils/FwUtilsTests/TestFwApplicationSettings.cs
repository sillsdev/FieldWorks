// Copyright (c) 2017 SIL International
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
	public class TestFwApplicationSettings : FwApplicationSettingsBase
	{
		/// <summary />
		public override bool UpdateGlobalWSStore { get; set; }

		/// <summary />
		public override ReportingSettings Reporting { get; set; }

		/// <summary />
		public override string LocalKeyboards { get; set; }

		/// <summary />
		public override string WebonaryUser { get; set; }

		/// <summary />
		public override string WebonaryPass { get; set; }

		/// <summary />
		public XDocument ConfigXml { get; set; }

		/// <summary />
		public override void UpgradeIfNecessary()
		{
			using (var stream = new MemoryStream())
			{
				ConfigXml.Save(stream);
				stream.Position = 0;
				MigrateIfNecessary(stream);
				ConfigXml = XDocument.Load(stream);
			}
		}
	}
}
