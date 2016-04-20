// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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
			if (mainChildControl is IFlexComponent)
			{
				var asFlexComponent = (IFlexComponent)mainChildControl;
				asFlexComponent.InitializeFlexComponent(flexComponentParameters);
			}
			parentControl.Controls.Add(newPaneBarContainer);
			parentControl.ResumeLayout();
			mainChildControl.BringToFront();

			return newPaneBarContainer;
		}

		/// <summary>
		/// Create a new PaneBarContainer
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="mainChildControl">Main child control for the new PaneBarContainer</param>
		/// <returns>The new PaneBarContainer instance.</returns>
		internal static PaneBarContainer Create(FlexComponentParameters flexComponentParameters, Control mainChildControl)
		{
			var newPaneBarContainer = new PaneBarContainer(mainChildControl)
			{
				Dock = DockStyle.Fill
			};
			if (mainChildControl is IFlexComponent)
			{
				((IFlexComponent)mainChildControl).InitializeFlexComponent(flexComponentParameters);
			}
			if (mainChildControl is MultiPane)
			{
				var mainChildControlAsMultiPane = (MultiPane)mainChildControl;
				// Set first control of MultiPane for PaneBar, if it is IPaneBarUser.
				if (mainChildControlAsMultiPane.FirstControl is IPaneBarUser)
				{
					((IPaneBarUser)mainChildControlAsMultiPane.FirstControl).MainPaneBar = newPaneBarContainer.PaneBar;
				}
			}

			return newPaneBarContainer;
		}

		/// <summary>
		/// Create a new PaneBarContainer
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="paneBar"></param>
		/// <param name="mainChildControl">Main child control for the new PaneBarContainer</param>
		/// <returns>The new PaneBarContainer instance.</returns>
		internal static PaneBarContainer Create(FlexComponentParameters flexComponentParameters, PaneBar.PaneBar paneBar, Control mainChildControl)
		{
			var newPaneBarContainer = new PaneBarContainer(paneBar, mainChildControl)
			{
				Dock = DockStyle.Fill
			};
			if (mainChildControl is IFlexComponent)
			{
				((IFlexComponent)mainChildControl).InitializeFlexComponent(flexComponentParameters);
			}
			if (mainChildControl is MultiPane)
			{
				var mainChildControlAsMultiPane = (MultiPane)mainChildControl;
				// Set first control of MultiPane for PaneBar, if it is IPaneBarUser.
				if (mainChildControlAsMultiPane.FirstControl is IPaneBarUser)
				{
					((IPaneBarUser)mainChildControlAsMultiPane.FirstControl).MainPaneBar = newPaneBarContainer.PaneBar;
				}
			}

			return newPaneBarContainer;
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

		internal static string CreateShowHiddenFieldsPropertyName(string toolMachineName)
		{
			return String.Format("{0}-{1}", LanguageExplorerResources.ksShowHiddenFields, toolMachineName);
		}
	}
}