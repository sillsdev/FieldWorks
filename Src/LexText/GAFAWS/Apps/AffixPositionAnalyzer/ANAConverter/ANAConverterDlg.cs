// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2003' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ANAConverterDlg.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Xml.Xsl;
using System.Xml;

using SIL.WordWorks.GAFAWS;

namespace SIL.WordWorks.GAFAWS.ANAConverter
{
	/// <summary>
	/// Summary description for ANAConverterDlg.
	/// </summary>
	internal class ANAConverterDlg : System.Windows.Forms.Form
	{
		#region Data members

		private string m_parametersPathname = null;

		private System.Windows.Forms.Button btnAnal;
		private ToolTip tipBtnAnal;
		private System.Windows.Forms.Button btnClose;
		private ToolTip tipBtnClose;
		private System.Windows.Forms.Button btnBrowse;
		private ToolTip tipBtnBrowse;
		private System.Windows.Forms.TextBox tbANAFile;
		private ToolTip tipTbANAFile;
		private System.Windows.Forms.CheckedListBox chBxCategories;
		private ToolTip tipChBxCategories;
		private System.Windows.Forms.TextBox tbAmbigMarker;
		private ToolTip tipTbAmbigMarker;
		private System.Windows.Forms.Label lbAmbMkr;
		// No tool tip for labels
		private System.Windows.Forms.Label lbOpenDel;
		// No tool tip for labels
		private System.Windows.Forms.TextBox tbOpenDel;
		private ToolTip tipTbOpenDel;
		private System.Windows.Forms.Label lbCloseDel;
		// No tool tip for labels
		private System.Windows.Forms.TextBox tbCloseDel;
		private ToolTip tipTbCloseDel;
		private System.Windows.Forms.Label lbAffixSep;
		// No tool tip for labels
		private System.Windows.Forms.TextBox tbAffixSep;
		private ToolTip tipTbAffixSep;
		private System.Windows.Forms.Button btnSelect;
		private ToolTip tipBtnSelect;
		private System.Windows.Forms.Label lbCatAnal;
		// No tool tip for labels
		private System.Windows.Forms.Label lbANAFile;
		// No tool tip for labels
		private System.Windows.Forms.HelpProvider HelpMeOne;
		// No tool tip for labels
		private ToolTip tipBtnOK;

		private System.ComponentModel.IContainer components;

		#endregion Data members

		#region Properties

		private string AmbiguityMarker
		{
			get { return tbAmbigMarker.Text; }
		}

		private string AffixSeparator
		{
			get { return tbAffixSep.Text; }
		}

		private string OpenDelimiter
		{
			get { return tbOpenDel.Text; }
		}

		private string CloseDelimiter
		{
			get { return tbCloseDel.Text; }
		}

		#endregion Properties

		#region Construction and disposal

		/// <summary>
		/// Initializes a new instance of the MainWnd class.
		/// </summary>
		internal ANAConverterDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		private void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
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
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion Construction and disposal

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ANAConverterDlg));
			this.btnAnal = new System.Windows.Forms.Button();
			this.tipBtnAnal = new System.Windows.Forms.ToolTip(this.components);
			this.btnClose = new System.Windows.Forms.Button();
			this.tbANAFile = new System.Windows.Forms.TextBox();
			this.tipBtnClose = new System.Windows.Forms.ToolTip(this.components);
			this.btnBrowse = new System.Windows.Forms.Button();
			this.tipTbANAFile = new System.Windows.Forms.ToolTip(this.components);
			this.tipBtnBrowse = new System.Windows.Forms.ToolTip(this.components);
			this.tbAmbigMarker = new System.Windows.Forms.TextBox();
			this.tbOpenDel = new System.Windows.Forms.TextBox();
			this.lbANAFile = new System.Windows.Forms.Label();
			this.tipTbAmbigMarker = new System.Windows.Forms.ToolTip(this.components);
			this.lbAmbMkr = new System.Windows.Forms.Label();
			this.lbOpenDel = new System.Windows.Forms.Label();
			this.tipTbOpenDel = new System.Windows.Forms.ToolTip(this.components);
			this.tbCloseDel = new System.Windows.Forms.TextBox();
			this.tbAffixSep = new System.Windows.Forms.TextBox();
			this.btnSelect = new System.Windows.Forms.Button();
			this.lbCloseDel = new System.Windows.Forms.Label();
			this.tipTbCloseDel = new System.Windows.Forms.ToolTip(this.components);
			this.chBxCategories = new System.Windows.Forms.CheckedListBox();
			this.lbAffixSep = new System.Windows.Forms.Label();
			this.tipTbAffixSep = new System.Windows.Forms.ToolTip(this.components);
			this.tipChBxCategories = new System.Windows.Forms.ToolTip(this.components);
			this.lbCatAnal = new System.Windows.Forms.Label();
			this.tipBtnSelect = new System.Windows.Forms.ToolTip(this.components);
			this.HelpMeOne = new System.Windows.Forms.HelpProvider();
			this.tipBtnOK = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			//
			// btnAnal
			//
			this.btnAnal.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnAnal, "btnAnal");
			this.btnAnal.Name = "btnAnal";
			this.tipBtnAnal.SetToolTip(this.btnAnal, resources.GetString("btnAnal.ToolTip"));
			this.btnAnal.Click += new System.EventHandler(this.btnAnal_Click);
			//
			// btnClose
			//
			this.btnClose.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.HelpMeOne.SetHelpString(this.btnClose, resources.GetString("btnClose.HelpString"));
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.Name = "btnClose";
			this.HelpMeOne.SetShowHelp(this.btnClose, ((bool)(resources.GetObject("btnClose.ShowHelp"))));
			this.tipBtnAnal.SetToolTip(this.btnClose, resources.GetString("btnClose.ToolTip"));
			this.tipBtnClose.SetToolTip(this.btnClose, resources.GetString("btnClose.ToolTip1"));
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			//
			// tbANAFile
			//
			this.tbANAFile.AcceptsReturn = true;
			resources.ApplyResources(this.tbANAFile, "tbANAFile");
			this.HelpMeOne.SetHelpString(this.tbANAFile, resources.GetString("tbANAFile.HelpString"));
			this.tbANAFile.Name = "tbANAFile";
			this.HelpMeOne.SetShowHelp(this.tbANAFile, ((bool)(resources.GetObject("tbANAFile.ShowHelp"))));
			this.tipBtnAnal.SetToolTip(this.tbANAFile, resources.GetString("tbANAFile.ToolTip"));
			this.tipTbANAFile.SetToolTip(this.tbANAFile, resources.GetString("tbANAFile.ToolTip1"));
			//
			// btnBrowse
			//
			this.HelpMeOne.SetHelpString(this.btnBrowse, resources.GetString("btnBrowse.HelpString"));
			resources.ApplyResources(this.btnBrowse, "btnBrowse");
			this.btnBrowse.Name = "btnBrowse";
			this.HelpMeOne.SetShowHelp(this.btnBrowse, ((bool)(resources.GetObject("btnBrowse.ShowHelp"))));
			this.tipBtnClose.SetToolTip(this.btnBrowse, resources.GetString("btnBrowse.ToolTip"));
			this.tipBtnBrowse.SetToolTip(this.btnBrowse, resources.GetString("btnBrowse.ToolTip1"));
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			//
			// tbAmbigMarker
			//
			resources.ApplyResources(this.tbAmbigMarker, "tbAmbigMarker");
			this.tbAmbigMarker.Name = "tbAmbigMarker";
			this.tipBtnBrowse.SetToolTip(this.tbAmbigMarker, resources.GetString("tbAmbigMarker.ToolTip"));
			this.tipTbAmbigMarker.SetToolTip(this.tbAmbigMarker, resources.GetString("tbAmbigMarker.ToolTip1"));
			this.tbAmbigMarker.Validating += new System.ComponentModel.CancelEventHandler(this.tbAmbigMarker_Validating);
			//
			// tbOpenDel
			//
			resources.ApplyResources(this.tbOpenDel, "tbOpenDel");
			this.tbOpenDel.Name = "tbOpenDel";
			this.tipBtnBrowse.SetToolTip(this.tbOpenDel, resources.GetString("tbOpenDel.ToolTip"));
			this.tipTbOpenDel.SetToolTip(this.tbOpenDel, resources.GetString("tbOpenDel.ToolTip1"));
			this.tbOpenDel.Validating += new System.ComponentModel.CancelEventHandler(this.tbOpenDel_Validating);
			//
			// lbANAFile
			//
			resources.ApplyResources(this.lbANAFile, "lbANAFile");
			this.lbANAFile.Name = "lbANAFile";
			//
			// lbAmbMkr
			//
			resources.ApplyResources(this.lbAmbMkr, "lbAmbMkr");
			this.lbAmbMkr.Name = "lbAmbMkr";
			//
			// lbOpenDel
			//
			resources.ApplyResources(this.lbOpenDel, "lbOpenDel");
			this.lbOpenDel.Name = "lbOpenDel";
			//
			// tbCloseDel
			//
			resources.ApplyResources(this.tbCloseDel, "tbCloseDel");
			this.tbCloseDel.Name = "tbCloseDel";
			this.tipTbOpenDel.SetToolTip(this.tbCloseDel, resources.GetString("tbCloseDel.ToolTip"));
			this.tipTbCloseDel.SetToolTip(this.tbCloseDel, resources.GetString("tbCloseDel.ToolTip1"));
			this.tbCloseDel.Validating += new System.ComponentModel.CancelEventHandler(this.tbCloseDel_Validating);
			//
			// tbAffixSep
			//
			resources.ApplyResources(this.tbAffixSep, "tbAffixSep");
			this.tbAffixSep.Name = "tbAffixSep";
			this.tipTbOpenDel.SetToolTip(this.tbAffixSep, resources.GetString("tbAffixSep.ToolTip"));
			this.tipTbAffixSep.SetToolTip(this.tbAffixSep, resources.GetString("tbAffixSep.ToolTip1"));
			this.tbAffixSep.Validating += new System.ComponentModel.CancelEventHandler(this.tbAffixSep_Validating);
			//
			// btnSelect
			//
			resources.ApplyResources(this.btnSelect, "btnSelect");
			this.btnSelect.Name = "btnSelect";
			this.tipTbOpenDel.SetToolTip(this.btnSelect, resources.GetString("btnSelect.ToolTip"));
			this.tipBtnSelect.SetToolTip(this.btnSelect, resources.GetString("btnSelect.ToolTip1"));
			this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
			//
			// lbCloseDel
			//
			resources.ApplyResources(this.lbCloseDel, "lbCloseDel");
			this.lbCloseDel.Name = "lbCloseDel";
			//
			// chBxCategories
			//
			resources.ApplyResources(this.chBxCategories, "chBxCategories");
			this.chBxCategories.MultiColumn = true;
			this.chBxCategories.Name = "chBxCategories";
			this.tipChBxCategories.SetToolTip(this.chBxCategories, resources.GetString("chBxCategories.ToolTip"));
			this.tipTbCloseDel.SetToolTip(this.chBxCategories, resources.GetString("chBxCategories.ToolTip1"));
			//
			// lbAffixSep
			//
			resources.ApplyResources(this.lbAffixSep, "lbAffixSep");
			this.lbAffixSep.Name = "lbAffixSep";
			//
			// lbCatAnal
			//
			resources.ApplyResources(this.lbCatAnal, "lbCatAnal");
			this.lbCatAnal.Name = "lbCatAnal";
			//
			// tipBtnOK
			//
			this.tipBtnOK.ShowAlways = true;
			//
			// ANAConverterDlg
			//
			this.AcceptButton = this.btnAnal;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnClose;
			this.Controls.Add(this.btnSelect);
			this.Controls.Add(this.lbCatAnal);
			this.Controls.Add(this.chBxCategories);
			this.Controls.Add(this.lbAffixSep);
			this.Controls.Add(this.tbAffixSep);
			this.Controls.Add(this.tbCloseDel);
			this.Controls.Add(this.tbOpenDel);
			this.Controls.Add(this.tbAmbigMarker);
			this.Controls.Add(this.tbANAFile);
			this.Controls.Add(this.lbCloseDel);
			this.Controls.Add(this.lbOpenDel);
			this.Controls.Add(this.lbAmbMkr);
			this.Controls.Add(this.lbANAFile);
			this.Controls.Add(this.btnBrowse);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.btnAnal);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.HelpButton = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ANAConverterDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		internal string ParametersPathname
		{
			get { return m_parametersPathname; }
		}

		internal string ANAPathname
		{
			get { return tbANAFile.Text; }
		}

		// <summary
		// ResetCategories - get categories from an ana file and display them.
		// </summary>
		private void ResetCategories()
		{
			chBxCategories.BeginUpdate();
			StreamReader reader = null;

			try
			{
				reader = new StreamReader(tbANAFile.Text);
				StringCollection analysisLines = new StringCollection();
				StringCollection categoryLines = new StringCollection();
				StringCollection categories = new StringCollection();
				string line = reader.ReadLine();

				while (line != null)
				{
					// If the line contains %0%, then skip it, as it is a failure
					// and has no useful information in it.
					bool failure = (line.IndexOf(String.Format("{0}0{1}", AmbiguityMarker, AmbiguityMarker)) > -1);
					if (line.StartsWith(@"\a")
						&& !analysisLines.Contains(line)
						&& !failure)
						analysisLines.Add(line); // Save unique \a lines.
					else if (line.StartsWith(@"\cat")
						&& !categoryLines.Contains(line)
						&& !failure)
						categoryLines.Add(line); // Save unique \cat lines.
					line = reader.ReadLine();
				}
				reader.Close();
				reader = null;

				// Process \a lines.
				foreach (string aLine in analysisLines)
				{
					// \a POS1 < N palabra >
					// \a %2%IMP CAUS < VA pegar1 >%C CAUS < VA pegar1 >%
					string[] tokens = aLine.Split(OpenDelimiter[0]);
					for (int i = 1; i < tokens.Length; ++i)
					{
						string str = tokens[i];
						// Find end of roots.
						str = str.Substring(0, str.IndexOf(CloseDelimiter[0])).Trim();
						// It can have compound roots, so get category from each root.
						string[] roots = str.Split();
						for (int j = 0; j < roots.Length; j = ++j + 1)
						{
							string category = roots[j];
							if (!categories.Contains(category))
								categories.Add(category);
						}
					}
				}

				// Process \cat lines.
				foreach (string catLine in categoryLines)
				{
					// \cat PN -- final category only
					// \cat %2%V%V% -- final category only
					// \cat %5%N N%PRT PRT%N N%PRT PRT%N N% -- final category+each morpheme category
					// \cat %5%N N%ADJ ADJ%N N%V VA/V=VA%V VA/V=VA% -- final category+each morpheme category
					// \cat V VA/V=VA=V/V -- final category+each morpheme category
					string[] catsMain = catLine.Substring(5).Split(AmbiguityMarker[0]);
					for (int i = (catsMain.Length == 1) ? 0 : 2; // Start with 0, if it wasn't ambiguous. otherwise start at 2.
						i < catsMain.Length;
						++i)
					{
						string[] catsInner = catsMain[i].Split();
						for (int j = 0; j < catsInner.Length; ++j)
						{
							if ((j % 2) == 0)
							{
								// Final word-level category
								string cat = catsInner[j];
								if (!categories.Contains(cat))
									categories.Add(cat);
							}
							else
							{
								// We want the root categories, but not affix categories.
								string[] catsInnermost = catsInner[j].Split('=');
								foreach (string cat in catsInnermost)
								{
									if (cat.IndexOf("/") == -1 && !categories.Contains(cat))
										categories.Add(cat);
								}
							}
						}
					}
				}

				// Add them to the control,and check them all.
				// TODO: Add a control to select/unselect all.
				foreach (string cat in categories)
				{
					if (cat != string.Empty)
					{
						int idx = chBxCategories.Items.Add(cat);
						chBxCategories.SetItemChecked(idx, true);
					}
				}

			}
			finally
			{
				if (reader != null)
					reader.Close();
				reader = null;
				chBxCategories.EndUpdate();
			}
		}

		/// <summary>
		/// ValidateTextBox - Check for valid ana delimiter characters
		/// </summary>
		/// <param name="textBox"></param>
		/// <param name="defaultValue"></param>
		private void ValidateTextBox(TextBox textBox, string defaultValue)
		{
			if (textBox.Text == string.Empty)
				textBox.Text = defaultValue;
		}

		#region Event handlers

		/// <summary>
		/// btnBrowse_Click - Browse for an ana file to analyze.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnBrowse_Click(object sender, System.EventArgs e)
		{
			using (OpenFileDialog openDlg = new OpenFileDialog())
			{
				openDlg.Filter = "ANA files (*.ana)|*.ana|All files (*.*)|*.*" ;
				openDlg.Title = "Ample Analysis File";
				chBxCategories.Items.Clear();
				if(openDlg.ShowDialog() == DialogResult.OK)
				{
					tbANAFile.Text = openDlg.FileName;
					btnSelect.Enabled = true;
					btnAnal.Enabled = true;
					chBxCategories.Enabled = true;
					ResetCategories();
				}
				else
				{
					tbANAFile.Text = "";
					btnSelect.Enabled = false;
					btnAnal.Enabled = false;
				}
			}
		}

		/// <summary>
		/// btnAnal_Click - Does the actual analysis.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnAnal_Click(object sender, System.EventArgs e)
		{
			// User Defined parameters.
			Parameters parms = new Parameters();
			m_parametersPathname = (tbANAFile.Text.Split('.'))[0] + ".prm";
			parms.Marker.Ambiguity = AmbiguityMarker[0];
			parms.RootDelimiter.OpenDelimiter = OpenDelimiter[0];
			parms.RootDelimiter.CloseDelimiter = CloseDelimiter[0];
			parms.Marker.Decomposition = AffixSeparator[0];
			for (int i = 0; i < chBxCategories.CheckedItems.Count; i++)
				parms.Categories.Add(new Category(chBxCategories.CheckedItems[i].ToString()));
			parms.Serialize(m_parametersPathname);

			Close();
		}

		/// <summary>
		/// btnClose_Click- Exit
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnClose_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		/// <summary>
		/// tbAmbigMarker_Validating- Validate for default ambiguity marker "%".
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void tbAmbigMarker_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			ValidateTextBox(sender as TextBox, "%");
		}

		/// <summary>
		/// tbAffixSep_Validating - Validate for default affix separator char "-".
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void tbAffixSep_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			ValidateTextBox(sender as TextBox, "-");
		}

		/// <summary>
		/// tbOpenDel_Validating - Validate for default opening char "<".
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void tbOpenDel_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			ValidateTextBox(sender as TextBox, "<");
		}

		/// <summary>
		/// tbCloseDel_Validating - Validate for default closing char ">".
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void tbCloseDel_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			ValidateTextBox(sender as TextBox, ">");
		}

		/// <summary>
		/// btnSelect_Click - Select or unselect all categories.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnSelect_Click(object sender, System.EventArgs e)
		{
			// Select or unselect them all.
			bool catChecked = false;

			if (btnSelect.Text == "Select All")
			{
				catChecked = true;
				btnSelect.Text = "DeSelect All";
			}
			else
			{
				catChecked = false;
				btnSelect.Text = "Select All";
			}

			chBxCategories.BeginUpdate();
			for (int i = 0; i < chBxCategories.Items.Count; ++i)
			{
				chBxCategories.SetItemChecked(i, catChecked);
			}
			chBxCategories.EndUpdate();
		}

	#endregion Event handlers
	}
}
