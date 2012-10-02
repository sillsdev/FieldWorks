using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SharpViewsDemo
{
	/// <summary>
	/// This class represents an individual paragraph object.
	/// </summary>
	class ParagraphDemo
	{
		private string m_contents = "";
		private ITsString m_tsContents;
		private IViewMultiString m_mlsContents;
		public event EventHandler<EventArgs> ContentsChanged;
		public event EventHandler<EventArgs> TsContentsChanged;
		public event EventHandler<EventArgs> MlsContentsChanged;

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

		public ITsString TsContents
		{
			get { return m_tsContents; }
			set
			{
				m_tsContents = value;
				if (TsContentsChanged != null)
					TsContentsChanged(this, new EventArgs());
			}
		}

		public IViewMultiString MlsContents
		{
			get { return m_mlsContents; }
			set
			{
				m_mlsContents = value;
				if (MlsContentsChanged != null)
					MlsContentsChanged(this, new EventArgs());
			}
		}

		public string ParaStyle { get; set; }
		// Todo: backing variable, ParaStyleChanged event
	}
}
