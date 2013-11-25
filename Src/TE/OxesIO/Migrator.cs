// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Migrator.cs
// Responsibility:
//
// <remarks>
// Although this was developed for use in FieldWorks, it doesn't depend on any FieldWorks
// specific classes, so it should be usable by other projects.
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Xsl;
using System.Xml;
using SIL.Utils;

namespace SIL.OxesIO
{
	/// <summary>
	/// This static class has methods to migrate old versions of OXES files to the current
	/// version.
	/// </summary>
	public class Migrator
	{
		/// <summary>
		/// Check whether this file needs to be migrated.
		/// </summary>
		/// <param name="pathToOxes"></param>
		/// <returns></returns>
		public static bool IsMigrationNeeded(string pathToOxes)
		{
			return (Validator.GetOxesVersion(pathToOxes) != Validator.OxesVersion);
		}

		/// <summary>
		/// Creates a new file migrated to the current version
		/// </summary>
		/// <param name="pathToOriginalOxes"></param>
		/// <returns>the path to the  migrated one, in the same directory</returns>
		public static string MigrateToLatestVersion(string pathToOriginalOxes)
		{
			if (!IsMigrationNeeded(pathToOriginalOxes))
			{
				throw new ArgumentException(OxesIOStrings.ksAlreadyCurrentVersion);
			}

			string sourceVersion = Validator.GetOxesVersion(pathToOriginalOxes);

			string migrationSourcePath = pathToOriginalOxes;
			while (sourceVersion != Validator.OxesVersion)
			{
				string xslName = GetNameOfXsltWhichConvertsFromVersion(sourceVersion);
				string targetVersion = xslName.Split(new[] { '-' })[2];
				targetVersion = targetVersion.Remove(targetVersion.LastIndexOf('.'));
				string migrationTargetPath = String.Format("{0}-{1}", pathToOriginalOxes, targetVersion);
				DoOneMigrationStep(xslName, migrationSourcePath, migrationTargetPath);
				if (migrationSourcePath != pathToOriginalOxes)
					FileUtils.Delete(migrationSourcePath);
				migrationSourcePath = migrationTargetPath;
				sourceVersion = targetVersion;
			}
			return migrationSourcePath;
		}

		private static void DoOneMigrationStep(string xslName, string migrationSourcePath, string migrationTargetPath)
		{
			using (Stream xslstream = Assembly.GetExecutingAssembly().GetManifestResourceStream(xslName))
			{
				XslCompiledTransform xsl = new XslCompiledTransform();
				using (var textReader = new XmlTextReader(xslstream))
				{
					xsl.Load(textReader);
					using (TextReader tr = FileUtils.OpenFileForRead(migrationSourcePath, Encoding.UTF8))
					{
						using (TextWriter tw = FileUtils.OpenFileForWrite(migrationTargetPath, Encoding.UTF8))
						{
							try
							{
								using (var xmlwriter = XmlWriter.Create(tw))
									xsl.Transform(XmlReader.Create(tr), xmlwriter);
							}
							catch (XmlException)
							{
								// XSLTransform crashes when the XML contains bad characters, so we cleanup.
								CleanupXmlAndTryAgain(xsl, migrationSourcePath, migrationTargetPath);
							}
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method cleans up XML to only contain valid XML characters as per the standard.
		/// </summary>
		/// <param name="xsl">The XSL.</param>
		/// <param name="migrationSourcePath">The migration source path.</param>
		/// <param name="migrationTargetPath">The migration target path.</param>
		/// ------------------------------------------------------------------------------------
		private static void CleanupXmlAndTryAgain(XslCompiledTransform xsl,
			string migrationSourcePath, string migrationTargetPath)
		{
			using (StreamReader reader = new StreamReader(migrationSourcePath))
			using (MemoryStream memoryStream = new MemoryStream(10))
			using (StreamWriter writer = new StreamWriter(memoryStream))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
					writer.WriteLine(MiscUtils.CleanupXmlString(line));
				writer.Flush();
				memoryStream.Seek(0, SeekOrigin.Begin);
				using (XmlTextWriter xmlWriter = new XmlTextWriter(migrationTargetPath, Encoding.UTF8))
					xsl.Transform(new XmlTextReader(memoryStream), xmlWriter);
			}
		}

		private static string GetNameOfXsltWhichConvertsFromVersion(string sourceVersion)
		{
			string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
			string xslName = null;
			foreach (string name in resources)
			{
				if (name.EndsWith(".xslt") && name.StartsWith("SIL.OxesIO.OXES-" + sourceVersion + "-"))
				{
					xslName = name;
					break;
				}
			}
			if (xslName == null)
				throw new ApplicationException(String.Format(OxesIOStrings.ksCannotConvertThisVersion,
					sourceVersion, Validator.OxesVersion));
			return xslName;
		}
	}
}
