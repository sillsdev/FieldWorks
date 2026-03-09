// Copyright (c) 2022 SIL International
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
	public partial class MorphTypesChooser : Form
	{
		LcmCache Cache { get; set; }
		public List<MorphType> MorphTypes { get; set; }
		public List<MorphType> SelectedMorphTypes { get; set; }
		List<string> morphTypesToUseGuids = new List<string>()
		{
			"d7f713e4-e8cf-11d3-9764-00c04f186933",
			"d7f713e7-e8cf-11d3-9764-00c04f186933",
			"0cc8c35a-cee9-434d-be58-5d29130fba5b",
			"56db04bf-3d58-44cc-b292-4c8aa68538f4",
			"a23b6faa-1052-4f4d-984b-4b338bdaf95f",
			"d7f713e5-e8cf-11d3-9764-00c04f186933",
			"d7f713e8-e8cf-11d3-9764-00c04f186933",
		};

		public MorphTypesChooser(LcmCache cache)
		{
			Cache = cache;
			MorphTypes = new List<MorphType>();
			SelectedMorphTypes = new List<MorphType>();
			InitializeComponent();
			clbMorphTypes.CheckOnClick = true;
			clbMorphTypes.Sorted = true;
			CreateMorphTypes();
		}

		void CreateMorphTypes()
		{
			ILcmOwningSequence<ICmPossibility> allTypes = Cache
				.LangProject
				.LexDbOA
				.MorphTypesOA
				.PossibilitiesOS;
			foreach (ICmPossibility mt in allTypes)
			{
				if (morphTypesToUseGuids.Contains(mt.Guid.ToString()))
				{
					MorphType morphType = new MorphType();
					morphType.Guid = mt.Guid.ToString();
					morphType.Name = mt.Name.AnalysisDefaultWritingSystem.Text;
					MorphTypes.Add(morphType);
				}
			}
		}

		public void setSelected(List<MorphType> selectedOnes)
		{
			SelectedMorphTypes.Clear();
			SelectedMorphTypes.AddRange(selectedOnes);
		}

		public void FillMorphTypesListBox()
		{
			clbMorphTypes.Items.Clear();
			foreach (MorphType mt in MorphTypes)
			{
				clbMorphTypes.Items.Add(mt);
			}
			for (int i = 0; i < clbMorphTypes.Items.Count; i++)
			{
				MorphType mt = clbMorphTypes.Items[i] as MorphType;
				for (int j = 0; j < SelectedMorphTypes.Count; j++)
				{
					if (SelectedMorphTypes[j].Guid == mt.Guid)
					{
						clbMorphTypes.SetItemChecked(i, true);
						break;
					}
				}
			}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			SelectedMorphTypes.Clear();
			for (int i = 0; i < clbMorphTypes.Items.Count; i++)
			{
				if (clbMorphTypes.GetItemChecked(i))
				{
					MorphType mt = clbMorphTypes.Items[i] as MorphType;
					SelectedMorphTypes.Add(mt);
				}
			}
		}

		private void clbMorphTypes_ItemCheckChanged(object sender, ItemCheckEventArgs e)
		{
			int augment = (e.NewValue == CheckState.Unchecked) ? -1 : 1;
			if ((clbMorphTypes.CheckedItems.Count + augment) > 0)
				btnOK.Enabled = true;
			else
				btnOK.Enabled = false;
		}
	}
}
