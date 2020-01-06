// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters40;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	public class AdvancedEncProps : UserControl
	{
		private System.Windows.Forms.ListView lvConverterInfo;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		/// <summary />
		public EncConverters Converters { get; set; }

		/// <summary />
		public AdvancedEncProps()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
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

		/// <summary />
		public void SelectMapping(string mapname)
		{
			var ec = (IEncConverter)Converters[mapname];
			lvConverterInfo.Items.Clear();

			lvConverterInfo.Items.Add(new ListViewItem(new[] { "CodePageInput", ec.CodePageInput.ToString() }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "CodePageOutput", ec.CodePageOutput.ToString() }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "Type", ec.ConversionType.ToString() }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "Identifier", ec.ConverterIdentifier }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "DirectionForward", ec.DirectionForward.ToString() }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "EncodingIn", ec.EncodingIn.ToString() }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "EncodingOut", ec.EncodingOut.ToString() }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "ImplementType", ec.ImplementType }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "ProcessType", ec.ProcessType.ToString() }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "LeftEncodingID", ec.LeftEncodingID }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "RightEncodingID", ec.RightEncodingID }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "NormalizeOutput", ec.NormalizeOutput.ToString() }));
			lvConverterInfo.Items.Add(new ListViewItem(new[] { "ProgramID", ec.ProgramID }));
		}
	}
}