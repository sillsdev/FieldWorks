using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpViewsDemo
{
	/// <summary>
	/// Demonstrates a class that can own a sequence of paragraphs
	/// </summary>
	class ParagraphOwnerDemo
	{
		public event EventHandler<EventArgs> ParagraphsChanged;
		private List<ParagraphDemo> m_paragraphs = new List<ParagraphDemo>();
		public List<ParagraphDemo> Paragraphs
		{
			get { return m_paragraphs; }
		}

		public void InsertParagraph(int index, ParagraphDemo item)
		{
			m_paragraphs.Insert(index, item);
			RaiseParagraphsChanged();
		}

		private void RaiseParagraphsChanged()
		{
		   if (ParagraphsChanged != null)
			   ParagraphsChanged(this, new EventArgs());
		}
	}
}
