using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.IText
{
	public class ConcordanceControlBase : UserControl, IxCoreContentControl, IFWDisposable
	{
		protected Mediator m_mediator;
		protected XmlNode m_configurationParameters;
		protected FdoCache m_cache;
		protected OccurrencesOfSelectedUnit m_clerk;
		protected IHelpTopicProvider m_helpTopicProvider;

		public virtual string AccName
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			CheckDisposed();
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates");
			targetCandidates.Add(this);
			return ContainsFocus ? this : null;
		}

		public virtual void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();
			m_mediator = mediator;
			m_helpTopicProvider = m_mediator.HelpTopicProvider;
			m_configurationParameters = configurationParameters;
			m_cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			string name = XmlUtils.GetAttributeValue(configurationParameters, "clerk");
			m_clerk = (OccurrencesOfSelectedUnit) m_mediator.PropertyTable.GetValue(name) ?? (OccurrencesOfSelectedUnit) RecordClerkFactory.CreateClerk(m_mediator, m_configurationParameters, true);
			m_clerk.ConcordanceControl = this;
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();
			return new IxCoreColleague[] { this };
		}

		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			((PaneBarContainer) Parent).PaneBar.Text = ITextStrings.ksSpecifyConcordanceCriteria;
		}

		public int Priority
		{
			get { return (int) ColleaguePriority.Medium; }
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
	}
}
