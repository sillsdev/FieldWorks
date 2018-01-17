// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	internal class ConcMdc : LcmMetaDataCacheDecoratorBase
	{
		public ConcMdc(IFwMetaDataCacheManaged metaDataCache)
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
				case WfiWordformTags.kClassId:
				{
					switch (bstrFieldName)
					{
						case "ExactOccurrences":
							return ConcDecorator.kflidWfExactOccurrences;
						case "Occurrences":
							return ConcDecorator.kflidWfOccurrences;
						case "ConcOccurrences":
							return ConcDecorator.kflidConcOccurrences;
						case "OccurrenceCount":
							return ConcDecorator.kflidOccurrenceCount;
					}
				}
					break;
				case WfiAnalysisTags.kClassId:
				{
					switch (bstrFieldName)
					{
						case "ExactOccurrences":
							return ConcDecorator.kflidWaExactOccurrences;
						case "Occurrences":
							return ConcDecorator.kflidWaOccurrences;
					}
				}
					break;
				case WfiGlossTags.kClassId:
				{
					switch (bstrFieldName)
					{
						case "ExactOccurrences":
							return ConcDecorator.kflidWgExactOccurrences;
						case "Occurrences":
							return ConcDecorator.kflidWgOccurrences;
					}
				}
					break;
				case LexSenseTags.kClassId:
				{
					switch (bstrFieldName)
					{
						case "Occurrences":
							return ConcDecorator.kflidSenseOccurrences;
					}
				}
					break;
				case LangProjectTags.kClassId:
				{
					switch (bstrFieldName)
					{
						case "ConcOccurrences":
							return ConcDecorator.kflidConcOccurrences;
					}
				}
					break;
				case ConcDecorator.kclidFakeOccurrence:
					switch (bstrFieldName)
					{
						case "Reference":
							return ConcDecorator.kflidReference;
						case "BeginOffset":
							return ConcDecorator.kflidBeginOffset;
						case "EndOffset":
							return ConcDecorator.kflidEndOffset;
						case "TextObject":
							return ConcDecorator.kflidTextObject;
						case "Paragraph":
							return ConcDecorator.kflidParagraph;
						case "Segment":
							return ConcDecorator.kflidSegment;
						case "Analysis":
							return ConcDecorator.kflidAnalysis;
						case "TextTitle":
							return ConcDecorator.kflidTextTitle;
						case "TextGenres":
							return ConcDecorator.kflidTextGenres;
						case "TextIsTranslation":
							return ConcDecorator.kflidTextIsTranslation;
						case "TextSource":
							return ConcDecorator.kflidTextSource;
						case "TextComment":
							return ConcDecorator.kflidTextComment;
					}
					break;
			}
			return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
		}

		public override int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
		{
			switch (bstrClassName)
			{
				case "FakeOccurrence":
					switch (bstrFieldName)
					{
						case "Reference":
							return ConcDecorator.kflidReference;
						case "BeginOffset":
							return ConcDecorator.kflidBeginOffset;
						case "EndOffset":
							return ConcDecorator.kflidEndOffset;
						case "TextObject":
							return ConcDecorator.kflidTextObject;
						case "Paragraph":
							return ConcDecorator.kflidParagraph;
						case "Segment":
							return ConcDecorator.kflidSegment;
						case "TextTitle":
							return ConcDecorator.kflidTextTitle;
						case "TextGenres":
							return ConcDecorator.kflidTextGenres;
						case "TextIsTranslation":
							return ConcDecorator.kflidTextIsTranslation;
						case "TextSource":
							return ConcDecorator.kflidTextSource;
						case "TextComment":
							return ConcDecorator.kflidTextComment;
					}
					break;
				case "WfiWordform":
					return GetFieldId2(WfiWordformTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "LexSense":
					return GetFieldId2(LexSenseTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "LangProject":
					return GetFieldId2(LangProjectTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "WfiAnalysis":
					return GetFieldId2(WfiAnalysisTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "WfiGloss":
					return GetFieldId2(WfiGlossTags.kClassId, bstrFieldName, fIncludeBaseClasses);
			}

			return base.GetFieldId(bstrClassName, bstrFieldName, fIncludeBaseClasses);
		}

		public override string GetOwnClsName(int flid)
		{
			switch (flid)
			{
				case ConcDecorator.kflidWfExactOccurrences: // Fall through
				case ConcDecorator.kflidWfOccurrences: return "WfiWordform";
				case ConcDecorator.kflidWaExactOccurrences: // Fall through
				case ConcDecorator.kflidWaOccurrences: return "WfiAnalysis";
				case ConcDecorator.kflidWgExactOccurrences: // Fall through
				case ConcDecorator.kflidWgOccurrences: return "WfiGloss";
				case ConcDecorator.kflidSenseOccurrences: return "LexSense";
				case ConcDecorator.kflidConcOccurrences: return "LangProject";
				case ConcDecorator.kflidTextSource: return "FakeOccurrence";
				case ConcDecorator.kflidTextTitle: return "FakeOccurrence";
				case ConcDecorator.kflidTextComment: return "FakeOccurrence";
				//case ConcDecorator.kflidOccurrenceCount: return "WfiWordform";
				//case ConcDecorator.kflidReference: return "FakeOccurrence";
				//case ConcDecorator.kflidBeginOffset: return "FakeOccurrence";
				//case ConcDecorator.kflidEndOffset: return "FakeOccurrence";
				// And several other FakeOccurrence properties.
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
				case ConcDecorator.kflidWfExactOccurrences: // Fall through.
				case ConcDecorator.kflidWfOccurrences:
					return 0;
				case ConcDecorator.kflidTextObject:
					return CmObjectTags.kClassId;
				case ConcDecorator.kflidParagraph:
					return StTxtParaTags.kClassId;
				case ConcDecorator.kflidOccurrenceCount:
					return 0;
				case ConcDecorator.kflidReference:
					return 0; // 'Reference' of an occurrence.
				case ConcDecorator.kflidBeginOffset:
					return 0; // 'BeginOffset' of an occurrence.
				case ConcDecorator.kflidEndOffset:
					return 0; // 'EndOffset' of an occurrence.
				case ConcDecorator.kflidSenseOccurrences:
					return 0; // top-level property for occurrences of a sense.
				case ConcDecorator.kflidSegment:
					return 030; // segment from occurrence.
				case ConcDecorator.kflidConcOccurrences:
					return 0; // occurrences in Concordance view, supposedly of LangProject.
				case ConcDecorator.kflidAnalysis:
					return 032; // from fake concordance object to Analysis.
				case ConcDecorator.kflidWaExactOccurrences: // Fall through.
				case ConcDecorator.kflidWaOccurrences:
					return 0; // occurrences of a WfiAnalysis
				case ConcDecorator.kflidWgExactOccurrences: // Fall through.
				case ConcDecorator.kflidWgOccurrences:
					return 0; // occurrences of a WfiGloss.
				case ConcDecorator.kflidTextTitle:
					return 0; // of a FakeOccurrence
				case ConcDecorator.kflidTextGenres:
					return 0; // of a FakeOccurrence
				case ConcDecorator.kflidTextIsTranslation:
					return 0; // of a FakeOccurrence
				case ConcDecorator.kflidTextSource:
					return 0; // of a FakeOccurrence
				case ConcDecorator.kflidTextComment:
					return 0; // of a FakeOccurrence
				case ConcDecorator.kclidFakeOccurrence:
					return 0;
			}
			return base.GetDstClsId(flid);
		}

		public override string GetClassName(int clid)
		{
			return clid == ConcDecorator.kclidFakeOccurrence ? "FakeOccurrence" : base.GetClassName(clid);
		}

		public override string GetFieldName(int flid)
		{
			switch (flid)
			{
				case ConcDecorator.kflidWfExactOccurrences: return "ExactOccurrences";
				case ConcDecorator.kflidWaExactOccurrences: return "ExactOccurrences";
				case ConcDecorator.kflidWgExactOccurrences: return "ExactOccurrences";
				case ConcDecorator.kflidWfOccurrences: return "Occurrences";
				case ConcDecorator.kflidWaOccurrences: return "Occurrences";
				case ConcDecorator.kflidWgOccurrences: return "Occurrences";
				case ConcDecorator.kflidSenseOccurrences: return "Occurrences";
				case ConcDecorator.kflidConcOccurrences: return "ConcOccurrences";
				//case ConcDecorator.kflidOccurrenceCount: return "OccurrenceCount";
				//case ConcDecorator.kflidReference: return "Reference";
				//case ConcDecorator.kflidBeginOffset: return "BeginOffset";
				//case ConcDecorator.kflidEndOffset: return "EndOffset";
				// and several other FakeObject properties
			}
			return base.GetFieldName(flid);
		}

		public override int GetFieldType(int flid)
		{
			switch (flid)
			{
				case ConcDecorator.kflidWfExactOccurrences:
				case ConcDecorator.kflidWfOccurrences:
				case ConcDecorator.kflidWaExactOccurrences:
				case ConcDecorator.kflidWaOccurrences:
				case ConcDecorator.kflidWgExactOccurrences:
				case ConcDecorator.kflidWgOccurrences:
				case ConcDecorator.kflidConcOccurrences:
				case ConcDecorator.kflidSenseOccurrences:
					return (int)CellarPropertyType.ReferenceSequence;
				case ConcDecorator.kflidOccurrenceCount:
					return (int)CellarPropertyType.Integer;
				case ConcDecorator.kflidReference:
					return (int)CellarPropertyType.String;
				case ConcDecorator.kflidBeginOffset:
					return (int)CellarPropertyType.Integer;
				case ConcDecorator.kflidEndOffset:
					return (int)CellarPropertyType.Integer;
				case ConcDecorator.kflidTextTitle:
					return (int)CellarPropertyType.MultiString;
				case ConcDecorator.kflidTextGenres:
					return (int)CellarPropertyType.ReferenceSequence;
				case ConcDecorator.kflidTextIsTranslation:
					return (int)CellarPropertyType.Boolean;
				case ConcDecorator.kflidTextSource:
					return (int)CellarPropertyType.MultiString;
				case ConcDecorator.kflidTextComment:
					return (int)CellarPropertyType.MultiString;
			}
			return base.GetFieldType(flid);
		}
	}
}