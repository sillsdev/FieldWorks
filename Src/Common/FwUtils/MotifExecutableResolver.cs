// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using Microsoft.Win32;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>Finds the installed Motif command-line executable.</summary>
	public sealed class MotifExecutableResolver
	{
		private const string MotifDirectoryVariable = "MOTIF_DIR";
		private const string MotifRegistryPath = @"Software\SIL\Motif";
		private const string MotifExecutableName = "motif.exe";
		private readonly Func<string> _getEnvironmentDirectory;
		private readonly Func<string> _getRegisteredExecutable;
		private readonly Func<string, bool> _fileExists;

		/// <summary>Creates a resolver for the current machine.</summary>
		public MotifExecutableResolver()
			: this(() => Environment.GetEnvironmentVariable(MotifDirectoryVariable),
				ReadRegisteredExecutable, File.Exists)
		{
		}

		/// <summary>Creates a resolver with injectable machine discovery.</summary>
		/// <param name="getEnvironmentDirectory">Returns the configured Motif directory.</param>
		/// <param name="getRegisteredExecutable">Returns the executable path in Motif's per-user
		/// install record.</param>
		/// <param name="fileExists">Checks whether a candidate executable exists.</param>
		public MotifExecutableResolver(Func<string> getEnvironmentDirectory,
			Func<string> getRegisteredExecutable, Func<string, bool> fileExists)
		{
			_getEnvironmentDirectory = getEnvironmentDirectory ?? throw new ArgumentNullException(nameof(getEnvironmentDirectory));
			_getRegisteredExecutable = getRegisteredExecutable ?? throw new ArgumentNullException(nameof(getRegisteredExecutable));
			_fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
		}

		/// <summary>Returns the executable path, or null when no configured installation
		/// exists.</summary>
		public string FindExecutable()
		{
			var executable = FindInDirectory(_getEnvironmentDirectory());
			if (executable != null)
				return executable;

			var registered = _getRegisteredExecutable();
			if (string.IsNullOrWhiteSpace(registered) || !_fileExists(registered))
				return null;
			return Path.GetFullPath(registered);
		}

		private string FindInDirectory(string directory)
		{
			if (string.IsNullOrWhiteSpace(directory))
				return null;

			var candidate = Path.Combine(directory, MotifExecutableName);
			return _fileExists(candidate) ? Path.GetFullPath(candidate) : null;
		}

		// Motif installs per user, so its install hook writes this record under HKCU, never HKLM.
		private static string ReadRegisteredExecutable()
		{
			if (!Platform.IsWindows)
				return null;

			try
			{
				using (var motifKey = Registry.CurrentUser.OpenSubKey(MotifRegistryPath))
				{
					return motifKey?.GetValue("CliPath") as string;
				}
			}
			catch (Exception error) when (error is IOException ||
				error is UnauthorizedAccessException || error is System.Security.SecurityException ||
				error is PlatformNotSupportedException)
			{
				return null;
			}
		}
	}
}
