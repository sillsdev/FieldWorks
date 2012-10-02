using System;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Utilities;

namespace SharpViewsDemo
{
	/// <summary>
	/// Demonstrates a class that can own a sequence of paragraphs
	/// </summary>
	class ParagraphOwnerDemo
	{
		public event EventHandler<ObjectSequenceEventArgs> ParagraphsChanged;
		private ModifiedMonitoredList<ParagraphDemo> m_paragraphs = new ModifiedMonitoredList<ParagraphDemo>();

		public ParagraphOwnerDemo()
		{
			m_paragraphs.Changed += RaiseParagraphsChanged;
		}
		public ModifiedMonitoredList<ParagraphDemo> Paragraphs
		{
			get { return m_paragraphs; }
		}

		public void InsertParagraph(int index, ParagraphDemo item)
		{
			m_paragraphs.Insert(index, item);
		}

		private void RaiseParagraphsChanged(object obj, ObjectSequenceEventArgs args)
		{
		   if (ParagraphsChanged != null)
			   ParagraphsChanged(this, args);
		}
	}
}
