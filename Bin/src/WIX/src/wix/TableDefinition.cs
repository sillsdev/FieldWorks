//-------------------------------------------------------------------------------------------------
// <copyright file="TableDefinition.cs" company="Microsoft">
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
// Definition of a table in a database.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Definition of a table in a database.
	/// </summary>
	public class TableDefinition
	{
		private string name;
		private bool unreal;
		private ColumnDefinitionCollection columns;

		/// <summary>
		/// Creates a table definition.
		/// </summary>
		/// <param name="name">Name of table to create.</param>
		/// <param name="unreal">Flag if table is unreal.</param>
		public TableDefinition(string name, bool unreal)
		{
			this.name = name;
			this.unreal = unreal;
			this.columns = new ColumnDefinitionCollection();
		}

		/// <summary>
		/// Gets the name of the table.
		/// </summary>
		/// <value>Name of the table.</value>
		public string Name
		{
			get { return this.name; }
		}

		/// <summary>
		/// Gets if the table is unreal.
		/// </summary>
		/// <value>Flag if table is unreal.</value>
		public bool IsUnreal
		{
			get { return this.unreal; }
		}

		/// <summary>
		/// Gets the collection of column definitions for this table.
		/// </summary>
		/// <value>Collection of column definitions for this table.</value>
		public ColumnDefinitionCollection Columns
		{
			get { return this.columns; }
		}

		/// <summary>
		/// Gets the column definition in the table by index.
		/// </summary>
		/// <param name="columnIndex">Index of column to locate.</param>
		/// <value>Column definition in the table by index.</value>
		public ColumnDefinition this[int columnIndex]
		{
			get { return (ColumnDefinition)this.columns[columnIndex]; }
		}

		/// <summary>
		/// Gets the column definition by column name.
		/// </summary>
		/// <param name="columnName">Name of column to locate.</param>
		/// <value>Column definition in the table by name.</value>
		/// <returns>The column definition of the named table.</returns>
		public ColumnDefinition GetColumnDefinition(string columnName)
		{
			for (int i = 0; i < this.columns.Count; ++i)
			{
				if (this.columns[i].Name == columnName)
				{
					return this.columns[i];
				}
			}

			throw new ArgumentException(String.Format("Cannot find column definition with name: {0}", columnName), "columnName");
		}

		/// <summary>
		/// Gets the table definition in IDT format.
		/// </summary>
		/// <returns>Table definition in IDT format.</returns>
		public string ToIdtDefinition()
		{
			bool first = true;
			StringBuilder columnString = new StringBuilder();
			StringBuilder dataString = new StringBuilder();
			StringBuilder tableString = new StringBuilder();

			tableString.Append(this.name);
			foreach (ColumnDefinition column in this.columns)
			{
				if (column.IsUnreal)
				{
					continue;
				}

				if (!first)
				{
					columnString.Append('\t');
					dataString.Append('\t');
				}

				columnString.Append(column.Name);
				dataString.Append(column.GetIdtType());

				if (column.IsPrimaryKey)
				{
					tableString.AppendFormat("\t{0}", column.Name);
				}

				first = false;
			}
			columnString.Append("\r\n");
			columnString.Append(dataString);
			columnString.Append("\r\n");
			columnString.Append(tableString);
			columnString.Append("\r\n");

			return columnString.ToString();
		}

		/// <summary>
		/// Parses table definition from xml reader.
		/// </summary>
		/// <param name="reader">Reader to get data from.</param>
		/// <returns>The TableDefintion represented by the Xml.</returns>
		internal static TableDefinition Parse(XmlReader reader)
		{
			Debug.Assert("tableDefinition" == reader.LocalName);

			string name = null;
			bool unreal = false;
			bool empty = reader.IsEmptyElement;

			// parse the attributes
			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "name":
						name = reader.Value;
						break;
					case "unreal":
						unreal = Common.IsYes(reader.Value, null, "tableDefinition", reader.Name, name);
						break;
					case "xmlns":
						break;
					default:
						throw new WixParseException(String.Format("The tableDefinition element contains an unexpected attribute '{0}'.", reader.Name));
				}
			}

			if (null == name)
			{
				throw new WixParseException("The tableDefinition/@name attribute was not found; it is required.");
			}

			TableDefinition tableDefinition = new TableDefinition(name, unreal);

			// parse the child elements
			if (!empty)
			{
				bool done = false;

				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
						switch (reader.LocalName)
						{
							case "columnDefinition":
								tableDefinition.columns.Add(ColumnDefinition.Parse(reader));
								break;
							default:
								throw new WixParseException(String.Format("The tableDefinition element contains an unexpected child element {0}.", reader.Name));
						}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixParseException("Missing end element while processing the tableDefinition element.");
				}
			}

			return tableDefinition;
		}

		/// <summary>
		/// Persists an output in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the Output should persist itself as XML.</param>
		internal void Persist(XmlWriter writer)
		{
			writer.WriteStartElement("tableDefinition", "http://schemas.microsoft.com/wix/2003/04/tables");

			writer.WriteAttributeString("name", this.name);
			if (this.unreal)
			{
				writer.WriteAttributeString("unreal", "yes");
			}

			foreach (ColumnDefinition columnDefinition in this.columns)
			{
				columnDefinition.Persist(writer);
			}

			writer.WriteEndElement();
		}

		/// <summary>
		/// Gets the validation rows for the table.
		/// </summary>
		/// <param name="validationTable">Defintion for the validation table.</param>
		/// <returns>Collection of output rows for the validation table.</returns>
		internal OutputRowCollection GetValidationRows(TableDefinition validationTable)
		{
			OutputRowCollection outputRows = new OutputRowCollection();

			foreach (ColumnDefinition columnDef in this.columns)
			{
				if (columnDef.IsUnreal)
				{
					continue;
				}

				Row row = new Row(validationTable);

				row[0] = this.name;

				row[1] = columnDef.Name;

				if (columnDef.IsNullable)
				{
					row[2] = "Y";
				}
				else
				{
					row[2] = "N";
				}

				if (columnDef.IsMinValueSet)
				{
					row[3] = columnDef.MinValue;
				}

				if (columnDef.IsMaxValueSet)
				{
					row[4] = columnDef.MaxValue;
				}

				row[5] = columnDef.KeyTable;

				if (columnDef.IsKeyColumnSet)
				{
					row[6] = columnDef.KeyColumn;
				}

				if (ColumnCategory.Unknown != columnDef.Category)
				{
					row[7] = columnDef.Category.ToString();
				}

				row[8] = columnDef.Possibilities;

				row[9] = columnDef.Description;

				outputRows.Add(new OutputRow(row));
			}

			return outputRows;
		}
	}
}
