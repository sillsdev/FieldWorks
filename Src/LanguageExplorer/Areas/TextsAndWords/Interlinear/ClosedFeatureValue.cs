// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public class ClosedFeatureValue
	{
		public ClosedFeatureValue(IFsSymFeatVal symbol, bool negate)
		{
			Symbol = symbol;
			Negate = negate;
		}

		public IFsSymFeatVal Symbol { get; }

		public bool Negate { get; }
	}
}