//-------------------------------------------------------------------------------------------------
// <copyright file="CompilerExtension.cs" company="Microsoft">
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
	using System.Xml;
	using System.Xml.Schema;

	/// <summary>
	/// Base class for creating a compiler extension.
	/// </summary>
	public abstract class CompilerExtension : SchemaExtension
	{
		private CompilerCore compilerCore;

		/// <summary>
		/// Gets or sets the compiler core for the extension.
		/// </summary>
		/// <value>Compiler core for the extension.</value>
		public CompilerCore Core
		{
			get { return this.compilerCore; }
			set { this.compilerCore = value; }
		}

		/// <summary>
		/// Called at the beginning of the compilation of a source file.
		/// </summary>
		public virtual void InitializeCompile()
		{
		}

		/// <summary>
		/// Called at the end of the compilation of a source file.
		/// </summary>
		public virtual void FinalizeCompile()
		{
		}

		/// <summary>
		/// Processes an attribute for the Compiler.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line number for the parent element.</param>
		/// <param name="parentElement">Parent element of attribute.</param>
		/// <param name="attribute">Attribute to process.</param>
		public abstract void ParseAttribute(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlAttribute attribute);

		/// <summary>
		/// Processes an element for the Compiler.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line number for the parent element.</param>
		/// <param name="parentElement">Parent element of element to process.</param>
		/// <param name="element">Element to process.</param>
		public abstract void ParseElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element);
	}
}
