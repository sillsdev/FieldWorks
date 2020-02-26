// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.XMLViews
{
	internal class XmlViewsMetaDataCacheDecorator : LcmMetaDataCacheDecoratorBase
	{
		public XmlViewsMetaDataCacheDecorator(IFwMetaDataCacheManaged metaDataCache) : base(metaDataCache)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotSupportedException();
		}

		// So far, this is the only query that needs to know about the virtual props.
		// It may not even need to know about all of these.
		public override string GetFieldName(int flid)
		{
			switch (flid)
			{
				case XMLViewsDataCache.ktagTagMe: return "Me";
				case XMLViewsDataCache.ktagActiveColumn: return "ActiveColumn";
				case XMLViewsDataCache.ktagAlternateValue: return "AlternateValue";
				case XMLViewsDataCache.ktagItemEnabled: return "ItemEnabled";
				case XMLViewsDataCache.ktagItemSelected: return "ItemSelected";
			}
			// Paste operations currently require the column to have some name.
			return flid >= XMLViewsDataCache.ktagEditColumnBase && flid < XMLViewsDataCache.ktagEditColumnLim
				? "RdeColumn" + (flid - XMLViewsDataCache.ktagEditColumnBase)
				: flid >= XMLViewsDataCache.ktagAlternateValueMultiBase && flid < XMLViewsDataCache.ktagAlternateValueMultiBaseLim ? "PhonFeatColumn" + (flid - XMLViewsDataCache.ktagAlternateValueMultiBase) : base.GetFieldName(flid);
		}

		public override int GetFieldType(int luFlid)
		{
			// This is a bit arbitrary. Technically, the form column isn't formattable, while the one shadowing
			// Definition could be. But pretending all are Unicode just means Collect Words can't do formatting
			// of definitions, while allowing it in the Form could lead to crashes when we copy to the real field.
			return luFlid >= XMLViewsDataCache.ktagEditColumnBase && luFlid < XMLViewsDataCache.ktagEditColumnLim ? (int)CellarPropertyType.Unicode : base.GetFieldType(luFlid);
		}
	}
}