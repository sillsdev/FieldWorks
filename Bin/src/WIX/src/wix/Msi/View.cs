//-------------------------------------------------------------------------------------------------
// <copyright file="View.cs" company="Microsoft">
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
// Wrapper for a database view.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
	using System;
	using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

	/// <summary>
	/// Enumeration of different modify modes.
	/// </summary>
	public enum ModifyView
	{
		/// <summary>
		/// Writes current data in the cursor to a table row. Updates record if the primary
		/// keys match an existing row and inserts if they do not match. Fails with a read-only
		/// database. This mode cannot be used with a view containing joins.
		/// </summary>
		Assign = MsiInterop.MSIMODIFYASSIGN,
		/// <summary>
		/// Remove a row from the table. You must first call the Fetch function with the same
		/// record. Fails if the row has been deleted. Works only with read-write records. This
		/// mode cannot be used with a view containing joins.
		/// </summary>
		Delete = MsiInterop.MSIMODIFYDELETE,
		/// <summary>
		/// Inserts a record. Fails if a row with the same primary keys exists. Fails with a read-only
		/// database. This mode cannot be used with a view containing joins.
		/// </summary>
		Insert = MsiInterop.MSIMODIFYINSERT,
		/// <summary>
		/// Inserts a temporary record. The information is not persistent. Fails if a row with the
		/// same primary key exists. Works only with read-write records. This mode cannot be
		/// used with a view containing joins.
		/// </summary>
		InsertTemporary = MsiInterop.MSIMODIFYINSERTTEMPORARY,
		/// <summary>
		/// Inserts or validates a record in a table. Inserts if primary keys do not match any row
		/// and validates if there is a match. Fails if the record does not match the data in
		/// the table. Fails if there is a record with a duplicate key that is not identical.
		/// Works only with read-write records. This mode cannot be used with a view containing joins.
		/// </summary>
		Merge = MsiInterop.MSIMODIFYMERGE,
		/// <summary>
		/// Refreshes the information in the record. Must first call Fetch with the
		/// same record. Fails for a deleted row. Works with read-write and read-only records.
		/// </summary>
		Refresh = MsiInterop.MSIMODIFYREFRESH,
		/// <summary>
		/// Updates or deletes and inserts a record into a table. Must first call Fetch with
		/// the same record. Updates record if the primary keys are unchanged. Deletes old row and
		/// inserts new if primary keys have changed. Fails with a read-only database. This mode cannot
		/// be used with a view containing joins.
		/// </summary>
		Replace = MsiInterop.MSIMODIFYREPLACE,
		/// <summary>
		/// Refreshes the information in the supplied record without changing the position in the
		/// result set and without affecting subsequent fetch operations. The record may then
		/// be used for subsequent Update, Delete, and Refresh. All primary key columns of the
		/// table must be in the query and the record must have at least as many fields as the
		/// query. Seek cannot be used with multi-table queries. This mode cannot be used with
		/// a view containing joins. See also the remarks.
		/// </summary>
		Seek = MsiInterop.MSIMODIFYSEEK,
		/// <summary>
		/// Updates an existing record. Non-primary keys only. Must first call Fetch. Fails with a
		/// deleted record. Works only with read-write records.
		/// </summary>
		Update = MsiInterop.MSIMODIFYUPDATE
	}

	/// <summary>
	/// Wrapper class for MSI API views.
	/// </summary>
	public class View : MsiHandle
	{
		/// <summary>
		/// Constructor that creates a view given a database handle and a query.
		/// </summary>
		/// <param name="db">Handle to the database to run the query on.</param>
		/// <param name="query">Query to be executed.</param>
		public View(Database db, string query)
		{
			if (IntPtr.Zero != this.handle)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}

			uint error = MsiInterop.MsiDatabaseOpenView(db.InternalHandle, query, out this.handle);
			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException(String.Concat("Failed to create view with query: ", query), (int)error);
			}
		}

		/// <summary>
		/// Executes a view with no customizable parameters.
		/// </summary>
		public void Execute()
		{
			this.Execute(null);
		}

		/// <summary>
		/// Executes a query substituing the values from the records into the customizable parameters
		/// in the view.
		/// </summary>
		/// <param name="record">Record containing parameters to be substituded into the view.</param>
		public void Execute(Record record)
		{
			if (IntPtr.Zero == this.handle)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}

			uint error = MsiInterop.MsiViewExecute(this.handle, null == record ? IntPtr.Zero : record.InternalHandle);
			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to execute view", (int)error);
			}
		}

		/// <summary>
		/// Fetches the next row in the view.
		/// </summary>
		/// <param name="record">Record for recieving the data in the next row of the view.</param>
		/// <returns>Returns true if there was another record to be fetched and false if there wasn't.</returns>
		public bool Fetch(out Record record)
		{
			if (IntPtr.Zero == this.handle)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}

			IntPtr recordHandle;
			uint error = MsiInterop.MsiViewFetch(this.handle, out recordHandle);
			if (259 == error)
			{
				record = null;
				return false;
			}
			else if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to fetch record from view", (int)error);
			}

			record = new Record(recordHandle);
			return true;
		}

		/// <summary>
		/// Updates a fetched record.
		/// </summary>
		/// <param name="type">Type of modification mode.</param>
		/// <param name="record">Record to be modified.</param>
		public void Modify(ModifyView type, Record record)
		{
			if (IntPtr.Zero == this.handle)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}

			uint error = MsiInterop.MsiViewModify(handle, Convert.ToInt32(type), record.InternalHandle);
			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to modify view", (int)error);
			}
		}

		/// <summary>
		/// Returns a record containing column names or definitions.
		/// </summary>
		/// <param name="columnType">Specifies a flag indicating what type of information is needed. Either MSICOLINFO_NAMES or MSICOLINFO_TYPES.</param>
		/// <param name="record">Record to get column info about.</param>
		public void GetColumnInfo(int columnType, out Record record)
		{
			if (IntPtr.Zero == handle)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}

			IntPtr recordHandle;
			uint error = MsiInterop.MsiViewGetColumnInfo(this.handle, columnType, out recordHandle);
			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to get column info on view", (int)error);
			}
			record = new Record(recordHandle);
		}
	}
}
