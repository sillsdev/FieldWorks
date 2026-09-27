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
		private const string MotifRegistryPath = @"SOFTWARE\SIL\Motif";
		private const string MotifExecutableName = "motif.exe";
		private readonly Func<string> _getEnvironmentDirectory;
		private readonly Func<RegistryView, string> _getRegisteredDirectory;
		private readonly Func<string, bool> _fileExists;

		/// <summary>Creates a resolver for the current machine.</summary>
		public MotifExecutableResolver()
			: this(() => Environment.GetEnvironmentVariable(MotifDirectoryVariable),
				ReadRegisteredDirectory, File.Exists)
		{
		}

		/// <summary>Creates a resolver with injectable machine discovery.</summary>
		/// <param name="getEnvironmentDirectory">Returns the configured Motif directory.</param>
		/// <param name="getRegisteredDirectory">Returns an install directory for a registry
		/// view.</param>
		/// <param name="fileExists">Checks whether a candidate executable exists.</param>
		public MotifExecutableResolver(Func<string> getEnvironmentDirectory,
			Func<RegistryView, string> getRegisteredDirectory, Func<string, bool> fileExists)
		{
			_getEnvironmentDirectory = getEnvironmentDirectory ?? throw new ArgumentNullException(nameof(getEnvironmentDirectory));
			_getRegisteredDirectory = getRegisteredDirectory ?? throw new ArgumentNullException(nameof(getRegisteredDirectory));
			_fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
		}

		/// <summary>Returns the executable path, or null when no configured installation
		/// exists.</summary>
		public string FindExecutable()
		{
			var executable = FindInDirectory(_getEnvironmentDirectory());
			if (executable != null)
				return executable;

			foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
			{
				executable = FindInDirectory(_getRegisteredDirectory(view));
				if (executable != null)
					return executable;
			}

			return null;
		}

		private string FindInDirectory(string directory)
		{
			if (string.IsNullOrWhiteSpace(directory))
				return null;

			var candidate = Path.Combine(directory, MotifExecutableName);
			return _fileExists(candidate) ? Path.GetFullPath(candidate) : null;
		}

		private static string ReadRegisteredDirectory(RegistryView view)
		{
			if (!Platform.IsWindows)
				return null;

			try
			{
				using (var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
				using (var motifKey = localMachine.OpenSubKey(MotifRegistryPath))
				{
					return motifKey?.GetValue("InstallationDir") as string;
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
