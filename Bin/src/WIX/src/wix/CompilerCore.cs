//-------------------------------------------------------------------------------------------------
// <copyright file="CompilerCore.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
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
// The base compiler extension.  Any of these methods can be overridden to change
// the behavior of the compiler.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using System.Xml;
	using System.Xml.Schema;

	/// <summary>
	/// Yes/no type (kinda like a boolean).
	/// </summary>
	public enum YesNoType
	{
		/// <summary>Value not set; equivalent to null for reference types.</summary>
		NotSet,
		/// <summary>The no value.</summary>
		No,
		/// <summary>The yes value.</summary>
		Yes,
		/// <summary>Not a valid yes or no value.</summary>
		IllegalValue,
	}

	/// <summary>
	/// Yes, No, Default xml simple type.
	/// </summary>
	public enum YesNoDefaultType
	{
		/// <summary>Value not set; equivalent to null for reference types.</summary>
		NotSet,
		/// <summary>The default value.</summary>
		Default,
		/// <summary>The no value.</summary>
		No,
		/// <summary>The yes value.</summary>
		Yes,
		/// <summary>Not a valid yes, no or default value.</summary>
		IllegalValue,
	}

	/// <summary>
	/// Core class for the compiler.
	/// </summary>
	public class CompilerCore : IExtensionMessageHandler, IMessageHandler
	{
		public const int IntegerNotSet = int.MinValue;
		public const int IllegalInteger = int.MinValue + 1;
		public const long LongNotSet = long.MinValue;
		public const long IllegalLong = long.MinValue + 1;
		public const string IllegalEmptyAttributeValue = "";
		public const string IllegalGuid = "IllegalGuid";
		public const string IllegalIdentifier = "";
		public static readonly Version IllegalVersion = new Version(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);
		public const string LegalShortFilenameCharacters = @"[^\\\?|><:/\*""\+,;=\[\] ]"; // illegal: \ ? | > < : / * " + , ; = [ ] (space)
		public const string LegalLongFilenameCharacters = @"[^\\\?|><:/\*""]"; // illegal: \ ? | > < : / * "

		public static readonly Regex AmbiguousFilename = new Regex(@"^.{6}\~\d", RegexOptions.Compiled);
		public static readonly Regex LegalIdentifierCharacters = new Regex(@"^[_A-Za-z][0-9A-Za-z_\.]*$", RegexOptions.Compiled);
		public static readonly Regex LegalShortFilename = new Regex(String.Concat("^", LegalShortFilenameCharacters, @"{1,8}(\.", LegalShortFilenameCharacters, "{0,3})?$"), RegexOptions.Compiled);
		public static readonly Regex LegalLongFilename = new Regex(String.Concat("^", LegalLongFilenameCharacters, @"{1,259}$"), RegexOptions.Compiled);
		public static readonly Regex LegalLocIdentifier = new Regex(@"^\$\(loc\.[_A-Za-z][0-9A-Za-z_]*\)$", RegexOptions.Compiled);

		private TableDefinitionCollection tableDefinitions;
		private Intermediate intermediate;
		private PedanticLevel pedanticLevel;

		private Section activeSection;
		private bool encounteredError;

		/// <summary>
		/// Constructor for all compiler core.
		/// </summary>
		/// <param name="intermediate">The Intermediate object representing compiled source document.</param>
		/// <param name="tableDefinitions">The loaded table definition collection.</param>
		/// <param name="messageHandler">The message handler.</param>
		internal CompilerCore(Intermediate intermediate, TableDefinitionCollection tableDefinitions, MessageEventHandler messageHandler)
		{
			this.tableDefinitions = tableDefinitions;
			this.intermediate = intermediate;
			this.MessageHandler = messageHandler;
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		private event MessageEventHandler MessageHandler;

		/// <summary>
		/// Gets whether the compiler core encoutered an error while processing.
		/// </summary>
		/// <value>Flag if core encountered and error during processing.</value>
		public bool EncounteredError
		{
			get { return this.encounteredError; }
		}

		/// <summary>
		/// Gets or sets the pedantic level.
		/// </summary>
		/// <value>The pedantic level.</value>
		public PedanticLevel PedanticLevel
		{
			get { return this.pedanticLevel; }
			set { this.pedanticLevel = value; }
		}

		/// <summary>
		/// Gets the table definitions used by the compiler core.
		/// </summary>
		/// <value>Table definition collection.</value>
		public TableDefinitionCollection TableDefinitions
		{
			get { return this.tableDefinitions; }
		}

		/// <summary>
		/// Verifies that a filename is ambiguous.
		/// </summary>
		/// <param name="filename">Filename to verify.</param>
		/// <returns>true if the filename is ambiguous; false otherwise.</returns>
		public static bool IsAmbiguousFilename(string filename)
		{
			if (null == filename || 0 == filename.Length)
			{
				return false;
			}

			return CompilerCore.AmbiguousFilename.IsMatch(filename);
		}

		/// <summary>
		/// Verifies that a value is a legal identifier.
		/// </summary>
		/// <param name="value">The value to verify.</param>
		/// <returns>true if the value is an identifier; false otherwise.</returns>
		public static bool IsIdentifier(string value)
		{
			if (null == value || 0 == value.Length)
			{
				return false;
			}

			return CompilerCore.LegalIdentifierCharacters.IsMatch(value);
		}

		/// <summary>
		/// Verifies if an identifier is a valid loc identifier.
		/// </summary>
		/// <param name="identifier">Identifier to verify.</param>
		/// <returns>True if the identifier is a valid loc identifier.</returns>
		public static bool IsValidLocIdentifier(string identifier)
		{
			if (null == identifier || 0 == identifier.Length)
			{
				return false;
			}

			return CompilerCore.LegalLocIdentifier.IsMatch(identifier);
		}

		/// <summary>
		/// Verifies if a filename is a valid short filename.
		/// </summary>
		/// <param name="filename">Filename to verify.</param>
		/// <returns>True if the filename is a valid short filename</returns>
		public static bool IsValidShortFilename(string filename)
		{
			if (null == filename || 0 == filename.Length)
			{
				return false;
			}

			return CompilerCore.LegalShortFilename.IsMatch(filename);
		}

		/// <summary>
		/// Adds a valid reference to the active section.
		/// </summary>
		/// <param name="tableName">Table name of the reference.</param>
		/// <param name="symbolId">Symbol Id of the reference.</param>
		public void AddValidReference(string tableName, string symbolId)
		{
			if (null != symbolId && 0 < symbolId.Length)
			{
				this.activeSection.References.Add(new Reference(tableName, symbolId));
			}
		}

		/// <summary>
		/// Creates a row in the active section.
		/// </summary>
		/// <param name="sourceLineNumbers">Source and line number of current row.</param>
		/// <param name="tableName">Name of table to create row in.</param>
		/// <returns>New row.</returns>
		public Row CreateRow(SourceLineNumberCollection sourceLineNumbers, string tableName)
		{
			Table table = this.activeSection.Tables[tableName];
			if (null == table)
			{
				TableDefinition tableDef = this.tableDefinitions[tableName];

				table = new Table(this.activeSection, tableDef);
				this.activeSection.Tables.Add(table);
			}

			return table.CreateRow(sourceLineNumbers);
		}

		/// <summary>
		/// Converts a DateTime value to the corresponding MSI format.
		/// </summary>
		/// <param name="date">Value to convert to MSI format.</param>
		/// <returns>Int representation of the date time.</returns>
		public int ConvertDateTimeToInteger(DateTime date)
		{
			int dateValue = CompilerCore.IntegerNotSet;

			if (DateTime.MinValue != date)
			{
				dateValue = ((((date.Year - 1980) * 512) + (date.Month * 32 + date.Day)) * 65536) +
					(date.Hour * 2048) + (date.Minute * 32) + (date.Second / 2);
			}

			return dateValue;
		}

		/// <summary>
		/// Get an attribute value and displays an error if the value is empty.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's value.</returns>
		public string GetAttributeValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			return this.GetAttributeValue(sourceLineNumbers, attribute, false);
		}

		/// <summary>
		/// Get an attribute value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <param name="canBeEmpty">If true, no error is raised on empty value. If false, an error is raised.</param>
		/// <returns>The attribute's value.</returns>
		public string GetAttributeValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool canBeEmpty)
		{
			if (!canBeEmpty && String.Empty == attribute.Value)
			{
				this.OnMessage(WixErrors.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name));

				return IllegalEmptyAttributeValue;
			}

			return attribute.Value;
		}

		/// <summary>
		/// Get an integer attribute value and displays an error for an illegal integer value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's integer value or a special value if an error occurred during conversion.</returns>
		public int GetAttributeIntegerValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				try
				{
					int integer = Convert.ToInt32(value, CultureInfo.InvariantCulture.NumberFormat);

					if (IntegerNotSet == integer || IllegalInteger == integer)
					{
						this.OnMessage(WixErrors.IntegerSentinelCollision(sourceLineNumbers, integer));
					}

					return integer;
				}
				catch (FormatException)
				{
					this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
				catch (OverflowException)
				{
					this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
			}

			return IllegalInteger;
		}

		/// <summary>
		/// Get a long integral attribute value and displays an error for an illegal long value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's long value or a special value if an error occurred during conversion.</returns>
		public long GetAttributeLongValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				try
				{
					long longValue = Convert.ToInt64(value, CultureInfo.InvariantCulture.NumberFormat);

					if (LongNotSet == longValue || IllegalLong == longValue)
					{
						this.OnMessage(WixErrors.LongSentinelCollision(sourceLineNumbers, longValue));
					}

					return longValue;
				}
				catch (FormatException)
				{
					this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
				catch (OverflowException)
				{
					this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
			}

			return IllegalLong;
		}

		/// <summary>
		/// Get a date time attribute value and display errors for illegal values.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attributes value as a DateTime.</returns>
		public DateTime GetAttributeDateTimeValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				try
				{
					DateTime date = DateTime.Parse(value, CultureInfo.InvariantCulture.DateTimeFormat);
					return date;
				}
				catch (FormatException)
				{
					this.OnMessage(WixErrors.InvalidDateTimeFormat(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
				catch (ArgumentOutOfRangeException)
				{
					this.OnMessage(WixErrors.InvalidDateTimeFormat(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
			}

			return DateTime.MinValue;
		}

		/// <summary>
		/// Get an integer attribute value or localize variable and displays an error for
		/// an illegal value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's integer value or localize variable as a string or a special value if an error occurred during conversion.</returns>
		public string GetAttributeLocalizableIntegerValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				if (value.StartsWith("$(loc.") && value.EndsWith(")"))
				{
					return value;
				}
				else
				{
					try
					{
						int integer = Convert.ToInt32(value, CultureInfo.InvariantCulture.NumberFormat);

						if (IntegerNotSet == integer || IllegalInteger == integer)
						{
							this.OnMessage(WixErrors.IntegerSentinelCollision(sourceLineNumbers, integer));
						}

						return value;
					}
					catch (FormatException)
					{
						this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
					}
					catch (OverflowException)
					{
						this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Get a guid attribute value and displays an error for an illegal guid value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <param name="generatable">Determines whether the guid can be automatically generated.</param>
		/// <returns>The attribute's guid value or a special value if an error occurred.</returns>
		public string GetAttributeGuidValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool generatable)
		{
			return this.GetAttributeGuidValue(sourceLineNumbers, attribute, generatable, false);
		}

		/// <summary>
		/// Get a guid attribute value and displays an error for an illegal guid value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <param name="generatable">Determines whether the guid can be automatically generated.</param>
		/// <param name="canBeEmpty">If true, no error is raised on empty value. If false, an error is raised.</param>
		/// <returns>The attribute's guid value or a special value if an error occurred.</returns>
		public string GetAttributeGuidValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool generatable, bool canBeEmpty)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute, canBeEmpty);

			if (String.Empty == value && canBeEmpty)
			{
				return value;
			}
			else if (IllegalEmptyAttributeValue != value)
			{
				// If the value starts and ends with braces or parenthesis, accept that and strip them off.
				if ((value.StartsWith("{") && value.EndsWith("}")) || (value.StartsWith("(") && value.EndsWith(")")))
				{
					value = value.Substring(1, value.Length - 2);
				}

				try
				{
					Guid guid;

					if (generatable && "????????-????-????-????-????????????" == value)
					{
						// this value will be substituted for a new guid in the binder
						return value;
					}
					else if ("PUT-GUID-HERE" == value)
					{
						this.OnMessage(WixErrors.ExampleGuid(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
						return IllegalGuid;
					}
					else
					{
						guid = new Guid(value);
					}

					string uppercaseGuid = guid.ToString().ToUpper(CultureInfo.InvariantCulture);

					if (PedanticLevel.Legendary == this.pedanticLevel)
					{
						if ("????????-????-????-????-????????????" != value && uppercaseGuid != value)
						{
							this.OnMessage(WixErrors.GuidContainsLowercaseLetters(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
						}
					}

					return uppercaseGuid;
				}
				catch (FormatException)
				{
					this.OnMessage(WixErrors.IllegalGuidValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
			}

			return IllegalGuid;
		}

		/// <summary>
		/// Get an identifier attribute value and displays an error for an illegal identifier value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
		public string GetAttributeIdentifierValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				if (LegalIdentifierCharacters.IsMatch(value))
				{
					if (72 < value.Length)
					{
						this.OnMessage(WixWarnings.IdentifierTooLong(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
					}

					return value;
				}
				else
				{
					this.OnMessage(WixErrors.IllegalIdentifier(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
			}

			return IllegalIdentifier;
		}

		/// <summary>
		/// Gets a yes/no value and displays an error for an illegal yes/no value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's YesNoType value.</returns>
		public YesNoType GetAttributeYesNoValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				switch (value)
				{
					case "no":
						return YesNoType.No;
					case "yes":
						return YesNoType.Yes;
					default:
						this.OnMessage(WixErrors.IllegalYesNoValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
						break;
				}
			}

			return YesNoType.IllegalValue;
		}

		/// <summary>
		/// Gets a yes/no/default value and displays an error for an illegal yes/no value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's YesNoDefaultType value.</returns>
		public YesNoDefaultType GetAttributeYesNoDefaultValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				switch (value)
				{
					case "default":
						return YesNoDefaultType.Default;
					case "no":
						return YesNoDefaultType.No;
					case "yes":
						return YesNoDefaultType.Yes;
					default:
						this.OnMessage(WixErrors.IllegalYesNoDefaultValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
						break;
				}
			}

			return YesNoDefaultType.IllegalValue;
		}

		/// <summary>
		/// Gets a short filename value and displays an error for an illegal short filename value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's short filename value.</returns>
		public string GetAttributeShortFilename(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				if (!CompilerCore.IsValidShortFilename(value) && !CompilerCore.IsValidLocIdentifier(value))
				{
					this.OnMessage(WixErrors.IllegalShortFilename(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
				else if (CompilerCore.IsAmbiguousFilename(value))
				{
					this.OnMessage(WixWarnings.AmbiguousFileOrDirectoryName(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
			}

			return value;
		}

		/// <summary>
		/// Gets a long filename value and displays an error for an illegal long filename value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's long filename value.</returns>
		public string GetAttributeLongFilename(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				if (!CompilerCore.LegalLongFilename.IsMatch(value) && !CompilerCore.IsValidLocIdentifier(value))
				{
					this.OnMessage(WixErrors.IllegalLongFilename(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
				else if (CompilerCore.IsAmbiguousFilename(value))
				{
					this.OnMessage(WixWarnings.AmbiguousFileOrDirectoryName(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
			}

			return value;
		}

		/// <summary>
		/// Gets a version value and displays an error for an illegal version value.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The attribute containing the value to get.</param>
		/// <returns>The attribute's version value.</returns>
		public Version GetAttributeVersionValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			string value = this.GetAttributeValue(sourceLineNumbers, attribute);

			if (IllegalEmptyAttributeValue != value)
			{
				try
				{
					return new Version(value);
				}
				catch (FormatException) // illegal integer in version
				{
					this.OnMessage(WixErrors.IllegalVersionValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
				catch (ArgumentException)
				{
					this.OnMessage(WixErrors.IllegalVersionValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
				}
			}

			return IllegalVersion;
		}

		/// <summary>
		/// Get an node's inner text and trims any extra whitespace.
		/// </summary>
		/// <param name="node">The node with inner text to be trimmed.</param>
		/// <returns>The node's inner text trimmed.</returns>
		public string GetTrimmedInnerText(XmlNode node)
		{
			string value = node.InnerText;
			if (0 < value.Length)
			{
				value = value.Trim();
			}

			return value;
		}

		/// <summary>
		/// Gets node's inner text and ensure's it is safe for use in a condition by trimming any extra whitespace.
		/// </summary>
		/// <param name="node">The node to ensure inner text is a condition.</param>
		/// <returns>The value converted into a safe condition.</returns>
		public string GetConditionInnerText(XmlNode node)
		{
			string value = node.InnerText;
			if (0 < value.Length)
			{
				value = value.Trim();
				value = value.Replace('\t', ' ');
				value = value.Replace('\r', ' ');
				value = value.Replace('\n', ' ');
			}

			return value;
		}

		/// <summary>
		/// Get the source line information for the current element.  The precompiler will insert
		/// special source line number processing instructions before each element that it
		/// encounters.  This is where those line numbers are read and processed.  This function
		/// may return an array of source line numbers because the element may have come from
		/// an included file, in which case the chain of imports is expressed in the array.
		/// </summary>
		/// <param name="node">Element to get source line information for.</param>
		/// <returns>Returns the stack of imports used to author the element being processed.</returns>
		public SourceLineNumberCollection GetSourceLineNumbers(XmlNode node)
		{
			if (XmlNodeType.Element != node.NodeType)   // only elements can have line numbers, sorry
			{
				return null;
			}

			SourceLineNumberCollection sourceLineNumbers = null;
			XmlNode prev = node.PreviousSibling;

			while (null != prev)
			{
				if (XmlNodeType.ProcessingInstruction == prev.NodeType)
				{
					if (Preprocessor.LineNumberElementName == prev.LocalName)   // if we have a line number
					{
						sourceLineNumbers = new SourceLineNumberCollection(prev.Value);
						break;
					}
					// otherwise keep walking up processing instructions
				}

				prev = prev.PreviousSibling;
			}

			if (null == sourceLineNumbers && null != this.intermediate.SourcePath)
			{
				sourceLineNumbers = SourceLineNumberCollection.FromFileName(this.intermediate.SourcePath);
			}

			return sourceLineNumbers;
		}

		/// <summary>
		/// Displays an unexpected attribute error if the attribute is not
		/// the namespace attribute.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information about the owner element.</param>
		/// <param name="attribute">The unexpected attribute.</param>
		public void UnexpectedAttribute(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
		{
			// ignore elements defined by the W3C because we'll assume they are always right
			if (!attribute.NamespaceURI.StartsWith("http://www.w3.org/"))
			{
				this.OnMessage(WixErrors.UnexpectedAttribute(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name));
			}
		}

		/// <summary>
		/// Display an unexepected element error.
		/// </summary>
		/// <param name="parentElement">The parent element.</param>
		/// <param name="childElement">The unexpected child element.</param>
		public void UnexpectedElement(XmlNode parentElement, XmlNode childElement)
		{
			SourceLineNumberCollection sourceLineNumbers = this.GetSourceLineNumbers(childElement);

			this.OnMessage(WixErrors.UnexpectedElement(sourceLineNumbers, parentElement.Name, childElement.Name));
		}

		/// <summary>
		/// Sends a message to the message delegate if there is one.
		/// </summary>
		/// <param name="mea">Message event arguments.</param>
		public void OnMessage(MessageEventArgs mea)
		{
			if (mea is WixError)
			{
				this.encounteredError = true;
			}
			if (null != this.MessageHandler)
			{
				this.MessageHandler(this, mea);
			}
		}

		/// <summary>
		/// Sends an error message.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="errorLevel">Level of the error message.</param>
		/// <param name="errorMessage">Error message string.</param>
		public void OnExtensionError(SourceLineNumberCollection sourceLineNumbers, ErrorLevel errorLevel, string errorMessage)
		{
			this.OnMessage(WixErrors.CompilerExtensionError(sourceLineNumbers, errorLevel, errorMessage));
		}

		/// <summary>
		/// Sends a warning message.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="warningLevel">Level of the warning message.</param>
		/// <param name="warningMessage">Warning message string.</param>
		public void OnExtensionWarning(SourceLineNumberCollection sourceLineNumbers, WarningLevel warningLevel, string warningMessage)
		{
			this.OnMessage(WixWarnings.CompilerExtensionWarning(sourceLineNumbers, warningLevel, warningMessage));
		}

		/// <summary>
		/// Sends a verbose message.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="verboseLevel">Level of the verbose message.</param>
		/// <param name="verboseMessage">Verbose message string.</param>
		public void OnExtensionVerbose(SourceLineNumberCollection sourceLineNumbers, VerboseLevel verboseLevel, string verboseMessage)
		{
			this.OnMessage(WixVerboses.CompilerExtensionVerbose(sourceLineNumbers, verboseLevel, verboseMessage));
		}

		/// <summary>
		/// Creates a new section and makes it the active section in the core.
		/// </summary>
		/// <param name="id">Unique identifier for the section.</param>
		/// <param name="type">Type of section to create.</param>
		/// <param name="codepage">Codepage for the resulting database for this ection.</param>
		/// <returns>New section.</returns>
		internal Section CreateActiveSection(string id, SectionType type, int codepage)
		{
			Section newSection = new Section(this.intermediate, id, type, codepage);

			this.intermediate.Sections.Add(newSection);
			this.activeSection = newSection;

			return newSection;
		}

		/// <summary>
		/// Adds a feature backlink to the active section.
		/// </summary>
		/// <param name="featureBacklink">Backlink to feature to add.</param>
		internal void AddFeatureBacklink(FeatureBacklink featureBacklink)
		{
			this.activeSection.FeatureBacklinks.Add(featureBacklink);
		}

		/// <summary>
		/// Adds a complex reference to the active section.
		/// </summary>
		/// <param name="complexReference">Complex reference to add.</param>
		internal void AddComplexReference(ComplexReference complexReference)
		{
			this.activeSection.ComplexReferences.Add(complexReference);
		}

		/// <summary>
		/// Adds an ignore modularization object to the active section.
		/// </summary>
		/// <param name="ignoreModularization">Ingore modulatization object to add.</param>
		internal void AddIgnoreModularization(IgnoreModularization ignoreModularization)
		{
			this.activeSection.IgnoreModularizations.Add(ignoreModularization);
		}
	}
}
