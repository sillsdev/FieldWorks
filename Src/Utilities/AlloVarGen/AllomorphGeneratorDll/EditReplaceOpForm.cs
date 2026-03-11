// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.AlloGenModel;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIL.AllomorphGenerator
{
	public partial class EditReplaceOpForm : Form
	{
		private FwTextBox fwtbFrom;
		private FwTextBox fwtbTo;

		public Replace ReplaceOp { get; set; }
		public bool OkPressed { get; set; }

		public EditReplaceOpForm()
		{
			InitializeComponent();
			InitializeFWComponents();
		}

		private void InitializeFWComponents()
		{
			fwtbFrom = new FwTextBox();
			((System.ComponentModel.ISupportInitialize)(this.fwtbFrom)).BeginInit();
			//
			// fwtbFrom
			//
			this.fwtbFrom.Location = new System.Drawing.Point(105, 97);
			this.fwtbFrom.Name = "fwtbFrom";
			this.fwtbFrom.Size = new System.Drawing.Size(235, 26);
			this.fwtbFrom.TabIndex = 5;

			this.fwtbFrom.AcceptsReturn = false;
			this.fwtbFrom.AdjustStringHeight = true;
			this.fwtbFrom.BackColor = System.Drawing.SystemColors.Window;
			this.fwtbFrom.controlID = null;
			//resources.ApplyResources(this.fwtbFrom, "fwtbFrom");
			this.fwtbFrom.HasBorder = true;
			this.fwtbFrom.Name = "fwtbFrom";
			this.fwtbFrom.SuppressEnter = true;
			this.fwtbFrom.WordWrap = false;
			this.fwtbFrom.Leave += new System.EventHandler(fwtb_Leave);
			((System.ComponentModel.ISupportInitialize)(this.fwtbFrom)).EndInit();
			this.Controls.Add(this.fwtbFrom);

			fwtbTo = new FwTextBox();
			((System.ComponentModel.ISupportInitialize)(this.fwtbTo)).BeginInit();
			//
			// fwtbTo
			//
			this.fwtbTo.Location = new System.Drawing.Point(105, 127);
			this.fwtbTo.Name = "fwtbTo";
			this.fwtbTo.Size = new System.Drawing.Size(235, 26);
			this.fwtbTo.TabIndex = 8;

			this.fwtbTo.AcceptsReturn = false;
			this.fwtbTo.AdjustStringHeight = true;
			this.fwtbTo.BackColor = System.Drawing.SystemColors.Window;
			this.fwtbTo.controlID = null;
			//resources.ApplyResources(this.fwtbTo, "fwtbTo");
			this.fwtbTo.HasBorder = true;
			this.fwtbTo.Name = "fwtbTo";
			this.fwtbTo.SuppressEnter = true;
			this.fwtbTo.WordWrap = false;
			this.fwtbTo.Leave += new System.EventHandler(fwtb_Leave);
			((System.ComponentModel.ISupportInitialize)(this.fwtbTo)).EndInit();
			this.Controls.Add(this.fwtbTo);
		}

		private void fwtb_Leave(object sender, EventArgs e)
		{
			if (cbRegEx.Checked)
			{
				FwTextBox tb = (FwTextBox)sender;
				if (!IsValidRegex(tb.Text))
				{
					tb.Focus();
				}
			}
		}

		public void Initialize(Replace replace, List<WritingSystem> writingSystems, LcmCache cache)
		{
			fwtbFrom.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			fwtbFrom.WritingSystemCode = cache
				.ServiceLocator
				.WritingSystems
				.DefaultVernacularWritingSystem
				.Handle;
			fwtbTo.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			fwtbTo.WritingSystemCode = cache
				.ServiceLocator
				.WritingSystems
				.DefaultVernacularWritingSystem
				.Handle;
			ReplaceOp = replace;
			tbName.Text = replace.Name;
			tbDescription.Text = replace.Description;
			fwtbFrom.Text = replace.From;
			fwtbTo.Text = replace.To;
			cbRegEx.Checked = replace.Mode;
			foreach (WritingSystem ws in writingSystems)
			{
				clbWritingSystems.Items.Add(ws);
				int index = clbWritingSystems.Items.Count - 1;
				if (replace.WritingSystemRefs.Contains(ws.Name))
				{
					clbWritingSystems.SetItemChecked(index, true);
				}
			}
			tbName.Select();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			ReplaceOp.Name = tbName.Text;
			ReplaceOp.Description = tbDescription.Text;
			ReplaceOp.From = fwtbFrom.Text;
			ReplaceOp.To = fwtbTo.Text;
			ReplaceOp.Mode = cbRegEx.Checked;
			ReplaceOp.WritingSystemRefs.Clear();
			for (int i = 0; i < clbWritingSystems.Items.Count; i++)
			{
				if (clbWritingSystems.GetItemChecked(i))
				{
					var ws = clbWritingSystems.Items[i] as WritingSystem;
					if (ws != null)
					{
						ReplaceOp.WritingSystemRefs.Add(ws.Name);
					}
				}
			}
			OkPressed = true;
			this.Close();
		}

		// Source - https://stackoverflow.com/a/1775017
		// Posted by Jeff Atwood, modified by community. See post 'Timeline' for change history
		// Retrieved 2026-03-10, License - CC BY-SA 4.0
		private static bool IsValidRegex(string pattern)
		{
			if (string.IsNullOrWhiteSpace(pattern)) return false;

			try
			{
				Regex.Match("", pattern);
			}
			catch (ArgumentException ex)
			{
				MessageBox.Show("Error found in regular expression:\n" + ex.Message);
				return false;
			}

			return true;
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			OkPressed = false;
			this.Close();
		}

		private void cbRegEx_CheckedChanged(object sender, EventArgs e)
		{
			ReplaceOp.Mode = cbRegEx.Checked;
		}
	}
}
