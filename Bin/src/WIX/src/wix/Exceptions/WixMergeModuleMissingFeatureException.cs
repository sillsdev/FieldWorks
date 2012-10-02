//-------------------------------------------------------------------------------------------------
// <copyright file="WixMergeModuleMissingFeatureException.cs" company="Microsoft">
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
// Exception for merge modules that are not in features (but need to be).
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Exception for merge modules that are not in features (but need to be).
	/// </summary>
	public class WixMergeModuleMissingFeatureException : WixException
	{
		private string mergeId;

		/// <summary>
		/// Instantiate a new WixMergeModuleMissingFeatureException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line number trace to where the exception occured.</param>
		/// <param name="mergeId">Identifier of the merge module.</param>
		public WixMergeModuleMissingFeatureException(SourceLineNumberCollection sourceLineNumbers, string mergeId)
			: base(sourceLineNumbers, WixExceptionType.MergeModuleMissingFeature)
		{
			this.mergeId = mergeId;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return String.Format("The merge module '{0}' is not assigned to a feature.  All merge modules must be assigned to at least one feature.", this.mergeId); }
		}
	}
}
