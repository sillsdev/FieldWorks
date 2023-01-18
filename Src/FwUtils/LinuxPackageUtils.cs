// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Extensions;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Allows querying for installed linux packages.
	/// </summary>
	public static class LinuxPackageUtils
	{
		private enum DpkgListFields
		{
			Name = 1,
			Version = 2
		}

		/// <summary>
		/// Find installed packages in the system.
		/// </summary>
		/// <param name="search">
		/// Search string to search for packages. for example "mono*"
		/// </param>
		/// <returns>
		/// Returns a collection of KeyValuePair's (Name, Version)
		/// </returns>
		public static IEnumerable<KeyValuePair<string, string>> FindInstalledPackages(string search)
		{
			var processError = false;
			string output;
			using (var process = new Process())
			{
				process.RunProcess("dpkg", $"-l '{search}'",
					exception => { processError = true; }, true);
				if (processError)
				{
					yield break;
				}

				output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();
			}

			var dpkgListedPackages = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			// ii means installed packages with no errors or pending changes.
			const string installedNoErrorState = "ii";

			// Foreach installed package.
			foreach (var s in dpkgListedPackages.Where(x => x.StartsWith(installedNoErrorState)))
			{
				var entries = s.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
				yield return new KeyValuePair<string, string>(entries[(int)DpkgListFields.Name], entries[(int)DpkgListFields.Version]);
			}
		}
	}
}