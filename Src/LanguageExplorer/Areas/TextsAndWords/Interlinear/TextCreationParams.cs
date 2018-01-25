// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class TextCreationParams
	{
		internal Interlineartext InterlinText;
		internal LcmCache Cache;
		internal IThreadedProgress Progress;
		internal ImportInterlinearOptions ImportOptions;
		internal int Version;
	}
}