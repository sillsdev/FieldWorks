// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	public enum NodeTestResult
	{
		kntrSomething, // really something here we could expand
		kntrPossible, // nothing here, but there could be
		kntrNothing // nothing could possibly be here, don't show collapsed OR expanded.
	}
}