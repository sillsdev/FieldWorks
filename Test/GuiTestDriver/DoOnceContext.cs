// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2005' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DoOnce.cs
// Responsibility: HintonD
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for DoOnce.
	/// This context is used for creating a group of instructions that
	/// are given an allowed max amount of time to complete. If they
	/// finish before the 'until' time has expired then the context will
	/// finish and move on to the next instruction.
	/// The order of 'on-dialog' commands is not required. IOW, they can
	/// occur in any order.
	/// Individual instruction commands that use the 'wait' attribute, will
	/// have that value ignored. The time to wait is for the whole group
	/// and is set via the 'until' attribute on the 'do-until' context.
	///
	/// </summary>
	public class DoOnce : Context
	{
		private Int32 m_waitTicks;
		private string m_waitingFor;
		private bool m_Result;

		public DoOnce(Int32 waitTicks)
		{
			m_waitTicks = waitTicks;
			m_waitingFor = null;
			m_tag = "do-once";
			m_Result = false;
		}

		public DoOnce() : this(30000) {}

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
			//			Assert.IsTrue(maxWait == null || maxWait.Length == 0, "Do-Once context must have an until attribute.");
			m_log.isTrue(m_waitTicks > 0, "Do-Once context must have a non-zero 'until' attribute.");
			ModelNode = con.ModelNode;
			return true;
		}

		public Int32 Until
		{
			get { return m_waitTicks; }
			set { m_waitTicks = value; }
		}

		public string WaitingFor
		{
			get { return m_waitingFor; }
			set { m_waitingFor = value; }
		}

		// when all the instructions pass, do-once passes
		// otherwise it fails
		public override void Execute()
		{
			// Don't call the base execute method! - want control..
			// base.Execute();
			if (Number == -1) Number = TestState.getOnly().IncInstructionCount;
			m_log.mark(this);
			m_ExecuteTickCount = System.Environment.TickCount;

			PrepareChildren(); // base method to build child instructions

			// remove all wait times in this context, but only at the child level
			this.RemoveWaitTime();
			foreach (Instruction ins in m_instructions)
			{
				ins.RemoveWaitTime();
			}

			PassFailInContext(OnPass, OnFail, out m_onPass, out m_onFail);
			AccessibilityHelper m_ah = Accessibility;
			if (1 == m_logLevel)
				m_log.paragraph(makeNameTag() + "Context is &quot;" + m_ah.Role + ":" + m_ah.Name + "&quot;");

			if (m_waitingFor != null && m_waitingFor != "")
				m_waitingFor = Utilities.evalExpr(m_waitingFor);

			int startTick = System.Environment.TickCount;

			m_log.paragraph(image());
			bool done = false;
			bool lastPass = false;	// used to allow 1 last pass over instructions after time is up
			while (!done)
			{
				CheckForErrorDialogs(true);
				// see if there are any cmds not finished
				done = true;	// start as if they're all done
				foreach (Instruction ins in m_instructions)
				{
					if (!ins.Finished)	// not already finished
					{
						// Don't assert onFail until it's the last pass (time is up)
						ins.DeferAssert = !lastPass;
						ins.Execute();
						if (!ins.Finished)	// still not finished
							done = false;
					}
				}
				m_Result = done;
				AccessibilityHelper ah = null;
				string title = null;
				if (m_waitingFor != null && m_waitingFor != "")
				{
					m_Result = false; // fail if the window is not found
					IntPtr foundHwndPtr = FindWindow(null,m_waitingFor);
					if ((int)foundHwndPtr != 0)
					{
						ah = new AccessibilityHelper(foundHwndPtr);
						// The ah constructor gets the topWindow if our window isn't found
						// Don't want that
						// ah = new AccessibilityHelper(m_waitingFor);

						// get the title, the ah.Name can be different.
						GuiPath path = new GuiPath("titlebar:NAMELESS");
						AccessibilityHelper ah1 = ah.SearchPath(path, null);
						if (ah1 != null)
							title = ah1.Value;
					}
				}
				if (ah != null) m_log.paragraph(makeNameTag() + "do-once found window:" + ah.Name);
				if (title != null) m_log.paragraph(makeNameTag() + "do-once found title:" + title);
				if (lastPass)
					done = true;
				if (title != null && title == m_waitingFor)
				{
					lastPass = true; // A window may appear a bit later
					m_Result = true;
				}

				// once time is up, allow finial pass over instructions allowing asserts as needed
				if (!lastPass)
					lastPass = Utilities.NumTicks(startTick, System.Environment.TickCount) > m_waitTicks;
			} // end of while loop

			Logger.getOnly().result(this);
			Finished = true; // tell do-once it's done

			if (m_onPass == "assert" && m_Result == true)
				m_log.fail(makeNameTag() + "do-once accomplished its task(s) but was not supposed to.");
			if (m_onFail == "assert" && m_Result == false)
				m_log.fail(makeNameTag() + "do-once did not accomplish all of its tasks.");
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "result";
			switch (name)
			{
			case "result": return m_Result.ToString();
			case "tick": return m_waitTicks.ToString();
			case "waiting-for": return m_waitingFor;
			default:
				return base.GetDataImage(name);
			}
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
			image += @" tick="""+m_waitTicks+@"""";
			if (m_waitingFor != null)
				image += @" waiting-for="""+Utilities.attrText(m_waitingFor)+@"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			image += @" result=""" + m_Result + @"""";
			return image;
		}
	}
}
