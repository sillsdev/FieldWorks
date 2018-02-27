// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace XAmpleManagedWrapper
{
	public interface IXAmpleWrapper
	{
		void Init();

		// returns xmlResult
		string ParseWord(string wordform);

		// returns xmlResult
		string TraceWord(string wordform, string selectedMorphs);

		void LoadFiles(string fixedFilesDir, string dynamicFilesDir, string databaseName);

		void SetParameter(string name, string value);

		// returns pid
		int AmpleThreadId
		{
			get;
		}
	}
}
