// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImageHolder.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for ImageHolder.
	/// </summary>
	public class ImageHolder : UserControl, IFWDisposable
	{
		public System.Windows.Forms.ImageList largeImages;
		public System.Windows.Forms.ImageList smallImages;
		public System.Windows.Forms.ImageList smallCommandImages;
		private System.Windows.Forms.Button button1;
		private System.ComponentModel.IContainer components;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ImageHolder"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ImageHolder()
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
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

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageHolder));
			this.largeImages = new System.Windows.Forms.ImageList(this.components);
			this.smallImages = new System.Windows.Forms.ImageList(this.components);
			this.smallCommandImages = new System.Windows.Forms.ImageList(this.components);
			this.button1 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// largeImages
			//
			this.largeImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("largeImages.ImageStream")));
			this.largeImages.TransparentColor = System.Drawing.Color.Fuchsia;
			this.largeImages.Images.SetKeyName(0, "");
			this.largeImages.Images.SetKeyName(1, "");
			this.largeImages.Images.SetKeyName(2, "");
			this.largeImages.Images.SetKeyName(3, "");
			this.largeImages.Images.SetKeyName(4, "");
			this.largeImages.Images.SetKeyName(5, "");
			//
			// smallImages
			//
			this.smallImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("smallImages.ImageStream")));
			this.smallImages.TransparentColor = System.Drawing.Color.Fuchsia;
			this.smallImages.Images.SetKeyName(0, "");
			this.smallImages.Images.SetKeyName(1, "");
			this.smallImages.Images.SetKeyName(2, "");
			this.smallImages.Images.SetKeyName(3, "");
			this.smallImages.Images.SetKeyName(4, "");
			this.smallImages.Images.SetKeyName(5, "");
			this.smallImages.Images.SetKeyName(6, "");
			this.smallImages.Images.SetKeyName(7, "");
			this.smallImages.Images.SetKeyName(8, "Insert Sub-Cat.ico");
			//
			// smallCommandImages
			//
			this.smallCommandImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.smallCommandImages.ImageSize = new System.Drawing.Size(16, 16);
			this.smallCommandImages.TransparentColor = System.Drawing.Color.Fuchsia;
			//
			// button1
			//
			this.button1.ImageIndex = 0;
			this.button1.ImageList = this.largeImages;
			this.button1.Location = new System.Drawing.Point(32, 40);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 56);
			this.button1.TabIndex = 0;
			this.button1.Text = "button1";
			this.button1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			//
			// ImageHolder
			//
			this.Controls.Add(this.button1);
			this.Name = "ImageHolder";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
