// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

#if !__MonoCS__

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;

namespace SIL.InstallValidator
{
	[TestFixture]
	public sealed class WixInstallerArtifactsTests
	{
		[Test]
		[Category("InstallerArtifacts")]
		public void InstallerArtifactsExist_AndMsiHasExpectedProperties()
		{
			var repoRoot = FindRepoRoot();
			if (repoRoot == null)
				Assert.Ignore("Could not locate repo root (FieldWorks.sln)." );

			var installerBinDir = Path.Combine(repoRoot, "FLExInstaller", "bin");
			if (!Directory.Exists(installerBinDir))
				Assert.Ignore("FLExInstaller/bin not found; installer likely not built in this checkout.");

			var msiPath = Directory
				.GetFiles(installerBinDir, "FieldWorks.msi", SearchOption.AllDirectories)
				.Where(p => p.EndsWith(Path.Combine("en-US", "FieldWorks.msi"), StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(File.GetLastWriteTimeUtc)
				.FirstOrDefault();

			if (msiPath == null)
				Assert.Ignore("No FieldWorks.msi found under FLExInstaller/bin/**/en-US. Run build.ps1 -BuildInstaller first.");

			Assert.That(new FileInfo(msiPath).Length, Is.GreaterThan(1024 * 1024), "MSI should be > 1MB");

			var msiDir = Path.GetDirectoryName(msiPath);
			Assert.That(msiDir, Is.Not.Null);

			var wixpdbPath = Path.Combine(msiDir!, "FieldWorks.wixpdb");
			Assert.That(File.Exists(wixpdbPath), Is.True, "Expected MSI .wixpdb next to the MSI");

			var bundleDir = Directory.GetParent(msiDir!)?.Parent?.FullName; // .../bin/x64/Debug
			if (!string.IsNullOrWhiteSpace(bundleDir))
			{
				var bundleExe = Path.Combine(bundleDir, "FieldWorksBundle.exe");
				var bundlePdb = Path.Combine(bundleDir, "FieldWorksBundle.wixpdb");
				Assert.That(File.Exists(bundleExe), Is.True, "Expected FieldWorksBundle.exe next to the culture folder");
				Assert.That(File.Exists(bundlePdb), Is.True, "Expected FieldWorksBundle.wixpdb next to the bundle exe");
			}

			using (var msi = MsiDatabase.OpenReadOnly(msiPath))
			{
				Assert.That(msi.GetProperty("ProductName"), Is.EqualTo("FieldWorks Language Explorer"));
				Assert.That(msi.GetProperty("Manufacturer"), Is.EqualTo("SIL International"));

				var productVersion = msi.GetProperty("ProductVersion");
				Assert.That(productVersion, Does.Match(@"^\d+\.\d+\.\d+\.\d+$"));

				var upgradeCode = NormalizeGuidString(msi.GetProperty("UpgradeCode"));
				Assert.That(upgradeCode, Is.EqualTo("1092269F-9EA1-419B-8685-90203F83E254"));
			}
		}

		private static string? FindRepoRoot()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null)
			{
				if (File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
					return dir.FullName;
				dir = dir.Parent;
			}

			return null;
		}

		private static string NormalizeGuidString(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return string.Empty;

			return value.Trim().Trim('{', '}').ToUpperInvariant();
		}

		private sealed class MsiDatabase : IDisposable
		{
			private readonly IntPtr _handle;

			private MsiDatabase(IntPtr handle)
			{
				_handle = handle;
			}

			public static MsiDatabase OpenReadOnly(string msiPath)
			{
				var rc = MsiOpenDatabase(msiPath, (IntPtr)0, out var db);
				if (rc != 0 || db == IntPtr.Zero)
					throw new InvalidOperationException($"MsiOpenDatabase failed ({rc}) for '{msiPath}'.");

				return new MsiDatabase(db);
			}

			public string GetProperty(string name)
			{
				var query = $"SELECT `Value` FROM `Property` WHERE `Property`='{EscapeSqlLiteral(name)}'";
				var rc = MsiDatabaseOpenView(_handle, query, out var view);
				if (rc != 0)
					throw new InvalidOperationException($"MsiDatabaseOpenView failed ({rc}) for query '{query}'.");

				try
				{
					rc = MsiViewExecute(view, IntPtr.Zero);
					if (rc != 0)
						throw new InvalidOperationException($"MsiViewExecute failed ({rc}) for query '{query}'.");

					rc = MsiViewFetch(view, out var record);
					if (rc == 259) // ERROR_NO_MORE_ITEMS
						return string.Empty;
					if (rc != 0)
						throw new InvalidOperationException($"MsiViewFetch failed ({rc}) for query '{query}'.");

					try
					{
						return MsiRecordGetString(record, 1);
					}
					finally
					{
						MsiCloseHandle(record);
					}
				}
				finally
				{
					MsiCloseHandle(view);
				}
			}

			public void Dispose()
			{
				if (_handle != IntPtr.Zero)
					MsiCloseHandle(_handle);
			}

			private static string EscapeSqlLiteral(string value)
			{
				return value.Replace("'", "''");
			}

			private static string MsiRecordGetString(IntPtr record, uint field)
			{
				uint length = 0;
				var rc = MsiRecordGetStringW(record, field, null, ref length);
				if (rc != 0 && rc != 234) // ERROR_MORE_DATA
					throw new InvalidOperationException($"MsiRecordGetStringW failed ({rc}) reading field {field}.");

				var builder = new StringBuilder(checked((int)length + 1));
				rc = MsiRecordGetStringW(record, field, builder, ref length);
				if (rc != 0)
					throw new InvalidOperationException($"MsiRecordGetStringW failed ({rc}) reading field {field}.");

				return builder.ToString();
			}

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			private static extern uint MsiOpenDatabase(string szDatabasePath, IntPtr szPersist, out IntPtr phDatabase);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			private static extern uint MsiDatabaseOpenView(IntPtr hDatabase, string szQuery, out IntPtr phView);

			[DllImport("msi.dll")]
			private static extern uint MsiViewExecute(IntPtr hView, IntPtr hRecord);

			[DllImport("msi.dll")]
			private static extern uint MsiViewFetch(IntPtr hView, out IntPtr phRecord);

			[DllImport("msi.dll", CharSet = CharSet.Unicode)]
			private static extern uint MsiRecordGetStringW(IntPtr hRecord, uint iField, StringBuilder? szValueBuf, ref uint pcchValueBuf);

			[DllImport("msi.dll")]
			private static extern uint MsiCloseHandle(IntPtr hAny);
		}
	}
}

#endif
