// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>Where View > Show Dictionary Preview is enabled.</summary>
	[TestFixture]
	public class DictionaryPreviewAvailabilityTests
	{
		[Test]
		public void IsOffered_InTheLexiconEditTools_Only()
		{
			Assert.That(DictionaryPreviewAvailability.IsOffered("lexicon", "lexiconEdit"), Is.True);
			Assert.That(DictionaryPreviewAvailability.IsOffered("lexicon", "lexiconFullEdit"), Is.True);
			Assert.That(DictionaryPreviewAvailability.IsOffered("lexicon", "reversalToolEditComplete"), Is.False);
			Assert.That(DictionaryPreviewAvailability.IsOffered("notebook", "lexiconEdit"), Is.False);
			Assert.That(DictionaryPreviewAvailability.IsOffered(null, null), Is.False);
		}
	}
}
