// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This is the search engine for WordformGoDlg.
	/// </summary>
	internal class WordformGoSearchEngine : SearchEngine
	{
		private readonly Virtuals m_virtuals;

		public WordformGoSearchEngine(FdoCache cache)
			: base(cache, SearchType.Prefix)
		{
			m_virtuals = Cache.ServiceLocator.GetInstance<Virtuals>();
		}

		protected override IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj)
		{
			var wf = (IWfiWordform) obj;

			int ws = field.String.get_WritingSystemAt(0);
			switch (field.Flid)
			{
				case WfiWordformTags.kflidForm:
					var form = wf.Form.StringOrNull(ws);
					if (form != null && form.Length > 0)
						yield return form;
					break;

				default:
					throw new ArgumentException("Unrecognized field.", "field");
			}
		}

		protected override IList<ICmObject> GetSearchableObjects()
		{
			return Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances().Cast<ICmObject>().ToArray();
		}

		protected override bool IsIndexResetRequired(int hvo, int flid)
		{
			if (flid == m_virtuals.LangProjectAllWordforms)
				return true;

			switch (flid)
			{
				case WfiWordformTags.kflidForm:
					return true;
			}

			return false;
		}

		protected override bool IsFieldMultiString(SearchField field)
		{
			switch (field.Flid)
			{
				case WfiWordformTags.kflidForm:
					return true;
			}

			throw new ArgumentException("Unrecognized field.", "field");
		}
	}
}
