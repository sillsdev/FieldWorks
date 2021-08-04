// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Implement this interface to generate content based off of the data in an LCM cache and the users dictionary configuration.
	/// </summary>
	public interface ILcmContentGenerator
	{
		string GenerateWsPrefixWithString(ConfiguredLcmGenerator.GeneratorSettings settings, bool displayAbbreviation, int wsId, string content);
		string GenerateAudioLinkContent(string classname, string srcAttribute, string caption, string safeAudioId);
		string WriteProcessedObject(bool isBlock, string elementContent, string className);
		string WriteProcessedCollection(bool isBlock, string elementContent, string className);
		string GenerateGramInfoBeforeSensesContent(string content);
		string GenerateGroupingNode(object field, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, ConfiguredLcmGenerator.GeneratorSettings settings,
			Func<object, ConfigurableDictionaryNode, DictionaryPublicationDecorator, ConfiguredLcmGenerator.GeneratorSettings, string> childContentGenrator);
		string AddSenseData(string senseNumberSpan, bool isBlockProperty, Guid ownerGuid, string senseContent, string className);
		string AddCollectionItem(bool isBlock, string collectionItemClass, string content);
		string AddProperty(string className, bool isBlockProperty, string content);

		IFragmentWriter CreateWriter(StringBuilder bldr);
		void StartMultiRunString(IFragmentWriter writer, string writingSystem);
		void EndMultiRunString(IFragmentWriter writer);
		void StartBiDiWrapper(IFragmentWriter writer, bool rightToLeft);
		void EndBiDiWrapper(IFragmentWriter writer);
		void StartRun(IFragmentWriter writer, string writingSystem);
		void EndRun(IFragmentWriter writer);
		void SetRunStyle(IFragmentWriter writer, string css);
		void StartLink(IFragmentWriter writer, Guid destination);
		void EndLink(IFragmentWriter writer);
		void AddToRunContent(IFragmentWriter writer, string txtContent);
		void AddLineBreakInRunContent(IFragmentWriter writer);
		void StartTable(IFragmentWriter writer);
		void AddTableTitle(IFragmentWriter writer, string content);
		void StartTableBody(IFragmentWriter writer);
		void StartTableRow(IFragmentWriter writer);
		void AddTableCell(IFragmentWriter writer, bool isHead, HorizontalAlign alignment, string content);
		void EndTableRow(IFragmentWriter writer);
		void EndTableBody(IFragmentWriter writer);
		void EndTable(IFragmentWriter writer);
		void StartEntry(IFragmentWriter writer, string className, Guid entryGuid, int index);
		void AddEntryData(IFragmentWriter writer, List<string> pieces);
		void EndEntry(IFragmentWriter writer);
		void AddCollection(IFragmentWriter writer, bool isBlockProperty, string className, string content);
		void BeginObjectProperty(IFragmentWriter writer, bool isBlockProperty, string getCollectionItemClassAttribute);
		void EndObject(IFragmentWriter writer);
		void WriteProcessedContents(IFragmentWriter writer, string contents);
		string AddImage(string classAttribute, string srcAttribute, string pictureGuid);
		string AddImageCaption(string captionContent);
		string GenerateSenseNumber(string formattedSenseNumber);
		string AddLexReferences(bool generateLexType, string lexTypeContent, string className, string referencesContent, bool typeBefore);
		void BeginCrossReference(IFragmentWriter writer, bool isBlockProperty, string className);
		void EndCrossReference(IFragmentWriter writer);
		string WriteProcessedSenses(bool isBlock, string senseContent, string className, string sharedCollectionInfo);
		string AddAudioWsContent(string wsId, Guid linkTarget, string fileContent);
		string GenerateErrorContent(StringBuilder badStrBuilder);
		string GenerateVideoLinkContent(string className, string mediaId, string srcAttribute,
			string caption);
	}
}