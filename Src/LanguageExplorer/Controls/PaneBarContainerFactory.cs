// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Create and destroy a PaneBarContainer instance.
	/// </summary>
	internal static class PaneBarContainerFactory
	{
		/// <summary>
		/// Create a new PaneBarContainer
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="parentControl">Parent control for the new PaneBarContainer</param>
		/// <param name="mainChildControl">Main child control for the new PaneBarContainer</param>
		/// <returns>The new PaneBarContainer instance.</returns>
		internal static PaneBarContainer Create(FlexComponentParameters flexComponentParameters, Control parentControl, Control mainChildControl)
		{
			parentControl.SuspendLayout();
			var newPaneBarContainer = new PaneBarContainer(mainChildControl)
			{
				Dock = DockStyle.Fill
			};
			newPaneBarContainer.InitializeFlexComponent(flexComponentParameters);
			if (mainChildControl is IPaneBarUser)
			{
				var asPaneBarUser = (IPaneBarUser)mainChildControl;
				asPaneBarUser.MainPaneBar = newPaneBarContainer.PaneBar;
			}
			if (mainChildControl is IFlexComponent)
			{
				var asFlexComponent = (IFlexComponent) mainChildControl;
				asFlexComponent.InitializeFlexComponent(flexComponentParameters);
			}
			parentControl.Controls.Add(newPaneBarContainer);
			parentControl.ResumeLayout();
			mainChildControl.BringToFront();

			return newPaneBarContainer;
		}

		/// <summary>
		/// Create a pair of PaneBarContainer instances for <paramref name="parentMultiPane"/>.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="parentMultiPane">Parent control for the new PaneBarContainers</param>
		/// <param name="firstChildControl">Main child control for the new Top/Left PaneBarContainer</param>
		/// <param name="secondChildControl">Main child control for the new Right/Bottom PaneBarContainer</param>
		/// <returns>The new PaneBarContainer instance.</returns>
		internal static void Create(FlexComponentParameters flexComponentParameters, MultiPane parentMultiPane, Control firstChildControl, Control secondChildControl)
		{
			parentMultiPane.SuspendLayout();
			var newPaneBarContainer1 = new PaneBarContainer(firstChildControl)
			{
				Dock = DockStyle.Fill
			};
			newPaneBarContainer1.InitializeFlexComponent(flexComponentParameters);
			parentMultiPane.FirstControl = newPaneBarContainer1;
			var newPaneBarContainer2 = new PaneBarContainer(secondChildControl)
			{
				Dock = DockStyle.Fill
			};
			newPaneBarContainer2.InitializeFlexComponent(flexComponentParameters);
			parentMultiPane.SecondControl = newPaneBarContainer2;
			parentMultiPane.ResumeLayout();
			firstChildControl.BringToFront();
		}

		/// <summary>
		/// Remove <paramref name="paneBarContainer"/> from parent control and dispose it.
		/// </summary>
		/// <param name="paneBarContainer">The PaneBarContainer to remove and dispose.</param>
		/// <param name="recordClerk">The RecordClerk data member to set to null.</param>
		internal static void RemoveFromParentAndDispose(ref PaneBarContainer paneBarContainer, ref RecordClerk recordClerk)
		{
			var parentControl = paneBarContainer.Parent;
			parentControl.SuspendLayout();
			parentControl.Controls.Remove(paneBarContainer);
			paneBarContainer.Dispose();
			parentControl.ResumeLayout();

			paneBarContainer = null;

			// recordClerk is disposed by XWorksViewBase in the call "paneBarContainer.Dispose()", but just set the variable to null here.
			// "recordClerk" is a data member of the caller. Rather than have every caller set its own data member to null,
			// we do it here for all of them.
			recordClerk = null;
		}
	}
}