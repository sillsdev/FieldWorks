// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class PossibilityAtomicReferenceViewSdaDecorator : DomainDataByFlidDecoratorBase
	{
		internal PossibilityAtomicReferenceViewSdaDecorator(ISilDataAccessManaged domainDataByFlid)
			: base(domainDataByFlid)
		{
			SetOverrideMdc(new PossibilityAtomicReferenceViewMetaDataCacheDecorator(domainDataByFlid.GetManagedMetaDataCache()));
		}

		public ITsString Tss { get; set; }

		public override ITsString get_StringProp(int hvo, int tag)
		{
			return tag == PossibilityAtomicReferenceView.kflidFake ? Tss : base.get_StringProp(hvo, tag);
		}

		public override void SetString(int hvo, int tag, ITsString tss)
		{
			if (tag == PossibilityAtomicReferenceView.kflidFake)
			{
				Tss = tss;
				SendPropChanged(hvo, tag, 0, 0, 0);
			}
			else
			{
				base.SetString(hvo, tag, tss);
			}
		}
	}
}