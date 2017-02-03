// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using NUnit.Framework;
using Palaso.TestUtilities;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary/>
	[TestFixture]
	class HomographConfigurationTests : BaseTest
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
	}
}
