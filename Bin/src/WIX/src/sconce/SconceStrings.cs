//--------------------------------------------------------------------------------------------------
// <copyright file="SconceStrings.cs" company="Microsoft">
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
// Contains all of the managed resource strings specific to the Sconce project.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections.Specialized;

	/// <summary>
	/// Contains all of the managed resource strings specific to the Sconce project.
	/// </summary>
	public sealed class SconceStrings
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static ManagedResourceManager manager = new ManagedResourceManager();
		private static StringDictionary stringIdMap;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Prevent direct instantiation of this class.
		/// </summary>
		private SconceStrings()
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
		/// Gets the error string when the user tries to enter a blank name for a hierarchy node.
		/// </summary>
		/// <value>You must enter a name.</value>
		public static string ErrorBlankCaption
		{
			get { return GetString(StringId.ErrorBlankCaption); }
		}

		/// <summary>
		/// Gets the error string when attempting to rename a file or folder with a file name that is empty or has an initial '.' character.
		/// </summary>
		public static string ErrorFileNameCannotContainLeadingPeriod
		{
			get { return GetString(StringId.ErrorFileNameCannotContainLeadingPeriod); }
		}

		/// <summary>
		/// Gets the error string when attempting to rename a file to an invalid file name.
		/// </summary>
		public static string ErrorInvalidFileOrFolderName
		{
			get { return GetString(StringId.ErrorInvalidFileOrFolderName); }
		}

		/// <summary>
		/// Gets the name used in the Property window for a file node.
		/// </summary>
		public static string FilePropertiesClassName
		{
			get { return GetString(StringId.FilePropertiesClassName); }
		}

		/// <summary>
		/// Gets the name used in the Property window for a folder node.
		/// </summary>
		public static string FolderPropertiesClassName
		{
			get { return GetString(StringId.FolderPropertiesClassName); }
		}

		/// <summary>
		/// Gets the text node underneath the project when the project can't be loaded.
		/// </summary>
		public static string ProjectPropertiesClassName
		{
			get { return GetString(StringId.ProjectPropertiesClassName); }
		}

		/// <summary>
		/// Gets a localized string like "Project File".
		/// </summary>
		public static string ProjectPropertiesProjectFile
		{
			get { return GetString(StringId.ProjectPropertiesProjectFile); }
		}

		/// <summary>
		/// Gets a localized string like "The project file cannot be loaded."
		/// </summary>
		/// <value>The project file cannot be loaded.</value>
		public static string ProjectUnavailable
		{
			get { return GetString(StringId.ProjectUnavailable); }
		}

		/// <summary>
		/// Gets the confirmation message when a user is trying to change the extension of a file.
		/// </summary>
		public static string PromptChangeExtension
		{
			get { return GetString(StringId.PromptChangeExtension); }
		}

		/// <summary>
		/// Gets the name of the General property page.
		/// </summary>
		public static string PropertyPageGeneral
		{
			get { return GetString(StringId.PropertyPageGeneral); }
		}

		/// <summary>
		/// Gets the name used in the Property window for a reference file node.
		/// </summary>
		public static string ReferenceFilePropertiesClassName
		{
			get { return GetString(StringId.ReferenceFilePropertiesClassName); }
		}

		/// <summary>
		/// Gets a localized string like "References".
		/// </summary>
		public static string ReferenceFolderCaption
		{
			get { return GetString(StringId.ReferenceFolderCaption); }
		}
		#endregion

		#region Enums
		//==========================================================================================
		// Enums
		//==========================================================================================

		/// <summary>
		/// These are all of the strings in the sconce.dll.
		/// </summary>
		public enum StringId
		{
			AddReferenceDialogFilter,
			AddReferenceDialogTitle,
			BuildSettingsOutputNameDescription,
			BuildSettingsOutputFileDescription,
			BuildSettingsOutputFileDisplayName,
			BuildSettingsOutputNameDisplayName,
			CategoryAdvanced,
			CategoryApplication,
			CategoryMisc,
			CategoryProject,
			ConsultTraceLog,
			ErrorBlankCaption,
			ErrorFileNameCannotContainLeadingPeriod,
			ErrorInvalidFileOrFolderName,
			ErrorItemAlreadyExistsOnDisk,
			ErrorMissingService,
			FileDoesNotExist,
			FilePropertiesClassName,
			FilePropertiesBuildAction,
			FilePropertiesBuildActionDescription,
			FilePropertiesFileName,
			FilePropertiesFileNameDescription,
			FilePropertiesFullPath,
			FilePropertiesFullPathDescription,
			FolderPropertiesClassName,
			FolderPropertiesFolderName,
			FolderPropertiesFolderNameDescription,
			ProjectPropertiesClassName,
			ProjectPropertiesProjectFile,
			ProjectPropertiesProjectFileDescription,
			ProjectPropertiesProjectFolder,
			ProjectPropertiesProjectFolderDescription,
			ProjectUnavailable,
			PromptChangeExtension,
			PropertyPageGeneral,
			ReferenceFilePropertiesClassName,
			ReferenceFilePropertiesName,
			ReferenceFilePropertiesNameDescription,
			ReferenceFilePropertiesPath,
			ReferenceFilePropertiesPathDescription,
			ReferenceFolderCaption,
			UnavailableCaption,
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Gets a localized string like "Please consult the trace log at '{0}' for more information."
		/// </summary>
		/// <param name="traceLogPath">The path to the trace log file.</param>
		/// <returns>A localized string like "Please consult the trace log at '{0}' for more information."</returns>
		public static string ConsultTraceLog(string traceLogPath)
		{
			Tracer.VerifyStringArgument(traceLogPath, "traceLogPath");
			return GetString(StringId.ConsultTraceLog, traceLogPath);
		}

		/// <summary>
		/// Gets a localized string like "A file or folder with the name '{0}' already exists on disk at this location. Please choose another name."
		/// </summary>
		/// <param name="fileOrFolderName">The file or folder name that already exists.</param>
		/// <returns>A localized string like "A file or folder with the name '{0}' already exists on disk at this location. Please choose another name."</returns>
		public static string ErrorItemAlreadyExistsOnDisk(string fileOrFolderName)
		{
			Tracer.VerifyStringArgument(fileOrFolderName, "fileOrFolderName");
			return GetString(StringId.ErrorItemAlreadyExistsOnDisk, fileOrFolderName);
		}

		/// <summary>
		/// Gets a localized string like "The package requires that service '{0}' be installed.  Ensure that this service is available by repairing your Visual Studio installation."
		/// </summary>
		/// <param name="serviceName"></param>
		/// <returns></returns>
		public static string ErrorMissingService(string serviceName)
		{
			Tracer.VerifyStringArgument(serviceName, "serviceName");
			return GetString(StringId.ErrorMissingService, serviceName);
		}

		/// <summary>
		/// Gets a localized string like "The file '{0}' does not exist."
		/// </summary>
		/// <param name="fileName">The file name that doesn't exist.</param>
		/// <returns>A localized string like "The file '{0}' does not exist."</returns>
		public static string FileDoesNotExist(string fileName)
		{
			Tracer.VerifyStringArgument(fileName, "fileName");
			return GetString(StringId.FileDoesNotExist, fileName);
		}

		/// <summary>
		/// Gets a localized string like "{0} (unavailable)".
		/// </summary>
		/// <param name="caption">The caption of the unavailable node.</param>
		/// <returns>A localized string like "{0} (unavailable)".</returns>
		public static string UnavailableCaption(string caption)
		{
			Tracer.VerifyStringArgument(caption, "caption");
			return GetString(StringId.UnavailableCaption, caption);
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