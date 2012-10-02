// ------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: InformationBarButton.cs
// Responsibility: ToddJ
// Last reviewed:
//
// <remarks>Implementation of InformationBarButton</remarks>
// ------------------------------------------------------------------------------
//
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	///
	/// </summary>
	[ToolboxItem(false)]
	[Designer("SIL.FieldWorks.Common.Controls.Design.InformationBarButtonDesigner")]
	public class InformationBarButton : FwButton
	{
		private System.ComponentModel.IContainer components = null;
		private ToolTip m_toolTip;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for button on information bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InformationBarButton()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			//
			// m_toolTip
			//
			this.m_toolTip.ShowAlways = true;
			//
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make right clicks behave like left clicks.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (e.Button == MouseButtons.Right)
				OnClick(null);
		}

		/// ------------------------------------------------------------------------------------
		///<summary>
		/// Gets or sets the tooltip text for the button.
		///</summary>
		/// ------------------------------------------------------------------------------------
		[Category("Help Texts")]
		[Description("The tooltip that is displayed when the mouse hovers over this button.")]
		public string TooltipText
		{
			get
			{
				CheckDisposed();

				return m_toolTip.GetToolTip(this);
			}
			set
			{
				CheckDisposed();

				m_toolTip.SetToolTip(this, value);
			}
		}
	}
}
