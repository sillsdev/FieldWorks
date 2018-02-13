// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// The 'Sandbox' is a small view used to edit the data associated with a single Wordform...
	/// specifically, the data associated with a standard interlinear view. It uses a different,
	/// simpler model than the real interlinear view, documented below under the constants we
	/// use to describe it. This view makes editing easier. A further advantage is that we don't
	/// have to worry, until the view closes, about whether the user is editing one of the
	/// existing analyses or creating a new one.
	/// </summary>
	internal class Sandbox : SandboxBase, IAnalysisControlInternal
	{
		#region Data members

		InterlinDocForAnalysis m_interlinDoc;

		#endregion Data members

		#region Construction and initialization

		/// <summary>
		/// Default Constructor.
		/// </summary>
		public Sandbox()
		{
		}

		/// <summary>
		/// Create a new one.
		/// </summary>
		public Sandbox(LcmCache cache, IVwStylesheet ss, InterlinLineChoices choices, AnalysisOccurrence selected, FocusBoxController focusBox)
			: this(cache, ss, choices)
		{
			FocusBox = focusBox;
			m_interlinDoc = focusBox.InterlinDoc;
			m_occurrenceSelected = selected;
			// Finish initialization with occurrence context.
			LoadForWordBundleAnalysis(m_occurrenceSelected.Analysis.Hvo);
		}

		/// <summary />
		public Sandbox(LcmCache cache, IVwStylesheet ss, InterlinLineChoices choices)
			: base(cache, ss, choices)
		{
		}

		public bool IsDirty => Caches.DataAccess.IsDirty();

		/// <summary>
		/// let the parent control the selection.
		/// </summary>
		public override bool WantInitialSelection => false;

		/// <summary>
		/// We don't want to do a load on our hvoAnalysis until our HvoAnnotation is setup.
		/// </summary>
		protected override void LoadForWordBundleAnalysis(int hvoAnalysis)
		{
			if (HvoAnnotation != 0)
			{
				base.LoadForWordBundleAnalysis(hvoAnalysis);
			}
		}

		/// <summary>
		/// Set up the sandbox to display the specified analysis, hvoAnalysis, which might be a WfiWordform,
		/// WfiAnalysis, or WfiGloss.
		/// </summary>
		public void SwitchWord(AnalysisOccurrence selected)
		{
			m_occurrenceSelected = selected;
			RawWordformWs = 0;
			TreatAsSentenceInitial = m_occurrenceSelected.Index == 0;
			ReconstructForWordBundleAnalysis(m_occurrenceSelected.Analysis.Hvo);
		}

		/// <summary>
		/// Use the cba offsets in the text to get the RawTextform
		/// </summary>
		public override ITsString RawWordform
		{
			get
			{
				if ((m_rawWordform == null || m_rawWordform.Length == 0) && m_occurrenceSelected != null)
				{
					// force reload of this string, just in case
					m_rawWordform = m_occurrenceSelected.BaselineText;
				}
				return m_rawWordform;
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			base.Dispose( disposing );

			if (disposing)
			{
			}

			m_interlinDoc = null;
		}

		#endregion Construction and initialization

		/// <summary>
		/// This is useful for determining the number of instances where a WordGloss is used in IText.
		/// The total number of wordglosses equals
		///		the WfiGlosses referenced by CmBaseAnnotation.InstanceOf plus
		///		the present sandbox WordGloss if it doesn't match the original state of the Sandbox.
		/// cf. (LT-1428).
		/// </summary>
		internal override int WordGlossReferenceCount
		{
			get
			{
				var glossReferenceCount = 0;
				var glossRepository = Cache.ServiceLocator.GetInstance<IWfiGlossRepository>();
				if (WordGlossHvo != 0)
				{
					var gloss = glossRepository.GetObject(WordGlossHvo);
					glossReferenceCount = SegmentServices.SegmentsContainingWag(new AnalysisTree(gloss)).Count();
				}
				// if FocusBox.InterlinWordControl.WordGlossHvo != m_hvoAnalysis
				//		then we are editing a different WordGloss, whose count is not reflected in
				//		the present state of the database for WfiGlosses.
				//		So, add it to the WfiGloss count before we return.
				if (WordGlossHvo != m_hvoInitialWag)
				{
					++glossReferenceCount;
				}

				return glossReferenceCount;
			}
		}

		internal override InterlinDocForAnalysis InterlinDoc => m_interlinDoc;

		private FocusBoxController FocusBox
		{
			get;
		}


		protected override void OnHandleEnter()
		{
#if RANDYTODO
			if (FocusBox is FocusBoxControllerForDisplay)
				(FocusBox as FocusBoxControllerForDisplay).OnApproveAndMoveNext();
#endif
		}

		#region IAnalysisControlInternal Members

		/// <summary>
		/// Data has been modified since we first loaded it from real data,
		/// indicating we may need to save the new state back to the database.
		/// </summary>
		bool IAnalysisControlInternal.HasChanged => Caches.DataAccess.IsDirty();

		/// <summary>
		/// This function will undo the last changes done to the project.
		/// This function is executed when the user clicks the undo menu item.
		/// </summary>
		void IAnalysisControlInternal.Undo()
		{
			HandleUndo(null, null);
		}

		#endregion
	}
}