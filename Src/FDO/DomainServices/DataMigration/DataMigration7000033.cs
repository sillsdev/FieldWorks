// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000031.cs
// Responsibility: FW Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000032 to 7000033.
	/// </summary>
	///
	/// <remarks>
	/// This migration does not actually change the data file except for updating the version.
	/// It looks for Configuration files ending in _Layouts.xml and fixes any custom field
	/// names used in them.
	///
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000033 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks for Configuration files ending in _Layouts.xml and fixes any custom field
		/// names used in them.
		/// </summary>
		/// <param name="repoDto">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// <remarks>
		/// Implementors of this interface should ensure the Repository's
		/// starting model version number is correct for the step.
		/// Implementors must also increment the Repository's model version number
		/// at the end of its migration work.
		///
		/// The method also should normally modify the xml string(s)
		/// of relevant DTOs, since that string will be used by the main
		/// data migration calling client (ie. BEP).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000032);
			var projectFolder = repoDto.ProjectFolder;
			// This is equivalent to DirectoryFinder.GetConfigSettingsDir(projectFolder) at the time of creating
			// the migration, but could conceivably change later.
			var targetDir = Path.Combine(projectFolder, "ConfigurationSettings");
			if (Directory.Exists(targetDir))
			{
				foreach (var path in Directory.GetFiles(targetDir, "*_Layouts.xml"))
				{
					var root = XElement.Load(path);
					bool fDirty = false;
					foreach (var part in root.Descendants("part"))
					{
						var firstChild = part.FirstNode as XElement;
						if (firstChild == null)
							continue;
						var fieldAttr = firstChild.Attribute("field");
						if (fieldAttr == null)
							continue;
						if (!fieldAttr.Value.StartsWith("custom"))
							continue;
						string tail = fieldAttr.Value.Substring("custom".Length);
						int dummy;
						if (tail.Length > 0 && !Int32.TryParse(tail, out dummy))
							continue; // not a plausible custom field from the old system
						var refAttr = part.Attribute("ref");
						if (refAttr == null || refAttr.Value != "$child")
							continue; // doesn't fit the pattern for generated custom field displays
						var labelAttr = part.Attribute("originalLabel");
						if (labelAttr == null)
						{
							labelAttr = part.Attribute("label");
							if (labelAttr == null)
								continue;
						}
						fieldAttr.Value = labelAttr.Value;
						fDirty = true;
					}
					if (fDirty)
					{
						using (var writer = XmlWriter.Create(path))
						{
							root.WriteTo(writer);
							writer.Close();
						}
					}
				}
			}

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}
		#endregion
	}
}
