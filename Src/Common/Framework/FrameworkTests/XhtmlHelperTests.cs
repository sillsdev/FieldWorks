using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Beginnings (just enough for one bug fix for now) of testing XhtmlHelper
	/// </summary>
	public class XhtmlHelperTests
	{
		/// <summary></summary>
		[Test]
		public void GetValidCssClassName_ConvertsHash()
		{
			Assert.That(new XhtmlHelper().GetValidCssClassName("LexSense#Class23"), Is.EqualTo("LexSenseNUMBER_SIGNClass23"));
		}

		/// <summary></summary>
		[Test]
		public void GetValidCssClassName_DoesNotConvertNonLeadingHyphens()
		{
			Assert.That(new XhtmlHelper().GetValidCssClassName("LexSense-Class23"), Is.EqualTo("LexSense-Class23"));
		}

		/// <summary></summary>
		[Test]
		public void GetValidCssClassName_FixesLeadingDigit()
		{
			Assert.That(new XhtmlHelper().GetValidCssClassName("23MyClass"), Is.EqualTo("X23MyClass"));
		}

		/// <summary></summary>
		[Test]
		public void GetValidCssClassName_FixesLeadingHyphen()
		{
			Assert.That(new XhtmlHelper().GetValidCssClassName("-MyClass"), Is.EqualTo("X-MyClass"));
		}

		/// <summary></summary>
		[Test]
		public void GetValidCssClassName_FixesLeadingHash()
		{
			Assert.That(new XhtmlHelper().GetValidCssClassName("#MyClass"), Is.EqualTo("NUMBER_SIGNMyClass"));
		}

		/// <summary>This verifies that we store language information using a safe class name,
		/// and as a result can retrieve it both ways (original key or equivalent safe one).</summary>
		[Test]
		public void MapCssToLang_UsesGetValidCssClassName()
		{
			var helper = new XhtmlHelper();
			helper.MapCssToLang("#MyClass", "dummy");
			List<string> output;

			Assert.That(helper.TryGetLangsFromCss("#MyClass", out output), Is.True, "unmodified class name should retrieve successfully");
			Assert.That(helper.TryGetLangsFromCss("NUMBER_SIGNMyClass", out output), Is.True, "corrected class name should also work");
		}
	}
}
