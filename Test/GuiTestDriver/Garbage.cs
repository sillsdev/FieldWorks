// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace GuiTestDriver
{
	class Garbage : Instruction
	{
		public Garbage()
		{
			m_tag = "garbage";
		}
		/// <summary>
		/// Forces Garbage collection for the NUnit process
		/// </summary>
		public override void Execute()
		{
			base.Execute ();
			System.GC.Collect(); // forces collection on NUnit process only
			// can we force garbage collection on FW?
			// if so, we can make it an attribute on the command
			// <garbage force="FW"/> or "AlL" for both, "NUnit" would be the default
			Finished = true; // tell do-once it's done
		}

	}
}
