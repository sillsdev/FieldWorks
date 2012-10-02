namespace XmlUnit.Tests {
	using XmlUnit;
	using NUnit.Framework;
	using System.IO;
	using System.Xml;
	using System.Xml.Schema;

	[TestFixture]
	public class DiffConfigurationTests {
		private static string xmlWithWhitespace = "<elemA>as if<elemB> \r\n </elemB>\t</elemA>";
		private static string xmlWithoutWhitespaceElement = "<elemA>as if<elemB/>\r\n</elemA>";
		private static string xmlWithWhitespaceElement = "<elemA>as if<elemB> </elemB></elemA>";
		private static string xmlWithoutWhitespace = "<elemA>as if<elemB/></elemA>";

		[Test] public void DefaultConfiguredWithGenericDescription() {
			DiffConfiguration diffConfiguration = new DiffConfiguration();
			Assertion.AssertEquals(DiffConfiguration.DEFAULT_DESCRIPTION,
								   diffConfiguration.Description);

			Assertion.AssertEquals(DiffConfiguration.DEFAULT_DESCRIPTION,
								   new XmlDiff("", "").OptionalDescription);
		}

		[Test] public void DefaultConfiguredToUseValidatingParser() {
			DiffConfiguration diffConfiguration = new DiffConfiguration();
			Assertion.AssertEquals(DiffConfiguration.DEFAULT_USE_VALIDATING_PARSER,
								   diffConfiguration.UseValidatingParser);

			FileStream controlFileStream = File.Open(ValidatorTests.VALID_FILE,
													 FileMode.Open, FileAccess.Read);
			FileStream testFileStream = File.Open(ValidatorTests.INVALID_FILE,
												  FileMode.Open, FileAccess.Read);
			try {
				XmlDiff diff = new XmlDiff(new StreamReader(controlFileStream),
										   new StreamReader(testFileStream));
				diff.Compare();
				Assertion.Fail("Expected validation failure");
			} catch (XmlSchemaException e) {
				string message = e.Message; // to prevent 'unused variable' compiler warning
			} finally {
				controlFileStream.Close();
				testFileStream.Close();
			}
		}

		[Test] public void CanConfigureNotToUseValidatingParser() {
			DiffConfiguration diffConfiguration = new DiffConfiguration(false);
			Assertion.AssertEquals(false, diffConfiguration.UseValidatingParser);

			FileStream controlFileStream = File.Open(ValidatorTests.VALID_FILE,
													 FileMode.Open, FileAccess.Read);
			FileStream testFileStream = File.Open(ValidatorTests.INVALID_FILE,
												  FileMode.Open, FileAccess.Read);
			try {
				XmlDiff diff = new XmlDiff(new XmlInput(controlFileStream),
										   new XmlInput(testFileStream),
										   diffConfiguration);
				diff.Compare();
			} catch (XmlSchemaException e) {
				Assertion.Fail("Unexpected validation failure: " + e.Message);
			} finally {
				controlFileStream.Close();
				testFileStream.Close();
			}
		}

		[Test] public void DefaultConfiguredWithWhitespaceHandlingAll() {
			DiffConfiguration diffConfiguration = new DiffConfiguration();
			Assertion.AssertEquals(WhitespaceHandling.All, diffConfiguration.WhitespaceHandling);

			PerformAssertion(xmlWithoutWhitespace, xmlWithWhitespaceElement, false);
			PerformAssertion(xmlWithoutWhitespace, xmlWithoutWhitespaceElement, false);
			PerformAssertion(xmlWithoutWhitespace, xmlWithWhitespace, false);
			PerformAssertion(xmlWithoutWhitespaceElement, xmlWithWhitespaceElement, false);
		}

		private void PerformAssertion(string control, string test, bool assertion) {
			XmlDiff diff = new XmlDiff(control, test);
			PerformAssertion(diff, assertion);
		}
		private void PerformAssertion(string control, string test, bool assertion,
									  DiffConfiguration xmlUnitConfiguration) {
			XmlDiff diff = new XmlDiff(new XmlInput(control), new XmlInput(test),
									   xmlUnitConfiguration);
			PerformAssertion(diff, assertion);
		}
		private void PerformAssertion(XmlDiff diff, bool assertion) {
			Assertion.AssertEquals(assertion, diff.Compare().Equal);
			Assertion.AssertEquals(assertion, diff.Compare().Identical);
		}

		[Test] public void CanConfigureWhitespaceHandlingSignificant() {
			DiffConfiguration xmlUnitConfiguration =
				new DiffConfiguration (WhitespaceHandling.Significant);
			PerformAssertion(xmlWithoutWhitespace, xmlWithWhitespaceElement,
							 true, xmlUnitConfiguration);
			PerformAssertion(xmlWithoutWhitespace, xmlWithoutWhitespaceElement,
							 true, xmlUnitConfiguration);
			PerformAssertion(xmlWithoutWhitespace, xmlWithWhitespace,
							 true, xmlUnitConfiguration);
			PerformAssertion(xmlWithoutWhitespaceElement, xmlWithWhitespaceElement,
							 true, xmlUnitConfiguration);
		}

		[Test] public void CanConfigureWhitespaceHandlingNone() {
			DiffConfiguration xmlUnitConfiguration =
				new DiffConfiguration(WhitespaceHandling.None);
			PerformAssertion(xmlWithoutWhitespace, xmlWithWhitespaceElement,
							 true, xmlUnitConfiguration);
			PerformAssertion(xmlWithoutWhitespace, xmlWithoutWhitespaceElement,
							 true, xmlUnitConfiguration);
			PerformAssertion(xmlWithoutWhitespace, xmlWithWhitespace,
							 true, xmlUnitConfiguration);
			PerformAssertion(xmlWithoutWhitespaceElement, xmlWithWhitespaceElement,
							 true, xmlUnitConfiguration);
		}
	}
}
