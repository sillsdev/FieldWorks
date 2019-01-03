// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	internal class SandboxForTests : Sandbox
	{
		private InterlinDocForAnalysis m_mockInterlinDoc;

		internal SandboxForTests(ISharedEventHandlers sharedEventHandlers, LcmCache cache, InterlinLineChoices lineChoices)
			: base(sharedEventHandlers, cache, null, lineChoices)
		{
		}

		internal ISilDataAccess SandboxCacheDa => Caches.DataAccess;

#pragma warning disable 169
		ISilDataAccess MainCacheDa => Caches.MainCache.MainCacheAccessor;
#pragma warning restore 169

		internal ITsString GetTssInSandbox(int flid, int ws)
		{
			ITsString tss;
			switch (flid)
			{
				default:
					tss = null;
					break;
				case InterlinLineChoices.kflidWordGloss:
					tss = SandboxCacheDa.get_MultiStringAlt(kSbWord, ktagSbWordGloss, ws);
					break;
			}
			return tss;
		}

		internal int GetRealHvoInSandbox(int flid, int ws)
		{
			var hvo = 0;
			switch (flid)
			{
				default:
					break;
				case InterlinLineChoices.kflidWordPos:
					hvo = Caches.RealHvo(SandboxCacheDa.get_ObjectProp(kSbWord, ktagSbWordPos));
					break;
			}
			return hvo;
		}


		internal ITsString SetTssInSandbox(int flid, int ws, string str)
		{
			var tss = TsStringUtils.MakeString(str, ws);
			switch (flid)
			{
				default:
					tss = null;
					break;
				case InterlinLineChoices.kflidWordGloss:
					Caches.DataAccess.SetMultiStringAlt(kSbWord, ktagSbWordGloss, ws, tss);
					break;
			}
			return tss;
		}

		/// <summary />
		/// <returns>hvo of item in Items</returns>
		internal int SelectIndexInCombo(IPropertyTable propertyTable, int flid, int morphIndex, int index)
		{
			using (var handler = GetComboHandler(propertyTable, flid, morphIndex))
			{
				handler.HandleSelect(index);
				return handler.Items[handler.IndexOfCurrentItem];
			}
		}

		/// <summary />
		/// <returns>index of item</returns>
		internal int SelectItemInCombo(IPropertyTable propertyTable, int flid, int morphIndex, string comboItem)
		{
			using (var handler = GetComboHandler(propertyTable, flid, morphIndex))
			{
				handler.SelectComboItem(comboItem);
				return handler.IndexOfCurrentItem;
			}
		}

		/// <summary />
		/// <returns>index of item in combo</returns>
		internal int SelectItemInCombo(IPropertyTable propertyTable, int flid, int morphIndex, int hvoTarget)
		{
			using (var handler = GetComboHandler(propertyTable, flid, morphIndex))
			{
				handler.SelectComboItem(hvoTarget);
				return handler.IndexOfCurrentItem;
			}

		}


		internal override bool ShouldAddWordGlossToLexicon => true;

		internal AnalysisTree ConfirmAnalysis()
		{
			IWfiAnalysis obsoleteAna;
			return GetRealAnalysis(true, out obsoleteAna);
		}

		internal ILexSense GetLexSenseForWord()
		{
			var hvoSenses = LexSensesForCurrentMorphs();
			// the sense only represents the whole word if there is only one morph.
			return hvoSenses.Count != 1 ? null : Cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoSenses[0]);
		}

		private InterlinComboHandler GetComboHandler(IPropertyTable propertyTable, int flid, int morphIndex)
		{
			// first select the proper pull down icon.
			int tagIcon = 0;
			switch (flid)
			{
				default:
					break;
				case InterlinLineChoices.kflidWordGloss:
					tagIcon = ktagWordGlossIcon;
					break;
				case InterlinLineChoices.kflidWordPos:
					tagIcon = ktagWordPosIcon;
					break;
			}
			return InterlinComboHandler.MakeCombo(propertyTable?.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), tagIcon, this, morphIndex) as InterlinComboHandler;
		}

		internal List<int> GetComboItems(IPropertyTable propertyTable, int flid, int morphIndex)
		{
			var items = new List<int>();
			using (var handler = GetComboHandler(propertyTable, flid, morphIndex))
			{
				items.AddRange(handler.Items);
			}
			return items;
		}
		/// <summary />
		internal int GetComboItemHvo(IPropertyTable propertyTable, int flid, int morphIndex, string target)
		{
			using (var handler = GetComboHandler(propertyTable, flid, morphIndex))
			{
				int index;
				var item = handler.GetComboItem(target, out index);
				if (item != null)
				{
					return handler.Items[index];
				}
			}
			return 0;
		}

		internal void SetInterlinDocForTest(InterlinDocForAnalysis mockDoc)
		{
			m_mockInterlinDoc = mockDoc;
		}

		internal override InterlinDocForAnalysis InterlinDoc => m_mockInterlinDoc;
	}
}