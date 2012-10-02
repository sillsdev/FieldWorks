//-------------------------------------------------------------------------------------------------
// <copyright file="Record.cs" company="Microsoft">
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
// Wrapper class around msi.dll interop for a record.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
	using System;
	using System.Text;
	using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

	/// <summary>
	/// Wrapper class around msi.dll interop for a record.
	/// </summary>
	public class Record : MsiHandle
	{
		/// <summary>
		/// Creates a record with the specified number of fields.
		/// </summary>
		/// <param name="fieldCount">Number of fields in record.</param>
		public Record(int fieldCount)
		{
			this.handle = MsiInterop.MsiCreateRecord(fieldCount);
			if (IntPtr.Zero == this.handle)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to create new record");
			}
		}

		/// <summary>
		/// Creates a record from a handle.
		/// </summary>
		/// <param name="handle">Handle to create record from.</param>
		public Record(IntPtr handle)
		{
			this.handle = handle;
		}

		/// <summary>
		/// Gets a string value at specified location.
		/// </summary>
		/// <param name="field">Index into record to get string.</param>
		public string this[int field]
		{
			get { return this.GetString(field); }
			set { this.SetString(field, (string)value); }
		}

		/// <summary>
		/// Determines if the value is null at the specified location.
		/// </summary>
		/// <param name="field">Index into record of the field to query.</param>
		/// <returns>true if the value is null, false otherwise.</returns>
		public bool IsNull(int field)
		{
			uint ret = MsiInterop.MsiRecordIsNull(handle, field);

			switch (ret)
			{
				case 0:
					return false;
				case 1:
					return true;
				default:
					throw new System.Runtime.InteropServices.ExternalException("Failed to determine if the field was null.", (int)ret);
			}
		}

		/// <summary>
		/// Gets integer value at specified location.
		/// </summary>
		/// <param name="field">Index into record to get integer</param>
		/// <returns>Integer value</returns>
		public int GetInteger(int field)
		{
			return MsiInterop.MsiRecordGetInteger(handle, field);
		}

		/// <summary>
		/// Sets integer value at specified location.
		/// </summary>
		/// <param name="field">Index into record to set integer.</param>
		/// <param name="value">Value to set into record.</param>
		public void SetInteger(int field, int value)
		{
			uint error = MsiInterop.MsiRecordSetInteger(this.handle, field, value);
			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to set integer into record", (int)error);
			}
		}

		/// <summary>
		/// Gets string value at specified location.
		/// </summary>
		/// <param name="field">Index into record to get string.</param>
		/// <returns>String value</returns>
		public string GetString(int field)
		{
			int bufferSize = 255;
			StringBuilder buffer = new StringBuilder(bufferSize);
			uint error = MsiInterop.MsiRecordGetString(this.handle, field, buffer, ref bufferSize);
			if (234 == error)
			{
				buffer.EnsureCapacity(++bufferSize);
				error = MsiInterop.MsiRecordGetString(this.handle, field, buffer, ref bufferSize);
			}

			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to get string from record", (int)error);
			}
			return buffer.ToString();
		}

		/// <summary>
		/// Set string value at specified location
		/// </summary>
		/// <param name="field">Index into record to set string.</param>
		/// <param name="value">Value to set into record</param>
		public void SetString(int field, string value)
		{
			uint error = MsiInterop.MsiRecordSetString(handle, field, value);
			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to set string into record", (int)error);
			}
		}

		/// <summary>
		/// Get stream at specified location.
		/// </summary>
		/// <param name="field">Index into record to get stream.</param>
		/// <param name="buffer">buffer to receive bytes from stream.</param>
		/// <param name="requestedBufferSize">Buffer size to read.</param>
		/// <returns>Stream read into string.</returns>
		public int GetStream(int field, byte[] buffer, int requestedBufferSize)
		{
			int bufferSize = 255;
			if (requestedBufferSize > 0)
			{
				bufferSize = requestedBufferSize;
			}

			uint error = MsiInterop.MsiRecordReadStream(this.handle, field, buffer, ref bufferSize);
			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("failed to read stream from record", (int)error);
			}
			return bufferSize;
		}

		/// <summary>
		/// Sets a stream at a specified location.
		/// </summary>
		/// <param name="field">Index into record to set stream.</param>
		/// <param name="path">Path to file to read into stream.</param>
		public void SetStream(int field, string path)
		{
			uint error = MsiInterop.MsiRecordSetStream(this.handle, field, path);
			if (161 == error)
			{
				throw new System.IO.FileNotFoundException("File not found, cannot set stream into record", path, null);
			}
			else if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to set stream into record", (int)error);
			}
		}

		/// <summary>
		/// Gets the number of fields in record.
		/// </summary>
		/// <returns>Count of fields in record.</returns>
		public int GetFieldCount()
		{
			int size = MsiInterop.MsiRecordGetFieldCount(this.handle);
			if (0 > size)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to get field count");
			}

			return size;
		}
	}
}
