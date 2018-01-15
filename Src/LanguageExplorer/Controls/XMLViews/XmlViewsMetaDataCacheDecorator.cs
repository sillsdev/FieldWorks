// Copyright (c) 2015-2018 SIL International
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
			throw new NotImplementedException();
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
			if (flid >= XMLViewsDataCache.ktagEditColumnBase && flid < XMLViewsDataCache.ktagEditColumnLim)
			{
				return "RdeColumn" + (flid - XMLViewsDataCache.ktagEditColumnBase);
			}
			if (flid >= XMLViewsDataCache.ktagAlternateValueMultiBase && flid < XMLViewsDataCache.ktagAlternateValueMultiBaseLim)
			{
				return "PhonFeatColumn" + (flid - XMLViewsDataCache.ktagAlternateValueMultiBase);
			}
			return base.GetFieldName(flid);
		}

		public override int GetFieldType(int luFlid)
		{
			// This is a bit arbitrary. Technically, the form column isn't formattable, while the one shadowing
			// Definition could be. But pretending all are Unicode just means Collect Words can't do formatting
			// of definitions, while allowing it in the Form could lead to crashes when we copy to the real field.
			if (luFlid >= XMLViewsDataCache.ktagEditColumnBase && luFlid < XMLViewsDataCache.ktagEditColumnLim)
			{
				return (int)CellarPropertyType.Unicode;
			}
			return base.GetFieldType(luFlid);
		}
	}
}