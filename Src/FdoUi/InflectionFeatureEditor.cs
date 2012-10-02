using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// InflectionFeatureEditor is the spec/display component of the Bulk Edit bar used to
	/// set the Inflection features of a LexSense (which must already have an MoStemMsa with
	/// POS set).
	///
	/// It is used for BulkEditBar, which is part of XmlViews, but it needs to use the PopupTreeManager class,
	/// and since FdoUi references XmlViews, XmlViews can't reference FdoUi. Also, it
	/// sort of makes sense to put it here as a class that is quite specific to a particular
	/// part of the model.
	/// </summary>
	public class InflectionFeatureEditor : IBulkEditSpecControl, IFWDisposable
	{
		Mediator m_mediator;
		TreeCombo m_tree;
		FdoCache m_cache;
		InflectionFeaturePopupTreeManager m_InflectionFeatureTreeManager;
		int m_selectedHvo = 0;
		string m_selectedLabel;
		private int m_displayWs = 0;
		public event EventHandler ControlActivated;
		public event FwSelectionChangedEventHandler ValueChanged;

		public InflectionFeatureEditor()
		{
			m_InflectionFeatureTreeManager = null;
			m_tree = new TreeCombo();
			m_tree.TreeLoad += new EventHandler(m_tree_TreeLoad);
			//	Handle AfterSelect event in m_tree_TreeLoad() through m_pOSPopupTreeManager
		}

		public InflectionFeatureEditor(XmlNode configurationNode)
			: this()
		{
			string displayWs = XmlUtils.GetOptionalAttributeValue(configurationNode, "displayWs", "best analorvern");
			m_displayWs = LangProject.GetMagicWsIdFromName(displayWs);
		}

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
		~InflectionFeatureEditor()
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
			Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
				if (m_InflectionFeatureTreeManager != null)
				{
					m_InflectionFeatureTreeManager.AfterSelect -= new TreeViewEventHandler(m_pOSPopupTreeManager_AfterSelect);
					m_InflectionFeatureTreeManager.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_selectedLabel = null;
			m_tree = null;
			m_InflectionFeatureTreeManager = null;
			m_mediator = null;
			m_cache = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

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
				if (m_cache != null && m_tree != null)
					m_tree.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			}
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

		private void m_tree_TreeLoad(object sender, EventArgs e)
		{
			if (m_InflectionFeatureTreeManager == null)
			{
				m_InflectionFeatureTreeManager = new InflectionFeaturePopupTreeManager(m_tree,
																					   m_cache, false, m_mediator,
																					   (Form)m_mediator.PropertyTable.GetValue("window"),
																					   m_displayWs);
				m_InflectionFeatureTreeManager.AfterSelect += new TreeViewEventHandler(m_pOSPopupTreeManager_AfterSelect);
			}
			m_InflectionFeatureTreeManager.LoadPopupTree(0);
		}

		private void m_pOSPopupTreeManager_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			// Todo: user selected a part of speech.
			// Arrange to turn all relevant items blue.
			// Remember which item was selected so we can later 'doit'.
			if (e.Node == null)
			{
				m_selectedHvo = 0;
				m_selectedLabel = "";
			}
			else
			{
				int hvo = (e.Node as HvoTreeNode).Hvo;
				int clid = m_cache.GetClassOfObject(hvo);
				if (m_cache.ClassIsOrInheritsFrom((uint)clid, (uint)FsFeatStruc.kclsidFsFeatStruc))
				{
					m_selectedHvo = hvo;
					m_selectedLabel = e.Node.Text;
				}
				else
				{
					m_selectedHvo = 0;
					m_selectedLabel = "";
					m_tree.Text = "";
				}
			}
			if (ControlActivated != null)
				ControlActivated(this, new EventArgs());

			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			if (ValueChanged != null)
				ValueChanged(this, new FwObjectSelectionEventArgs(m_selectedHvo));
		}

		/// <summary>
		/// Required interface member not yet used.
		/// </summary>
		public IVwStylesheet Stylesheet
		{
			set {  }
		}

		/// <summary>
		/// Execute the change requested by the current selection in the combo.
		/// Basically we want a copy of the FsFeatStruc indicated by m_selectedHvo, (even if 0?? not yet possible),
		/// to become the MsFeatures of each record that is appropriate to change.
		/// We do nothing to records where the check box is turned off,
		/// and nothing to ones that currently have an MSA other than an MoStemMsa,
		/// and nothing to ones that currently have an MSA with the wrong POS.
		/// (a) If the owning entry has an MoStemMsa with a matching MsFeatures (and presumably POS),
		/// set the sense to use it.
		/// (b) If all senses using the current MoStemMsa are to be changed, just update
		/// the MsFeatures of that MoStemMsa.
		/// We could add this...but very probably unused MSAs would have been taken over
		/// when setting the POS.
		/// --(c) If the entry has an MoStemMsa which is not being used at all, change it to
		/// --the required POS and inflection class and use it.
		/// (d) Make a new MoStemMsa in the LexEntry with the required POS and features
		/// and point the sense at it.
		/// </summary>
		public void DoIt(Set<int> itemsToChange, ProgressState state)
		{
			CheckDisposed();

			int hvoPos = GetPOS();
			// Make a Set of eligible parts of speech to use in filtering.
			Set<int> possiblePOS = GetPossiblePartsOfSpeech();
			// Make a Dictionary from HVO of entry to list of modified senses.
			Dictionary<int, Set<ILexSense>> sensesByEntry = new Dictionary<int, Set<ILexSense>>();
			int tagOwningEntry = m_cache.VwCacheDaAccessor.GetVirtualHandlerName("LexSense", "OwningEntry").Tag;
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count / 50, 1));
			foreach(int hvoSense in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 20 / itemsToChange.Count;
					state.Breath();
				}
				if (!IsItemEligible(m_cache.MainCacheAccessor, hvoSense, possiblePOS))
					continue;
				ILexSense ls = (ILexSense)CmObject.CreateFromDBObject(m_cache, hvoSense, false);
				IMoMorphSynAnalysis msa = ls.MorphoSyntaxAnalysisRA;
				int hvoEntry = m_cache.MainCacheAccessor.get_ObjectProp(ls.Hvo, tagOwningEntry);
				if (!sensesByEntry.ContainsKey(hvoEntry))
					sensesByEntry[hvoEntry] = new Set<ILexSense>();
				sensesByEntry[hvoEntry].Add(ls);
			}
			//REVIEW: Should these really be the same Undo/Redo strings as for InflectionClassEditor.cs?
			m_cache.BeginUndoTask(FdoUiStrings.ksUndoBEInflClass, FdoUiStrings.ksRedoBEInflClass);
			BulkEditBar.ForceRefreshOnUndoRedo(Cache.MainCacheAccessor);
			i = 0;
			interval = Math.Min(100, Math.Max(sensesByEntry.Count / 50, 1));
			IFsFeatStruc fsTarget = null;
			if (m_selectedHvo != 0)
				fsTarget = FsFeatStruc.CreateFromDBObject(Cache, m_selectedHvo);
			foreach (KeyValuePair<int, Set<ILexSense>> kvp in sensesByEntry)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 80 / sensesByEntry.Count + 20;
					state.Breath();
				}
				ILexEntry entry = (ILexEntry)CmObject.CreateFromDBObject(m_cache, kvp.Key, false);
				Set<ILexSense> sensesToChange = kvp.Value;
				IMoStemMsa msmTarget = null;
				foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
				{
					IMoStemMsa msm = msa as IMoStemMsa;
					if (msm != null && MsaMatchesTarget(msm, fsTarget))
					{
						// Can reuse this one!
						msmTarget = msm;
						break;
					}
				}
				if (msmTarget == null)
				{
					// See if we can reuse an existing MoStemMsa by changing it.
					// This is possible if it is used only by senses in the list, or not used at all.
					Set<ILexSense> otherSenses = new Set<ILexSense>();
					Set<ILexSense> senses = new Set<ILexSense>(entry.AllSenses.ToArray());
					if (senses.Count != sensesToChange.Count)
					{
						foreach (ILexSense ls in senses)
						{
							if (!sensesToChange.Contains(ls))
								otherSenses.Add(ls);
						}
					}
					foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
					{
						IMoStemMsa msm = msa as IMoStemMsa;
						if (msm == null)
							continue;
						bool fOk = true;
						foreach (ILexSense ls in otherSenses)
						{
							if (ls.MorphoSyntaxAnalysisRA == msm)
							{
								fOk = false;
								break;
							}
						}
						if (fOk)
						{
							// Can reuse this one! Nothing we don't want to change uses it.
							// Adjust its POS as well as its inflection feature, just to be sure.
							// Ensure that we don't change the POS!  See LT-6835.
							msmTarget = msm;
							InitMsa(msmTarget, msm.PartOfSpeechRAHvo);
							break;
						}
					}
				}
				if (msmTarget == null)
				{
					// Nothing we can reuse...make a new one.
					msmTarget = new MoStemMsa();
					entry.MorphoSyntaxAnalysesOC.Add(msmTarget);
					InitMsa(msmTarget, hvoPos);
				}
				// Finally! Make the senses we want to change use it.
				foreach (ILexSense ls in sensesToChange)
				{
					ls.MorphoSyntaxAnalysisRA = msmTarget;
				}
			}
			m_cache.EndUndoTask();
		}

		/// <summary>
		/// Can't (yet) clear the field value.
		/// </summary>
		public bool CanClearField
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// <summary>
		/// Not needed since we said we can't do it.
		/// </summary>
		public void SetClearField()
		{
			CheckDisposed();

			throw new NotImplementedException();
		}

		private void InitMsa(IMoStemMsa msmTarget, int hvoPos)
		{
			msmTarget.PartOfSpeechRAHvo = hvoPos;
			msmTarget.MsFeaturesOA = null; // Delete the old one.
			Cache.CopyObject(m_selectedHvo, msmTarget.Hvo, (int)MoStemMsa.MoStemMsaTags.kflidMsFeatures);
		}

		/// <summary>
		/// Answer true if the selected MSA has an MsFeatures that is the same as the argument.
		/// </summary>
		/// <returns></returns>
		private bool MsaMatchesTarget(IMoStemMsa msm, IFsFeatStruc fsTarget)
		{
			if (m_selectedHvo == 0 && msm.MsFeaturesOAHvo == 0)
				return true;
			if (msm.MsFeaturesOAHvo == 0)
				return false;
			return msm.MsFeaturesOA.IsEquivalent(fsTarget);
		}

		/// <summary>
		/// Add to possiblePOS all the children (recursively) of hvoPos
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvoPos"></param>
		/// <param name="possiblePOS"></param>
		void AddChildPos(ISilDataAccess sda, int hvoPos, Set<int> possiblePOS)
		{
			possiblePOS.Add(hvoPos);
			int chvo = sda.get_VecSize(hvoPos, (int)CmPossibility.CmPossibilityTags.kflidSubPossibilities);
			for (int i = 0; i < chvo; i++)
				AddChildPos(sda, sda.get_VecItem(hvoPos,
					(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities, i), possiblePOS);
		}

		/// <summary>
		/// Fake doing the change by setting the specified property to the appropriate value
		/// for each item in the set. Disable items that can't be set.
		/// </summary>
		/// <param name="itemsToChange"></param>
		/// <param name="ktagFakeFlid"></param>
		public void FakeDoit(Set<int> itemsToChange, int tagFakeFlid, int tagEnable, ProgressState state)
		{
			CheckDisposed();

			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			ITsString tss = m_cache.MakeAnalysisTss(m_selectedLabel);
			// Build a Set of parts of speech that can take this class.
			Set<int> possiblePOS = GetPossiblePartsOfSpeech();
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
				bool fEnable = IsItemEligible(sda, hvo, possiblePOS);
				if (fEnable)
					cda.CacheStringProp(hvo, tagFakeFlid, tss);
				cda.CacheIntProp(hvo, tagEnable, (fEnable ? 1 : 0));
			}
		}

		public List<int> FieldPath
		{
			get
			{
				return new List<int>(new int[]{(int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis,
					(int)MoStemMsa.MoStemMsaTags.kflidPartOfSpeech,
					(int)MoStemMsa.MoStemMsaTags.kflidMsFeatures});
			}
		}

		private bool IsItemEligible(ISilDataAccess sda, int hvo, Set<int> possiblePOS)
		{
			bool fEnable = false;
			int hvoMsa = sda.get_ObjectProp(hvo, (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis);
			if (hvoMsa != 0)
			{
				int clsid = m_cache.GetClassOfObject(hvoMsa);
				if (clsid == MoStemMsa.kClassId)
				{
					int pos = sda.get_ObjectProp(hvoMsa, (int)MoStemMsa.MoStemMsaTags.kflidPartOfSpeech);
					if (pos != 0 && possiblePOS.Contains(pos))
					{
						// Only show it as a change if it is different
						int hvoFeature = sda.get_ObjectProp(hvoMsa, (int)MoStemMsa.MoStemMsaTags.kflidMsFeatures);
						fEnable = hvoFeature != m_selectedHvo;
					}
				}
			}
			return fEnable;
		}

		private int GetPOS()
		{
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			if (m_selectedHvo != 0)
			{
				int hvoRootPos = m_cache.GetOwnerOfObject(m_selectedHvo);
				while (hvoRootPos != 0 && m_cache.GetClassOfObject(hvoRootPos) != PartOfSpeech.kClassId)
					hvoRootPos = m_cache.GetOwnerOfObject(hvoRootPos);
				return hvoRootPos;
			}
			return 0;
		}

		private Set<int> GetPossiblePartsOfSpeech()
		{
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			Set<int> possiblePOS = new Set<int>();
			if (m_selectedHvo != 0)
			{
				int hvoRootPos = m_cache.GetOwnerOfObject(m_selectedHvo);
				while (hvoRootPos != 0 && m_cache.GetClassOfObject(hvoRootPos) != PartOfSpeech.kClassId)
					hvoRootPos = m_cache.GetOwnerOfObject(hvoRootPos);
				if (hvoRootPos != 0)
				{
					AddChildPos(sda, hvoRootPos, possiblePOS);
				}
			}
			return possiblePOS;
		}
	}
}
