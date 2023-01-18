// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	internal interface ISetupLineChoices
	{
		/// <summary>
		/// True if we will be doing editing (display sandbox, restrict field order choices, etc.).
		/// </summary>
		bool ForEditing { get; set; }
		InterlinLineChoices SetupLineChoices(string lineConfigPropName, string oldConfigPropName, InterlinMode mode);
	}
}