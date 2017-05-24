// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.LCModel.DomainImpl
{
	/// <summary/>
	[TestFixture]
	class HomographConfigurationTests
	{
		/// <summary>
		/// Test the restore of the HomographConfiguration from a settings string
		/// </summary>
		[Test]
		public void HomographConfiguration_Persist_RestoresOldConfig()
		{
			// property string settings which set homograph to before, turn off the dictionary cross reference(hn:dcr),
			// turn off the reversal cross reference(hn:rcr), turn off show sense number in both dictionary and reversal
			const string oldConfigString = "before hn:dcr hn:rcr snRef snRev";
			var settings = new HomographConfiguration();
			Assert.IsFalse(settings.HomographNumberBefore);
			Assert.IsTrue(settings.ShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef));
			Assert.IsTrue(settings.ShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef));
			Assert.IsTrue(settings.ShowSenseNumberRef);
			Assert.IsTrue(settings.ShowSenseNumberReversal);
			settings.PersistData = oldConfigString;
			Assert.IsTrue(settings.HomographNumberBefore);
			Assert.IsFalse(settings.ShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef));
			Assert.IsFalse(settings.ShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef));
			Assert.IsFalse(settings.ShowSenseNumberRef);
			Assert.IsFalse(settings.ShowSenseNumberReversal);
		}

		/// <summary>
		/// Test the persistance of writing system and custom homograph numbers
		/// </summary>
		[Test]
		public void HomographConfiguration_Persist_Restores_WritingSystemAndCustomNumbers()
		{
			const string oldConfigString = "ws:en customHn:1;2;3;4;00";
			var settings = new HomographConfiguration();
			Assert.IsTrue(string.IsNullOrEmpty(settings.WritingSystem));
			CollectionAssert.IsEmpty(settings.CustomHomographNumbers);
			settings.PersistData = oldConfigString;
			Assert.AreEqual("en", settings.WritingSystem);
			CollectionAssert.AreEqual(new List<string> {"1", "2","3","4", "00"}, settings.CustomHomographNumbers);
		}

		/// <summary>
		/// Test the persistance of writing system and custom homograph numbers
		/// </summary>
		[Test]
		public void HomographConfiguration_Persist_Saves_WritingSystemAndCustomNumbers()
		{
			var settings = new HomographConfiguration();
			settings.CustomHomographNumbers = new List<string> { "a", "b", "c"};
			settings.WritingSystem = "fr";
			var persistanceString = settings.PersistData;
			Assert.That(persistanceString, Is.StringContaining("ws:fr"));
			Assert.That(persistanceString, Is.StringContaining("customHn:a;b;c"));
		}
	}
}
