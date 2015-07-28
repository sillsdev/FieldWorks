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

using ECInterfaces;
using SilEncConverters40;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Summary description for AdvancedEncProps.
	/// </summary>
	public class AdvancedEncProps : UserControl, IFWDisposable
	{
		private System.Windows.Forms.ListView lvConverterInfo;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		EncConverters m_encConverters;

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

		/// <summary></summary>
		public EncConverters Converters
		{
			get
			{
				CheckDisposed();
				return m_encConverters;
			}
			set
			{
				CheckDisposed();
				m_encConverters = value;
			}
		}

		/// <summary></summary>
		public AdvancedEncProps()
		{
			CheckDisposed();

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

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
			System.Windows.Forms.ColumnHeader columnHeader1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdvancedEncProps));
			System.Windows.Forms.ColumnHeader columnHeader2;
			System.Windows.Forms.HelpProvider helpProvider1;
			this.lvConverterInfo = new System.Windows.Forms.ListView();
			columnHeader1 = new System.Windows.Forms.ColumnHeader();
			columnHeader2 = new System.Windows.Forms.ColumnHeader();
			helpProvider1 = new HelpProvider();
			this.SuspendLayout();
			//
			// lvConverterInfo
			//
			this.lvConverterInfo.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			columnHeader1,
			columnHeader2});
			resources.ApplyResources(this.lvConverterInfo, "lvConverterInfo");
			this.lvConverterInfo.FullRowSelect = true;
			helpProvider1.SetHelpString(this.lvConverterInfo, resources.GetString("lvConverterInfo.HelpString"));
			this.lvConverterInfo.MultiSelect = false;
			this.lvConverterInfo.Name = "lvConverterInfo";
			helpProvider1.SetShowHelp(this.lvConverterInfo, ((bool)(resources.GetObject("lvConverterInfo.ShowHelp"))));
			this.lvConverterInfo.UseCompatibleStateImageBehavior = false;
			this.lvConverterInfo.View = System.Windows.Forms.View.Details;
			//
			// columnHeader1
			//
			resources.ApplyResources(columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(columnHeader2, "columnHeader2");
			//
			// AdvancedEncProps
			//
			this.Controls.Add(this.lvConverterInfo);
			this.Name = "AdvancedEncProps";
			helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary></summary>
		public void SelectMapping(string mapname)
		{
			CheckDisposed();

			IEncConverter ec =  (IEncConverter)m_encConverters[mapname];

			lvConverterInfo.Items.Clear();

			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"CodePageInput", ec.CodePageInput.ToString()}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"CodePageOutput", ec.CodePageOutput.ToString()}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"Type", ec.ConversionType.ToString()}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"Identifier", ec.ConverterIdentifier}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"DirectionForward", ec.DirectionForward.ToString()}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"EncodingIn", ec.EncodingIn.ToString()}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"EncodingOut", ec.EncodingOut.ToString()}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"ImplementType", ec.ImplementType}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"ProcessType", ec.ProcessType.ToString()}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"LeftEncodingID", ec.LeftEncodingID}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"RightEncodingID", ec.RightEncodingID}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"NormalizeOutput", ec.NormalizeOutput.ToString()}));
			lvConverterInfo.Items.Add(new ListViewItem(new string[] {"ProgramID", ec.ProgramID}));
		}
	}
}
