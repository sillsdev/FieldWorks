//-------------------------------------------------------------------------------------------------
// <copyright file="Row.cs" company="Microsoft">
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
// Row containing data for a table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Row containing data for a table.
	/// </summary>
	public class Row
	{
		private static int rowCount = 0;

		private Table table;
		private TableDefinition tableDef;

		private int rowNumber;
		private string sectionId;
		private SourceLineNumberCollection sourceLineNumbers;

		private Field[] fields;
		private bool unreal;

		private bool hasSymbol;
		private Symbol symbol;

		/// <summary>
		/// Creates a row that belongs to a table.
		/// </summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="table">Table this row belongs to and should get its column definitions from.</param>
		/// <remarks>The compiler should use this constructor exclusively.</remarks>
		public Row(SourceLineNumberCollection sourceLineNumbers, Table table) :
			this(sourceLineNumbers, null == table ? null : table.Columns)
		{
			this.table = table;
		}

		/// <summary>
		/// Creates a row that does not belong to a table.
		/// </summary>
		/// <param name="tableDef">TableDefinition this row should get its column definitions from.</param>
		/// <remarks>This constructor is used in cases where there isn't a clear owner of the row.  The linker uses this constructor for the rows it generates.</remarks>
		public Row(TableDefinition tableDef) :
			this(null, null == tableDef ? null : tableDef.Columns)
		{
			this.tableDef = tableDef;
		}

		/// <summary>
		/// Creates a row that does not belong to a table.
		/// </summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="tableDef">TableDefinition this row should get its column definitions from.</param>
		/// <remarks>This constructor is used in cases where there isn't a clear owner of the row.  The linker uses this constructor for the rows it generates.</remarks>
		public Row(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
			this(sourceLineNumbers, null == tableDef ? null : tableDef.Columns)
		{
			this.tableDef = tableDef;
		}

		/// <summary>
		/// Helper constructor.
		/// </summary>
		/// <param name="sourceLineNumbers">Source file and line number for this row.</param>
		/// <param name="columns">Definition of columns for this row.</param>
		private Row(SourceLineNumberCollection sourceLineNumbers, ColumnDefinitionCollection columns)
		{
			if (null == columns)
			{
				throw new ArgumentNullException("columns");
			}

			this.rowNumber = rowCount++;
			this.sourceLineNumbers = sourceLineNumbers;
			this.fields = new Field[columns.Count];

			for (int i = 0; i < this.fields.Length; ++i)
			{
				this.fields[i] = new Field(columns[i]);
				if (this.fields[i].Column.IsSymbol)
				{
					this.hasSymbol = true;
				}
			}
		}

		/// <summary>
		/// Gets the unique number for the row.
		/// </summary>
		/// <value>Number for row.</value>
		public int Number
		{
			get { return this.rowNumber; }
		}

		/// <summary>
		/// Gets or sets the SectionId property on the row.
		/// </summary>
		/// <value>The SectionId property on the row.</value>
		public string SectionId
		{
			get { return this.sectionId; }
			set { this.sectionId = value; }
		}

		/// <summary>
		/// Gets the source file and line number for the row.
		/// </summary>
		/// <value>Source file and line number.</value>
		public SourceLineNumberCollection SourceLineNumbers
		{
			get { return this.sourceLineNumbers; }
		}

		/// <summary>
		/// Gets the table this row belongs to.
		/// </summary>
		/// <value>null if Row does not belong to a Table, or owner Table otherwise.</value>
		public Table Table
		{
			get { return this.table; }
		}

		/// <summary>
		/// Gets the table definition for this row.
		/// </summary>
		/// <remarks>A Row always has a TableDefinition, even if the Row does not belong to a Table.</remarks>
		/// <value>TableDefinition for Row.</value>
		public TableDefinition TableDefinition
		{
			get { return (null == this.table) ? this.tableDef : this.table.Definition; }
		}

		/// <summary>
		/// Gets the fields contained by this row.
		/// </summary>
		/// <value>Array of field objects</value>
		public Field[] Fields
		{
			get { return this.fields; }
		}

		/// <summary>
		/// Gets or sets if this row is unreal (virtual).
		/// </summary>
		/// <value>Flag if row is unreal.</value>
		public bool IsUnreal
		{
			get { return this.unreal; }
			set { this.unreal = value; }
		}

		/// <summary>
		/// Gets the symbol that represents this row.
		/// </summary>
		/// <value>null if Row has no symbol colums, or the Symbol that represents this Row otherwise.</value>
		public Symbol Symbol
		{
			get
			{
				if (this.hasSymbol && null == this.symbol)
				{
					this.symbol = new Symbol(this);
				}

				return this.symbol;
			}
		}

		/// <summary>
		/// Gets or sets the value of a particular field in the row.
		/// </summary>
		/// <param name="field">field index.</param>
		/// <value>Value of a field in the row.</value>
		public object this[int field]
		{
			get { return this.fields[field].Data; }
			set { this.fields[field].Data = value; }
		}

		/// <summary>
		/// Returns true if the specified field is null or an empty string.
		/// </summary>
		/// <param name="field">Index of the field to check.</param>
		/// <returns>true if the specified field is null or an empty string, false otherwise.</returns>
		public bool IsColumnEmpty(int field)
		{
			if (this.fields[field].Data == null)
			{
				return true;
			}

			string dataString = this.fields[field].Data as string;
			if (dataString != null && 0 == dataString.Length)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Tests if the passed in row is identical.
		/// </summary>
		/// <param name="row">Row to compare against.</param>
		/// <returns>True if two rows are identical.</returns>
		public bool IsIdentical(Row row)
		{
			bool identical = (this.TableDefinition.Name == row.TableDefinition.Name && this.fields.Length == row.fields.Length);

			for (int i = 0; identical && i < this.fields.Length; ++i)
			{
				if (!(this.fields[i].IsIdentical(row.fields[i])))
				{
					identical = false;
				}
			}

			return identical;
		}

		/// <summary>
		/// Gets or sets the value of a particular field by name in the row.
		/// </summary>
		/// <param name="fieldColumnName">column name of field.</param>
		/// <returns>Value of a field by name in the row.</returns>
		public object GetData(string fieldColumnName)
		{
			for (int i = 0; i < this.fields.Length; ++i)
			{
				if (this.fields[i].Name == fieldColumnName)
				{
					return this.fields[i].Data;
				}
			}

			throw new ApplicationException(String.Format("Unknown field name: {0}", fieldColumnName));
		}

		/// <summary>
		/// Sets the value of a particular field.
		/// </summary>
		/// <param name="fieldColumnName">Column name of field.</param>
		/// <param name="data">Data to place into field.</param>
		public void SetData(string fieldColumnName, object data)
		{
			for (int i = 0; i < this.fields.Length; ++i)
			{
				if (this.fields[i].Name == fieldColumnName)
				{
					this.fields[i].Data = data;
					return;
				}
			}

			throw new ApplicationException(String.Format("Unknown field name: {0}", fieldColumnName));
		}

		/// <summary>
		/// Creates a Row from the XmlReader
		/// </summary>
		/// <param name="reader">Reader to get data from.</param>
		/// <param name="section">Section the row is added to.</param>
		/// <param name="tableDef">Table definition for this row.</param>
		/// <returns>New row object.</returns>
		internal static Row Parse(XmlReader reader, Section section, TableDefinition tableDef)
		{
			Debug.Assert("tuple" == reader.LocalName);

			bool empty = reader.IsEmptyElement;
			string sectionId = null;
			SourceLineNumberCollection sourceLineNumbers = null;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "sectionId":
						sectionId = reader.Value;
						break;
					case "sourceLineNumber":
						sourceLineNumbers = new SourceLineNumberCollection(reader.Value);
						break;
					default:
						throw new WixParseException(String.Format("The tuple element contains an unexpected attribute {0}.", reader.Name));
				}
			}

			Row row = Common.CreateRowInSection(sourceLineNumbers, section, tableDef);
			row.sectionId = sectionId;

			// loop through all the fields in a row
			if (!empty)
			{
				bool done = false;
				int field = 0;

				// loop through all the fields in a row
				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
						switch (reader.LocalName)
						{
							case "field":
								if (row.Fields.Length <= field)
								{
									throw new WixParseException(String.Format("This tuple has more fields for table '{0}' than are defined. This is potentially because a standard table is being redefined as a custom table.", tableDef.Name));
								}
								row[field] = Field.Parse(reader);
								++field;
								break;
							default:
								throw new WixParseException(String.Format("The tuple element contains an unexpected child element {0}.", reader.Name));
						}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixParseException("Missing end element while processing the tuple element.");
				}
			}

			return row;
		}

		/// <summary>
		/// Persists a row in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the Row should persist itself as XML.</param>
		internal void Persist(XmlWriter writer)
		{
			writer.WriteStartElement("tuple");
			if (null != this.sectionId)
			{
				writer.WriteAttributeString("sectionId", this.sectionId);
			}
			if (null != this.sourceLineNumbers)
			{
				writer.WriteAttributeString("sourceLineNumber", this.sourceLineNumbers.EncodedSourceLineNumbers);
			}

			for (int i = 0; i < this.fields.Length; ++i)
			{
				this.fields[i].Persist(writer);
			}

			writer.WriteEndElement();
		}
	}
}
