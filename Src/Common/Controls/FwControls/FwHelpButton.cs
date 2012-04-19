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
// File: FwHelpButton.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A button that (optionally) displays a help icon in front and can automatically handle
	/// the F1 help click.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ToolboxBitmap(typeof(FwHelpButton), "resources.HelpButton.ICO")]
	[Designer("SIL.FieldWorks.Common.Controls.Design.FwHelpButtonDesigner")]
	public partial class FwHelpButton : UserControl
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwHelpButton"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwHelpButton()
		{
			InitializeComponent();
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text to display on the button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(true)]
		[Bindable(true)]
		[DefaultValue("&Help")]
		public override string Text
		{
			get { return button.Text; }
			set { button.Text = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to show the image.
		/// </summary>
		/// <value><c>true</c> to show image; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool ShowImage
		{
			get { return button.Image != null; }
			set
			{
				if (value)
				{
					ComponentResourceManager resources = new ComponentResourceManager(
						typeof(FwHelpButton));
					button.Image = ((System.Drawing.Image)(resources.GetObject("button.Image")));
					button.ImageAlign = ContentAlignment.MiddleLeft;
					button.TextImageRelation = TextImageRelation.ImageBeforeText;
				}
				else
				{
					button.Image = null;
					button.ImageAlign = ContentAlignment.MiddleCenter;
					button.TextImageRelation = TextImageRelation.Overlay;
				}
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the parent changed event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);

			if (Parent != null)
				Parent.HelpRequested += new HelpEventHandler(OnParentHelpRequested);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the F1 help request. We treat it as the user pressing the help button (i.e.
		/// us).
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="hlpevent">The <see cref="T:System.Windows.Forms.HelpEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnParentHelpRequested(object sender, HelpEventArgs hlpevent)
		{
			hlpevent.Handled = true;
			OnClick(EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the button control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void button_Click(object sender, EventArgs e)
		{
			OnClick(e);
		}
	}
}
