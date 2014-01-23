// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Context.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices; // DllImport
using System.Windows.Forms; // AccessibleRole
using System.Xml;

namespace GuiTestDriver
{
	/// <summary>
	/// Context gathers together a list of other organizing objects.
	/// </summary>
	public class Context : Instruction
	{
		[DllImport("User32.dll")]
		public static extern IntPtr FindWindow(string strClassName, string strWindowName);

		[DllImport("User32.dll")]
		public static extern int FindWindowEx(
			IntPtr hwndParent,
			IntPtr hwndChildAfter,
			string lpszClass,
			string lpszWindow);


		protected ArrayList m_instructions = new ArrayList();
		protected AccessibilityHelper m_ah;
		protected string m_onFail;
		protected string m_onPass;
		protected XmlPath m_modelNode = null;

		public Context() : base()
		{
			m_ah     = null;
			m_onFail = null;
			m_onPass = null;
			m_tag    = "on-context";
		}

		public string OnFail
		{
			get {return m_onFail;}
			set {m_onFail = value;}
		}
		public string OnPass
		{
			get {return m_onPass;}
			set {m_onPass = value;}
		}

		public XmlPath ModelNode
		{
			get { return m_modelNode; }
			set { m_modelNode = value; }
		}

		/// <summary>
		/// Converts child instructions from XML the first time this is called.
		/// Some contexts need to build their children but control execution
		/// themselves. They don't call this.Execute, which calls this internally.
		/// </summary>
		public void PrepareChildren()
		{
			PrepareChildren(false); // add children if none yet
		}

		/// <summary>
		/// Converts child instructions from XML the first time this is called.
		/// Some contexts need to build their children but control execution
		/// themselves. They don't call this. Execute, which calls this internally.
		/// </summary>
		public void PrepareChildren(bool addMore)
		{
			if (1 == m_logLevel)
			{
				if (m_ah == null) m_log.paragraph(makeNameTag() + "PrepareChildren: Context is not yet defined.");
				else m_log.paragraph(makeNameTag() + "PrepareChildren: Context is &quot;" + m_ah.Role + ":" + m_ah.Name + "&quot;");
			}
			// m_instructions may have been stocked before execution as by click.
			// They also may have been built on a previous execution via do-once.
			if (addMore || m_instructions.Count == 0)
			{ // Build the child instructions
				// Insertions may be made by expansion of some instructions into
				// multiple instructions (via include) so iterators and foreach can't be used.
				int count = 0;
				if (count < m_elt.ChildNodes.Count)
				{
					XmlNode xn = m_elt.ChildNodes[count];
					while (xn != null)
					{   // MakeShell can add multiple instructions to m_instructions.
						Instruction ins = Instructionator.MakeShell(xn, this);
						// pass higher log levels to the children
						if (ins != null && ins.Log < Log)
							ins.Log = Log;
						count += 1;
						if (count < m_elt.ChildNodes.Count)
							xn = m_elt.ChildNodes[count];
						else xn = null;
					}
				}
			}
		}

		/// <summary>
		/// Executes this context with its instructions.
		/// Child instructions are converted from XML the first time this is called.
		/// Each instruction is executed in turn so that
		/// the instruction tree is traversed depth-first.
		/// </summary>
		public override void Execute()
		{
			base.Execute();
			if (m_instructions.Count == 0 && !(this is Model)) PrepareChildren();
			// Execute the child instructions
			// Insertions may be made by expansion of some instructions into
			// multiple instructions (via @select) so iterators and foreach can't be used.
			int count = 0;
			if (count < m_instructions.Count)
			{
				SetContextVariables();
				Instruction inst = (Instruction)m_instructions[count];
				while (inst != null)
				{
					if (!inst.Finished) inst.Execute();
					count += 1;
					if (count < m_instructions.Count)
						inst = (Instruction)m_instructions[count];
					else inst = null;
				}
				// reinstate the parent context's variables
				Context con = (Context)Ancestor(typeof(Context));
				if (con != null) con.SetContextVariables();
				if (con is OnApplication)
				{ // refress the process data for the application
					OnApplication app = (OnApplication)con;
					Process proc = app.Application.Process;
					m_log.paragraph("Application process refreshed.");
				}
			}
			//Logger.getOnly().result(this); derivative classes call this after processing results
		}

		/// <summary>
		/// Overriden in derived classes to set context variables available to child instructions.
		/// </summary>
		protected virtual void SetContextVariables(){}

		/// <summary>
		/// Adds an instruction to this context.
		/// </summary>
		/// <param name="ins">The instruction to add.</param>
		public void Add(Instruction ins)
		{
			m_log.isNotNull(ins, base.makeNameTag() + " Tried to add a null instruction");
			m_instructions.Add(ins);
		}

		/// <summary>
		/// Adds an instruction to this context immediately after the one referenced.
		/// </summary>
		/// <param name="afterThis">The instruction to add ins after.</param>
		/// <param name="ins">The instruction to add.</param>
		public void Add(Instruction afterThis, Instruction ins)
		{
			m_log.isNotNull(afterThis, base.makeNameTag() + " Tried to reference a null instruction");
			m_log.isNotNull(ins, base.makeNameTag() + " Tried to add a null instruction");
			m_instructions.Insert(m_instructions.IndexOf(afterThis)+1, ins);
		}

		public int Count
		{
			get
			{
				return m_instructions.Count;
			}
		}

		public AccessibilityHelper Accessibility
		{
			get
			{
				if ((this as OnDialog) != null)
					return new AccessibilityHelper((this as OnDialog).Title);
				if (m_ah != null)
					return m_ah;
				Context ancestor = (Context) Ancestor(typeof(Context));
				if (ancestor != null)
					return ancestor.Accessibility;
				return null;
			}
		}

		/// <summary>
		/// Gets the image of the specified data. If name is null,
		/// the instruction's sequence number is returned.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "number";
			switch (name)
			{
				case "on-fail":	return m_onFail;
				case "on-pass":	return m_onPass;
				default:		return base.GetDataImage(name);
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
			if (m_ah != null)
			{
				AccessibleRole ar;
				string ars, an;
				try {ar = m_ah.Role; ars = ar.ToString();}
				catch(Exception e){ars = e.Message;}
				try {an = m_ah.Name;}
				catch(Exception e){an = e.Message;}
				image += @" ah="""+ars+@":"+Utilities.attrText(an)+@"""";
			}
			else image += @" ah=""null""";
			if (m_onFail != null) image += @" on-fail="""+m_onFail+@"""";
			if (m_onPass != null) image += @" on-pass="""+m_onPass+@"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			image += @" closed="""+m_tag+@"""";
			if ((m_onFail != null) && (1 == m_logLevel)) image += @" on-fail=""" + m_onFail + @"""";
			if ((m_onPass != null) && (1 == m_logLevel)) image += @" on-pass=""" + m_onPass + @"""";
			return image;
		}
	}
}
