//-------------------------------------------------------------------------------------------------
// <copyright file="WixInvalidAttributeException.cs" company="Microsoft">
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
// WiX invalid attribute exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX invalid attribute exception.
	/// </summary>
	public class WixInvalidAttributeException : WixException
	{
		private string element;
		private string id;
		private string attribute;
		private string detail;

		/// <summary>
		/// Instantiate new WixInvalidAttributeException.
		/// </summary>
		/// <param name="sourceLineNumbers">source line information of the exception.</param>
		/// <param name="elementName">Name of the element containing the invalid attribute.</param>
		/// <param name="attributeName">Name of the invalid attribute.</param>
		/// <param name="detail">Detail of the exception.</param>
		public WixInvalidAttributeException(SourceLineNumberCollection sourceLineNumbers, string elementName, string attributeName, string detail) :
			this(sourceLineNumbers, elementName, attributeName, detail, null)
		{
		}

		/// <summary>
		/// Instantiate new WixInvalidAttributeException.
		/// </summary>
		/// <param name="sourceLineNumbers">source line information of the exception.</param>
		/// <param name="elementName">Name of the element containing the invalid attribute.</param>
		/// <param name="attributeName">Name of the invalid attribute.</param>
		/// <param name="detail">Detail of the exception.</param>
		/// <param name="id">Id of the element containing the invalid attribute.</param>
		public WixInvalidAttributeException(SourceLineNumberCollection sourceLineNumbers, string elementName, string attributeName, string detail, string id) :
			base(sourceLineNumbers, WixExceptionType.InvalidAttribute)
		{
			this.element = elementName;
			this.id = id;
			this.attribute = attributeName;
			this.detail = detail;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				if (null == this.id)
				{
					return String.Format("The element: {0} attribute: {1} is invalid, detail: {2}", this.element, this.attribute, this.detail);
				}
				else
				{
					return String.Format("The element: {0}[{1}] attribute: {2} is invalid, detail: {3}", this.element, this.id, this.attribute, this.detail);
				}
			}
		}
	}
}
