// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Implement this interface to generate content based off of the data in an LCM cache and the users dictionary configuration.
	/// </summary>
	public interface ILcmContentGenerator
	{
		IFragment GenerateWsPrefixWithString(ConfigurableDictionaryNode config, ConfiguredLcmGenerator.GeneratorSettings settings, bool displayAbbreviation, int wsId, IFragment content);
		IFragment GenerateAudioLinkContent(ConfigurableDictionaryNode config, string classname, string srcAttribute, string caption, string safeAudioId);
		IFragment WriteProcessedObject(ConfigurableDictionaryNode config, bool isBlock, IFragment elementContent, string className);
		IFragment WriteProcessedCollection(ConfigurableDictionaryNode config, bool isBlock, IFragment elementContent, string className);
		IFragment GenerateGramInfoBeforeSensesContent(IFragment content, ConfigurableDictionaryNode config);
		IFragment GenerateGroupingNode(ConfigurableDictionaryNode config, object field, string className,  DictionaryPublicationDecorator publicationDecorator, ConfiguredLcmGenerator.GeneratorSettings settings,
			Func<object, ConfigurableDictionaryNode, DictionaryPublicationDecorator, ConfiguredLcmGenerator.GeneratorSettings, IFragment> childContentGenerator);
		IFragment AddSenseData(ConfigurableDictionaryNode config, IFragment senseNumberSpan, Guid ownerGuid, IFragment senseContent, bool first);
		IFragment AddCollectionItem(ConfigurableDictionaryNode config, bool isBlock, string collectionItemClass, IFragment content, bool first);
		IFragment AddProperty(ConfigurableDictionaryNode config, ReadOnlyPropertyTable propTable, string className, bool isBlockProperty, string content, string writingSystem);
		IFragment CreateFragment();
		IFragment CreateFragment(string str);
		IFragmentWriter CreateWriter(IFragment fragment);
		void StartMultiRunString(IFragmentWriter writer, ConfigurableDictionaryNode config, string writingSystem);
		void EndMultiRunString(IFragmentWriter writer);
		void StartBiDiWrapper(IFragmentWriter writer, ConfigurableDictionaryNode config, bool rightToLeft);
		void EndBiDiWrapper(IFragmentWriter writer);
		void StartRun(IFragmentWriter writer, ConfigurableDictionaryNode config, ReadOnlyPropertyTable propTable, string writingSystem, bool first);
		void EndRun(IFragmentWriter writer);
		void SetRunStyle(IFragmentWriter writer, ConfigurableDictionaryNode config, ReadOnlyPropertyTable propertyTable, string writingSystem, string runStyle, bool error);
		void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, Guid destination);
		void StartLink(IFragmentWriter writer, ConfigurableDictionaryNode config, string externalDestination);
		void EndLink(IFragmentWriter writer);
		void AddToRunContent(IFragmentWriter writer, string txtContent);
		void AddLineBreakInRunContent(IFragmentWriter writer, ConfigurableDictionaryNode config);
		void StartTable(IFragmentWriter writer, ConfigurableDictionaryNode config);
		void AddTableTitle(IFragmentWriter writer, IFragment content);
		void StartTableBody(IFragmentWriter writer);
		void StartTableRow(IFragmentWriter writer);
		void AddTableCell(IFragmentWriter writer, bool isHead, int colSpan, HorizontalAlign alignment, IFragment content);
		void EndTableRow(IFragmentWriter writer);
		void EndTableBody(IFragmentWriter writer);
		void EndTable(IFragmentWriter writer, ConfigurableDictionaryNode config);
		void StartEntry(IFragmentWriter writer, ConfigurableDictionaryNode config, string className, Guid entryGuid, int index, RecordClerk clerk);
		void AddEntryData(IFragmentWriter writer, List<ConfiguredLcmGenerator.ConfigFragment> pieces);
		void EndEntry(IFragmentWriter writer);
		void AddCollection(IFragmentWriter writer, ConfigurableDictionaryNode config, bool isBlockProperty, string className, IFragment content);
		void BeginObjectProperty(IFragmentWriter writer, ConfigurableDictionaryNode config, bool isBlockProperty, string getCollectionItemClassAttribute);
		void EndObject(IFragmentWriter writer);
		void WriteProcessedContents(IFragmentWriter writer, ConfigurableDictionaryNode config, IFragment contents);
		IFragment AddImage(ConfigurableDictionaryNode config, string classAttribute, string srcAttribute, string pictureGuid);
		IFragment AddImageCaption(ConfigurableDictionaryNode config, IFragment captionContent);
		IFragment GenerateSenseNumber(ConfigurableDictionaryNode config, string formattedSenseNumber, string senseNumberWs);
		IFragment AddLexReferences(ConfigurableDictionaryNode config, bool generateLexType, IFragment lexTypeContent, string className, IFragment referencesContent, bool typeBefore);
		void BeginCrossReference(IFragmentWriter writer, ConfigurableDictionaryNode config, bool isBlockProperty, string className);
		void EndCrossReference(IFragmentWriter writer);
		void BetweenCrossReferenceType(IFragment content, ConfigurableDictionaryNode node, bool firstItem);
		IFragment WriteProcessedSenses(ConfigurableDictionaryNode config, bool isBlock, IFragment senseContent, string className, IFragment sharedCollectionInfo);
		IFragment AddAudioWsContent(string wsId, Guid linkTarget, IFragment fileContent);
		IFragment GenerateErrorContent(StringBuilder badStrBuilder);
		IFragment GenerateVideoLinkContent(ConfigurableDictionaryNode config, string className, string mediaId, string srcAttribute,
			string caption);
	}
}