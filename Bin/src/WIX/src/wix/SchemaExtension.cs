//-------------------------------------------------------------------------------------------------
// <copyright file="SchemaExtension.cs" company="Microsoft">
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
// The base schema extension.  Any of these methods can be overridden to extend
// the wix schema processing.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Xml.Schema;

	/// <summary>
	/// Base class for creating a schema extension.
	/// </summary>
	public abstract class SchemaExtension
	{
		protected XmlSchema xmlSchema;
		protected TableDefinitionCollection tableDefinitionCollection;
		private ExtensionMessages messages;

		/// <summary>
		/// Gets and sets the object for message handling.
		/// </summary>
		/// <value>Wrapper object for sending messages.</value>
		public ExtensionMessages Messages
		{
			get { return this.messages; }
			set { this.messages = value; }
		}

		/// <summary>
		/// Gets the schema for this schema extension.
		/// </summary>
		/// <value>Schema for this schema extension.</value>
		public XmlSchema Schema
		{
			get { return this.xmlSchema; }
		}

		/// <summary>
		/// Gets the table definitions for this schema extension.
		/// </summary>
		/// <value>Table definitions for this schema extension.</value>
		public TableDefinitionCollection TableDefinitions
		{
			get { return this.tableDefinitionCollection; }
		}
	}
}
