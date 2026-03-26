// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.AlloGenModel;
using SIL.FieldWorks.Common.FwUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace SIL.AlloGenService
{
	public class DatabaseMigrator
	{
		// is public and can be changed for testing purposes
		public int LatestVersion { get; set; } = 6;

		void MakeBackupOfFile(string fileName)
		{
			if (File.Exists(fileName))
			{
				string backupName = CreateBackupFileName(fileName);
				File.Copy(fileName, backupName, true);
			}
		}

		public string CreateBackupFileName(string fileName)
		{
			string backupName = "";
			if (fileName.Length > 4)
			{
				int iAgf = fileName.LastIndexOf(".agf");
				if (iAgf > -1)
				{
					backupName = fileName.Substring(0, iAgf) + ".bak";
				}
				else
				{
					backupName = fileName + ".bak";
				}
			}
			return backupName;
		}

		public string Migrate(string fileName)
		{
			if (!File.Exists(fileName))
			{
				// effectively do nothing
				return fileName;
			}
			int version = GetFileVersionNumber(fileName);
			if (version < 0)
				return fileName;
			string newFileName = "";
			bool didMigration = false;
			while (version < LatestVersion)
			{
				MakeBackupOfFile(fileName);
				switch (version)
				{
					case 3:
						newFileName = ApplyTransform(fileName, "DBVersion3To4.xslt", "04");
						didMigration = true;
						break;
					default:
						Console.WriteLine("Migrator: version=" + version);
						break;
				}
				version++;
			}
			if (didMigration)
				return newFileName;
			else
				return fileName;
		}

		private string ApplyTransform(string fileName, string transform, string newVersion)
		{
			XPathDocument myXPathDoc = new XPathDocument(fileName);
			XslCompiledTransform myXslTrans = new XslCompiledTransform();
			string migrationStyleSheet = Path.Combine(FwDirectoryFinder.SourceDirectory, "Utilities", "AlloVarGen", "AlloGenService", "AlloGenDataMigrations", transform);

			myXslTrans.Load(migrationStyleSheet);
			string resultFile = Path.Combine(
				Path.GetTempPath(),
				String.Concat("AlloGenMigrationTo", newVersion, ".agf")
			);

			XmlTextWriter myWriter = new XmlTextWriter(resultFile, null);
			myXslTrans.Transform(myXPathDoc, null, myWriter);
			myWriter.Close();
			return resultFile;
		}

		private static string GetAppBaseDir()
		{
			Uri uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
			string rootdir = Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
			return rootdir;
		}

		private int GetFileVersionNumber(string fileName)
		{
			if (!File.Exists(fileName))
				return -1;
			string contents = File.ReadAllText(fileName);
			int index = contents.IndexOf("dbVersion=\"");
			if (index < 0)
				return -1;
			index += 11;
			int indexEnd = contents.Substring(index).IndexOf("\"");
			string value = contents.Substring(index, indexEnd);
			return Int32.Parse(value);
		}

	}
}
