using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Tests (for now pretty incomplete) of the EnchantHelper class.
	/// </summary>
	[TestFixture]
	public class EnchantHelperTests
	{
		/// <summary>
		/// Check how spelling status is set and cleared.
		/// </summary>
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux FWNX-610: libenchant isn't fully case sensitive - need to patch libenchant.")]
		public void BasicSpellingStatus()
		{
			var dirPath = EnchantHelper.GetSpellingDirectoryPath();
			Directory.CreateDirectory(dirPath);
			var dictId = "testDummy1";
			var filePath = EnchantHelper.GetDicPath(dirPath, dictId);
			File.Delete(filePath);
			File.Delete(Path.ChangeExtension(filePath, ".aff"));
			string path = EnchantHelper.GetDicPath(EnchantHelper.GetSpellingOverridesDirectory(), dictId);
			File.Delete(path);
			File.Delete(Path.ChangeExtension(path, ".exc"));
			EnchantHelper.EnsureDictionary(dictId);
			Assert.IsTrue(Enchant.Broker.IsLibEnchantAvailable, "Enchant is not available!");
			Assert.IsTrue(Enchant.Broker.Default.DictionaryExists(dictId), "Dictionary doesn't exist!");
			using (var dict = EnchantHelper.GetDict(dictId))
			{
			Assert.That(dict, Is.Not.Null);
			Assert.That(dict.Check("nonsense"), Is.False);
			Assert.That(dict.Check("big"), Is.False);
			EnchantHelper.SetSpellingStatus("big", true, dict);
			Assert.That(dict.Check("big"), Is.True);
			// The standard Enchant fails this test; it is designed to accept title case or all-caps versions of any
			// word that is known correct.
			// For FieldWorks we build a special version that does not.
			// If this test fails, you probably have a new version of Enchant that has not been patched.
			// The required fix (as of version 1.6 of Enchant) is in pwl.c, in the function enchant_pwl_check.
			// Remove the entire block that starts with
			// if(enchant_is_title_case(word, len) || (isAllCaps = enchant_is_all_caps(word, len)))
			Assert.That(dict.Check("Big"), Is.False);
			EnchantHelper.SetSpellingStatus("Big", false, dict);
			Assert.That(dict.Check("Big"), Is.False);
			Assert.That(dict.Check("big"), Is.True);

			// If we set the upper case version only, that is considered correct, but the LC version is not.
			Assert.That(dict.Check("Bother"), Is.False);
			EnchantHelper.SetSpellingStatus("Bother", true, dict);
			Assert.That(dict.Check("Bother"), Is.True);
			Assert.That(dict.Check("bother"), Is.False);

			// Subsequently explicitly setting the LC version to false is not a problem.
			EnchantHelper.SetSpellingStatus("bother", false, dict);
			Assert.That(dict.Check("Bother"), Is.True);
			Assert.That(dict.Check("bother"), Is.False);

			// Now if we set the UC version false, both are.
			EnchantHelper.SetSpellingStatus("Bother", false, dict);
			Assert.That(dict.Check("Bother"), Is.False);
			Assert.That(dict.Check("bother"), Is.False);

			// Now both are explicitly false. Set the LC one to true.
			EnchantHelper.SetSpellingStatus("bother", true, dict);
			Assert.That(dict.Check("Bother"), Is.False);
			Assert.That(dict.Check("bother"), Is.True);

			// Now make the LC one false again.
			EnchantHelper.SetSpellingStatus("bother", false, dict);
			Assert.That(dict.Check("Bother"), Is.False);
			Assert.That(dict.Check("bother"), Is.False);

			// Now make the UC one true again.
			EnchantHelper.SetSpellingStatus("Bother", true, dict);
			Assert.That(dict.Check("Bother"), Is.True);
			Assert.That(dict.Check("bother"), Is.False);
		}
	}
}
}
