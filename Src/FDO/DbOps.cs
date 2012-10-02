using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices; // needed for Marshal

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Class has a collection of static methods for database-related stuff. I (JohnT) moved them here
	/// from their original location in ITextDll because they take an FdoCache as an argument and
	/// none of our Utils projects reference FDO, while it references most of them. So it seemed
	/// the most general place they would actually build.
	/// </summary>
	public class DbOps
	{
		/// <summary></summary>
		static protected DbOps s_DbOps = new DbOps();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Release the given parameter, and set the input variable to null.
		/// </summary>
		/// <param name="odc">Referecne to the object to close down.</param>
		/// ------------------------------------------------------------------------------------
		static public void ShutdownODC(ref IOleDbCommand odc)
		{
			if ((odc != null) && Marshal.IsComObject(odc))
			{
				Marshal.FinalReleaseComObject(odc);
				odc = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the query sql, which contains one string parameter whose value is
		/// supplied as param. The result is a single rowset, which may contain one or
		/// zero rows. If it contains zero rows, return val as 0 and result false.
		/// If it contains (at least) one row, read an integer from the first column
		/// of the first row, return it in val, and return true. (but if the value is
		/// null, return 0 and true.)
		/// </summary>
		/// <param name="cache">parameter value, or null if not required.</param>
		/// <param name="sql">The SQL.</param>
		/// <param name="param">optional string parameter...if null, query needs none.</param>
		/// <param name="val">The val.</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual bool InternalReadOneIntFromCommand(FdoCache cache, string sql, string param,
			out int val)
		{
			val = 0;
			IOleDbCommand odc = null;
			try
			{
				odc = MakeRowSet(cache, sql, param);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);

				if(!fMoreRows)
					return false;
				val = ReadInt(odc, 0);
			}
			finally
			{
				ShutdownODC(ref odc);
			}

			return true;
		}

		/// <summary>
		/// Execute the specified SQL and return the string which should be the first column of the first row.
		/// If there aren't that many rows return null.
		/// </summary>
		public static string ReadString(FdoCache cache, string sql, string param)
		{
			IOleDbCommand odc = null;
			try
			{
				odc = MakeRowSet(cache, sql, param);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);

				if (!fMoreRows)
					return null;
				return ReadString(odc, 0);
			}
			finally
			{
				ShutdownODC(ref odc);
			}
		}
		/// <summary>
		/// Execute the query sql, which may contain one string parameter whose value is
		/// supplied as param. The result is a single rowset, which may contain one or
		/// zero rows. If it contains zero rows, return 0.
		/// If it contains (at least) one row, read a long from the first column
		/// of the first row and return it. Return zero if it is null.
		/// </summary>
		/// <param name="cache">Cache to read from.</param>
		/// <param name="sql"></param>
		/// <param name="param">optional string parameter...if null, query needs none.</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		public static long ReadOneLongFromCommand(FdoCache cache, string sql, string param)
		{
			IOleDbCommand odc = null;
			try
			{
				odc = MakeRowSet(cache, sql, param);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);

				if (!fMoreRows)
					return 0;
				bool fIsNull;
				uint cbSpaceTaken;
				using (ArrayPtr src = MarshalEx.ArrayToNative(1, typeof(long)))
				{
					odc.GetColValue((uint)(1), src, src.Size, out cbSpaceTaken, out fIsNull, 0);
					if (fIsNull)
						return 0;
					// Unfortunately this produces a long with the bytes reversed.
					ulong revVal = (ulong) Marshal.ReadInt64(src.IntPtr);
					ulong result = 0;
					for (int i = 0; i < 8; i++)
					{
						ulong b = (revVal >> i * 8) % 0x100;
						result += b << ((7 - i) * 8);
					}
					return (long) result;
				}
			}
			finally
			{
				ShutdownODC(ref odc);
			}
		}

		/// <summary>
		/// Execute the query sql, which contains one string parameter whose value is
		/// supplied as param. The result is a single rowset, which may contain one or
		/// zero rows. If it contains zero rows, return val as 0 and result false.
		/// If it contains (at least) one row, read an integer from the first column
		/// of the first row, return it in val, and return true. (but if the value is
		/// null, return 0 and true.)
		/// </summary>
		/// <param name="cache">parameter value, or null if not required.</param>
		/// <param name="sql"></param>
		/// <param name="param">optional string parameter...if null, query needs none.</param>
		/// <param name="val"></param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		static public bool ReadOneIntFromCommand(FdoCache cache, string sql, string param,
			out int val)
		{
			return s_DbOps.InternalReadOneIntFromCommand(cache, sql, param, out val);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the query sql, which contains one string parameter whose value is
		/// supplied as param. The result is a single rowset, which may contain one or
		/// zero rows. If it contains zero rows, return val as 0 and result false.
		/// If it contains (at least) one row, read an integer from the first column
		/// of the first row, return it in val, and return true. (but if the value is
		/// null, return 0 and true.)
		/// </summary>
		/// <param name="cache">parameter value, or null if not required.</param>
		/// <param name="sql">The SQL.</param>
		/// <param name="param">Required int parameter.</param>
		/// <param name="val">The val.</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		static public bool ReadOneIntFromCommand(FdoCache cache, string sql, int param,
			out int val)
		{
			val = 0;
			List<int> ints = ReadIntsFromCommand(cache, sql, param);
			if (ints.Count > 0)
			{
				val = (int)ints[0];
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the specified sql, supplying the parameter param if it is non-null.
		/// Obtain the first rowset from the result.
		/// Return an odc ready for reading rows from the first row set.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sql">The SQL.</param>
		/// <param name="param">The param.</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		public static IOleDbCommand MakeRowSet(FdoCache cache, string sql, string param)
		{
			IOleDbCommand odc;
			cache.DatabaseAccessor.CreateCommand(out odc);
			if (param == null)
				param = ""; // Paramaters aren't optional, so look for null string.
			odc.SetStringParameter(1, // 1-based parameter index
				(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
				null, //flags
				param,
				(uint)param.Length); // despite doc, impl makes clear this is char count
			odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
			odc.GetRowset(0);
			return odc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the stored procedure indicated in the sql, which may take one
		/// optional parameter (pass null if no param is needed). It is not expected
		/// to return any results.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sql">The SQL.</param>
		/// <param name="param">The param.</param>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		public static void ExecuteStoredProc(FdoCache cache, string sql, string param)
		{
			IOleDbCommand odc = null;
			try
			{
				cache.DatabaseAccessor.CreateCommand(out odc);
				if (param != null)
				{
					odc.SetStringParameter(1, // 1-based parameter index
						(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
						null, //flags
						param,
						(uint)param.Length); // despite doc, impl makes clear this is char count
				}
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtStoredProcedure);
			}
			finally
			{
				ShutdownODC(ref odc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute some arbitrary sql, which may take one
		/// optional parameter (pass null if no param is needed). It is not expected
		/// to return any results.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sql">The SQL.</param>
		/// <param name="param">The param.</param>
		/// ------------------------------------------------------------------------------------
		public static void ExecuteStatementNoResults(FdoCache cache, string sql, string param)
		{
			IOleDbCommand odc = null;
			try
			{
				cache.DatabaseAccessor.CreateCommand(out odc);
				if (param != null)
				{
					odc.SetStringParameter(1, // 1-based parameter index
						(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
						null, //flags
						param,
						(uint)param.Length); // despite doc, impl makes clear this is char count
				}
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtNoResults);
			}
			finally
			{
				ShutdownODC(ref odc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the icol'th column from the current row as an integer. Return zero if the
		/// value is null.
		/// </summary>
		/// <param name="odc">The odc.</param>
		/// <param name="icol">ZERO-based column number</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		public static int ReadInt(IOleDbCommand odc, int icol)
		{
			bool fIsNull;
			uint cbSpaceTaken;
			using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
			{
				odc.GetColValue((uint)(icol + 1), rgHvo, rgHvo.Size, out cbSpaceTaken, out fIsNull, 0);
				if (fIsNull)
					return 0;
				return IntFromStartOfUintArrayPtr(rgHvo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a string (up to 4000 characters) from the specified column of the result set.
		/// (This should not be used yet for BigString propertis which might exceed 4000 chars.)
		/// </summary>
		/// <param name="odc">The odc.</param>
		/// <param name="icol">The icol.</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		public static string ReadString(IOleDbCommand odc, int icol)
		{
			byte[] rgbTemp = ReadBytes(odc, icol);
			if (rgbTemp == null)
				return "";
			return Encoding.Unicode.GetString(rgbTemp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return an array of strings given an SQL query.
		/// <param name="cache">The cache in use</param>
		/// <param name="qry">An SQL query to execute</param>
		/// </summary>
		/// ------------------------------------------------------------------------------------

		public static string[] ReadMultiUnicodeTxtStrings(FdoCache cache, string qry)
		{
			StringCollection col = new StringCollection();
			IOleDbCommand odc = null;
			cache.DatabaseAccessor.CreateCommand(out odc);
			try
			{
				uint cbSpaceTaken;
				bool fMoreRows;
				bool fIsNull;
				uint uintSize = (uint)Marshal.SizeOf(typeof(uint));
				odc.ExecCommand(qry, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				odc.NextRow(out fMoreRows);
				while (fMoreRows)
				{
					using (ArrayPtr prgchName = MarshalEx.ArrayToNative(4000, typeof(char)))
					{
						odc.GetColValue(1, prgchName, prgchName.Size, out cbSpaceTaken, out fIsNull, 0);
						byte[] rgbTemp = (byte[])MarshalEx.NativeToArray(prgchName, (int)cbSpaceTaken, typeof(byte));
						col.Add(Encoding.Unicode.GetString(rgbTemp));
					}
					odc.NextRow(out fMoreRows);
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			string[] strings = new string[col.Count];
			for (int i = 0; i < col.Count; ++i)
				strings[i] = col[i];
			return strings;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a string (up to 4000 characters) from the specified column of the result set.
		/// (This should not be used yet for BigString propertis which might exceed 4000 chars.)
		/// </summary>
		/// <param name="odc">The odc.</param>
		/// <param name="icol">The icol.</param>
		/// <param name="ws">The ws.</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		public static ITsString ReadTss(IOleDbCommand odc, int icol, int ws)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(ReadString(odc, icol), ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read a string (up to 4000 characters) from the specified column of the result set.
		/// (This should not be used yet for BigString properties which might exceed 4000 chars.)
		/// Also read the following column to obtain a writing system.
		/// Returns null if ws is zero (or null).
		/// </summary>
		/// <param name="odc">The odc.</param>
		/// <param name="icol">The icol.</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		public static ITsString ReadTss2(IOleDbCommand odc, int icol)
		{
			int ws = ReadInt(odc, icol + 1);
			if (ws != 0)
				return ReadTss(odc, icol, ws);
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a byte array read from the specified column
		/// </summary>
		/// <param name="odc">The odc.</param>
		/// <param name="icol">ZERO-based column index.</param>
		/// <returns>null or byte array</returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		public static byte[] ReadBytes(IOleDbCommand odc, int icol)
		{
			using (ArrayPtr prgch = MarshalEx.ArrayToNative(4000, typeof(byte)))
			{
				uint cbSpaceTaken;
				bool fIsNull;
				odc.GetColValue((uint)(icol + 1), prgch, prgch.Size, out cbSpaceTaken, out fIsNull, 0);
				if (fIsNull)
					return null;
				return (byte[])MarshalEx.NativeToArray(prgch, (int)cbSpaceTaken, typeof(byte));
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the query sql, which contains zero or one string parameter whose value is
		/// supplied as param (or null). The result is a single rowset, which may contain one or
		/// zero rows. If it contains zero rows, return an empty array.
		/// If it contains (at least) one row, read ccol columns, and
		/// make an array out of them.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sql">The SQL.</param>
		/// <param name="param">The param.</param>
		/// <param name="cols">The cols.</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		static public int[] ReadIntsFromRow(FdoCache cache, string sql, string param, int cols)
		{
			int[] result = new int[0];
			IOleDbCommand odc = null;
			try
			{
				cache.DatabaseAccessor.CreateCommand(out odc);
				if (param != null)
				{
					odc.SetStringParameter(1, // 1-based parameter index
						(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
						null, //flags
						param,
						(uint)param.Length); // despite doc, impl makes clear this is char count
				}
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				if(!fMoreRows)
					return result;
				result = new int[cols];
				using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					for (int i = 0; i < cols; ++i)
					{
						bool fIsNull;
						uint cbSpaceTaken;
						odc.GetColValue((uint)(i + 1), rgHvo, rgHvo.Size, out cbSpaceTaken, out fIsNull, 0);
						if (!fIsNull)
							result[i] = IntFromStartOfUintArrayPtr(rgHvo);
					}
				}
			}
			finally
			{
				ShutdownODC(ref odc);
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the query sql, which contains one string parameter whose value is
		/// supplied as param. The result is a single rowset, which may contain zero or
		/// more rows. Read an integer from each row and return them as an array.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sql">The SQL.</param>
		/// <param name="param">May be null, if not required.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public int[] ReadIntArrayFromCommand(FdoCache cache, string sql, string param)
		{
			return ListToIntArray(ReadIntsFromCommand(cache, sql, param));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Utility function to make an integer array out of a List containing integers.
		/// </summary>
		/// <param name="list">The list.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public int[] ListToIntArray(List<int> list)
		{
			return list.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Utility function to make an integer array out of a Set containing integers.
		/// </summary>
		/// <param name="set">The set.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public int[] ListToIntArray(Set<int> set)
		{
			return set.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given an ArrayPtr created like MarshalEx.ArrayToNative(1, typeof(uint)),
		/// Extract an int from its first element.
		/// </summary>
		/// <param name="src"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public int IntFromStartOfUintArrayPtr(ArrayPtr src)
		{
			return (int)(uint)Marshal.PtrToStructure((IntPtr)src, typeof(uint));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the query sql, which contains one string parameter whose value is
		/// supplied as param. The result is a single rowset, which may contain zero or
		/// more rows. Read an integer from each row and return them as a List.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sql"></param>
		/// <param name="param">May be null, if not required.</param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		/// ------------------------------------------------------------------------------------
		static public List<int> ReadIntsFromCommand(FdoCache cache, string sql, string param)
		{
			return s_DbOps.InternalReadIntsFromCommand(cache, sql, param);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Internals the read ints from command.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sql">The SQL.</param>
		/// <param name="param">The param.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual List<int> InternalReadIntsFromCommand(FdoCache cache, string sql, string param)
		{
			List<int> list = new List<int>();
			IOleDbCommand odc = null;
			try
			{
				cache.DatabaseAccessor.CreateCommand(out odc);
				if (param != null)
				{
					odc.SetStringParameter(1, // 1-based parameter index
						(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
						null, //flags
						param,
						(uint)param.Length); // despite doc, impl makes clear this is char count
				}
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				bool fIsNull;
				uint cbSpaceTaken;

				using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					while (fMoreRows)
					{
						odc.GetColValue(1, rgHvo, rgHvo.Size, out cbSpaceTaken, out fIsNull, 0);
						if (!fIsNull)
						{
							list.Add(IntFromStartOfUintArrayPtr(rgHvo));
						}
						odc.NextRow(out fMoreRows);
					}
				}
			}
			finally
			{
				ShutdownODC(ref odc);
			}
			return list;
		}
		/// <summary>
		/// Execute the query sql, which optionally contains one string parameter whose value is
		/// supplied as param. The result is a single rowset, which may contain zero or
		/// more rows, each containing a pair of integers. The row set represents multiple
		/// sequences. A sequence is defined by consecutive rows with the same value for the first
		/// item. Each sequence is entered into the values dictionary, with the column 1 value
		/// as the key, and a list of the column 2 values as the value.
		/// Rows where either value is null or key is zero will be ignored.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sql"></param>
		/// <param name="param">May be null, if not required.</param>
		/// <param name="values"></param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		static public void LoadDictionaryFromCommand(FdoCache cache, string sql, string param, Dictionary<int, List<int> > values)
		{
			// As of 11/30/2006, all callers of this method are looking for reference sequence data,
			// so this List of ints cannot be a set.
			List<int> list = null;
			IOleDbCommand odc = null;
			try
			{
				cache.DatabaseAccessor.CreateCommand(out odc);
				if (param != null)
				{
					odc.SetStringParameter(1, // 1-based parameter index
						(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
						null, //flags
						param,
						(uint)param.Length); // despite doc, impl makes clear this is char count
				}
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				bool fIsNull;
				uint cbSpaceTaken;

				int currentKey = 0;

				using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					for (; fMoreRows;  odc.NextRow(out fMoreRows))
					{
						int key, val;
						odc.GetColValue(1, rgHvo, rgHvo.Size, out cbSpaceTaken, out fIsNull, 0);
						if (fIsNull)
							continue;
						key = IntFromStartOfUintArrayPtr(rgHvo);
						if (key == 0)
							continue;
						odc.GetColValue(2, rgHvo, rgHvo.Size, out cbSpaceTaken, out fIsNull, 0);
						if (fIsNull)
							continue;
						val = IntFromStartOfUintArrayPtr(rgHvo);
						if (key != currentKey)
						{
							list = new List<int>();
							currentKey = key;
							values[currentKey] = list;
						}
						list.Add(val);
					}
				}
			}
			finally
			{
				ShutdownODC(ref odc);
			}
		}

		/// <summary>
		/// Execute the query sql, which contains one required integer parameter whose value is
		/// supplied as param. The result is a single rowset, which may contain zero or
		/// more rows. Read an integer from each row and return them as a List.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sql"></param>
		/// <param name="param">Required integer parameter.</param>
		/// <returns>A List containing zero, or more, integers.</returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		static public List<int> ReadIntsFromCommand(FdoCache cache, string sql, int param)
		{
			List<int> list = new List<int>();
			IOleDbCommand odc = null;
			try
			{
				cache.DatabaseAccessor.CreateCommand(out odc);
				uint uintSize = (uint)Marshal.SizeOf(typeof(uint));
				odc.SetParameter(1, // 1-based parameter index
					(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
					null, //flags
					(ushort)DBTYPEENUM.DBTYPE_I4,
					new uint[] { (uint)param },
					uintSize);
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				bool fIsNull;
				uint cbSpaceTaken;

				using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					while(fMoreRows)
					{
						odc.GetColValue(1, rgHvo, uintSize, out cbSpaceTaken, out fIsNull, 0);
						if (!fIsNull)
						{
							list.Add(IntFromStartOfUintArrayPtr(rgHvo));
						}
						odc.NextRow(out fMoreRows);
					}
				}
			}
			finally
			{
				ShutdownODC(ref odc);
			}
			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a database object that has the specified value in the specified ws of the
		/// specified string property, and return its HVO. Returns 0 if there is no such object.
		/// We force a binary collation so that only exact matches are found. This is especially
		/// necessary because SQLServer ignores unknown characters, including all characters
		/// added to Unicode since 2000, such as Khmer and Phonetic characters.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="flid">The flid.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="fHasFmt">True to read from MultiStr$ for properties with full string value;
		/// false to read from MultiTxt$ for properties with just ws info.</param>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public int FindObjectWithStringInFlid(string key, int flid, int ws, bool fHasFmt,
			FdoCache cache)
		{
			string sql;
			string sField = cache.MetaDataCacheAccessor.GetFieldName((uint)flid);
			string sClass = cache.MetaDataCacheAccessor.GetOwnClsName((uint)flid);
			/*
			if (fHasFmt)
			{
				sql = String.Format("SELECT TOP 1 obj FROM {0}_{1} WHERE Ws ={2} AND Txt = ?",
					sClass, sField, ws);
			}
			else
			{
				sql = String.Format("SELECT TOP 1 obj FROM {0}_{1} WHERE Ws ={2} AND Txt = ?",
					sClass, sField, ws);
			}
			*/
			sql = String.Format("SELECT TOP 1 obj FROM {0}_{1} WHERE Ws={2} AND Txt=?",
				sClass, sField, ws);
			int val;
			ReadOneIntFromCommand(cache, sql, key, out val);
			return val;
		}

		/// <summary>
		/// Execute the query sql, which contains zero or one string parameter whose value is
		/// supplied as param (or null). The result is a single rowset, which may contain zero or
		/// more rows, each containing cols columns, each an integer value. Read the entire rowset,
		/// returning a List of int[cols] arrays.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sql"></param>
		/// <param name="param"></param>
		/// <param name="cols"></param>
		/// <returns></returns>
		/// <remarks>The SQL command must NOT modify the database in any way!</remarks>
		static public List<int[]> ReadIntArray(FdoCache cache, string sql, string param, int cols)
		{
			List<int[]> resultList = new List<int[]>();
			IOleDbCommand odc = null;
			try
			{
				cache.DatabaseAccessor.CreateCommand(out odc);
				if (param != null)
				{
					odc.SetStringParameter(1, // 1-based parameter index
						(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
						null, //flags
						param,
						(uint)param.Length); // despite doc, impl makes clear this is char count
				}
				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					while (fMoreRows)
					{
						int[] result = new int[cols];
						for (int i = 0; i < cols; ++i)
						{
							bool fIsNull;
							uint cbSpaceTaken;
							odc.GetColValue((uint)(i + 1), rgHvo, rgHvo.Size, out cbSpaceTaken, out fIsNull, 0);
							if (!fIsNull)
							{
								result[i] = IntFromStartOfUintArrayPtr(rgHvo);
							}
						}
						resultList.Add(result);
						odc.NextRow(out fMoreRows);
					}
				}
			}
			finally
			{
				ShutdownODC(ref odc);
			}
			return resultList;
		}

		/// <summary>
		/// Creates and returns a string containing some or all of the members of m_hvos. Function
		/// stops adding members when the overall string reaches the length defined in internal
		/// constant kcchMaxIdList.
		/// </summary>
		/// <param name="iNextGroup">Index of first member of m_hvos to add.
		/// </param>
		/// <param name="hvos">The objects to select a group of.</param>
		/// <returns>String made of comma-separated integers
		/// </returns>
		public static string MakePartialIdList(ref int iNextGroup, int[] hvos)
		{
			StringBuilder sb = new StringBuilder();

			const int kcchMaxIdList = 3000; // List may get a little longer (by string length of 1 int).

			for (; sb.Length < kcchMaxIdList && iNextGroup < hvos.Length; )
			{
				sb.Append(hvos[iNextGroup++].ToString());
				sb.Append(',');
			}
			if (sb.Length > 0)
			{
				// Remove last comma
				sb.Remove(sb.Length - 1, 1);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Get a list of flids for the given class, and type.
		/// </summary>
		/// <param name="mdc"></param>
		/// <param name="clsid"></param>
		/// <param name="flidType"></param>
		/// <returns></returns>
		static public uint[] GetFieldsInClassOfType(IFwMetaDataCache mdc, int clsid, FieldType flidType)
		{
			return GetFieldsInClassOfType(mdc, (uint)clsid, flidType);
		}

		/// <summary>
		/// Get a list of flids for the given class, and type.
		/// </summary>
		/// <param name="mdc"></param>
		/// <param name="clsid"></param>
		/// <param name="flidType"></param>
		/// <returns></returns>
		static public uint[] GetFieldsInClassOfType(IFwMetaDataCache mdc, uint clsid, FieldType flidType)
		{
			uint[] retval = new uint[0];
			int nflidType = (int)flidType;
			int flidCount = mdc.GetFields(clsid, true, nflidType, 0, ArrayPtr.Null);
			if(flidCount > 0)
			{
				using (ArrayPtr rgflid = new ArrayPtr(flidCount * Marshal.SizeOf(typeof(uint))))
				{
					int flidCount2 = mdc.GetFields(clsid, true, nflidType, flidCount, rgflid);
					Debug.Assert(flidCount == flidCount2);
					retval = (uint[])MarshalEx.NativeToArray(rgflid, flidCount, typeof(uint));
				}
			}
			return retval;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of connections to the current database.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns>Number of connections to current database, or 0 if error occurred.</returns>
		/// ------------------------------------------------------------------------------------
		static public int GetNumberOfConnectionsToDb(FdoCache cache)
		{
			string sql = string.Format("select count(distinct spid) from master.dbo.sysprocesses " +
				"sproc join master.dbo.sysdatabases sdb on sdb.dbid = sproc.dbid and name = '{0}'",
				cache.DatabaseName);

			int val;
			if (!s_DbOps.InternalReadOneIntFromCommand(cache, sql, "", out val))
				return 0;
			return val;
		}
	}
}
