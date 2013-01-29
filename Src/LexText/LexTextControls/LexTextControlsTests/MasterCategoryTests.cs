using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;

namespace LexTextControlsTests
{
	/// <summary>
	/// Start of tests for MasterCategory. Very incomplete as yet.
	/// </summary>
	[TestFixture]
	public class MasterCategoryTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void MasterCategoryWithGuidNode_MakesPosWithRightGuid()
		{
			string input =
				@"<eticPOSList xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns:rdfs='http://www.w3.org/2000/01/rdf-schema#' xmlns:owl='http://www.w3.org/2002/07/owl#'>
   <item type='category' id='Adjective' guid='30d07580-5052-4d91-bc24-469b8b2d7df9'>
	  <abbrev ws='en'>adj</abbrev>
	  <term ws='en'>Adjective</term>
	  <def ws='en'>An adjective is a part of speech whose members modify nouns. An adjective specifies the attributes of a noun referent. Note: this is one case among many. Adjectives are a class of modifiers.</def>
   </item>
   <item type='category' id='Adposition' guid='ae115ea8-2cd7-4501-8ae7-dc638e4f17c5'>
	  <abbrev ws='en'>adp</abbrev>
	  <term ws='en'>Adposition</term>
	  <def ws='en'>An adposition is a part of speech whose members are of a closed set and occur before or after a complement composed of a noun phrase, noun, pronoun, or clause that functions as a noun phrase and forms a single structure with the complement to express its grammatical and semantic relation to another unit within a clause.</def>
	  <item type='category' id='Postposition' guid='18f1b2b8-0ce3-4889-90e9-003fed6a969f'>
		 <abbrev ws='en'>post</abbrev>
		 <term ws='en'>Postposition</term>
		 <def ws='en'>A postposition is an adposition that occurs after its complement.</def>
		  <item type='category' id='PPchild' guid='82B1250A-E64F-4AD8-8B8C-5ABBC732087A'>
			 <abbrev ws='en'>ppc</abbrev>
			 <term ws='en'>PPchild</term>
			 <def ws='en'>An imaginary POS to test another code path.</def>
		  </item>
	  </item>
	</item>
</eticPOSList>";
			m_actionHandler.EndUndoTask(); // AddToDatabase makes its own

			var posList = Cache.LangProject.PartsOfSpeechOA;
			Assert.That(posList, Is.Not.Null, "Test requires default init of cache to create POS list");
			CheckPosDoesNotExist("ae115ea8-2cd7-4501-8ae7-dc638e4f17c5");
			CheckPosDoesNotExist("18f1b2b8-0ce3-4889-90e9-003fed6a969f");
			// Not checking the third one because it is non-standard guid for an imaginary POS; negligible chance it already exists.

			var doc = new XmlDocument();
			doc.LoadXml(input);
			var rootItem = doc.DocumentElement.ChildNodes[1];

			var mc = MasterCategoryListDlg.MasterCategory.Create(new Set<IPartOfSpeech>(), rootItem, Cache);
			mc.AddToDatabase(Cache, posList, null, null);
			var adposition = CheckPos("ae115ea8-2cd7-4501-8ae7-dc638e4f17c5", posList);

			var childItem = rootItem.ChildNodes[3];
			var mcChild = MasterCategoryListDlg.MasterCategory.Create(new Set<IPartOfSpeech> { adposition }, childItem, Cache);
			mcChild.AddToDatabase(Cache, posList, null, adposition);
			var postPosition = CheckPos("18f1b2b8-0ce3-4889-90e9-003fed6a969f", adposition);

			var grandChildItem = childItem.ChildNodes[3];
			var mcGrandChild = MasterCategoryListDlg.MasterCategory.Create(new Set<IPartOfSpeech> { adposition, postPosition }, grandChildItem, Cache);
			mcGrandChild.AddToDatabase(Cache, posList, mcChild, null);
			CheckPos("82B1250A-E64F-4AD8-8B8C-5ABBC732087A", postPosition);
		}

		private IPartOfSpeech CheckPos(string guid, ICmObject owner)
		{
			IPartOfSpeech pos;
			Assert.That(Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().TryGetObject(new Guid(guid), out pos),
				Is.True,
				"expected POS should be created with the right guid");
			Assert.That(pos.Owner, Is.EqualTo(owner), "POS should be created at the right place in the hierarchy");
			return pos;
		}

		private void CheckPosDoesNotExist(string id)
		{
			IPartOfSpeech pos;
			Assert.That(Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().TryGetObject(new Guid(id), out pos), Is.False,
				"default possibility list should not already contain objects that this test creates");
		}
	}
}
