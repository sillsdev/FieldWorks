// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public class ImportInterlinearOptions
	{
		public IThreadedProgress Progress;
		/// <summary>
		/// The bird data. NOTE: caller is responsible for disposing stream!
		/// </summary>
		public Stream BirdData;
		public int AllottedProgress;
		public Func<LcmCache, Interlineartext, ILgWritingSystemFactory, IThreadedProgress, bool> CheckAndAddLanguages;
		public ImportAnalysesLevel AnalysesLevel;
	}
}