using System;
using System.Collections;

namespace GuiTestDriver
{
	/// <summary>
	/// The tracking visitor makes a list of all ah's given it via visitNode.
	/// The list is traversed via StepDownPath() using the owner's visitor.
	/// </summary>
	public class TrackingVisitor : ArrayList, IPathVisitor
	{
		public TrackingVisitor()
		{

		}

		/// <summary>
		/// Collects ah's associated with steps in a path.
		/// </summary>
		/// <param name="ah">The ah of a non-terminal node found along a path</param>
		public void visitNode(AccessibilityHelper ah)
		{  // add them so the first to be visited later is the first one added.
			base.Add(ah);
		}

		/// <summary>
		/// Method to apply when a path node is not found.
		/// No path above it and none below will call this method;
		/// only the one not found.
		/// </summary>
		/// <param name="path">The path step last tried.</param>
		public void notFound(GuiPath path)
		{
			// do nothing
		}

		/// <summary>
		/// Allows a visitor to go through the list of captured Accessibility objects.
		/// </summary>
		/// <param name="visitor">The visitor to step through each ah collected.</param>
		public void StepDownPath(IPathVisitor visitor)
		{
			foreach (AccessibilityHelper ah in this)
			{
				visitor.visitNode(ah);
			}
		}
	}
}
