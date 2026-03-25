// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.AlloGenModel;
using SIL.LCModel;
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
	public partial class ReplaceOperationsChooser : Form
	{
		AllomorphGenerators AlloGens { get; set; }
		public List<Replace> ReplaceOps { get; set; }
		public List<Replace> SelectedReplaceOps { get; set; }

		public ReplaceOperationsChooser(AllomorphGenerators alloGens)
		{
			AlloGens = alloGens;
			ReplaceOps = new List<Replace>();
			SelectedReplaceOps = new List<Replace>();
			InitializeComponent();
			clbReplaceOps.CheckOnClick = true;
			clbReplaceOps.Sorted = true;
			CreateReplaceOps();
		}

		void CreateReplaceOps()
		{
			foreach (Replace replace in AlloGens.ReplaceOperations)
			{
				ReplaceOps.Add(replace);
			}
		}

		public void setSelected(List<Replace> selectedOnes)
		{
			SelectedReplaceOps.Clear();
			SelectedReplaceOps.AddRange(selectedOnes);
		}

		public void FillReplaceOpsListBox()
		{
			clbReplaceOps.Items.Clear();
			foreach (Replace replace in ReplaceOps)
			{
				clbReplaceOps.Items.Add(replace);
			}
			//for (int i = 0; i < clbReplaceOps.Items.Count; i++)
			//{
			//    Replace env = clbReplaceOps.Items[i] as Replace;
			//    for (int j = 0; j < SelectedReplaceOps.Count; j++)
			//    {
			//        if (SelectedReplaceOps[j].Guid == env.Guid)
			//        {
			//            clbReplaceOps.SetItemChecked(i, true);
			//            break;
			//        }
			//    }
			//}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			SelectedReplaceOps.Clear();
			for (int i = 0; i < clbReplaceOps.Items.Count; i++)
			{
				if (clbReplaceOps.GetItemChecked(i))
				{
					Replace replace = clbReplaceOps.Items[i] as Replace;
					SelectedReplaceOps.Add(replace);
				}
			}
		}
	}
}
