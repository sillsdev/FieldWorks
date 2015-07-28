// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SIL.FieldWorks.SharpViews.Builders
{
	public class MultiAccessor : IViewMultiString
	{
		public int VernWs;
		public int AnalysisWs;

		public MultiAccessor(int vws, int aws)
		{
			VernWs = vws;
			AnalysisWs = aws;
		}

		private Dictionary<int, ITsString> m_values = new Dictionary<int, ITsString>();

		#region IMultiAccessorBase Members

		public ITsString AnalysisDefaultWritingSystem
		{
			get
			{
				return m_values[AnalysisWs];
			}
			set
			{
				set_String(AnalysisWs, value);
			}
		}

		public void SetAnalysisDefaultWritingSystem(string val)
		{
			AnalysisDefaultWritingSystem = TsStrFactoryClass.Create().MakeString(val, AnalysisWs);
		}

		public void SetVernacularDefaultWritingSystem(string val)
		{
			VernacularDefaultWritingSystem = TsStrFactoryClass.Create().MakeString(val, VernWs);
		}

		public ITsString VernacularDefaultWritingSystem
		{
			get
			{
				return m_values[VernWs];
			}
			set
			{
				set_String(VernWs, value);
			}
		}

		public ITsString get_String(int ws)
		{
			return m_values[ws];
		}

		public void set_String(int ws, ITsString tss)
		{
			m_values[ws] = tss;
			if (StringChanged != null)
				StringChanged(this, new MlsChangedEventArgs(ws));
		}

		public event EventHandler<MlsChangedEventArgs> StringChanged;

		#endregion
	}
}
