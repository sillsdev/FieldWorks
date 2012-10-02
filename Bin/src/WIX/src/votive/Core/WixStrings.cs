//--------------------------------------------------------------------------------------------------
// <copyright file="WixStrings.cs" company="Microsoft">
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
// Contains all of the managed resource strings specific to the Votive project.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Collections.Specialized;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Contains all of the managed resource strings specific to the Votive project.
	/// </summary>
	public sealed class WixStrings
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static WixManagedResourceManager manager = new WixManagedResourceManager();
		private static StringDictionary stringIdMap;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Prevent direct instantiation of this class.
		/// </summary>
		private WixStrings()
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the filter to use in the Add Reference dialog.
		/// </summary>
		public static string AddReferenceDialogFilter
		{
			get
			{
				string raw = GetString(StringId.AddReferenceDialogFilter);

				// Visual Studio wants the dialog filter parts separated with a null character.
				// Reading the raw string from the resource file does not replace \0 with a null
				// character. Rather it gives us the string "\0". So we'll replace it ourself.
				string formatted = raw.Replace(@"\0", "\0");
				return formatted;
			}
		}

		/// <summary>
		/// Gets the title of the Add Reference dialog.
		/// </summary>
		public static string AddReferenceDialogTitle
		{
			get { return GetString(StringId.AddReferenceDialogTitle); }
		}

		/// <summary>
		/// Gets a localized string like "Wixlib References".
		/// </summary>
		public static string WixlibReferenceFolderCaption
		{
			get { return GetString(StringId.WixlibReferenceFolderCaption); }
		}
		#endregion

		#region Enums
		//==========================================================================================
		// Enums
		//==========================================================================================

		/// <summary>
		/// These are all of the strings in the votive.dll.
		/// </summary>
		public enum StringId
		{
			AddReferenceDialogFilter,
			AddReferenceDialogTitle,
			OutputWindowClean,
			WixBuildSettingsOutputTypeDescription,
			WixBuildSettingsOutputTypeDisplayName,
			WixlibReferenceFolderCaption,
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Gets a localized string like "Deleting intermediate files and output files for project '{0}', configuration '{1}'."
		/// </summary>
		/// <param name="projectName">The project name.</param>
		/// <param name="configurationName">The configuration name (i.e. Debug).</param>
		/// <returns>Deleting intermediate files and output files for project '{0}', configuration '{1}'.</returns>
		public static string OutputWindowClean(string projectName, string configurationName)
		{
			Tracer.VerifyStringArgument(projectName, "projectName");
			Tracer.VerifyStringArgument(configurationName, "configurationName");
			return GetString(StringId.OutputWindowClean, projectName, configurationName);
		}

		/// <summary>
		/// Returns a value indicating whether the specified name is a valid string resource name.
		/// </summary>
		/// <param name="name">The resource identifier to check.</param>
		/// <returns>true if the string identifier is defined in our assembly; otherwise, false.</returns>
		internal static bool IsValidStringName(string name)
		{
			// We want to create a cached string id map so that we don't have to use costly reflection
			// every time we retrieve a string from the resource dll.
			if (stringIdMap == null)
			{
				stringIdMap = new StringDictionary();
				foreach (string id in Enum.GetNames(typeof(StringId)))
				{
					stringIdMap.Add(id, id);
				}
			}

			return stringIdMap.ContainsKey(name);
		}

		/// <summary>
		/// Gets an unformatted string from the resource file.
		/// </summary>
		/// <param name="id">The resource identifier of the string to retrieve.</param>
		/// <returns>An unformatted string from the resource file.</returns>
		private static string GetString(StringId id)
		{
			return GetString(id, null);
		}

		/// <summary>
		/// Gets a string from the resource file and formats it using the specified arguments.
		/// </summary>
		/// <param name="id">The resource identifier of the string to retrieve.</param>
		/// <param name="args">An array of objects to use in the formatting. Can be null or empty.</param>
		/// <returns>A formatted string from the resource file.</returns>
		private static string GetString(StringId id, params object[] args)
		{
			string resourceName = id.ToString();
			return manager.GetString(resourceName, args);
		}
		#endregion
	}
}