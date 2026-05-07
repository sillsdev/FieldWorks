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
	public partial class CategoryChooser : Form
	{
		public List<Category> Categories { get; set; }
		public Category SelectedCategory { get; set; } = new Category();
		public Category NoneChosen { get; set; } = new Category();

		public CategoryChooser()
		{
			Categories = new List<Category>();
			NoneChosen.Name = "<None>";
			NoneChosen.Guid = "";
			InitializeComponent();
			FillCategoriesListBox();
		}

		public void FillCategoriesListBox()
		{
			lBoxCategories.BeginUpdate();
			lBoxCategories.Items.Clear();
			foreach (Category category in Categories)
			{
				lBoxCategories.Items.Add(category);
			}
			lBoxCategories.Items.Add(NoneChosen);
			lBoxCategories.EndUpdate();
		}

		public void SelectCategory(int index)
		{
			if (index > -1 && index <= Categories.Count)
			{
				lBoxCategories.SelectedIndex = index;
			}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			SelectedCategory = (Category)lBoxCategories.SelectedItem;
		}
	}
}
