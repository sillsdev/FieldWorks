// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class DictConfigModelExtTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void Creator()
		{
			Assert.That(ConfiguredXHTMLGeneratorTests.CreatePicture(Cache).Creator(), Is.EqualTo("Jason Naylor"));
			Assert.That(ConfiguredXHTMLGeneratorTests.CreatePicture(Cache, false).Creator(), Is.Null, "no file on disk");
			// LT-21573: PictureFileRA can be null after an incomplete SFM import
			var picLess = ConfiguredXHTMLGeneratorTests.CreatePicture(Cache);
			picLess.PictureFileRA = null;
			Assert.That(picLess.Creator(), Is.Null, "null PictureFileRA");
		}

		[Test]
		public void CopyrightAndLicense()
		{
			Assert.That(ConfiguredXHTMLGeneratorTests.CreatePicture(Cache).CopyrightAndLicense(), Is.EqualTo("Copyright © 2023, Jason Naylor, CC BY-NC 4.0"));
			Assert.That(ConfiguredXHTMLGeneratorTests.CreatePicture(Cache, false).CopyrightAndLicense(), Is.Null);
		}
	}
}
