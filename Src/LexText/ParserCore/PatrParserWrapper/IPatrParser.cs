namespace PatrParserWrapper
{
	/// <summary>
	/// based upon from IPatrParser COM interface
	/// </summary>
	public interface IPatrParser
	{
		string ParseString(string bstrSentence);
		void ParseFile(string bstrInput, string bstrOutput);
		void LoadGrammarFile(string bstrGrammarFile);
		void LoadLexiconFile(string bstrLexiconFile, int fAdd);
		void Clear();
		void OpenLog(string bstrLogFile);
		void CloseLog();
		string GrammarFile { get;}
		string get_LexiconFile(long iFile);
		string LogFile { get; }
		int Unification { get; set; }
		long TreeDisplay { get; set; }
		long RootGlossFeature { get; set;}
		int Gloss { get; set;}
		long MaxAmbiguity { get; set;}
		int CheckCycles { get; set;}
		long CommentChar { get; set;}
		long TimeLimit { get; set;}
		int TopDownFilter { get; set;}
		int TrimEmptyFeatures { get; set;}
		long DebuggingLevel { get; set;}
		string LexRecordMarker { get; set;}
		string LexWordMarker { get; set;}
		string LexCategoryMarker { get; set;}
		string LexFeaturesMarker { get; set;}
		string LexGlossMarker { get; set;}
		string LexRootGlossMarker { get; set;}
		int TopFeatureOnly { get; set;}
		int DisplayFeatures { get; set;}
		int FlatFeatureDisplay { get; set;}
		int Failures { get; set;}
		long CodePage { get; set;}
		void DisambiguateAnaFile(string bstrInput, string bstrOutput);
		int WriteAmpleParses { get; set; }
		void LoadAnaFile(string bstrAnaFile, int fAdd);
		void ReloadLexicon();
		long LexiconFileCount { get; }
		int PromoteDefaultAtoms { get; set;}
		string SentenceFinalPunctuation { get; set;}
		int AmplePropertyIsFeature{ get; set; }
	}
}