//-------------------------------------------------------------------------------------------------
// <copyright file="Database.cs" company="Microsoft">
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
// Wrapper for MSI API database functions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
	using System;
	using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

	/// <summary>
	/// Enum of predefined persist modes used when opening a database.
	/// </summary>
	public enum OpenDatabase
	{
		/// <summary>
		/// Open a database read-only, no persistent changes.
		/// </summary>
		ReadOnly = MsiInterop.MSIDBOPENREADONLY,
		/// <summary>
		/// Open a database read/write in transaction mode.
		/// </summary>
		Transact = MsiInterop.MSIDBOPENTRANSACT,
		/// <summary>
		/// Open a database direct read/write without transaction.
		/// </summary>
		Direct = MsiInterop.MSIDBOPENDIRECT,
		/// <summary>
		/// Create a new database, transact mode read/write.
		/// </summary>
		Create = MsiInterop.MSIDBOPENCREATE,
		/// <summary>
		/// Create a new database, direct mode read/write.
		/// </summary>
		CreateDirect = MsiInterop.MSIDBOPENCREATEDIRECT,
	}

	/// <summary>
	/// Wrapper class for managing MSI API database handles.
	/// </summary>
	public class Database : MsiHandle
	{
		/// <summary>
		/// Constructor that opens an MSI database.
		/// </summary>
		/// <param name="path">Path to the database to be opened.</param>
		/// <param name="type">Persist mode to use when opening the database.</param>
		public Database(string path, OpenDatabase type)
		{
			this.Open(path, type);
		}

		/// <summary>
		/// Opens an MSI database.
		/// </summary>
		/// <param name="path">Path to the database to be opened.</param>
		/// <param name="type">Persist mode to use when opening the database.</param>
		public void Open(string path, OpenDatabase type)
		{
			if (IntPtr.Zero != handle)
			{
				throw new ArgumentException("Database already open");
			}

			uint er = MsiInterop.MsiOpenDatabase(path, (int)type, out handle);
			if (110 == er)
			{
				throw new System.IO.IOException(String.Concat("Failed to open Windows Installer database: ", path));
			}
			else if (0 != er)
			{
				throw new System.Runtime.InteropServices.ExternalException(String.Format("Failed to open database: {0}", path), (int)er);
			}
		}

		/// <summary>
		/// Closes the database without commiting changes.
		/// </summary>
		public override void Close()
		{
			this.Close(false);
		}

		/// <summary>
		/// Closes the database.
		/// </summary>
		/// <param name="commit">Specifies whether or not to commit changes before closing.</param>
		public void Close(bool commit)
		{
			if (commit)
			{
				this.Commit();
			}

			base.Close();
		}

		/// <summary>
		/// Commits changes made to the database.
		/// </summary>
		public void Commit()
		{
			uint er = MsiInterop.MsiDatabaseCommit(handle);
			if (0 != er)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to Commit database", (int)er);
			}
		}

		/// <summary>
		/// Imports an installer text archive table (idt file) into an open database.
		/// </summary>
		/// <param name="folderPath">Specifies the path to the folder containing archive files.</param>
		/// <param name="fileName">Specifies the name of the file to import.</param>
		public void Import(string folderPath, string fileName)
		{
			if (IntPtr.Zero == handle)
			{
				throw new ArgumentException("Invalid handle");
			}

			uint er = MsiInterop.MsiDatabaseImport(handle, folderPath, fileName);
			if (1627 == er)
			{
				throw new System.Configuration.ConfigurationException("Invalid IDT file", String.Concat(folderPath, "\\", fileName), 0);
			}
			else if (0 != er)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to Import file", (int)er);
			}
		}

		/// <summary>
		/// Exports an installer table from an open database to a text archive file (idt file).
		/// </summary>
		/// <param name="tableName">Specifies the name of the table to export.</param>
		/// <param name="folderPath">Specifies the name of the folder that contains archive files. If null or empty string, uses current directory.</param>
		/// <param name="fileName">Specifies the name of the exported table archive file.</param>
		public void Export(string tableName, string folderPath, string fileName)
		{
			if (IntPtr.Zero == handle)
			{
				throw new ArgumentException("Invalid handle");
			}

			if (null == folderPath || 0 == folderPath.Length)
			{
				folderPath = System.Environment.CurrentDirectory;
			}

			uint er = MsiInterop.MsiDatabaseExport(handle, tableName, folderPath, fileName);
			if (0 != er)
			{
				throw new System.Runtime.InteropServices.ExternalException(String.Format("Failed to Export file: {0}", er), (int)er);
			}
		}

		/// <summary>
		/// Prepares a database query and creates a <see cref="View">View</see> object.
		/// </summary>
		/// <param name="query">Specifies a SQL query string for querying the database.</param>
		/// <returns>A view object is returned if the query was successful.</returns>
		public View OpenView(string query)
		{
			return new View(this, query);
		}

		/// <summary>
		/// Prepares and executes a database query and creates a <see cref="View">View</see> object.
		/// </summary>
		/// <param name="query">Specifies a SQL query string for querying the database.</param>
		/// <returns>A view object is returned if the query was successful.</returns>
		public View OpenExecuteView(string query)
		{
			View view = new View(this, query);

			view.Execute();
			return view;
		}

		/// <summary>
		/// Verifies the existence or absence of a table.
		/// </summary>
		/// <param name="tableName">Table name to to verify the existence of.</param>
		/// <returns>Returns true if the table exists, false if it does not.</returns>
		public bool TableExists(string tableName)
		{
			int result = MsiInterop.MsiDatabaseIsTablePersistent(handle, tableName);
			return MsiInterop.MSICONDITIONTRUE == result;
		}

		/// <summary>
		/// Returns a <see cref="Record">Record</see> containing the names of all the primary
		/// key columns for a specified table.
		/// </summary>
		/// <param name="tableName">Specifies the name of the table from which to obtain
		/// primary key names.</param>
		/// <returns>Returns a <see cref="Record">Record</see> containing the names of all the
		/// primary key columns for a specified table.</returns>
		public Record PrimaryKeys(string tableName)
		{
			IntPtr recordHandle;
			uint er = MsiInterop.MsiDatabaseGetPrimaryKeys(handle, tableName, out recordHandle);
			if (0 != er)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to get primary keys", (int)er);
			}
			return new Record(recordHandle);
		}
	}
}
