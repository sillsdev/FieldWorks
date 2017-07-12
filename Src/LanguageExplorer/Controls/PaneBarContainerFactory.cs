// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas;
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
		/// <param name="mainCollapsingSplitContainer">Put the new PaneBarContainer into SecondControl.</param>
		/// <param name="mainChildControl">Main child control for the new PaneBarContainer</param>
		/// <returns>The new PaneBarContainer instance.</returns>
		internal static PaneBarContainer Create(FlexComponentParameters flexComponentParameters, ICollapsingSplitContainer mainCollapsingSplitContainer, Control mainChildControl)
		{
			var newPaneBarContainer = new PaneBarContainer(mainChildControl);
			mainCollapsingSplitContainer.SecondControl = newPaneBarContainer;
			newPaneBarContainer.InitializeFlexComponent(flexComponentParameters);
			if (mainChildControl is IFlexComponent)
			{
				var asFlexComponent = (IFlexComponent)mainChildControl;
				asFlexComponent.InitializeFlexComponent(flexComponentParameters);
			}
			mainChildControl.BringToFront();

			return newPaneBarContainer;
		}

		/// <summary>
		/// Create a new PaneBarContainer
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="mainCollapsingSplitContainer">Put the new PaneBarContainer into SecondControl.</param>
		/// <param name="paneBar"></param>
		/// <param name="mainChildControl">Main child control for the new PaneBarContainer</param>
		/// <returns>The new PaneBarContainer instance.</returns>
		internal static PaneBarContainer Create(FlexComponentParameters flexComponentParameters, ICollapsingSplitContainer mainCollapsingSplitContainer, PaneBar.PaneBar paneBar, Control mainChildControl)
		{
			var newPaneBarContainer = new PaneBarContainer(paneBar, mainChildControl);
			mainCollapsingSplitContainer.SecondControl = newPaneBarContainer;
			newPaneBarContainer.InitializeFlexComponent(flexComponentParameters);
			if (mainChildControl is IFlexComponent)
			{
				var asFlexComponent = (IFlexComponent)mainChildControl;
				asFlexComponent.InitializeFlexComponent(flexComponentParameters);
			}
			mainChildControl.BringToFront();

			return newPaneBarContainer;
		}

		/// <summary>
		/// Create a new PaneBarContainer, but not one that is the 'SecondControl' of an ICollapsingSplitContainer instance
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
			mainChildControl.BringToFront();

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
			mainChildControl.BringToFront();

			return newPaneBarContainer;
		}

		/// <summary>
		/// Remove <paramref name="paneBarContainer"/> from parent control and dispose it and set clerk to null.
		/// </summary>
		internal static void RemoveFromParentAndDispose(ICollapsingSplitContainer mainCollapsingSplitContainer, DataNavigationManager dataNavigationManager, IRecordClerkRepository recordClerkRepository, ref PaneBarContainer paneBarContainer)
		{
			// Re-setting SecondControl, will dispose paneBarContainer.
			mainCollapsingSplitContainer.SecondControl = null;
			dataNavigationManager.Clerk = null;
			recordClerkRepository.ActiveRecordClerk = null;
			paneBarContainer = null;
		}

		internal static string CreateShowHiddenFieldsPropertyName(string toolMachineName)
		{
			return $"{LanguageExplorerResources.ksShowHiddenFields}-{toolMachineName}";
		}
	}
}