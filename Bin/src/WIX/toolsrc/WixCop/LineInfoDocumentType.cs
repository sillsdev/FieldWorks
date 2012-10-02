//-------------------------------------------------------------------------------------------------
// <copyright file="LineInfoDocumentType.cs" company="Microsoft">
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
// Wrapper for XmlDocumentType that implements IXmlLineInfo.
// Originally from www.gotdotnet.com/userfiles/XMLDom/extendDOM.zip
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools
{
	using System;
	using System.Xml;

	/// <summary>
	/// Wrapper for XmlDocumentType that implements IXmlLineInfo.
	/// </summary>
	public class LineInfoDocumentType : XmlDocumentType, IXmlLineInfo
	{
		private int lineNumber = -1;
		private int linePosition = -1;

		/// <summary>
		/// Instantiate a new LineInfoDocumentType class.
		/// </summary>
		/// <param name="name">Name of the document type.</param>
		/// <param name="publicId">The public identifier of the document type or a null reference (Nothing in Visual Basic).</param>
		/// <param name="systemId">The system identifier of the document type or a null reference (Nothing in Visual Basic).</param>
		/// <param name="internalSubset">The DTD internal subset of the document type or a null reference (Nothing in Visual Basic).</param>
		/// <param name="doc">The document that owns this node.</param>
		internal LineInfoDocumentType(string name, string publicId, string systemId, string internalSubset, XmlDocument doc) : base(name, publicId, systemId, internalSubset, doc)
		{
		}

		/// <summary>
		/// Gets the line number.
		/// </summary>
		/// <value>The line number.</value>
		public int LineNumber
		{
			get { return this.lineNumber; }
		}

		/// <summary>
		/// Gets the line position.
		/// </summary>
		/// <value>The line position.</value>
		public int LinePosition
		{
			get { return this.linePosition; }
		}

		/// <summary>
		/// Set the line information for this node.
		/// </summary>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="linePosition">The line position.</param>
		public void SetLineInfo(int lineNumber, int linePosition)
		{
			this.lineNumber = lineNumber;
			this.linePosition = linePosition;
		}

		/// <summary>
		/// Determines if this node has line information.
		/// </summary>
		/// <returns>true.</returns>
		public bool HasLineInfo()
		{
			return true;
		}
	}
}
