//-------------------------------------------------------------------------------------------------
// <copyright file="SummaryInformation.cs" company="Microsoft">
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
// Summary information for the MSI files.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
	using System;
	using System.Text;
	using System.Runtime.InteropServices;
	using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

	/// <summary>
	/// Summary information for the MSI files.
	/// </summary>
	public class SummaryInformation : MsiHandle
	{
		/// <summary>
		/// Instantiate a new SummaryInformation class from the database.
		/// </summary>
		/// <param name="databaseHandle">Handle to the database.</param>
		public SummaryInformation(IntPtr databaseHandle)
		{
			uint error = MsiInterop.MsiGetSummaryInformation(databaseHandle, null, 20, ref handle);
			if (0 != error)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}
		}

		/// <summary>
		/// Instantiate a new SummaryInformation class from the database.
		/// </summary>
		/// <param name="db">Database to retrieve summary information from.</param>
		public SummaryInformation(Database db)
		{
			if (null == db)
			{
				throw new ArgumentNullException("db");
			}

			uint error = MsiInterop.MsiGetSummaryInformation(db.InternalHandle, null, 20, ref handle);
			if (0 != error)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}
		}

		/// <summary>
		/// Instantiate a new SummaryInformation class from the database.
		/// </summary>
		/// <param name="databasePath">Path to the database to open.</param>
		public SummaryInformation(string databasePath)
		{
			uint error = MsiInterop.MsiGetSummaryInformation(IntPtr.Zero, databasePath, 20, ref handle);
			if (0 != error)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}
		}

		/// <summary>
		/// Variant types in the summary information table.
		/// </summary>
		private enum VT : uint
		{
			/// <summary>Variant has not been assigned.</summary>
			EMPTY    = 0,
			/// <summary>Null variant type.</summary>
			NULL     = 1,
			/// <summary>16-bit integer variant type.</summary>
			I2       = 2,
			/// <summary>32-bit integer variant type.</summary>
			I4       = 3,
			/// <summary>String variant type.</summary>
			LPSTR    = 30,
			/// <summary>Date time (FILETIME, converted to Variant time) variant type.</summary>
			FILETIME = 64,
		}

		/// <summary>
		/// Gets the current number of property values in the summary information object, taking
		/// into account properties that have been added, deleted, or replaced.
		/// </summary>
		/// <returns>The current number of property values in the summary information object.</returns>
		public int GetPropertyCount()
		{
			uint count;
			uint ret = MsiInterop.MsiSummaryInfoGetPropertyCount(handle, out count);
			if (0 != ret)
			{
				throw new ApplicationException("MsiSummaryInfoGetPropertyCount failed.");
			}
			return (int) count;
		}

		/// <summary>
		/// Gets a summary information property.
		/// </summary>
		/// <param name="index">Index of the summary information property.</param>
		/// <returns>The summary information property.</returns>
		public object GetProperty(int index)
		{
			uint dataType;
			StringBuilder stringValue = new StringBuilder("");
			int bufSize = 0;
			int intValue;
			FILETIME timeValue;
			timeValue.dwHighDateTime = 0;
			timeValue.dwLowDateTime = 0;
			uint ret = MsiInterop.MsiSummaryInfoGetProperty(handle, index, out dataType, out intValue, ref timeValue, stringValue, ref bufSize);
			//if(ret != (dataType == (uint) VT.LPSTR ? (uint) MsiInterop.Error.MORE_DATA : 0))
			if (234 == ret)
			{
				stringValue.EnsureCapacity(++bufSize);
				ret = MsiInterop.MsiSummaryInfoGetProperty(handle, index, out dataType, out intValue, ref timeValue, stringValue, ref bufSize);
			}
			if (0 != ret)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}
			switch ((VT)dataType)
			{
				case VT.EMPTY:
				{
					return "";
					//if(type == typeof(DateTime)) return DateTime.MinValue;
					//else if(type == typeof(string)) return "";
					//else if(type == typeof(short)) return (short) 0;
					//else if(type == typeof(int)) return (int) 0;
					//else throw new ArgumentNullException(); // TODO: come up with a real exception to throw
				}

				case VT.LPSTR:
				{
					return stringValue.ToString();
					//if(type == typeof(string)) return stringValue.ToString();
					//else if(type == typeof(short) || type == typeof(int) || type == typeof(DateTime)) throw new InstallerException();
					//else throw new ArgumentNullException();// TODO: come up with a real exception to throw
				}

				case VT.I2:
				case VT.I4:
				{
					return "" + intValue;
					//if(type == typeof(short)) return (short) intValue;
					//else if(type == typeof(int)) return intValue;
					//else if(type == typeof(string)) return "" + intValue;
					//else if(type == typeof(DateTime)) throw new InstallerException();
					//else throw new ArgumentException();
				}

				case VT.FILETIME:
				{
					return timeValue.ToString();
					//if(type == typeof(DateTime)) return DateTime.FromFileTime(timeValue);
					//else if(type == typeof(string)) return "" + DateTime.FromFileTime(timeValue);
					//else if(type == typeof(short) || type == typeof(int)) throw new InstallerException();
					//else throw new ArgumentException();
				}

				default:
				{
					throw new ArgumentNullException(); // TODO: come up with a real exception to throw
				}
			}
		}

		/// <summary>
		/// Sets a summary information property.
		/// </summary>
		/// <param name="index">Index of the summary information property.</param>
		/// <param name="property">Data to set into the property.</param>
		public void SetProperty(int index, object property)
		{
			if (IntPtr.Zero == handle)
			{
				throw new ApplicationException("Handle cannot be null.");
			}

			uint error = 0;
			FILETIME ft;
			ft.dwHighDateTime = 0;
			ft.dwLowDateTime = 0;

			if (11 == index || 12 == index || 13 == index)   // must be a FileTime object
			{
				long l = Common.ToFileUtc(Convert.ToDateTime(property, System.Globalization.CultureInfo.InvariantCulture));
				ft.dwHighDateTime = (int)(l >> 32);
				ft.dwLowDateTime = (int)(l & 0xFFFFFFFF);
				error = MsiInterop.MsiSummaryInfoSetProperty(handle, index, MsiInterop.VTFILETIME, 0, ref ft, null);
			}
			else if (1 == index)
			{
				error = MsiInterop.MsiSummaryInfoSetProperty(handle, index, MsiInterop.VTI2, Convert.ToInt32(property), ref ft, null);
			}
			else if (14 == index || 15 == index || 16 == index || 19 == index)
			{
				error = MsiInterop.MsiSummaryInfoSetProperty(handle, index, MsiInterop.VTI4, Convert.ToInt32(property), ref ft, null);
			}
			else   // must be a string
			{
				error = MsiInterop.MsiSummaryInfoSetProperty(handle, index, MsiInterop.VTLPWSTR, 0, ref ft, (string)property);
			}

			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException(String.Concat("Failed to set summary information property: ", index), (int)error);
			}
		}

		/// <summary>
		/// Close the handle to the summary information.
		/// </summary>
		public override void Close()
		{
			this.Close(false);
		}

		/// <summary>
		/// Close the handle to the summary information.
		/// </summary>
		/// <param name="commit">True if the summary information should be committed.</param>
		public void Close(bool commit)
		{
			if (commit)
			{
				this.Persist();
			}

			base.Close();
		}

		/// <summary>
		/// Persist the summary information back to the database.
		/// </summary>
		public void Persist()
		{
			uint error = MsiInterop.MsiSummaryInfoPersist(handle);
			if (0 != error)
			{
				throw new System.Runtime.InteropServices.ExternalException("Failed to persist summary information.", (int)error);
			}
		}
	}
}
