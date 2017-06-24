// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.XWorks;
using SIL.Xml;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public class ConcordanceControlBase : UserControl, IMainContentControl
	{
		protected XmlNode m_configurationParameters;
		protected FdoCache m_cache;
		protected OccurrencesOfSelectedUnit m_clerk;
		protected IHelpTopicProvider m_helpTopicProvider;

		public ConcordanceControlBase()
		{}

		internal ConcordanceControlBase(OccurrencesOfSelectedUnit clerk)
		{
			m_clerk = clerk;
		}

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
		public virtual void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			m_helpTopicProvider = PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
#if RANDYTODO
			m_configurationParameters = configurationParameters;
#endif
			m_cache = PropertyTable.GetValue<FdoCache>("cache");
#if RANDYTODO
			var name = RecordClerk.GetCorrespondingPropertyName(XmlUtils.GetAttributeValue(configurationParameters, "clerk"));
			m_clerk = PropertyTable.GetValue<OccurrencesOfSelectedUnit>(name) ?? (OccurrencesOfSelectedUnit)RecordClerkFactory.CreateClerk(PropertyTable, Publisher, Subscriber, true);
			m_clerk.ConcordanceControl = this;
#endif
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

			var paneBarContainer = Parent as IPaneBarContainer;
			if (paneBarContainer == null) return;
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
#if RANDYTODO
				// TODO: Do something to make the area name(s) be constants, here and for other cases.
				// TODO: Do the same for tool names.
#endif
				return "textAndWords";
			}
		}

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			if (disposing)
			{
				if (m_clerk != null)
					m_clerk.ConcordanceControl = null;

				// Don't dispose of the clerk, since it can monitor relevant PropChanges
				// that affect the NeedToReloadVirtualProperty.
			}
			m_clerk = null;
			m_cache = null;

			base.Dispose(disposing);
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
