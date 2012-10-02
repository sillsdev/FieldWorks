using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace SetEnvHelper
{
	class Program
	{
		static void Main(string[] args)
		{
			// Set environment variable. We do this in a separate app so that
			// it works on Vista where settings an environment variable in the
			// registry requires admin privileges (UAC dialog)

			foreach (string arg in args)
			{
				string[] keyValue = arg.Split('=');
				if (keyValue.Length < 2)
					continue;

				SetVariable(keyValue[0], keyValue[1]);
			}
		}

		private static void SetVariable(string key, string value)
		{
			RegistryKey environment = Registry.LocalMachine.CreateSubKey(
				@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment");
			environment.SetValue(key, value);
		}
	}
}
