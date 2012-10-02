// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TimeStamp.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// a class to wrap the SqlServer timestamp class, which is not actually a time (as of version 2000),
	/// but rather something that gets incremented.
	/// </summary>
	public class TimeStamp
	{
		protected System.Array m_bytes;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// create an "empty" TimeStamp
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TimeStamp()
		{
			m_bytes = System.Array.CreateInstance(typeof(Byte), 8);
		}

		/// <summary>
		/// create a TimeStamp by asking the database for its most recent TimeStamp.
		/// </summary>
		/// <param name="server"></param>
		/// <param name="database"></param>
		public TimeStamp(SqlConnection connection)
			:this((System.Array)GetScalarFromQuery(connection, "select @@DBTS"))
		{
		}

		/// <summary>
		/// create a TimeStamp from the type returned from SQL server when you ask for a TimeStamp.
		/// </summary>
		/// <param name="stamp"></param>
		public TimeStamp(System.Array stamp)
		{
			m_bytes = (System.Array)stamp.Clone();
		}

		/// <summary>
		/// returns true if this TimeStamp was created with the default Constructor and has not been modified since.
		/// </summary>
		public bool Empty
		{
			get
			{
				return (byte)(m_bytes.GetValue(7))== 0
					&& (byte)(m_bytes.GetValue(6)) == 0
					&& (byte)(m_bytes.GetValue(5)) == 0
					&& (byte)(m_bytes.GetValue(4)) == 0;
			}
		}
		public string Hex
		{
			get
			{
				StringBuilder sb = new StringBuilder(20);
				sb.Append("0x");
				for (int i = 0; i < 8; i++)
					sb.Append(((byte) m_bytes.GetValue(i)).ToString("x2"));
				return sb.ToString();
			}
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="stamp"></param>
		/// <returns></returns>
		public static explicit operator System.Array(TimeStamp stamp)
		{
			return stamp.m_bytes;
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="stamp"></param>
		/// <returns></returns>
		public static explicit operator TimeStamp(System.Array stamp)
		{
			return new TimeStamp(stamp);
		}

		protected static object GetScalarFromQuery(SqlConnection connection, string query)
		{
			object retval = null;
			try
			{
				SqlCommand command = connection.CreateCommand();
				command.CommandText = query;
				retval = command.ExecuteScalar();
			}
			catch (Exception error)
			{
				Debug.Assert(error == null);
			}
			return retval;
		}
	}
}
