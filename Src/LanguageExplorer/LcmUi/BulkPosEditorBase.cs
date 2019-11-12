// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// BulkPosEditor is the spec/display component of the Bulk Edit bar used to
	/// set the PartOfSpeech of group of LexSenses (actually by creating or modifying an
	/// MoStemMsa that is the MorphoSyntaxAnalysis of the sense).
	/// </summary>
	public abstract class BulkPosEditorBase : IBulkEditSpecControl, IFlexComponent, ITextChangedNotification, IDisposable
	{
		#region Data members & event declarations

		protected TreeCombo m_tree;
		protected LcmCache m_cache;
		protected XMLViewsDataCache m_sda;
		protected POSPopupTreeManager m_pOSPopupTreeManager;
		protected int m_selectedHvo;
		protected string m_selectedLabel;
		public event EventHandler ControlActivated;
		public event FwSelectionChangedEventHandler ValueChanged;

		#endregion Data members & event declarations

		#region Construction

		protected BulkPosEditorBase()
		{
			m_pOSPopupTreeManager = null;
			m_tree = new TreeCombo();
			m_tree.TreeLoad += m_tree_TreeLoad;
			//	Handle AfterSelect event in m_tree_TreeLoad() through m_pOSPopupTreeManager
		}

		/// <summary>
		/// Inform the editor that the text of the tree changed (without changing the selected index...
		/// that needs to be updated).
		/// </summary>
		public void ControlTextChanged()
		{
			if (m_pOSPopupTreeManager == null)
			{
				var oldText = m_tree.Text;
				m_tree_TreeLoad(this, new EventArgs());
				m_tree.Text = oldText; // Load clears it.
			}
			var nodes = m_tree.Nodes.Find(m_tree.Text, true);
			if (nodes.Length == 0)
			{
				m_tree.Text = string.Empty;
			}
			else
			{
				m_tree.SelectedNode = nodes[0];
				// AfterSelect doesn't do this because the selection is not 'by mouse' so it is defeated by
				// the code that prevents AfterSelect handling up and down arrows which change the selection
				// without confirming it.
				SelectNode(nodes[0]);
			}
		}

		#endregion Construction

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~BulkPosEditorBase()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_tree != null)
				{
					m_tree.Load -= m_tree_TreeLoad;
					m_tree.Dispose();
				}
				if (m_pOSPopupTreeManager != null)
				{
					m_pOSPopupTreeManager.AfterSelect -= m_pOSPopupTreeManager_AfterSelect;
					m_pOSPopupTreeManager.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_selectedLabel = null;
			m_tree = null;
			m_pOSPopupTreeManager = null;
			m_cache = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Properties

		/// <summary>
		/// Get or set the cache. Must be set before the tree values need to load.
		/// </summary>
		public LcmCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{
				m_cache = value;
				// The following fixes LT-6298: when a control needs a writing system factory,
				// it needs a *VALID* writing system factory!
				if (m_cache != null && m_tree != null)
				{
					m_tree.WritingSystemFactory = m_cache.WritingSystemFactory;
					m_tree.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;   // should it be DefaultUserWs?
				}
			}
		}

		/// <summary>
		/// The special cache that can handle the preview and check-box properties.
		/// </summary>
		public XMLViewsDataCache DataAccess
		{
			get
			{
				if (m_sda == null)
				{
					throw new InvalidOperationException("Must set the special cache of a BulkEditSpecControl");
				}
				return m_sda;
			}
			set { m_sda = value; }
		}

		/// <summary>
		/// Get the actual tree control.
		/// </summary>
		public Control Control => m_tree;

		protected abstract ICmPossibilityList List
		{
			get;
		}

		#endregion Properties

		#region Event handlers

		private void m_tree_TreeLoad(object sender, EventArgs e)
		{
			if (m_pOSPopupTreeManager == null)
			{
				m_pOSPopupTreeManager = new POSPopupTreeManager(m_tree,
					m_cache,
					List,
					m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle,
					false,
					new FlexComponentParameters(PropertyTable, Publisher, Subscriber),
					PropertyTable.GetValue<Form>(FwUtils.window));
				m_pOSPopupTreeManager.AfterSelect += m_pOSPopupTreeManager_AfterSelect;
			}
			m_pOSPopupTreeManager.LoadPopupTree(0);
		}

		private void m_pOSPopupTreeManager_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// Todo: user selected a part of speech.
			// Arrange to turn all relevant items blue.
			SelectNode(e.Node);
			ControlActivated?.Invoke(this, new EventArgs());
			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			if (ValueChanged == null)
			{
				return;
			}
			// the user may have selected "<Not Sure>", which has a 0 hvo value
			// but that's a valid thing to allow the user to Apply/Preview.
			// So, we also pass in an index to resolve ambiguity.
			var index = m_tree.SelectedNode.Index;
			ValueChanged(sender, new FwObjectSelectionEventArgs(m_selectedHvo, index));
		}

		// Do the core data-affecting tasks associated with selecting a node.
		private void SelectNode(TreeNode node)
		{
			// Remember which item was selected so we can later 'doit'.
			if (node == null)
			{
				m_selectedHvo = 0;
				m_selectedLabel = string.Empty;
			}
			else
			{
				m_selectedHvo = (node as HvoTreeNode).Hvo;
				m_selectedLabel = node.Text;
			}
		}

		#endregion Event handlers

		#region IBulkEditSpecControl implementation

		/// <summary>
		/// Returns the Suggest button if our target is Semantic Domains, otherwise null.
		/// </summary>
		public Button SuggestButton => null;

		public abstract void DoIt(IEnumerable<int> itemsToChange, ProgressState state);

		public void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnable, ProgressState state)
		{
			var tss = TsStringUtils.MakeString(m_selectedLabel, m_cache.DefaultAnalWs);
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach (var hvo in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				var fEnable = CanFakeIt(hvo);
				if (fEnable)
				{
					m_sda.SetString(hvo, tagMadeUpFieldIdentifier, tss);
				}
				m_sda.SetInt(hvo, tagEnable, (fEnable ? 1 : 0));
			}
		}

		/// <summary>
		/// Used by SemanticDomainChooserBEditControl to make suggestions and then call FakeDoIt
		/// </summary>
		public void MakeSuggestions(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			throw new NotSupportedException("The method or operation is not supported.");
		}

		/// <summary>
		/// Required interface member currently ignored.
		/// </summary>
		public IVwStylesheet Stylesheet
		{
			set { }
		}

		/// <summary>
		/// Subclasses may override if they can clear the field value.
		/// </summary>
		public virtual bool CanClearField => false;

		/// <summary>
		/// Subclasses should override if they override CanClearField to return true.
		/// </summary>
		public virtual void SetClearField()
		{
			throw new NotSupportedException();
		}

		public virtual List<int> FieldPath => null;

		#endregion IBulkEditSpecControl implementation

		#region Other methods

		protected abstract bool CanFakeIt(int hvo);

		#endregion Other methods

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; set; }

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
		}

		#endregion
	}
}