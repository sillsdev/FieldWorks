// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.AlloGenModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIL.AllomorphGenerator
{
	public partial class StemNameChooser : Form
	{
		public List<StemName> StemNames { get; set; }
		public StemName SelectedStemName { get; set; } = new StemName();
		public StemName NoneChosen { get; set; } = new StemName();

		public StemNameChooser()
		{
			StemNames = new List<StemName>();
			NoneChosen.Name = "<None>";
			NoneChosen.Guid = "";
			InitializeComponent();
			FillStemNamesListBox();
		}

		public void FillStemNamesListBox()
		{
			lBoxStemNames.BeginUpdate();
			lBoxStemNames.Items.Clear();
			foreach (StemName stemName in StemNames)
			{
				lBoxStemNames.Items.Add(stemName);
			}
			lBoxStemNames.Items.Add(NoneChosen);
			lBoxStemNames.EndUpdate();
		}

		public void SelectStemName(int index)
		{
			if (index > -1 && index <= StemNames.Count)
			{
				lBoxStemNames.SelectedIndex = index;
			}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			SelectedStemName = (StemName)lBoxStemNames.SelectedItem;
		}
	}
}
