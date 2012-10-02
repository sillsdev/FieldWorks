#region // Copyright (c) 2002-2007, SIL International. All Rights Reserved.
// ---------------------------------------------------------------------------------------------
// <copyright from='2002' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms.Design;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of FwHelpButtonDesigner which contains additional design-time
	/// functionality for FwHelpButton.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class FwHelpButtonDesigner: ControlDesigner
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="defaultValues"></param>
		/// ------------------------------------------------------------------------------------
		public override void InitializeNewComponent(IDictionary defaultValues)
		{
			base.InitializeNewComponent(defaultValues);
			base.Control.Text = "&Help";
		}
	}
}
