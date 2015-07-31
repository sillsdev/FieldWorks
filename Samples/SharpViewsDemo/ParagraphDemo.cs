// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
