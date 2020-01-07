// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Menu item subclass containing the information needed to add an item to a dictionary.
	/// </summary>
	public class AddToDictMenuItem : ToolStripMenuItem
	{
		private readonly ISpellEngine m_dict;
		private readonly IVwRootBox m_rootb;
		private readonly int m_hvoObj;
		private readonly int m_tag;
		private readonly int m_wsAlt; // 0 if not multilingual--not yet implemented.
		private readonly LcmCache m_cache;

		/// <summary />
		internal AddToDictMenuItem(ISpellEngine dict, string word, IVwRootBox rootb, int hvoObj, int tag, int wsAlt, int wsText, string text, LcmCache cache)
			: base(text)
		{
			m_rootb = rootb;
			m_dict = dict;
			Word = word;
			m_hvoObj = hvoObj;
			m_tag = tag;
			m_wsAlt = wsAlt;
			WritingSystem = wsText;
			m_cache = cache;
		}

		/// <summary />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}

		/// <summary>
		/// Add the current word to the dictionary.
		/// </summary>
		public void AddWordToDictionary()
		{
			m_rootb.DataAccess.BeginUndoTask(RootSiteStrings.ksUndoAddToSpellDictionary, RootSiteStrings.ksRedoAddToSpellDictionary);
			if (m_rootb.DataAccess.GetActionHandler() != null)
			{
				m_rootb.DataAccess.GetActionHandler().AddAction(new UndoAddToSpellDictAction(WritingSystem, Word, m_rootb, m_hvoObj, m_tag, m_wsAlt));
			}
			AddToSpellDict(m_dict, Word, WritingSystem);
			m_rootb.PropChanged(m_hvoObj, m_tag, m_wsAlt, 1, 1);
			m_rootb.DataAccess.EndUndoTask();
		}

		/// <summary>
		/// This information is useful for an override of MakeSpellCheckMenuOptions in TeEditingHelper.
		/// </summary>
		public string Word { get; }

		/// <summary>
		/// The writing system of the actual mis-spelled word.
		/// </summary>
		public int WritingSystem { get; }

		/// <summary>
		/// Add the word to the spelling dictionary.
		/// Overrides to also add to the wordform inventory.
		/// </summary>
		private void AddToSpellDict(ISpellEngine dict, string word, int ws)
		{
			dict.SetStatus(word, true);
			if (m_cache == null)
			{
				return; // bizarre, but means we just can't do it.
			}
			// If it's in a current vernacular writing system, we want to update the WFI as well.
			if (!m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Any(wsObj => wsObj.Handle == ws))
			{
				return;
			}
			// Now get matching wordform (create if needed).
			var servLoc = m_cache.ServiceLocator;
			var wf = servLoc.GetInstance<IWfiWordformRepository>().GetMatchingWordform(ws, word) ?? servLoc.GetInstance<IWfiWordformFactory>().Create(TsStringUtils.MakeString(word, ws));
			wf.SpellingStatus = (int)SpellingStatusStates.correct;
		}

		/// <summary>
		/// Supports undoing and redoing adding an item to a dictionary
		/// </summary>
		private sealed class UndoAddToSpellDictAction : IUndoAction
		{
			private readonly int m_wsText;
			private readonly string m_word;
			private readonly int m_hvoObj;
			private readonly int m_tag;
			private readonly int m_wsAlt;
			private readonly IVwRootBox m_rootb;

			public UndoAddToSpellDictAction(int wsText, string word, IVwRootBox rootb, int hvoObj, int tag, int wsAlt)
			{
				m_wsText = wsText;
				m_word = word;
				m_hvoObj = hvoObj;
				m_tag = tag;
				m_wsAlt = wsAlt;
				m_rootb = rootb;
			}

			#region IUndoAction Members

			public void Commit()
			{
			}

			public bool IsDataChange => true;

			public bool IsRedoable => true;

			public bool Redo()
			{
				SpellingHelper.SetSpellingStatus(m_word, m_wsText, m_rootb.DataAccess.WritingSystemFactory, true);
				m_rootb.PropChanged(m_hvoObj, m_tag, m_wsAlt, 1, 1);
				return true;
			}

			public bool SuppressNotification
			{
				set { }
			}

			public bool Undo()
			{
				SpellingHelper.SetSpellingStatus(m_word, m_wsText, m_rootb.DataAccess.WritingSystemFactory, false);
				m_rootb.PropChanged(m_hvoObj, m_tag, m_wsAlt, 1, 1);
				return true;
			}

			#endregion
		}
	}
}