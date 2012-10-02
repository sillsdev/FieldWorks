using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters31;

namespace DChartHelper
{
	public partial class PickAmbiguity : Form
	{
		protected Font m_fontSource = null;
		protected Font m_fontTarget = null;

		public PickAmbiguity(string strWords, Font fontSource, Font fontTarget)
		{
			InitializeComponent();
			m_fontSource = fontSource;
			m_fontTarget = fontTarget;
			string[] astrWords = strWords.Split(new char[] { '%' });
			foreach (string strWord in astrWords)
				listBoxAmbiguousWords.Items.Add(strWord);
		}

		/// <summary>
		/// this constructor is used when the grid is passing a whole phrase worth of
		/// words (which is the cell's Value property). This ctor will first split it
		/// into words and if any of them are ambiguous, it will prompt for the correct
		/// value one-by-one. Then it will return the entire string with the selected
		/// ambiguities
		/// </summary>
		/// <param name="oPhrase"></param>
		public PickAmbiguity(object oPhrase, DirectableEncConverter aEC, Font fontSource, Font fontTarget)
		{
			InitializeComponent();
			m_fontSource = fontSource;
			m_fontTarget = fontTarget;

			if ((aEC != null) && (aEC.GetEncConverter.GetType() == typeof(AdaptItEncConverter)))
				m_aEC = (AdaptItEncConverter)aEC.GetEncConverter;

			if (oPhrase != null)
			{
				string strPhrase = (string)oPhrase;

				// first split it based on words:
				m_astrWords = new List<string>();
				string[] astrWords = strPhrase.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string strWord in astrWords)
					m_astrWords.Add(strWord);

				FixupListForPossibleMultiWordAmbiguity(ref m_astrWords);
			}

			if (AddToKb)
				Text = "Add to KB";
		}

		private void FixupListForPossibleMultiWordAmbiguity(ref List<string> astrWords)
		{
			// what we just did (i.e. split on 'space' works 99% of the time, but if there were ambiguities
			//  that were multi words, then it fails (e.g. "%3%one two%one two three%four%")
			for (int i = 0; i < astrWords.Count; i++)
			{
				string strWord = astrWords[i];
				int nIndex = strWord.IndexOf('%');
				if (nIndex != -1)
				{
					// make sure we have the whole ambiguity set
					nIndex = strWord.IndexOf('%', 1);
					try
					{
						int nAmbs = Convert.ToInt32(strWord.Substring(1, nIndex - 1));
						while (nAmbs-- > 0)
						{
							int nNewIndex = strWord.IndexOf('%', nIndex + 1);
							if (nNewIndex == -1)
							{
								// this means that one of the ambiguities had a space! (e.g. /%2%one%or/
								//  when it should have been %2%one%or two%). This means that the next word
								//  is actually part of this word (and it presumably equals /two%/)
								while ((i < astrWords.Count) && (nNewIndex == -1))
								{
									// grab the next word (with a space in between)
									string strNextWord = m_astrWords[i + 1];
									nNewIndex = strNextWord.IndexOf('%');
									strWord += ' ' + strNextWord;
									m_astrWords[i] = strWord;
									m_astrWords.RemoveAt(i + 1);
								}
								nIndex += nNewIndex + 1;
							}
							else
								nIndex = nNewIndex;
						}
					}
					catch
					{
						System.Diagnostics.Debug.Assert(false, "shouldn't have failed here!");
					}
				}
			}
		}

		private AdaptItEncConverter m_aEC = null;
		private int m_nIndexAmb = -1;
		private List<string> m_astrWords = null;

		private bool AddToKb
		{
			get { return (m_aEC != null); }
		}

		public new DialogResult ShowDialog()
		{
			if (ShouldShowDialog())
				return base.ShowDialog();
			else
				return DialogResult.OK;
		}

		protected bool IsPhrase
		{
			get { return (m_astrWords != null); }
		}

		string strFrontMatter = null;
		string strBackMatter = null;

		protected bool ShouldShowDialog()
		{
			/* This isn't correct: what if we want the 'Add' dialog. A single word is totally fine.
			if (!IsPhrase)    // for a single word, we're already ready
				return true;
			*/
			if (AddToKb)
			{
				if (m_astrWords.Count == 1)
				{
					int nIndex = listBoxAmbiguousWords.Items.Add(m_astrWords[0]);
					listBoxAmbiguousWords.SelectedIndex = nIndex;
					// UpdateKB();
					return false;
				}
				else
				{
					listBoxAmbiguousWords.Items.Clear();
					foreach (string strWord in m_astrWords)
						listBoxAmbiguousWords.Items.Add(strWord);
					return true;
				}
			}
			else
			{
				m_nIndexAmb = -1;   // only show if there's an ambiguity to choose
				for (int i = 0; i < m_astrWords.Count; i++)
				{
					string strWord = m_astrWords[i];
					int nIndex = -1;
					if ((nIndex = strWord.IndexOf('%')) != -1)
					{
						// in case of any preceding punctuation
						if (nIndex != 0)
						{
							strFrontMatter = strWord.Substring(0, nIndex);
							strWord = strWord.Substring(nIndex);
						}
						else
							strFrontMatter = null;

						System.Diagnostics.Debug.Assert(strWord.IndexOf('%') == 0);

						// if there is punctuation between two sets of ambiguous words
						//  (e.g. "%2%water%to.put.on-DFUT%-%2%water%to.put.on-DFUT%")
						// then the old way won't work, so count the
						// the new way is to count the '%'s until we get thru the ambiguity
						nIndex = strWord.IndexOf('%', 1);
						try
						{
							int nAmbs = Convert.ToInt32(strWord.Substring(1, nIndex - 1));
							while (nAmbs-- > 0)
								nIndex = strWord.IndexOf('%', nIndex + 1);
							System.Diagnostics.Debug.Assert(nIndex != -1);  // shouldn't happen with FixupListForPossibleMultiWordAmbiguity
						}
						catch
						{
							System.Diagnostics.Debug.Assert(false, "shouldn't have failed here!");
						}

						if (nIndex < (strWord.Length - 1))
						{
							strBackMatter = strWord.Substring(nIndex + 1);
							strWord = strWord.Substring(0, nIndex);
						}
						else
							strBackMatter = null;

						m_nIndexAmb = i;
						string[] astrAmbs = strWord.Split(new char[] { '%' }, StringSplitOptions.RemoveEmptyEntries);

						int j = 0;
						if (astrAmbs.Length > 1)
							j = 1;

						listBoxAmbiguousWords.Items.Clear();
						while (j < astrAmbs.Length)
							listBoxAmbiguousWords.Items.Add(astrAmbs[j++]);

						break;
					}
				}
				return (m_nIndexAmb >= 0);
			}
		}

		public string SelectedWord
		{
			get { return (string)this.listBoxAmbiguousWords.SelectedItem; }
			set
			{
				if (this.listBoxAmbiguousWords.SelectedIndex != -1)
					this.listBoxAmbiguousWords.Items[this.listBoxAmbiguousWords.SelectedIndex] = value;
			}
		}

		public string DisambiguatedPhrase
		{
			get
			{
				string str = null;
				foreach (string strWord in m_astrWords)
					str += strWord + ' ';
				if (!String.IsNullOrEmpty(str))
					str = str.Remove(str.Length - 1);
				return str;
			}
		}

		private void UpdateKB()
		{
			System.Diagnostics.Debug.Assert(m_aEC != null);
			AddKBEntry dlg = new AddKBEntry(SelectedWord, SelectedWord, m_fontSource, m_fontTarget);
			if (dlg.ShowDialog() == DialogResult.OK)
			{
#if !MovedToEncCnvtrs
				try
				{
					m_aEC.AddEntryPair(dlg.Source, dlg.Target);
				}
				catch (Exception ex)
				{
					MessageBox.Show(String.Format("Unable to access the knowledge base because:{0}{0}{1}",
						Environment.NewLine, ex.Message), DiscourseChartForm.cstrCaption);
				}

				m_bTurnOffSelectedIndexChange = true;
				SelectedWord = dlg.Target;
				m_bTurnOffSelectedIndexChange = false;
#else
				AdaptItKnowledgeBase aikb = new AdaptItKnowledgeBase();
				string strKbPath = m_aEC.GetEncConverter.ConverterIdentifier;
				aikb.ReadXml(strKbPath);
				if ((aikb.KB.Count > 0) && (aikb.MAP.Count > 0))
				{
					// first see if there's already a row with this word in it (in which case, we just add a new entry)
					if (!FindExistingEntry(aikb, strKbPath, dlg.Source, dlg.Target))
					{
						AdaptItKnowledgeBase.TURow aTURow = aikb.TU.AddTURow("0", dlg.Source, aikb.MAP[0]);
						aikb.RS.AddRSRow("1", dlg.Target, aTURow);
						aikb.WriteXml(strKbPath);
					}

					m_bTurnOffSelectedIndexChange = true;
					SelectedWord = dlg.Target;
					m_bTurnOffSelectedIndexChange = false;
				}
#endif
			}
		}

		private bool m_bTurnOffSelectedIndexChange = false;

		private void listBoxAmbiguousWords_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_bTurnOffSelectedIndexChange || (listBoxAmbiguousWords.SelectedIndex == -1))
				return;

			if (IsPhrase)
			{
				if (AddToKb)
				{
					UpdateKB();
					m_astrWords[this.listBoxAmbiguousWords.SelectedIndex] = SelectedWord;
					return;
				}
				else
				{
					m_astrWords[m_nIndexAmb] = strFrontMatter + SelectedWord + strBackMatter;
					if (ShouldShowDialog())
						return;
				}
			}

			DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}