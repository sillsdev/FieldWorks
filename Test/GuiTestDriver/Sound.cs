// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Sound.cs
// Responsibility:HintonD
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;
using System.Xml;

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

		public Sound() : this(400, 300) { }

		/// <summary>
		/// Called to finish construction when an instruction has been instantiated by
		/// a factory and had its properties set.
		/// This can check the integrity of the instruction or perform other initialization tasks.
		/// </summary>
		/// <param name="xn">XML node describing the instruction</param>
		/// <param name="con">Parent xml node instruction</param>
		/// <returns></returns>
		public override bool finishCreation(XmlNode xn, Context con)
		{  // finish factory construction
			m_log.isTrue(m_hz > 0 && m_duration > 0, makeNameTag() + "Sound instruction must have a non-zero frequency and duration.");
			return true;
		}

		public UInt32 Frequency
		{
			get { return m_hz; }
			set { m_hz = value; }
		}

		public UInt32 Duration
		{
			get { return m_duration; }
			set { m_duration = value; }
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
