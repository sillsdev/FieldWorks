using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public partial class FindExampleSentenceDlg : Form, IFwGuiControl
	{
		FdoCache m_cache = null;
		Mediator m_mediator = null;
		XmlNode m_configurationNode = null;
		LexExampleSentence m_les = null;
		ILexSense m_owningSense = null;
		int m_virtFlidReference = -1;
		SenseInTwficsOccurrenceBrowseView m_rbv = null;
		XmlView m_previewPane = null;
		string m_helpTopic = "khtpFindExampleSentence";
		RecordClerk m_clerk;

		public FindExampleSentenceDlg()
		{
			InitializeComponent();
			if (FwApp.App != null)
				this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			this.helpProvider.SetShowHelp(this, true);
			if (FwApp.App != null)
			{
				helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(m_helpTopic, 0));
				btnHelp.Enabled = true;
			}

		}

		#region IFWDisposable Members

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion

		#region IFwGuiControl Members

		public void Init(Mediator mediator, XmlNode configurationNode, ICmObject sourceObject)
		{
			CheckDisposed();

			m_cache = sourceObject.Cache;

			// Find the sense owning our LexExampleSentence
			if (sourceObject is LexExampleSentence)
			{
				m_les = sourceObject as LexExampleSentence;
				m_owningSense = LexSense.CreateFromDBObject(m_cache, m_les.OwnerHVO);
			}
			else if (sourceObject is LexSense)
			{
				m_owningSense = sourceObject as ILexSense;
			}
			else
			{
				throw new ArgumentException("Invalid object type for sourceObject.");
			}

			m_mediator = mediator;
			m_configurationNode = configurationNode;
			AddConfigurableControls();

			m_virtFlidReference = BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "CmBaseAnnotation", "Reference");
		}

		public void Launch()
		{
			CheckDisposed();
			ShowDialog((Form)m_mediator.PropertyTable.GetValue("window"));
		}

		#endregion

		XmlNode BrowseViewControlParameters
		{
			get
			{
				return m_configurationNode.SelectSingleNode(
					String.Format("control/parameters[@id='{0}']", "senseInTwficsOccurrenceList"));
			}
		}

		private void AddConfigurableControls()
		{
			// Load the controls.

			// 1. Initialize the preview pane (lower pane)
			m_previewPane = new XmlView(0, "publicationNew", null, false);
			m_previewPane.Cache = m_cache;
			m_previewPane.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);

			BasicPaneBarContainer pbc = new BasicPaneBarContainer();
			pbc.Init(m_mediator, m_previewPane);
			pbc.Dock = DockStyle.Fill;
			pbc.PaneBar.Text = LexEdStrings.ksFindExampleSentenceDlgPreviewPaneTitle;
			panel2.Controls.Add(pbc);
			if (m_previewPane.RootBox == null)
				m_previewPane.MakeRoot();

			// 2. load the browse view. (upper pane)
			XmlNode xnBrowseViewControlParameters = this.BrowseViewControlParameters;

			// First create our Clerk, since we can't set it's OwningObject via the configuration/mediator/PropertyTable info.
			m_clerk = RecordClerkFactory.CreateClerk(m_mediator, xnBrowseViewControlParameters);
			m_clerk.OwningObject = m_owningSense;

			m_rbv = DynamicLoader.CreateObject(xnBrowseViewControlParameters.ParentNode.SelectSingleNode("dynamicloaderinfo")) as SenseInTwficsOccurrenceBrowseView;
			m_rbv.Init(m_mediator, xnBrowseViewControlParameters, m_previewPane);
			m_rbv.CheckBoxChanged += new CheckBoxChangedEventHandler(m_rbv_CheckBoxChanged);
			// add it to our controls.
			BasicPaneBarContainer pbc1 = new BasicPaneBarContainer();
			pbc1.Init(m_mediator, m_rbv);
			pbc1.BorderStyle = BorderStyle.FixedSingle;
			pbc1.Dock = DockStyle.Fill;
			pbc1.PaneBar.Text = LexEdStrings.ksFindExampleSentenceDlgBrowseViewPaneTitle;
			panel1.Controls.Add(pbc1);

			CheckAddBtnEnabling();
		}

		void m_rbv_CheckBoxChanged(object sender, CheckBoxChangedEventArgs e)
		{
			CheckAddBtnEnabling();
		}

		private void CheckAddBtnEnabling()
		{
			btnAdd.Enabled = m_rbv.CheckedItems.Count > 0;
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			// Get the checked twfics;
			List<int> twfics = m_rbv.CheckedItems;
			if (twfics == null || twfics.Count == 0)
			{
				// do nothing.
				return;
			}
			List<int> twficSegments = StTxtPara.TwficSegments(m_cache, twfics);
			Set<int> uniqueSegments = new Set<int>(twficSegments);
			FdoObjectSet<CmBaseAnnotation> cbaSegments = new FdoObjectSet<CmBaseAnnotation>(m_cache, uniqueSegments.ToArray(), true);
			int insertIndex = m_owningSense.ExamplesOS.Count; // by default, insert at the end.
			int sourceIndex = -1;
			if (m_les != null)
			{
				// we were given a LexExampleSentence, so set our insertion index after the given one.
				List<int> examples = new List<int>(m_owningSense.ExamplesOS.HvoArray);
				sourceIndex = examples.IndexOf(m_les.Hvo);
				insertIndex = sourceIndex + 1;
			}

			// Load all the annotations for these twfics.
			int tagSegFF = StTxtPara.SegmentFreeformAnnotationsFlid(m_cache);
			Set<int> allAnalWsIds = new Set<int>(m_cache.LangProject.AnalysisWssRC.HvoArray);
			StTxtPara.LoadSegmentFreeformAnnotationData(m_cache, uniqueSegments, allAnalWsIds);

			bool fBoolModifiedExisting = false;
			List<ILexExampleSentence> newExamples = new List<ILexExampleSentence>(); // keep track of how many new objects we created.
			ILexExampleSentence newLexExample = null;
			// delay prop changes until all the new examples are added.
			using (new IgnorePropChanged(m_cache, PropChangedHandling.SuppressView))
			{
				foreach (CmBaseAnnotation cbaSegment in cbaSegments)
				{
					if (newExamples.Count == 0 && m_les != null && m_les.Example.BestVernacularAlternative.Text == "***" &&
						(m_les.TranslationsOC == null || m_les.TranslationsOC.Count == 0) &&
						m_les.Reference.Length == 0)
					{
						// we were given an empty LexExampleSentence, so use this one for our first new Example.
						newLexExample = m_les;
						fBoolModifiedExisting = true;
					}
					else
					{
						// create a new example sentence.
						newLexExample = m_owningSense.ExamplesOS.InsertAt(new LexExampleSentence(), insertIndex + newExamples.Count);
						newExamples.Add(newLexExample);
					}
					// copy the segment string into the new LexExampleSentence
					// Enhance: bold the relevant twfic(s).
					newLexExample.Example.SetVernacularDefaultWritingSystem(StTxtPara.TssSubstring(cbaSegment).Text);

					int segDefn_literalTranslation = m_cache.GetIdFromGuid(LangProject.kguidAnnLiteralTranslation);
					int segDefn_freeTranslation = m_cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);

					int hvoTransType_literalTranslation = m_cache.GetIdFromGuid(LangProject.kguidTranLiteralTranslation);
					int hvoTransType_freeTranslation = m_cache.GetIdFromGuid(LangProject.kguidTranFreeTranslation);

					// copy the translation information
					List<ICmTranslation> newTranslations = new List<ICmTranslation>();
					foreach (int freeFormAnnotationId in m_cache.GetVectorProperty(cbaSegment.Hvo, tagSegFF, true))
					{
						int hvoAnnType = m_cache.MainCacheAccessor.get_ObjectProp(freeFormAnnotationId, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);

						// map annotation type to translation type.
						int hvoTranslationType = 0;
						if (hvoAnnType == segDefn_literalTranslation)
						{
							hvoTranslationType = hvoTransType_literalTranslation;
						}
						else if (hvoAnnType == segDefn_freeTranslation)
						{
							hvoTranslationType = hvoTransType_freeTranslation;
						}
						else
						{
							continue; // skip unsupported translation type.
						}

						ICmTranslation newTranslation = newLexExample.TranslationsOC.Add(new CmTranslation());
						newTranslations.Add(newTranslation);
						newTranslation.TypeRAHvo = hvoTranslationType;
						foreach (int analWs in allAnalWsIds)
						{
							ITsString tssComment = m_cache.GetMultiStringAlt(freeFormAnnotationId, (int)CmAnnotation.CmAnnotationTags.kflidComment, analWs);
							if (tssComment.Length > 0)
							{
								newTranslation.Translation.SetAlternative(tssComment, analWs);
							}
						}
					}

					// copy the reference.
					// Enhance: get the ws from the 'Reference' column spec?
					// Enhance: AnnotationRefHandler can also m_cache the reference directly to the segment.
					int iTwfic = twficSegments.IndexOf(cbaSegment.Hvo);
					newLexExample.Reference.UnderlyingTsString = m_cache.GetTsStringProperty(twfics[iTwfic], m_virtFlidReference);
					// Need to correctly add Translations (or ghost if none).
				}
			}
			if (fBoolModifiedExisting)
			{
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_les.Hvo,
					(int)LexExampleSentence.LexExampleSentenceTags.kflidExample, 0, 1, 1);
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_les.Hvo,
					(int)LexExampleSentence.LexExampleSentenceTags.kflidReference, 0, 1, 1);
				//m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_les.Hvo,
				//	(int)LexExampleSentence.LexExampleSentenceTags.kflidTranslations, 0, 1, 1);
				//m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_owningSense.Hvo,
				//	(int)LexSense.LexSenseTags.kflidExamples, sourceIndex, 1, 1);
			}
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_owningSense.Hvo,
				(int)LexSense.LexSenseTags.kflidExamples, insertIndex, newExamples.Count, 0);
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, m_helpTopic);
		}
	}

	internal class SenseInTwficsOccurrenceBrowseView : RecordBrowseView
	{
		int m_hvoSelectedTwfic = 0;
		XmlView m_previewPane = null;

		internal void Init(Mediator mediator, XmlNode xnBrowseViewControlParameters, XmlView pubView)
		{
			m_previewPane = pubView;
			base.Init(mediator, xnBrowseViewControlParameters);
		}

		public override void OnSelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			PreviewCurrentSelection(e.Hvo);
			base.OnSelectionChanged(sender, e);
		}

		protected override void ShowRecord()
		{
			if (!m_fullyInitialized || m_suppressShowRecord)
				return;
			if (Clerk == null || Clerk.CurrentObject == null)
				return;
			PreviewCurrentSelection(Clerk.CurrentObject.Hvo);
			base.ShowRecord();
		}

		private void PreviewCurrentSelection(int hvoTwficAnn)
		{
			if (m_hvoSelectedTwfic == hvoTwficAnn)
				return;
			m_hvoSelectedTwfic = hvoTwficAnn;
			// Load selected twfic info.
			List<int> twficSegments = StTxtPara.TwficSegments(Cache, new List<int>(new int[] { hvoTwficAnn }));
			if (twficSegments.Count > 0)
			{
				// Load all the annotations for this twfic.
				Set<int> allAnalWsIds = new Set<int>(Cache.LangProject.AnalysisWssRC.HvoArray);
				StTxtPara.LoadSegmentFreeformAnnotationData(Cache, new Set<int>(twficSegments), allAnalWsIds);

				m_previewPane.RootObjectHvo = twficSegments[0];
			}
		}

	}
}