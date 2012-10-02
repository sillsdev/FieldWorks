//-------------------------------------------------------------------------------------------------
// <copyright file="PreprocessorExtension.cs" company="Microsoft">
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
// The base preprocessor extension.  Any of these methods can be overridden to change
// the behavior of the preprocessor.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.IO;

	/// <summary>
	/// Base class for creating a proprocessor extension.
	/// </summary>
	public abstract class PreprocessorExtension
	{
		private ExtensionMessages messages;
		private string type;
		private Hashtable variables;

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
		/// Gets or sets the type for the extension.
		/// </summary>
		/// <value>type for the extension.</value>
		public string Type
		{
			get { return this.type; }
			set { this.type = value; }
		}

		/// <summary>
		/// Gets or sets the Variables hash for the preprocessor.
		/// </summary>
		/// <value>variables for the preprocessor.</value>
		public Hashtable Variables
		{
			get { return this.variables; }
			set { this.variables = value; }
		}

		/// <summary>
		/// Called at the beginning of the proprocessing of a source file.
		/// </summary>
		public virtual void InitializePreprocess()
		{
		}

		/// <summary>
		/// Document preprocessing called after default preprocessing is complete but before document is validated.
		/// </summary>
		/// <param name="document">The document after preprocessing.</param>
		/// <returns>New document after extension has preprocessed it.</returns>
		public virtual StringWriter PreprocessDocument(StringWriter document)
		{
			return document;
		}

		/// <summary>
		/// Preprocesses a parameter.
		/// </summary>
		/// <param name="name">Name of parameter that matches extension.</param>
		/// <returns>The value of the parameter after processing.</returns>
		/// <remarks>By default this method will cause an error if its called.</remarks>
		public virtual string PreprocessParameter(string name)
		{
			return null;
		}

		/// <summary>
		/// Determines if the variable used in a foreach is legal.
		/// </summary>
		/// <param name="variableName">The name of the variable.</param>
		/// <returns>true if the variable is allowed; false otherwise.</returns>
		/// <remarks>
		/// We use this to lockdown the foreach loops to certain variable
		/// values since they can easily be abused to obfuscate code.
		/// </remarks>
		public virtual bool IsForeachVariableAllowed(string variableName)
		{
			return false;
		}
	}
}
