// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel.Core.Phonology;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// Describe Error in Environment's explanation: none for a well-formed or empty string,
	/// otherwise the message both detail views show.
	/// </summary>
	[TestFixture]
	public class EnvironmentErrorsTests
	{
		private static PhonEnvRecognizer Recognizer()
			=> new PhonEnvRecognizer(new[] { "a", "b", "i" }, new[] { "C", "V" });

		[TestCase("/_#", TestName = "Describe_IsNull_ForAWellFormedEnvironment")]
		[TestCase("/[C]_a", TestName = "Describe_IsNull_ForAKnownNaturalClass")]
		[TestCase("", TestName = "Describe_IsNull_ForEmptyText")]
		[TestCase(null, TestName = "Describe_IsNull_ForNoText")]
		public void Describe_IsNull(string text)
		{
			Assert.That(EnvironmentErrors.Describe(Recognizer(), text), Is.Null);
		}

		[Test]
		public void Describe_ExplainsAMalformedEnvironment_NamingTheString()
		{
			var message = EnvironmentErrors.Describe(Recognizer(), "/#");

			Assert.That(message, Does.StartWith("There is a problem with this environment string '/#': "),
				"the string is named, followed by a space before the explanation");
			Assert.That(message, Does.Contain("missing underscore"));
		}

		[Test]
		public void Describe_NamesAnUnknownNaturalClass()
		{
			Assert.That(EnvironmentErrors.Describe(Recognizer(), "/[X]_"),
				Does.Contain("The abbreviation for the class 'X' was not found"));
		}
	}
}
