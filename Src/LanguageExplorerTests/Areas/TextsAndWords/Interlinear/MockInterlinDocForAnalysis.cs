// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	internal sealed class MockInterlinDocForAnalysis : InterlinDocForAnalysis
	{
		internal MockInterlinDocForAnalysis(IStText testText)
		{
			Cache = testText.Cache;
			m_hvoRoot = testText.Hvo;
			Vc = new InterlinVc(Cache)
			{
				RootSite = this
			};
		}

		protected override FocusBoxController CreateFocusBoxInternal()
		{
			return new TestableFocusBox();
		}

		public override void SelectOccurrence(AnalysisOccurrence target)
		{
			InstallFocusBox();
			FocusBox.SelectOccurrence(target);
		}

		internal override void UpdateGuesses(HashSet<IWfiWordform> wordforms)
		{
			// for now, don't update guesses in these tests.
		}

		internal IVwRootBox MockedRootBox
		{
			set { RootBox = value; }
		}

		/// <summary>
		/// For testing purposes, we want to pretend to have focus all the time.
		/// </summary>
		public override bool Focused => true;

		/// <summary>
		/// Calls SetActiveFreeform on the view constructor to simulate having an empty free
		/// translation line selected (with the "Press Enter..." prompt).
		/// </summary>
		internal void CallSetActiveFreeform(int hvoSeg, int ws)
		{
			ReflectionHelper.CallMethod(Vc, "SetActiveFreeform", hvoSeg, SegmentTags.kflidFreeTranslation, ws, 0);
		}
	}
}