// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
	/// Summary description for MEImages.
	/// </summary>
	public class MEImages : UserControl, IFWDisposable
	{
		public System.Windows.Forms.ImageList buttonImages;
		private System.ComponentModel.IContainer components;

		public MEImages()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

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

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MEImages));
			this.buttonImages = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// buttonImages
			//
			this.buttonImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("buttonImages.ImageStream")));
			this.buttonImages.TransparentColor = System.Drawing.Color.Fuchsia;
			this.buttonImages.Images.SetKeyName(0, "");
			this.buttonImages.Images.SetKeyName(1, "Headed Comp Rule.ico");
			this.buttonImages.Images.SetKeyName(2, "non-Headed Comp Rule.ico");
			this.buttonImages.Images.SetKeyName(3, "Phoneme.ico");
			this.buttonImages.Images.SetKeyName(4, "");
			this.buttonImages.Images.SetKeyName(5, "Natural Class.ico");
			this.buttonImages.Images.SetKeyName(6, "Environment.ico");
			this.buttonImages.Images.SetKeyName(7, "adhoc Morpheme rule.ico");
			this.buttonImages.Images.SetKeyName(8, "adhoc Allomorph rule.ico");
			this.buttonImages.Images.SetKeyName(9, "adhoc Group.ico");
			this.buttonImages.Images.SetKeyName(10, "Insert Cat.ico");
			this.buttonImages.Images.SetKeyName(11, "Insert Feature.ico");
			this.buttonImages.Images.SetKeyName(12, "Insert Complex Feature.ico");
			this.buttonImages.Images.SetKeyName(13, "Exception Feature.ico");
			this.buttonImages.Images.SetKeyName(14, "Metathesis.ico");
			//
			// MEImages
			//
			this.Name = "MEImages";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
