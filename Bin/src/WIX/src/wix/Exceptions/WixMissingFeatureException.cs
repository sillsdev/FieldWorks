//-------------------------------------------------------------------------------------------------
// <copyright file="WixMissingFeatureException.cs" company="Microsoft">
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
// Exception for components that are not in features (but need to be).
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Exception for components that are not in features (but need to be).
	/// </summary>
	public class WixMissingFeatureException : WixException
	{
		private FeatureBacklink blink;

		/// <summary>
		/// Instantiate a new WixPreprocessorException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line number trace to where the exception occured.</param>
		/// <param name="blink">FeatureBacklink that is missing a feature to link with.</param>
		public WixMissingFeatureException(SourceLineNumberCollection sourceLineNumbers, FeatureBacklink blink) :
			base(sourceLineNumbers, WixExceptionType.MissingFeature)
		{
			this.blink = blink;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return String.Format("Component '{0}' is not assigned to a feature.  The component's {1} '{2}' requires it to be assigned to a feature.", this.blink.Component, this.blink.Type.ToString(), this.blink.Target); }
		}
	}
}
