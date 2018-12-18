// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Create and destroy a MultiPane instance.
	/// </summary>
	internal static class MultiPaneFactory
	{
		/// <summary>
		/// Create a new nested MultiPane instance, which is nested in another MultiPane control
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		/// <param name="multiPaneParameters">Boat load of goodies need to create a MultiPane.</param>
		/// <returns>A newly created nested MultiPane instance.</returns>
		internal static MultiPane CreateNestedMultiPane(FlexComponentParameters flexComponentParameters, MultiPaneParameters multiPaneParameters)
		{
			var firstControl = multiPaneParameters.FirstControlParameters.Control;
			firstControl.Dock = DockStyle.Fill;
			if (firstControl is IFlexComponent)
			{
				((IFlexComponent)firstControl).InitializeFlexComponent(flexComponentParameters);
			}
			var secondControl = multiPaneParameters.SecondControlParameters.Control;
			secondControl.Dock = DockStyle.Fill;
			if (secondControl is IFlexComponent)
			{
				((IFlexComponent)secondControl).InitializeFlexComponent(flexComponentParameters);
			}
			var nestedMultiPane = new MultiPane(multiPaneParameters);
			InitializeSubControl(nestedMultiPane, firstControl, true);
			InitializeSubControl(nestedMultiPane, secondControl, false);
			firstControl.BringToFront();
			secondControl.BringToFront();
			return nestedMultiPane;
		}

		private static void InitializeSubControl(MultiPane parentMultiPane, Control subControl, bool isFirstControl)
		{
			var contentClassName = subControl.GetType().FullName;
			if (subControl.AccessibleName == null)
			{
				subControl.AccessibleName = contentClassName;
			}
			if (!(subControl is IMainUserControl))
			{
				throw new ApplicationException($"FLEx can only handle controls which implement IMainUserControl. '{contentClassName}' does not.");
			}
			subControl.Dock = DockStyle.Fill;
			// we add this before Initializing so that this child control will have access
			// to its eventual height and width, in case it needs to make initialization
			// decisions based on that.  for example, if the child is another multipane, it
			// will use this to come up with a reasonable default location for its splitter.
			if (subControl is MultiPane)
			{
				var mpSubControl = (MultiPane)subControl;
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
				var mpSubControl = (PaneBarContainer)subControl;
				mpSubControl.ParentSizeHint = parentMultiPane.ParentSizeHint;
				// cause our subcontrol to inherit our DefaultPrintPane property.
				mpSubControl.DefaultPrintPaneId = parentMultiPane.DefaultPrintPaneId;
			}
			if (isFirstControl)
			{
				subControl.AccessibleName += @".First";
			}
			else
			{
				subControl.AccessibleName += @".Second";
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
			var firstControl = multiPaneParameters.FirstControlParameters.Control;
			(firstControl as IFlexComponent)?.InitializeFlexComponent(flexComponentParameters);
			var secondControl = multiPaneParameters.SecondControlParameters.Control;
			(secondControl as IFlexComponent)?.InitializeFlexComponent(flexComponentParameters);
			// All tools with MultiPane as main second child of top level mainCollapsingSplitContainer
			// have PaneBarContainer children, which then have other main children,
			var newMultiPane = new MultiPane(multiPaneParameters);
			InitializeSubControl(newMultiPane, multiPaneParameters.FirstControlParameters.Control, true);
			InitializeSubControl(newMultiPane, multiPaneParameters.SecondControlParameters.Control, false);
			newMultiPane.InitializeFlexComponent(flexComponentParameters);
			mainCollapsingSplitContainer.SecondControl = newMultiPane;
			// I (RBR) used to do "BringToFront()" for "firstControl" and "secondControl", but proved to be in the wrong place.
			// I now do that in other locations, and it works better.
			// The problem I was trying to solve was the controls were clipped by the pane bar.
			// It works *much* better in the new locations in "PaneBarContainerFactory".
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
		/// <param name="firstPaneBar"></param>
		/// <param name="secondControl">Child control of new Right/Bottom PaneBarContainer instance</param>
		/// <param name="secondlabel"></param>
		/// <param name="secondPaneBar"></param>
		/// <returns>New instance of MultiPane that has PaneBarContainers as it two main controls.</returns>
		internal static MultiPane CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(FlexComponentParameters flexComponentParameters, ICollapsingSplitContainer mainCollapsingSplitContainer, MultiPaneParameters multiPaneParameters, Control firstControl, string firstlabel, PaneBar firstPaneBar, Control secondControl, string secondlabel, PaneBar secondPaneBar)
		{
			var mainCollapsingSplitContainerAsControl = (Control)mainCollapsingSplitContainer;
			mainCollapsingSplitContainerAsControl.SuspendLayout();
			multiPaneParameters.FirstControlParameters = new SplitterChildControlParameters
			{
				Control = PaneBarContainerFactory.Create(flexComponentParameters, firstControl, firstPaneBar),
				Label = firstlabel
			};
			multiPaneParameters.SecondControlParameters = new SplitterChildControlParameters
			{
				Control = PaneBarContainerFactory.Create(flexComponentParameters, secondControl, secondPaneBar),
				Label = secondlabel
			};
			var multiPane = CreateInMainCollapsingSplitContainer(flexComponentParameters, mainCollapsingSplitContainer, multiPaneParameters);
			if (secondControl is IPaneBarUser)
			{
				var aspbUser = (IPaneBarUser)secondControl;
				if (aspbUser.MainPaneBar == null)
				{
					((IPaneBarUser)secondControl).MainPaneBar = ((IPaneBarContainer)multiPane.SecondControl).PaneBar;
				}
			}
			mainCollapsingSplitContainerAsControl.ResumeLayout();
			return multiPane;
		}

		internal static ConcordanceContainer CreateConcordanceContainer(FlexComponentParameters flexComponentParameters, ICollapsingSplitContainer mainCollapsingSplitContainer, MultiPaneParameters concordanceContainerParameters, MultiPaneParameters leftSideNestedMultiPaneParameters)
		{
			var mainCollapsingSplitContainerAsControl = (Control)mainCollapsingSplitContainer;
			mainCollapsingSplitContainerAsControl.SuspendLayout();
			// NB: Caller creates leftSideNestedMultiPaneParameters.FirstControlParameters and leftSideNestedMultiPaneParameters.SecondControlParameters
			// and sets the Control & Label values of each.
			var nestedLeftSideMultiPane = CreateNestedMultiPane(flexComponentParameters, leftSideNestedMultiPaneParameters);
			// concordanceContainerParameters.FirstControlParameters.Control & concordanceContainerParameters.SecondControlParameters.Control should both be null,
			// but the Labels of each should be present.
			concordanceContainerParameters.FirstControlParameters.Control = nestedLeftSideMultiPane;
			// Set by caller, including PBC. concordanceContainerParameters.SecondControlParameters.Control;
			var concordanceContainer = new ConcordanceContainer(concordanceContainerParameters);
			var concordanceContainerAsControl = (Control)concordanceContainer;
			concordanceContainerAsControl.SuspendLayout();
			InitializeSubControl(concordanceContainer, concordanceContainerParameters.FirstControlParameters.Control, true);
			InitializeSubControl(concordanceContainer, concordanceContainerParameters.SecondControlParameters.Control, false);
			concordanceContainer.InitializeFlexComponent(flexComponentParameters);
			mainCollapsingSplitContainer.SecondControl = concordanceContainer;
			var firstControl = concordanceContainerParameters.FirstControlParameters.Control;
			if (firstControl is IFlexComponent)
			{
				((IFlexComponent)firstControl).InitializeFlexComponent(flexComponentParameters);
			}
			var secondControl = concordanceContainerParameters.SecondControlParameters.Control;
			firstControl.BringToFront();
			secondControl.BringToFront();
			concordanceContainerAsControl.ResumeLayout();
			mainCollapsingSplitContainerAsControl.ResumeLayout();
			return concordanceContainer;
		}

		/// <summary>
		/// Remove <paramref name="multiPane"/> from parent control and dispose it and set <paramref name="multiPane"/> to null.
		/// </summary>
		internal static void RemoveFromParentAndDispose(ICollapsingSplitContainer mainCollapsingSplitContainer, ref MultiPane multiPane)
		{
			// Re-setting SecondControl, will dispose its multiPane.
			mainCollapsingSplitContainer.SecondControl = null;
			multiPane = null;
		}
	}
}