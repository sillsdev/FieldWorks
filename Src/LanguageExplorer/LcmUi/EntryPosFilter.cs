// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// A special filter, where items are LexEntries, and matches are ones where an MSA is an MoStemMsa that
	/// has the correct POS. (not used yet.
	/// </summary>
	internal class EntryPosFilter : ListChoiceFilter
	{
		/// <summary>
		/// Default constructor for persistence.
		/// </summary>
		public EntryPosFilter() { }
		internal EntryPosFilter(LcmCache cache, ListMatchOptions mode, int[] targets)
			: base(cache, mode, targets)
		{
		}

		protected override string BeSpec
		{
			get { return "external"; }
		}

		public override bool CompatibleFilter(XElement colSpec)
		{
			if (!base.CompatibleFilter(colSpec))
				return false;
			return DynamicLoader.TypeForLoaderNode(colSpec) == this.GetType();
		}

		const int kflidMsas = LexEntryTags.kflidMorphoSyntaxAnalyses;
		const int kflidEntrySenses = LexEntryTags.kflidSenses;

		/// <summary>
		/// Get the items to be compared against the filter.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		protected override int[] GetItems(IManyOnePathSortItem item)
		{
			ISilDataAccess sda = m_cache.DomainDataByFlid;
			List<int> results = new List<int>();
			if (item.PathLength > 0 && item.PathFlid(0) == kflidMsas)
			{
				// sorted by MSA, match just the one MSA.
				// I don't think this path can occur with the current XML spec where this is used.
				int hvoMsa;
				if (item.PathLength > 1)
					hvoMsa = item.PathObject(1);
				else
					hvoMsa = item.KeyObject;
				GetItemsForMsaType(sda, ref results, hvoMsa);
			}
			else if (item.PathLength >= 1 && item.PathFlid(0) == kflidEntrySenses)
			{
				// sorted in a way that shows one sense per row, test that sense's MSA.
				int hvoSense;
				if (item.PathLength > 1)
					hvoSense = item.PathObject(1);
				else
					hvoSense = item.KeyObject;
				int hvoMsa = sda.get_ObjectProp(hvoSense, LexSenseTags.kflidMorphoSyntaxAnalysis);
				GetItemsForMsaType(sda, ref results, hvoMsa);
			}
			else
			{
				int hvoEntry = item.RootObjectHvo;
				int cmsa = sda.get_VecSize(hvoEntry, kflidMsas);
				for (int imsa = 0; imsa < cmsa; imsa++)
				{
					int hvoMsa = sda.get_VecItem(hvoEntry, kflidMsas, imsa);
					GetItemsForMsaType(sda, ref results, hvoMsa);
				}
			}
			return results.ToArray();
		}

		private void GetItemsForMsaType(ISilDataAccess sda, ref List<int> results, int hvoMsa)
		{
			if (hvoMsa == 0)
				return;
			int kclsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoMsa).ClassID;
			switch (kclsid)
			{
				case MoStemMsaTags.kClassId:
					AddHvoPOStoResults(sda, results, hvoMsa, MoStemMsaTags.kflidPartOfSpeech);
					break;
				case MoInflAffMsaTags.kClassId:
					AddHvoPOStoResults(sda, results, hvoMsa, MoInflAffMsaTags.kflidPartOfSpeech);
					break;
				case MoDerivAffMsaTags.kClassId:
					AddHvoPOStoResults(sda, results, hvoMsa, MoDerivAffMsaTags.kflidFromPartOfSpeech);
					AddHvoPOStoResults(sda, results, hvoMsa, MoDerivAffMsaTags.kflidToPartOfSpeech);
					break;
				case MoUnclassifiedAffixMsaTags.kClassId:
					AddHvoPOStoResults(sda, results, hvoMsa, MoUnclassifiedAffixMsaTags.kflidPartOfSpeech);
					break;
			}
		}

		private static void AddHvoPOStoResults(ISilDataAccess sda, List<int> results, int hvoMsa, int flidPos)
		{
			int hvoPOS;
			hvoPOS = sda.get_ObjectProp(hvoMsa, flidPos);
			if (hvoPOS != 0)
				results.Add(hvoPOS);
		}

		/// <summary>
		/// Return the HVO of the list from which choices can be made.
		/// </summary>
		static public int List(LcmCache cache)
		{
			return cache.LanguageProject.PartsOfSpeechOA.Hvo;
		}
	}
}