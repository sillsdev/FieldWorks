// --------------------------------------------------------------------------------------------
#region // Copyright © 2002-2004, SIL International. All Rights Reserved.
// <copyright from='2002' to='2004' company='SIL International'>
//		Copyright © 2002-2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwHelpAbout.cs
// Responsibility: TE Team
//
// <remarks>
// Help About dialog
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region IFwHelpAbout interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Public interface (exported with COM wrapper) for the FW Help About dialog box
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[GuidAttribute("0F7EAA72-512A-4c73-AA1E-6BCE6BDFD4B4")]
	public interface IFwHelpAbout
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Shows the form as a modal dialog with the currently active form as its owner
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int ShowDialog();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product name which appears in the Name label on the about dialog box
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string ProdName
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product version which appears in the App Version label on the about dialog box
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string ProdVersion
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The integer portion of the "OLE Automation Date" for the product.  This is from the
		/// fourth (and final) field of the product version.
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.  C++ clients should set this
		/// before they set ProdVersion.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		int ProdOADate
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product version which appears in the FW Version label on the about dialog box
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string FieldworksVersion
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The drive letter whose free space will be reported in the About box.
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string DriveLetter
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The date reported in the About box after which the program stops working.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		DateTime DropDeadDate
		{
			set;
		}
	}
	#endregion

	#region FwHelpAbout implementation
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FW Help about dialog (previously HelpAboutDlg in AfDialog.cpp)
	/// </summary>
	/// <remarks>
	/// This dialog shows the registration key from HKLM\Software\SIL\FieldWorks\FwUserReg.
	/// If a DropDeadDate is to something different then 1/1/3000 it also displays the date
	/// after which the program is no longer working.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	[ProgId("FwCoreDlgs.FwHelpAbout")]
	// Key attribute to hide the "clutter" from System.Windows.Forms.Form
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("03C5DF82-A942-423d-A302-AD65B286BED6")]
	[ComVisible(true)]
	public class FwHelpAbout : Form, IFWDisposable, IFwHelpAbout
	{
		#region Data members

		private System.ComponentModel.IContainer components;

		private string m_sAvailableMemoryFmt;
		private string m_sAppVersionFmt;
		private string m_sFwVersionFmt;
		private string m_sTitleFmt;
		private string m_sAvailableDiskSpaceFmt;
		private string m_sExpirationDateLabelFmt;
		private string m_sDriveLetter;
		private string m_sProdDate = "";
		private System.Windows.Forms.Label lblName;
		private System.Windows.Forms.Label lblCopyright;
		private System.Windows.Forms.Label edtAvailableDiskSpace;
		private System.Windows.Forms.Label edtAvailableMemory;
		private System.Windows.Forms.Label lblExpirationDate;
		private System.Windows.Forms.Label lblAppVersion;
		private System.Windows.Forms.Label lblFwVersion;
		private DateTime m_dropDeadDate = new DateTime(3000, 1, 1);
		#endregion

		#region Construction and Disposal
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public FwHelpAbout()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// Cache the format strings. The control labels will be overwritten when the
			// dialog is shown.
			m_sAvailableMemoryFmt = edtAvailableMemory.Text;
			m_sAppVersionFmt = lblAppVersion.Text;
			m_sFwVersionFmt = lblFwVersion.Text;
			m_sTitleFmt = Text;
			m_sAvailableDiskSpaceFmt = edtAvailableDiskSpace.Text;
			m_sExpirationDateLabelFmt = lblExpirationDate.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dropDeadDate">Date after which the program stops working</param>
		/// ------------------------------------------------------------------------------------
		public FwHelpAbout(DateTime dropDeadDate) : this()
		{
			m_dropDeadDate = dropDeadDate;
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
			System.Windows.Forms.Label lblAvailableDiskSpace;
			System.Windows.Forms.Label lblAvailableMemory;
			System.Windows.Forms.ToolTip m_toolTip;
			this.lblName = new System.Windows.Forms.Label();
			this.lblCopyright = new System.Windows.Forms.Label();
			this.edtAvailableDiskSpace = new System.Windows.Forms.Label();
			this.edtAvailableMemory = new System.Windows.Forms.Label();
			this.lblAppVersion = new System.Windows.Forms.Label();
			this.lblExpirationDate = new System.Windows.Forms.Label();
			this.lblFwVersion = new System.Windows.Forms.Label();
			buttonOk = new System.Windows.Forms.Button();
			lblSILFieldWorks1 = new System.Windows.Forms.Label();
			fieldWorksIcon = new System.Windows.Forms.PictureBox();
			lblAvailableDiskSpace = new System.Windows.Forms.Label();
			lblAvailableMemory = new System.Windows.Forms.Label();
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
			resources.ApplyResources(lblAvailableDiskSpace, "lblAvailableDiskSpace");
			lblAvailableDiskSpace.Name = "lblAvailableDiskSpace";
			//
			// lblAvailableMemory
			//
			resources.ApplyResources(lblAvailableMemory, "lblAvailableMemory");
			lblAvailableMemory.Name = "lblAvailableMemory";
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
			// lblExpirationDate
			//
			resources.ApplyResources(this.lblExpirationDate, "lblExpirationDate");
			this.lblExpirationDate.Name = "lblExpirationDate";
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
			this.BackColor = System.Drawing.SystemColors.Window;
			this.CancelButton = buttonOk;
			this.Controls.Add(this.lblFwVersion);
			this.Controls.Add(this.lblExpirationDate);
			this.Controls.Add(this.lblAppVersion);
			this.Controls.Add(this.edtAvailableMemory);
			this.Controls.Add(this.edtAvailableDiskSpace);
			this.Controls.Add(lblAvailableMemory);
			this.Controls.Add(lblAvailableDiskSpace);
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
				Assembly assembly = Assembly.GetEntryAssembly();
				string strRoot;
				object[] attributes;
				if (assembly != null)
				{
					attributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
					string productName = (attributes != null && attributes.Length > 0) ?
						((AssemblyTitleAttribute)attributes[0]).Title : Application.ProductName;
					lblName.Text = productName;

					// Set the application version text
					attributes = assembly.GetCustomAttributes(
						typeof(AssemblyFileVersionAttribute), false);
					string appVersion = (attributes != null && attributes.Length > 0) ?
						((AssemblyFileVersionAttribute)attributes[0]).Version :
						Application.ProductVersion;
					// Extract the fourth (and final) field of the version to get a date value.
					int ich = appVersion.IndexOf('.');
					if (ich >= 0)
						ich = appVersion.IndexOf('.', ich + 1);
					if (ich >= 0)
						ich = appVersion.IndexOf('.', ich + 1);
					if (ich >= 0)
					{
						int iDate = System.Convert.ToInt32( appVersion.Substring(ich + 1) );
						if (iDate > 0)
							(this as IFwHelpAbout).ProdOADate = iDate;
					}
#if DEBUG
					lblAppVersion.Text = string.Format(m_sAppVersionFmt, appVersion, m_sProdDate, "(Debug version)");
#else
					lblAppVersion.Text = string.Format(m_sAppVersionFmt, appVersion, m_sProdDate, "");
#endif

					// Set the Fieldworks version text
					attributes = assembly.GetCustomAttributes(
						typeof(AssemblyInformationalVersionAttribute), false);
					string fwVersion = (attributes != null && attributes.Length > 0) ?
						((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion :
						Application.ProductVersion;
					// Omit the revision number from the suite version string if it's zero.
					ich = fwVersion.LastIndexOf(".0");
					if (ich == fwVersion.Length - 2 && ich > fwVersion.IndexOf('.'))
						fwVersion = fwVersion.Substring(0, ich);
					lblFwVersion.Text = string.Format(m_sFwVersionFmt, fwVersion);

					// Set the title bar text
					Text = string.Format(m_sTitleFmt, Application.ProductName);

					strRoot = Application.ExecutablePath.Substring(0, 2) + "\\";
				}
				else
				{
					// called from COM client
					strRoot = m_sDriveLetter + ":\\";
				}

				// Get copyright information from assembly info. By doing this we don't have
				// to update the about dialog each year.
				string copyRight;
				attributes = Assembly.GetExecutingAssembly()
					.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (attributes != null && attributes.Length > 0)
					copyRight = ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
				else
				{
					// if we can't find it in the assembly info, use generic one (which
					// might be out of date)
					copyRight = "(C) 2002-2004 SIL International";
				}
				lblCopyright.Text = string.Format(lblCopyright.Text,
					copyRight.Replace("(C)", "©"));

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

				// Set the Registration Number.
				RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\SIL\FieldWorks");
				string regNum = null;
				if (key != null)
				{
					regNum = key.GetValue("FwUserReg") as string;
					key.Close();
				}

				if (regNum == null || regNum == string.Empty)
				{
					ResourceManager resources = new ResourceManager(
						"SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs", Assembly.GetExecutingAssembly());
					regNum = resources.GetString("kstidUnregistered");
				}

				//lblRegistrationNumber.Text = string.Format(lblRegistrationNumber.Text, regNum);

				// Set expiration date information
				if (m_dropDeadDate.Year > 2999)
					lblExpirationDate.Visible = false;
				else
					lblExpirationDate.Text = string.Format(m_sExpirationDateLabelFmt,
						m_dropDeadDate.ToShortDateString());
			}
			catch
			{
				// ignore errors
			}
		}

		#endregion

		#region IFwHelpAbout interface implementation
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the form as a modal dialog with the currently active form as its owner
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		int IFwHelpAbout.ShowDialog()
		{
			CheckDisposed();

			return (int)base.ShowDialog();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product name which appears in the Name label on the about dialog box
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string IFwHelpAbout.ProdName
		{
			set
			{
				CheckDisposed();

				lblName.Text = value;
				Text = string.Format(m_sTitleFmt, value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product version which appears in the App Version label on the about dialog box
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string IFwHelpAbout.ProdVersion
		{
			set
			{
				CheckDisposed();

#if DEBUG
				lblAppVersion.Text = string.Format(m_sAppVersionFmt, value, m_sProdDate, "(Debug version)");
#else
				lblAppVersion.Text = string.Format(m_sAppVersionFmt, value, m_sProdDate, "");
#endif
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The integer portion of the "OLE Automation Date" for the product.  This is from the
		/// fourth (and final) field of the product version.
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.  C++ clients should set this
		/// before they set ProdVersion.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		int IFwHelpAbout.ProdOADate
		{
			set
			{
				CheckDisposed();

				double oadate = System.Convert.ToDouble(value);
				DateTime dt = DateTime.FromOADate(oadate);
				m_sProdDate = dt.ToString("yyyy/MM/dd");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product version which appears in the FW Version label on the about dialog box
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string IFwHelpAbout.FieldworksVersion
		{
			set
			{
				CheckDisposed();
				lblFwVersion.Text = string.Format(m_sFwVersionFmt, value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The drive letter whose free space will be reported in the About box.
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string IFwHelpAbout.DriveLetter
		{
			set
			{
				CheckDisposed();
				m_sDriveLetter = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The date reported in the About box after which the program stops working.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		DateTime IFwHelpAbout.DropDeadDate
		{
			set
			{
				CheckDisposed();
				m_dropDeadDate = value;
			}
		}

		#endregion
	}
	#endregion
}
