//-------------------------------------------------------------------------------------------------
// <copyright file="Table.cs" company="Microsoft">
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
// Object that represents a table in a database.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Object that represents a table in a database.
	/// </summary>
	public class Table
	{
		private Section section;
		private TableDefinition tableDef;
		private RowCollection rows;

		/// <summary>
		/// Creates a table in a section.
		/// </summary>
		/// <param name="section">Section to add table to.</param>
		/// <param name="tableDef">Definition of table.</param>
		public Table(Section section, TableDefinition tableDef)
		{
			this.section = section;
			this.tableDef = tableDef;
			this.rows = new RowCollection();
		}

		/// <summary>
		/// Gets the section for the table.
		/// </summary>
		/// <value>Section for the table.</value>
		public Section Section
		{
			get { return this.section; }
		}

		/// <summary>
		/// Gets the table definition.
		/// </summary>
		/// <value>Definition of the table.</value>
		public TableDefinition Definition
		{
			get { return this.tableDef; }
		}

		/// <summary>
		/// Gets the name of the table.
		/// </summary>
		/// <value>Name of the table.</value>
		public string Name
		{
			get { return this.tableDef.Name; }
		}

		/// <summary>
		/// Gets the column definitions for the table.
		/// </summary>
		/// <value>Column definitions for the table.</value>
		public ColumnDefinitionCollection Columns
		{
			get { return this.tableDef.Columns; }
		}

		/// <summary>
		/// Gets the rows contained in the table.
		/// </summary>
		/// <value>Rows contained in the table.</value>
		public RowCollection Rows
		{
			get { return this.rows; }
		}

		/// <summary>
		/// Creates a new row in the table.
		/// </summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <returns>Row created in table.</returns>
		public Row CreateRow(SourceLineNumberCollection sourceLineNumbers)
		{
			Row row;
			switch (this.Name)
			{
				// TODO:  create strongly types rows here
				case "BBControl":
					row = new BBControlRow(sourceLineNumbers, this);
					break;
				case "Control":
					row = new ControlRow(sourceLineNumbers, this);
					break;
				case "File":
					row = new FileRow(sourceLineNumbers, this);
					break;
				case "Media":
					row = new MediaRow(sourceLineNumbers, this);
					break;
				case "Merge":
					row = new MergeRow(sourceLineNumbers, this);
					break;
				case "Property":
					row = new PropertyRow(sourceLineNumbers, this);
					break;
				case "Upgrade":
					row = new UpgradeRow(sourceLineNumbers, this);
					break;

				default:
					row = new Row(sourceLineNumbers, this);
					break;
			}

			this.rows.Add(row);
			return row;
		}

		/// <summary>
		/// Persists a row in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the Row should persist itself as XML.</param>
		public void Persist(XmlWriter writer)
		{
			if (null == writer)
			{
				throw new ArgumentNullException("writer");
			}

			writer.WriteStartElement("table");
			writer.WriteAttributeString("name", this.Name);

			foreach (Row row in this.rows)
			{
				row.Persist(writer);
			}

			writer.WriteEndElement();
		}
	}
}
