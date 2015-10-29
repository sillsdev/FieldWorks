// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace SIL.Utils
{
	/// <summary>
	/// Allows querying for installed linux packages.
	/// </summary>
	public static class LinuxPackageUtils
	{
		internal enum DpkgListFields : int { Status = 0, Name, Version, Description }

		/// <summary>
		/// Find installed packages in the system.
		/// </summary>
		/// <param name="search">
		/// Search string to search for packages. for example "mono*"
		/// </param>
		/// <returns>
		/// Returns a collection of KeyValuePair's (Name, Version)
		/// </returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "StandardOutput is reference")]
		public static IEnumerable<KeyValuePair<string, string>>
			FindInstalledPackages(string search)
		{
			bool processError = false;
			var process = MiscUtils.RunProcess("dpkg", String.Format("-l '{0}'", search),
				(exception) => { processError = true; });
			if (processError)
				yield break;

			string output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			var dpkgListedPackages = output.Split(new string[] {System.Environment.NewLine},
				StringSplitOptions.RemoveEmptyEntries);

			// ii means installed packages with no errors or pending changes.
			const string installedNoErrorState = "ii";

			// Foreach installed package.
			foreach(var s in dpkgListedPackages.Where(x => x.StartsWith(installedNoErrorState)))
			{
				string[] entries = s.Split(new string[] {"  "},
					StringSplitOptions.RemoveEmptyEntries);
				yield return new KeyValuePair<string, string> ( entries[(int)DpkgListFields.Name],
					entries[(int)DpkgListFields.Version]);
			}
		}
	}
}
