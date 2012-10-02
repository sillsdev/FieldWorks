// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LexEntryImages.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexEntryImages.
	/// </summary>
	public class LexEntryImages : UserControl, IFWDisposable
	{
		public System.Windows.Forms.ImageList buttonImages;
		private System.ComponentModel.IContainer components;

		public LexEntryImages()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
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

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LexEntryImages));
			this.buttonImages = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// buttonImages
			//
			this.buttonImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("buttonImages.ImageStream")));
			this.buttonImages.TransparentColor = System.Drawing.Color.Fuchsia;
			this.buttonImages.Images.SetKeyName(0, "Create Entry.ico");
			this.buttonImages.Images.SetKeyName(1, "");
			this.buttonImages.Images.SetKeyName(2, "");
			this.buttonImages.Images.SetKeyName(3, "Find Lexical Entry.ico");
			this.buttonImages.Images.SetKeyName(4, "Create Reversal Entry.ico");
			this.buttonImages.Images.SetKeyName(5, "Find Reversal Entry.ico");
			//
			// LexEntryImages
			//
			this.Name = "LexEntryImages";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
