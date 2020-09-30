// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Tools
{
	internal sealed class ImportInterlinearOptions
	{
		internal IThreadedProgress Progress;
		/// <summary>
		/// The bird data. NOTE: caller is responsible for disposing stream!
		/// </summary>
		internal Stream BirdData;
		internal int AllottedProgress;
		internal Func<LcmCache, Interlineartext, ILgWritingSystemFactory, IThreadedProgress, bool> CheckAndAddLanguages;
		internal ImportAnalysesLevel AnalysesLevel;
	}
}