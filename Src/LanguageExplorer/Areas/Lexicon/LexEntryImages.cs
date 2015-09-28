// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.Utils;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// Summary description for LexEntryImages.
	/// </summary>
	internal sealed class LexEntryImages : UserControl, IFWDisposable
	{
		/// <summary />
		public System.Windows.Forms.ImageList buttonImages;
		private System.ComponentModel.IContainer components;

		/// <summary />
		public LexEntryImages()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Design", "UseCorrectDisposeSignaturesRule",
			Justification = "Has to be protected in sealed class, since the superclass has it be protected.")]
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
			this.buttonImages.Images.SetKeyName(0, "majorEntry");
			this.buttonImages.Images.SetKeyName(1, "minorEntry");
			this.buttonImages.Images.SetKeyName(2, "subentry");
			this.buttonImages.Images.SetKeyName(3, "goToEntry");
			this.buttonImages.Images.SetKeyName(4, "reversalEntry");
			this.buttonImages.Images.SetKeyName(5, "gotoReversalEntry");
			//
			// LexEntryImages
			//
			this.Name = "LexEntryImages";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
