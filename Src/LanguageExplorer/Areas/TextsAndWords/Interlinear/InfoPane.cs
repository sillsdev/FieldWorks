// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using LanguageExplorer.Works;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Summary description for InfoPane.
	/// </summary>
	public class InfoPane : UserControl, IFlexComponent, IInterlinearTabControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		// Local variables.
		private LcmCache m_cache;
		RecordEditView m_xrev;
		int m_currentRoot = 0;      // Stores the root (IStText) Hvo.

		#region Constructors, destructors, and suchlike methods.

		// This constructor is used by the Windows.Forms Form Designer.
		public InfoPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
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
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		/// <summary>
		/// Initialize the pane with a record clerk. (It already has the cache.)
		/// </summary>
		internal void Initialize(RecordClerk clerk, ToolStripMenuItem printMenu)
		{
			if (m_xrev != null)
			{
				// Already done
				return;
			}
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var dataTree = new InterlinearTextsRecordEditView.StTextDataTree(m_cache);
			m_xrev = new InterlinearTextsRecordEditView(this, new XElement("parameters", new XAttribute("layout", "FullInformation")), m_cache, clerk, dataTree, printMenu);
			m_xrev.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			m_xrev.Dock = DockStyle.Fill;
			Controls.Add(m_xrev);
			DisplayCurrentRoot();
		}

		/// <summary>
		/// Check whether the pane has been initialized.
		/// </summary>
		internal bool IsInitialized => PropertyTable != null;

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				components?.Dispose();
				m_xrev?.Dispose();
			}
			m_cache = null;
			m_xrev = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			base.Dispose( disposing );
		}

		#endregion // Constructors, destructors, and suchlike methods.

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InfoPane));
			this.SuspendLayout();
			//
			// InfoPane
			//
			this.Name = "InfoPane";
			resources.ApplyResources(this, "$this");
			this.Load += new System.EventHandler(this.InfoPane_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void InfoPane_Load(object sender, EventArgs e)
		{

		}

		internal sealed class InterlinearTextsRecordEditView : RecordEditView
		{
			public InterlinearTextsRecordEditView(InfoPane infoPane, XElement configurationParametersElement, LcmCache cache, RecordClerk clerk, DataTree dataTree, ToolStripMenuItem printMenu)
				: base(configurationParametersElement, XDocument.Parse(AreaResources.VisibilityFilter_All), cache, clerk, dataTree, printMenu)
			{
				(m_dataTree as StTextDataTree).InfoPane = infoPane;
			}

			#region Overrides of RecordEditView
			/// <summary>
			/// Initialize a FLEx component with the basic interfaces.
			/// </summary>
			/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
			public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
			{
				base.InitializeFlexComponent(flexComponentParameters);

				ReadParameters();
				SetupDataContext();
				ShowRecord();
			}
			#endregion

			internal sealed class StTextDataTree : DataTree
			{
				private InfoPane m_infoPane;

				internal InfoPane InfoPane
				{
					set { m_infoPane = value; }
				}

				internal StTextDataTree(LcmCache cache)
					: base()
				{
					m_cache = cache;
					InitializeBasic(cache, false);
					InitializeComponent();
				}

				protected override void SetDefaultCurrentSlice(bool suppressFocusChange)
				{
					base.SetDefaultCurrentSlice(suppressFocusChange);
					// currently we always want the focus in the first slice by default,
					// since the user cannot control the governing browse view with a cursor.
					if (!suppressFocusChange && CurrentSlice == null)
						FocusFirstPossibleSlice();
				}

				public override void ShowObject(ICmObject root, string layoutName, string layoutChoiceField, ICmObject descendant, bool suppressFocusChange)
				{
					if (m_infoPane != null && m_infoPane.CurrentRootHvo == 0)
						return;
					//Debug.Assert(m_info.CurrentRootHvo == root.Hvo);
					ICmObject showObj = root;
					ICmObject stText;
					if (root.ClassID == CmBaseAnnotationTags.kClassId)  // RecordClerk is tracking the annotation
					{
						// This pane, as well as knowing how to work with a record list of Texts, knows
						// how to work with one of CmBaseAnnotations, that is, a list of occurrences of
						// a word.
						var cba = (ICmBaseAnnotation)root;
						ICmObject cmoPara = cba.BeginObjectRA;
						stText = cmoPara.Owner;
						showObj = stText;
					}
					else
					{
						stText = root;
					}
					if (stText.OwningFlid == TextTags.kflidContents)
						showObj = stText.Owner;
					base.ShowObject(showObj, layoutName, layoutChoiceField, showObj, suppressFocusChange);
				}

			}
		}

		#region IInterlinearTabControl Members

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public LcmCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		#endregion

		#region IChangeRootObject Members

		public void SetRoot(int hvo)
		{
			m_currentRoot = hvo;
			if (m_xrev != null)
				DisplayCurrentRoot();
		}

		#endregion

		private void DisplayCurrentRoot()
		{
			if (m_currentRoot > 0)
			{
				m_xrev.DatTree.Visible = true;
				ICmObjectRepository repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
				ICmObject root;
				// JohnT: I don't know why this is done at all. Therefore I made a minimal change rather than removing it
				// altogether. If someone knows why it sometimes needs doing, please comment. Or if you know why it once did
				// and it no longer applies, please remove it. I added the test that the Clerk is not aleady looking
				// at this object to suppress switching back to the raw text pane when clicking on the Info pane of an empty text.
				// (FWR-3180)
				if (repo.TryGetObject(m_currentRoot, out root) && root is IStText && m_xrev.Clerk.CurrentObjectHvo != m_currentRoot)
					m_xrev.Clerk.JumpToRecord(m_currentRoot);
			}
			else if (m_currentRoot == 0)
			{
				m_xrev.DatTree.Visible = false;
			}
		}

		internal int CurrentRootHvo => m_currentRoot;
	}
}
