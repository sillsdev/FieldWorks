// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.Framework.DetailControls;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Summary description for InfoPane.
	/// </summary>
	public class InfoPane : UserControl, IInterlinearTabControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		// Local variables.
		private LcmCache m_cache;
		Mediator m_mediator;
		private PropertyTable m_propertyTable;
		RecordEditView m_xrev;
		int m_currentRoot = 0;		// Stores the root (IStText) Hvo.

		#region Constructors, destructors, and suchlike methods.

		// This constructor is used by the Windows.Forms Form Designer.
		public InfoPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		public InfoPane(LcmCache cache, Mediator mediator, PropertyTable propertyTable, RecordClerk clerk)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			Initialize(cache, mediator, propertyTable, clerk);
		}

		/// <summary>
		/// Initialize the pane with a Mediator and a RecordClerk.
		/// </summary>
		internal void Initialize(LcmCache cache, Mediator mediator, PropertyTable propertyTable, RecordClerk clerk)
		{
			m_cache = cache;
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			InitializeInfoView(clerk);
		}

		private void InitializeInfoView(RecordClerk clerk)
		{
			if (m_mediator == null)
				return;
			if (m_propertyTable == null)
				return;
			var xnWindow = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			if (xnWindow == null)
				return;
			XmlNode xnControl = xnWindow.SelectSingleNode(
				"controls/parameters/guicontrol[@id=\"TextInformationPane\"]/control/parameters");
			if (xnControl == null)
				return;
			var activeClerk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk");
			var toolChoice = m_propertyTable.GetStringProperty("currentContentControl", null);
			if(m_xrev != null)
			{
				//when re-using the infoview we want to remove and dispose of the old recordeditview and
				//associated datatree. (LT-13216)
				Controls.Remove(m_xrev);
				m_xrev.Dispose();
			}
			m_xrev = new InterlinearTextsRecordEditView(this);
			if (clerk.GetType().Name == "InterlinearTextsRecordClerk")
			{
				m_xrev.Clerk = clerk;
			}
			else
			{
				//We want to make sure that the following initialization line will initialize this
				//clerk if we haven't already set it. Without this assignment to null, the InfoPane
				//misbehaves in the Concordance view (it uses the filter from the InterlinearTexts view)
				m_xrev.Clerk = null;
			}
			m_xrev.Init(m_mediator, m_propertyTable, xnControl); // <-- This call will change the ActiveClerk
			DisplayCurrentRoot();
			m_xrev.Dock = DockStyle.Fill;
			Controls.Add(m_xrev);
			// There are times when moving to the InfoPane causes the wrong ActiveClerk to be set.
			// See FWR-3390 (and InterlinearTextsRecordClerk.OnDisplayInsertInterlinText).
			var activeClerkNew = m_propertyTable.GetValue<RecordClerk>("ActiveClerk");
			if (toolChoice != "interlinearEdit" && activeClerk != null && activeClerk != activeClerkNew)
			{
				m_propertyTable.SetProperty("ActiveClerk", activeClerk, true);
				activeClerk.ActivateUI(true);
			}
		}

		/// <summary>
		/// Check whether the pane has been initialized.
		/// </summary>
		internal bool IsInitialized
		{
			get { return m_mediator != null; }
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
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

			if (disposing)
			{
				components?.Dispose();
			}

			base.Dispose(disposing);
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
			SuspendLayout();
			//
			// InfoPane
			//
			Name = "InfoPane";
			resources.ApplyResources(this, "$this");
			Load += new EventHandler(InfoPane_Load);
			ResumeLayout(false);

		}
		#endregion

		private void InfoPane_Load(object sender, EventArgs e)
		{

		}

		internal class InterlinearTextsRecordEditView : RecordEditView
		{
			public InterlinearTextsRecordEditView(InfoPane info)
				: base(new StTextDataTree())
			{
				(m_dataEntryForm as StTextDataTree).InfoPane = info;
			}

			private class StTextDataTree : DataTree
			{
				private InfoPane m_info;

				internal InfoPane InfoPane
				{
					set { m_info = value; }
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
					if (m_info != null && m_info.CurrentRootHvo == 0)
						return;
					//Debug.Assert(m_info.CurrentRootHvo == root.Hvo);

					bool isRecordClerkTracking = root.ClassID == CmBaseAnnotationTags.kClassId; // RecordClerk is tracking the annotation

					// This pane, as well as knowing how to work with a record list of Texts, knows
					// how to work with one of CmBaseAnnotations, that is, a list of occurrences of
					// a word.
					ICmObject stText = isRecordClerkTracking ?
						((ICmBaseAnnotation)root).BeginObjectRA.Owner :
						root;
					ICmObject showObj = isRecordClerkTracking ?
						stText :
						root;

					if (stText.OwningFlid == TextTags.kflidContents)
						showObj = stText.Owner;
					base.ShowObject(showObj, layoutName, layoutChoiceField, showObj, suppressFocusChange);
				}

			}
		}

		#region IInterlinearTabControl Members

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

		internal int CurrentRootHvo
		{
			get { return m_currentRoot; }
		}
	}
}
