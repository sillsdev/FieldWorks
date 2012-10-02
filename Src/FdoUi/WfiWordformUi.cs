using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;

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
			// JohnT: hopefully we have code in FDO or in various decorators to update stuff as much as we choose,
			// based on side effect and PropChanged handlers.
			// Perfect updating is probably too expensive.
			// Nothing useful we can currently do here, anyway.

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
				((IWfiWordform) Object).Form.get_String(m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle), null);
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
