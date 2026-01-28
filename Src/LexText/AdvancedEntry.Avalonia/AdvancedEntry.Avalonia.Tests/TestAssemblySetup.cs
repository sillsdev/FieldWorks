using NUnit.Framework;
using SIL.WritingSystems;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[SetUpFixture]
public sealed class TestAssemblySetup
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		Sldr.Initialize();
	}
}
