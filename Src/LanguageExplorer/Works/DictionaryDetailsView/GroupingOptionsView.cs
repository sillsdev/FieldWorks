// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer.Works.DictionaryDetailsView
{
	/// <summary>
	/// This view is responsible for the display of options for a GroupingNode in the configuration dialog
	/// </summary>
	public partial class GroupingOptionsView : UserControl, IDictionaryGroupingOptionsView
	{
		private Control m_panelContents;
		private readonly ToolTip m_tt = new ToolTip();

		public GroupingOptionsView()
		{
			InitializeComponent();
			m_tt.SetToolTip(descriptionBox, "Description of the intended use of this grouping node. (Not published)");
		}

		public string Description
		{
			get { return descriptionBox.Text; }
			set { descriptionBox.Text = value; }
		}

		public bool DisplayInParagraph
		{
			get { return displayInParagraph.Checked; }
			set { displayInParagraph.Checked = value; }
		}

		public event EventHandler DisplayInParagraphChanged
		{
			add { displayInParagraph.CheckedChanged += value; }
			remove { displayInParagraph.CheckedChanged -= value; }
		}

		public event EventHandler DescriptionChanged
		{
			add { descriptionBox.TextChanged += value; }
			remove { descriptionBox.TextChanged -= value; }
		}
	}
}
