// Copyright (c) 2005-2020 SIL International
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
			LlCode = subItems[kLlCode].Text;
			LlName = subItems[kLlName].Text;
			FwCode = subItems[kFwCode].Text;
			FwName = subItems[kFwName].Text;
			EncodingConverter = subItems[kec].Text;
		}

		private const int kLlName = 0;
		private const int kFwName = 1;
		private const int kec = 2;
		private const int kLlCode = 3;
		private const int kFwCode = 4;
	}
}