// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: HoverOver.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using System.Collections;
using System.Xml;

namespace GuiTestDriver
{
	/// <summary>
	/// Causes the cursor to hover over a specified GUI element.
	/// </summary>
	public class HoverOver : ActionBase, IPathVisitor
	{
		string m_message = "";

		public HoverOver()
		{
			m_path = null;
			m_tag  = "hover-over";
		}

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
			m_log.isNotNull(Path, makeNameTag() + "Hover-over instruction must have a path.");
			m_log.isTrue(Path != "", makeNameTag() + "Hover-over instruction must have a non-empty path.");
			return true;
		}

		public override void Execute()
		{
			base.Execute();
			Context con = (Context)Ancestor(typeof(Context));
			m_log.isNotNull(con, makeNameTag() + "Hover-over must occur in some context");
			AccessibilityHelper ah = con.Accessibility;
			m_log.isNotNull(ah, makeNameTag() + "The hover-over context is not accessible");
			m_path = Utilities.evalExpr(m_path);

			// Use the path
			if (m_path != null && m_path != "")
			{
				GuiPath gpath = new GuiPath(m_path);
				m_log.isNotNull(gpath, makeNameTag() + "attribute path='" + m_path + "' not parsed");
				string BadPath = HoverPath(ah, gpath);
				if (BadPath != "") m_log.fail(BadPath);
			}
			else m_log.fail(makeNameTag() + "attribute 'path' must be set.");
			Finished = true; // tell do-once it's done
		}

		string HoverPath(AccessibilityHelper ah, GuiPath gpath)
		{
			ah = gpath.FindInGui(ah, this);
			visitNode(ah);
			return m_message;
		}

		/// <summary>
		/// method to apply when a non-terminal node has been found
		/// </summary>
		/// <param name="ah"></param>
		public void visitNode(AccessibilityHelper ah)
		{ // does this ah need to be clicked or something to get to its children?
			ah.MoveMouseOverMe(); // hover
			if (1 == m_logLevel)
				m_log.paragraph(makeNameTag() + "HoverOver hovering over &quot;" + ah.Role + ":" + ah.Name + "&quot;");
		}

		/// <summary>
		/// Method to apply when a path node is not found.
		/// No path above it and none below will call this method;
		/// only the one not found.
		/// </summary>
		/// <param name="path">The path step last tried.</param>
		public void notFound(GuiPath path)
		{
			m_message = "Item '" + path.Role + ":" + path.Name + "' not found for hover";
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "path";
			switch (name)
			{
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
			return image;
		}
	}
}
