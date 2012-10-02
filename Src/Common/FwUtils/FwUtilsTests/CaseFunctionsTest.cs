using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Utils
{
	/// <summary>
	/// Test the CaseFunctions class.
	/// </summary>
	[TestFixture]
	public class CaseFunctionsTest
	{
		/// <summary>
		///
		/// </summary>
		public CaseFunctionsTest()
		{

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			try
			{
				StringUtils.InitIcuDataDir();
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void TestToLower()
		{
			CaseFunctions cf = new CaseFunctions("en");
			Assert.AreEqual("abc", cf.ToLower("ABC"));
		}
		/// <summary>
		///
		/// </summary>
		[Test]
		public void TestStringCase()
		{
			CaseFunctions cf = new CaseFunctions("en");
			Assert.AreEqual(StringCaseStatus.allLower, cf.StringCase("abc"));
			Assert.AreEqual(StringCaseStatus.allLower, cf.StringCase(""));
			Assert.AreEqual(StringCaseStatus.allLower, cf.StringCase(null));
			Assert.AreEqual(StringCaseStatus.title, cf.StringCase("Abc"));
			Assert.AreEqual(StringCaseStatus.title, cf.StringCase("A"));
			Assert.AreEqual(StringCaseStatus.mixed, cf.StringCase("AbC"));
			Assert.AreEqual(StringCaseStatus.mixed, cf.StringCase("ABC"));
			Assert.AreEqual(StringCaseStatus.mixed, cf.StringCase("aBC"));
			int surrogateUc = 0x10400; // DESERET CAPITAL LETTER LONG I
			int surrogateLc = 0x10428; // DESERET SMALL LETTER LONG I
			string strUcSurrogate = Surrogates.StringFromCodePoint(surrogateUc);
			string strLcSurrogate = Surrogates.StringFromCodePoint(surrogateLc);
			// A single upper case surrogate is treated as title.
			Assert.AreEqual(StringCaseStatus.title, cf.StringCase(strUcSurrogate));
			Assert.AreEqual(StringCaseStatus.title, cf.StringCase(strUcSurrogate + "bc"));
			Assert.AreEqual(StringCaseStatus.mixed, cf.StringCase(strUcSurrogate + "bC"));
			Assert.AreEqual(StringCaseStatus.allLower, cf.StringCase(strLcSurrogate + "bc"));
		}
	}
}
