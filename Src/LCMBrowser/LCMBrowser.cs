// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.WritingSystems;

namespace LCMBrowser
{
	static class LCMBrowser
	{
		/// <summary/>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			FwRegistryHelper.Initialize();
			FwUtils.InitializeIcu();
			Sldr.Initialize();
			using (var form = new LCMBrowserForm())
			{
				Application.Run(form);
			}

			Sldr.Cleanup();
		}
	}
}