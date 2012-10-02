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
// File: ImageHolder.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for ImageHolder.
	/// </summary>
	public class ImageHolder : Form, IFWDisposable
	{
		public System.Windows.Forms.ImageList airportImages;
		public System.Windows.Forms.ImageList miscImages;
		private System.ComponentModel.IContainer components;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ImageHolder"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ImageHolder()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ImageHolder));
			this.airportImages = new System.Windows.Forms.ImageList(this.components);
			this.miscImages = new System.Windows.Forms.ImageList(this.components);
			//
			// airportImages
			//
			this.airportImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.airportImages.ImageSize = new System.Drawing.Size(16, 16);
			this.airportImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("airportImages.ImageStream")));
			this.airportImages.TransparentColor = System.Drawing.Color.Transparent;
			//
			// miscImages
			//
			this.miscImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.miscImages.ImageSize = new System.Drawing.Size(16, 16);
			this.miscImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("miscImages.ImageStream")));
			this.miscImages.TransparentColor = System.Drawing.Color.Transparent;
			//
			// ImageHolder
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Name = "ImageHolder";
			this.Text = "ImageHolder";

		}
		#endregion
	}
}
