//-------------------------------------------------------------------------------------------------
// <copyright file="TableDefinitionCollection.cs" company="Microsoft">
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
// Hash table collection for table definitions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Diagnostics;
	using System.Xml;

	/// <summary>
	/// Hash table collection for table definitions.
	/// </summary>
	public class TableDefinitionCollection : HashCollectionBase
	{
		/// <summary>
		/// Creates a new table definition collection.
		/// </summary>
		public TableDefinitionCollection()
		{
		}

		/// <summary>
		/// Creates a table definition collection with the given core collection.
		/// </summary>
		/// <param name="collection">Collection to use in the new table definition collection.</param>
		protected TableDefinitionCollection(Hashtable collection) : base(collection)
		{
		}

		/// <summary>
		/// Gets a table definition by name.
		/// </summary>
		/// <param name="tableName">Name of table to locate.</param>
		public TableDefinition this[string tableName]
		{
			get
			{
				if (!this.collection.ContainsKey(tableName))
				{
					throw new WixMissingTableDefinitionException(tableName);
				}

				return (TableDefinition)this.collection[tableName];
			}
		}

		/// <summary>
		/// Load a table definition collection from an XmlReader.
		/// </summary>
		/// <param name="reader">Reader to get data from.</param>
		/// <returns>The TableDefinitionCollection represented by the xml.</returns>
		public static TableDefinitionCollection Load(XmlReader reader)
		{
			if (null == reader)
			{
				throw new ArgumentNullException("reader");
			}

			reader.MoveToContent();

			if ("tableDefinitions" != reader.LocalName)
			{
				throw new ApplicationException(String.Format("The xml document element was expected to be tableDefinitions, but was actually {0}.", reader.Name));
			}

			return Parse(reader);
		}

		/// <summary>
		/// Creates a shallow copy of this table definition collection.
		/// </summary>
		/// <returns>A shallow copy of this table definition collection.</returns>
		public TableDefinitionCollection Clone()
		{
			return new TableDefinitionCollection((Hashtable)this.collection.Clone());
		}

		/// <summary>
		/// Adds a table definition to the collection.
		/// </summary>
		/// <param name="tableDefinition">Table definition to add to the collection.</param>
		/// <value>Indexes by table definition name.</value>
		public void Add(TableDefinition tableDefinition)
		{
			if (null == tableDefinition)
			{
				throw new ArgumentNullException("tableDefinition");
			}

			this.collection.Add(tableDefinition.Name, tableDefinition);
		}

		/// <summary>
		/// Loads a collection of table definitions from a XmlReader in memory.
		/// </summary>
		/// <param name="reader">Reader to get data from.</param>
		/// <returns>The TableDefinitionCollection represented by the xml.</returns>
		private static TableDefinitionCollection Parse(XmlReader reader)
		{
			Debug.Assert("tableDefinitions" == reader.LocalName);

			bool empty = reader.IsEmptyElement;

			TableDefinitionCollection tableDefinitionCollection = new TableDefinitionCollection();

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
								case "tableDefinition":
									tableDefinitionCollection.Add(TableDefinition.Parse(reader));
									break;
								default:
									throw new WixParseException(String.Format("The tableDefinitions element contains an unexpected child element {0}.", reader.Name));
							}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixParseException("Missing end element while processing the tableDefinitions element.");
				}
			}

			return tableDefinitionCollection;
		}
	}
}
