// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IControlCreator.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Allows creating a control (e.g. view)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IControlCreator
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the control based on the create info.
		/// </summary>
		/// <param name="sender">The caller.</param>
		/// <param name="createInfo">The create info previously specified by the client.</param>
		/// <returns>The newly created control.</returns>
		/// ------------------------------------------------------------------------------------
		Control Create(object sender, object createInfo);
	}
}
