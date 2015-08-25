using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.IText
{
	public class ConcordanceControlBase : UserControl, IMainContentControl, IFWDisposable
	{
		protected XmlNode m_configurationParameters;
		protected FdoCache m_cache;
		protected OccurrencesOfSelectedUnit m_clerk;
		protected IHelpTopicProvider m_helpTopicProvider;

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
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public virtual void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;

			m_helpTopicProvider = PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
#if RANDYTODO
			m_configurationParameters = configurationParameters;
#endif
			m_cache = PropertyTable.GetValue<FdoCache>("cache");
#if RANDYTODO
			var name = RecordClerk.GetCorrespondingPropertyName(XmlUtils.GetAttributeValue(configurationParameters, "clerk"));
#else
			var name = "FAKE_CLERK_NAME_FIX_ME";
#endif
			m_clerk = PropertyTable.GetValue<OccurrencesOfSelectedUnit>(name) ?? (OccurrencesOfSelectedUnit)RecordClerkFactory.CreateClerk(PropertyTable, Publisher, Subscriber, true);
			m_clerk.ConcordanceControl = this;
		}

		#endregion

		public virtual string AccName
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Get/set string that will trigger a message box to show.
		/// </summary>
		/// <remarks>Set to null or string.Empty to not show the message box.</remarks>
		public string MessageBoxTrigger { get; set; }

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			CheckDisposed();
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates");
			targetCandidates.Add(this);
			return ContainsFocus ? this : null;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			var paneBarContainer = Parent as PaneBarContainer;
			if (paneBarContainer != null)
				paneBarContainer.PaneBar.Text = ITextStrings.ksSpecifyConcordanceCriteria;
		}

		public bool PrepareToGoAway()
		{
			CheckDisposed();
			return true;
		}

		public string AreaName
		{
			get
			{
				CheckDisposed();
				return XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "area", "unknown");
			}
		}

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		// True after the first time we do it.
		internal protected bool HasLoadedMatches { get; protected set; }
		// True while loading matches, to prevent recursive call.
		internal protected bool IsLoadingMatches { get; protected set; }

		internal protected void LoadMatches()
		{
			LoadMatches(true);
		}

		internal protected void LoadMatches(bool fLoadVirtualProperty)
		{
			var occurrences = SearchForMatches();
			var decorator = (ConcDecorator) ((DomainDataByFlidDecoratorBase) m_clerk.VirtualListPublisher).BaseSda;
			// Set this BEFORE we start loading, otherwise, calls to ReloadList triggered here just make it empty.
			HasLoadedMatches = true;
			IsLoadingMatches = true;
			try
			{
				m_clerk.OwningObject = m_cache.LangProject;
				decorator.SetOccurrences(m_cache.LangProject.Hvo, occurrences);
				m_clerk.UpdateList(true);
			}
			finally
			{
				IsLoadingMatches = false;
			}
		}

		protected ConcDecorator ConcDecorator
		{
			get { return ((ObjectListPublisher) m_clerk.VirtualListPublisher).BaseSda as ConcDecorator; }
		}

		protected virtual List<IParaFragment> SearchForMatches()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// If asked to Refresh, update your results list.
		/// </summary>
		public bool RefreshDisplay()
		{
			LoadMatches(true);
			//I claim that all descendants which are refreshable have been refreshed -naylor
			return true;
		}

		#region Implementation of IMainUserControl

		/// <summary>
		/// Get or set the name to be used by the accessibility object.
		/// </summary>
		string IMainUserControl.AccName { get; set; }

		#endregion
	}
}
