// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000037.cs
// Responsibility: mcconnel

using System;
using System.IO;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000036 to 7000037.  This fixes a data conversion problem for
	/// externalLink attributes in Run elements coming from FieldWorks 6.0 into FieldWorks 7.0.
	/// See FWR-782 and FWR-3364 for motivation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000037 : IDataMigration
	{
		private static readonly byte[] s_externalLinkTag = Encoding.UTF8.GetBytes("externalLink");

		// Moved here from FWLinkArgs. We don't want it to change here if it is changed there since
		// the data migration will expect it to be what it was at that time, not the new value.
		private const string kFwUrlPrefix = "silfw://localhost/link?";

		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000036);

			string path = domainObjectDtoRepository.ProjectFolder;
			string project = Path.GetFileName(path);

			foreach (var dto in domainObjectDtoRepository.AllInstancesWithValidClasses())
			{
				if (dto.XmlBytes.IndexOfSubArray(s_externalLinkTag) < 0)
					continue;
				XElement xel = XElement.Parse(dto.Xml);
				if (FixExternalLinks(xel, project))
					DataMigrationServices.UpdateDTO(domainObjectDtoRepository, dto, xel.ToString());
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion

		private bool FixExternalLinks(XElement xel, string projectName)
		{
			bool fChanged = false;
			foreach (var xeRun in xel.XPathSelectElements("//Run"))
			{
				var attrLink = xeRun.Attribute("externalLink");
				if (attrLink == null)
					continue;
				string value = attrLink.Value;
				if (value.StartsWith(kFwUrlPrefix.Substring(1)))
					value = kFwUrlPrefix.Substring(0, 1) + value;
				if (!value.StartsWith(kFwUrlPrefix))
					continue;
				string query = HttpUtility.UrlDecode(value.Substring(kFwUrlPrefix.Length));
				string[] rgsProps = query.Split(new char[] {'&'}, StringSplitOptions.RemoveEmptyEntries);
				string database = null;
				int idxDatabase = -1;
				string server = null;
				bool fChange = false;
				for (int i = 0; i < rgsProps.Length; ++i)
				{
					string prop = rgsProps[i];
					string[] propPair = prop.Split('=');
					if (propPair.Length != 2)
					{
						rgsProps[i] = null;
						fChange = true;
						continue;
					}
					switch (propPair[0])
					{
						case "app":
							if (propPair[1] == "Harvest" || propPair[1] == "Language Explorer")
							{
								rgsProps[i] = "app=flex";
								fChange = true;
							}
							break;
						case "database":
							database = propPair[1];
							idxDatabase = i;
							if (propPair[1] != projectName)
							{
								if (propPair[1].ToLowerInvariant() == projectName.ToLowerInvariant() ||
									Path.GetFileName(propPair[1]).ToLowerInvariant() == projectName.ToLowerInvariant())
								{
									database = projectName;
									rgsProps[i] = "database=" + projectName;
									fChange = true;
								}
							}
							break;
						case "server":
							server = propPair[1];
							if (propPair[1] == ".\\SILFW")
							{
								server = String.Empty;
								rgsProps[i] = "server=";
								fChange = true;
							}
							else if (propPair[1].EndsWith("\\SILFW"))
							{
								server = propPair[1].Remove(propPair[1].Length - 6);
								rgsProps[i] = "server=" + server;
								fChange = true;
							}
							break;
					}
				}
				if (String.IsNullOrEmpty(server) && database == projectName && idxDatabase >= 0)
				{
					rgsProps[idxDatabase] = "database=this$";
					fChange = true;
				}
				if (!fChange)
					continue;

				value = kFwUrlPrefix + HttpUtility.UrlEncode(rgsProps.ToString("&"));
				if (attrLink.Value != value)
				{
					attrLink.Value = value;
					fChanged = true;
				}
			}
			return fChanged;
		}
	}
}
