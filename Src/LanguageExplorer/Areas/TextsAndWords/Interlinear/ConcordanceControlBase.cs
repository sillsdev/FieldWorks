// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class ConcordanceControlBase : UserControl, IMainContentControl
	{
		protected XmlNode m_configurationParameters;
		protected LcmCache m_cache;
		protected MatchingConcordanceRecordList m_recordList;
		protected IHelpTopicProvider m_helpTopicProvider;

		public ConcordanceControlBase()
		{}

		internal ConcordanceControlBase(MatchingConcordanceRecordList recordList)
		{
			m_recordList = recordList;
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

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public virtual void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			m_helpTopicProvider = PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider);
			m_cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			m_recordList.ConcordanceControl = this;
		}

		#endregion

		public virtual string AccName
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Get/set string that will trigger a message box to show.
		/// </summary>
		/// <remarks>Set to null or string.Empty to not show the message box.</remarks>
		public string MessageBoxTrigger { get; set; }

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			Guard.AgainstNull(targetCandidates, nameof(targetCandidates));

			targetCandidates.Add(this);
			return ContainsFocus ? this : null;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			var paneBarContainer = Parent as IPaneBarContainer;
			if (paneBarContainer == null)
			{
				return;
			}
			paneBarContainer.PaneBar.Text = ITextStrings.ksSpecifyConcordanceCriteria;
		}

		public bool PrepareToGoAway()
		{
			return true;
		}

		public string AreaName => AreaServices.TextAndWordsAreaMachineName;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				if (m_recordList != null)
				{
					m_recordList.ConcordanceControl = null;
				}

				// Don't dispose of the record list, since it can monitor relevant PropChanges
				// that affect the NeedToReloadVirtualProperty.
			}
			m_recordList = null;
			m_cache = null;

			base.Dispose(disposing);
		}

		// True after the first time we do it.
		protected internal bool HasLoadedMatches { get; protected set; }
		// True while loading matches, to prevent recursive call.
		protected internal bool IsLoadingMatches { get; protected set; }

		protected internal void LoadMatches()
		{
			LoadMatches(true);
		}

		protected internal void LoadMatches(bool fLoadVirtualProperty)
		{
			var occurrences = SearchForMatches();
			var decorator = (ConcDecorator)((DomainDataByFlidDecoratorBase)m_recordList.VirtualListPublisher).BaseSda;
			// Set this BEFORE we start loading, otherwise, calls to ReloadList triggered here just make it empty.
			HasLoadedMatches = true;
			IsLoadingMatches = true;
			try
			{
				m_recordList.OwningObject = m_cache.LangProject;
				decorator.SetOccurrences(m_cache.LangProject.Hvo, occurrences);
				m_recordList.UpdateList(true);
			}
			finally
			{
				IsLoadingMatches = false;
			}
		}

		protected ConcDecorator ConcDecorator => ((ObjectListPublisher)m_recordList.VirtualListPublisher).BaseSda as ConcDecorator;

		protected virtual List<IParaFragment> SearchForMatches()
		{
			throw new NotSupportedException();
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