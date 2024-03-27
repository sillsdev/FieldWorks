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
		IFragment GenerateWsPrefixWithString(ConfigurableDictionaryNode config, ConfiguredLcmGenerator.GeneratorSettings settings, bool displayAbbreviation, int wsId, IFragment content);
		IFragment GenerateAudioLinkContent(string classname, string srcAttribute, string caption, string safeAudioId);
		IFragment WriteProcessedObject(bool isBlock, IFragment elementContent, ConfigurableDictionaryNode config, string className);
		IFragment WriteProcessedCollection(bool isBlock, IFragment elementContent, ConfigurableDictionaryNode config, string className);
		IFragment GenerateGramInfoBeforeSensesContent(IFragment content, ConfigurableDictionaryNode config);
		IFragment GenerateGroupingNode(object field, string className, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, ConfiguredLcmGenerator.GeneratorSettings settings,
			Func<object, ConfigurableDictionaryNode, DictionaryPublicationDecorator, ConfiguredLcmGenerator.GeneratorSettings, IFragment> childContentGenerator);
		IFragment AddSenseData(IFragment senseNumberSpan, bool isBlockProperty, Guid ownerGuid, IFragment senseContent, string className);
		IFragment AddCollectionItem(bool isBlock, string collectionItemClass, ConfigurableDictionaryNode config,IFragment content);
		IFragment AddProperty(string className, bool isBlockProperty, string content);
		IFragment CreateFragment();
		IFragment CreateFragment(string str);
		IFragmentWriter CreateWriter(IFragment fragment);
		void StartMultiRunString(IFragmentWriter writer, string writingSystem);
		void EndMultiRunString(IFragmentWriter writer);
		void StartBiDiWrapper(IFragmentWriter writer, bool rightToLeft);
		void EndBiDiWrapper(IFragmentWriter writer);
		void StartRun(IFragmentWriter writer, string writingSystem);
		void EndRun(IFragmentWriter writer);
		void SetRunStyle(IFragmentWriter writer, ConfigurableDictionaryNode config, string writingSystem, string css, string runStyle);
		void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, Guid destination);
		void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, string externalDestination);
		void EndLink(IFragmentWriter writer);
		void AddToRunContent(IFragmentWriter writer, string txtContent);
		void AddLineBreakInRunContent(IFragmentWriter writer);
		void StartTable(IFragmentWriter writer);
		void AddTableTitle(IFragmentWriter writer, IFragment content);
		void StartTableBody(IFragmentWriter writer);
		void StartTableRow(IFragmentWriter writer);
		void AddTableCell(IFragmentWriter writer, bool isHead, int colSpan, HorizontalAlign alignment, IFragment content);
		void EndTableRow(IFragmentWriter writer);
		void EndTableBody(IFragmentWriter writer);
		void EndTable(IFragmentWriter writer);
		void StartEntry(IFragmentWriter writer, ConfigurableDictionaryNode config, string className, Guid entryGuid, int index, RecordClerk clerk);
		void AddEntryData(IFragmentWriter writer, List<ConfiguredLcmGenerator.ConfigFragment> pieces);
		void EndEntry(IFragmentWriter writer);
		void AddCollection(IFragmentWriter writer, bool isBlockProperty, string className, ConfigurableDictionaryNode config, string content);
		void BeginObjectProperty(IFragmentWriter writer, bool isBlockProperty, string getCollectionItemClassAttribute);
		void EndObject(IFragmentWriter writer);
		void WriteProcessedContents(IFragmentWriter writer, IFragment contents);
		IFragment AddImage(string classAttribute, string srcAttribute, string pictureGuid);
		IFragment AddImageCaption(string captionContent);
		IFragment GenerateSenseNumber(string formattedSenseNumber, string senseNumberWs, ConfigurableDictionaryNode config);
		IFragment AddLexReferences(bool generateLexType, IFragment lexTypeContent, ConfigurableDictionaryNode config, string className, string referencesContent, bool typeBefore);
		void BeginCrossReference(IFragmentWriter writer, bool isBlockProperty, string className);
		void EndCrossReference(IFragmentWriter writer);
		IFragment WriteProcessedSenses(bool isBlock, IFragment senseContent, ConfigurableDictionaryNode config, string className, IFragment sharedCollectionInfo);
		IFragment AddAudioWsContent(string wsId, Guid linkTarget, IFragment fileContent);
		IFragment GenerateErrorContent(StringBuilder badStrBuilder);
		IFragment GenerateVideoLinkContent(string className, string mediaId, string srcAttribute,
			string caption);
	}
}