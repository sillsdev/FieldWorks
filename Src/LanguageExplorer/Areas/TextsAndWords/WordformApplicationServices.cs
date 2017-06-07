// Copyright (c) 2005-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// Static application level service class for wordforms.
	/// </summary>
	internal static class WordformApplicationServices
	{
		/// <summary>
		/// Find an extant wordform,
		/// or create one (with nonundoable UOW), if one does not exist.
		/// </summary>
		internal static IWfiWordform GetWordformForForm(FdoCache cache, ITsString form)
		{
			var servLoc = cache.ServiceLocator;
			var wordformRepos = servLoc.GetInstance<IWfiWordformRepository>();
			IWfiWordform retval;
			if (!wordformRepos.TryGetObject(form, false, out retval))
			{
				// Have to make it.
				var wordformFactory = servLoc.GetInstance<IWfiWordformFactory>();
				NonUndoableUnitOfWorkHelper.Do(servLoc.GetInstance<IActionHandler>(), () =>
				{
					retval = wordformFactory.Create(form);
				});
			}
			return retval;
		}
	}
}