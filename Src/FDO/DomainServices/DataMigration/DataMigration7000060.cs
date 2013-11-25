// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000044.cs
// Responsibility: mcconnel

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Palaso.WritingSystems.Migration;
using Palaso.WritingSystems.Migration.WritingSystemsLdmlV0To1Migration;
using SIL.FieldWorks.Common.FwUtils;
using System;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Fix the LDML files in the project's local store so all are version 2.
	/// In the process some writing system tags may be changed. Also, there may be some tags
	/// that are not valid for LDML version 2 writing systems, but which don't have corresponding
	/// LDML files in the project's local store. Therefore, whether or not we change a tag in
	/// the store, we need to scan all tags in the project to see whether any need to change.
	/// We never merge two writing systems together in this migration, so if the natural result
	/// of migrating one that needs to change is a duplicate, we append -dupN to the variation
	/// to make it unique.
	/// While we are scanning all the strings, we take the opportunity to remove any empty
	/// multistring alterntives. They are redundant (ignored when reading the object) and
	/// therefore both waste space, and may also confuse things by being left behind if the
	/// user subsequently merges two writing systems. (They get left behind because, not being
	/// read in, they don't show up as an existing alternative, and then there is no change to
	/// their object so no reason to write itout.)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000060 : IDataMigration
	{
		Dictionary<string, string> m_tagMap = new Dictionary<string, string>();
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000059);

			var configFolder = Path.Combine(repoDto.ProjectFolder, DirectoryFinder.ksConfigurationSettingsDir);
			if (Directory.Exists(configFolder)) // Some of Randy's test data doesn't have the config folder, so it crashes here.
			{
				const string layoutSuffix = "_Layouts.xml";
				var filesToRename = Directory.GetFiles(configFolder, "*" + layoutSuffix);
				foreach (var path in filesToRename)
				{
					var newPath = path.Substring(0, path.Length - layoutSuffix.Length) + ".fwlayout";
					File.Move(path, newPath);
				}
			}

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}


	}
}
