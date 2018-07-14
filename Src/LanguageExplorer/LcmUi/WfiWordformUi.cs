// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// WfiWordformUi provides UI-specific methods for the WfiWordformUi class.
	/// </summary>
	public class WfiWordformUi : CmObjectUi
	{
		/// <summary>
		/// Create one. Argument must be a WfiWordform.
		/// Note that declaring it to be forces us to just do a cast in every case of MakeLcmModelUiObject, which is
		/// passed an obj anyway.
		/// </summary>
		public WfiWordformUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IWfiWordform);
		}

		internal WfiWordformUi() {}

		/// <summary>
		/// This will recache some information related to a wordform and its analyses,
		/// and call PropChanged to get the display to refresh.
		/// </summary>
		/// <remarks>
		/// It makes no sense to call this method if the active area isn't the Words area,
		/// and the tool isn't Analyses.
		/// </remarks>
		public void UpdateWordsToolDisplay(int curDisplayedWfId, bool updateUserCount, bool updateUserStatusIcon, bool updateParserCount, bool updateParserStatusIcon)
		{
			// JohnT: hopefully we have code in LCM or in various decorators to update stuff as much as we choose,
			// based on side effect and PropChanged handlers.
			// Perfect updating is probably too expensive.
			// Nothing useful we can currently do here, anyway.
		}

		/// <summary>
		/// This method implements the FindInDictionary menu item. It is called using
		/// reflection by xCore, not directly.
		/// </summary>
		protected bool OnFindInDictionary(object args)
		{
			LexEntryUi.DisplayEntries(m_cache, Form.ActiveForm, PropertyTable, Publisher, Subscriber, null, null,
				((IWfiWordform)MyCmObject).Form.get_String(m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle), null);
			return true;
		}

		protected override bool IsAcceptableContextToJump(string toolCurrent, string toolTarget)
		{
			if (toolCurrent == AreaServices.WordListConcordanceMachineName && toolTarget == AreaServices.ConcordanceMachineName)
			{
				return false;
			}
			return base.IsAcceptableContextToJump(toolCurrent, toolTarget);
		}

		public override bool CanDelete(out string cannotDeleteMsg)
		{
			if (base.CanDelete(out cannotDeleteMsg))
			{
				return true;
			}
			cannotDeleteMsg = LcmUiStrings.ksCannotDeleteWordform;
			return false;
		}
	}
}
