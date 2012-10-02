using System;
using System.Collections.Generic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

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
			int vernWs = StTxtPara.GetWsAtParaOffset(m_cache, hvoPara, 0);
			if (vernWs == m_vernWs)
				return;	// already setup.
			m_vernWs = vernWs;
			string sql = string.Format("select txt from WfiWordform_Form where ws={0}", vernWs);
			IOleDbCommand odc = DbOps.MakeRowSet(m_cache, sql, null);
			m_maxChars = 0;
			try
			{
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				while (fMoreRows)
				{
					string word = DbOps.ReadString(odc, 0);
					m_words.Add(word);
					m_maxChars = Math.Max(m_maxChars, word.Length);
					odc.NextRow(out fMoreRows);
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
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
			ITsString tss = m_sda.get_StringProp(hvoPara, (int)StTxtPara.StTxtParaTags.kflidContents);
			ITsStrBldr bldr = tss.GetBldr();
			int ichLim = ichLim1;
			if (ichLim1 == -1)
				ichLim = tss.Length;
			string txt = tss.Text;
			int offset = 0; // offset in tsb caused by previously introduced spaces.
			for (int ichStart = 0; ichStart < ichLim; ) // no advance! decide inside loop.
			{
				int cch = Match(ichStart, ichLim, txt);
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
				m_sda.SetString(hvoPara, (int)StTxtPara.StTxtParaTags.kflidContents, bldr.GetString());
				m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoPara,
					(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 0);
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
