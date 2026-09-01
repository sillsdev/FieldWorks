// Copyright (c) 2010-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SIL.Extensions;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for getting version information out of an assembly
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class VersionInfoProvider
	{

		internal static DateTime DefaultBuildDate = new DateTime(2001, 06, 23);
		/// <summary>Fallback copyright string, through the current year, for an assembly
		/// that carries no copyright attribute</summary>
		public static readonly string kDefaultCopyrightString =
			$"Copyright (c) 2002-{DateTime.Now.Year} SIL International";
		/// <summary>Fallback copyright string for sensitive areas, which omit the SIL
		/// name</summary>
		public static readonly string kSensitiveCopyrightString =
			$"Copyright (c) 2002-{DateTime.Now.Year}";

		private readonly Assembly m_assembly;
		private readonly bool m_fShowSILInfo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="VersionInfoProvider"/> class.
		/// </summary>
		/// <param name="assembly">The assembly used to get the information.</param>
		/// <param name="fShowSILInfo">if set to <c>false</c>, any SIL-identifying information
		/// will be hidden.</param>
		/// ------------------------------------------------------------------------------------
		public VersionInfoProvider(Assembly assembly, bool fShowSILInfo)
		{
			m_assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
			m_fShowSILInfo = fShowSILInfo;
		}

		private static string InternalProductName
		{
			// Code copied from Mono implementation of Application.ProductName
			get
			{
				var name = string.Empty;

				var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();


				var attrs = (AssemblyProductAttribute[])assembly.GetCustomAttributes(
					typeof(AssemblyProductAttribute), true);

				if (attrs != null && attrs.Length > 0)
					name = attrs [0].Product;

				// If there is no [AssemblyProduct], .NET returns the name of the innermost
				// namespace and if that fails, resorts to the name of the class containing Main()
				if (string.IsNullOrEmpty(name) && assembly.EntryPoint != null)
				{
					name = assembly.EntryPoint.DeclaringType.Namespace;

					if (name != null)
					{
						int lastDot = name.LastIndexOf('.');
						if (lastDot >= 0 && lastDot < name.Length - 1)
							name = name.Substring(lastDot + 1);
					}

					if (string.IsNullOrEmpty(name))
						name = assembly.EntryPoint.DeclaringType.FullName;
				}

				return name;
			}
		}

		private static string InternalProductVersion
		{
			get
			{
				var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
				ParseInformationalVersion(assembly, out var productVersion, out _);
				if (string.IsNullOrEmpty(productVersion))
				{
					var fileVersion = Attribute.GetCustomAttribute(assembly, typeof(AssemblyFileVersionAttribute))
						as AssemblyFileVersionAttribute;
					if (fileVersion != null)
						productVersion = fileVersion.Version;
				}

				// If neither [AssemblyInformationalVersionAttribute] nor [AssemblyFileVersion]
				// are present, then use the assembly version
				if (string.IsNullOrEmpty(productVersion))
					productVersion = assembly.GetName().Version.ToString();

				return productVersion;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the product.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProductName
		{
			get
			{
				object[] attributes = m_assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				return attributes != null && attributes.Length > 0 ?
					((AssemblyTitleAttribute)attributes[0]).Title : InternalProductName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of the application in the format x.x.x.x.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string NumericAppVersion
		{
			get
			{
				object[] attributes = m_assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				string version = (attributes.Length > 0) ?
					((AssemblyFileVersionAttribute)attributes[0]).Version : InternalProductVersion;
				int ichSpace = version.IndexOf(' ');
				return (ichSpace > 0) ? version.Remove(ichSpace) : version;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of the application in the format x.x.x.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ShortNumericAppVersion
		{
			get
			{
				var version = NumericAppVersion;
				while (version.Count(c => c == '.') > 2)
					version = version.Substring(0, version.LastIndexOf('.'));

				return version;
			}
		}

		/// <summary>
		/// Gets the build number of the base installer (used for downloading patches)
		/// </summary>
		public int BaseBuildNumber
		{
			get
			{
				var baseBuildAtt = m_assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute)).Cast<AssemblyMetadataAttribute>()
					.FirstOrDefault(att => "BaseBuildNumber".Equals(att.Key)) ;
				return int.TryParse(baseBuildAtt?.Value, out var baseBuildNum) ? baseBuildNum : 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a user-friendly version of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ApplicationVersion
		{
			get
			{
				// The entry assembly is only a fallback: under a test runner it is not
				// FieldWorks.
				ParseInformationalVersion(m_assembly, out var appVersion, out var productDate);
				if (string.IsNullOrEmpty(appVersion))
					appVersion = InternalProductVersion;
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

#if DEBUG
				return string.Format(FwUtilsStrings.kstidAppVersionFmt, appVersion, productDate, bitness + " (Debug version)");
#else
				return string.Format(FwUtilsStrings.kstidAppVersionFmt, appVersion, productDate, bitness);
#endif
			}
		}

		/// <summary>
		/// Find the AssemblyInformationalVersion and parse out the version, date, and product type.
		/// Expects version similar to 9.0.6 5234532 Alpha, where the second part is an encoded date
		/// </summary>
		/// <param name="assembly"></param>
		/// <param name="productVersion">filled with version or empty string</param>
		/// <param name="productDate">filled with build date or empty string</param>
		private static void ParseInformationalVersion(Assembly assembly, out string productVersion, out string productDate)
		{
			var informationalVersionAttr = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
			var appInfoVersion = informationalVersionAttr.Length > 0
				? ((AssemblyInformationalVersionAttribute)informationalVersionAttr[0]).InformationalVersion
				: null;
			var versionParts = appInfoVersion?.Split(new[] {' '}, 3);
			productDate = string.Empty;
			productVersion = string.Empty;
			var productType = string.Empty;
			switch (versionParts?.Length)
			{
				case 3:
				{
					// FWBETAVERSION is empty for stable builds, leaving a trailing space in the
					// informational version; don't let it leak into the parsed display strings.
					if (!string.IsNullOrWhiteSpace(versionParts[2]))
						productType = " " + versionParts[2].Trim();
					goto case 2;
				}
				case 2:
				{
					// A non-numeric token here would otherwise throw out of a property getter
					// that the splash screen and About box read.
					int.TryParse(versionParts[1], out var date);
					if (date > 0)
					{
						DateTime dt = DateTime.FromOADate(date);
						productDate = dt.ToISO8601TimeFormatDateOnlyString();
					}

					goto case 1;
				}
				case 1:
				{
					productVersion = versionParts[0] + productType;
					break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of FieldWorks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string MajorVersion
		{
			get
			{
				// Parts are MAJOR.MINOR.REVISION.BUILDNUMBER STABILITY; stable builds carry no
				// STABILITY, so a missing part must yield nothing rather than a placeholder.
				ParseInformationalVersion(m_assembly, out var productVersion, out _);
				var realParts = productVersion.Split('.', ' ');
				var major = realParts.Length > 0 ? realParts[0] : "?";
				var minor = realParts.Length > 1 ? realParts[1] : "?";
				var stability = realParts.Length > 4 ? realParts[4] : string.Empty;

				return string.Format(FwUtilsStrings.kstidMajorVersionFmt, $"{major}.{minor} {stability}".TrimEnd());
			}
		}

		/// <summary>The date this version of FieldWorks was built, or the date of the first FieldWorks checkin</summary>
		internal DateTime ApparentBuildDate
		{
			get
			{
				ParseInformationalVersion(m_assembly, out _, out var date);
				if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None,
						out var buildDate))
				{
					return buildDate;
				}
				return DefaultBuildDate;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string containing the SIL copyright.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CopyrightString
		{
			get
			{
				// Both modes derive from the assembly attribute, so the years stay current.
				string copyRight = null;
				object[] attributes = m_assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (attributes != null && attributes.Length > 0)
					copyRight = ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
				if (string.IsNullOrEmpty(copyRight))
					copyRight = m_fShowSILInfo ? kDefaultCopyrightString : kSensitiveCopyrightString;
				if (!m_fShowSILInfo)
					copyRight = copyRight.Replace("SIL International", string.Empty).TrimEnd();
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
