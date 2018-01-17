// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	internal class NullTargetAdjuster : IPreferedTargetAdjuster
	{
		public ICmObject AdjustTarget(ICmObject target)
		{
			return target;
		}
	}
}