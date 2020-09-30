// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.Controls.XMLViews;
using NUnit.Framework;

namespace LanguageExplorerTests.Controls.XMLViews
{
	/// <summary>
	/// Beginnings (just enough for one bug fix for now) of testing XhtmlHelper
	/// </summary>
	[TestFixture]
	public class XhtmlHelperTests
	{
		private XhtmlHelper _xhtmlHelper;

		[SetUp]
		public void TestSetup()
		{
			_xhtmlHelper = new XhtmlHelper();
		}

		/// <summary />
		[Test]
		public void GetValidCssClassName_ConvertsHash()
		{
			Assert.That(_xhtmlHelper.GetValidCssClassName("LexSense#Class23"), Is.EqualTo("LexSenseNUMBER_SIGNClass23"));
		}

		/// <summary />
		[Test]
		public void GetValidCssClassName_DoesNotConvertNonLeadingHyphens()
		{
			Assert.That(_xhtmlHelper.GetValidCssClassName("LexSense-Class23"), Is.EqualTo("LexSense-Class23"));
		}

		/// <summary />
		[Test]
		public void GetValidCssClassName_FixesLeadingDigit()
		{
			Assert.That(_xhtmlHelper.GetValidCssClassName("23MyClass"), Is.EqualTo("X23MyClass"));
		}

		/// <summary />
		[Test]
		public void GetValidCssClassName_FixesLeadingHyphen()
		{
			Assert.That(_xhtmlHelper.GetValidCssClassName("-MyClass"), Is.EqualTo("X-MyClass"));
		}

		/// <summary />
		[Test]
		public void GetValidCssClassName_FixesLeadingHash()
		{
			Assert.That(_xhtmlHelper.GetValidCssClassName("#MyClass"), Is.EqualTo("NUMBER_SIGNMyClass"));
		}

		/// <summary>This verifies that we store language information using a safe class name,
		/// and as a result can retrieve it both ways (original key or equivalent safe one).</summary>
		[Test]
		public void MapCssToLang_UsesGetValidCssClassName()
		{
			_xhtmlHelper.MapCssToLang("#MyClass", "dummy");
			Assert.That(_xhtmlHelper.TryGetLangsFromCss("#MyClass", out _), Is.True, "unmodified class name should retrieve successfully");
			Assert.That(_xhtmlHelper.TryGetLangsFromCss("NUMBER_SIGNMyClass", out _), Is.True, "corrected class name should also work");
		}
	}
}