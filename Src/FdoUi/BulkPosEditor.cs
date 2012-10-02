using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using XCore;
using SIL.FieldWorks.Filters;
using SIL.Utils;
using System.Xml;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// BulkPosEditor is the spec/display component of the Bulk Edit bar used to
	/// set the PartOfSpeech of group of LexSenses (actually by creating or modifying an
	/// MoStemMsa that is the MorphoSyntaxAnalysis of the sense).
	///
	/// It was originally part of XmlViews, but it needs to use the POSPopupTreeManager class,
	/// and since FdoUi references XmlViews, XmlViews can't reference FdoUi. Also, it
	/// sort of makes sense to put it here as a class that is quite specific to a particular
	/// part of the model.
	/// </summary>
	public abstract class BulkPosEditorBase : IBulkEditSpecControl, IFWDisposable, ITextChangedNotification
	{
		#region Data members & event declarations

		protected Mediator m_mediator;
		protected TreeCombo m_tree;
		protected FdoCache m_cache;
		protected XMLViewsDataCache m_sda;
		protected POSPopupTreeManager m_pOSPopupTreeManager;
		protected int m_selectedHvo = 0;
		protected string m_selectedLabel;

		public event EventHandler ControlActivated;
		public event FwSelectionChangedEventHandler ValueChanged;

		#endregion Data members & event declarations

		#region Construction

		public BulkPosEditorBase()
		{
			m_pOSPopupTreeManager = null;
			m_tree = new TreeCombo();
			m_tree.TreeLoad += new EventHandler(m_tree_TreeLoad);
			//	Handle AfterSelect event in m_tree_TreeLoad() through m_pOSPopupTreeManager
		}

		/// <summary>
		/// Inform the editor that the text of the tree changed (without changing the selected index...
		/// that needs to be updated).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void ControlTextChanged()
		{
			if (m_pOSPopupTreeManager == null)
			{
				string oldText = m_tree.Text;
				m_tree_TreeLoad(this, new EventArgs());
				m_tree.Text = oldText; // Load clears it.
			}
			TreeNode[] nodes = m_tree.Nodes.Find(m_tree.Text, true);
			if (nodes.Length == 0)
				m_tree.Text = "";
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
		// Region last reviewed: never

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
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

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

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_tree != null)
				{
					m_tree.Load -= new EventHandler(m_tree_TreeLoad);
					m_tree.Dispose();
				}
				if (m_pOSPopupTreeManager != null)
				{
					m_pOSPopupTreeManager.AfterSelect -= new TreeViewEventHandler(m_pOSPopupTreeManager_AfterSelect);
					m_pOSPopupTreeManager.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_selectedLabel = null;
			m_tree = null;
			m_pOSPopupTreeManager = null;
			m_mediator = null;
			m_cache = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Properties

		/// <summary>
		/// Get or set the mediator.
		/// </summary>
		public Mediator Mediator
		{
			get
			{
				CheckDisposed();
				return m_mediator;
			}
			set
			{
				CheckDisposed();
				m_mediator = value;
			}
		}

		/// <summary>
		/// Get or set the cache. Must be set before the tree values need to load.
		/// </summary>
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
			set
			{
				CheckDisposed();
				m_cache = value;
				// The following fixes LT-6298: when a control needs a writing system factory,
				// it needs a *VALID* writing system factory!
				if (m_cache != null && m_tree != null)
				{
					m_tree.WritingSystemFactory = m_cache.WritingSystemFactory;
					m_tree.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;	// should it be DefaultUserWs?
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
					throw new InvalidOperationException("Must set the special cache of a BulkEditSpecControl");
				return m_sda;
			}
			set { m_sda = value; }
		}

		/// <summary>
		/// Get the actual tree control.
		/// </summary>
		public Control Control
		{
			get
			{
				CheckDisposed();
				return m_tree;
			}
		}

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
					m_mediator,
					(Form)m_mediator.PropertyTable.GetValue("window"));
				m_pOSPopupTreeManager.AfterSelect += m_pOSPopupTreeManager_AfterSelect;
			}
			m_pOSPopupTreeManager.LoadPopupTree(0);
		}

		private void m_pOSPopupTreeManager_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// Todo: user selected a part of speech.
			// Arrange to turn all relevant items blue.
			SelectNode(e.Node);
			if (ControlActivated != null)
				ControlActivated(this, new EventArgs());

			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			if (ValueChanged != null)
			{
				// the user may have selected "<Not Sure>", which has a 0 hvo value
				// but that's a valid thing to allow the user to Apply/Preview.
				// So, we also pass in an index to resolve ambiguity.
				int index = m_tree.SelectedNode.Index;
				ValueChanged(sender, new FwObjectSelectionEventArgs(m_selectedHvo, index));
			}
		}

		// Do the core data-affecting tasks associated with selecting a node.
		private void SelectNode(TreeNode node)
		{
			// Remember which item was selected so we can later 'doit'.
			if (node == null)
			{
				m_selectedHvo = 0;
				m_selectedLabel = "";
			}
			else
			{
				m_selectedHvo = (node as HvoTreeNode).Hvo;
				m_selectedLabel = node.Text;
			}
		}

		#endregion Event handlers

		#region IBulkEditSpecControl implementation

		public abstract void DoIt(Set<int> itemsToChange, ProgressState state);

		public void FakeDoit(Set<int> itemsToChange, int tagFakeFlid, int tagEnable, ProgressState state)
		{
			CheckDisposed();

			ITsString tss = StringUtils.MakeTss(m_selectedLabel, m_cache.DefaultAnalWs);
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count / 50, 1));
			foreach (int hvo in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count;
					state.Breath();
				}
				bool fEnable = CanFakeIt(hvo);
				if (fEnable)
					m_sda.SetString(hvo, tagFakeFlid, tss);
				m_sda.SetInt(hvo, tagEnable, (fEnable ? 1 : 0));
			}
		}

		/// <summary>
		/// Required interface member currently ignored.
		/// </summary>
		public IVwStylesheet Stylesheet
		{
			set {}
		}

		/// <summary>
		/// Subclasses may override if they can clear the field value.
		/// </summary>
		public virtual bool CanClearField
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// <summary>
		/// Subclasses should override if they override CanClearField to return true.
		/// </summary>
		public virtual void SetClearField()
		{
			CheckDisposed();

			throw new NotImplementedException();
		}

		public virtual List<int> FieldPath
		{
			get { return null;  }
		}

		#endregion IBulkEditSpecControl implementation

		#region Other methods

		protected abstract bool CanFakeIt(int hvo);

		#endregion Other methods
	}
	/// <summary>
	/// BulkPosEditor is the spec/display component of the Bulk Edit bar used to
	/// set the PartOfSpeech of group of LexSenses (actually by creating or modifying an
	/// MoStemMsa that is the MorphoSyntaxAnalysis of the sense).
	///
	/// It was originally part of XmlViews, but it needs to use the POSPopupTreeManager class,
	/// and since FdoUi references XmlViews, XmlViews can't reference FdoUi. Also, it
	/// sort of makes sense to put it here as a class that is quite specific to a particular
	/// part of the model.
	/// </summary>
	public class BulkPosEditor : BulkPosEditorBase
	{
		public BulkPosEditor()
		{
		}

		protected override ICmPossibilityList List
		{
			get {return m_cache.LanguageProject.PartsOfSpeechOA; }
		}

		public override void DoIt(Set<int> itemsToChange, ProgressState state)
		{
			CheckDisposed();
			var senseRepo = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			// FWR-2781 should be able to bulk edit entries to POS <Not sure>.
			IPartOfSpeech posWanted = null;
			if (m_selectedHvo > 0)
				posWanted = (IPartOfSpeech) m_cache.ServiceLocator.GetObject(m_selectedHvo);

			// Make a hashtable from entry to list of modified senses.
			var sensesByEntry = new Dictionary<ILexEntry, List<ILexSense>>();
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count / 50, 1));
			foreach (int hvoSense in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 20 / itemsToChange.Count;
					state.Breath();
				}
				var sense = senseRepo.GetObject(hvoSense);
				var msa = sense.MorphoSyntaxAnalysisRA;
				if (msa != null && msa.ClassID != MoStemMsaTags.kClassId)
					continue; // can't fix this one, not a stem.
				var entry = sense.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
				List<ILexSense> senses;
				if (!sensesByEntry.TryGetValue(entry, out senses))
				{
					senses = new List<ILexSense>();
					sensesByEntry[entry] = senses;
				}
				senses.Add(sense);
			}
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoBulkEditPOS, FdoUiStrings.ksRedoBulkEditPOS, m_cache.ActionHandlerAccessor,
				()=>DoUpdatePos(state, sensesByEntry, posWanted));
		}

		private void DoUpdatePos(ProgressState state, Dictionary<ILexEntry, List<ILexSense>> sensesByEntry, IPartOfSpeech posWanted)
		{
			int i;
			int interval;
			i = 0;
			interval = Math.Min(100, Math.Max(sensesByEntry.Count / 50, 1));
			foreach (var kvp in sensesByEntry)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 80 / sensesByEntry.Count + 20;
					state.Breath();
				}
				var entry = kvp.Key;
				var sensesToChange = kvp.Value;
				// Try to find an existing MSA with the right POS.
				var msmTarget = (from msa in entry.MorphoSyntaxAnalysesOC
								 where msa.ClassID == (uint) MoStemMsaTags.kClassId
									   && ((IMoStemMsa)msa).PartOfSpeechRA == posWanted
								 select (IMoStemMsa) msa).FirstOrDefault();
				if (msmTarget == null)
				{
					// No existing MSA has the desired POS.
					// See if we can reuse an existing MoStemMsa by changing it.
					// This is possible if it is used only by senses in the list, or not used at all.
					var otherSenses = new List<ILexSense>();
					AddExcludedSenses(entry, otherSenses, sensesToChange); // Get all the unchanged senses of the entry.
					foreach (var msa in entry.MorphoSyntaxAnalysesOC)
					{
						if (msa.ClassID != MoStemMsaTags.kClassId)
							continue;
						bool fOk = true;
						foreach (var otherSense in otherSenses)
						{
							if (otherSense.MorphoSyntaxAnalysisRA == msa)
							{
								fOk = false; // we can't change it, one of the unchanged senses uses it
								break;
							}
						}
						if (fOk)
						{
							// Can reuse this one! Nothing we don't want to change uses it. Go ahead and set it to the
							// required POS.
							msmTarget = (IMoStemMsa)msa;
							var oldPOS = msmTarget.PartOfSpeechRA;
							msmTarget.PartOfSpeechRA = posWanted;

							// compare MoStemMsa.ResetInflectionClass: changing POS requires us to clear inflection class,
							// if it is set.
							if (oldPOS != null && msmTarget.InflectionClassRA != null)
								msmTarget.InflectionClassRA = null;
							break;
						}
					}
				}
				if (msmTarget == null)
				{
					// Nothing we can reuse...make a new one.
					msmTarget = m_cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
					entry.MorphoSyntaxAnalysesOC.Add(msmTarget);
					msmTarget.PartOfSpeechRA = posWanted;
				}
				// Finally! Make the senses we want to change use it.
				foreach (var sense in sensesToChange)
				{
					if (sense.MorphoSyntaxAnalysisRA == msmTarget)
						continue; // reusing a modified msa.
					sense.MorphoSyntaxAnalysisRA = msmTarget;
				}
			}
		}

		/// <summary>
		/// We can set POS to null.
		/// </summary>
		public override bool CanClearField
		{
			get
			{
				CheckDisposed();

				return true;
			}
		}

		/// <summary>
		/// We can set POS to null.
		/// </summary>
		public override void SetClearField()
		{
			CheckDisposed();

			m_selectedHvo = 0;
			m_selectedLabel = "";
			// Do NOT call base method (it throws not implemented)
		}

		public override List<int> FieldPath
		{
			get { return new List<int>(new[] { LexSenseTags.kflidMorphoSyntaxAnalysis }); }
		}

		/// <summary>
		/// Add to excludedSenses any sense of the entry (directly or indirectly owned)
		/// which is not a member of includedSenses.
		/// </summary>
		void AddExcludedSenses(ILexEntry entry, List<ILexSense> excludedSenses, List<ILexSense> includedSenses)
		{
			foreach (var sense in entry.SensesOS)
			{
				if (!includedSenses.Contains(sense))
					excludedSenses.Add(sense);
				AddExcludedSenses(sense, excludedSenses, includedSenses);
			}
		}

		/// <summary>
		/// Add to excludedSenses any sense of the entry (directly or indirectly owned)
		/// which is not a member of includedSenses.
		/// </summary>
		void AddExcludedSenses(ILexSense owningSense, List<ILexSense> excludedSenses, List<ILexSense> includedSenses)
		{
			foreach (var sense in owningSense.SensesOS)
			{
				if (!includedSenses.Contains(sense))
					excludedSenses.Add(sense);
				AddExcludedSenses(sense, excludedSenses, includedSenses);
			}
		}

		protected override bool CanFakeIt(int hvo)
		{
			bool canFakeit = true;
			int hvoMsa = m_cache.DomainDataByFlid.get_ObjectProp(hvo, LexSenseTags.kflidMorphoSyntaxAnalysis);
			if (hvoMsa != 0)
			{
				int clsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoMsa).ClassID;
				canFakeit = (clsid == MoStemMsaTags.kClassId);
			}
			return canFakeit;
		}

		/// <summary>
		/// Get a type we can use to create a compatible filter.
		/// </summary>
		public static Type FilterType()
		{
			return typeof(PosFilter);
		}
	}

	/// <summary>
	/// A special filter, where items are LexSenses, and matches are ones where an MSA is an MoStemMsa that
	/// has the correct POS.
	/// </summary>
	class PosFilter : ColumnSpecFilter
	{
		/// <summary>
		/// Default constructor for persistence.
		/// </summary>
		public PosFilter() { }
		public PosFilter(FdoCache cache, ListMatchOptions mode, int[] targets, XmlNode colSpec)
			: base(cache, mode, targets, colSpec)
		{
		}

		protected override string BeSpec
		{
			get { return "external"; }
		}

		public override bool CompatibleFilter(XmlNode colSpec)
		{
			if (!base.CompatibleFilter(colSpec))
				return false;
			var typeForLoaderNode = DynamicLoader.TypeForLoaderNode(colSpec);
			// Naturally we are compatible with ourself, and BulkPosEditor has a FilterType which causes
			// a filter of this type to be created, too.
			return typeForLoaderNode == typeof(BulkPosEditor) || typeForLoaderNode == typeof(PosFilter);
		}

		/// <summary>
		/// Return the HVO of the list from which choices can be made.
		/// </summary>
		static public int List(FdoCache cache)
		{
			return cache.LanguageProject.PartsOfSpeechOA.Hvo;
		}

		/// <summary>
		/// This is a filter for an atomic property, and the "all" and "only" options should not be presented.
		/// </summary>
		public static bool Atomic
		{
			get { return true; }
		}
	}
	/// <summary>
	/// A special filter, where items are LexEntries, and matches are ones where an MSA is an MoStemMsa that
	/// has the correct POS. (not used yet.
	/// </summary>
	class EntryPosFilter : ListChoiceFilter
	{
		/// <summary>
		/// Default constructor for persistence.
		/// </summary>
		public EntryPosFilter() { }
		public EntryPosFilter(FdoCache cache, ListMatchOptions mode, int[] targets)
			: base(cache, mode, targets)
		{
		}

		protected override string BeSpec
		{
			get { return "external"; }
		}

		public override bool CompatibleFilter(System.Xml.XmlNode colSpec)
		{
			if (!base.CompatibleFilter(colSpec))
				return false;
			return DynamicLoader.TypeForLoaderNode(colSpec) == this.GetType();
		}

		const int kflidMsas = LexEntryTags.kflidMorphoSyntaxAnalyses;
		const int kflidEntrySenses = LexEntryTags.kflidSenses;

		/// <summary>
		/// Get the items to be compared against the filter.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		protected override int[] GetItems(IManyOnePathSortItem item)
		{
			ISilDataAccess sda = m_cache.DomainDataByFlid;
			List<int> results = new List<int>();
			if (item.PathLength > 0 && item.PathFlid(0) == kflidMsas)
			{
				// sorted by MSA, match just the one MSA.
				// I don't think this path can occur with the current XML spec where this is used.
				int hvoMsa;
				if (item.PathLength > 1)
					hvoMsa = item.PathObject(1);
				else
					hvoMsa = item.KeyObject;
				GetItemsForMsaType(sda, ref results, hvoMsa);
			}
			else if (item.PathLength >= 1 && item.PathFlid(0) == kflidEntrySenses)
			{
				// sorted in a way that shows one sense per row, test that sense's MSA.
				int hvoSense;
				if (item.PathLength > 1)
					hvoSense = item.PathObject(1);
				else
					hvoSense = item.KeyObject;
				int hvoMsa = sda.get_ObjectProp(hvoSense, LexSenseTags.kflidMorphoSyntaxAnalysis);
				GetItemsForMsaType(sda, ref results, hvoMsa);
			}
			else
			{
				int hvoEntry = item.RootObjectHvo;
				int cmsa = sda.get_VecSize(hvoEntry, kflidMsas);
				for (int imsa = 0; imsa < cmsa; imsa++)
				{
					int hvoMsa = sda.get_VecItem(hvoEntry, kflidMsas, imsa);
					GetItemsForMsaType(sda, ref results, hvoMsa);
				}
			}
			return results.ToArray();
		}

		private void GetItemsForMsaType(ISilDataAccess sda, ref List<int> results, int hvoMsa)
		{
			if (hvoMsa == 0)
				return;
			int kclsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoMsa).ClassID;
			switch (kclsid)
			{
				case MoStemMsaTags.kClassId:
					AddHvoPOStoResults(sda, results, hvoMsa, MoStemMsaTags.kflidPartOfSpeech);
					break;
				case MoInflAffMsaTags.kClassId:
					AddHvoPOStoResults(sda, results, hvoMsa, MoInflAffMsaTags.kflidPartOfSpeech);
					break;
				case MoDerivAffMsaTags.kClassId:
					AddHvoPOStoResults(sda, results, hvoMsa, MoDerivAffMsaTags.kflidFromPartOfSpeech);
					AddHvoPOStoResults(sda, results, hvoMsa, MoDerivAffMsaTags.kflidToPartOfSpeech);
					break;
				case MoUnclassifiedAffixMsaTags.kClassId:
					AddHvoPOStoResults(sda, results, hvoMsa, MoUnclassifiedAffixMsaTags.kflidPartOfSpeech);
					break;
			}
		}

		private static void AddHvoPOStoResults(ISilDataAccess sda, List<int> results, int hvoMsa, int flidPos)
		{
			int hvoPOS;
			hvoPOS = sda.get_ObjectProp(hvoMsa, flidPos);
			if (hvoPOS != 0)
				results.Add(hvoPOS);
		}

		/// <summary>
		/// Return the HVO of the list from which choices can be made.
		/// </summary>
		static public int List(FdoCache cache)
		{
			return cache.LanguageProject.PartsOfSpeechOA.Hvo;
		}
	}
}
