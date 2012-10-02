using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Access = Microsoft.Office.Interop.Access;

namespace SILConvertersOffice
{
	internal partial class DbFieldSelect : Form
	{
		public DbFieldSelect(dao.TableDefs aTableDefs)
		{
			InitializeComponent();

			for(int i = 0; i < aTableDefs.Count; i++ )
			{
				dao.TableDef aTable = aTableDefs[i];

				if (aTable.Attributes == 0)
				{
					TreeNode node = this.treeViewTablesFields.Nodes.Add(aTable.Name);
					dao.Fields aFields = aTable.Fields;
					for (int j = 0; j < aFields.Count; j++)
					{
						dao.Field aField = aFields[j];
						node.Nodes.Add(aField.Name);

						OfficeApp.ReleaseComObject(aField); // needed or Access stays running after exit
					}
					OfficeApp.ReleaseComObject(aFields);
				}
				OfficeApp.ReleaseComObject(aTable);
			}
		}

		public string TableName
		{
			get { return this.treeViewTablesFields.SelectedNode.Parent.Text; }
		}

		public string FieldName
		{
			get { return this.treeViewTablesFields.SelectedNode.Text; }
		}

		private void buttonConvert_Click(object sender, EventArgs e)
		{
			if (this.treeViewTablesFields.SelectedNode.Parent != null)
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void treeViewTablesFields_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Node.Parent != null)
				buttonConvert_Click(sender, e);
		}

		private void treeViewTablesFields_AfterSelect(object sender, TreeViewEventArgs e)
		{
			this.buttonConvert.Enabled = (e.Node.Parent != null);
		}
	}
}