//-------------------------------------------------------------------------------------------------
// <copyright file="MsmMerge.cs" company="Microsoft">
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
// Merge merge modules into an MSI file.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Runtime.InteropServices;

	/// <summary>
	/// IMsmMerge2 interface.
	/// </summary>
	[Guid("351A72AB-21CB-47ab-B7AA-C4D7B02EA305"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
	interface IMsmMerge2
	{
		/// <summary>
		/// The OpenDatabase method of the Merge object opens a Windows Installer installation
		/// database, located at a specified path, that is to be merged with a module.
		/// </summary>
		/// <param name="databasePath">Path to the database being opened.</param>
		void OpenDatabase([In, MarshalAs(UnmanagedType.BStr)] string databasePath);

		/// <summary>
		/// The OpenModule method of the Merge object opens a Windows Installer merge module
		/// in read-only mode. A module must be opened before it can be merged with an installation database.
		/// </summary>
		/// <param name="modulePath">Fully qualified file name pointing to a merge module.</param>
		/// <param name="language">A valid language identifier (LANGID).</param>
		void OpenModule([In, MarshalAs(UnmanagedType.BStr)] string modulePath, [In] int language);

		/// <summary>
		/// The CloseDatabase method of the Merge object closes the currently open Windows Installer database.
		/// </summary>
		/// <param name="commit">true if changes should be saved, false otherwise.</param>
		void CloseDatabase([In] bool commit);

		/// <summary>
		/// The CloseModule method of the Merge object closes the currently open Windows Installer merge module.
		/// </summary>
		void CloseModule();

		/// <summary>
		/// The OpenLog method of the Merge object opens a log file that receives progress and error messages.
		/// If the log file already exists, the installer appends new messages. If the log file does not exist,
		/// the installer creates a log file.
		/// </summary>
		/// <param name="logPath">Fully qualified filename pointing to a file to open or create.</param>
		void OpenLog([In, MarshalAs(UnmanagedType.BStr)] string logPath);

		/// <summary>
		/// The CloseLog method of the Merge object closes the current log file.
		/// </summary>
		void CloseLog();

		/// <summary>
		/// The Log method of the Merge object writes a text string to the currently open log file.
		/// </summary>
		/// <param name="message">The text string to write to the log file.</param>
		void Log([In, MarshalAs(UnmanagedType.BStr)] string message);

		/// <summary>
		/// The get_Errors function implements the Errors property of the Merge object.
		/// This function retrieves the current collection of errors.
		/// </summary>
		/// <param name="errors">Pointer to a memory location containing another pointer to an IMsmErrors interface.</param>
		void get_Errors([Out, MarshalAs(UnmanagedType.Interface)] out IMsmErrors errors);

		/// <summary>
		/// The get_Dependencies function implements the Dependencies property
		/// (Merge object) of the Merge object.
		/// </summary>
		/// <param name="o">Pointer to a memory location to be filled with a pointer to an
		/// IMsmDependencies interface which provides access to a collection of unsatisfied
		/// dependencies for the current database. If there is an error, the memory location
		/// pointed to by Dependencies will be set to NULL.</param>
		void get_Dependencies([Out, MarshalAs(UnmanagedType.Interface)] out object o);

		/// <summary>
		/// The Merge method of the Merge object executes a merge of the current database and current
		/// module. The merge attaches the components in the module to the feature identified by Feature.
		/// The root of the module's directory tree is redirected to the location given by RedirectDir.
		/// </summary>
		/// <param name="feature">The name of a feature in the database.</param>
		/// <param name="directory">The key of an entry in the Directory table of the database.
		/// This parameter may be NULL or an empty string.</param>
		void Merge([In, MarshalAs(UnmanagedType.BStr)] string feature, [In, MarshalAs(UnmanagedType.BStr)] string directory);

		/// <summary>
		/// The Connect method of the Merge object connects a module to an additional feature.
		/// The module must have already been merged into the database or will be merged into the database.
		/// The feature must exist before calling this function.
		/// </summary>
		/// <param name="feature">The name of a feature already existing in the database.</param>
		void Connect([In, MarshalAs(UnmanagedType.BStr)] string feature);

		/// <summary>
		/// The ExtractCAB method of the Merge object extracts the embedded .cab file from a module and
		/// saves it as the specified file. The installer creates this file if it does not already exist
		/// and overwritten if it does exist.
		/// </summary>
		/// <param name="path">The fully qualified destination file.</param>
		void ExtractCAB([In, MarshalAs(UnmanagedType.BStr)] string path);

		/// <summary>
		/// The ExtractFiles method of the Merge object extracts the embedded .cab file from a module
		/// and then writes those files to the destination directory.
		/// </summary>
		/// <param name="path">The fully qualified destination directory.</param>
		void ExtractFiles([In, MarshalAs(UnmanagedType.BStr)] string path);

		/// <summary>
		/// The MergeEx method of the Merge object is equivalent to the Merge function, except that it
		/// takes an extra argument.  The Merge method executes a merge of the current database and
		/// current module. The merge attaches the components in the module to the feature identified
		/// by Feature. The root of the module's directory tree is redirected to the location given by RedirectDir.
		/// </summary>
		/// <param name="feature">The name of a feature in the database.</param>
		/// <param name="directory">The key of an entry in the Directory table of the database. This parameter may
		/// be NULL or an empty string.</param>
		/// <param name="o">The pConfiguration argument is an interface implemented by the client. The argument may
		/// be NULL. The presence of this argument indicates that the client is capable of supporting the configuration
		/// functionality, but does not obligate the client to provide configuration data for any specific configurable item.</param>
		void MergeEx([In, MarshalAs(UnmanagedType.BStr)] string feature, [In, MarshalAs(UnmanagedType.BStr)] string directory, [In, MarshalAs(UnmanagedType.Interface)] object o);

		/// <summary>
		/// The ExtractFilesEx method of the Merge object extracts the embedded .cab file from a module and
		/// then writes those files to the destination directory.
		/// </summary>
		/// <param name="path">The fully qualified destination directory.</param>
		/// <param name="longNames">Set to specify using long file names for path segments and final file names.</param>
		/// <param name="o">This is a list of fully-qualified paths for the files that were successfully extracted.</param>
		void ExtractFilesEx([In, MarshalAs(UnmanagedType.BStr)] string path, [In] bool longNames, [Out, MarshalAs(UnmanagedType.Interface)] out object o);

		/// <summary>
		/// The get_ConfigurableItems function implements the ConfigurableItems property of the Merge object.
		/// This function retrieves the current collection of configurable items.
		/// </summary>
		/// <param name="o">Pointer to a memory location containing another pointer to an IMsmConfigurableItems interface.</param>
		void get_ConfigurableItems([Out, MarshalAs(UnmanagedType.Interface)] out object o);

		/// <summary>
		/// The CreateSourceImage method of the Merge object allows the client to extract the files from a module to
		/// a source image on disk after a merge, taking into account changes to the module that might have been made
		/// during module configuration. The list of files to be extracted is taken from the file table of the module
		/// during the merge process. The list of files consists of every file successfully copied from the file table
		/// of the module to the target database. File table entries that were not copied due to primary key conflicts
		/// with existing rows in the database are not a part of this list. At image creation time, the directory for
		/// each of these files comes from the open (post-merge) database. The path specified in the Path parameter is
		/// the root of the source image for the install. fLongFileNames determines whether or not long file names are
		/// used for both path segments and final file names. The function fails if no database is open, no module is
		/// open, or no merge has been performed.
		/// </summary>
		/// <param name="path">The path of the root of the source image for the install.</param>
		/// <param name="longNames">Determines whether or not long file names are used for both path segments and final file names. </param>
		/// <param name="o">This is a list of fully-qualified paths for the files that were successfully extracted.</param>
		void CreateSourceImage([In, MarshalAs(UnmanagedType.BStr)] string path, [In] bool longNames, [Out, MarshalAs(UnmanagedType.Interface)] out object o);

		/// <summary>
		/// The get_ModuleFiles function implements the ModuleFiles property of the GetFiles object. This function
		/// returns the primary keys in the File table of the currently open module. The primary keys are returned
		/// as a collection of strings. The module must be opened by a call to the OpenModule function before calling get_ModuleFiles.
		/// </summary>
		/// <param name="o">Collection of IMsmStrings that are the primary keys of the File table for the currently open module.</param>
		void get_ModuleFiles([Out, MarshalAs(UnmanagedType.Interface)] out object o);
	}

	/// <summary>
	/// IMsmErrors interface.
	/// </summary>
	[Guid("0ADDA82A-2C26-11D2-AD65-00A0C9AF11A6"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
	interface IMsmErrors
	{
		/// <summary>
		/// Gets an error item by index.
		/// </summary>
		/// <param name="item">Index of the item.</param>
		/// <param name="o">IMsmError being retrieved.</param>
		void get_Item(long item, [Out, MarshalAs(UnmanagedType.Interface)] out object o);

		/// <summary>
		/// Gets the count of IMsmError objects in this collection.
		/// </summary>
		/// <returns>The number of objects in this collection.</returns>
		long get_Count();

		/// <summary>
		/// Gets a new enumerator.
		/// </summary>
		/// <param name="o">Object being retrieved.</param>
		void get_NewEnum([Out, MarshalAs(UnmanagedType.Interface)] out object o);
	}

	/// <summary>
	/// Callback for configurable merge modules.
	/// </summary>
	[ComImport, Guid("AC013209-18A7-4851-8A21-2353443D70A0"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
	interface IMsmConfigureModule
	{
		/// <summary>
		/// Callback to retrieve text data for configurable merge modules.
		/// </summary>
		/// <param name="Name">Name of the data to be retrieved.</param>
		/// <param name="ConfigData">The data corresponding to the name.</param>
		/// <returns>The error code (HRESULT).</returns>
		[PreserveSig]
		int ProvideTextData([In, MarshalAs(UnmanagedType.BStr)] string Name, [MarshalAs(UnmanagedType.BStr)] out string ConfigData);

		/// <summary>
		/// Callback to retrieve integer data for configurable merge modules.
		/// </summary>
		/// <param name="Name">Name of the data to be retrieved.</param>
		/// <param name="ConfigData">The data corresponding to the name.</param>
		/// <returns>The error code (HRESULT).</returns>
		[PreserveSig]
		int ProvideIntegerData([In, MarshalAs(UnmanagedType.BStr)] string Name, out int ConfigData);
	}

	/// <summary>
	/// Merge merge modules into an MSI file.
	/// </summary>
	[ComImport, Guid("F94985D5-29F9-4743-9805-99BC3F35B678")]
	public class MsmMerge
	{
	}

}
