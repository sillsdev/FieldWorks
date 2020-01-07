// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	internal sealed class TestableFocusBox : FocusBoxController
	{
		internal TestableFocusBox()
		{
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}
			base.Dispose(disposing);
		}

		protected override IAnalysisControlInternal CreateNewSandbox(AnalysisOccurrence selected)
		{
			var sandbox = new MockSandbox
			{
				CurrentAnalysisTree = { Analysis = selected.Analysis },
				NewAnalysisTree = { Analysis = selected.Analysis }
			};
			return sandbox;
		}

		public override void SelectOccurrence(AnalysisOccurrence selected)
		{
			// when we change wordforms we should create a new Analysis Tree, so we don't
			// overwrite the last state of one we may have saved during the tests.
			if (InterlinWordControl != null && selected != SelectedOccurrence)
			{
				(InterlinWordControl as MockSandbox).NewAnalysisTree = new AnalysisTree();
			}
			base.SelectOccurrence(selected);
		}

		/// <summary>
		/// Use to establish a new analysis to be approved.
		/// </summary>
		internal delegate AnalysisTree CreateNewAnalysis();

		internal CreateNewAnalysis DoDuringUnitOfWork { get; set; }

		protected override bool ShouldCreateAnalysisFromSandbox(bool fSaveGuess)
		{
			return DoDuringUnitOfWork != null || base.ShouldCreateAnalysisFromSandbox(fSaveGuess);
		}

		protected override void ApproveAnalysis(bool fSaveGuess)
		{
			if (DoDuringUnitOfWork != null)
			{
				NewAnalysisTree.Analysis = DoDuringUnitOfWork().Analysis;
			}
			base.ApproveAnalysis(fSaveGuess);
		}

		internal AnalysisTree NewAnalysisTree => (InterlinWordControl as MockSandbox).NewAnalysisTree;

		private sealed class MockSandbox : UserControl, IAnalysisControlInternal
		{
			internal MockSandbox()
			{
				CurrentAnalysisTree = new AnalysisTree();
				NewAnalysisTree = new AnalysisTree();
			}

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				base.Dispose(disposing);
			}

			#region IAnalysisControlInternal Members

			bool IAnalysisControlInternal.HasChanged => CurrentAnalysisTree.Analysis != NewAnalysisTree.Analysis;

			void IAnalysisControlInternal.MakeDefaultSelection()
			{
			}

			bool IAnalysisControlInternal.RightToLeftWritingSystem => false;

			void IAnalysisControlInternal.SwitchWord(AnalysisOccurrence selected)
			{
				CurrentAnalysisTree.Analysis = selected.Analysis;
				NewAnalysisTree.Analysis = selected.Analysis;
			}

			internal AnalysisTree CurrentAnalysisTree { get; set; }
			internal AnalysisTree NewAnalysisTree { get; set; }

			bool IAnalysisControlInternal.ShouldSave(bool fSaveGuess)
			{
				return (this as IAnalysisControlInternal).HasChanged;
			}

			void IAnalysisControlInternal.Undo()
			{
			}

			#endregion

			AnalysisTree IAnalysisControlInternal.GetRealAnalysis(bool fSaveGuess, out IWfiAnalysis obsoleteAna)
			{
				obsoleteAna = null;
				return NewAnalysisTree;
			}

			public int GetLineOfCurrentSelection()
			{
				throw new NotSupportedException();
			}

			public bool SelectOnOrBeyondLine(int startLine, int increment)
			{
				throw new NotSupportedException();
			}

			public void UpdateLineChoices(InterlinLineChoices choices)
			{
				throw new NotSupportedException();
			}

			public int MultipleAnalysisColor
			{
				set {; }
			}

			public bool IsDirty
			{
				get { throw new NotSupportedException(); }
			}
		}
	}
}