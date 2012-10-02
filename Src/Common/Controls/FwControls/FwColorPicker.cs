// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: FwColorPicker.cs
// Responsibility: DavidO
// Last reviewed:
//
// Implementation of FwColorPicker
//
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for FwColorPicker.
	/// </summary>
	public class FwColorPicker : UserControl, IFWDisposable
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		///
		/// </summary>
		public FwColorPicker()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitForm call

		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// FwColorPicker
			//
			this.Name = "FwColorPicker";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
