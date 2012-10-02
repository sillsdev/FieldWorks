//-------------------------------------------------------------------------------------------------
// <copyright file="WixRequiredAttributeException.cs" company="Microsoft">
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
// WiX required attribute exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX required attribute exception.
	/// </summary>
	public class WixRequiredAttributeException : WixException
	{
		private string element;
		private string id;
		private string attribute;
		private string otherAttribute;
		private string otherAttributeValue;

		/// <summary>
		/// Instantiate a new WixRequiredAttributeException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="elementName">Name of the element missing a required attribute.</param>
		/// <param name="attributeName">Name of the missing required attribute.</param>
		public WixRequiredAttributeException(SourceLineNumberCollection sourceLineNumbers, string elementName, string attributeName) :
			this(sourceLineNumbers, elementName, attributeName, null, null, null)
		{
		}

		/// <summary>
		/// Instantiate a new WixRequiredAttributeException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="elementName">Name of the element missing a required attribute.</param>
		/// <param name="attributeName">Name of the missing required attribute.</param>
		/// <param name="id">Id of the element missing a required attribute.</param>
		public WixRequiredAttributeException(SourceLineNumberCollection sourceLineNumbers, string elementName, string attributeName, string id) :
			this(sourceLineNumbers, elementName, attributeName, id, null, null)
		{
		}

		/// <summary>
		/// Instantiate a new WixRequiredAttributeException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="elementName">Name of the element missing a required attribute.</param>
		/// <param name="attributeName">Name of the missing required attribute.</param>
		/// <param name="id">Id of the element missing a required attribute.</param>
		/// <param name="otherAttribute">Name of another attribute that makes this one required.</param>
		public WixRequiredAttributeException(SourceLineNumberCollection sourceLineNumbers, string elementName, string attributeName, string id, string otherAttribute) :
			this(sourceLineNumbers, elementName, attributeName, id, otherAttribute, null)
		{
		}

		/// <summary>
		/// Instantiate a new WixRequiredAttributeException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="elementName">Name of the element missing a required attribute.</param>
		/// <param name="attributeName">Name of the missing required attribute.</param>
		/// <param name="id">Id of the element missing a required attribute.</param>
		/// <param name="otherAttribute">Name of another attribute that makes this one required.</param>
		/// <param name="otherAttributeValue">Value of another attribute that makes this one required.</param>
		public WixRequiredAttributeException(SourceLineNumberCollection sourceLineNumbers, string elementName, string attributeName, string id, string otherAttribute, string otherAttributeValue) :
			base(sourceLineNumbers, WixExceptionType.RequiredAttribute)
		{
			this.element = elementName;
			this.id = id;
			this.attribute = attributeName;
			this.otherAttribute = otherAttribute;
			this.otherAttributeValue = otherAttributeValue;
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
					return String.Format("The element: {0} requires attribute: {1}", this.element, this.attribute);
				}
				else if (null == this.otherAttribute)
				{
					return String.Format("The element: {0}[{1}] requires attribute: {2}", this.element, this.id, this.attribute);
				}
				else if (null == this.otherAttributeValue)
				{
					return String.Format("The element: {0}[{1}] requires attribute: {2} when attribute: {3} is present", this.element, this.id, this.attribute, this.otherAttribute);
				}
				else
				{
					return String.Format("The element: {0}[{1}] requires attribute: {2} when attribute: {3} is '{4}'", this.element, this.id, this.attribute, this.otherAttribute, this.otherAttributeValue);
				}
			}
		}
	}
}
