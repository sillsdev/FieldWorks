// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.CoreImpl.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace LexEdDllTests
{
	internal static class TestUtils
	{
		internal static void AddComplexFormComponents(FdoCache cache, ILexEntry entry, List<ICmObject> list, List<ILexEntryType> types = null)
		{
			UndoableUnitOfWorkHelper.Do("undo", "redo", cache.ActionHandlerAccessor, () =>
			{
				var dummy = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var ler = cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				dummy.EntryRefsOS.Add(ler);
				ler.RefType = LexEntryRefTags.krtComplexForm;
				foreach (var item in list)
				{
					ler.ComponentLexemesRS.Add(item);
					ler.PrimaryLexemesRS.Add(item);
				}
				// Change the owner to the real entry: this bypasses the check for circular references in FdoList.Add().
				entry.EntryRefsOS.Add(ler);
				dummy.Delete();

				if (types == null)
					return;
				foreach (var type in types)
					ler.ComplexEntryTypesRS.Add(type);
			});
		}

		internal static ILexEntry MakeEntry(FdoCache cache, string lf, string gloss)
		{
			ILexEntry entry = null;
			UndoableUnitOfWorkHelper.Do("undo", "redo", cache.ActionHandlerAccessor, () =>
			{
				entry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var form = cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				entry.LexemeFormOA = form;
				form.Form.VernacularDefaultWritingSystem =
					TsStringUtils.MakeString(lf, cache.DefaultVernWs);
				var sense = cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(gloss, cache.DefaultAnalWs);
			});
			return entry;
		}
	}
}
