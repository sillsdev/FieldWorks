// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	internal struct LanguageMapping
	{
		/// <summary />
		public string LlCode;
		/// <summary />
		public string LlName;
		/// <summary />
		public string FwCode;
		/// <summary />
		public string FwName;
		/// <summary />
		public string EncodingConverter;
		/// <summary />
		public LanguageMapping(ListViewItem.ListViewSubItemCollection subItems)
		{
			Debug.Assert(subItems.Count == 5);
			LlCode = subItems[LinguaLinksImportDlg.kLlCode].Text;
			LlName = subItems[LinguaLinksImportDlg.kLlName].Text;
			FwCode = subItems[LinguaLinksImportDlg.kFwCode].Text;
			FwName = subItems[LinguaLinksImportDlg.kFwName].Text;
			EncodingConverter = subItems[LinguaLinksImportDlg.kec].Text;
		}
	}
}