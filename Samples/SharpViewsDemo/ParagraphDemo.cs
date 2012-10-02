using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpViewsDemo
{
	/// <summary>
	/// This class represents an individual paragraph object.
	/// </summary>
	class ParagraphDemo
	{
		private string m_contents = "";
		public event EventHandler<EventArgs> ContentsChanged;
		public string Contents
		{
			get { return m_contents; }
			set
			{
				m_contents = value;
				if (ContentsChanged != null)
					ContentsChanged(this, new EventArgs());
			}
		}
	}
}
