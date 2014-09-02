using System;

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
