//-------------------------------------------------------------------------------------------------
// <copyright file="PackageSettings.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Contains all of the various registry settings for the package.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Globalization;
	using Microsoft.VisualStudio.Shell.Interop;
	using Microsoft.Win32;

	/// <summary>
	/// Helper class for setting and retrieving registry settings for the package. All machine
	/// settings are cached on first use, so only one registry read is performed.
	/// </summary>
	public class PackageSettings
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(PackageSettings);
		private static readonly Version DefaultVersion = new Version(7, 1, 3088);

		private const string MachineSettingsRegKey = @"InstalledProducts\Project";
		private const string VisualStudioVersionRegKey = @"Setup\VS\BuildNumber";

		private string machineRootPath;
		private string visualStudioRegistryRoot;
		private Version visualStudioVersion = null;

		// Machine settings
		private MachineSettingEnum traceLevel;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="PackageSettings"/> class.
		/// </summary>
		/// <param name="serviceProvider">The <see cref="ServiceProvider"/> to use.</param>
		public PackageSettings(ServiceProvider serviceProvider) : this(serviceProvider, MachineSettingsRegKey)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PackageSettings"/> class.
		/// </summary>
		/// <param name="serviceProvider">The <see cref="ServiceProvider"/> to use.</param>
		/// <param name="machineSettingsRegistryKey">
		/// Relative registry path to machine-level settings. The path is relative to the Visual Studio registry root.
		/// </param>
		protected PackageSettings(ServiceProvider serviceProvider, string machineSettingsRegistryKey)
		{
			Tracer.VerifyNonNullArgument(serviceProvider, "serviceProvider");

			// Read in the Visual Studio registry root.
			IVsShell vsShell = serviceProvider.GetVsShell(classType, Tracer.ConstructorMethodName);
			object rootPathObj;
			int hr = vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_VirtualRegistryRoot, out rootPathObj);
			this.visualStudioRegistryRoot = (string)rootPathObj;
			this.machineRootPath = this.RegistryPathCombine(this.visualStudioRegistryRoot, machineSettingsRegistryKey);

			// Initialize all of the machine settings.
			this.traceLevel = new MachineSettingEnum(this.MachineRootPath, KeyNames.TraceLevel, Tracer.Level.Critical, typeof(Tracer.Level));
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the trace level threshold.
		/// </summary>
		public Tracer.Level TraceLevel
		{
			get { return (Tracer.Level)this.traceLevel.Value;}
		}

		/// <summary>
		/// Gets the version of the currently running instance of Visual Studio.
		/// </summary>
		public Version VisualStudioVersion
		{
			get
			{
				if (this.visualStudioVersion == null)
				{
					string regPath = this.RegistryPathCombine(this.VisualStudioRegistryRoot, VisualStudioVersionRegKey);
					using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(regPath, false))
					{
						string lcid = CultureInfo.CurrentUICulture.LCID.ToString();
						string versionString = regKey.GetValue(lcid) as string;
						if (versionString == null)
						{
							Tracer.Fail("Cannot find the Visual Studio environment version in the registry path '{0}'.", this.RegistryPathCombine(regPath, lcid));
							this.visualStudioVersion = DefaultVersion;
						}
						else
						{
							try
							{
								this.visualStudioVersion = new Version(versionString);
							}
							catch (Exception e)
							{
								Tracer.Fail("Cannot parse the Visual Studio environment version string {0}: {1}", versionString, e.ToString());
								this.visualStudioVersion = DefaultVersion;
							}
						}
					}
				}
				return this.visualStudioVersion;
			}
		}

		/// <summary>
		/// Gets the root path to the machine-level registry settings.
		/// </summary>
		protected string MachineRootPath
		{
			get { return this.machineRootPath; }
		}

		/// <summary>
		/// Gets the root registry path to the current version of Visual Studio.
		/// </summary>
		protected string VisualStudioRegistryRoot
		{
			get { return this.visualStudioRegistryRoot; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Combines two registry paths.
		/// </summary>
		/// <param name="path1">The first path to combine.</param>
		/// <param name="path2">The second path to combine.</param>
		/// <returns>The concatenation of the first path with the second, delimeted with a '\'.</returns>
		protected string RegistryPathCombine(string path1, string path2)
		{
			Tracer.VerifyStringArgument(path1, "path1");
			Tracer.VerifyStringArgument(path2, "path2");

			return PackageUtility.EnsureTrailingChar(path1, '\\') + path2;
		}
		#endregion

		#region Classes
		//==========================================================================================
		// Classes
		//==========================================================================================

		/// <summary>
		/// Names of the various registry keys that store our settings.
		/// </summary>
		private sealed class KeyNames
		{
			public static readonly string TraceCategoryFilter = "TraceCategoryFilter";
			public static readonly string TraceLevel = "TraceLevel";
		}

		/// <summary>
		/// Abstract base class for a strongly-typed machine-level setting.
		/// </summary>
		protected abstract class MachineSetting
		{
			private object defaultValue;
			private bool initialized;
			private string name;
			private string rootPath;

			public MachineSetting(string rootPath, string name, object defaultValue)
			{
				this.rootPath = rootPath;
				this.name = name;
				this.defaultValue = defaultValue;
			}

			public string Name
			{
				get { return this.name; }
			}

			protected object DefaultValue
			{
				get { return this.defaultValue; }
			}

			protected bool Initialized
			{
				get { return this.initialized; }
			}

			public void Refresh()
			{
				using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(this.rootPath, false))
				{
					object value = regKey.GetValue(this.name, this.defaultValue);
					this.initialized = true;
					this.CastAndStoreValue(value);
				}
			}

			protected abstract void CastAndStoreValue(object value);
		}

		/// <summary>
		/// Represents a strongly-typed integer machine setting.
		/// </summary>
		protected class MachineSettingInt32 : MachineSetting
		{
			private int value;

			public MachineSettingInt32(string rootPath, string name, int defaultValue) : base(rootPath, name, defaultValue)
			{
			}

			public int Value
			{
				get
				{
					if (!this.Initialized)
					{
						this.Refresh();
					}
					return this.value;
				}
			}

			protected override void CastAndStoreValue(object value)
			{
				try
				{
					this.value = (int)value;
				}
				catch (InvalidCastException)
				{
					this.value = (int)this.DefaultValue;
					Tracer.Fail("Cannot convert '{0}' to an Int32.", value.ToString());
				}
			}
		}

		/// <summary>
		/// Represents a strongly-typed string machine setting.
		/// </summary>
		protected class MachineSettingString : MachineSetting
		{
			private string value;

			public MachineSettingString(string rootPath, string name, string defaultValue) : base(rootPath, name, defaultValue)
			{
			}

			public string Value
			{
				get
				{
					if (!this.Initialized)
					{
						this.Refresh();
					}
					return this.value;
				}
			}

			protected override void CastAndStoreValue(object value)
			{
				try
				{
					this.value = (string)value;
				}
				catch (InvalidCastException)
				{
					this.value = (string)this.DefaultValue;
					Tracer.Fail("Cannot convert '{0}' to a string.", value.ToString());
				}
			}
		}

		/// <summary>
		/// Represents a strongly-typed enum machine setting.
		/// </summary>
		protected class MachineSettingEnum : MachineSetting
		{
			private Type enumType;
			private Enum value;

			public MachineSettingEnum(string rootPath, string name, Enum defaultValue, Type enumType) : base(rootPath, name, defaultValue)
			{
				this.enumType = enumType;
			}

			public Enum Value
			{
				get
				{
					if (!this.Initialized)
					{
						this.Refresh();
					}
					return this.value;
				}
			}

			protected override void CastAndStoreValue(object value)
			{
				try
				{
					this.value = (Enum)Enum.Parse(this.enumType, value.ToString(), true);
				}
				catch (FormatException)
				{
					this.value = (Enum)this.DefaultValue;
					Tracer.Fail("Cannot convert '{0}' to an enum of type '{1}'.", value.ToString(), this.enumType.Name);
				}
				catch (InvalidCastException)
				{
					this.value = (Enum)this.DefaultValue;
					Tracer.Fail("Cannot convert '{0}' to a string.", value.ToString());
				}
			}
		}
		#endregion
	}
}
