// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.TextsAndWords;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Dumpster
{
#if RANDYTODO
	// TODO: I don't expect this class to survive, but its useful code moved elsewhere, as ordinary event handlers.
#endif
	/// <summary />
	internal sealed class MorphologyListener : IFlexComponent
	{
		#region Data members

		private IWfiWordformRepository m_wordformRepos;

		#endregion Data members

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			Cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			m_wordformRepos = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
		}

		#endregion

		#region XCore Message handlers

		private LcmCache Cache { get; set; }

		/// <summary>
		/// Try to find a WfiWordform object corresponding the the focus selection.
		/// If successful return its guid, otherwise, return Guid.Empty.
		/// </summary>
		/// <returns></returns>
		private static Guid ActiveWordform(IWfiWordformRepository wordformRepos, IPropertyTable propertyTable)
		{
			var app = propertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
			var window = app?.ActiveMainWindow as IFwMainWnd;
			var activeView = window?.ActiveView;
			if (activeView == null)
			{
				return Guid.Empty;
			}
			var roots = activeView.AllRootBoxes();
			if (!roots.Any())
			{
				return Guid.Empty;
			}
			var helper = SelectionHelper.Create(roots[0].Site);
			var word = helper?.SelectedWord;
			if (word == null || word.Length == 0)
			{
				return Guid.Empty;
			}
			IWfiWordform wordform;
			return wordformRepos.TryGetObject(word, out wordform) ? wordform.Guid : Guid.Empty;
		}

#if RANDYTODO
		public bool OnDisplayEditSpellingStatus(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		/// Called by reflection to implement the command.
		/// </summary>
		public bool OnEditSpellingStatus(object argument)
		{
			// Without checking both the SpellingStatus and (virtual) FullConcordanceCount
			// fields for the ActiveWordform() result, it's too likely that the user
			// will get a puzzling "Target not found" message popping up.  See LT-8717.
			FwLinkArgs link = new FwAppArgs(Cache.ProjectId.Handle, AreaServices.BulkEditWordformsMachineName, Guid.Empty);
			var additionalProps = link.LinkProperties;
			additionalProps.Add(new LinkProperty("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new LinkProperty("LinkSetupInfo", "ReviewUndecidedSpelling"));
			LinkHandler.PublishFollowLinkMessage(Publisher, link);
			return true;
		}

#if RANDYTODO
		public bool OnDisplayViewIncorrectWords(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		public bool OnViewIncorrectWords(object argument)
		{
			FwLinkArgs link = new FwAppArgs(Cache.ProjectId.Handle, AreaServices.AnalysesMachineName, ActiveWordform(m_wordformRepos, PropertyTable));
			var additionalProps = link.LinkProperties;
			additionalProps.Add(new LinkProperty("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new LinkProperty("LinkSetupInfo", "CorrectSpelling"));
			LinkHandler.PublishFollowLinkMessage(Publisher, link);
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		private bool InFriendlyArea => PropertyTable.GetValue<string>(AreaServices.AreaChoice) == AreaServices.TextAndWordsAreaMachineName;

		/// <summary>
		/// Handle enabled menu items for jumping to another tool, or another location in the
		/// current tool.
		/// </summary>
		public bool OnJumpToTool(object commandObject)
		{
			if (!InFriendlyArea)
			{
				return false;
			}
#if RANDYTODO
			var command = (Command)commandObject;
			if (command.TargetId != Guid.Empty)
			{
				LinkHandler.PublishFollowLinkMessage(Publisher, new FwLinkArgs(XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "tool"), command.TargetId));
				command.TargetId = Guid.Empty;	// clear the target for future use.
				return true;
			}
#endif
			return false;
		}
		#endregion XCore Message handlers
	}
}
