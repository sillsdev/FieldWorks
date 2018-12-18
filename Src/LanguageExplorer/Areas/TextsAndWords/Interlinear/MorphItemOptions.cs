// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class MorphItemOptions
	{
		internal int HvoMoForm;
		internal int HvoEntry;
		internal int HvoSense;
		internal int HvoMsa;
		internal ILexEntryInflType InflType;
		internal ILexEntryRef EntryRef;
		internal ITsString TssName;
		internal string SenseName;
		internal string MsaName;
	}
}