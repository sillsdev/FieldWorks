// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Impls
{
	internal interface IFlexConverterOnlyForTesting
	{
		bool AddPossibleAutoField(string className, string fwID);

		void Convert(string sfmFileName, string mappingFileName, string outputFileName);
	}
}