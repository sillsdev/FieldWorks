// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls;

namespace LanguageExplorer.Areas.Lexicon
{
	internal interface ISemanticDomainTreeBarHandler : ITreeBarHandler
	{
		void FinishInitialization(IPaneBar paneBar);
	}
}