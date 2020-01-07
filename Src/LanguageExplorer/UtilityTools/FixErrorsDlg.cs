// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.UtilityTools
{
	/// <summary />
	public partial class FixErrorsDlg : Form
	{
		/// <summary />
		public FixErrorsDlg()
		{
			InitializeComponent();
			m_btnFixLinks.Enabled = false;
			foreach (var dir in Directory.GetDirectories(FwDirectoryFinder.ProjectsDirectory))
			{
				var ext = LcmFileHelper.ksFwDataXmlFileExtension;
				var lockext = ext + ".lock";
				var basename = Path.GetFileName(dir);
				var datafile = Path.Combine(dir, basename + ext);
				var lockfile = Path.Combine(dir, basename + lockext);
				if (File.Exists(datafile) && !File.Exists(lockfile))
				{
					m_lvProjects.Items.Add(basename);
				}
			}
		}

		/// <summary>
		/// Return the name of the selected project.
		/// </summary>
		public string SelectedProject => m_lvProjects.CheckedItems.Count > 0 ? m_lvProjects.CheckedItems[0].ToString() : null;

		private void m_btnFixLinks_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		/// <summary>
		/// Allow only one item to be checked at a time.
		/// </summary>
		private void m_lvProjects_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (m_lvProjects.SelectedIndex == e.Index)
			{
				var ie = m_lvProjects.CheckedIndices.GetEnumerator();
				var indexes = new List<int>();
				while (ie.MoveNext())
				{
					var idx = (int)ie.Current;
					if (idx != m_lvProjects.SelectedIndex)
					{
						indexes.Add(idx);
					}
				}
				foreach (var idx in indexes)
				{
					m_lvProjects.SetItemChecked(idx, false);
				}
				m_btnFixLinks.Enabled = (e.NewValue == CheckState.Checked);
			}
		}
	}
}