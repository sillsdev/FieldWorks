// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class UploadToWebonaryModelTests
	{
		[Test]
		public void EncryptPassword_NullAndEmptyDoNotCrash()
		{
			Assert.That(() => UploadToWebonaryModel.EncryptPassword(null), Throws.Nothing);
			Assert.That(() => UploadToWebonaryModel.EncryptPassword(string.Empty), Throws.Nothing);
		}

		[Test]
		public void DecryptPassword_NullAndEmptyDoNotCrash()
		{
			Assert.That(() => UploadToWebonaryModel.DecryptPassword(null), Throws.Nothing);
			Assert.That(() => UploadToWebonaryModel.DecryptPassword(string.Empty), Throws.Nothing);
		}
	}
}
