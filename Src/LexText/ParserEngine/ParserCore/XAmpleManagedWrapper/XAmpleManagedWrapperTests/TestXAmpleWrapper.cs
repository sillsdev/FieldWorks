using NUnit.Framework;
using XAmpleManagedWrapper;

namespace XAmpleManagedWrapperTests
{

	[TestFixture]
	public class TestXAmpleWrapper: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		protected XAmpleWrapper InitHelper()
		{
			var xAmple = new XAmpleWrapper();
			xAmple.Init("");
			return xAmple;
		}

		protected void LoadFilesHelper(XAmpleWrapper wrapper)
		{
			var tempPath = "../../Src/LexText/ParserEngine/ParserCore/ParserCoreTests/M3ToXAmpleTransformerTestsDataFiles";
			// TODO: use DirectoryFinder.FWCodeDirectory
			var xPath = "../../DistFiles/" + "/Language Explorer/Configuration/Grammar";
			wrapper.LoadFiles(xPath, tempPath, "StemName3");
		}

		[Test]
		// FWNX-556 Reenable after upgrade Linux build machine
		[Category("ByHand")]
		public void TestInit ()
		{
			using (var wrapper = InitHelper())
				Assert.IsNotNull(wrapper);
		}

		[Test]
		// FWNX-556 Reenable after upgrade Linux build machine
		[Category("ByHand")]
		public void TestParseWord()
		{
			using (var xAmple = InitHelper())
			{
			LoadFilesHelper(xAmple);
			string parsedWord = xAmple.ParseWord("Hello");
			Assert.IsNotNull(parsedWord);
			Assert.IsNotEmpty(parsedWord);
		}
		}

		[Test]
		// FWNX-556 Reenable after upgrade Linux build machine
		[Category("ByHand")]
		public void TestTraceWord()
		{
			using (var xAmple = InitHelper())
			{
			LoadFilesHelper(xAmple);
			string tracedWord = xAmple.TraceWord("Hello", "Hello");
			Assert.IsNotNull(tracedWord);
			Assert.IsNotEmpty(tracedWord);
		}
		}

		[Test]
		// FWNX-556 Reenable after upgrade Linux build machine
		[Category("ByHand")]
		public void TestLoadFiles()
		{
			using (var xAmple = InitHelper())
			LoadFilesHelper(xAmple);
		}

		[Test]
		// FWNX-556 Reenable after upgrade Linux build machine
		[Category("ByHand")]
		public void TestSetParameter()
		{
			using (var xAmple = InitHelper())
			xAmple.SetParameter("MaxAnalysesToReturn", "3");
		}

		[Test]
		// FWNX-556 Reenable after upgrade Linux build machine
		[Category("ByHand")]
		public void TestAmpleThreadId()
		{
			using (var xAmple = InitHelper())
			{
#if __MonoCS__
			Assert.AreEqual(0, xAmple.AmpleThreadId);
#else
			Assert.AreNotEqual(0, xAmple.AmpleThreadId);
#endif
		}
	}
}
}
