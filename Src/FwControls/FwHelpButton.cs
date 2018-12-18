// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls.Design;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// A button that (optionally) displays a help icon in front and can automatically handle
	/// the F1 help click.
	/// </summary>
	[ToolboxBitmap(typeof(FwHelpButton), "resources.HelpButton.ICO")]
	[Designer(typeof(FwHelpButtonDesigner))]
	public partial class FwHelpButton : UserControl
	{
		/// <summary />
		public FwHelpButton()
		{
			InitializeComponent();
		}

		#region Properties

		/// <inheritdoc />
		[Browsable(true)]
		[Bindable(true)]
		[DefaultValue("&Help")]
		public override string Text
		{
			get { return button.Text; }
			set { button.Text = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether to show the image.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool ShowImage
		{
			get { return button.Image != null; }
			set
			{
				if (value)
				{
					var resources = new ComponentResourceManager(typeof(FwHelpButton));
					button.Image = ((Image)(resources.GetObject("button.Image")));
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

		/// <inheritdoc />
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);

			if (Parent != null)
			{
				Parent.HelpRequested += OnParentHelpRequested;
			}
		}

		/// <summary>
		/// Handle the F1 help request. We treat it as the user pressing the help button (i.e.
		/// us).
		/// </summary>
		private void OnParentHelpRequested(object sender, HelpEventArgs hlpevent)
		{
			hlpevent.Handled = true;
			OnClick(EventArgs.Empty);
		}

		/// <summary>
		/// Handles the Click event of the button control.
		/// </summary>
		private void button_Click(object sender, EventArgs e)
		{
			OnClick(e);
		}
	}
}