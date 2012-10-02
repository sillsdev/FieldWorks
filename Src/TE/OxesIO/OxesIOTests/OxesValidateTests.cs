using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using OxesIO;

namespace OxesIO.Tests
{
	/// <summary>
	/// These methods test the methods in the OxesIO.Validator class.
	/// </summary>
	[TestFixture]
	public class OxesValidateTests
	{
		/// <summary>
		/// Check that the embedded RNG schema matches the version number in the C# code.
		/// </summary>
		[Test]
		public void OxesVersion_MatchesEmbeddedSchemaVersion()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(typeof(OxesIO.Validator).Assembly.GetManifestResourceStream("OxesIO.oxes.rng"));
			string sNamespace = String.Format("http://www.wycliffe.net/scripture/namespace/version_{0}", Validator.OxesVersion);
			string query = String.Format("/x:grammar/@ns[.='{0}']", sNamespace);
			XmlNamespaceManager m = new XmlNamespaceManager(doc.NameTable);
			m.AddNamespace("x", "http://relaxng.org/ns/structure/1.0");
			Assert.IsNotNull(doc.SelectSingleNode(query, m));
		}

		/// <summary>
		/// Check that an empty OXES file is invalid.
		/// </summary>
		[Test]
		public void Validate_EmptyFile_Fails()
		{
			string path = TempOxesFiles.EmptyOxesFile(null);
			ValidateAndDelete(path, false);
		}

		/// <summary>
		/// Check that a minimal OXES file is valid.
		/// </summary>
		[Test]
		public void Validate_MinimalFile_Succeeds()
		{
			string path = TempOxesFiles.MinimalValidFile(null);
			ValidateAndDelete(path, true);
		}

		/// <summary>
		/// Check that a minimal OXES file with errors is not valid.
		/// </summary>
		[Test]
		public void Validate_TinyBadFile_Fails()
		{
			string path = TempOxesFiles.BadMinimalFile(null);
			ValidateAndDelete(path, false);
		}

		/// <summary>
		/// Check that a small exported OXES file is valid.
		/// </summary>
		[Test]
		public void Validate_SmallFile_Succeeds()
		{
			string path = TempOxesFiles.SmallValidFile(null);
			ValidateAndDelete(path, true);
		}

		/// <summary>
		/// Check that a file with the wrong version number gives a meaningful error message.
		/// </summary>
		[Test]
		public void WrongVersionNumberGivesHelpfulMessage()
		{
			string path = TempOxesFiles.MinimalVersion107File(null);
			string errors = ValidateAndDelete(path, false);
			Assert.IsTrue(errors.Contains("This file claims to be version"));
		}

		private string ValidateAndDelete(string path, bool fShouldPass)
		{
			string errors;
			try
			{
				errors = Validator.GetAnyValidationErrors(path);
				if (fShouldPass)
				{
					if (errors != null)
					{
						Console.WriteLine(errors);
					}
					Assert.IsNull(errors);
				}
				else
				{
					Assert.IsNotNull(errors);
				}
			}
			finally
			{
				File.Delete(path);
			}
			return errors;
		}

	}
}
