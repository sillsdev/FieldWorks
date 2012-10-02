using System;
using System.Collections.Generic;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// WordBreakGuesser inserts word breaks into a text paragraph, based on the current
	/// wordform inventory.
	/// </summary>
	public class WordBreakGuesser
	{
		Set<string> m_words = new Set<string>(); // From text of word to dummy.
		int m_maxChars; // length of longest word
		ISilDataAccess m_sda;
		int m_vernWs = 0;
		FdoCache m_cache;
		public WordBreakGuesser(FdoCache cache, int hvoParaStart)
		{
			m_cache = cache;
			m_sda = cache.MainCacheAccessor;
			Setup(hvoParaStart);
		}

		private void Setup(int hvoPara)
		{
			var para = (IStTxtPara)m_cache.ServiceLocator.GetObject(hvoPara);
			int vernWs = StringUtils.GetWsAtOffset(para.Contents, 0);
			if (vernWs == m_vernWs)
				return;	// already setup.
			m_vernWs = vernWs;
			foreach (var wf in m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances())
			{
				string word = wf.Form.get_String(m_vernWs).Text;
				if (String.IsNullOrEmpty(word))
					continue;
				if (wf.SpellingStatus == (int)SpellingStatusStates.incorrect)
					continue; // if it's known to be incorrect don't use it to split things up!
				m_words.Add(word);
				m_maxChars = Math.Max(m_maxChars, word.Length);
			}
		}
		/// <summary>
		/// Insert breaks for the text in the indicated range of the indicate StTxtPara
		/// </summary>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="hvoPara"></param>
		public void Guess(int ichMin, int ichLim1, int hvoPara)
		{
			Setup(hvoPara);
			ITsString tss = m_sda.get_StringProp(hvoPara, StTxtParaTags.kflidContents);
			ITsStrBldr bldr = tss.GetBldr();
			int ichLim = ichLim1;
			if (ichLim1 == -1)
				ichLim = tss.Length;
			string txt = tss.Text;
			int offset = 0; // offset in tsb caused by previously introduced spaces.
			for (int ichStart = 0; ichStart < ichLim; ) // no advance! decide inside loop.
			{
				int lim = ichLim;
				// Most likely the input is one massive string which is already recorded as one wordform, unfortunately.
				// If we try to match the whole thing we match that one wordform and don't do anything.
				if (ichStart == ichMin)
					lim--;
				int cch = Match(ichStart, lim, txt);
				if (cch < 0)
				{
					// no match
					ichStart ++;
				}
				else
				{
					// match, ensure spaces.

					if (ichStart > 0 && !SpaceAt(bldr, ichStart + offset - 1))
					{
						InsertSpace(bldr, ichStart + offset);
						offset++;
					}
					if (ichStart + cch < ichLim && !SpaceAt(bldr, ichStart + cch + offset))
					{
						InsertSpace(bldr, ichStart + cch + offset);
						offset++;
					}
					ichStart += cch;
				}
			}
			if (offset > 0)
			{
				UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoGuessWordBreaks, ITextStrings.ksRedoGuessWordBreaks, m_cache.ActionHandlerAccessor,
				() => m_sda.SetString(hvoPara, StTxtParaTags.kflidContents, bldr.GetString()));
			}
		}

		bool SpaceAt(ITsStrBldr bldr, int ich)
		{
			return bldr.Text[ich] == ' '; // optimize: do some trick with FetchChars
		}

		void InsertSpace(ITsStrBldr bldr, int ich)
		{
			bldr.Replace(ich, ich, " ", null);
		}

		/// <summary>
		/// Find the longest match possible starting at ichStart up to but not including ichLim
		/// </summary>
		/// <param name="ichStart"></param>
		/// <param name="ichLim"></param>
		/// <param name="bldr"></param>
		/// <returns></returns>
		int Match(int ichStart, int ichLim, string txt)
		{
			int max = Math.Min(ichLim - ichStart, m_maxChars);
			for (int cch = max; cch > 0; cch--)
			{
				if (m_words.Contains(txt.Substring(ichStart, cch)))
					return cch;
			}
			return -1;
		}
	}
}
