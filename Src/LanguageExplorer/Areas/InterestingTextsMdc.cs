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
			switch (luClid)
			{
				case LangProjectTags.kClassId:
					{
						switch (bstrFieldName)
						{
							case AreaServices.InterestingTexts:
								return InterestingTextsDecorator.kflidInterestingTexts;
						}
					}
					break;
			}
			return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
		}

		public override int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
		{
			switch (bstrClassName)
			{
				case "LangProject":
					return GetFieldId2(LangProjectTags.kClassId, bstrFieldName, fIncludeBaseClasses);
			}

			return base.GetFieldId(bstrClassName, bstrFieldName, fIncludeBaseClasses);
		}

		public override string GetOwnClsName(int flid)
		{
			switch (flid)
			{
				case InterestingTextsDecorator.kflidInterestingTexts: return "LangProject";
			}
			return base.GetOwnClsName(flid);
		}

		public override int GetDstClsId(int flid)
		{
			switch (flid)
			{
				case InterestingTextsDecorator.kflidInterestingTexts:
					return StTextTags.kClassId;
			}
			return base.GetDstClsId(flid);
		}

		public override string GetFieldName(int flid)
		{
			switch (flid)
			{
				case InterestingTextsDecorator.kflidInterestingTexts: return AreaServices.InterestingTexts;
			}
			return base.GetFieldName(flid);
		}

		public override int GetFieldType(int flid)
		{
			switch (flid)
			{
				case InterestingTextsDecorator.kflidInterestingTexts:
					return (int)CellarPropertyType.ReferenceSequence;
			}
			return base.GetFieldType(flid);
		}
	}
}