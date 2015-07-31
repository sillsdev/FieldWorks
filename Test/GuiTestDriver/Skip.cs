// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;

namespace GuiTestDriver
{
	public class Skip : Context
	{
		public Skip()
		{
			m_tag = "skip";
		}

		/// <summary>
		/// Skip all instructions contained.
		/// </summary>
		public override void Execute()
		{
			//base.Execute();
			base.Finished = true;
		}
	}
}
