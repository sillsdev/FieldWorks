// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
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
			Status = 0,
			Name,
			Version,
			Description
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
			var process = MiscUtils.RunProcess("dpkg", $"-l '{search}'", exception => { processError = true; });
			if (processError)
			{
				yield break;
			}
			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
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