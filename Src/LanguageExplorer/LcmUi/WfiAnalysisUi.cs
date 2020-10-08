// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI functions for WfiAnalysis.
	/// </summary>
	internal sealed class WfiAnalysisUi : CmObjectUi
	{
		protected override void ReallyDeleteUnderlyingObject()
		{
			using (var helper = new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor, LcmUiResources.ksUndoDelete, LcmUiResources.ksRedoDelete))
			{
				// we need to include resetting the wordform's checksum as part of the undo action for deleting this analysis.
				base.ReallyDeleteUnderlyingObject();
				// Make sure it gets parsed the next time.
				((IWfiWordform)MyCmObject.Owner).Checksum = 0;
				helper.RollBack = false;
			}
		}
	}
}