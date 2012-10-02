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
// File: ImageContent.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for ImageContent.
	/// </summary>
	/// <remarks>
	/// IxCoreContentControl includes IxCoreColleague now,
	/// so only IxCoreContentControl needs to be declared here.
	/// </remarks>
	public class ImageContent : XCoreUserControl, IxCoreContentControl
	{
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label imagePath;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ImageContent"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ImageContent()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			base.AccNameDefault = "ImageContent";	// default accessibility name
		}

		//IxCoreColleague
		public void Init(Mediator mediator,  XmlNode configurationParameters)
		{
			CheckDisposed();

			base.m_configurationParameters = configurationParameters;	// save for acc info
			string path = XmlUtils.GetManditoryAttributeValue(configurationParameters, "imagePath");
			 path = mediator.GetRealPath(path);
			if(System.IO.File.Exists(path))
				this.pictureBox1.Image  = Image.FromFile(FileUtils.ActualFilePath(path));
			else
			{
				imagePath.Text=path;
			}//throw new ConfigurationException("Could not find this file", configurationParameters);

		}

		//IxCoreColleague
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[]{};
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// Mediator message handling Priority
		/// </summary>
		public int Priority
		{
			get { return (int)ColleaguePriority.Low; }
		}


		public string AreaName
		{
			get
			{
				CheckDisposed();
				return "unknown";
			}
		}

		//IxCoreContentControl
		public bool PrepareToGoAway()
		{
			CheckDisposed();

			return true;
		}

		//IxCoreCtrlTabProvider
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			targetCandidates.Add(this);

			return ContainsFocus ? this : null;
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
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ImageContent));
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.imagePath = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// pictureBox1
			//
			this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(0, 0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(432, 400);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			//
			// imagePath
			//
			this.imagePath.Location = new System.Drawing.Point(56, 264);
			this.imagePath.Name = "imagePath";
			this.imagePath.Size = new System.Drawing.Size(224, 23);
			this.imagePath.TabIndex = 1;
			this.imagePath.Text = "imagePath";
			//
			// ImageContent
			//
			this.Controls.Add(this.imagePath);
			this.Controls.Add(this.pictureBox1);
			this.Name = "ImageContent";
			this.Size = new System.Drawing.Size(432, 400);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
