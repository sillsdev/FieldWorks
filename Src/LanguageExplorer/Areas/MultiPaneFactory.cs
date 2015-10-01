// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.CoreImpl;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Create and destroy a MultiPane instance.
	/// </summary>
	internal static class MultiPaneFactory
	{
		/// <summary>
		/// Create a new MultiPane instance where both main child controls are PaneBarContainer instances
		/// </summary>
		/// <param name="propertyTable">The property table</param>
		/// <param name="publisher">The Publisher</param>
		/// <param name="subscriber">The Subscriber</param>
		/// <param name="parentControl">Parent control for the new MultiPane</param>
		/// <param name="tool"></param>
		/// <param name="multiPaneId"></param>
		/// <param name="firstControl">Child control of new Left/Top PaneBarContainer instance</param>
		/// <param name="firstlabel">Label of the Left/Top control of the MultiPane</param>
		/// <param name="secondControl">Child control of new Right/Bottom PaneBarContainer instance</param>
		/// <param name="secondlabel">Label of the Right/Bottom control of the MultiPane</param>
		/// <param name="orientation">Orientation of the splitter bar.</param>
		/// <returns></returns>
		internal static MultiPane Create(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, Control parentControl, ITool tool, string multiPaneId, Control firstControl, string firstlabel, Control secondControl, string secondlabel, Orientation orientation)
		{
			// All tools with MultiPane as main second child of top level mainCollapsingSplitContainer
			// have PaneBarContainer children, which then have other main children,
			parentControl.SuspendLayout();
			var newMultiPane = new MultiPane(tool.MachineName, tool.AreaMachineName, multiPaneId)
			{
				Dock = DockStyle.Fill,
				Orientation = orientation,
				FirstLabel = firstlabel,
				SecondLabel = secondlabel
			};
			PaneBarContainerFactory.Create(propertyTable, publisher, subscriber, newMultiPane, firstControl, secondControl);

			newMultiPane.InitializeFlexComponent(propertyTable, publisher, subscriber);
			parentControl.Controls.Add(newMultiPane);
			parentControl.ResumeLayout();
			firstControl.BringToFront();
			secondControl.BringToFront();

			return newMultiPane;
		}

		/// <summary>
		/// Remove <paramref name="multiPane"/> from parent control and dispose it.
		/// </summary>
		/// <param name="multiPane">The MultiPane to remove and dispose.</param>
		internal static void RemoveFromParentAndDispose(MultiPane multiPane)
		{
			var parentControl = multiPane.Parent;
			parentControl.SuspendLayout();
			parentControl.Controls.Remove(multiPane);
			multiPane.Dispose();
			parentControl.ResumeLayout();
		}
	}
}