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
