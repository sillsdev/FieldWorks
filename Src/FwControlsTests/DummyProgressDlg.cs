// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary />
	public class DummyProgressDlg : ProgressDialogWithTask
	{
		/// <summary />
		public DummyProgressDlg(ISynchronizeInvoke synchronizeInvoke)
			: base(synchronizeInvoke)
		{
		}
	}
}