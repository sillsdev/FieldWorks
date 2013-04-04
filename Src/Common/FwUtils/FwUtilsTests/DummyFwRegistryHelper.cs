using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region DummyFwRegistryHelper class
	/// <summary>
	/// Alternative implementation for unit tests
	/// </summary>
	public class DummyFwRegistryHelper : IFwRegistryHelper
	{
		#region IFwRegistryHelper implementation

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool Paratext7orLaterInstalled()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		public RegistryKey FieldWorksRegistryKeyLocalMachine
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		///
		/// </summary>
		public RegistryKey FieldWorksBridgeRegistryKeyLocalMachine
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		///
		/// </summary>
		public RegistryKey FieldWorksRegistryKeyLocalMachineForWriting
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		///
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning a reference")]
		public RegistryKey FieldWorksRegistryKey
		{
			get
			{
				return Registry.CurrentUser.CreateSubKey(
					@"Software\SIL\FieldWorks\UnitTests\DirectoryFinderTests");
			}
		}

		/// <summary>
		///
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning a reference")]
		public RegistryKey FieldWorksVersionlessRegistryKey
		{
			get
			{
				return Registry.CurrentUser.CreateSubKey(
					@"Software\SIL\FieldWorks\UnitTests");
			}
		}

		/// <summary>
		///
		/// </summary>
		public string UserLocaleValueName
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		#endregion

		/// <summary>
		/// For testing the upgrade of user registry keys from FW7 to FW8
		/// </summary>
		public RegistryKey SetupVersion7Settings()
		{
			var version7Key = FieldWorksVersionlessRegistryKey.CreateSubKey("7.0");
			// add some test keys and values here
			const string flexKeyName = "LanguageExplorer";
			const string teKeyName = "TE";
			const string dirName = "TestDir";
			const string crashes = "NumberOfHorrendousCrashes";
			const string valueName3 = "FlexTestValue1";
			const string valueName4 = "FlexTestValue2";
			const string launches = "launches";
			var flexKey = version7Key.CreateSubKey(flexKeyName);
			var teKey = version7Key.CreateSubKey(teKeyName);
			version7Key.SetValue(dirName, "Z:\\somedirectory\\subdir\\subdir\\DontUseThis");
			version7Key.SetValue(crashes, 200);
			flexKey.SetValue(valueName3, 20);
			flexKey.SetValue(valueName4, "somestring");
			flexKey.SetValue(launches, 44);
			teKey.SetValue(crashes, 10);
			teKey.SetValue(dirName, "Z:\\somedirectory");
			return version7Key;
		}
	}
	#endregion
}
