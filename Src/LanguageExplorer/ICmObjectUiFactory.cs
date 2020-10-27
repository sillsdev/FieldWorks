// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer
{
	internal interface ICmObjectUiFactory
	{
		ICmObjectUi MakeLcmModelUiObject(ICmObject cmObject);

		ICmObjectUi MakeLcmModelUiObject(ICmObject cmObject, int newObjectClassId, int flid, int insertionPosition);
	}
}