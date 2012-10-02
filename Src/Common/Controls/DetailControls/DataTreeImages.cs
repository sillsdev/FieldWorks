using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for DataTreeImages.
	/// </summary>
	public class DataTreeImages : System.Windows.Forms.UserControl
	{
		public System.Windows.Forms.ImageList nodeImages;
		private System.ComponentModel.IContainer components;

		public DataTreeImages()
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataTreeImages));
			this.nodeImages = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// nodeImages
			//
			this.nodeImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("nodeImages.ImageStream")));
			this.nodeImages.TransparentColor = System.Drawing.Color.Fuchsia;
			this.nodeImages.Images.SetKeyName(0, "");
			this.nodeImages.Images.SetKeyName(1, "");
			this.nodeImages.Images.SetKeyName(2, "");
			//
			// DataTreeImages
			//
			this.Name = "DataTreeImages";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
