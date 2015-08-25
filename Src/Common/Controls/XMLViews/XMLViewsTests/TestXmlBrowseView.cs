using System;
using System.Xml;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;

namespace XMLViewsTests
{
	[TestFixture]
	public class TestXmlBrowseView
	{
		[Test]
		public void DoubleClickEmptyItem()
		{
			IPublisher publisher;
			ISubscriber subscriber;
			PubSubSystemFactory.CreatePubSubSystem(out publisher, out subscriber);
			using (var propertyTable = PropertyTableFactory.CreatePropertyTable(publisher))
			using (var bv = new XmlBrowseView())
			{
				bv.InitializeFlexComponent(propertyTable, publisher, subscriber);
				bv.SimulateDoubleClick(new EventArgs());
			}
		}

		[Test]
		public void MigrateBrowseColumns()
		{
			var input =
				"<root version=\"12\">" +
				"<column layout=\"Unknown Test\"/>" +
				"<column label=\"Headword\" sortmethod=\"FullSortKey\" ws=\"$ws=vernacular\" editable=\"false\" width=\"96000\"><span><properties><editable value=\"false\" /></properties><string field=\"MLHeadWord\" ws=\"vernacular\" /></span></column>" +
				"<column layout=\"Weather\" rubbish=\"nonsense\"/>" +
				"<column layout=\"IsAHeadwordForEntry\" label=\"Is a Headword\" visibility=\"dialog\"/>" +
				"<column layout=\"IsAbstractFormForEntry\" label=\"Is Abstract Form\" visibility=\"dialog\"  bulkEdit=\"integerOnSubfield\" bulkDelete=\"false\" field=\"LexEntry.LexemeForm\" subfield=\"MoForm.IsAbstract\" items=\"0:no;1:yes\" blankPossible=\"false\" sortType=\"YesNo\"/>" +
				"<column layout=\"ExceptionFeatures\" label=\"'Exception' Features\" multipara=\"true\" width=\"25%\"/>" +
				"<column layout=\"PictureCaptionForSense\" label=\"Picture-Caption\"  multipara=\"true\" editable=\"false\" visibility=\"dialog\" />" +
				"<column layout=\"AcademicDomainsForSense\" label=\"Academic Domains\" displayNameProperty=\"ShortNameTSS\" displayWs=\"analysis\" visibility=\"dialog\" />" +
				"<column layout=\"StatusForSense\" label=\"Status\"  field=\"LexSense.Status\" list=\"LexDb.Status\"  visibility=\"dialog\"/>" +
				"<column layout=\"ComplexEntryTypesBrowse\" ws=\"$ws=analysis\" label=\"Complex Form Types\" multipara=\"true\" />" +
				"<column layout=\"VariantEntryTypesBrowse\" ws=\"$ws=analysis\" label=\"Variant Types\" multipara=\"true\" />" +
				"<column layout=\"CustomIntegerForEntry_MyField\" label=\"$label\" visibility=\"menu\"/>" +
				"<column layout=\"CustomGenDateForEntry_SomeField\" label=\"$label\" visibility=\"menu\"/>" +
				"<column layout=\"CustomPossVectorForEntry_MyField\" label=\"$label\" multipara=\"true\" visibility=\"menu\"/>" +
				"<column layout=\"CustomPossAtomForEntry_AField\" label=\"$label\" visibility=\"menu\"/>" +
				"<column layout=\"CustomIntegerForSense_SenseField\" label=\"$label\" visibility=\"menu\"/>" +
				"<column layout=\"CustomPossVectorForSense_SenseVec\" label=\"$label\" multipara=\"true\" visibility=\"menu\"/>" +
				"<column layout=\"CustomPossAtomForSense_SenseAtom\" label=\"$label\" visibility=\"menu\"/>" +
				"<column layout=\"CustomGenDateForAllomorph_MorphDate\" label=\"$label\" visibility=\"menu\"/>" +
				"<column layout=\"CustomPossAtomForExample_ExAtom\" label=\"$label\" visibility=\"menu\"/>" +
				"</root>";
			IPublisher publisher;
			ISubscriber subscriber;
			PubSubSystemFactory.CreatePubSubSystem(out publisher, out subscriber);
			using (var propertyTable = PropertyTableFactory.CreatePropertyTable(publisher))
			{
				var output = XmlBrowseViewBaseVc.GetSavedColumns(input, propertyTable, "myKey");
				Assert.That(XmlUtils.GetOptionalAttributeValue(output.DocumentElement, "version"), Is.EqualTo(BrowseViewer.kBrowseViewVersion.ToString()));
				var headwordNode = output.SelectSingleNode("//column[@label='Headword']");
				Assert.That(headwordNode, Is.Not.Null);
				Assert.That(XmlUtils.GetOptionalAttributeValue(headwordNode, "layout"), Is.EqualTo("EntryHeadwordForFindEntry"));
				Assert.That(propertyTable.GetValue("myKey", string.Empty), Contains.Substring("EntryHeadwordForFindEntry"));
				var weatherNode = output.SelectSingleNode("//column[@layout='Weather']");
				Assert.That(weatherNode, Is.Null);
				Assert.That(propertyTable.GetValue("myKey", string.Empty), Contains.Substring("EntryHeadwordForFindEntry"));
				// Should not affect other nodes
				var unknownNode = output.SelectSingleNode("//column[@layout='Unknown Test']");
				Assert.That(unknownNode, Is.Not.Null);
				var abstractFormNode = output.SelectSingleNode("//column[@layout='IsAbstractFormForEntry']");
				Assert.That(abstractFormNode, Is.Not.Null);
				Assert.That(XmlUtils.GetOptionalAttributeValue(abstractFormNode, "bulkEdit"), Is.EqualTo("booleanOnSubfield"));
				Assert.That(XmlUtils.GetOptionalAttributeValue(abstractFormNode, "visibility"), Is.EqualTo("dialog"));
				Assert.That(XmlUtils.GetOptionalAttributeValue(abstractFormNode, "bulkDelete"), Is.EqualTo("false"));
				VerifyColumn(output, "ExceptionFeatures", "label", "Exception 'Features'");
				VerifyColumn(output, "PictureCaptionForSense", "ws", "$ws=vernacular analysis");
				VerifyColumn(output, "PictureCaptionForSense", "visibility", "dialog");
				VerifyColumn(output, "AcademicDomainsForSense", "displayWs", "best analysis");
				VerifyColumn(output, "StatusForSense", "list", "LangProject.Status");
				VerifyColumn(output, "ComplexEntryTypesBrowse", "ghostListField", "LexDb.AllComplexEntryRefPropertyTargets");
				VerifyColumn(output, "VariantEntryTypesBrowse", "ghostListField", "LexDb.AllVariantEntryRefPropertyTargets");
				VerifyColumn(output, "CustomIntegerForEntry_MyField", "sortType", "integer");
				VerifyColumn(output, "CustomGenDateForEntry_SomeField", "sortType", "genDate");

				VerifyColumn(output, "CustomPossVectorForEntry_MyField", "bulkEdit", "complexListMultiple");
				VerifyColumn(output, "CustomPossVectorForEntry_MyField", "field", "LexEntry.$fieldName");
				VerifyColumn(output, "CustomPossVectorForEntry_MyField", "list", "$targetList");
				VerifyColumn(output, "CustomPossVectorForEntry_MyField", "displayNameProperty", "ShortNameTSS");

				VerifyColumn(output, "CustomPossAtomForEntry_AField", "bulkEdit", "atomicFlatListItem");
				VerifyColumn(output, "CustomPossAtomForEntry_AField", "field", "LexEntry.$fieldName");
				VerifyColumn(output, "CustomPossAtomForEntry_AField", "list", "$targetList");

				VerifyColumn(output, "CustomIntegerForSense_SenseField", "sortType", "integer");
				VerifyColumn(output, "CustomPossVectorForSense_SenseVec", "field", "LexSense.$fieldName");
				VerifyColumn(output, "CustomPossAtomForSense_SenseAtom", "field", "LexSense.$fieldName");

				VerifyColumn(output, "CustomGenDateForAllomorph_MorphDate", "sortType", "genDate");
				VerifyColumn(output, "CustomPossAtomForExample_ExAtom", "field", "LexExampleSentence.$fieldName");

				// version 15
				var isAHeadwordNode = output.SelectSingleNode("//column[@layout='IsAHeadwordForEntry']");
				Assert.That(isAHeadwordNode, Is.Null);
				var publishAsHeadwordNode = output.SelectSingleNode("//column[@layout='PublishAsHeadword']");
				Assert.That(publishAsHeadwordNode, Is.Not.Null);

				// version 14
				// Todo!

				// Just version 15
				input =
				"<root version=\"14\">" +
				"<column layout=\"Unknown Test\"/>" +
				"<column layout=\"IsAHeadwordForEntry\" label=\"Is a Headword\" visibility=\"dialog\"/>" +
				"</root>";
				output = XmlBrowseViewBaseVc.GetSavedColumns(input, propertyTable, "myKey");
				Assert.That(XmlUtils.GetOptionalAttributeValue(output.DocumentElement, "version"), Is.EqualTo(BrowseViewer.kBrowseViewVersion.ToString()));
				isAHeadwordNode = output.SelectSingleNode("//column[@layout='IsAHeadwordForEntry']");
				Assert.That(isAHeadwordNode, Is.Null);
				publishAsHeadwordNode = output.SelectSingleNode("//column[@layout='PublishAsHeadword']");
				Assert.That(publishAsHeadwordNode, Is.Not.Null);
				Assert.That(propertyTable.GetValue("myKey", string.Empty), Contains.Substring("PublishAsHeadword"));
			}
		}

		void VerifyColumn(XmlNode output, string layoutName, string attrName, string attrVal)
		{
			var node = output.SelectSingleNode("//column[@layout='" + layoutName + "']");
			Assert.That(node, Is.Not.Null);
			Assert.That(XmlUtils.GetOptionalAttributeValue(node, attrName), Is.EqualTo(attrVal));

		}
	}
}
