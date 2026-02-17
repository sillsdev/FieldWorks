// Copyright (c) 2002-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SIL.Acknowledgements;
using SIL.Extensions;
using SIL.FieldWorks.Common.FwUtils;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FwHelpAbout implementation
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FW Help about dialog (previously HelpAboutDlg in AfDialog.cpp)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwHelpAbout : Form
	{
		#region Data members

		private System.ComponentModel.IContainer components;

		private const double BytesPerMiB = 1024 * 1024;
		private const double BytesPerGiB = 1024 * BytesPerMiB;

		private readonly string m_sAvailableMemoryFmt;
		private readonly string m_sTitleFmt;
		private readonly string m_sAvailableDiskSpaceFmt;
		private Label lblName;
		private Label edtAvailableDiskSpace;
		private Label edtAvailableMemory;
		private Label lblAppVersion;
		private Label lblAvailableDiskSpace;
		private Label lblAvailableMemory;
		private Label lblFwVersion;
		private TextBox txtCopyright;

		/// <summary>The assembly of the product-specific EXE (e.g., TE.exe or the unified FieldWorks.exe that now hosts FLEx).
		/// .Net callers should set this.</summary>
		public Assembly ProductExecutableAssembly { get; set; }
		#endregion

		#region Construction and Disposal
		/// <inheritdoc />
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
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
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
			this.edtAvailableDiskSpace = new System.Windows.Forms.Label();
			this.edtAvailableMemory = new System.Windows.Forms.Label();
			this.lblAppVersion = new System.Windows.Forms.Label();
			this.lblFwVersion = new System.Windows.Forms.Label();
			this.txtCopyright = new System.Windows.Forms.TextBox();
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
			// m_toolTip
			// 
			m_toolTip.AutomaticDelay = 100;
			m_toolTip.AutoPopDelay = 1000;
			m_toolTip.InitialDelay = 100;
			m_toolTip.ReshowDelay = 100;
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
			// lblName
			// 
			resources.ApplyResources(this.lblName, "lblName");
			this.lblName.Name = "lblName";
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
			// txtCopyright
			// 
			this.txtCopyright.BackColor = System.Drawing.Color.White;
			this.txtCopyright.Cursor = System.Windows.Forms.Cursors.SizeAll;
			resources.ApplyResources(this.txtCopyright, "txtCopyright");
			this.txtCopyright.Name = "txtCopyright";
			this.txtCopyright.ReadOnly = true;
			this.txtCopyright.TabStop = false;
			// 
			// FwHelpAbout
			// 
			this.AcceptButton = buttonOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.CancelButton = buttonOk;
			this.Controls.Add(this.txtCopyright);
			this.Controls.Add(this.lblFwVersion);
			this.Controls.Add(this.lblAppVersion);
			this.Controls.Add(this.edtAvailableMemory);
			this.Controls.Add(this.edtAvailableDiskSpace);
			this.Controls.Add(this.lblAvailableMemory);
			this.Controls.Add(this.lblAvailableDiskSpace);
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
			this.PerformLayout();

		}
		#endregion

		#region Initialization Methods
		/// <summary>
		/// When the window handle gets created we want to initialize the controls
		/// </summary>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			try
			{
				// Set the Application label to the name of the app
				var viProvider = new VersionInfoProvider(ProductExecutableAssembly, true);
				lblName.Text = viProvider.ProductName;
				lblAppVersion.Text = viProvider.ApplicationVersion;
				lblFwVersion.Text = viProvider.MajorVersion;

				Dictionary<string, AcknowledgementAttribute> acknowledgements;
				try
				{
					AppDomain.CurrentDomain.AssemblyResolve += ResolveFailedAssemblyLoadsByName;
					// List the copyright information
					acknowledgements = AcknowledgementsProvider.CollectAcknowledgements();
				}
				finally
				{
					AppDomain.CurrentDomain.AssemblyResolve -= ResolveFailedAssemblyLoadsByName;
				}
				var list = acknowledgements.Keys.ToList();
				list.Sort();
				var text = viProvider.CopyrightString + Environment.NewLine + viProvider.LicenseString + Environment.NewLine + viProvider.LicenseURL;
				foreach (var key in list)
				{
					text += "\r\n" + "\r\n" + key + "\r\n" + acknowledgements[key].Copyright + " " + acknowledgements[key].Url + " " + acknowledgements[key].LicenseUrl;
				}
				txtCopyright.Text = text;

				// Set the title bar text
				Text = string.Format(m_sTitleFmt, viProvider.ProductName);

				var strRoot = Path.GetPathRoot(Application.ExecutablePath);

				if (Platform.IsUnix)
				{
					return;
				}

				// Set the memory information
				var memStatEx = new Win32.MemoryStatusEx();
				memStatEx.dwLength = (uint)Marshal.SizeOf(memStatEx);
				Win32.GlobalMemoryStatusEx(ref memStatEx);
				edtAvailableMemory.Text = string.Format(m_sAvailableMemoryFmt, memStatEx.ullAvailPhys / BytesPerMiB, memStatEx.ullTotalPhys / BytesPerMiB);

				// Set the available disk space information.
				Win32.GetDiskFreeSpaceEx(strRoot, out _, out var lpTotalNumberOfBytes, out var lpTotalNumberOfFreeBytes);
				var gbFree = lpTotalNumberOfFreeBytes / BytesPerGiB;
				var gbTotal = lpTotalNumberOfBytes / BytesPerGiB;
				edtAvailableDiskSpace.Text = string.Format(m_sAvailableDiskSpaceFmt, gbFree, gbTotal, strRoot);
			}
			catch(Exception ex)
			{
				Console.WriteLine("HelpAbout ignoring exception: " + ex);
			}
		}

		private Assembly ResolveFailedAssemblyLoadsByName(object sender, ResolveEventArgs args)
		{
			var assemblyName = args.Name.Split(',')[0];
			var outputPath = Path.GetDirectoryName(ProductExecutableAssembly.Location);
			return Assembly.LoadFile(Path.Combine(outputPath, assemblyName) + ".dll");
		}
		#endregion

		/// <summary>
		/// Show System Monitor in Linux
		/// </summary>
		private void HandleSystemMonitorLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			var program = "gnome-system-monitor";
			using (var process = new Process().RunProcess(program, null, null))
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
