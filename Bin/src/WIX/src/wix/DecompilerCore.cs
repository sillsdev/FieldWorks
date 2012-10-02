//-------------------------------------------------------------------------------------------------
// <copyright file="DecompilerCore.cs" company="Microsoft">
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
// The base of the decompiler. Holds some variables used by the decompiler and extensions,
// as well as some utility methods.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using System.Xml;
	using System.Xml.Schema;
	using Microsoft.Tools.WindowsInstallerXml.Msi;

	/// <summary>
	/// Core class for the decompiler.
	/// </summary>
	public class DecompilerCore
	{
		public const string LegalShortFilenameCharacters = @"[^\\\?|><:/\*""\+,;=\[\] ]"; // illegal: \ ? | > < : / * " + , ; = [ ] (space)
		public const string LegalLongFilenameCharacters = @"[^\\\?|><:/\*""]"; // illegal: \ ? | > < : / * "

		public static readonly Regex LegalIdentifierFirstCharacter = new Regex(@"^[_A-Za-z].*$", RegexOptions.Compiled);
		public static readonly Regex LegalIdentifierNonFirstCharacter = new Regex(@"^[0-9A-Za-z_\.].*$", RegexOptions.Compiled);
		public static readonly Regex LegalIdentifierCharacters = new Regex(@"^[_A-Za-z][0-9A-Za-z_\.]*$", RegexOptions.Compiled);
		public static readonly Regex LegalShortFilename = new Regex(String.Concat("^", LegalShortFilenameCharacters, @"{1,8}(\.", LegalShortFilenameCharacters, "{0,3})?$"), RegexOptions.Compiled);
		public static readonly Regex LegalLongFilename = new Regex(String.Concat("^", LegalLongFilenameCharacters, @"{1,259}$"), RegexOptions.Compiled);
		public static readonly Regex LegalVersion = new Regex(@"^\d{1,5}(?:\.\d{1,5}){0,3}$", RegexOptions.Compiled);

		private bool encounteredError;

		/// <summary>
		/// XML writer for writing generated source
		/// </summary>
		private XmlTextWriter writer;

		/// <summary>
		/// Whether to output comments or not.
		/// </summary>
		private bool addComments;

		/// <summary>
		/// Prevent CustomTable generation for these tables.
		/// </summary>
		private Hashtable coveredTables;

		/// <summary>
		/// Constructor for decompiler core.
		/// </summary>
		/// <param name="writer">XmlTextWriter for writing generated source.</param>
		/// <param name="messageHandler">Message handler for generating messages.</param>
		internal DecompilerCore(XmlTextWriter writer, MessageEventHandler messageHandler)
		{
			this.writer = writer;
			this.MessageHandler = messageHandler;
			this.coveredTables = new Hashtable();
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		private event MessageEventHandler MessageHandler;

		/// <summary>
		/// Gets and sets whether or not to add comments to the output.
		/// </summary>
		public bool AddComments
		{
			get { return this.addComments; }
			set { this.addComments = value; }
		}

		/// <summary>
		/// Gets whether the decompiler core encoutered an error while processing.
		/// </summary>
		/// <value>Flag if core encountered an error during processing.</value>
		public bool EncounteredError
		{
			get { return this.encounteredError; }
		}

		/// <summary>
		/// Gets the xml text writer for writing generated source.
		/// </summary>
		public XmlTextWriter Writer
		{
			get { return this.writer; }
		}

		/// <summary>
		/// Verifies if a filename is a valid short filename.
		/// </summary>
		/// <param name="filename">Filename to verify.</param>
		/// <returns>True if the filename is a valid short filename</returns>
		public static bool IsValidShortFilename(string filename)
		{
			return DecompilerCore.LegalShortFilename.IsMatch(filename);
		}

		/// <summary>
		/// Gets who is processing table.
		/// </summary>
		/// <param name="tableName">The name of the table to lookup.</param>
		/// <returns>Who is processing the table.</returns>
		public string CoveredTableProcessor(string tableName)
		{
			return (string)this.coveredTables[tableName];
		}

		/// <summary>
		/// Set who is processing table, thus avoiding defaulting to CustomTable definitions
		/// </summary>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="processor">Who is processing the table.</param>
		public void SetCoveredTable(string tableName, string processor)
		{
			this.coveredTables.Add(tableName, processor);
		}

		/// <summary>
		/// Converts a MSI formatted integer into the corresponding XML date string value.
		/// </summary>
		/// <param name="msiDate">Value to convert to XML date format.</param>
		/// <returns>XML formatted string representation of the integer.</returns>
		public string ConvertMSIDateToXmlDate(string msiDate)
		{
			if (null == msiDate || 0 == msiDate.Length)
			{
				return null;
			}

			int intValue = Convert.ToInt32(msiDate);
			int dateValue = intValue / 65536;
			int timeValue = intValue % 65536;

			return String.Format(
				CultureInfo.InvariantCulture,
				"{0,4}-{1:D2}-{2:D2}T{3:D2}:{4:D2}:{5:D2}",
				(dateValue / 512) + 1980,
				(dateValue % 512) / 32,
				dateValue % 32,
				timeValue / 2048,
				(timeValue % 2048) / 32,
				(timeValue % 32) * 2);
		}

		/// <summary>
		/// Generates a valid short filename as needed.
		/// </summary>
		/// <param name="originalName">The original file name.</param>
		/// <param name="table">table from which identifier came from.</param>
		/// <param name="id">Identifier for the file.</param>
		/// <param name="columnName">Column name of the file.</param>
		/// <returns>Valid identifier.</returns>
		public string GetValidShortName(string originalName, string table, string id, string columnName)
		{
			string generatedName = originalName;
			if (0 < generatedName.Length && !DecompilerCore.LegalShortFilename.IsMatch(generatedName))
			{
				generatedName = Guid.NewGuid().ToString();
				generatedName = generatedName.Replace("-", "_");
				string generatedExt = generatedName.Substring(10, 3);
				generatedName = String.Concat(generatedName.Substring(0, 8), ".", generatedExt);
				this.OnMessage(WixWarnings.GeneratingShortName(null, WarningLevel.Major, originalName, generatedName, table, id, columnName));
			}
			return generatedName;
		}

		/// <summary>
		/// Generates a valid identifier as needed
		/// </summary>
		/// <param name="identifier">Identifier to verify.</param>
		/// <param name="table">table from which identifier came from.</param>
		/// <returns>Valid identifier</returns>
		public string GetValidIdentifier(string identifier, string table)
		{
			string generatedName = identifier;
			if (!DecompilerCore.LegalIdentifierFirstCharacter.IsMatch(generatedName))
			{
				generatedName = String.Concat("_", generatedName);
			}

			if (72 < generatedName.Length)
			{
				generatedName = generatedName.Substring(0, 72);
			}

			if (!DecompilerCore.LegalIdentifierCharacters.IsMatch(generatedName))
			{
				char[] badName = generatedName.ToCharArray();
				foreach (char test in badName)
				{
					if (!DecompilerCore.LegalIdentifierNonFirstCharacter.IsMatch(test.ToString()))
					{
						generatedName = generatedName.Replace(test.ToString(), "_");
					}
				}
			}

			if (generatedName != identifier)
			{
				this.OnMessage(WixWarnings.GeneratingIdentifier(null, WarningLevel.Major, identifier, table, generatedName));
			}

			return generatedName;
		}

		/// <summary>
		/// Helper method to avoid writing attributes with null strings
		/// </summary>
		/// <param name="name">Name of attribute to write.</param>
		/// <param name="data">Data to write into attribute.</param>
		public void WriteAttributeString(string name, string data)
		{
			this.WriteAttributeString(this.writer, null, name, data, false);
		}

		/// <summary>
		/// Helper method to avoid writing attributes with null strings
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="name">Name of attribute to write.</param>
		/// <param name="data">Data to write into attribute.</param>
		public void WriteAttributeString(XmlWriter writer, string name, string data)
		{
			this.WriteAttributeString(writer, null, name, data, false);
		}

		/// <summary>
		/// Helper method to avoid writing attributes with null strings
		/// </summary>
		/// <param name="attributeNamespace">Namespace for the attribute.</param>
		/// <param name="name">Name of attribute to write.</param>
		/// <param name="data">Data to write into attribute.</param>
		public void WriteAttributeString(string attributeNamespace, string name, string data)
		{
			this.WriteAttributeString(this.writer, attributeNamespace, name, data, false);
		}

		/// <summary>
		/// Helper method to avoid writing attributes with null strings
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="attributeNamespace">Namespace for the attribute.</param>
		/// <param name="name">Name of attribute to write.</param>
		/// <param name="data">Data to write into attribute.</param>
		/// <param name="canBeEmpty">Allow an empty attribute value to be written.</param>
		public void WriteAttributeString(XmlWriter writer, string attributeNamespace, string name, string data, bool canBeEmpty)
		{
			if (canBeEmpty || (null != data && 0 < data.Length))
			{
				// This should be a regular expression to check for every instance of $(...) in the raw data
				if (1 < data.Length && data.Substring(0, 2) == "$(")
				{
					data = String.Concat("$", data);
				}

				if (null == attributeNamespace)
				{
					writer.WriteAttributeString(name, attributeNamespace, data);
				}
				else
				{
					writer.WriteAttributeString(name, data);
				}
			}
		}

		/// <summary>
		/// Helper method to avoid writing attributes with null strings
		/// </summary>
		/// <param name="data">Data to write into attribute.</param>
		public void WriteString(string data)
		{
			this.WriteString(this.writer, data);
		}

		/// <summary>
		/// Helper method to avoid writing attributes with null strings
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="data">Data to write into attribute.</param>
		public void WriteString(XmlWriter writer, string data)
		{
			// This should be a regular expression to check for every instance of $(...) in the raw data
			if (1 < data.Length && data.Substring(0, 2) == "$(")
			{
				data = String.Concat("$", data);
			}
			writer.WriteString(data);
		}

		/// <summary>
		/// Helper method to pivot writing comments on the command line option
		/// </summary>
		/// <param name="comment">Comment to write into attribute.</param>
		public void WriteComment(string comment)
		{
			this.WriteComment(this.writer, comment);
		}

		/// <summary>
		/// Helper method to pivot writing comments on the command line option
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="comment">Comment to write into attribute.</param>
		public void WriteComment(XmlWriter writer, string comment)
		{
			// This should be a regular expression to check for every instance of $(...) in the raw data
			if (0 < comment.Length && this.addComments)
			{
				writer.WriteComment(comment);
			}
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
		/// <param name="errorLevel">Level of the error message.</param>
		/// <param name="errorMessage">Error message string.</param>
		public void OnExtensionError(ErrorLevel errorLevel, string errorMessage)
		{
			this.OnMessage(WixErrors.DecompilerExtensionError(errorLevel, errorMessage));
		}

		/// <summary>
		/// Sends a warning message.
		/// </summary>
		/// <param name="warningLevel">Level of the warning message.</param>
		/// <param name="warningMessage">Warning message string.</param>
		public void OnExtensionWarning(WarningLevel warningLevel, string warningMessage)
		{
			this.OnMessage(WixWarnings.DecompilerExtensionWarning(warningLevel, warningMessage));
		}

		/// <summary>
		/// Sends a verbose message.
		/// </summary>
		/// <param name="verboseLevel">Level of the verbose message.</param>
		/// <param name="verboseMessage">Verbose message string.</param>
		public void OnExtensionVerbose(VerboseLevel verboseLevel, string verboseMessage)
		{
			this.OnMessage(WixVerboses.DecompilerExtensionVerbose(verboseLevel, verboseMessage));
		}
	}
}
