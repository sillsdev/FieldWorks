// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
