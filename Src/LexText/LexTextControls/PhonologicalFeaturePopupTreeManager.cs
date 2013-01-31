using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Handles a TreeCombo control (Widgets assembly) for use in selecting inflection features.
	/// </summary>
	public class PhonologicalFeaturePopupTreeManager : PopupTreeManager
	{
		private const int kEmpty = 0;
		private const int kLine = -1;
		/// <summary>
		/// Used to indicate that a feature needs to be removed from the list of feature/value pairs in a phoneme
		/// </summary>
		public const int kRemoveThisFeature = -2;
		private const int kChoosePhonologicaFeatures = -3;
		private List<ICmBaseAnnotation> m_annotations = new List<ICmBaseAnnotation>();
		private IFsClosedFeature m_closedFeature;
		/// <summary>
		/// Constructor.
		/// </summary>
		public PhonologicalFeaturePopupTreeManager(TreeCombo treeCombo, FdoCache cache,  bool useAbbr, Mediator mediator, Form parent, int wsDisplay, IFsClosedFeature closedFeature)
			: base(treeCombo, cache, mediator, cache.LanguageProject.PartsOfSpeechOA, wsDisplay, useAbbr, parent)
		{
			m_closedFeature = closedFeature;
		}

		/// <summary>
		/// The target feature (the one that the user selects in the "Target Field" dropdown combo box)
		/// </summary>
		public IFsClosedFeature ClosedFeature
		{
			get { return m_closedFeature; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		/// <remarks>These annotations and their feature structure objects are private to this control.
		/// They are deleted when this control is disposed.
		/// </remarks>
		public IFsFeatStruc CreateEmptyFeatureStructureInAnnotation(ICmObject obj)
		{
			var cba = Cache.ServiceLocator.GetInstance<ICmBaseAnnotationFactory>().Create();
			Cache.LanguageProject.AnnotationsOC.Add(cba);
			cba.BeginObjectRA = obj;
			var fs = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			cba.FeaturesOA = fs;
			m_annotations.Add(cba);
			return fs;
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{

			TreeNode match = null;

			// We need a way to store feature structures the user has chosen during this session.
			// We use an annotation to do this.
			foreach (ICmBaseAnnotation cba in m_annotations)
			{
				IFsFeatStruc fs = cba.FeaturesOA;
				if (fs == null || fs.IsEmpty)
					continue;
				if (cba.BeginObjectRA != null)
					continue;  // is not one of the feature structures created via the phon feat chooser
				HvoTreeNode node = new HvoTreeNode(fs.LongNameTSS, fs.Hvo);
				popupTree.Nodes.Add(node);
				if (fs.Hvo == hvoTarget)
					match = node;
			}

			if (ClosedFeature != null)
			{
				foreach (var closedValue in ClosedFeature.ValuesOC)
				{
					HvoTreeNode node = new HvoTreeNode(closedValue.Abbreviation.BestAnalysisAlternative, closedValue.Hvo);
					popupTree.Nodes.Add(node);
					if (closedValue.Hvo == hvoTarget)
						match = node;
				}
			}

			popupTree.Nodes.Add(new HvoTreeNode(
					Cache.TsStrFactory.MakeString(LexTextControls.ksRemoveThisFeature, Cache.WritingSystemFactory.UserWs),
					kRemoveThisFeature));
			popupTree.Nodes.Add(new HvoTreeNode(
					Cache.TsStrFactory.MakeString(LexTextControls.ksChoosePhonFeats, Cache.WritingSystemFactory.UserWs),
					kChoosePhonologicaFeatures));

			return match;

		}

		protected override void m_treeCombo_AfterSelect(object sender, TreeViewEventArgs e)
		{
			HvoTreeNode selectedNode = e.Node as HvoTreeNode;
			PopupTree pt = GetPopupTree();

			switch (selectedNode.Hvo)
			{
				case kChoosePhonologicaFeatures:
					// Only launch the dialog by a mouse click (or simulated mouse click).
					if (e.Action != TreeViewAction.ByMouse)
						break;
					// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
					// This will effectively revert the list selection to a previous confirmed state.
					// Whatever happens below, we don't want to actually leave the "Choose phonological features" node selected!
					// This is at least required if the user selects "Cancel" from the dialog below.
					// N.B. the above does not seem to be true; therefore we check for cancel and an empty result
					// and force the combo text to be what it should be.
					pt.Hide();
					using (PhonologicalFeatureChooserDlg dlg = new PhonologicalFeatureChooserDlg())
					{
						Cache.DomainDataByFlid.BeginUndoTask(LexTextControls.ksUndoInsertPhonologicalFeature, LexTextControls.ksRedoInsertPhonologicalFeature);
						var fs = CreateEmptyFeatureStructureInAnnotation(null);
						dlg.SetDlgInfo(Cache, m_mediator, fs);
						dlg.ShowIgnoreInsteadOfDontCare = true;

						DialogResult result = dlg.ShowDialog(ParentForm);
						if (result == DialogResult.OK)
						{
							if (dlg.FS != null)
							{
								var sFeatures = dlg.FS.LongName;
								if (string.IsNullOrEmpty(sFeatures))
								{
									// user did not select anything in chooser; we want to show the last known node
									// in the dropdown, not "choose phonological feature".
									SetComboTextToLastConfirmedSelection();
								}
								else if (!pt.Nodes.ContainsKey(sFeatures))
								{
									var newSelectedNode = new HvoTreeNode(fs.LongNameTSS, fs.Hvo);
									pt.Nodes.Add(newSelectedNode);
									LoadPopupTree(fs.Hvo);
									selectedNode = newSelectedNode;
								}
							}
						}
						else if (result != DialogResult.Cancel)
						{
							dlg.HandleJump();
						}
						else if (result == DialogResult.Cancel)
						{
							// The user canceled out of the chooser; we want to show the last known node
							// in the dropdown, not "choose phonological feature".
							SetComboTextToLastConfirmedSelection();
						}
						Cache.DomainDataByFlid.EndUndoTask();
					}
					break;
				default:
					break;
			}
			// FWR-3432 - If we get here and we still haven't got a valid Hvo, don't continue
			// on to the base method. It'll crash.
			if (selectedNode.Hvo == kChoosePhonologicaFeatures)
				return;
			base.m_treeCombo_AfterSelect(sender, e);
		}
		#region IDisposable & Co. implementation
		// Region last reviewed: never


		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~PhonologicalFeaturePopupTreeManager()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
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
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (m_annotations != null)
				{
					Cache.DomainDataByFlid.BeginUndoTask(LexTextControls.ksUndoInsertPhonologicalFeature,
									 LexTextControls.ksRedoInsertPhonologicalFeature);
					foreach (var cmBaseAnnotation in m_annotations)
					{
						cmBaseAnnotation.Delete();
					}
					Cache.DomainDataByFlid.EndUndoTask();
				}
			}
			// Dispose unmanaged resources here, whether disposing is true or false.
			m_annotations = null;
			base.Dispose(disposing);
		}

		#endregion IDisposable & Co. implementation
	}
}
