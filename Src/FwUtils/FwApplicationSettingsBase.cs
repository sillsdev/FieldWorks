// Copyright (c) 2017-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using System.Xml.Linq;
using SIL.Reporting;
using SIL.Settings;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This is the abstract base class for application settings. It can be extended for use in unit tests.
	/// </summary>
	public abstract class FwApplicationSettingsBase
	{
		public abstract ReportingSettings Reporting { get; set; }
		public abstract UpdateSettings Update { get; set; }
		public abstract string LocalKeyboards { get; set; }
		public abstract string WebonaryUser { get; set; }
		public abstract string WebonaryPass { get; set; }

		/// <summary>
		/// Upgrades the settings if necessary.
		/// </summary>
		public abstract void UpgradeIfNecessary();

		/// <summary>
		/// Saves the settings.
		/// </summary>
		public virtual void Save()
		{
		}

		/// <summary>
		/// Migrates the old CoreImpl config section to the new settings if necessary.
		/// </summary>
		protected bool MigrateIfNecessary(Stream stream)
		{
			XDocument configDoc = XDocument.Load(stream);
			stream.Position = 0;
			XElement userSettingsGroupElem = configDoc.Root?.Element("configSections")?.Elements("sectionGroup")
				.FirstOrDefault(e => (string)e.Attribute("name") == "userSettings");
			XElement coreImplSectionElem = userSettingsGroupElem?.Elements("section")
				.FirstOrDefault(e => (string)e.Attribute("name") == "SIL.CoreImpl.Properties.Settings");
			if (coreImplSectionElem != null)
			{
				XElement coreImplElem = configDoc.Root.Element("userSettings")?.Element("SIL.CoreImpl.Properties.Settings");
				if (coreImplElem != null)
				{
					foreach (XElement settingElem in coreImplElem.Elements("setting"))
					{
						XElement valueElem = settingElem.Element("value");
						if (valueElem == null)
							continue;

						switch ((string) settingElem.Attribute("name"))
						{
							case "UpdateGlobalWSStore":
								// UpdateGlobalWSStore is no longer used no longer used
								break;
							case "WebonaryUser":
								WebonaryUser = (string) valueElem;
								break;
							case "WebonaryPass":
								WebonaryPass = (string) valueElem;
								break;
							case "Reporting":
								Reporting = Deserialize<ReportingSettings>(valueElem);
								break;
							case nameof(Update):
								Update = Deserialize<UpdateSettings>(valueElem);
								break;
							case "LocalKeyboards":
								LocalKeyboards = (string) valueElem;
								break;
						}
					}
					coreImplElem.Remove();
				}
				coreImplSectionElem.Remove();

				stream.SetLength(0);
				configDoc.Save(stream);
				stream.Position = 0;
				return true;
			}
			return false;
		}

		private static T Deserialize<T>(XNode node)
		{
			var reader = node.CreateReader();
			reader.MoveToContent();
			var xml = reader.ReadInnerXml();
			return Xml.XmlSerializationHelper.DeserializeFromString<T>(xml);
		}
	}
}
