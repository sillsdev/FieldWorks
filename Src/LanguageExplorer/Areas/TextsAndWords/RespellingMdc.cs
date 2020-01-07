// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords
{
	internal class RespellingMdc : ConcMdc
	{
		public RespellingMdc(IFwMetaDataCacheManaged metaDataCache)
			: base(metaDataCache)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotSupportedException();
		}

		public override int GetFieldId2(int clid, string sFieldName, bool fIncludeBaseClasses)
		{
			switch (clid)
			{
				case ConcDecorator.kclidFakeOccurrence:
					switch (sFieldName)
					{
						case "AdjustedBeginOffset":
							return RespellingSda.kflidAdjustedBeginOffset;
						case "AdjustedEndOffset":
							return RespellingSda.kflidAdjustedEndOffset;
						case "SpellingPreview":
							return RespellingSda.kflidSpellingPreview;
					}
					break;
				case WfiWordformTags.kClassId:
					if (sFieldName == "OccurrencesInCaptions")
					{
						return RespellingSda.kflidOccurrencesInCaptions;
					}
					break;
			}
			return base.GetFieldId2(clid, sFieldName, fIncludeBaseClasses);
		}

		public override int GetFieldId(string sClassName, string sFieldName, bool fIncludeBaseClasses)
		{
			switch (sClassName)
			{
				case "FakeOccurrence":
					switch (sFieldName)
					{
						case "AdjustedBeginOffset":
							return RespellingSda.kflidAdjustedBeginOffset;
						case "AdjustedEndOffset":
							return RespellingSda.kflidAdjustedEndOffset;
						case "SpellingPreview":
							return RespellingSda.kflidSpellingPreview;
					}
					break;
				case "WfiWordform":
					if (sFieldName == "OccurrencesInCaptions")
					{
						return RespellingSda.kflidOccurrencesInCaptions;
					}
					break;
			}
			return base.GetFieldId(sClassName, sFieldName, fIncludeBaseClasses);
		}

		public override string GetOwnClsName(int flid)
		{
			switch (flid)
			{
				case RespellingSda.kflidAdjustedBeginOffset:
				case RespellingSda.kflidAdjustedEndOffset:
				case RespellingSda.kflidSpellingPreview:
					return "FakeOccurrence";
				case RespellingSda.kflidOccurrencesInCaptions:
					return "WfiWordform";
			}
			return base.GetOwnClsName(flid);
		}

		/// <summary>
		/// The record list currently ignores properties with signature 0, so doesn't do more with them.
		/// </summary>
		public override int GetDstClsId(int flid)
		{
			switch (flid)
			{
				case RespellingSda.kflidAdjustedBeginOffset:
				case RespellingSda.kflidAdjustedEndOffset:
				case RespellingSda.kflidSpellingPreview:
					return 0;
				case RespellingSda.kflidOccurrencesInCaptions:
					return ConcDecorator.kclidFakeOccurrence;
			}
			return base.GetDstClsId(flid);
		}

		public override string GetFieldName(int flid)
		{
			switch (flid)
			{
				case RespellingSda.kflidAdjustedBeginOffset:
					return "AdjustedBeginOffset";
				case RespellingSda.kflidAdjustedEndOffset:
					return "AdjustedEndOffset";
				case RespellingSda.kflidSpellingPreview:
					return "SpellingPreview";
				case RespellingSda.kflidOccurrencesInCaptions:
					return "OccurrencesInCaptions";
			}
			return base.GetFieldName(flid);
		}

		public override int GetFieldType(int flid)
		{
			switch (flid)
			{
				case RespellingSda.kflidOccurrencesInCaptions:
					return (int)CellarPropertyType.ReferenceSequence;
				case RespellingSda.kflidAdjustedBeginOffset:
				case RespellingSda.kflidAdjustedEndOffset:
					return (int)CellarPropertyType.Integer;
				case RespellingSda.kflidSpellingPreview:
					return (int)CellarPropertyType.String;
			}
			return base.GetFieldType(flid);
		}
	}
}