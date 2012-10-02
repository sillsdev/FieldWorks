//-------------------------------------------------------------------------------------------------
// <copyright file="ColumnDefinition.cs" company="Microsoft">
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
// Definition of a table's column.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Diagnostics;
	using System.Xml;

	/// <summary>
	/// Defines MSI column types.
	/// </summary>
	public enum ColumnType
	{
		/// <summary>Unknown column type, default and invalid.</summary>
		Unknown,

		/// <summary>Column is a string.</summary>
		String,

		/// <summary>Column is a localizable string.</summary>
		Localized,

		/// <summary>Column is a number.</summary>
		Number,

		/// <summary>Column is a binary stream.</summary>
		Object,

		/// <summary>Column is a number or localized token.</summary>
		LocalizedNumber
	}

	/// <summary>
	/// Specifies if the column should be modularized.
	/// </summary>
	public enum ColumnModularizeType
	{
		/// <summary>Column should not be modularized.</summary>
		None,

		/// <summary>Column should be modularized.</summary>
		Column,

		/// <summary>When the column is an primary or foreign key to the Icon table it should be modularized special.</summary>
		Icon,

		/// <summary>When the column is a companion file it should be modularized.</summary>
		CompanionFile,

		/// <summary>Column is a condition and should be modularized.</summary>
		Condition,

		/// <summary>Special modularization type for the ControlEvent table's Argument column.</summary>
		ControlEventArgument,

		/// <summary>Any Properties in the column should be modularized.</summary>
		Property,

		/// <summary>Semi-colon list of keys, all of which need to be modularized.</summary>
		SemicolonDelimited,
	}

	/// <summary>
	/// Column validation category type
	/// </summary>
	public enum ColumnCategory
	{
		/// <summary>Unknown category, default and invalid.</summary>
		Unknown,

		/// <summary>Text category.</summary>
		Text,

		/// <summary>UpperCase category.</summary>
		UpperCase,

		/// <summary>LowerCase category.</summary>
		LowerCase,

		/// <summary>Integer category.</summary>
		Integer,

		/// <summary>DoubleInteger category.</summary>
		DoubleInteger,

		/// <summary>TimeDate category.</summary>
		TimeDate,

		/// <summary>Identifier category.</summary>
		Identifier,

		/// <summary>Property category.</summary>
		Property,

		/// <summary>Filename category.</summary>
		Filename,

		/// <summary>WildCardFilename category.</summary>
		WildCardFilename,

		/// <summary>Path category.</summary>
		Path,

		/// <summary>Paths category.</summary>
		Paths,

		/// <summary>AnyPath category.</summary>
		AnyPath,

		/// <summary>DefaultDir category.</summary>
		DefaultDir,

		/// <summary>RegPath category.</summary>
		RegPath,

		/// <summary>Formatted category.</summary>
		Formatted,

		/// <summary>Template category.</summary>
		Template,

		/// <summary>Condition category.</summary>
		Condition,

		/// <summary>Guid category.</summary>
		Guid,

		/// <summary>Version category.</summary>
		Version,

		/// <summary>Language category.</summary>
		Language,

		/// <summary>Binary category.</summary>
		Binary,

		/// <summary>CustomSource category.</summary>
		CustomSource,

		/// <summary>Cabinet category.</summary>
		Cabinet,

		/// <summary>Shortcut category.</summary>
		Shortcut
	}

	/// <summary>
	/// Definition of a table's column.
	/// </summary>
	public class ColumnDefinition
	{
		private string name;
		private ColumnType type;
		private int length;
		private bool primaryKey;
		private bool symbol;
		private bool nullable;
		private ColumnModularizeType modularize;
		private bool unreal;
		private bool localizable;

		private bool minValueSet;
		private int minValue;
		private bool maxValueSet;
		private int maxValue;
		private string keyTable;
		private bool keyColumnSet;
		private int keyColumn;
		private ColumnCategory category;
		private string possibilities;
		private string description;
		private bool escapeIdtCharacters;
		private bool useCData;

		/// <summary>
		/// Creates a new column definition.
		/// </summary>
		/// <param name="name">Name of column.</param>
		/// <param name="type">Type of column</param>
		/// <param name="length">Length of column.</param>
		/// <param name="primaryKey">If column is primary key.</param>
		/// <param name="symbol">If column is part of the symbol.</param>
		/// <param name="nullable">If column is nullable.</param>
		/// <param name="modularizeType">Type of modularization for column</param>
		/// <param name="unreal">If column is unreal/virtual.</param>
		/// <param name="localizable">If the column is localizable.</param>
		/// <param name="minValueSet">If the minimum of the value was set.</param>
		/// <param name="minValue">Minimum value for the column.</param>
		/// <param name="maxValueSet">If the maximum value was set.</param>
		/// <param name="maxValue">Maximum value for the colum.</param>
		/// <param name="keyTable">Optional name of table for foreign key.</param>
		/// <param name="keyColumnSet">If the key column was set.</param>
		/// <param name="keyColumn">Optional name of column for foreign key.</param>
		/// <param name="category">Validation category for column.</param>
		/// <param name="possibilities">Set of possible values for column.</param>
		/// <param name="description">Description of column in vaidation table.</param>
		/// <param name="escapeIdtCharacters">If characters should be escaped in IDT.</param>
		/// <param name="useCData">If whitespace should be preserved in a CDATA node.</param>
		public ColumnDefinition(string name, ColumnType type, int length, bool primaryKey, bool symbol, bool nullable, ColumnModularizeType modularizeType, bool unreal, bool localizable, bool minValueSet, int minValue, bool maxValueSet, int maxValue, string keyTable, bool keyColumnSet, int keyColumn, ColumnCategory category, string possibilities, string description, bool escapeIdtCharacters, bool useCData)
		{
			this.name = name;
			this.type = type;
			this.length = length;
			this.primaryKey = primaryKey;
			this.symbol = symbol;
			this.nullable = nullable;
			this.modularize = modularizeType;
			this.unreal = unreal;
			this.localizable = localizable;
			this.minValueSet = minValueSet;
			this.minValue = minValue;
			this.maxValueSet = maxValueSet;
			this.maxValue = maxValue;
			this.keyTable = keyTable;
			this.keyColumnSet = keyColumnSet;
			this.keyColumn = keyColumn;
			this.category = category;
			this.possibilities = possibilities;
			this.description = description;
			this.escapeIdtCharacters = escapeIdtCharacters;
			this.useCData = useCData;
		}

		/// <summary>
		/// Gets the name of the column.
		/// </summary>
		/// <value>Name of column.</value>
		public string Name
		{
			get { return this.name; }
		}

		/// <summary>
		/// Gets the type of the column.
		/// </summary>
		/// <value>Type of column.</value>
		public ColumnType Type
		{
			get { return this.type; }
		}

		/// <summary>
		/// Gets the length of the column.
		/// </summary>
		/// <value>Length of column.</value>
		public int Length
		{
			get { return this.length; }
		}

		/// <summary>
		/// Gets if the column is a primary key.
		/// </summary>
		/// <value>true if column is primary key.</value>
		public bool IsPrimaryKey
		{
			get { return this.primaryKey; }
		}

		/// <summary>
		/// Gets if the column is part of a row's symbol.
		/// </summary>
		/// <value>true if column is part of the symbol.</value>
		public bool IsSymbol
		{
			get { return this.symbol; }
		}

		/// <summary>
		/// Gets if the column is nullable.
		/// </summary>
		/// <value>true if column is nullable.</value>
		public bool IsNullable
		{
			get { return this.nullable; }
		}

		/// <summary>
		/// Gets the type of modularization for this column.
		/// </summary>
		/// <value>Column's modularization type.</value>
		public ColumnModularizeType ModularizeType
		{
			get { return this.modularize; }
		}

		/// <summary>
		/// Gets if the column is localizable. Can be because the type is localizable, or because the column
		/// was explicitly set to be so.
		/// </summary>
		/// <value>true if column is localizable.</value>
		public bool IsLocalizable
		{
			get { return this.localizable || ColumnType.Localized == this.Type || ColumnType.LocalizedNumber == this.Type; }
		}

		/// <summary>
		/// Gets if the column is unreal/virtual and should not show up in final MSI.
		/// </summary>
		/// <value>true if column is unreal.</value>
		public bool IsUnreal
		{
			get { return this.unreal;  }
		}

		/// <summary>
		/// Gets if the minimum value of the column is set.
		/// </summary>
		/// <value>true if minimum value is set.</value>
		public bool IsMinValueSet
		{
			get { return this.minValueSet; }
		}

		/// <summary>
		/// Gets the minimum value for the column, only valid if IsMinValueSet returns true.
		/// </summary>
		/// <value>Minimum value for the column.</value>
		public int MinValue
		{
			get { return this.minValue; }
		}

		/// <summary>
		/// Gets if the maximum value of the column is set.
		/// </summary>
		/// <value>true if maximum value is set.</value>
		public bool IsMaxValueSet
		{
			get { return this.maxValueSet; }
		}

		/// <summary>
		/// Gets the maximum value for the column, only valid if IsMinValueSet returns true.
		/// </summary>
		/// <value>Maximum value for the column.</value>
		public int MaxValue
		{
			get { return this.maxValue; }
		}

		/// <summary>
		/// Gets the table that has the foreign key for this column
		/// </summary>
		/// <value>Foreign key table name.</value>
		public string KeyTable
		{
			get { return this.keyTable; }
		}

		/// <summary>
		/// Gets if the key column is set.
		/// </summary>
		/// <value>True if the key column is set.</value>
		public bool IsKeyColumnSet
		{
			get { return this.keyColumnSet; }
		}

		/// <summary>
		/// Gets the foreign key column that this column refers to.
		/// </summary>
		/// <value>Foreign key column.</value>
		public int KeyColumn
		{
			get { return this.keyColumn; }
		}

		/// <summary>
		/// Gets the validation category for this column.
		/// </summary>
		/// <value>Validaiton category.</value>
		public ColumnCategory Category
		{
			get { return this.category; }
		}

		/// <summary>
		/// Gets the set of possibilities for this column.
		/// </summary>
		/// <value>Set of possibilities for this column.</value>
		public string Possibilities
		{
			get { return this.possibilities; }
		}

		/// <summary>
		/// Gets the description for this column.
		/// </summary>
		/// <value>Description of column.</value>
		public string Description
		{
			get { return this.description; }
		}

		/// <summary>
		/// Gets if characters should be escaped to fit into IDT.
		/// </summary>
		/// <value>true if data should be escaped when adding to IDT.</value>
		public bool EscapeIdtCharacters
		{
			get { return this.escapeIdtCharacters; }
		}

		/// <summary>
		/// Gets if whitespace should be preserved in a CDATA node.
		/// </summary>
		/// <value>true if whitespace should be preserved in a CDATA node.</value>
		public bool UseCData
		{
			get { return this.useCData; }
		}

		/// <summary>
		/// Gets the type of the column in IDT format.
		/// </summary>
		/// <returns>IDT format for column type.</returns>
		public string GetIdtType()
		{
			char typeCharacter;
			switch (this.type)
			{
				case ColumnType.Number:
				case ColumnType.LocalizedNumber:
					typeCharacter = this.nullable ? 'I' : 'i';
					break;
				case ColumnType.String:
					typeCharacter = this.nullable ? 'S' : 's';
					break;
				case ColumnType.Localized:
					typeCharacter = this.nullable ? 'L' : 'l';
					break;
				case ColumnType.Object:
					typeCharacter = this.nullable ? 'V' : 'v';
					break;
				default:
					throw new ApplicationException(String.Concat("Unknown column type: ", this.type));
			}

			return String.Concat(typeCharacter, this.length);
		}

		/// <summary>
		/// Parses a column definition in a table definition.
		/// </summary>
		/// <param name="reader">Reader to get data from.</param>
		/// <returns>The ColumnDefintion represented by the Xml.</returns>
		internal static ColumnDefinition Parse(XmlReader reader)
		{
			Debug.Assert("columnDefinition" == reader.LocalName);

			string name = null;
			ColumnType type = ColumnType.Unknown;
			int length = -1;
			bool primaryKey = false;
			bool symbol = false;
			bool nullable = false;
			ColumnModularizeType modularize = ColumnModularizeType.None;
			bool unreal = false;
			bool localizable = false;

			bool minValueSet = false;
			int minValue = 0;
			bool maxValueSet = false;
			int maxValue = 0;
			string keyTable = null;
			bool keyColumnSet = false;
			int keyColumn = -1;
			ColumnCategory category = ColumnCategory.Unknown;
			string possibilities = null;
			string description = null;
			bool escapeIdtCharacters = false;
			bool useCData = false;
			bool empty = reader.IsEmptyElement;

			// parse the attributes
			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "name":
						name = reader.Value;
						break;
					case "type":
						switch (reader.Value)
						{
							case "string":
								type = ColumnType.String;
								break;
							case "localized":
								type = ColumnType.Localized;
								break;
							case "localizedNumber":
								type = ColumnType.LocalizedNumber;
								break;
							case "number":
								type = ColumnType.Number;
								break;
							case "object":
								type = ColumnType.Object;
								break;
							default:
								throw new WixParseException(String.Format("The columnDefinition/@type attribute contains an unexpected value '{0}'.", reader.Value));
						}
						break;
					case "length":
						length = Convert.ToInt32(reader.Value, 10);
						break;
					case "primaryKey":
						primaryKey = Common.IsYes(reader.Value, null, "columnDefinition", "primaryKey", name);
						break;
					case "nullable":
						nullable = Common.IsYes(reader.Value, null, "columnDefinition", "nullable", name);
						break;
					case "modularize":
						switch (reader.Value)
						{
							case "none":
								modularize = ColumnModularizeType.None;
								break;
							case "column":
								modularize = ColumnModularizeType.Column;
								break;
							case "companionFile":
								modularize = ColumnModularizeType.CompanionFile;
								break;
							case "condition":
								modularize = ColumnModularizeType.Condition;
								break;
							case "controlEventArgument":
								modularize = ColumnModularizeType.ControlEventArgument;
								break;
							case "icon":
								modularize = ColumnModularizeType.Icon;
								break;
							case "property":
								modularize = ColumnModularizeType.Property;
								break;
							case "semicolonDelimited":
								modularize = ColumnModularizeType.SemicolonDelimited;
								break;
							default:
								throw new WixParseException(String.Format("The columnDefinition/@modularize attribute contains an unexpected value '{0}'.", reader.Value));
						}
						break;
					case "symbol":
						symbol = Common.IsYes(reader.Value, null, "columnDefinition", "symbol", name);
						break;
					case "unreal":
						unreal = Common.IsYes(reader.Value, null, "columnDefinition", "unreal", name);
						break;
					case "localizable":
						localizable = Common.IsYes(reader.Value, null, "columnDefinition", "localizable", name);
						break;
					case "minValue":
						minValueSet = true;
						minValue = Convert.ToInt32(reader.Value, 10);
						break;
					case "maxValue":
						maxValueSet = true;
						maxValue = Convert.ToInt32(reader.Value, 10);
						break;
					case "keyTable":
						keyTable = reader.Value;
						break;
					case "keyColumn":
						keyColumnSet = true;
						keyColumn = Convert.ToInt32(reader.Value, 10);
						break;
					case "category":
						switch (reader.Value)
						{
							case "text":
								category = ColumnCategory.Text;
								break;
							case "upperCase":
								category = ColumnCategory.UpperCase;
								break;
							case "lowerCase":
								category = ColumnCategory.LowerCase;
								break;
							case "integer":
								category = ColumnCategory.Integer;
								break;
							case "doubleInteger":
								category = ColumnCategory.DoubleInteger;
								break;
							case "timeDate":
								category = ColumnCategory.TimeDate;
								break;
							case "identifier":
								category = ColumnCategory.Identifier;
								break;
							case "property":
								category = ColumnCategory.Property;
								break;
							case "filename":
								category = ColumnCategory.Filename;
								break;
							case "wildCardFilename":
								category = ColumnCategory.WildCardFilename;
								break;
							case "path":
								category = ColumnCategory.Path;
								break;
							case "paths":
								category = ColumnCategory.Paths;
								break;
							case "anyPath":
								category = ColumnCategory.AnyPath;
								break;
							case "defaultDir":
								category = ColumnCategory.DefaultDir;
								break;
							case "regPath":
								category = ColumnCategory.RegPath;
								break;
							case "formatted":
								category = ColumnCategory.Formatted;
								break;
							case "template":
								category = ColumnCategory.Template;
								break;
							case "condition":
								category = ColumnCategory.Condition;
								break;
							case "guid":
								category = ColumnCategory.Guid;
								break;
							case "version":
								category = ColumnCategory.Version;
								break;
							case "language":
								category = ColumnCategory.Language;
								break;
							case "binary":
								category = ColumnCategory.Binary;
								break;
							case "customSource":
								category = ColumnCategory.CustomSource;
								break;
							case "cabinet":
								category = ColumnCategory.Cabinet;
								break;
							case "shortcut":
								category = ColumnCategory.Shortcut;
								break;
							default:
								throw new WixParseException(String.Format("The columnDefinition/@category attribute contains an unexpected value '{0}'.", reader.Value));
						}
						break;
					case "set":
						possibilities = reader.Value;
						break;
					case "description":
						description = reader.Value;
						break;
					case "escapeIdtCharacters":
						escapeIdtCharacters = Common.IsYes(reader.Value, null, "columnDefinition", reader.LocalName, name);
						break;
					case "useCData":
						useCData = Common.IsYes(reader.Value, null, "columnDefinition", reader.LocalName, name);
						break;
					default:
						throw new WixParseException(String.Format("The columnDefinition element contains an unexpected attribute '{0}'.", reader.Name));
				}
			}

			// parse the child elements (there should be none)
			if (!empty)
			{
				bool done = false;

				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
							throw new WixParseException(String.Format("The columnDefinition element contains an unexpected child element {0}.", reader.Name));
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixParseException("Missing end element while processing the columnDefinition element.");
				}
			}

			return new ColumnDefinition(name, type, length, primaryKey, symbol, nullable, modularize, unreal, localizable, minValueSet, minValue, maxValueSet, maxValue, keyTable, keyColumnSet, keyColumn, category, possibilities, description, escapeIdtCharacters, useCData);
		}

		/// <summary>
		/// Persists a ColumnDefinition in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the Output should persist itself as XML.</param>
		internal void Persist(XmlWriter writer)
		{
			writer.WriteStartElement("columnDefinition", "http://schemas.microsoft.com/wix/2003/04/tables");

			writer.WriteAttributeString("name", this.name);
			switch (this.type)
			{
				case ColumnType.Localized:
					writer.WriteAttributeString("type", "localized");
					break;
				case ColumnType.LocalizedNumber:
					writer.WriteAttributeString("type", "localizedNumber");
					break;
				case ColumnType.Number:
					writer.WriteAttributeString("type", "number");
					break;
				case ColumnType.Object:
					writer.WriteAttributeString("type", "object");
					break;
				case ColumnType.String:
					writer.WriteAttributeString("type", "string");
					break;
			}
			writer.WriteAttributeString("length", this.length.ToString());
			if (this.primaryKey)
			{
				writer.WriteAttributeString("primaryKey", "yes");
			}
			if (this.nullable)
			{
				writer.WriteAttributeString("nullable", "yes");
			}

			if (this.localizable)
			{
				writer.WriteAttributeString("localizable", "yes");
			}

			switch (this.modularize)
			{
				case ColumnModularizeType.Column:
					writer.WriteAttributeString("modularize", "column");
					break;
				case ColumnModularizeType.CompanionFile:
					writer.WriteAttributeString("modularize", "companionFile");
					break;
				case ColumnModularizeType.Condition:
					writer.WriteAttributeString("modularize", "condition");
					break;
				case ColumnModularizeType.ControlEventArgument:
					writer.WriteAttributeString("modularize", "controlEventArgument");
					break;
				case ColumnModularizeType.Icon:
					writer.WriteAttributeString("modularize", "icon");
					break;
				case ColumnModularizeType.None:
					writer.WriteAttributeString("modularize", "none");
					break;
				case ColumnModularizeType.Property:
					writer.WriteAttributeString("modularize", "property");
					break;
				case ColumnModularizeType.SemicolonDelimited:
					writer.WriteAttributeString("modularize", "semicolonDelimited");
					break;
			}
			if (this.symbol)
			{
				writer.WriteAttributeString("symbol", "yes");
			}
			if (this.unreal)
			{
				writer.WriteAttributeString("unreal", "yes");
			}
			if (this.minValueSet)
			{
				writer.WriteAttributeString("minValue", this.minValue.ToString());
			}
			if (this.maxValueSet)
			{
				writer.WriteAttributeString("maxValue", this.maxValue.ToString());
			}
			writer.WriteAttributeString("keyTable", this.KeyTable);
			if (this.keyColumnSet)
			{
				writer.WriteAttributeString("keyColumn", this.keyColumn.ToString());
			}
			switch (this.category)
			{
				case ColumnCategory.AnyPath:
					writer.WriteAttributeString("category", "anyPath");
					break;
				case ColumnCategory.Binary:
					writer.WriteAttributeString("category", "binary");
					break;
				case ColumnCategory.Cabinet:
					writer.WriteAttributeString("category", "cabinet");
					break;
				case ColumnCategory.Condition:
					writer.WriteAttributeString("category", "condition");
					break;
				case ColumnCategory.CustomSource:
					writer.WriteAttributeString("category", "customSource");
					break;
				case ColumnCategory.DefaultDir:
					writer.WriteAttributeString("category", "defaultDir");
					break;
				case ColumnCategory.DoubleInteger:
					writer.WriteAttributeString("category", "doubleInteger");
					break;
				case ColumnCategory.Filename:
					writer.WriteAttributeString("category", "filename");
					break;
				case ColumnCategory.Formatted:
					writer.WriteAttributeString("category", "formatted");
					break;
				case ColumnCategory.Guid:
					writer.WriteAttributeString("category", "guid");
					break;
				case ColumnCategory.Identifier:
					writer.WriteAttributeString("category", "identifier");
					break;
				case ColumnCategory.Integer:
					writer.WriteAttributeString("category", "integer");
					break;
				case ColumnCategory.Language:
					writer.WriteAttributeString("category", "language");
					break;
				case ColumnCategory.LowerCase:
					writer.WriteAttributeString("category", "lowerCase");
					break;
				case ColumnCategory.Path:
					writer.WriteAttributeString("category", "path");
					break;
				case ColumnCategory.Paths:
					writer.WriteAttributeString("category", "paths");
					break;
				case ColumnCategory.Property:
					writer.WriteAttributeString("category", "property");
					break;
				case ColumnCategory.RegPath:
					writer.WriteAttributeString("category", "regPath");
					break;
				case ColumnCategory.Shortcut:
					writer.WriteAttributeString("category", "shortcut");
					break;
				case ColumnCategory.Template:
					writer.WriteAttributeString("category", "template");
					break;
				case ColumnCategory.Text:
					writer.WriteAttributeString("category", "text");
					break;
				case ColumnCategory.TimeDate:
					writer.WriteAttributeString("category", "timeDate");
					break;
				case ColumnCategory.UpperCase:
					writer.WriteAttributeString("category", "upperCase");
					break;
				case ColumnCategory.Version:
					writer.WriteAttributeString("category", "version");
					break;
				case ColumnCategory.WildCardFilename:
					writer.WriteAttributeString("category", "wildCardFilename");
					break;
			}
			writer.WriteAttributeString("set", this.possibilities);
			writer.WriteAttributeString("description", this.description);
			if (this.escapeIdtCharacters)
			{
				writer.WriteAttributeString("escapeIdtCharacters", "yes");
			}
			if (this.useCData)
			{
				writer.WriteAttributeString("useCData", "yes");
			}

			writer.WriteEndElement();
		}
	}
}
