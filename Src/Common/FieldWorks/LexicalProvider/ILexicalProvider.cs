// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ILexicalProvider.cs
// Responsibility: FW Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace SIL.FieldWorks.LexicalProvider
{
	#region ILexicalServiceProvider interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides a service contract for getting a lexical provider from an application.
	/// WARNING: Paratext contains its own identical definition of these interfaces.
	/// Any change must be coordinated (both in corresponding source files and in terms
	/// of product release schedules.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ServiceContract]
	public interface ILexicalServiceProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location for the provider for the specified project and
		/// provider type. If the providerType is not supported, return null for the Uri.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OperationContract]
		Uri GetProviderLocation(string projhandle, string providerType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of the specified provider that the server supports. If the
		/// providerType is not supported, return 0 for the version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OperationContract]
		int GetSupportedVersion(string providerType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unlike a normal ping method that gets a response, we just use this ping method
		/// to determine if the service provider is actually valid since no exception is
		/// thrown until a method is called.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OperationContract]
		void Ping();
	}
	#endregion

	#region ILexicalProvider interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides a service contract for accessing lexical data from other applications.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ServiceContract]
	public interface ILexicalProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the specified entry using the application with the lexical data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OperationContract]
		void ShowEntry(string entry, EntryType entryType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the related words using the application with the lexical data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OperationContract]
		void ShowRelatedWords(string entry, EntryType entryType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all lexemes in the Lexicon
		/// </summary>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forces a save of lexicon
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OperationContract]
		void Save();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This must be called before entries are changed to ensure that
		/// it is saved to disk. Since the lexicon is a complex structure
		/// and other features depend on knowing when it is changed,
		/// all work done with the lexicon is marked with a begin and
		/// end change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OperationContract]
		void BeginChange();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This must be called after entries are changed to ensure that
		/// other features dependent on the lexicon are made aware of the
		/// change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OperationContract]
		void EndChange();
	}
	#endregion

	#region EntryType enumeration
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Types of lexical entries that can be requested by a client of a LexicalProvider
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum EntryType
	{
		/// <summary>entry represents a word</summary>
		Word
	}
	#endregion

	#region LexemeType enumeration
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// All known lexeme types
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum LexemeType
	{
		/// <summary></summary>
		Phrase,
		/// <summary></summary>
		Word,
		/// <summary></summary>
		Lemma,
		/// <summary></summary>
		Stem,
		/// <summary></summary>
		Prefix,
		/// <summary></summary>
		Suffix
	};
	#endregion

	#region LexicalEntry class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Data contract used by WCF for holding information about a Lexeme
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[DataContract(Namespace = "LexicalData")]
	public sealed class LexicalEntry
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexicalEntry"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LexicalEntry(LexemeType type, string form, int homograph)
		{
			Type = type;
			LexicalForm = form;
			Homograph = homograph;
			Senses = new List<LexSense>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DataMember]
		public LexemeType Type { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the lexical form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DataMember]
		public string LexicalForm { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the homograph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DataMember]
		public int Homograph { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the senses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DataMember]
		public IList<LexSense> Senses { get; set; }
	}
	#endregion

	#region LexSense class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Data contract used by WCF for holding information about a Sense
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[DataContract(Namespace = "LexicalData")]
	public sealed class LexSense
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexSense"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LexSense(string id)
		{
			Id = id;
			Glosses = new List<LexGloss>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the id.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DataMember]
		public string Id { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the glosses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DataMember]
		public IList<LexGloss> Glosses { get; set; }
	}
	#endregion

	#region LexGloss class
	/// <summary>
	/// Data contract used by WCF for holding information about a Gloss
	/// </summary>
	[DataContract(Namespace = "LexicalData")]
	public sealed class LexGloss
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexGloss"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LexGloss(string language, string text)
		{
			Language = language;
			Text = text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DataMember]
		public string Language { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DataMember]
		public string Text { get; private set; }
	}
	#endregion
}
