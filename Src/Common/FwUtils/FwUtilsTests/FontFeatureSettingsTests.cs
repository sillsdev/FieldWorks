using System.Linq;
using System.Diagnostics;
using System.IO;
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
		public void Parse_LogsIgnoredInvalidEntries()
		{
			var writer = new StringWriter();
			var listener = new TextWriterTraceListener(writer);
			var previousLevel = FontFeatureSettings.DiagnosticsSwitch.Level;

			try
			{
				FontFeatureSettings.DiagnosticsSwitch.Level = TraceLevel.Warning;
				Trace.Listeners.Add(listener);

				FontFeatureSettings.Parse("smcp=1,bad=2,kern=x,broken");

				listener.Flush();
				var output = writer.ToString();
				Assert.That(output, Does.Contain("Ignored invalid font feature entry 'bad=2'"));
				Assert.That(output, Does.Contain("Ignored invalid font feature entry 'kern=x'"));
				Assert.That(output, Does.Contain("Ignored invalid font feature entry 'broken'"));
			}
			finally
			{
				Trace.Listeners.Remove(listener);
				listener.Dispose();
				FontFeatureSettings.DiagnosticsSwitch.Level = previousLevel;
			}
		}

		[Test]
		public void Parse_AcceptsCustomPrintableAsciiTags()
		{
			var settings = FontFeatureSettings.Parse("!abc=1,a\"b\\=2").ToArray();

			Assert.That(settings.Select(setting => setting.ToString()),
				Is.EqualTo(new[] { "!abc=1", "a\"b\\=2" }));
		}

		[Test]
		public void Normalize_ReturnsDeterministicRendererNeutralString()
		{
			Assert.That(FontFeatureSettings.Normalize(" smcp = 1, kern=0 "), Is.EqualTo("kern=0,smcp=1"));
		}

		[Test]
		public void NormalizePreservingLegacy_PreservesNumericGraphiteFeatureIds()
		{
			Assert.That(FontFeatureSettings.NormalizePreservingLegacy(" 123=1,456=2 "), Is.EqualTo("123=1,456=2"));
		}

		[Test]
		public void NormalizePreservingLegacy_NormalizesOpenTypeFeatures()
		{
			Assert.That(FontFeatureSettings.NormalizePreservingLegacy(" smcp = 1, kern=0 "), Is.EqualTo("kern=0,smcp=1"));
		}

		[Test]
		public void NormalizePreservingLegacy_NormalizesOpenTypeFeaturesThatStartWithPunctuation()
		{
			Assert.That(FontFeatureSettings.NormalizePreservingLegacy(" !abc = 1, kern=0 "),
				Is.EqualTo("!abc=1,kern=0"));
		}
	}
}
