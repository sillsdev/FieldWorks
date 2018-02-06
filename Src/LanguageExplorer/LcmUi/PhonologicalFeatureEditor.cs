// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Xml.Linq;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// PhonologicalFeatureEditor is the spec/display component of the Bulk Edit bar used to
	/// set the Phonological features of a PhPhoneme.
	/// </summary>
	public class PhonologicalFeatureEditor : IBulkEditSpecControl, IDisposable
	{
		private TreeCombo m_tree;
		private LcmCache m_cache;
		private IPublisher m_publisher;
		protected XMLViewsDataCache m_sda;
		private PhonologicalFeaturePopupTreeManager m_PhonologicalFeatureTreeManager;
		private int m_displayWs;
		private IFsClosedFeature m_closedFeature;
		public event FwSelectionChangedEventHandler ValueChanged;
		public event EventHandler<TargetFeatureEventArgs> EnableTargetFeatureCombo;

		private PhonologicalFeatureEditor()
		{
			m_PhonologicalFeatureTreeManager = null;
			m_tree = new TreeCombo();
			m_tree.TreeLoad += m_tree_TreeLoad;
			//	Handle AfterSelect event in m_tree_TreeLoad() through m_pOSPopupTreeManager
		}

		public PhonologicalFeatureEditor(IPublisher publisher, XElement configurationNode)
			: this()
		{
			m_publisher = publisher;
			var displayWs = XmlUtils.GetOptionalAttributeValue(configurationNode, "displayWs", "best analorvern");
			m_displayWs = WritingSystemServices.GetMagicWsIdFromName(displayWs);
			var layout = XmlUtils.GetOptionalAttributeValue(configurationNode, "layout");
			if (string.IsNullOrEmpty(layout))
			{
				return;
			}
			const string layoutName = "CustomMultiStringForFeatureDefn_";
			var i = layout.IndexOf(layoutName);
			if (i >= 0)
			{
				FeatDefnAbbr = layout.Substring(i + layoutName.Length);
			}
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
			{
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~PhonologicalFeatureEditor()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary />
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
			if (IsDisposed)
			{
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
				if (m_PhonologicalFeatureTreeManager != null)
				{
					m_PhonologicalFeatureTreeManager.AfterSelect -= m_PhonFeaturePopupTreeManager_AfterSelect;
					m_PhonologicalFeatureTreeManager.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			SelectedLabel = null;
			m_tree = null;
			m_PhonologicalFeatureTreeManager = null;
			m_cache = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Get/Set the property table.
		/// </summary>
		public IPropertyTable PropertyTable { get; set; }

		/// <summary>
		/// Get or set the cache. Must be set before the tree values need to load.
		/// </summary>
		public LcmCache Cache
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
				{
					m_tree.WritingSystemFactory = m_cache.WritingSystemFactory;
				}
			}
		}

		/// <summary>
		/// Semantic Domain Chooser BEdit Control overrides to return its Button
		/// </summary>
		public Button SuggestButton => null;

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
			if (m_PhonologicalFeatureTreeManager == null)
			{
				if (!string.IsNullOrEmpty(FeatDefnAbbr))
				{
					// Find the feature definition this editor was created to choose options from
					var featDefns = m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.Where(s => s.Abbreviation.BestAnalysisAlternative.Text == FeatDefnAbbr);
					if (featDefns.Any())
					{
						m_closedFeature = featDefns.First() as IFsClosedFeature;
					}
				}

				m_PhonologicalFeatureTreeManager = new PhonologicalFeaturePopupTreeManager(m_tree,
																						   m_cache, false, PropertyTable, m_publisher,
																						   PropertyTable.GetValue<Form>("window"),
																						   m_displayWs, m_closedFeature);
				m_PhonologicalFeatureTreeManager.AfterSelect += m_PhonFeaturePopupTreeManager_AfterSelect;
			}
			m_PhonologicalFeatureTreeManager.LoadPopupTree(0);
		}

		private void m_PhonFeaturePopupTreeManager_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// Arrange to turn all relevant items blue.
			// Remember which item was selected so we can later 'doit'.
			if (e.Node == null)
			{
				SelectedHvo = 0;
				SelectedLabel = string.Empty;
			}
			else
			{
				var hvo = ((HvoTreeNode) e.Node).Hvo;
				if (hvo == PhonologicalFeaturePopupTreeManager.kRemoveThisFeature)
				{
					var ptm = sender as PhonologicalFeaturePopupTreeManager;
					if (ptm != null)
					{
						SelectedHvo = ptm.ClosedFeature.Hvo;
						SelectedLabel = LcmUiStrings.ksRemoveThisFeature;
						EnableTargetFeatureCombo?.Invoke(this, new TargetFeatureEventArgs(true));
					}
				}
				else
				{
					var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					if (obj is IFsFeatStruc)
					{
						SelectedHvo = hvo;
						SelectedLabel = e.Node.Text;
						// since we're using the phonological feature chooser, disable the
						// Target Feature combo (it's no longer relevant)
						EnableTargetFeatureCombo?.Invoke(this, new TargetFeatureEventArgs(false));
					}
					else if (obj is IFsSymFeatVal)
					{
						SelectedHvo = hvo;
						SelectedLabel = e.Node.Text;
						EnableTargetFeatureCombo?.Invoke(this, new TargetFeatureEventArgs(true));
					}
					else
					{
						SelectedHvo = 0;
						SelectedLabel = string.Empty;
						m_tree.Text = string.Empty;
						EnableTargetFeatureCombo?.Invoke(this, new TargetFeatureEventArgs(true));
					}
				}
			}

			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			ValueChanged?.Invoke(this, new FwObjectSelectionEventArgs(SelectedHvo));
		}

		/// <summary>
		/// Required interface member not yet used.
		/// </summary>
		public IVwStylesheet Stylesheet
		{
			set { }
		}

		/// <summary>
		///
		/// </summary>
		public bool SelectedItemIsFsFeatStruc => m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetClsid(SelectedHvo) == FsFeatStrucTags.kClassId;

		/// <summary>
		/// Execute the change requested by the current selection in the combo.
		/// There are two main cases:
		/// 1. The user is removing a feature.
		/// 2. The user is using priority union to include the values of a feature structure.
		/// The latter has two subcases:
		/// a. The user has selected a value for the targeted feature and we have put that value in a FsFeatStruc.
		/// b. The user has employed the chooser to build a FsFeatStruc with the value(s) to change.  These values
		/// may or may not be for the targeted feature.
		/// We do nothing to (phoneme) records where the check box is turned off.
		/// For phonemes with the check box on, we either
		/// 1. remove the specified feature from the phoneme or
		/// 2. use priority union to set the value(s) in the FsFeatStruc.
		/// </summary>
		public void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			CheckDisposed();

			var selectedObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(SelectedHvo);
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChange.Count()/50, 1));
			m_cache.DomainDataByFlid.BeginUndoTask(LcmUiStrings.ksUndoBEPhonemeFeatures, LcmUiStrings.ksRedoBEPhonemeFeatures);
			if (SelectedHvo != 0)
			{
				var fsTarget = GetTargetFsFeatStruc();
				foreach (var hvoPhoneme in itemsToChange)
				{
					i++;
					if (i%interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count() + 20;
						state.Breath();
					}
					var phoneme = m_cache.ServiceLocator.GetInstance<IPhPhonemeRepository>().GetObject(hvoPhoneme);
					if (phoneme.FeaturesOA == null)
					{
						phoneme.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
					}
					if (fsTarget == null && selectedObject is IFsClosedFeature)
					{  // it's the remove option
						var closedValues = phoneme.FeaturesOA.FeatureSpecsOC.Where(s => s.FeatureRA == selectedObject).ToList();
						if (closedValues.Any())
						{
							phoneme.FeaturesOA.FeatureSpecsOC.Remove(closedValues.First());
						}
					}
					else
					{
						phoneme.FeaturesOA.PriorityUnion(fsTarget);
					}
				}
			}
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		private IFsFeatStruc GetTargetFsFeatStruc()
		{
			IFsFeatStruc fsTarget = null;
			var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(SelectedHvo);
			if (obj is IFsFeatStruc)
			{
				fsTarget = (IFsFeatStruc)obj;
			}
			else if (obj is IFsSymFeatVal)
			{
				var closedValue = (IFsSymFeatVal)obj;
				fsTarget = m_PhonologicalFeatureTreeManager.CreateEmptyFeatureStructureInAnnotation(obj);
				var fsClosedValue = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
				fsTarget.FeatureSpecsOC.Add(fsClosedValue);
				fsClosedValue.FeatureRA = (IFsFeatDefn) closedValue.Owner;
				fsClosedValue.ValueRA = closedValue;
			}
			return fsTarget;
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

		public void ClearPreviousPreviews(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier)
		{
			foreach (var hvo in itemsToChange)
			{
				m_sda.RemoveMultiBaseStrings(hvo, tagMadeUpFieldIdentifier);
			}
		}

		/// <summary>
		/// Fake doing the change by setting the specified property to the appropriate value
		/// for each item in the set. Disable items that can't be set.
		/// </summary>
		public void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnable, ProgressState state)
		{
			CheckDisposed();

			var labelToShow = SelectedLabel;
			// selectedHvo refers to either a FsFeatStruc we've made or the targeted feature
			var selectedObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(SelectedHvo);
			if (selectedObject is IFsFeatStruc)
			{
				labelToShow = GetLabelToShow(); // get the value for the targeted feature from the FsFeatStruc
			}
			else if (selectedObject is IFsClosedFeature)
			{
				labelToShow = " "; // it is the remove option so we just show nothing after the arrow
			}
			var tss = TsStringUtils.MakeString(labelToShow, m_cache.DefaultAnalWs);
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChange.Count()/50, 1));
			foreach (var hvo in itemsToChange)
			{
				i++;
				if (i%interval == 0)
				{
					state.PercentDone = i*100/itemsToChange.Count();
					state.Breath();
				}
				var fEnable = IsItemEligible(m_sda, hvo, selectedObject, labelToShow);
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
			throw new NotSupportedException();
		}

		/// <summary>
		/// This finds the value of the targeted feature in a FsFeatStruc
		/// </summary>
		private string GetLabelToShow()
		{
			var labelToShow = string.Empty;
			var featureValuePairs = FeatureValuePairsInSelectedFeatStruc;
			if (!featureValuePairs.Any())
			{
				return labelToShow;
			}
			var matchPattern = FeatDefnAbbr + ":";
			var results = featureValuePairs.Where(abbr => abbr.StartsWith(matchPattern)).ToList();
			if (!results.Any())
			{
				return labelToShow;
			}
			var item = results.First();
			var patternLength = matchPattern.Length;
			labelToShow = item.Substring(patternLength, item.Length - patternLength);
			return labelToShow;
		}

		/// <summary>
		/// Return list of strings of abbreviations
		/// </summary>
		public string[] FeatureValuePairsInSelectedFeatStruc
		{
			get
			{
				var featValuePairs = SelectedLabel.Split(' ');
				if (featValuePairs.Any())
				{
					if (featValuePairs[0].StartsWith("["))
					{
						featValuePairs[0] = featValuePairs[0].Substring(1);
					}

					if (featValuePairs.Last().EndsWith("]"))
					{
						featValuePairs[featValuePairs.Length - 1] = featValuePairs.Last().Substring(0, featValuePairs.Last().Length - 1);
					}
				}
				return featValuePairs;
			}

		}
		/// <summary>
		/// Get feature definition abbreviation (column heading)
		/// </summary>
		public string FeatDefnAbbr { get; }

		public List<int> FieldPath => new List<int>(new[] { PhPhonemeTags.kflidFeatures });

		/// <summary />
		public int SelectedHvo { get; set; }

		/// <summary />
		public string SelectedLabel { get; set; }

		private bool IsItemEligible(ISilDataAccess sda, int hvo, ICmObject selectedObject, string labelToShow)
		{
			if (string.IsNullOrEmpty(labelToShow))
			{
				return false;
			}
			var hvoFeats = sda.get_ObjectProp(hvo, PhPhonemeTags.kflidFeatures);
			if (hvoFeats == 0)
			{
				return true; // phoneme does not have any features yet, so it is eligible
			}

			var feats = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoFeats);
			var clsid = feats.ClassID;
			if (clsid != FsFeatStrucTags.kClassId)
			{
				return false;
			}
			var fEnable = false;
			// Only show it as a change if it is different
			var features = feats as IFsFeatStruc;
			switch (selectedObject.ClassID)
			{
				case FsSymFeatValTags.kClassId: // user has chosen a value for the targeted feature
					var symFeatVal = selectedObject as IFsSymFeatVal;
					if (symFeatVal != null && features != null)
					{
						var closedValue = features.FeatureSpecsOC.Where(s => s.ClassID == FsClosedValueTags.kClassId && ((IFsClosedValue) s).ValueRA == symFeatVal);
						fEnable = !closedValue.Any();
					}
					break;
				case FsFeatStrucTags.kClassId: // user has specified one or more feature/value pairs
					var fs = selectedObject as IFsFeatStruc;
					if (fs != null)
					{
						var closedValue = features.FeatureSpecsOC.Where(s =>
							s.ClassID == FsClosedValueTags.kClassId &&
							s.FeatureRA.Abbreviation.BestAnalysisAlternative.Text == FeatDefnAbbr &&
							((IFsClosedValue) s).ValueRA.Abbreviation.BestAnalysisAlternative.Text == labelToShow);
						fEnable = !closedValue.Any();
					}

					break;
				case FsClosedFeatureTags.kClassId: // user has chosen the remove targeted feature option
					var closedFeature = selectedObject as IFsClosedFeature;
					if (closedFeature != null)
					{
						var closedFeatures = features.FeatureSpecsOC.Where(s => s.FeatureRA == closedFeature);
						fEnable = closedFeatures.Any();
					}

					break;
				default:
					fEnable = hvoFeats != SelectedHvo;
					break;
			}
			return fEnable;
		}
	}
}