// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace SIL.FieldWorks.LexicalProvider
{
	/// <summary>
	/// Provides a service contract for accessing lexical data from other applications.
	/// </summary>
	[ServiceContract]
	public interface ILexicalProvider
	{
		/// <summary>
		/// Displays the specified entry using the application with the lexical data.
		/// </summary>
		[OperationContract]
		void ShowEntry(string entry, EntryType entryType);

		/// <summary>
		/// Displays the related words using the application with the lexical data.
		/// </summary>
		[OperationContract]
		void ShowRelatedWords(string entry, EntryType entryType);

		/// <summary>
		/// Gets all lexemes in the Lexicon
		/// </summary>
		[OperationContract]
		IEnumerable<LexicalEntry> Lexemes();

		/// <summary>
		/// Looks up an lexeme in the lexicon
		/// </summary>
		[OperationContract]
		LexicalEntry GetLexeme(LexemeType type, string lexicalForm, int homograph);

		/// <summary>
		/// Adds the lexeme to the lexicon.
		/// </summary>
		/// <exception cref="ArgumentException">if matching lexeme is already in lexicon</exception>
		[OperationContract]
		void AddLexeme(LexicalEntry lexeme);

		/// <summary>
		/// Adds a new sense to the lexeme with the specified information
		/// </summary>
		[OperationContract]
		LexSense AddSenseToEntry(LexemeType type, string lexicalForm, int homograph);

		/// <summary>
		/// Adds a new gloss to the sense with the specified information
		/// </summary>
		[OperationContract]
		LexGloss AddGlossToSense(LexemeType type, string lexicalForm, int homograph, string senseId, string language, string text);

		/// <summary>
		/// Removes the gloss with the specified language form the sense with the specified information
		/// </summary>
		[OperationContract]
		void RemoveGloss(LexemeType type, string lexicalForm, int homograph, string senseId, string language);

		/// <summary>
		/// Forces a save of lexicon
		/// </summary>
		[OperationContract]
		void Save();

		/// <summary>
		/// This must be called before entries are changed to ensure that
		/// it is saved to disk. Since the lexicon is a complex structure
		/// and other features depend on knowing when it is changed,
		/// all work done with the lexicon is marked with a begin and
		/// end change.
		/// </summary>
		[OperationContract]
		void BeginChange();

		/// <summary>
		/// This must be called after entries are changed to ensure that
		/// other features dependent on the lexicon are made aware of the
		/// change.
		/// </summary>
		[OperationContract]
		void EndChange();
	}
}