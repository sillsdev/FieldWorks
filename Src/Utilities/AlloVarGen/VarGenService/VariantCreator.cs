// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.AlloGenModel;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.VarGenService
{
	public class VariantCreator
	{
		LcmCache Cache { get; set; }
		List<WritingSystem> WritingSystems { get; set; } = new List<WritingSystem>();
		ILexEntry variantEntry = null;

		public VariantCreator(LcmCache cache, List<WritingSystem> writingSystems)
		{
			Cache = cache;
			WritingSystems = writingSystems;
		}

		public ILexEntryRef CreateVariant(ILexEntry entry, List<string> forms)
		{
			variantEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			IMoStemAllomorph form = variantEntry.Services
				.GetInstance<IMoStemAllomorphFactory>()
				.Create();
			variantEntry.LexemeFormOA = form;
			variantEntry.LexemeFormOA.MorphTypeRA = entry.LexemeFormOA.MorphTypeRA;
			for (int i = 0; i < WritingSystems.Count && i < forms.Count; i++)
			{
				form.Form.set_String(WritingSystems[i].Handle, forms[i]);
			}
			ILexEntryRef entryRef = Cache.ServiceLocator
				.GetInstance<ILexEntryRefFactory>()
				.Create();
			variantEntry.EntryRefsOS.Add(entryRef);
			entryRef.ComponentLexemesRS.Add(entry);
			return entryRef;
		}

		public void AddVariantTypes(ILexEntryRef entryRef, List<AlloGenModel.VariantType> types)
		{
			foreach (AlloGenModel.VariantType variantType in types)
			{
				var varType = Cache.ServiceLocator.ObjectRepository.GetObjectOrIdWithHvoFromGuid(
					new Guid(variantType.Guid)
				);
				if (varType != null)
				{
					entryRef.VariantEntryTypesRS.Add((ILexEntryType)varType);
				}
			}
		}

		public void SetShowMinorEntry(ILexEntryRef entryRef, bool showMinorEntry)
		{
			if (entryRef != null)
			{
				entryRef.HideMinorEntry = showMinorEntry ? 0 : 1;
			}
		}

		public void AddPublishEntryInItems(List<AlloGenModel.PublishEntryInItem> pubInItems)
		{
			if (variantEntry == null)
			{
				return;
			}
			variantEntry.DoNotPublishInRC.Clear();
			ICmPossibilityList allPublishEntryInItems = Cache
				.LangProject
				.LexDbOA
				.PublicationTypesOA;
			foreach (ICmPossibility pubType in allPublishEntryInItems.PossibilitiesOS)
			{
				var item = pubInItems.FirstOrDefault(p => p.Guid == pubType.Guid.ToString());
				if (item == null)
				{
					variantEntry.DoNotPublishInRC.Add(pubType);
				}
			}
		}
	}
}
