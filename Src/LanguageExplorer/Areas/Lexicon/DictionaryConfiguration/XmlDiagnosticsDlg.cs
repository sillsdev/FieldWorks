// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using Gecko;

namespace LanguageExplorer.Areas.Lexicon.DictionaryConfiguration
{
	public partial class XmlDiagnosticsDlg : Form
	{
		public XmlDiagnosticsDlg(GeckoElement element, Guid guid)
		{
			InitializeComponent();

			m_tb_guid.Text = guid.ToString();
			var htmlElement = element as GeckoHtmlElement;
			m_tb_xml.Text = htmlElement != null ? htmlElement.OuterHtml : element.TextContent;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}
	}
}