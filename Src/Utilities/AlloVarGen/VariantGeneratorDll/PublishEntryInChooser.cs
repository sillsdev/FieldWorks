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
	public partial class PublishEntryInChooser : Form
	{
		LcmCache Cache { get; set; }
		public List<AlloGenModel.PublishEntryInItem> PublishEntryInItems { get; set; }
		public List<AlloGenModel.PublishEntryInItem> SelectedPublishEntryInItems { get; set; }

		public PublishEntryInChooser(LcmCache cache)
		{
			Cache = cache;
			PublishEntryInItems = new List<AlloGenModel.PublishEntryInItem>();
			SelectedPublishEntryInItems = new List<AlloGenModel.PublishEntryInItem>();
			InitializeComponent();
			clbPublishEntryInItems.CheckOnClick = true;
			clbPublishEntryInItems.Sorted = true;
			CreatePublishEntryInItems();
		}

		void CreatePublishEntryInItems()
		{
			ICmPossibilityList allPublishEntryInItems = Cache
				.LangProject
				.LexDbOA
				.PublicationTypesOA;
			foreach (ICmPossibility pubType in allPublishEntryInItems.PossibilitiesOS)
			{
				AlloGenModel.PublishEntryInItem item = new AlloGenModel.PublishEntryInItem();
				item.Guid = pubType.Guid.ToString();
				item.Name = pubType.ChooserNameTS.Text;
				PublishEntryInItems.Add(item);
			}
		}

		public void setSelected(List<AlloGenModel.PublishEntryInItem> selectedOnes)
		{
			SelectedPublishEntryInItems.Clear();
			SelectedPublishEntryInItems.AddRange(selectedOnes);
		}

		public void FillPublishEntryInItemsListBox()
		{
			clbPublishEntryInItems.BeginUpdate();
			clbPublishEntryInItems.Items.Clear();
			foreach (AlloGenModel.PublishEntryInItem pubInItem in PublishEntryInItems)
			{
				if (!string.IsNullOrEmpty(pubInItem.Name))
					clbPublishEntryInItems.Items.Add(pubInItem);
			}
			for (int i = 0; i < clbPublishEntryInItems.Items.Count; i++)
			{
				AlloGenModel.PublishEntryInItem pubEntryInItem =
					clbPublishEntryInItems.Items[i] as AlloGenModel.PublishEntryInItem;
				for (int j = 0; j < SelectedPublishEntryInItems.Count; j++)
				{
					if (SelectedPublishEntryInItems[j].Guid == pubEntryInItem.Guid)
					{
						clbPublishEntryInItems.SetItemChecked(i, true);
						break;
					}
				}
			}
			clbPublishEntryInItems.EndUpdate();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			SelectedPublishEntryInItems.Clear();
			for (int i = 0; i < clbPublishEntryInItems.Items.Count; i++)
			{
				if (clbPublishEntryInItems.GetItemChecked(i))
				{
					AlloGenModel.PublishEntryInItem pubEntryItem =
						clbPublishEntryInItems.Items[i] as AlloGenModel.PublishEntryInItem;
					SelectedPublishEntryInItems.Add(pubEntryItem);
				}
			}
		}
	}
}
