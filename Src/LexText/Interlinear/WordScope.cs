using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using XCore;

namespace SIL.FieldWorks.IText
{
	public delegate void AdvanceWordEventHandler(object sender, AdvanceWordArgs e);

	public class AdvanceWordArgs : EventArgs
	{
		int m_hvoAnnotation;
		int m_hvoAnalysis;
		public AdvanceWordArgs(int hvoAnnotation, int hvoAnalysis)
		{
			m_hvoAnnotation = hvoAnnotation;
			m_hvoAnalysis = hvoAnalysis;
		}
		public int Annotation
		{
			get { return m_hvoAnnotation; }
			set { m_hvoAnnotation = value; }
		}

		public int Analysis
		{
			get { return m_hvoAnalysis; }
			set { m_hvoAnalysis = value; }
		}
	}
}
