using System;
using NUnit.Framework;

namespace SIL.CoreImpl
{
	[TestFixture]
	public class DirectoryFinderTests
	{
		private string m_previousEnvironment;

		private void SetupEnvironment(string path)
		{
			m_previousEnvironment = Environment.GetEnvironmentVariable("FW_CommonAppData");
			DirectoryFinder.ResetStaticVars();
			Environment.SetEnvironmentVariable("FW_CommonAppData", path);
		}

		private void ResetEnvironment()
		{
			Environment.SetEnvironmentVariable("FW_CommonAppData", m_previousEnvironment);
		}

		[Test]
		public void GetFolderPath_NoEnvVariableSet()
		{
			try
			{
				SetupEnvironment(null);
				string path = DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
#if __MonoCS__
				Assert.That(path, Is.EqualTo("/var/lib/fieldworks"));
#else
				Assert.That(path, Is.EqualTo(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
#endif
			}
			finally
			{
				ResetEnvironment();
			}
		}

		[Test]
		public void GetFolderPath_EnvVariableSet()
		{
			try
			{
				SetupEnvironment("/bla");
				string path = DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
#if __MonoCS__
				Assert.That(path, Is.EqualTo("/bla"));
#else
				Assert.That(path, Is.EqualTo(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
#endif
			}
			finally
			{
				ResetEnvironment();
			}
		}

	}
}
