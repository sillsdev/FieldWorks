// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Reflection;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Class for getting version information out of an assembly
	/// </summary>
	public class VersionInfoProvider
	{
		private readonly Assembly m_assembly;
		private readonly bool m_fShowSILInfo;

		/// <summary />
		/// <param name="assembly">The assembly used to get the information.</param>
		/// <param name="fShowSILInfo">if set to <c>false</c>, any SIL-identifying information
		/// will be hidden.</param>
		public VersionInfoProvider(Assembly assembly, bool fShowSILInfo)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException(nameof(assembly));
			}

			m_assembly = assembly;
			m_fShowSILInfo = fShowSILInfo;
		}

		private static string InternalProductName
		{
			// Code copied from Mono implementation of Application.ProductName
			get
			{
				var name = string.Empty;
				var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
				var attrs = (AssemblyProductAttribute[])assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
				if (attrs != null && attrs.Length > 0)
				{
					name = attrs [0].Product;
				}
				// If there is no [AssemblyProduct], .NET returns the name of the innermost
				// namespace and if that fails, resorts to the name of the class containing Main()
				if (string.IsNullOrEmpty(name) && assembly.EntryPoint != null)
				{
					name = assembly.EntryPoint.DeclaringType.Namespace;
					if (name != null)
					{
						var lastDot = name.LastIndexOf('.');
						if (lastDot >= 0 && lastDot < name.Length - 1)
						{
							name = name.Substring(lastDot + 1);
						}
					}
					if (string.IsNullOrEmpty(name))
					{
						name = assembly.EntryPoint.DeclaringType.FullName;
					}
				}

				return name;
			}
		}

		private static string InternalProductVersion
		{
			// Code copied from Mono implementation of Application.ProductVersion
			get
			{
				var version = string.Empty;
				var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
				var infoVersion = Attribute.GetCustomAttribute(assembly, typeof (AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
				if (infoVersion != null)
				{
					version = infoVersion.InformationalVersion;
				}
				// If [AssemblyFileVersion] is present it is used
				// before resorting to assembly version
				if (string.IsNullOrEmpty(version))
				{
					var fileVersion = Attribute.GetCustomAttribute(assembly, typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
					if (fileVersion != null)
					{
						version = fileVersion.Version;
					}
				}
				// If neither [AssemblyInformationalVersionAttribute] nor [AssemblyFileVersion]
				// are present, then use the assembly version
				if (string.IsNullOrEmpty(version))
				{
					version = assembly.GetName().Version.ToString();
				}
				return version;
			}
		}

		/// <summary>
		/// Gets the name of the product.
		/// </summary>
		public string ProductName
		{
			get
			{
				var attributes = m_assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				return attributes != null && attributes.Length > 0 ? ((AssemblyTitleAttribute)attributes[0]).Title : InternalProductName;
			}
		}

		/// <summary>
		/// Gets the version of the application in the format x.x.x.x.
		/// </summary>
		public string NumericAppVersion
		{
			get
			{
				var attributes = m_assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				var version = (attributes.Length > 0) ? ((AssemblyFileVersionAttribute)attributes[0]).Version : InternalProductVersion;
				var ichSpace = version.IndexOf(' ');
				return ichSpace > 0 ? version.Remove(ichSpace) : version;
			}
		}

		/// <summary>
		/// Gets the version of the application in the format x.x.x.
		/// </summary>
		public string ShortNumericAppVersion
		{
			get
			{
				var version = NumericAppVersion;
				while (version.Count(c => c == '.') > 2)
				{
					version = version.Substring(0, version.LastIndexOf('.'));
				}
				return version;
			}
		}

		/// <summary>
		/// Gets a user-friendly version of the application.
		/// </summary>
		public string ApplicationVersion
		{
			get
			{
				// Set the application version text
				var attributes = m_assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				var appVersion = attributes.Length > 0 ? ((AssemblyFileVersionAttribute)attributes[0]).Version : InternalProductVersion;
				// Extract the fourth (and final) field of the version to get a date value.
				var ich = 0;
				for (var i = 0; i < 3; i++)
				{
					ich = appVersion.IndexOf('.', ich + 1);
				}
				var productDate = string.Empty;
				if (ich >= 0)
				{
					var iDate = Convert.ToInt32(appVersion.Substring(ich + 1));
					if (iDate > 0)
					{
						var dt = DateTime.FromOADate(iDate);
						productDate = dt.ToString("yyyy/MM/dd");
					}
				}
				string bitness;
				switch (IntPtr.Size)
				{
					case 4:
						bitness = "(32 bit)";
						break;
					case 8:
						bitness = "(64 bit)";
						break;
					default:
						bitness = "(Why hasn't he come back yet?)";
						break;
				}

				var buildSuffix = string.Empty;
#if DEBUG
				buildSuffix = "(Debug version)";
#endif
				return string.Format(FwUtilsStrings.kstidAppVersionFmt, appVersion, productDate, $"{bitness}{buildSuffix}");
			}
		}

		/// <summary>
		/// Gets the version of FieldWorks.
		/// </summary>
		public string MajorVersion
		{
			get
			{
				// Set the Fieldworks version text
				var attributes = m_assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
				var version = attributes.Length > 0 ? ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion : InternalProductVersion;
				return string.Format(FwUtilsStrings.kstidMajorVersionFmt, version);
			}
		}

		/// <summary>
		/// Gets a string containing the SIL copyright.
		/// </summary>
		public string CopyrightString
		{
			get
			{
				// Get copyright information from assembly info. By doing this we don't have
				// to update the splash screen each year.
				// Start as context sensitive and with current year.
				var copyRight = $"Copyright (c) 2002-{DateTime.Now.Year}";
				if (m_fShowSILInfo)
				{
					var attributes = m_assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
					copyRight = attributes.Length > 0 ? ((AssemblyCopyrightAttribute)attributes[0]).Copyright : $"{copyRight} SIL International";
				}
				// 00a9 is the copyright sign
				return copyRight.Replace("(c)", "\u00a9");
			}
		}

		/// <summary>
		/// Gets a description of the software license
		/// </summary>
		public string LicenseString => FwUtilsStrings.kstidLicense;

		/// <summary/>
		public string LicenseURL => FwUtilsStrings.kstidLicenseURL;
	}
}