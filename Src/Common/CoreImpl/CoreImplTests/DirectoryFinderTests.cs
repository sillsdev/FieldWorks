using System;
using NUnit.Framework;

namespace SIL.CoreImpl
{
		/// <summary>
		/// Base class for testing CommonApplicationData. This base class deals with setting
		/// and resetting the environment variable.
		/// </summary>
		public class GetCommonAppDataBaseTest
		{
			private string PreviousEnvironment;

			/// <summary>
			/// Setup the tests.
			/// </summary>
			[TestFixtureSetUp]
			public void FixtureSetup()
			{
				DirectoryFinder.ResetStaticVars();
				PreviousEnvironment = Environment.GetEnvironmentVariable("FW_CommonAppData");
				var properties = (PropertyAttribute[])GetType().GetCustomAttributes(typeof(PropertyAttribute), true);
				Assert.That(properties.Length, Is.GreaterThan(0));
				Environment.SetEnvironmentVariable("FW_CommonAppData", (string)properties[0].Properties["Value"]);
			}

			/// <summary>
			/// Reset environment variable to previous value
			/// </summary>
			[TestFixtureTearDown]
			public void FixtureTeardown()
			{
				Environment.SetEnvironmentVariable("FW_CommonAppData", PreviousEnvironment);
			}
		}

		/// <summary>
		/// Tests the GetFolderPath method for CommonApplicationData when no environment variable
		/// is set.
		/// </summary>
		[TestFixture]
		[Property("Value", null)]
		public class GetCommonAppDataNormalTests: GetCommonAppDataBaseTest
		{
			/// <summary>Tests the GetFolderPath method for CommonApplicationData when no environment
			/// variable is set</summary>
			[Test]
			[Platform(Include="Linux", Reason="Test is Linux specific")]
			public void Linux()
			{
				Assert.AreEqual("/var/lib/fieldworks",
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			}

			/// <summary>Tests the GetFolderPath method for CommonApplicationData when no environment
			/// variable is set</summary>
			[Test]
			[Platform(Exclude="Linux", Reason="Test is Windows specific")]
			public void Windows()
			{
				Assert.AreEqual(
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			}
		}

		/// <summary>
		/// Tests the GetFolderPath method for CommonApplicationData when the environment variable
		/// is set.
		/// </summary>
		[TestFixture]
		[Property("Value", "/bla")]
		public class GetCommonAppDataOverrideTests: GetCommonAppDataBaseTest
		{
			/// <summary>Tests the GetFolderPath method for CommonApplicationData when the environment
			/// variable is set</summary>
			[Test]
			[Platform(Include="Linux", Reason="Test is Linux specific")]
			public void Linux()
			{
				Assert.AreEqual("/bla",
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			}

			/// <summary>Tests the GetFolderPath method for CommonApplicationData when the environment
			/// variable is set</summary>
			[Test]
			[Platform(Exclude="Linux", Reason="Test is Windows specific")]
			public void Windows()
			{
				Assert.AreEqual(
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			}
		}
}
