// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Beep.cs
// Responsibility:HintonD
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Beep.
	/// </summary>
	public class Beep : Instruction
	{
		[DllImport("User32.dll")]
		static extern Boolean MessageBeep(UInt32 beepType);

		public Beep()
		{
			m_tag = "beep";
		}

		public override void Execute()
		{
			base.Execute ();
			MessageBeep(0);
			Finished = true; // tell do-once it's done
		}

//		public Beep(){}
	}
}
