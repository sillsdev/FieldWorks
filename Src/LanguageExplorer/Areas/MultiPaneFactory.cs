// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Create and destroy a MultiPane instance.
	/// </summary>
	internal static class MultiPaneFactory
	{
		/// <summary>
		/// Create a new nested MultiPane instance, which is nested in anotehr MultiPane control
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="multiPaneParameters">Boat load of goodies need to create a MultiPane.</param>
		/// <returns>A newly created nested MultiPane instance.</returns>
		internal static MultiPane CreateNestedMultiPane(FlexComponentParameters flexComponentParameters, MultiPaneParameters multiPaneParameters)
		{
			var nestedMultiPane = new MultiPane(multiPaneParameters);
			var firstControl = multiPaneParameters.FirstControlParameters.Control;
			InitializeSubControl(nestedMultiPane, firstControl, true);
			if (firstControl is IFlexComponent)
			{
				((IFlexComponent)firstControl).InitializeFlexComponent(flexComponentParameters);
			}
			var secondControl = multiPaneParameters.SecondControlParameters.Control;
			InitializeSubControl(nestedMultiPane, secondControl, false);
			if (secondControl is IFlexComponent)
			{
				((IFlexComponent)secondControl).InitializeFlexComponent(flexComponentParameters);
			}

			firstControl.BringToFront();
			secondControl.BringToFront();

			return nestedMultiPane;
		}

		private static void InitializeSubControl(MultiPane parentMultiPane, Control subControl, bool isFirstControl)
		{
			var contentClassName = subControl.GetType().FullName;
			if (subControl.AccessibleName == null)
				subControl.AccessibleName = contentClassName;
			if (!(subControl is IMainUserControl))
			{
#if RANDYTODO
				// TODO: We tolerate other controls, such as those Panel hacks, while the tool displays are being set up.
				// TODO: Once those hacks are gone, then this can be enabled.
				throw new ApplicationException(
					"FLEx can only handle controls which implement IMainUserControl. '" + contentClassName + "' does not.");
#endif
			}
			subControl.Dock = DockStyle.Fill;

			// we add this before Initializing so that this child control will have access
			// to its eventual height and width, in case it needs to make initialization
			// decisions based on that.  for example, if the child is another multipane, it
			// will use this to come up with a reasonable default location for its splitter.
			if (subControl is MultiPane)
			{
				var mpSubControl = subControl as MultiPane;
				mpSubControl.ParentSizeHint = parentMultiPane.ParentSizeHint;
				// cause our subcontrol to inherit our DefaultPrintPane property.
				mpSubControl.DefaultPrintPaneId = parentMultiPane.DefaultPrintPaneId;
			}
			// we add this before Initializing so that this child control will have access
			// to its eventual height and width, in case it needs to make initialization
			// decisions based on that.  for example, if the child is another multipane, it
			// will use this to come up with a reasonable default location for its splitter.
			if (subControl is PaneBarContainer)
			{
				var mpSubControl = subControl as PaneBarContainer;
				mpSubControl.ParentSizeHint = parentMultiPane.ParentSizeHint;
				// cause our subcontrol to inherit our DefaultPrintPane property.
				mpSubControl.DefaultPrintPaneId = parentMultiPane.DefaultPrintPaneId;
			}
			if (isFirstControl)
			{
				subControl.AccessibleName += ".First";
			}
			else
			{
				subControl.AccessibleName += ".Second";
			}
			subControl.ResumeLayout();
		}

		/// <summary>
		/// Create a new MultiPane instance where both main child controls are PaneBarContainer instances
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="mainCollapsingSplitContainer">Parent control for the new MultiPane, which goes into its SecondControl</param>
		/// <param name="multiPaneParameters"></param>
		/// <returns>New instance of MultiPane that has PaneBarContainers as it two main controls.</returns>
		internal static MultiPane CreateInMainCollapsingSplitContainer(FlexComponentParameters flexComponentParameters, ICollapsingSplitContainer mainCollapsingSplitContainer, MultiPaneParameters multiPaneParameters)
		{
			// All tools with MultiPane as main second child of top level mainCollapsingSplitContainer
			// have PaneBarContainer children, which then have other main children,
			var mainCollapsingSplitContainerAsControl = (Control)mainCollapsingSplitContainer;
			mainCollapsingSplitContainerAsControl.SuspendLayout();
			// Get rid of any other controls
			var oldSecondControl = mainCollapsingSplitContainer.SecondControl;
			mainCollapsingSplitContainerAsControl.Controls.Remove(oldSecondControl);
			oldSecondControl.Dispose();
			var newMultiPane = new MultiPane(multiPaneParameters);

			InitializeSubControl(newMultiPane, multiPaneParameters.FirstControlParameters.Control, true);
			InitializeSubControl(newMultiPane, multiPaneParameters.SecondControlParameters.Control, false);

			newMultiPane.InitializeFlexComponent(flexComponentParameters);
			mainCollapsingSplitContainer.SecondControl = newMultiPane;
			var firstControl = multiPaneParameters.FirstControlParameters.Control;
			if (firstControl is IFlexComponent)
			{
				((IFlexComponent)firstControl).InitializeFlexComponent(flexComponentParameters);
			}
			var secondControl = multiPaneParameters.SecondControlParameters.Control;
			if (secondControl is IFlexComponent)
			{
				((IFlexComponent)secondControl).InitializeFlexComponent(flexComponentParameters);
			}
			mainCollapsingSplitContainerAsControl.ResumeLayout();
			multiPaneParameters.FirstControlParameters.Control.BringToFront();
			multiPaneParameters.SecondControlParameters.Control.BringToFront();
			firstControl.BringToFront();
			secondControl.BringToFront();

			return newMultiPane;
		}

		/// <summary>
		/// Create a new MultiPane instance where both main child controls are PaneBarContainer instances
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="mainCollapsingSplitContainer">Parent control for the new MultiPane, which goes into its SecondControl</param>
		/// <param name="multiPaneParameters"></param>
		/// <param name="firstControl">Child control of new Left/Top PaneBarContainer instance</param>
		/// <param name="firstlabel"></param>
		/// <param name="secondControl">Child control of new Right/Bottom PaneBarContainer instance</param>
		/// <param name="secondlabel"></param>
		/// <param name="paneBar"></param>
		/// <returns>New instance of MultiPane that has PaneBarContainers as it two main controls.</returns>
		internal static MultiPane CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(FlexComponentParameters flexComponentParameters, ICollapsingSplitContainer mainCollapsingSplitContainer, MultiPaneParameters multiPaneParameters, Control firstControl, string firstlabel, Control secondControl, string secondlabel, PaneBar paneBar)
		{
			var mainCollapsingSplitContainerAsControl = (Control)mainCollapsingSplitContainer;
			mainCollapsingSplitContainerAsControl.SuspendLayout();

			multiPaneParameters.FirstControlParameters = new SplitterChildControlParameters
			{
				Control = PaneBarContainerFactory.Create(flexComponentParameters, firstControl),
				Label = firstlabel
			};
			multiPaneParameters.SecondControlParameters = new SplitterChildControlParameters
			{
				Control = PaneBarContainerFactory.Create(flexComponentParameters, paneBar, secondControl),
				Label = secondlabel
			};

			var multiPane = CreateInMainCollapsingSplitContainer(flexComponentParameters, mainCollapsingSplitContainer, multiPaneParameters);
			if (secondControl is IPaneBarUser)
			{
				((IPaneBarUser)secondControl).MainPaneBar = ((IPaneBarContainer)multiPane.SecondControl).PaneBar;
			}

			mainCollapsingSplitContainerAsControl.ResumeLayout();
			return multiPane;
		}

		/// <summary>
		/// Create a new MultiPane instance where both main child controls are PaneBarContainer instances
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="mainCollapsingSplitContainer">Parent control for the new MultiPane, which goes into its SecondControl</param>
		/// <param name="multiPaneParameters"></param>
		/// <param name="firstControl">Child control of new Left/Top PaneBarContainer instance</param>
		/// <param name="firstlabel"></param>
		/// <param name="secondControl">Child control of new Right/Bottom PaneBarContainer instance</param>
		/// <param name="secondlabel"></param>
		/// <returns>New instance of MultiPane that has PaneBarContainers as it two main controls.</returns>
		internal static MultiPane CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(FlexComponentParameters flexComponentParameters, ICollapsingSplitContainer mainCollapsingSplitContainer, MultiPaneParameters multiPaneParameters, Control firstControl, string firstlabel, Control secondControl, string secondlabel)
		{
			return CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(flexComponentParameters, mainCollapsingSplitContainer, multiPaneParameters, firstControl, firstlabel, secondControl, secondlabel, new PaneBar());
		}

		/// <summary>
		/// Create a new MultiPane instance where both main child controls are PaneBarContainer instances
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="mainCollapsingSplitContainer">Parent control for the new MultiPane, which goes into its SecondControl</param>
		/// <param name="tool"></param>
		/// <param name="multiPaneId"></param>
		/// <param name="firstControl">Child control of new Left/Top PaneBarContainer instance</param>
		/// <param name="firstlabel">Label of the Left/Top control of the MultiPane</param>
		/// <param name="secondControl">Child control of new Right/Bottom PaneBarContainer instance</param>
		/// <param name="secondlabel">Label of the Right/Bottom control of the MultiPane</param>
		/// <param name="orientation">Orientation of the splitter bar.</param>
		/// <returns></returns>
		internal static MultiPane CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(FlexComponentParameters flexComponentParameters, ICollapsingSplitContainer mainCollapsingSplitContainer, ITool tool, string multiPaneId, Control firstControl, string firstlabel, Control secondControl, string secondlabel, Orientation orientation)
		{
#if RANDYTODO
		// TODO: Get current users switched to one of the other overloaded methods.
#endif
			var multiPaneParameters = new MultiPaneParameters
			{
				Orientation = orientation,
				AreaMachineName = tool.AreaMachineName,
				Id = multiPaneId,
				ToolMachineName = tool.MachineName
			};
			return CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(flexComponentParameters, mainCollapsingSplitContainer, multiPaneParameters, firstControl, firstlabel, secondControl, secondlabel);
		}

		/// <summary>
		/// Remove <paramref name="multiPane"/> from parent control and dispose it.
		/// </summary>
		/// <param name="mainCollapsingSplitContainer"></param>
		/// <param name="multiPane">The MultiPane to remove and dispose.</param>
		/// <param name="recordClerk">The RecordClerk data member to set to null.</param>
		internal static void RemoveFromParentAndDispose(ICollapsingSplitContainer mainCollapsingSplitContainer, ref MultiPane multiPane, ref RecordClerk recordClerk)
		{
			var parentControl = multiPane.Parent;
			parentControl.SuspendLayout();
			parentControl.Controls.Remove(multiPane);
			multiPane.Dispose();
			mainCollapsingSplitContainer.SecondControl = new Panel(); // Keep something current in it.
			parentControl.ResumeLayout();
			multiPane = null;

			// recordClerk is disposed by XWorksViewBase in the call "multiPane.Dispose()", but just set the variable to null here.
			// "recordClerk" is a data member of the caller. Rather than have every caller set its own data member to null,
			// we do it here for all of them.
			recordClerk = null;
		}
	}
}