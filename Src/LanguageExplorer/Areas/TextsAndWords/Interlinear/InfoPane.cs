// Copyright (c) 20105-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

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
		private Container components = null;

		// Local variables.
		RecordEditView m_xrev;

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

		#endregion

		#region Implementation of IFlexComponent

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
		/// Initialize the pane with a record list. (It already has the cache.)
		/// </summary>
		internal void Initialize(ISharedEventHandlers sharedEventHandlers, IRecordList recordList, ToolStripMenuItem printMenu)
		{
			if (m_xrev != null)
			{
				// Already done
				return;
			}
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var dataTree = new StTextDataTree(sharedEventHandlers, Cache);
			m_xrev = new InterlinearTextsRecordEditView(this, new XElement("parameters", new XAttribute("layout", "FullInformation")), Cache, recordList, dataTree, printMenu);
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
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
				components?.Dispose();
				m_xrev?.Dispose();
			}
			Cache = null;
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
			ComponentResourceManager resources = new ComponentResourceManager(typeof(InfoPane));
			this.SuspendLayout();
			//
			// InfoPane
			//
			this.Name = "InfoPane";
			resources.ApplyResources(this, "$this");
			this.Load += new EventHandler(this.InfoPane_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void InfoPane_Load(object sender, EventArgs e)
		{

		}

		#region IInterlinearTabControl Members

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public LcmCache Cache { get; set; }

		#endregion

		#region IChangeRootObject Members

		public void SetRoot(int hvo)
		{
			CurrentRootHvo = hvo;
			if (m_xrev != null)
			{
				DisplayCurrentRoot();
			}
		}

		#endregion

		private void DisplayCurrentRoot()
		{
			if (CurrentRootHvo > 0)
			{
				m_xrev.MyDataTree.Visible = true;
				var repo = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
				ICmObject root;
				// JohnT: I don't know why this is done at all. Therefore I made a minimal change rather than removing it
				// altogether. If someone knows why it sometimes needs doing, please comment. Or if you know why it once did
				// and it no longer applies, please remove it. I added the test that the record list is not aleady looking
				// at this object to suppress switching back to the raw text pane when clicking on the Info pane of an empty text.
				// (FWR-3180)
				if (repo.TryGetObject(CurrentRootHvo, out root) && root is IStText && m_xrev.MyRecordList.CurrentObjectHvo != CurrentRootHvo)
				{
					m_xrev.MyRecordList.JumpToRecord(CurrentRootHvo);
				}
			}
			else if (CurrentRootHvo == 0)
			{
				m_xrev.MyDataTree.Visible = false;
			}
		}

		internal int CurrentRootHvo { get; private set; }
	}
}