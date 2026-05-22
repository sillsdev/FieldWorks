using System.Linq;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	public class FontFeatureSettingsTests
	{
		[Test]
		public void Parse_ReturnsNormalizedTagValueSettings()
		{
			var settings = FontFeatureSettings.Parse(" smcp = 1, kern=0,cv01=2 ").ToArray();

			Assert.That(settings.Select(setting => setting.ToString()),
				Is.EqualTo(new[] { "cv01=2", "kern=0", "smcp=1" }));
		}

		[Test]
		public void Parse_LastValueWinsForDuplicateTags()
		{
			var settings = FontFeatureSettings.Parse("smcp=1,smcp=0").ToArray();

			Assert.That(settings, Has.Length.EqualTo(1));
			Assert.That(settings[0].ToString(), Is.EqualTo("smcp=0"));
		}

		[Test]
		public void Parse_IgnoresInvalidEntries()
		{
			var settings = FontFeatureSettings.Parse("smcp=1,bad=2,cv01=-1,kern=x,liga=0").ToArray();

			Assert.That(settings.Select(setting => setting.ToString()),
				Is.EqualTo(new[] { "liga=0", "smcp=1" }));
		}

		[Test]
		public void Normalize_ReturnsDeterministicRendererNeutralString()
		{
			Assert.That(FontFeatureSettings.Normalize(" smcp = 1, kern=0 "), Is.EqualTo("kern=0,smcp=1"));
		}
	}
}
