// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This is the abstract base class for application settings. It can be extended for use in unit tests.
	/// </summary>
	public abstract class FwApplicationSettingsBase
	{
		public abstract bool UpdateGlobalWSStore { get; set; }
		public abstract ReportingSettings Reporting { get; set; }
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
								UpdateGlobalWSStore = (bool) valueElem;
								break;
							case "WebonaryUser":
								WebonaryUser = (string) valueElem;
								break;
							case "WebonaryPass":
								WebonaryPass = (string) valueElem;
								break;
							case "Reporting":
								XmlReader reader = valueElem.CreateReader();
								reader.MoveToContent();
								string xml = reader.ReadInnerXml();
								Reporting = Xml.XmlSerializationHelper.DeserializeFromString<ReportingSettings>(xml);
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
	}
}
