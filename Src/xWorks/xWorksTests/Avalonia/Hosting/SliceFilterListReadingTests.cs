// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Reading a tool's filter list off its configuration: the step between the file on disk and
	/// the ids the composer withholds rows by. Every other test in this area supplies that set by
	/// hand, so without these the whole feature could be a silent no-op and still look green.
	/// </summary>
	[TestFixture]
	public class SliceFilterListReadingTests
	{
		// The filterPath Grammar's Category Edit and Exception "Features" both configure.
		private const string ShippedFilterPath =
			@"Language Explorer\Configuration\Grammar\Edit\DataEntryFilters\basicPlusFilter.xml";

		private static XmlNode Configuration(string attributes)
		{
			var document = new XmlDocument();
			document.LoadXml("<parameters " + attributes + "/>");
			return document.DocumentElement;
		}

		/// <summary>
		/// The whole chain against a real shipped filter file: the attribute name, the path
		/// resolution, the XPath and the id attribute. Asserts CONTAINMENT, so editing that file
		/// does not break the test, but breaking any link in the chain does.
		/// </summary>
		[Test]
		public void AConfiguredFilterPath_YieldsTheIdsThatFileNames()
		{
			var ids = RecordEditView.ReadSliceFilterIds(
				Configuration(@"filterPath=""" + ShippedFilterPath + @""""));

			Assert.That(ids, Does.Contain("CmPossibilityStatus"),
				"the ids the file names must come back, or the composer is handed an empty set "
				+ "and withholds nothing while every other test still passes");
			Assert.That(ids, Does.Contain("CmPossibilityDiscussion")
				.And.Contains("CmPossibilityConfidence")
				.And.Contains("CmPossibilityResearchers")
				.And.Contains("CmPossibilityRestrictions"));
		}

		[Test]
		public void AConfigurationWithNoFilterPath_YieldsNoIds()
		{
			var ids = RecordEditView.ReadSliceFilterIds(Configuration(@"clerk=""entries"""));

			Assert.That(ids, Is.Empty,
				"most tools configure no filter, and they must withhold nothing");
		}

		[Test]
		public void AFilterPathThatResolvesToNothing_YieldsNoIds_WithoutThrowing()
		{
			ISet<string> ids = null;

			Assert.DoesNotThrow(() => ids = RecordEditView.ReadSliceFilterIds(
					Configuration(@"filterPath=""no\such\filter.xml""")),
				"an unreadable filter must not stop the detail view opening");
			Assert.That(ids, Is.Empty);
		}

		[Test]
		public void ANullConfiguration_YieldsNoIds()
		{
			Assert.That(RecordEditView.ReadSliceFilterIds(null), Is.Empty);
		}
	}
}
