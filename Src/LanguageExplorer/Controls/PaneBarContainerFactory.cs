// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;

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
		/// <param name="flexComponentParameterObject">Parameter object that contains the required three interfaces.</param>
		/// <param name="parentControl">Parent control for the new PaneBarContainer</param>
		/// <param name="mainChildControl">Main child control for the new PaneBarContainer</param>
		/// <returns>The new PaneBarContainer instance.</returns>
		internal static PaneBarContainer Create(FlexComponentParameterObject flexComponentParameterObject, Control parentControl, Control mainChildControl)
		{
			parentControl.SuspendLayout();
			var newPaneBarContainer = new PaneBarContainer(mainChildControl)
			{
				Dock = DockStyle.Fill
			};
			newPaneBarContainer.InitializeFlexComponent(flexComponentParameterObject);
			if (mainChildControl is IPaneBarUser)
			{
				var asPaneBarUser = (IPaneBarUser)mainChildControl;
				asPaneBarUser.MainPaneBar = newPaneBarContainer.PaneBar;
			}
			if (mainChildControl is IFlexComponent)
			{
				var asFlexComponent = (IFlexComponent) mainChildControl;
				asFlexComponent.InitializeFlexComponent(flexComponentParameterObject);
			}
			parentControl.Controls.Add(newPaneBarContainer);
			parentControl.ResumeLayout();
			mainChildControl.BringToFront();

			return newPaneBarContainer;
		}

		/// <summary>
		/// Create a pair of PaneBarContainer instances for <paramref name="parentMultiPane"/>.
		/// </summary>
		/// <param name="flexComponentParameterObject">Parameter object that contains the required three interfaces.</param>
		/// <param name="parentMultiPane">Parent control for the new PaneBarContainers</param>
		/// <param name="firstChildControl">Main child control for the new PaneBarContainer</param>
		/// <param name="secondChildControl">Main child control for the new PaneBarContainer</param>
		/// <returns>The new PaneBarContainer instance.</returns>
		internal static void Create(FlexComponentParameterObject flexComponentParameterObject, MultiPane parentMultiPane, Control firstChildControl, Control secondChildControl)
		{
			parentMultiPane.SuspendLayout();
			var newPaneBarContainer1 = new PaneBarContainer(firstChildControl)
			{
				Dock = DockStyle.Fill
			};
			newPaneBarContainer1.InitializeFlexComponent(flexComponentParameterObject);
			parentMultiPane.FirstControl = newPaneBarContainer1;
			var newPaneBarContainer2 = new PaneBarContainer(secondChildControl)
			{
				Dock = DockStyle.Fill
			};
			newPaneBarContainer2.InitializeFlexComponent(flexComponentParameterObject);
			parentMultiPane.SecondControl = newPaneBarContainer2;
			parentMultiPane.ResumeLayout();
			firstChildControl.BringToFront();
		}

		/// <summary>
		/// Remove <paramref name="paneBarContainer"/> from parent control and dispose it.
		/// </summary>
		/// <param name="paneBarContainer">The PaneBarContainer to remove and dispose.</param>
		internal static void RemoveFromParentAndDispose(PaneBarContainer paneBarContainer)
		{
			var parentControl = paneBarContainer.Parent;
			parentControl.SuspendLayout();
			parentControl.Controls.Remove(paneBarContainer);
			paneBarContainer.Dispose();
			parentControl.ResumeLayout();
		}
	}
}