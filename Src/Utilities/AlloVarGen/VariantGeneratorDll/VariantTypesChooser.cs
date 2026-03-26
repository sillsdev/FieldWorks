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

namespace SIL.VariantGenerator
{
	public partial class VariantTypesChooser : Form
	{
		LcmCache Cache { get; set; }
		public List<AlloGenModel.VariantType> VariantTypes { get; set; }
		public List<AlloGenModel.VariantType> SelectedVariantTypes { get; set; }

		public VariantTypesChooser(LcmCache cache)
		{
			Cache = cache;
			VariantTypes = new List<AlloGenModel.VariantType>();
			SelectedVariantTypes = new List<AlloGenModel.VariantType>();
			InitializeComponent();
			clbVariantTypes.CheckOnClick = true;
			clbVariantTypes.Sorted = true;
			CreateVariantTypes();
		}

		void CreateVariantTypes()
		{
			ICmPossibilityList allVariants = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
			CreateVariantSubtypes(allVariants.PossibilitiesOS);
		}

		void CreateVariantSubtypes(ILcmOwningSequence<ICmPossibility> subtypes)
		{
			foreach (ILexEntryType variant in subtypes)
			{
				AlloGenModel.VariantType varType = new AlloGenModel.VariantType();
				varType.Guid = variant.Guid.ToString();
				varType.Name = variant.ChooserNameTS.Text;
				VariantTypes.Add(varType);
				CreateVariantSubtypes(variant.SubPossibilitiesOS);
			}
		}

		public void setSelected(List<AlloGenModel.VariantType> selectedOnes)
		{
			SelectedVariantTypes.Clear();
			SelectedVariantTypes.AddRange(selectedOnes);
		}

		public void FillVariantTypesListBox()
		{
			clbVariantTypes.BeginUpdate();
			clbVariantTypes.Items.Clear();
			foreach (AlloGenModel.VariantType varType in VariantTypes)
			{
				if (!string.IsNullOrEmpty(varType.Name))
					clbVariantTypes.Items.Add(varType);
			}
			for (int i = 0; i < clbVariantTypes.Items.Count; i++)
			{
				AlloGenModel.VariantType varType =
					clbVariantTypes.Items[i] as AlloGenModel.VariantType;
				for (int j = 0; j < SelectedVariantTypes.Count; j++)
				{
					if (SelectedVariantTypes[j].Guid == varType.Guid)
					{
						clbVariantTypes.SetItemChecked(i, true);
						break;
					}
				}
			}
			clbVariantTypes.EndUpdate();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			SelectedVariantTypes.Clear();
			for (int i = 0; i < clbVariantTypes.Items.Count; i++)
			{
				if (clbVariantTypes.GetItemChecked(i))
				{
					AlloGenModel.VariantType varType =
						clbVariantTypes.Items[i] as AlloGenModel.VariantType;
					SelectedVariantTypes.Add(varType);
				}
			}
		}
	}
}
