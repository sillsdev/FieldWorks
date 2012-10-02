using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Summary description for DataTreeImages.
	/// </summary>
	public class DataTreeImages : UserControl, IFWDisposable
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DataTreeImages));
			this.nodeImages = new System.Windows.Forms.ImageList(this.components);
			//
			// nodelImages
			//
			this.nodeImages.ImageSize = new System.Drawing.Size(16, 16);
			this.nodeImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("nodelImages.ImageStream")));
			this.nodeImages.TransparentColor = System.Drawing.Color.Fuchsia;
			//
			// DataTreeImages
			//
			this.Name = "DataTreeImages";

		}
		#endregion
	}
}
