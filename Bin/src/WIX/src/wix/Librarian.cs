//-------------------------------------------------------------------------------------------------
// <copyright file="Librarian.cs" company="Microsoft">
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
// Core librarian tool.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;

	/// <summary>
	/// Core librarian tool.
	/// </summary>
	public class Librarian : IExtensionMessageHandler, IMessageHandler
	{
		private TableDefinitionCollection tableDefinitions;
		private Hashtable extensions;
		private ArrayList intermediates;
		private bool foundError;
		private ExtensionMessages extensionMessages;

		/// <summary>
		/// Instantiate a new Librarian class.
		/// </summary>
		/// <param name="smallTables">Use small table definitions for MSI/MSM.</param>
		public Librarian(bool smallTables)
		{
			this.tableDefinitions = Common.GetTableDefinitions(smallTables);
			this.extensions = new Hashtable();
			this.intermediates = new ArrayList();
			this.foundError = false;
			this.extensionMessages = new ExtensionMessages(this);
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		public event MessageEventHandler Message;

		/// <summary>
		/// Gets table definitions used by this librarian.
		/// </summary>
		/// <value>Table definitions.</value>
		public TableDefinitionCollection TableDefinitions
		{
			get { return this.tableDefinitions; }
		}

		/// <summary>
		/// Adds an extension to the librarian.
		/// </summary>
		/// <param name="extension">Schema extension to add to librarian.</param>
		public void AddExtension(SchemaExtension extension)
		{
			extension.Messages = this.extensionMessages;

			// check if this extension is addding a schema namespace that already exists
			if (this.extensions.Contains(extension.Schema.TargetNamespace))
			{
				throw new WixExtensionNamespaceConflictException(extension, (SchemaExtension)this.extensions[extension.Schema.TargetNamespace]);
			}

			// check if the extension is adding a table that already exists
			foreach (TableDefinition tableDefinition in extension.TableDefinitions)
			{
				if (this.tableDefinitions.Contains(tableDefinition.Name))
				{
					throw new WixExtensionTableDefinitionConflictException(extension, tableDefinition);
				}
			}

			// add the extension and its table definitions to the librarian
			this.extensions.Add(extension.Schema.TargetNamespace, extension);
			foreach (TableDefinition tableDefinition in extension.TableDefinitions)
			{
				this.tableDefinitions.Add(tableDefinition);
			}
		}

		/// <summary>
		/// Create a library by combining several intermediates (objects).
		/// </summary>
		/// <param name="intermediates">Intermediates to combine.</param>
		/// <returns>Returns the new library.</returns>
		public Library Combine(Intermediate[] intermediates)
		{
			Library library = new Library();

			foreach (Intermediate intermediate in intermediates)
			{
				library.Intermediates.Add(intermediate);
			}

			// check for multiple entry sections and duplicate symbols
			this.Validate(this, library);

			return (this.foundError ? null : library);
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="errorLevel">Level of the error message.</param>
		/// <param name="errorMessage">Error message string.</param>
		public void OnExtensionError(SourceLineNumberCollection sourceLineNumbers, ErrorLevel errorLevel, string errorMessage)
		{
			this.OnMessage(WixErrors.LibrarianExtensionError(sourceLineNumbers, errorLevel, errorMessage));
		}

		/// <summary>
		/// Sends a warning to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="warningLevel">Level of the warning message.</param>
		/// <param name="warningMessage">Warning message string.</param>
		public void OnExtensionWarning(SourceLineNumberCollection sourceLineNumbers, WarningLevel warningLevel, string warningMessage)
		{
			this.OnMessage(WixWarnings.LibrarianExtensionWarning(sourceLineNumbers, warningLevel, warningMessage));
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="verboseLevel">Level of the verbose message.</param>
		/// <param name="verboseMessage">Verbose message string.</param>
		public void OnExtensionVerbose(SourceLineNumberCollection sourceLineNumbers, VerboseLevel verboseLevel, string verboseMessage)
		{
			this.OnMessage(WixVerboses.LibrarianExtensionVerbose(sourceLineNumbers, verboseLevel, verboseMessage));
		}

		/// <summary>
		/// Sends a message to the message delegate if there is one.
		/// </summary>
		/// <param name="mea">Message event arguments.</param>
		public void OnMessage(MessageEventArgs mea)
		{
			if (mea is WixError)
			{
				this.foundError = true;
			}
			if (null != this.Message)
			{
				this.Message(this, mea);
			}
		}

		/// <summary>
		/// Validate that a library contains one entry section and no duplicate symbols.
		/// </summary>
		/// <param name="messageHandler">Message handler for errors.</param>
		/// <param name="library">Library to validate.</param>
		private void Validate(IMessageHandler messageHandler, Library library)
		{
			Section entrySection;
			SymbolCollection allSymbols;

			ArrayList intermediates = new ArrayList();
			SectionCollection sections = new SectionCollection();

			StringCollection referencedSymbols = new StringCollection();
			ArrayList unresolvedReferences = new ArrayList();

			intermediates.AddRange(library.Intermediates);
			Common.FindEntrySectionAndLoadSymbols((Intermediate[])intermediates.ToArray(typeof(Intermediate)), false, this, out entrySection, out allSymbols);

			foreach (Intermediate intermediate in library.Intermediates)
			{
				foreach (Section section in intermediate.Sections)
				{
					Common.ResolveReferences(OutputType.Unknown, sections, section, allSymbols, referencedSymbols, unresolvedReferences, this);
				}
			}
		}
	}
}
