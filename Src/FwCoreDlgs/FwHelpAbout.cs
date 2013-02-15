// --------------------------------------------------------------------------------------------
#region // Copyright © 2002-2010, SIL International. All Rights Reserved.
// <copyright from='2002' to='2010' company='SIL International'>
//		Copyright © 2002-2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwHelpAbout.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using System.Diagnostics;
using System.Threading;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FwHelpAbout implementation
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FW Help about dialog (previously HelpAboutDlg in AfDialog.cpp)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwHelpAbout : Form, IFWDisposable
	{
		#region Data members

		private System.ComponentModel.IContainer components;

		private string m_sAvailableMemoryFmt;
		private string m_sTitleFmt;
		private string m_sAvailableDiskSpaceFmt;
		private string m_sProdDate = string.Empty;
		private Label lblName;
		private Label lblCopyright;
		private Label edtAvailableDiskSpace;
		private Label edtAvailableMemory;
		private Label lblAppVersion;
		private Label lblAvailableDiskSpace;
		private Label lblAvailableMemory;
		private Label lblFwVersion;
		private LinkLabel m_systemMonitorLink;

		/// <summary>The assembly of the product-specific EXE (e.g., TE.exe or FLEx.exe).
		/// .Net callers should set this.</summary>
		public Assembly ProductExecutableAssembly { get; set; }
		#endregion

		#region Construction and Disposal
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		public FwHelpAbout()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			// Cache the format strings. The control labels will be overwritten when the
			// dialog is shown.
			m_sAvailableMemoryFmt = edtAvailableMemory.Text;
			m_sTitleFmt = Text;
			m_sAvailableDiskSpaceFmt = edtAvailableDiskSpace.Text;

			if (MiscUtils.IsUnix)
			{
				// Link to System Monitor

				// Hide memory and disk usage fields and show a link to
				// Gnome System Monitor in their place.

				lblAvailableMemory.Visible = false;
				edtAvailableMemory.Visible = false;
				lblAvailableDiskSpace.Visible = false;
				edtAvailableDiskSpace.Visible = false;

				m_systemMonitorLink = new LinkLabel {
					Text = FwCoreDlgs.kstidMemoryDiskUsageInformation,
					Visible = true,
					Name = "systemMonitorLink",
					TabStop = true,
					Top = lblAvailableMemory.Top,
					Left = lblAvailableMemory.Left,
					Width = edtAvailableMemory.Right - lblAvailableMemory.Left,
				};
				m_systemMonitorLink.LinkClicked += HandleSystemMonitorLinkClicked;
				Controls.Add(m_systemMonitorLink);

				// Package information

				int oldHeight = this.Height;
				this.Height += 200;
				var packageVersionLabel = new Label { Text = "Package versions:", Top = oldHeight - 20, Width = this.Width - 10 };
				var versionInformation = new TextBox { 	Height = 200 - 30,
														Top = oldHeight,
														Multiline = true,
														ReadOnly = true,
														Width = this.Width - 10,
														ScrollBars = ScrollBars.Vertical };
				this.Controls.Add(packageVersionLabel);
				this.Controls.Add(versionInformation);

				foreach(var info in LinuxPackageUtils.FindInstalledPackages("fieldworks*"))
				{
					versionInformation.AppendText(String.Format("{0} {1} {2}", info.Key, info.Value, Environment.NewLine));
				}
			}
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

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
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
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Button buttonOk;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwHelpAbout));
			System.Windows.Forms.Label lblSILFieldWorks1;
			System.Windows.Forms.PictureBox fieldWorksIcon;
			System.Windows.Forms.ToolTip m_toolTip;
			this.lblAvailableDiskSpace = new System.Windows.Forms.Label();
			this.lblAvailableMemory = new System.Windows.Forms.Label();
			this.lblName = new System.Windows.Forms.Label();
			this.lblCopyright = new System.Windows.Forms.Label();
			this.edtAvailableDiskSpace = new System.Windows.Forms.Label();
			this.edtAvailableMemory = new System.Windows.Forms.Label();
			this.lblAppVersion = new System.Windows.Forms.Label();
			this.lblFwVersion = new System.Windows.Forms.Label();
			buttonOk = new System.Windows.Forms.Button();
			lblSILFieldWorks1 = new System.Windows.Forms.Label();
			fieldWorksIcon = new System.Windows.Forms.PictureBox();
			m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(fieldWorksIcon)).BeginInit();
			this.SuspendLayout();
			//
			// buttonOk
			//
			buttonOk.BackColor = System.Drawing.SystemColors.Control;
			buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(buttonOk, "buttonOk");
			buttonOk.Name = "buttonOk";
			m_toolTip.SetToolTip(buttonOk, resources.GetString("buttonOk.ToolTip"));
			buttonOk.UseVisualStyleBackColor = true;
			//
			// lblSILFieldWorks1
			//
			resources.ApplyResources(lblSILFieldWorks1, "lblSILFieldWorks1");
			lblSILFieldWorks1.Name = "lblSILFieldWorks1";
			//
			// fieldWorksIcon
			//
			resources.ApplyResources(fieldWorksIcon, "fieldWorksIcon");
			fieldWorksIcon.Name = "fieldWorksIcon";
			fieldWorksIcon.TabStop = false;
			//
			// lblAvailableDiskSpace
			//
			resources.ApplyResources(this.lblAvailableDiskSpace, "lblAvailableDiskSpace");
			this.lblAvailableDiskSpace.Name = "lblAvailableDiskSpace";
			//
			// lblAvailableMemory
			//
			resources.ApplyResources(this.lblAvailableMemory, "lblAvailableMemory");
			this.lblAvailableMemory.Name = "lblAvailableMemory";
			//
			// m_toolTip
			//
			m_toolTip.AutomaticDelay = 100;
			m_toolTip.AutoPopDelay = 1000;
			m_toolTip.InitialDelay = 100;
			m_toolTip.ReshowDelay = 100;
			//
			// lblName
			//
			resources.ApplyResources(this.lblName, "lblName");
			this.lblName.Name = "lblName";
			//
			// lblCopyright
			//
			resources.ApplyResources(this.lblCopyright, "lblCopyright");
			this.lblCopyright.Name = "lblCopyright";
			//
			// edtAvailableDiskSpace
			//
			this.edtAvailableDiskSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.edtAvailableDiskSpace, "edtAvailableDiskSpace");
			this.edtAvailableDiskSpace.Name = "edtAvailableDiskSpace";
			//
			// edtAvailableMemory
			//
			this.edtAvailableMemory.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.edtAvailableMemory, "edtAvailableMemory");
			this.edtAvailableMemory.Name = "edtAvailableMemory";
			//
			// lblAppVersion
			//
			resources.ApplyResources(this.lblAppVersion, "lblAppVersion");
			this.lblAppVersion.Name = "lblAppVersion";
			//
			// lblFwVersion
			//
			resources.ApplyResources(this.lblFwVersion, "lblFwVersion");
			this.lblFwVersion.Name = "lblFwVersion";
			//
			// FwHelpAbout
			//
			this.AcceptButton = buttonOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.CancelButton = buttonOk;
			this.Controls.Add(this.lblFwVersion);
			this.Controls.Add(this.lblAppVersion);
			this.Controls.Add(this.edtAvailableMemory);
			this.Controls.Add(this.edtAvailableDiskSpace);
			this.Controls.Add(this.lblAvailableMemory);
			this.Controls.Add(this.lblAvailableDiskSpace);
			this.Controls.Add(this.lblCopyright);
			this.Controls.Add(fieldWorksIcon);
			this.Controls.Add(this.lblName);
			this.Controls.Add(lblSILFieldWorks1);
			this.Controls.Add(buttonOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwHelpAbout";
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(fieldWorksIcon)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Initialization Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the window handle gets created we want to initialize the controls
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			try
			{
				// Set the Application label to the name of the app
				FwVersionInfoProvider viProvider = new FwVersionInfoProvider(ProductExecutableAssembly, true);
				lblName.Text = viProvider.ProductName;
				lblAppVersion.Text = viProvider.ApplicationVersion;
				lblFwVersion.Text = viProvider.FieldWorksVersion;
				lblCopyright.Text = viProvider.CopyrightString;

				// Set the title bar text
				Text = string.Format(m_sTitleFmt, viProvider.ProductName);

				string strRoot = Path.GetPathRoot(Application.ExecutablePath);

				// Set the memory information
				Win32.MemoryStatus ms = new Win32.MemoryStatus();
				Win32.GlobalMemoryStatus(ref ms);
				edtAvailableMemory.Text = string.Format(m_sAvailableMemoryFmt,
					ms.dwAvailPhys / 1024, ms.dwTotalPhys / 1024);

				// Set the available disk space information.
				uint cSectorsPerCluster = 0, cBytesPerSector = 0, cFreeClusters = 0,
					cTotalClusters = 0;
				Win32.GetDiskFreeSpace(strRoot, ref cSectorsPerCluster, ref cBytesPerSector,
					ref cFreeClusters, ref cTotalClusters);
				uint cbKbFree =
					(uint)(((Int64)cFreeClusters * cSectorsPerCluster * cBytesPerSector) >> 10);

				edtAvailableDiskSpace.Text =
					string.Format(m_sAvailableDiskSpaceFmt, cbKbFree, strRoot);
			}
			catch
			{
				// ignore errors
			}
		}
		#endregion

		/// <summary>
		/// Show System Monitor in Linux
		/// </summary>
		private void HandleSystemMonitorLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			var program = "gnome-system-monitor";
			using (var process = MiscUtils.RunProcess(program, null, null))
			{
				Thread.Sleep(300);
				// If gnome-system-monitor is already open, HasExited will be true with ExitCode of 0
				if (process.HasExited && process.ExitCode != 0)
					MessageBox.Show(string.Format(FwCoreDlgs.kstidUnableToStart, program));
			}
		}
	}
	#endregion
}
