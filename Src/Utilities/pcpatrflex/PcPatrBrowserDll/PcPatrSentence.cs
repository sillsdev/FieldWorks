using System;
using System.Xml;

namespace SIL.PcPatrBrowser
{
	/// <summary>
	/// Summary description for PcPatrSentence.
	/// </summary>
	public class PcPatrSentence
	{
		protected XmlNode m_node;
		protected Array m_aParses;
		int m_iCurrentParse;
		public bool OutOfTimeFailure { get; set; }

		public PcPatrSentence()
		{
			OutOfTimeFailure = false;
		}

		public PcPatrSentence(XmlNode node)
		{
			m_node = node;

			AllocateArrayOfParses();

			SplitIntoParses();

			m_iCurrentParse = 0;
			string sent = this.ToString();
			string s2 = sent.ToLower();
			OutOfTimeFailure = false;
		}

		private void AllocateArrayOfParses()
		{
			XmlNode attr = m_node.SelectSingleNode("/Analysis/@count");
			int iCount = Convert.ToInt32(attr.InnerText);
			m_aParses = Array.CreateInstance(typeof(PcPatrParse), iCount);
		}

		private void SplitIntoParses()
		{
			int iIndex = 0;
			XmlNode parseNode = GetNextParseInSentence(m_node, iIndex);
			while (parseNode != null)
			{
				PcPatrParse parse = new PcPatrParse(parseNode);
				m_aParses.SetValue(parse, iIndex);
				iIndex++;
				parseNode = GetNextParseInSentence(m_node, iIndex);
			}
		}

		/// <summary>
		/// Get parses
		/// </summary>
		public Array Parses
		{
			get { return m_aParses; }
		}

		/// <summary>
		/// Get sentence based on its number
		/// </summary>
		/// <param name="iSentenceNumber">sentence number to get</param>
		/// <returns></returns>
		public PcPatrParse GoToParse(int iParseNumber)
		{
			if (iParseNumber <= 0)
				return FirstParse;
			if (iParseNumber >= NumberOfParses)
				return LastParse;
			m_iCurrentParse = iParseNumber - 1;
			return CurrentParse;
		}

		/// <summary>
		/// Get XML node
		/// </summary>
		public XmlNode Node
		{
			get { return m_node; }
		}

		/// <summary>
		/// Get currently selected parse in the document
		/// </summary>
		public PcPatrParse CurrentParse
		{
			get
			{
				if (m_aParses.Length > 0)
					return (PcPatrParse)m_aParses.GetValue(m_iCurrentParse);
				else
					return null;
			}
		}

		/// <summary>
		/// Get the number of the currently selected parse in the sentence
		/// </summary>
		public int CurrentParseNumber
		{
			get { return m_iCurrentParse + 1; }
		}

		/// <summary>
		/// Get number of parses in the sentence
		/// </summary>
		public int NumberOfParses
		{
			get
			{
				if (m_aParses != null)
					return m_aParses.Length;
				else
					return 0;
			}
		}

		/// <summary>
		/// Get the first parse in the document
		/// </summary>
		public PcPatrParse FirstParse
		{
			get
			{
				m_iCurrentParse = 0;
				return CurrentParse;
			}
		}

		/// <summary>
		/// Get the last parse in the document
		/// </summary>
		public PcPatrParse LastParse
		{
			get
			{
				m_iCurrentParse = m_aParses.Length - 1;
				return CurrentParse;
			}
		}

		/// <summary>
		/// Get the next parse in the document
		/// </summary>
		public PcPatrParse NextParse
		{
			get
			{
				m_iCurrentParse = Math.Min(m_iCurrentParse + 1, m_aParses.Length - 1);
				return CurrentParse;
			}
		}

		/// <summary>
		/// Get the next parse in the document
		/// </summary>
		public PcPatrParse PreviousParse
		{
			get
			{
				m_iCurrentParse = Math.Max(m_iCurrentParse - 1, 0);
				return CurrentParse;
			}
		}

		/// <summary>
		/// Obtain next possible parse element from an xml sentence
		/// </summary>
		/// <param name="sSentence"></param>
		/// <param name="iBegin"></param>
		/// <returns></returns>
		public XmlNode GetNextParseInSentence(XmlNode sentenceNode, int iIndex)
		{
			int i = iIndex + 1;
			XmlNode node = sentenceNode.SelectSingleNode("/Analysis/Parse[" + i.ToString() + "]");
			return node;
		}
	}
}
