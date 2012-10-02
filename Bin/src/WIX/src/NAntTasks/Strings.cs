//--------------------------------------------------------------------------------------------------
// <copyright file="Strings.cs" company="Microsoft">
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
// Base class for all Wix-related NAnt tasks.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.NAntTasks
{
	using System;
	using System.Globalization;
	using System.Resources;

	/// <summary>
	/// Contains properties and methods for retrieving all of the strings in the assembly.
	/// </summary>
	internal sealed class Strings
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static ResourceManager manager;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="Strings"/> class.
		/// </summary>
		private Strings()
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// The exedir attribute is missing or invalid. The PATH environment variable will be used to find the executable.
		/// </summary>
		public static string ExeDirMissing
		{
			get { return GetString("ExeDirMissing"); }
		}

		/// <summary>
		/// Rebuilding: the 'rebuild' attribute is set to true.
		/// </summary>
		public static string RebuildAttributeSetToTrue
		{
			get { return GetString("RebuildAttributeSetToTrue"); }
		}

		/// <summary>
		/// Gets the <see cref="ResourceManager"/> for this assembly.
		/// </summary>
		private static ResourceManager Manager
		{
			get
			{
				if (manager == null)
				{
					Type thisType = typeof(Strings);
					manager = new ResourceManager(thisType.FullName, thisType.Assembly);
				}
				return manager;
			}
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Building {0} files to '{1}'.
		/// </summary>
		/// <param name="fileCount">The number of files that are building.</param>
		/// <param name="targetDirectory">The target directory of the build.</param>
		/// <returns>.</returns>
		public static string BuildingFiles(int fileCount, string targetDirectory)
		{
			return GetString("BuildingFiles", fileCount, targetDirectory);
		}

		/// <summary>
		/// Contents of the response file '{0}'.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <returns>.</returns>
		public static string ContentsOfResponseFile(string fileName)
		{
			return GetString("ContentsOfResponseFile", fileName);
		}

		/// <summary>
		/// Rebuilding: file '{0}' has been updated.
		/// </summary>
		/// <param name="fileName">The name of the file that was updated.</param>
		/// <returns>.</returns>
		public static string FileHasBeenUpdated(string fileName)
		{
			return GetString("FileHasBeenUpdated", fileName);
		}

		/// <summary>
		/// Rebuilding: the output file '{0}' does not exist.
		/// </summary>
		/// <param name="outputFile">The name of the output file.</param>
		/// <returns>.</returns>
		public static string OutputFileDoesNotExist(string outputFile)
		{
			return GetString("OutputFileDoesNotExist", outputFile);
		}

		/// <summary>
		/// Gets an unformatted string from the resource file.
		/// </summary>
		/// <param name="name">The identifier of the string to retrieve.</param>
		/// <returns>An unformatted string from the resource file.</returns>
		private static string GetString(string name)
		{
			return GetString(name, null);
		}

		/// <summary>
		/// Gets a formatted string from the resource file.
		/// </summary>
		/// <param name="name">The identifier of the string to retrieve and format.</param>
		/// <param name="args">The format arguments.</param>
		/// <returns>A formatted string from the resource file.</returns>
		private static string GetString(string name, params object[] args)
		{
			string resourceString = Manager.GetString(name, CultureInfo.CurrentUICulture);

			if (args != null && args.Length > 0)
			{
				resourceString = String.Format(CultureInfo.CurrentCulture, resourceString, args);
			}

			return resourceString;
		}
		#endregion
	}
}