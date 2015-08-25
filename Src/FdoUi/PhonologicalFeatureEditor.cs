using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.XWorks.MorphologyEditor;
using SIL.Utils;
using SIL.CoreImpl;
using System.Linq;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// PhonologicalFeatureEditor is the spec/display component of the Bulk Edit bar used to
	/// set the Phonological features of a PhPhoneme.
	///
	/// It is used for BulkEditBar, which is part of XmlViews, but it needs to use the PopupTreeManager class,
	/// and since FdoUi references XmlViews, XmlViews can't reference FdoUi. Also, it
	/// sort of makes sense to put it here as a class that is quite specific to a particular
	/// part of the model.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification="m_cache and m_mediator are references")]
	public class PhonologicalFeatureEditor : IBulkEditSpecControl, IFWDisposable
	{
		private TreeCombo m_tree;
		private FdoCache m_cache;
		private IPublisher m_publisher;
		protected XMLViewsDataCache m_sda;
		private PhonologicalFeaturePopupTreeManager m_PhonologicalFeatureTreeManager;
		private int m_selectedHvo = 0;
		private string m_selectedLabel;
		private int m_displayWs = 0;
		private IFsClosedFeature m_closedFeature;
		private string m_featDefnAbbr;
		public event FwSelectionChangedEventHandler ValueChanged;
		public event EventHandler<TargetFeatureEventArgs> EnableTargetFeatureCombo;

		private PhonologicalFeatureEditor()
		{
			m_PhonologicalFeatureTreeManager = null;
			m_tree = new TreeCombo();
			m_tree.TreeLoad += new EventHandler(m_tree_TreeLoad);
			//	Handle AfterSelect event in m_tree_TreeLoad() through m_pOSPopupTreeManager
		}

		public PhonologicalFeatureEditor(IPublisher publisher, XmlNode configurationNode)
			: this()
		{
			m_publisher = publisher;
			string displayWs = XmlUtils.GetOptionalAttributeValue(configurationNode, "displayWs", "best analorvern");
			m_displayWs = WritingSystemServices.GetMagicWsIdFromName(displayWs);
			string layout = XmlUtils.GetAttributeValue(configurationNode, "layout");
			if (!String.IsNullOrEmpty(layout))
			{
				const string layoutName = "CustomMultiStringForFeatureDefn_";
				int i = layout.IndexOf(layoutName);
				if (i >= 0)
				{
					m_featDefnAbbr = layout.Substring(i + layoutName.Length);
				}
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
		~PhonologicalFeatureEditor()
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
				if (m_PhonologicalFeatureTreeManager != null)
				{
					m_PhonologicalFeatureTreeManager.AfterSelect -= new TreeViewEventHandler(m_PhonFeaturePopupTreeManager_AfterSelect);
					m_PhonologicalFeatureTreeManager.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			SelectedLabel = null;
			m_tree = null;
			m_PhonologicalFeatureTreeManager = null;
			m_cache = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Get/Set the property table.
		/// </summary>
		public IPropertyTable PropertyTable { get; set; }

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
					m_tree.WritingSystemFactory = m_cache.WritingSystemFactory;
			}
		}

		/// <summary>
		/// Semantic Domain Chooser BEdit Control overrides to return its Button
		/// </summary>
		public Button SuggestButton
		{
			get { return null; }
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

		private void m_tree_TreeLoad(object sender, EventArgs e)
		{
			if (m_PhonologicalFeatureTreeManager == null)
			{
				if (!String.IsNullOrEmpty(m_featDefnAbbr))
				{
					// Find the feature definition this editor was created to choose options from
					var featDefns = from s in m_cache.LangProject.PhFeatureSystemOA.FeaturesOC
									where s.Abbreviation.BestAnalysisAlternative.Text == m_featDefnAbbr
									select s;
					if (featDefns.Any())
						m_closedFeature = featDefns.First() as IFsClosedFeature;
				}

				m_PhonologicalFeatureTreeManager = new PhonologicalFeaturePopupTreeManager(m_tree,
																						   m_cache, false, PropertyTable, m_publisher,
																						   PropertyTable.GetValue<Form>("window"),
																						   m_displayWs, m_closedFeature);
				m_PhonologicalFeatureTreeManager.AfterSelect += new TreeViewEventHandler(m_PhonFeaturePopupTreeManager_AfterSelect);
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
				SelectedLabel = "";
			}
			else
			{
				int hvo = ((HvoTreeNode) e.Node).Hvo;
				if (hvo == PhonologicalFeaturePopupTreeManager.kRemoveThisFeature)
				{
					var ptm = sender as PhonologicalFeaturePopupTreeManager;
					if (ptm != null)
					{
						SelectedHvo = ptm.ClosedFeature.Hvo;
						SelectedLabel = FdoUiStrings.ksRemoveThisFeature;
						if (EnableTargetFeatureCombo != null)
							EnableTargetFeatureCombo(this, new TargetFeatureEventArgs(true));
					}
				}
				else
				{
					var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					if (obj is IFsFeatStruc)
					{
						SelectedHvo = hvo;
						SelectedLabel = e.Node.Text;
						if (EnableTargetFeatureCombo != null)
							// since we're using the phonological feature chooser, disable the
							// Target Feature combo (it's no longer relevant)
							EnableTargetFeatureCombo(this, new TargetFeatureEventArgs(false));
					}
					else if (obj is IFsSymFeatVal)
					{
						SelectedHvo = hvo;
						SelectedLabel = e.Node.Text;
						if (EnableTargetFeatureCombo != null)
							EnableTargetFeatureCombo(this, new TargetFeatureEventArgs(true));
					}
					else
					{
						SelectedHvo = 0;
						SelectedLabel = "";
						m_tree.Text = "";
						if (EnableTargetFeatureCombo != null)
							EnableTargetFeatureCombo(this, new TargetFeatureEventArgs(true));
					}
				}
			}

			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			if (ValueChanged != null)
				ValueChanged(this, new FwObjectSelectionEventArgs(SelectedHvo));
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
		public bool SelectedItemIsFsFeatStruc
		{
			get
			{
				return m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetClsid(SelectedHvo)
					   == FsFeatStrucTags.kClassId;
			}
		}
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

			string labelToShow = SelectedLabel;
			var selectedObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(SelectedHvo);

			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count()/50, 1));
			m_cache.DomainDataByFlid.BeginUndoTask(FdoUiStrings.ksUndoBEPhonemeFeatures, FdoUiStrings.ksRedoBEPhonemeFeatures);
			if (SelectedHvo != 0)
			{
				IFsFeatStruc fsTarget = GetTargetFsFeatStruc();
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
						phoneme.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
					if (fsTarget == null && selectedObject is IFsClosedFeature)
					{  // it's the remove option
						var closedValues = from s in phoneme.FeaturesOA.FeatureSpecsOC
										   where s.FeatureRA == selectedObject
										   select s;
						if (closedValues.Any())
							phoneme.FeaturesOA.FeatureSpecsOC.Remove(closedValues.First());
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
				fsTarget = (IFsFeatStruc) obj;
			else if (obj is IFsSymFeatVal)
			{
				IFsSymFeatVal closedValue = (IFsSymFeatVal) obj;
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

		public void ClearPreviousPreviews(IEnumerable<int> itemsToChange, int tagFakeFlid)
		{
			foreach (int hvo in itemsToChange)
			{
				m_sda.RemoveMultiBaseStrings(hvo, tagFakeFlid);
			}
		}

		/// <summary>
		/// Fake doing the change by setting the specified property to the appropriate value
		/// for each item in the set. Disable items that can't be set.
		/// </summary>
		/// <param name="itemsToChange"></param>
		/// <param name="tagFakeFlid"></param>
		/// <param name="tagEnable"></param>
		/// <param name="state"></param>
		public void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnable, ProgressState state)
		{
			CheckDisposed();

			string labelToShow = SelectedLabel;
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
			ITsString tss = TsStringUtils.MakeTss(labelToShow, m_cache.DefaultAnalWs);
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count()/50, 1));
			foreach (int hvo in itemsToChange)
			{
				i++;
				if (i%interval == 0)
				{
					state.PercentDone = i*100/itemsToChange.Count();
					state.Breath();
				}
				bool fEnable = IsItemEligible(m_sda, hvo, selectedObject, labelToShow);
				if (fEnable)
					m_sda.SetString(hvo, tagFakeFlid, tss);
				m_sda.SetInt(hvo, tagEnable, (fEnable ? 1 : 0));
			}
		}

		/// <summary>
		/// Used by SemanticDomainChooserBEditControl to make suggestions and then call FakeDoIt
		/// </summary>
		public void MakeSuggestions(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnabled, ProgressState state)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// This finds the value of the targeted feature in a FsFeatStruc
		/// </summary>
		/// <returns></returns>
		private string GetLabelToShow()
		{
			string labelToShow = "";
			string[] featureValuePairs = FeatureValuePairsInSelectedFeatStruc;
			if (featureValuePairs.Any())
			{
				string matchPattern = m_featDefnAbbr + ":";
				var results = from abbr in featureValuePairs
							  where abbr.StartsWith(matchPattern)
							  select abbr;
				if (results.Any())
				{
					string item = results.First();
					int patternLength = matchPattern.Length;
					labelToShow = item.Substring(patternLength, item.Length - patternLength);
				}
			}
			return labelToShow;
		}

		/// <summary>
		/// Return list of strings of abbreviations
		/// </summary>
		public string[] FeatureValuePairsInSelectedFeatStruc
		{
			get
			{
				string[] featValuePairs = SelectedLabel.Split(' ');
				if (featValuePairs.Any())
				{
					if (featValuePairs[0].StartsWith("["))
						featValuePairs[0] = featValuePairs[0].Substring(1);
					if (featValuePairs.Last().EndsWith("]"))
						featValuePairs[featValuePairs.Length - 1] = featValuePairs.Last().Substring(0, featValuePairs.Last().Length - 1);
				}
				return featValuePairs;
			}

		}
		/// <summary>
		/// Get feature definition abbreviation (column heading)
		/// </summary>
		public string FeatDefnAbbr
		{
			get { return m_featDefnAbbr; }

		}
		public List<int> FieldPath
		{
			get
			{
				return new List<int>(new[]
										{
											PhPhonemeTags.kflidFeatures
										});
			}
		}

		/// <summary>
		///
		/// </summary>
		public int SelectedHvo
		{
			get { return m_selectedHvo; }
			set { m_selectedHvo = value; }
		}

		/// <summary>
		///
		/// </summary>
		public string SelectedLabel
		{
			get { return m_selectedLabel; }
			set { m_selectedLabel = value; }
		}

		private bool IsItemEligible(ISilDataAccess sda, int hvo, ICmObject selectedObject, string labelToShow)
		{
			bool fEnable = false;
			if (string.IsNullOrEmpty(labelToShow))
				return fEnable;
			int hvoFeats = sda.get_ObjectProp(hvo, PhPhonemeTags.kflidFeatures);
			if (hvoFeats == 0)
				return true; // phoneme does not have any features yet, so it is eligible

			var feats = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoFeats);
			int clsid = feats.ClassID;
			if (clsid == FsFeatStrucTags.kClassId)
			{
				// Only show it as a change if it is different
				var features = feats as IFsFeatStruc;
				switch (selectedObject.ClassID)
				{
					case FsSymFeatValTags.kClassId: // user has chosen a value for the targeted feature
						var symFeatVal = selectedObject as IFsSymFeatVal;
						if (symFeatVal != null && features != null)
						{
							var closedValue = from s in features.FeatureSpecsOC
											  where s.ClassID == FsClosedValueTags.kClassId &&
													((IFsClosedValue) s).ValueRA == symFeatVal
											  select s;
							fEnable = !closedValue.Any();
						}
						break;
					case FsFeatStrucTags.kClassId: // user has specified one or more feature/value pairs
						var fs = selectedObject as IFsFeatStruc;
						if (fs != null)
						{
							var closedValue = from s in features.FeatureSpecsOC
											  where s.ClassID == FsClosedValueTags.kClassId &&
													s.FeatureRA.Abbreviation.BestAnalysisAlternative.Text == m_featDefnAbbr &&
													((IFsClosedValue) s).ValueRA.Abbreviation.BestAnalysisAlternative.Text == labelToShow
											  select s;
							fEnable = !closedValue.Any();
						}

						break;
					case FsClosedFeatureTags.kClassId: // user has chosen the remove targeted feature option
						var closedFeature = selectedObject as IFsClosedFeature;
						if (closedFeature != null)
						{
							var closedFeatures = from s in features.FeatureSpecsOC
												 where s.FeatureRA == closedFeature
												 select s;
							fEnable = closedFeatures.Any();
						}

						break;
					default:
						fEnable = hvoFeats != SelectedHvo;
						break;
				}
			}
			return fEnable;
		}
	}

	/// <summary>
	/// Bulk edit bar used for assigning phonological features to phonemes
	/// </summary>
	public class BulkEditBarPhonologicalFeatures : BulkEditBar
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create one
		/// </summary>
		/// <param name="bv">The BrowseViewer that it is part of.</param>
		/// <param name="spec">The parameters element of the BV, containing the
		/// 'columns' elements that specify the BE bar (among other things).</param>
		/// <param name="propertyTable"></param>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public BulkEditBarPhonologicalFeatures(BrowseViewer bv, XmlNode spec, IPropertyTable propertyTable, FdoCache cache) :
			base(bv, spec, propertyTable, cache)
		{
			m_operationsTabControl.Controls.Remove(BulkCopyTab);
			m_operationsTabControl.Controls.Remove(ClickCopyTab);
			m_operationsTabControl.Controls.Remove(FindReplaceTab);
			m_operationsTabControl.Controls.Remove(TransduceTab);
			m_operationsTabControl.Controls.Remove(DeleteTab);
			if (m_listChoiceControl != null)
				m_listChoiceControl.Text = "";
			EnablePreviewApplyForListChoice();
		}

		void BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo(object sender, TargetFeatureEventArgs e)
		{
			TargetCombo.Enabled = e.Enable;
		}

		protected override BulkEditItem MakeItem(XmlNode colSpec)
		{
			BulkEditItem bei = base.MakeItem(colSpec);
			if (bei == null)
				return null;
			var besc = bei.BulkEditControl as PhonologicalFeatureEditor;
			if (besc != null)
				besc.EnableTargetFeatureCombo +=
					new EventHandler<TargetFeatureEventArgs>(BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo);
			return bei;
		}

		protected override void ShowPreviewItems(ProgressState state)
		{
			m_bv.BrowseView.Vc.MultiColumnPreview = false;
			var itemsToChange = ItemsToChange(false);
			BulkEditItem bei = m_beItems[m_itemIndex];
			var phonFeatEditor = bei.BulkEditControl as PhonologicalFeatureEditor;
			if (phonFeatEditor == null)
			{ // User chose to remove the targeted feature
				bei.BulkEditControl.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValue,
											 XMLViewsDataCache.ktagItemEnabled, state);
			}
			else
			{
				if (!phonFeatEditor.SelectedItemIsFsFeatStruc)
				{ // User chose one of the values of the targeted feature
					phonFeatEditor.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValue,
											XMLViewsDataCache.ktagItemEnabled, state);
				}
				else
				{   // User built a FsFeatStruc with the features and values to change.
					// This means we have to find the columns for each feature in the FsFeatStruc and
					// then show the change for that feature in that column.
					int selectedHvo = phonFeatEditor.SelectedHvo;
					string selectedLabel = phonFeatEditor.SelectedLabel;

					string[] featureValuePairs = phonFeatEditor.FeatureValuePairsInSelectedFeatStruc;
					var featureAbbreviations = featureValuePairs.Select(s =>
																			{
																				int i = s.IndexOf(":");
																				return s.Substring(0, i);
																			});
					m_bv.BrowseView.Vc.MultiColumnPreview = true;
					for (int iColumn = 0; iColumn < m_beItems.Count(); iColumn++)
					{
						if (m_beItems[iColumn] == null)
							continue;

						var pfe = m_beItems[iColumn].BulkEditControl as PhonologicalFeatureEditor;
						if (pfe != null)
						{
							pfe.ClearPreviousPreviews(itemsToChange, XMLViewsDataCache.ktagAlternateValueMultiBase + iColumn + 1);
							if (featureAbbreviations.Contains(pfe.FeatDefnAbbr))
							{
								int tempSelectedHvo = pfe.SelectedHvo;
								pfe.SelectedHvo = selectedHvo;
								string tempSelectedLabel = pfe.SelectedLabel;
								pfe.SelectedLabel = selectedLabel;
								pfe.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValueMultiBase + iColumn + 1,
											 XMLViewsDataCache.ktagItemEnabled, state);
								pfe.SelectedHvo = tempSelectedHvo;
								pfe.SelectedLabel = tempSelectedLabel;
							}
						}
					}
				}
			}
		}
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
				foreach (BulkEditItem bei in m_beItems)
				{
					if (bei != null)
					{
						var besc = bei.BulkEditControl as PhonologicalFeatureEditor;
						if (besc != null)
							besc.EnableTargetFeatureCombo -=
								new EventHandler<TargetFeatureEventArgs>(BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo);
					}
				}
			}

			base.Dispose(disposing);
		}

	}
	/// <summary>
	/// Browse viewer used for assigning phonolgical features to phonemes
	/// </summary>
	public class BrowseViewerPhonologicalFeatures : BrowseViewer
	{

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BrowseViewer"/> class.
		/// The sortItemProvider is typically the RecordList that impelements sorting and
		/// filtering of the items we are displaying.
		/// The data access passed typically is a decorator for the one in the cache, adding
		/// the sorted, filtered list of objects accessed as property fakeFlid of hvoRoot.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BrowseViewerPhonologicalFeatures(XmlNode nodeSpec, int hvoRoot, int fakeFlid,
			FdoCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
			: base(nodeSpec, hvoRoot, fakeFlid, cache, sortItemProvider, sda)
		{ }


		///  <summary>
		///
		///  </summary>
		///  <param name="bv"></param>
		///  <param name="spec"></param>
		/// <param name="propertyTable"></param>
		/// <param name="cache"></param>
		///  <returns></returns>
		protected override BulkEditBar CreateBulkEditBar(BrowseViewer bv, XmlNode spec, IPropertyTable propertyTable, FdoCache cache)
		{
			return new BulkEditBarPhonologicalFeatures(bv, spec, propertyTable, cache);
		}

	}
	public class TargetFeatureEventArgs : EventArgs
	{
		public TargetFeatureEventArgs(bool enable)
		{
			Enable = enable;
		}

		public bool Enable { get; private set; }
	}
}
