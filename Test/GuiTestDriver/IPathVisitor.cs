// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;

namespace GuiTestDriver
{
	/// <summary>
	/// This interface establishes a couple of methods to tell a GuiPath what to do
	/// when it finds the GUI elements on the display that it represents.
	/// </summary>
	public interface IPathVisitor
	{
		/// <summary>
		/// method to apply when a non-terminal node has been found
		/// </summary>
		/// <param name="ah"></param>
		void visitNode(AccessibilityHelper ah);

		/// <summary>
		/// Method to apply when a path node is not found.
		/// No path above it and none below will call this method;
		/// only the one not found.
		/// </summary>
		/// <param name="path">The path step tried last.</param>
		void notFound(GuiPath path);
	}
}
