//-------------------------------------------------------------------------------------------------
// <copyright file="DecompilerExtension.cs" company="Microsoft">
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
// The base decompiler extension.  Any of these methods can be overridden to change
// the behavior of the decompiler.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Xml;
	using System.Xml.Schema;

	/// <summary>
	/// Base class for creating a decompiler extension.
	/// </summary>
	public abstract class DecompilerExtension
	{
		private ExtensionMessages messages;
		private DecompilerCore decompilerCore;

		/// <summary>
		/// Gets or sets the extension messages object.
		/// </summary>
		/// <value>Wrapper object to use when sending messages.</value>
		public ExtensionMessages Messages
		{
			get { return this.messages; }
			set { this.messages = value; }
		}

		/// <summary>
		/// Gets or sets the compiler core for the extension.
		/// </summary>
		/// <value>Compiler core for the extension.</value>
		public DecompilerCore Core
		{
			get { return this.decompilerCore; }
			set { this.decompilerCore = value; }
		}

		/// <summary>
		/// Called at the beginning of the decompilation of a database
		/// </summary>
		public virtual void InitializeDecompile()
		{
		}

		/// <summary>
		/// Called at the end of the decompilation of a database
		/// </summary>
		public virtual void FinalizeDecompile()
		{
		}

		/// <summary>
		/// Called after base decompiler has completed all the tables it meant to process.
		/// </summary>
		public virtual void ProcessOtherTables()
		{
		}

		/// <summary>
		/// Called after base decompiler has completed all the attributes it intends to process.
		/// </summary>
		/// <param name="elementName">Name of the element to process.</param>
		/// <param name="identifierName">Identifier of the element.</param>
		public virtual void ExtendAttributesOfElement(string elementName, string identifierName)
		{
		}

		/// <summary>
		/// Called after base decompiler has completed all the child elements it intends to process
		/// </summary>
		/// <param name="elementName">Name of the element to process.</param>
		/// <param name="identifierName">Identifier of the element.</param>
		public virtual void ExtendChildrenOfElement(string elementName, string identifierName)
		{
		}
	}
}
