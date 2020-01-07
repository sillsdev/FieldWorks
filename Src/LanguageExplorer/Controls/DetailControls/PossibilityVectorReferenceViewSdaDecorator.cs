// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This class maintains a cache allowing possibility item display names to be looked up rather than computed after the
	/// first time they are used.
	/// </summary>
	internal class PossibilityVectorReferenceViewSdaDecorator : DomainDataByFlidDecoratorBase
	{
		private LcmCache Cache { get; }
		private string DisplayNameProperty { get; }
		private string DisplayWs { get; }
		private readonly Dictionary<int, ITsString> m_strings;
		/// <summary>
		/// The empty string displayed (hopefully temporarily) for any object we don't have a fake string for.
		/// </summary>
		public ITsString Empty;

		public PossibilityVectorReferenceViewSdaDecorator(ISilDataAccessManaged domainDataByFlid, LcmCache cache, string displayNameProperty, string displayWs)
			: base(domainDataByFlid)
		{
			SetOverrideMdc(new PossibilityVectorReferenceViewMetaDataCacheDecorator(domainDataByFlid.GetManagedMetaDataCache()));
			m_strings = new Dictionary<int, ITsString>();
			Cache = cache;
			DisplayNameProperty = displayNameProperty;
			DisplayWs = displayWs;
		}

		public IDictionary<int, ITsString> Strings => m_strings;

		public ITsString GetLabelFor(int hvo)
		{
			ITsString value;
			if (m_strings.TryGetValue(hvo, out value))
			{
				return value;
			}
			Debug.Assert(Cache != null);
			var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			Debug.Assert(obj != null);
			var label = ObjectLabel.CreateObjectLabel(Cache, obj, DisplayNameProperty, DisplayWs);
			var tss = label.AsTss ?? TsStringUtils.EmptyString(Cache.DefaultUserWs);
			Strings[hvo] = tss;
			return tss; // never return null!
		}

		public override ITsString get_StringProp(int hvo, int tag)
		{
			return tag == PossibilityVectorReferenceView.kflidFake ? GetLabelFor(hvo) : base.get_StringProp(hvo, tag);
		}

		public override void SetString(int hvo, int tag, ITsString tss)
		{
			if (tag == PossibilityVectorReferenceView.kflidFake)
			{
				m_strings[hvo] = tss;
				SendPropChanged(hvo, tag, 0, 0, 0);
			}
			else
			{
				base.SetString(hvo, tag, tss);
			}
		}
	}
}