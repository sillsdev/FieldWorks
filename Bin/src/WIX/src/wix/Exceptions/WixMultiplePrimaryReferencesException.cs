//-------------------------------------------------------------------------------------------------
// <copyright file="WixMultiplePrimaryReferencesException.cs" company="Microsoft">
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
// WiX multiple primary references exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX multiple primary references exception.
	/// </summary>
	public class WixMultiplePrimaryReferencesException : WixException
	{
		private ComplexReference cref;
		private string conflictingParentId;

		/// <summary>
		/// Instantiate a new WixMultiplePrimaryReferencesException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="cref">Complex reference with conflicting primary references.</param>
		/// <param name="conflictingParentId">Id of the comflicting parent.</param>
		internal WixMultiplePrimaryReferencesException(SourceLineNumberCollection sourceLineNumbers, ComplexReference cref, string conflictingParentId) :
			base(sourceLineNumbers, WixExceptionType.MultiplePrimaryReferences)
		{
			this.cref = cref;
			this.conflictingParentId = conflictingParentId;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return String.Format("Multiple primary references were found for {0} {1} in {2} {3} and {4}", this.cref.ChildType.ToString(), this.cref.ChildId, this.cref.ParentType.ToString(), this.cref.ParentId, this.conflictingParentId); }
		}
	}
}
