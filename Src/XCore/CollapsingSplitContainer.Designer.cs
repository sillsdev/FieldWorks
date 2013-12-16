// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CollapsingSplitContainer.cs
// Responsibility: Randy
//
// <remarks>
// </remarks>

using System.Diagnostics.CodeAnalysis;
namespace XCore
{
	partial class CollapsingSplitContainer
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();

				if (m_expandSplitterIcons != null)
				{
					for (int i = 0; i < m_expandSplitterIcons.Length; ++i)
					{
						if (m_expandSplitterIcons[i] != null)
						{
							m_expandSplitterIcons[i].Dispose();
							m_expandSplitterIcons[i] = null;
						}
					}
				}
				if (m_firstIconControl != null && m_firstIconControl.Parent == null)
				{
					m_firstIconControl.Dispose();
				}
				if (m_secondIconControl != null && m_secondIconControl.Parent == null)
				{
					m_secondIconControl.Dispose();
				}
				if (m_firstMainControl != null && m_firstMainControl.Parent == null)
				{
					m_firstMainControl.Dispose();
				}
				if (m_secondMainControl != null && m_secondMainControl.Parent == null)
				{
					m_secondMainControl.Dispose();
				}
			}
			m_firstIconControl = null;
			m_secondIconControl = null;
			m_firstMainControl = null;
			m_secondMainControl = null;
			m_firstLabel = null;
			m_secondLabel = null;

			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="TabStop is not implemented on Mono")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CollapsingSplitContainer));
			this.m_imageList16x16 = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// m_imageList16x16
			//
			this.m_imageList16x16.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList16x16.ImageStream")));
			this.m_imageList16x16.TransparentColor = System.Drawing.Color.Magenta;
			this.m_imageList16x16.Images.SetKeyName(0, "ExpandSplitterIcon");
			//
			// CollapsingSplitContainer
			//
			this.TabStop = false;
			this.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.OnSplitterMoved);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ImageList m_imageList16x16;

	}
}
