// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FieldDescription.cs
// Responsibility: Randy Regnier
// Last reviewed:
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Runtime.InteropServices; // needed for Marshal

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Class that represents one row in the Field$ table.
	/// </summary>
	public class FieldDescription
	{
		#region Data members
/*
Field$
	[Id] [int] NOT NULL , ********
	[Type] [int] NOT NULL , ********
	[Class] [int] NOT NULL , ********
	[DstCls] [int],
	[Name] [nvarchar] (100) NOT NULL , ********
	[Custom] [tinyint] NOT NULL, ********
	[CustomId] [uniqueidentifier],
	[Min] [bigint],
	[Max] [bigint],
	[Big] [bit],
	[UserLabel] [nvarchar] (100),
	[HelpString] [nvarchar] (100),
	[ListRootId] [int],
	[WsSelector] [int],
	[XmlUI] [ntext],
bigint - Integer (whole number) data from -2^63 (-9223372036854775808) through 2^63-1 (9223372036854775807). Storage size is 8 bytes.
int - Integer (whole number) data from -2^31 (-2,147,483,648) through 2^31 - 1 (2,147,483,647). Storage size is 4 bytes. The SQL-92 synonym for int is integer.
smallint - Integer data from -2^15 (-32,768) through 2^15 - 1 (32,767). Storage size is 2 bytes.
tinyint - Integer data from 0 through 255. Storage size is 1 byte.
*/
		private int m_id;
		private int m_type;
		private int m_class;
		private int m_dstCls;
		private string m_name; // max length is 100.
		private byte m_custom;
		private Guid m_customId = Guid.Empty;
		private long m_min;
		private long m_max;
		private bool m_big;
		private string m_userlabel; // max length is 100.
		private string m_helpString; // max length is 100.
		private int m_listRootId;
		private int m_wsSelector;
		private string m_xmlUI;
		private bool m_isDirty = false;
		private bool m_doDelete = false;
		private FdoCache m_cache;

		#endregion Data members

		#region Properties

		/// <summary>
		///
		/// </summary>
		public bool IsCustomField
		{
			get { return (m_custom != 0); }
		}

		/// <summary>
		///
		/// </summary>
		public bool IsDirty
		{
			get { return m_isDirty; }
		}

		/// <summary>
		///
		/// </summary>
		public bool IsInstalled
		{
			get { return m_id > 0; }
		}

		/// <summary>
		/// Mark a row for deletion from the database
		/// </summary>
		/// <exception cref="ApplicationException">
		/// Thrown if the row is a builtin field.
		/// </exception>
		public bool MarkForDeletion
		{
			get { return m_doDelete; }
			set
			{
				if (!IsCustomField)
					throw new ApplicationException("Builtin fields cannot be deleted.");
				m_doDelete = value;
				m_isDirty = true;
			}
		}

		/// <summary>
		/// Id of the field description.
		/// </summary>
		public int Id
		{
			get { return m_id; }
		}

		/// <summary>
		/// The type of field.
		/// </summary>
		public int Type
		{
			get { return m_type; }
			set
			{
				CheckNotNull(value);
				if (m_type != value)
					m_isDirty = true;
				m_type = value;
			}
		}

		/// <summary>
		/// Class of the field.
		/// </summary>
		public int Class
		{
			get { return m_class; }
			set
			{
				CheckNotNull(value);
				if (m_class != value)
					m_isDirty = true;
				m_class = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int DstCls
		{
			get { return m_dstCls; }
			set
			{
				if (m_dstCls != value)
					m_isDirty = true;
				m_dstCls = value;
			}
		}

		/// <summary>
		/// Field name, now read only as it's created by the db.
		/// </summary>
		public string Name
		{
			get { return m_name; }
		}

		/// <summary>
		///
		/// </summary>
		public byte Custom
		{
			get { return m_custom; }
		}

		/// <summary>
		/// Guid for custom field.
		/// </summary>
		public Guid CustomId
		{
			get { return m_customId; }
		}

		/// <summary>
		///
		/// </summary>
		public long Min
		{
			get { return m_min; }
			set
			{
				if (m_min != value)
					m_isDirty = true;
				m_min = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public long Max
		{
			get { return m_max; }
			set
			{
				if (m_max != value)
					m_isDirty = true;
				m_max = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool Big
		{
			get { return m_big; }
			set
			{
				if (m_big != value)
					m_isDirty = true;
				m_big = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public string Userlabel
		{
			get { return m_userlabel; }
			set
			{
				if (m_userlabel != value)
					m_isDirty = true;
				if (value.Length > 100)
					m_userlabel = value.Substring(0, 100);
				else
					m_userlabel = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public string HelpString
		{
			get { return m_helpString; }
			set
			{
				if (m_helpString != value)
					m_isDirty = true;
				if (value.Length > 100)
					m_helpString = value.Substring(0, 100);
				else
					m_helpString = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int ListRootId
		{
			get { return m_listRootId; }
			set
			{
				if (m_listRootId != value)
					m_isDirty = true;
				m_listRootId = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int WsSelector
		{
			get { return m_wsSelector; }
			set
			{
				if (m_wsSelector != value)
					m_isDirty = true;
				m_wsSelector = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public string XmlUI
		{
			get { return m_xmlUI; }
			set
			{
				if (m_xmlUI != value)
					m_isDirty = true;
				m_xmlUI = value;
			}
		}

		#endregion Properties

		#region Construction

		/// <summary>
		/// Constructor
		/// </summary>
		public FieldDescription(FdoCache cache)
		{
			Debug.Assert(cache != null);

			m_id = 0;
			m_custom = 1;
			m_cache = cache;
			//m_customId = Guid.NewGuid();
		}

		#endregion Construction

		#region Methods

		/// <summary>
		/// Update modified row or add a new one, but only if it is a custom field.
		/// </summary>
		public void UpdateDatabase()
		{
			// We do nothing for builtin fields or rows that have not been modified.
			if (m_isDirty && IsCustomField)
			{
				String sqlCommand;
				IOleDbCommand odc = null;
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				try
				{
					// TODO: Maybe check for required columns for custom fields.
					if (IsInstalled)
					{
						// Update (or delete) existing row.
						if (m_doDelete)
						{
							sqlCommand = string.Format("DELETE FROM Field$ WITH (SERIALIZABLE) WHERE Id={0}",
								m_id);
							// TODO KenZ(RandyR): What should happen to the data, if any exists?
						}
						else
						{
							// Only update changeable fields.
							// Id, Type, Class, Name, Custom, and CustomId are not changeable by
							// the user, once they have been placed in the DB, so we won't
							// update them here no matter what.
							uint index = 1; // Current parameter index
							sqlCommand = string.Format("UPDATE Field$ WITH (SERIALIZABLE)" +
								" SET Min={0}, Max={1}, Big={2}, UserLabel={3}," +
								" HelpString={4}, ListRootId={5}, WsSelector={6}, XmlUI={7}" +
								" WHERE Id={8}",
								AsSql(m_min), AsSql(m_max), AsSql(m_big), AsSql(m_userlabel, odc, ref index),
								AsSql(m_helpString, odc, ref index), AsSql(m_listRootId), AsWSSql(m_wsSelector),
								AsSql(m_xmlUI, odc, ref index), m_id);
						}
						odc.ExecCommand(sqlCommand, (int)SqlStmtType.knSqlStmtNoResults);
					}
					else
					{
						// ================ Added Start ===========================
						// First use a stored procedure to determine what the Name field should
						// be, passing in the UserLabel for possible/future use.
						string sqlQuery = 	"declare @res nvarchar(400)"
							+ " exec GenerateCustomName @res OUTPUT"
							+ " select @res";
						uint cbSpaceTaken;
						bool fMoreRows;
						bool fIsNull;
						using (ArrayPtr rgchUsername = MarshalEx.ArrayToNative(100, typeof(char)))
						{
							odc.ExecCommand(sqlQuery,
								(int)SqlStmtType.knSqlStmtStoredProcedure);
							odc.GetRowset(0);
							odc.NextRow(out fMoreRows);
							// odc.GetColValue calls are all 1-based... (post error comment)
							odc.GetColValue(1, rgchUsername, rgchUsername.Size, out cbSpaceTaken, out fIsNull,
								0);
							byte[] rgbTemp = (byte[])MarshalEx.NativeToArray(rgchUsername,
								(int)cbSpaceTaken, typeof(byte));
							m_name = Encoding.Unicode.GetString(rgbTemp);
						}
						// ================ Added End ===========================

						// Note: There is no need to worry about deletion, as this one isn't in
						// the DB.  Make new row in DB.
						// Use SP to create the new one: .
						uint uintSize = (uint)Marshal.SizeOf(typeof(uint));
						uint index = 1;
						odc.SetParameter(index++, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISOUTPUT,
							null, (ushort)DBTYPEENUM.DBTYPE_I4,
							new uint[1] {0}, uintSize);
						sqlCommand = string.Format("exec AddCustomField$ ? output, {0}, {1}, {2}, " +
							"{3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}",
							AsSql(m_name, odc, ref index), m_type, m_class, AsSql(m_dstCls), AsSql(m_min),
							AsSql(m_max), AsSql(m_big), AsSql(m_userlabel, odc, ref index), AsSql(m_helpString, odc, ref index),
							AsSql(m_listRootId), AsWSSql(m_wsSelector), AsSql(m_xmlUI, odc, ref index));
						using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
						{
							odc.ExecCommand(sqlCommand, (int)SqlStmtType.knSqlStmtStoredProcedure);
							odc.GetParameter(1, rgHvo, uintSize, out fIsNull);
							m_id = (int)(((uint[])MarshalEx.NativeToArray(rgHvo, 1,
								typeof(uint)))[0]);
						}
					}
				}
				finally
				{
					DbOps.ShutdownODC(ref odc);
				}
				// Before continuing, we have to close any open transactions (essentially the
				// File/Save operation).  Otherwise, lots of things can break or timeout...
				if (m_cache.DatabaseAccessor.IsTransactionOpen())
					m_cache.DatabaseAccessor.CommitTrans();
				if (m_cache.ActionHandlerAccessor != null)
					m_cache.ActionHandlerAccessor.Commit();
			}
		}

		// Note: There used to be a method here called AsSql(string data) that just put quotes around any data.
		// Putting strings directly into SQL queries is very dangerous, and a user uncovered this bug by trying
		// to create a custom field with an apostrophe.  All string data should be put into the query using
		// SetStringParamter.

		/// <summary>
		/// Used in preparing a string for an SQL command.  Returns "null" if the string passed in is null, returns "?"
		/// if the string isn't null.  The odc handle is used to do a SetStringParameter with the string and index passed in.
		/// </summary>
		/// <param name="data">string to insert into query</param>
		/// <param name="odc">odc handle to use</param>
		/// <param name="index">index to use for the SetStringParameter if the string is not null.  Will be incremented if the string is not null.</param>
		/// <returns></returns>
		private string AsSql(string data, IOleDbCommand odc, ref uint index)
		{
			if (data == null)
				return "null";

			odc.SetStringParameter(index, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
								null, data, (uint)data.Length);

			index++;  // Increment the index by one for next time

			return "?";
		}

		// Enhance CurtisH: Ideally, all of these should be done using SetParameter.  However, for integers,
		// it shouldn't make that much of a difference if they are just directly inserted.

		private string AsWSSql(int data)
		{
			string retval = "null";
			if (data != 0)
				retval = data.ToString();
			return retval;
		}

		private string AsSql(int data)
		{
			string retval = "null";
			if (data > 0)
				retval = data.ToString();
			return retval;
		}

		private string AsSql(long data)
		{
			string retval = "null";
			if (data > 0)
				retval = data.ToString();
			return retval;
		}

		private string AsSql(bool data)
		{
			if (data)
				return "1";
			else
				return "0";
		}

		private void CheckNotNull(object value)
		{
			if (value == null)
				throw new ArgumentNullException("The value for the property cannot be null.");
		}

		#endregion Methods

		#region Static methods

		static Dictionary<FdoCache, List<FieldDescription>> m_fieldDescriptors = new Dictionary<FdoCache, List<FieldDescription>>();

		/// <summary>
		/// Forget anything you know about the specified cache.
		/// </summary>
		/// <param name="cache"></param>
		public static void ClearDataAbout(FdoCache cache)
		{
			m_fieldDescriptors.Remove(cache);
		}

		/// <summary>
		/// Static method that returns an array of FieldDescriptor objects,
		/// where each one represents a row in the Field$ table.
		/// </summary>
		/// <param name="cache">FDO cache to collect the data from.</param>
		/// <returns>A List of FieldDescription objects,
		/// where each object in the array represents a row in the Field$ table.</returns>
		public static List<FieldDescription> FieldDescriptors(FdoCache cache)
		{
			Debug.Assert(cache != null);
			if (m_fieldDescriptors.ContainsKey(cache))
				return m_fieldDescriptors[cache];

			List<FieldDescription> list = new List<FieldDescription>();
			m_fieldDescriptors[cache] = list;

			SqlConnection connection = null;
			SqlDataReader reader = null;
			try
			{
				string sSql = "Server=" + cache.ServerName + "; Database=" + cache.DatabaseName +
					"; User ID=FWDeveloper; Password=careful; Pooling=false;";
				connection = new SqlConnection(sSql);
				connection.Open();
				SqlCommand command = connection.CreateCommand();
				command.CommandText = "select * from Field$";
				reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
				while (reader.Read())
				{
					FieldDescription fd = new FieldDescription(cache);
					for (int i = 0; i < reader.FieldCount; ++i)
					{
						if (!reader.IsDBNull(i))
						{
							switch (reader.GetName(i))
							{
								default:
									throw new Exception("Unrecognized column name.");
								case "Id":
									fd.m_id = reader.GetInt32(i);
									break;
								case "Type":
									fd.m_type = reader.GetInt32(i);
									break;
								case "Class":
									fd.m_class = reader.GetInt32(i);
									break;
								case "DstCls":
									fd.m_dstCls = reader.GetInt32(i);
									break;
								case "Name":
									fd.m_name = reader.GetString(i);
									break;
								case "Custom":
									fd.m_custom = reader.GetByte(i);
									break;
								case "CustomId":
									fd.m_customId = reader.GetGuid(i);
									break;
								case "Min":
									fd.m_min = reader.GetInt64(i);
									break;
								case "Max":
									fd.m_max = reader.GetInt64(i);
									break;
								case "Big":
									fd.m_big = reader.GetBoolean(i);
									break;
								case "UserLabel":
									fd.m_userlabel = reader.GetString(i);
									break;
								case "HelpString":
									fd.m_helpString = reader.GetString(i);
									break;
								case "ListRootId":
									fd.m_listRootId = reader.GetInt32(i);
									break;
								case "WsSelector":
									fd.m_wsSelector = reader.GetInt32(i);
									break;
								case "XmlUI":
									fd.m_xmlUI = reader.GetString(i);
									break;
							}
						}
					}
					list.Add(fd);
				}
			}
			finally
			{
				if (reader != null)
					reader.Close();
				if (connection != null)
					connection.Close();
			}

			return list;
		}

		#endregion Static methods
	}
}
