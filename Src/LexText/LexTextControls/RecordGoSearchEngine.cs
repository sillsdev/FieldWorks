using System;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// This is the search engine for RecordGoDlg.
	/// </summary>
	internal class RecordGoSearchEngine : SearchEngine
	{
		public RecordGoSearchEngine(FdoCache cache)
			: base(cache, SearchType.FullText)
		{
		}

		protected override IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj)
		{
			var rec = (IRnGenericRec) obj;
			switch (field.Flid)
			{
				case RnGenericRecTags.kflidTitle:
					var title = rec.Title;
					if (title != null && title.Length > 0)
						yield return title;
					break;

				default:
					throw new ArgumentException("Unrecognized field.", "field");
			}
		}

		protected override IList<ICmObject> GetSearchableObjects()
		{
			return Cache.ServiceLocator.GetInstance<IRnGenericRecRepository>().AllInstances().Cast<ICmObject>().ToArray();
		}

		protected override bool IsIndexResetRequired(int hvo, int flid)
		{
			switch (flid)
			{
				case RnResearchNbkTags.kflidRecords:
				case RnGenericRecTags.kflidSubRecords:
				case RnGenericRecTags.kflidTitle:
					return true;
			}

			return false;
		}

		protected override bool IsFieldMultiString(SearchField field)
		{
			switch (field.Flid)
			{
				case RnGenericRecTags.kflidTitle:
					return false;
			}

			throw new ArgumentException("Unrecognized field.", "field");
		}
	}
}
