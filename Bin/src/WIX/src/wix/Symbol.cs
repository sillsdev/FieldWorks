//-------------------------------------------------------------------------------------------------
// <copyright file="Symbol.cs" company="Microsoft">
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
// Symbol representing a single row in a database.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Text;

	/// <summary>
	/// Symbol representing a single row in a database.
	/// </summary>
	public class Symbol
	{
		private Row row;

		private Section section;
		private string tableName;
		private string rowId;

		/// <summary>
		/// Creates a symbol for a row.
		/// </summary>
		/// <param name="row">Row for the symbol</param>
		public Symbol(Row row)
		{
			this.row = row;
		}

		/// <summary>
		/// Creates a symbol without a row reference.
		/// </summary>
		/// <param name="section">Section to add symbol to.</param>
		/// <param name="tableName">Name of table for symbol.</param>
		/// <param name="rowId">Id of row for symbol.</param>
		public Symbol(Section section, string tableName, string rowId)
		{
			this.section = section;
			this.tableName = tableName;
			this.rowId = rowId;
		}

		/// <summary>
		/// Gets the name of the symbol.
		/// </summary>
		/// <value>Name of the symbol.</value>
		public string Name
		{
			get
			{
				if (null != this.tableName)
				{
					return String.Concat(this.tableName, ":", this.rowId);
				}

				StringBuilder sb = new StringBuilder();
				bool first = true;

				sb.Append(this.row.TableDefinition.Name);
				sb.Append(":");
				for (int i = 0; i < this.row.Fields.Length; ++i)
				{
					if (!this.row.Fields[i].Column.IsSymbol)
					{
						continue;
					}

					if (!first)
					{
						sb.Append("/");
					}
					else
					{
						first = false;
					}

					sb.Append(this.row[i]);
				}

				return sb.ToString();
			}
		}

		/// <summary>
		/// Gets the section for the symbol.
		/// </summary>
		/// <value>Section for the symbol.</value>
		public Section Section
		{
			get
			{
				if (null != this.section)
				{
					return this.section;
				}

				return (null == this.row.Table) ? null : this.row.Table.Section;
			}
		}

		/// <summary>
		/// Gets the row identifier for the symbol.
		/// </summary>
		/// <value>Row identifier for the symbol.</value>
		public string RowId
		{
			get
			{
				if (null != this.rowId)
				{
					return this.rowId;
				}

				StringBuilder sb = new StringBuilder();
				bool first = true;

				for (int i = 0; i < this.row.Fields.Length; ++i)
				{
					if (!this.row.Fields[i].Column.IsSymbol)
					{
						continue;
					}

					if (first)
					{
						first = false;
					}
					else
					{
						sb.Append("/");
					}

					sb.Append(this.row[i]);
				}

				return sb.ToString();
			}
		}

		/// <summary>
		/// Gets the table name for this symbol.
		/// </summary>
		/// <value>Table name for this symbol.</value>
		public string TableName
		{
			get { return this.tableName; }
		}

		/// <summary>
		/// Gets the row for this symbol.
		/// </summary>
		/// <value>Row for this symbol.</value>
		public Row Row
		{
			get { return this.row; }
		}
	}
}
