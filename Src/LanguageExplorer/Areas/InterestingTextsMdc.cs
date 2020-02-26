// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	public class InterestingTextsMdc : LcmMetaDataCacheDecoratorBase
	{
		public InterestingTextsMdc(IFwMetaDataCacheManaged metaDataCache)
			: base(metaDataCache)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotSupportedException();
		}

		// Not sure which of these we need, do both.
		public override int GetFieldId2(int luClid, string bstrFieldName, bool fIncludeBaseClasses)
		{
			return luClid == LangProjectTags.kClassId && bstrFieldName == AreaServices.InterestingTexts ? InterestingTextsDecorator.kflidInterestingTexts : base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
		}

		public override int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
		{
			return bstrClassName == "LangProject" ? GetFieldId2(LangProjectTags.kClassId, bstrFieldName, fIncludeBaseClasses) : base.GetFieldId(bstrClassName, bstrFieldName, fIncludeBaseClasses);
		}

		public override string GetOwnClsName(int flid)
		{
			return flid == InterestingTextsDecorator.kflidInterestingTexts ? "LangProject" : base.GetOwnClsName(flid);
		}

		public override int GetDstClsId(int flid)
		{
			return flid == InterestingTextsDecorator.kflidInterestingTexts ? StTextTags.kClassId : base.GetDstClsId(flid);
		}

		public override string GetFieldName(int flid)
		{
			return flid == InterestingTextsDecorator.kflidInterestingTexts ? AreaServices.InterestingTexts : base.GetFieldName(flid);
		}

		public override int GetFieldType(int flid)
		{
			return flid == InterestingTextsDecorator.kflidInterestingTexts ? (int)CellarPropertyType.ReferenceSequence : base.GetFieldType(flid);
		}
	}
}