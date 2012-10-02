using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// WfiWordformUi provides UI-specific methods for the WfiWordformUi class.
	/// </summary>
	public class WfiWordformUi : CmObjectUi
	{
		/// <summary>
		/// Create one. Argument must be a WfiWordform.
		/// Note that declaring it to be forces us to just do a cast in every case of MakeUi, which is
		/// passed an obj anyway.
		/// </summary>
		/// <param name="obj"></param>
		public WfiWordformUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IWfiWordform);
		}

		internal WfiWordformUi() {}

		/// <summary>
		/// This will recache some information related to a wordform and its analyses,
		/// and call PropChanged to get the display to refresh.
		/// </summary>
		/// <param name="curDisplayedWfId"></param>
		/// <param name="updateUserCount"></param>
		/// <param name="updateUserStatusIcon"></param>
		/// <param name="updateParserCount"></param>
		/// <param name="updateParserStatusIcon"></param>
		/// <remarks>
		/// It makes no sense to call this method if the active area isn't the Words area,
		/// and the tool isn't Analyses.
		/// </remarks>
		public void UpdateWordsToolDisplay(
			int curDisplayedWfId,
			bool updateUserCount, bool updateUserStatusIcon,
			bool updateParserCount, bool updateParserStatusIcon)
		{
			CheckDisposed();

			IWfiWordform wf = (IWfiWordform)Object;

			// First update the cache of the real property on which everything else depends.
			// This should take care of LT-3874.
			string sql = string.Format("select id from WfiAnalysis_ where owner$ = {0}", wf.Hvo);
			int[] newAnalyses = DbOps.ReadIntArrayFromCommand(wf.Cache, sql, null);
			int oldCount = m_cache.MainCacheAccessor.get_VecSize(wf.Hvo, (int)WfiWordform.WfiWordformTags.kflidAnalyses);
			m_cache.VwCacheDaAccessor.CacheVecProp(wf.Hvo, (int)WfiWordform.WfiWordformTags.kflidAnalyses, newAnalyses, newAnalyses.Length);
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, wf.Hvo,
				(int)WfiWordform.WfiWordformTags.kflidAnalyses, 0, newAnalyses.Length, oldCount);

			bool updateConflictCount = false;
			if (updateUserCount)
			{
				m_cache.PropChanged(
					null,
					PropChangeType.kpctNotifyAll,
					wf.Hvo,
					BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "UserCount"),
					0, 0, 0);
				updateConflictCount = true;
			}
			if (updateParserCount)
			{
				m_cache.PropChanged(
					null,
					PropChangeType.kpctNotifyAll,
					wf.Hvo,
					BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "ParserCount"),
					0, 0, 0);
				updateConflictCount = true;
			}
			if (updateConflictCount)
			{
				m_cache.PropChanged(
					null,
					PropChangeType.kpctNotifyAll,
					wf.Hvo,
					BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "ConflictCount"),
					0, 0, 0);
			}

			if (curDisplayedWfId == wf.Hvo)
			{
				// Update "WfiWordform"-"HumanApprovedAnalyses"
				m_cache.PropChanged(
					null,
					PropChangeType.kpctNotifyAll,
					wf.Hvo,
					BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "HumanApprovedAnalyses"),
					0, 0, 0);
				// Update "WfiWordform"-"HumanNoOpinionParses"
				m_cache.PropChanged(
					null,
					PropChangeType.kpctNotifyAll,
					wf.Hvo,
					BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "HumanNoOpinionParses"),
					0, 0, 0);
				// Update "WfiWordform"-"HumanDisapprovedParses"
				m_cache.PropChanged(
					null,
					PropChangeType.kpctNotifyAll,
					wf.Hvo,
					BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "HumanDisapprovedParses"),
					0, 0, 0);
				// Update "WfiAnalysis", "ParserStatusIcon"
				int psiFlid = BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiAnalysis", "ParserStatusIcon");
				foreach (int analId in m_cache.GetVectorProperty(wf.Hvo, (int)WfiWordform.WfiWordformTags.kflidAnalyses, true))
				{
					if (updateParserStatusIcon)
					{
						// This will force an update to the slice the shows the parser results.
						WfiAnalysis anal = (WfiAnalysis)CmObject.CreateFromDBObject(m_cache, analId, true);
						m_cache.PropChanged(
							null,
							PropChangeType.kpctNotifyAll,
							analId,
							psiFlid,
							0, 0, 0);
					}
				}
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method implements the FindInDictionary menu item. It is called using
		/// reflection by xCore, not directly.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFindInDictionary(object args)
		{
			LexEntryUi.DisplayEntries(m_cache, Form.ActiveForm, m_mediator, null, null,
				(this.Object as WfiWordform).Form.GetAlternativeTss(m_cache.DefaultVernWs));
			return true;
		}

		protected override bool IsAcceptableContextToJump(string toolCurrent, string toolTarget)
		{
			if (toolCurrent == "wordListConcordance" && toolTarget == "concordance")
				return false;
			return base.IsAcceptableContextToJump(toolCurrent, toolTarget);
		}

	}
}
