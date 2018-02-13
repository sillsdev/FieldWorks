// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// The ghost slice displays just one string; this decorator stores and returns it independent of
	/// the flid.
	/// </summary>
	internal class GhostDaDecorator : DomainDataByFlidDecoratorBase
	{
		private ITsString m_tss;
		private int m_clidDst;

		public GhostDaDecorator(ISilDataAccessManaged domainDataByFlid, ITsString tss, int clidDst)
			: base(domainDataByFlid)
		{
			m_tss = tss;
			m_clidDst = clidDst;
			SetOverrideMdc(new GhostDataCacheDecorator((IFwMetaDataCacheManaged)MetaDataCache));
		}

		public override ITsString get_StringProp(int hvo, int tag)
		{
			Debug.Assert(hvo == GhostStringSlice.khvoFake);
			Debug.Assert(tag == GhostStringSlice.kflidFake);
			return m_tss;
		}

		public override void SetString(int hvo, int tag, ITsString tss)
		{
			Debug.Assert(hvo == GhostStringSlice.khvoFake);
			Debug.Assert(tag == GhostStringSlice.kflidFake);
			m_tss = tss;
		}

		// Pretend it is of our expected destination class. Very few things should care about this,
		// but it allows IsValidObject to return true for it, which is important when reconstructing
		// the root box, as happens during Refresh.
		public override int get_IntProp(int hvo, int tag)
		{
			return tag == (int)CmObjectFields.kflidCmObject_Class ? m_clidDst : base.get_IntProp(hvo, tag);
		}
	}
}