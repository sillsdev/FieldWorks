// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI functions for WfiAnalysis.
	/// </summary>
	public class WfiAnalysisUi : CmObjectUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj">The object.</param>
		public WfiAnalysisUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IWfiAnalysis);
		}

		internal WfiAnalysisUi()
		{ }

		protected override bool ShouldDisplayMenuForClass(int specifiedClsid)
		{
			return WfiAnalysisTags.kClassId == specifiedClsid;
		}

		protected override void ReallyDeleteUnderlyingObject()
		{
			// Gather original counts.
			var wf = (IWfiWordform) Object.Owner;
			int prePACount = wf.ParserCount;
			int preUACount = wf.UserCount;
			// we need to include resetting the wordform's checksum as part of the undo action
			// for deleting this analysis.
			using (var helper = new UndoableUnitOfWorkHelper(
				m_cache.ActionHandlerAccessor, LcmUiStrings.ksUndoDelete, LcmUiStrings.ksRedoDelete))
			{
				base.ReallyDeleteUnderlyingObject();

				// We need to fire off a notification about the deletion for several virtual fields.
				using (var wfui = new WfiWordformUi(wf))
				{
					bool updateUserCountAndIcon = (preUACount != wf.UserCount);
					bool updateParserCountAndIcon = (prePACount != wf.ParserCount);
					wfui.UpdateWordsToolDisplay(wf.Hvo,
						updateUserCountAndIcon, updateUserCountAndIcon,
						updateParserCountAndIcon, updateParserCountAndIcon);
				}

				// Make sure it gets parsed the next time.
				wf.Checksum = 0;

				helper.RollBack = false;
			}
		}
	}
}