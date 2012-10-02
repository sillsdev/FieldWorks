// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Beep.cs
// Responsibility:HintonD
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
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
