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
// File: Sound.cs
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
	/// Summary description for Sound.
	/// </summary>
	public class Sound : Instruction
	{
		[DllImport("Kernel32.dll", SetLastError=true)]
		static extern Boolean Beep(UInt32 frequency, UInt32 duration);

		private UInt32 m_hz;
		private UInt32 m_duration;
		public Sound(UInt32 f, UInt32 d)
		{
			m_tag = "sound";
			m_hz = f;
			m_duration = d;
		}
		public override void Execute()
		{
			base.Execute ();
			Beep(m_hz, m_duration);
			Finished = true; // tell do-once it's done
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public override string image()
		{
			string image = base.image();
			image += @" hz="""+m_hz+@"""";
			image += @" duration="""+m_duration+@"""";
			return image;
		}
	}
}
