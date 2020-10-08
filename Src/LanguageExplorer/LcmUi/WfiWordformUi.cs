// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// WfiWordformUi provides UI-specific methods for the WfiWordformUi class.
	/// </summary>
	internal sealed class WfiWordformUi : CmObjectUi
	{
		internal override bool CanDelete(out string cannotDeleteMsg)
		{
			if (base.CanDelete(out cannotDeleteMsg))
			{
				return true;
			}
			cannotDeleteMsg = LcmUiResources.ksCannotDeleteWordform;
			return false;
		}
	}
}