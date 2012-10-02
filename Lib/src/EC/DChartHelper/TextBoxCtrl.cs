using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms; // for Clipboard

namespace DChartHelper
{
	class TextBoxCtrl : System.Windows.Forms.RichTextBox
	{
		protected List<string> m_astrLines = new List<string>();
		protected int m_nLineIndex;
		protected List<string> m_astrsWords = new List<string>();
		protected char[] m_achDelims = new char[] { ' ' };

		public void InitLines(string[] astrLines)
		{
			m_astrLines.Clear();
			Clear();
			foreach (string str in astrLines)
				m_astrLines.Add(str);

			m_nLineIndex = 0;
			ParseLine(m_astrLines[m_nLineIndex]);
		}

		public string GetCurrentWord()
		{
			string str = null;
			if (m_astrsWords.Count > 0)
				str = m_astrsWords[0];

			NextWord();
			return str;
		}

		public void SetCurrentWord(string strWord)
		{
			if ((strWord != null) && (m_astrsWords != null))
			{
				m_astrsWords.Insert(0, strWord);
				UpdateText();
			}
		}

		public void MoveNextLine()
		{
			if (++m_nLineIndex < m_astrLines.Count)
				ParseLine(m_astrLines[m_nLineIndex]);
		}

		public void MovePrevLine()
		{
			if (--m_nLineIndex < 0)
				m_nLineIndex = 0;
			if (m_nLineIndex < m_astrLines.Count)
				ParseLine(m_astrLines[m_nLineIndex]);
		}

		public void PutLineParse(string strLine)
		{
			// m_astrLines.Insert(m_nLineIndex, strLine);
			m_astrLines.Clear();
			m_astrLines.Add(strLine);
			m_nLineIndex = 0;
			ParseLine(strLine);
		}

		protected void ParseLine(string strLine)
		{
			m_astrsWords.Clear();
			string[] astrWords = strLine.Split(m_achDelims, StringSplitOptions.RemoveEmptyEntries);
			if (astrWords.Length > 0)
			{
				foreach (string strWord in astrWords)
					m_astrsWords.Add(strWord);
				UpdateText();
			}
		}

		protected void UpdateText()
		{
			string strLine = null;
			foreach (string strWord in m_astrsWords)
			{
				strLine += strWord + ' ';
			}

			if (strLine == null)
				this.Text = strLine;
			else
				this.Text = strLine.Remove(strLine.Length - 1);
		}

		public void NextWord()
		{
			if ((m_astrLines == null) || (m_astrLines.Count == 0))
				return;

			else if (m_astrsWords.Count > 0)
				m_astrsWords.RemoveAt(0);

			if (m_astrsWords.Count == 0)
			{
				if (++m_nLineIndex < m_astrLines.Count)
					ParseLine(m_astrLines[m_nLineIndex]);
				else
					m_nLineIndex--;
			}

			UpdateText();
		}

		protected override void OnMouseClick(System.Windows.Forms.MouseEventArgs e)
		{
			NextWord();
		}
	}
}
