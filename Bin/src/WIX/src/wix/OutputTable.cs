//-------------------------------------------------------------------------------------------------
// <copyright file="OutputTable.cs" company="Microsoft">
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
// Table in an output object.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Diagnostics;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Table in an output object.
	/// </summary>
	public class OutputTable
	{
		private TableDefinition tableDef;
		private OutputRowCollection outputRows;

		/// <summary>
		/// Creates an output table with the specified definition.
		/// </summary>
		/// <param name="tableDef">Definition for all tables in this output table.</param>
		public OutputTable(TableDefinition tableDef)
		{
			this.tableDef = tableDef;
			this.outputRows = new OutputRowCollection();
		}

		/// <summary>
		/// Gets the name of this output table.
		/// </summary>
		/// <value>Name of output table.</value>
		public string Name
		{
			get { return this.tableDef.Name; }
		}

		/// <summary>
		/// Gets the table definition for this output table.
		/// </summary>
		/// <value>Table definition of output table.</value>
		public TableDefinition TableDefinition
		{
			get { return this.tableDef; }
		}

		/// <summary>
		/// Gets the collection of output rows in this output table.
		/// </summary>
		/// <value>Output rows in output table.</value>
		public OutputRowCollection OutputRows
		{
			get { return this.outputRows; }
		}

		/// <summary>
		/// Validates the rows of this OutputTable and throws if it collides on
		/// primary keys.
		/// </summary>
		public void ValidateRows()
		{
			Hashtable primaryKeys = new Hashtable();
			foreach (OutputRow outRow in this.outputRows)
			{
				string primaryKey = String.Empty;
				int i = 0;
				ArrayList keys = new ArrayList();
				ArrayList columns = new ArrayList();
				foreach (ColumnDefinition columnDef in this.tableDef.Columns)
				{
					if (columnDef.IsPrimaryKey)
					{
						primaryKey = String.Concat(primaryKey, "|", Convert.ToString(outRow.Row.Fields[i].Data));
						keys.Add(Convert.ToString(outRow.Row.Fields[i].Data));
						columns.Add(columnDef.Name);
					}
					++i;
				}

				if (primaryKeys.Contains(primaryKey))
				{
					throw new WixDuplicatePrimaryKeyException((SourceLineNumberCollection)primaryKeys[primaryKey], this.tableDef.Name, keys, columns, null, null);
				}
				primaryKeys.Add(primaryKey, outRow.Row.SourceLineNumbers);
			}
		}

		/// <summary>
		/// Returns the table in a format usable in IDT files.
		/// </summary>
		/// <param name="moduleGuid">String containing the GUID of the Merge Module, if appropriate.</param>
		/// <param name="ignoreModularizations">Optional collection of identifers that should not be modularized.</param>
		/// <remarks>moduleGuid is expected to be null when not being used to compile a Merge Module.</remarks>
		/// <returns>null if OutputTable is unreal, or string with tab delimited field values otherwise</returns>
		public string ToIdtDefinition(string moduleGuid, IgnoreModularizationCollection ignoreModularizations)
		{
			if (this.tableDef.IsUnreal)
			{
				return null;
			}

			StringBuilder sb = new StringBuilder();

			// tack on the table header
			sb.Append(this.tableDef.ToIdtDefinition());
			foreach (OutputRow outputRow in this.outputRows)
			{
				string idtDefinition = outputRow.ToIdtDefinition(moduleGuid, ignoreModularizations);
				if (null != idtDefinition)
				{
					sb.Append(idtDefinition);
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Processes an XmlReader and builds up the output table object.
		/// </summary>
		/// <param name="reader">Reader to get data from.</param>
		/// <param name="section">Section to add loaded rows into.</param>
		/// <returns>Output table.</returns>
		internal static OutputTable Parse(XmlReader reader, Section section)
		{
			Debug.Assert("outputTable" == reader.LocalName);

			string name = null;
			bool empty = reader.IsEmptyElement;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "name":
						name = reader.Value;
						break;
					default:
						throw new WixParseException(String.Format("The outputTable element contains an unexpected attribute {0}.", reader.Name));
				}
			}
			if (null == name)
			{
				throw new WixParseException("The outputTable/@name attribute was not found; it is required.");
			}

			OutputTable outputTable = null;
			if (!empty)
			{
				bool done = false;

				// loop through all the fields in a row
				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
						switch (reader.LocalName)
						{
							case "tableDefinition":
								outputTable = new OutputTable(TableDefinition.Parse(reader));
								break;
							case "tuple":
								if (null == outputTable)
								{
									throw new WixParseException("The outputTable element is missing a tableDefinition child element.");
								}
								outputTable.outputRows.Add(new OutputRow(Row.Parse(reader, section, outputTable.tableDef)));
								break;
							default:
								throw new WixParseException(String.Format("The outputTable element contains an unexpected child element {0}.", reader.Name));
						}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixParseException("Missing end element while processing the outputTable element.");
				}
			}

			return outputTable;
		}

		/// <summary>
		/// Persists the output table in XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the output table should persist itself as XML.</param>
		internal void Persist(XmlWriter writer)
		{
			writer.WriteStartElement("outputTable");
			writer.WriteAttributeString("name", this.Name);

			this.tableDef.Persist(writer);

			foreach (OutputRow outputRow in this.outputRows)
			{
				outputRow.Row.Persist(writer);
			}

			writer.WriteEndElement();
		}
	}
}
