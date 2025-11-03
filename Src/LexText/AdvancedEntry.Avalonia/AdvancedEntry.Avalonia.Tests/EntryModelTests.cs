using NUnit.Framework;
using AdvancedEntry.Avalonia.Models;

namespace AdvancedEntry.Avalonia.Tests
{
    public class EntryModelTests
    {
        [Test]
        public void Sample_Has_Defaults_And_Validates()
        {
            var m = EntryModel.CreateSample();
            Assert.That(m.LexicalForm, Is.EqualTo("demo"));
            Assert.That(m.MorphType, Is.EqualTo("root"));
            // ValidateAllProperties should not throw and should keep required values present
            Assert.DoesNotThrow(() => m.ValidateAllProperties());
        }
    }
}
