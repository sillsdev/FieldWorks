//-------------------------------------------------------------------------------------------------
// <copyright file="Field.cs" company="Microsoft">
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
//    Field containing data for a column in a row.
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
	/// Field containing data for a column in a row.
	/// </summary>
	public class Field
	{
		private ColumnDefinition columnDefinition;
		private object data;

		/// <summary>
		/// Creates a blank field.
		/// </summary>
		/// <param name="columnDefinition">Column definition for this field.</param>
		public Field(ColumnDefinition columnDefinition) :
			this(columnDefinition, null)
		{
		}

		/// <summary>
		/// Create a field with data.
		/// </summary>
		/// <param name="columnDefinition">Column definition for this field.</param>
		/// <param name="data">Data to put in field.</param>
		public Field(ColumnDefinition columnDefinition, object data)
		{
			this.columnDefinition = columnDefinition;
			this.data = data;
		}

		/// <summary>
		/// Gets or sets the column definition for this field.
		/// </summary>
		/// <value>Column definition.</value>
		public ColumnDefinition Column
		{
			get { return this.columnDefinition; }
			set { this.columnDefinition = value; }
		}

		/// <summary>
		/// Gets or sets the data for this field.
		/// </summary>
		/// <value>Data in the field.</value>
		public object Data
		{
			get { return this.data; }
			set { this.data = value; }
		}

		/// <summary>
		/// Name of field.
		/// </summary>
		/// <value>Name of field.</value>
		public string Name
		{
			get { return this.columnDefinition.Name; }
		}

		/// <summary>
		/// Determine if this field is identical to another field.
		/// </summary>
		/// <param name="field">The other field to compare to.</param>
		/// <returns>true if they are equal; false otherwise.</returns>
		public bool IsIdentical(Field field)
		{
			return (this.columnDefinition.Name == field.columnDefinition.Name) && (this.data.Equals(field.data));
		}

		/// <summary>
		/// Parse a field from the xml.
		/// </summary>
		/// <param name="reader">XmlReader where the intermediate is persisted.</param>
		/// <returns>Returns the value of the field.</returns>
		internal static string Parse(XmlReader reader)
		{
			Debug.Assert("field" == reader.LocalName);

			string fieldValue = null;
			bool empty = reader.IsEmptyElement;

			if (!empty)
			{
				bool done = false;

				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
							throw new WixParseException(String.Format("The field element contains an unexpected child element {0}.", reader.Name));
						case XmlNodeType.CDATA:
						case XmlNodeType.Text:
							if (0 < reader.Value.Length)
							{
								fieldValue = reader.Value;
							}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixParseException("Missing end element while processing the field element.");
				}
			}

			return fieldValue;
		}

		/// <summary>
		/// Persists a field in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the Field should persist itself as XML.</param>
		internal void Persist(XmlWriter writer)
		{
			string text;

			// convert the data to a string that will persist nicely
			if (null == this.data)
			{
				text = "";
			}
			else
			{
				text = Convert.ToString(this.data);
			}

			if (this.columnDefinition.UseCData)
			{
				writer.WriteStartElement("field");
				writer.WriteCData(text);
				writer.WriteEndElement();
			}
			else
			{
				writer.WriteElementString("field", text);
			}
		}
	}
}
