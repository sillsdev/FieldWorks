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
// File: LocalDllSettingsProvider.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides persistence for application settings classes for use with DLLs.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LocalDllSettingsProvider: LocalFileSettingsProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LocalDllSettingsProvider"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LocalDllSettingsProvider()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="name"></param>
		/// <param name="values"></param>
		/// ------------------------------------------------------------------------------------
		public override void Initialize(string name, System.Collections.Specialized.NameValueCollection values)
		{
			base.Initialize(name, values);
			ApplicationName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the collection of setting property values for the specified application instance
		/// and settings property group.
		/// </summary>
		/// <param name="context">A <see cref="T:System.Configuration.SettingsContext"></see>
		/// describing the current application usage.</param>
		/// <param name="properties">A <see cref="T:System.Configuration.SettingsPropertyCollection"/>
		/// containing the settings property group whose values are to be retrieved.</param>
		/// <returns>A <see cref="T:System.Configuration.SettingsPropertyValueCollection"/> containing
		/// the values for the specified settings property group.
		/// </returns>
		///
		/// <exception cref="T:System.Configuration.ConfigurationErrorsException">
		/// A user-scoped setting was encountered but the current configuration only supports application-scoped settings.
		/// </exception>
		/// <PermissionSet>
		///		<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
		///			version="1" Unrestricted="true"/>
		///		<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
		///			version="1" Unrestricted="true"/>
		///		<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
		///			version="1" Flags="ControlEvidence, ControlPrincipal"/>
		/// </PermissionSet>
		/// ------------------------------------------------------------------------------------------
		public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context,
			SettingsPropertyCollection properties)
		{
			// Set the config files
			ExeConfigurationFileMap configMap = SetConfigFiles();

			// Create new collection of values
			SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();

			ReadProperties(context, properties, configMap, ConfigurationUserLevel.None,
				values);
			bool fHasUserConfig = ReadProperties(context, properties, configMap,
				ConfigurationUserLevel.PerUserRoamingAndLocal, values);
			ReadProperties(context, properties, configMap, ConfigurationUserLevel.PerUserRoaming,
				values);

			//if (!fHasUserConfig)
			{
				// save new user config file
				try
				{
					SetPropertyValues(context, values);
				}
				catch
				{
				}
			}

			return values;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the values of the specified group of property settings.
		/// </summary>
		/// <param name="context">A <see cref="T:System.Configuration.SettingsContext"/>
		/// describing the current application usage.</param>
		/// <param name="values">A <see cref="T:System.Configuration.SettingsPropertyValueCollection"/>
		/// representing the group of property settings to set.</param>
		/// <exception cref="T:System.Configuration.ConfigurationErrorsException">A user-scoped
		/// setting was encountered but the current configuration only supports application-
		/// scoped settings.
		/// -or-There was a general failure saving the settings to the configuration file.
		/// </exception>
		/// <PermissionSet>
		///		<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
		///			version="1" Unrestricted="true"/>
		///		<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
		///			version="1" Unrestricted="true"/>
		///		<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
		///			version="1" Flags="ControlEvidence, ControlPrincipal"/>
		/// </PermissionSet>
		/// ------------------------------------------------------------------------------------
		public override void SetPropertyValues(SettingsContext context,
			SettingsPropertyValueCollection values)
		{
			// Set the config files
			ExeConfigurationFileMap configMap = SetConfigFiles();

			Configuration localConfig = ConfigurationManager.OpenMappedExeConfiguration(configMap,
				ConfigurationUserLevel.PerUserRoamingAndLocal);
			Configuration roamingConfig = ConfigurationManager.OpenMappedExeConfiguration(configMap,
				ConfigurationUserLevel.PerUserRoaming);
			string groupName = (string)context["GroupName"];

			ClientSettingsSection localSettings =
				localConfig.GetSectionGroup("userSettings").Sections[groupName] as ClientSettingsSection;
			ClientSettingsSection roamingSettings =
				roamingConfig.GetSectionGroup("userSettings").Sections[groupName] as ClientSettingsSection;

			SettingElementCollection localCollection = localSettings.Settings;
			SettingElementCollection roamingCollection = roamingSettings.Settings;

			// Create new collection of values
			foreach (SettingsPropertyValue value in values)
			{
				if (value.Property.Attributes[typeof(UserScopedSettingAttribute)] != null)
				{
					SettingElement elem;
					if (value.Property.Attributes[typeof(SettingsManageabilityAttribute)] == null)
					{
						// this is a property for a local user
						elem = localCollection.Get(value.Name);
						if (elem == null)
						{
							elem = new SettingElement();
							elem.Name = value.Name;
							localCollection.Add(elem);
						}
					}
					else
					{
						// this is a property for a roaming user
						elem = roamingCollection.Get(value.Name);
						if (elem == null)
						{
							elem = new SettingElement();
							elem.Name = value.Name;
							roamingCollection.Add(elem);
						}
					}
					elem.SerializeAs = value.Property.SerializeAs;
					elem.Value.ValueXml = SerializeToXmlElement(value);
				}
			}

			if (localCollection.Count > 0)
				localConfig.Save();
			if (roamingCollection.Count > 0)
				roamingConfig.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the properties.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="properties">The properties.</param>
		/// <param name="configMap">The config map.</param>
		/// <param name="userLevel">The user level.</param>
		/// <param name="values">The values.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool ReadProperties(SettingsContext context,
			SettingsPropertyCollection properties, ExeConfigurationFileMap configMap,
			ConfigurationUserLevel userLevel, SettingsPropertyValueCollection values)
		{
			Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, userLevel);
			string groupName = (string)context["GroupName"];
			ClientSettingsSection appSettings = config.GetSection("applicationSettings/" + groupName) as ClientSettingsSection;
			ClientSettingsSection userSettings = config.GetSection("userSettings/" + groupName) as ClientSettingsSection;

			// Create new collection of values
			foreach (SettingsProperty setting in properties)
			{
				SettingsPropertyValue value = values[setting.Name];
				if (value == null)
				{
					values.Add(new SettingsPropertyValue(setting));
					value = values[setting.Name];
					value.IsDirty = true;
					value.SerializedValue = setting.DefaultValue;
				}

				if (setting.Attributes[typeof(UserScopedSettingAttribute)] != null && userSettings != null)
				{
					bool fIsRoamingProperty = setting.Attributes[typeof(SettingsManageabilityAttribute)] != null;
					if (userLevel == ConfigurationUserLevel.PerUserRoaming && !fIsRoamingProperty)
					{
						// we are processing only roaming user properties right now, and this isn't one
						continue;
					}

					SettingElement elem = userSettings.Settings.Get(setting.Name);
					if (elem != null)
						SetProperty(value, setting, elem);
				}
				else if (appSettings != null && userLevel == ConfigurationUserLevel.None)
				{
					// do this only if we are processing the app.config file
					SettingElement elem = appSettings.Settings.Get(setting.Name);
					if (elem != null)
						SetProperty(value, setting, elem);
				}
			}

			return config.HasFile;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the configuration files.
		/// </summary>
		/// <returns>File mapping for configuration files</returns>
		/// ------------------------------------------------------------------------------------
		private ExeConfigurationFileMap SetConfigFiles()
		{
			ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
			Assembly assembly = Assembly.GetCallingAssembly();
			object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
			AssemblyCompanyAttribute company;
			if (attributes.Length > 0)
				company = (AssemblyCompanyAttribute)attributes[0];
			else
				company = new AssemblyCompanyAttribute("Company");
			attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
			AssemblyProductAttribute product;
			if (attributes.Length > 0)
				product = (AssemblyProductAttribute)attributes[0];
			else
				product = new AssemblyProductAttribute("Product");
			string configFilename = Path.Combine(company.Company, Path.Combine(product.Product, "user.config"));
			configMap.LocalUserConfigFilename = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				configFilename);
			configMap.RoamingUserConfigFilename = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), configFilename);
			configMap.ExeConfigFilename = Path.Combine(Path.GetDirectoryName(assembly.Location), product.Product + ".config");
			return configMap;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extracts a property from the XML element.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="setting">The setting.</param>
		/// <param name="elem">The elem.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private void SetProperty(SettingsPropertyValue property,
			SettingsProperty setting, SettingElement elem)
		{
			string value = elem.Value.ValueXml.InnerXml;
			if (setting.SerializeAs == SettingsSerializeAs.String)
				value = elem.Value.ValueXml.InnerText;
			property.SerializedValue = value;
			property.IsDirty = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serializes to XML element.
		/// </summary>
		/// <param name="value">The property.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private XmlNode SerializeToXmlElement(SettingsPropertyValue value)
		{
			XmlElement element = new XmlDocument().CreateElement("value");
			string serializedString = value.SerializedValue as string;
			if ((serializedString == null) && (value.Property.SerializeAs == SettingsSerializeAs.Binary))
			{
				if (value.SerializedValue is byte[])
					serializedString = Convert.ToBase64String(value.SerializedValue as byte[]);
			}
			if (serializedString == null)
				serializedString = string.Empty;
			if (value.Property.SerializeAs == SettingsSerializeAs.String)
				element.InnerText = serializedString;
			else
				element.InnerXml = serializedString;
			return element;
		}


	}
}
