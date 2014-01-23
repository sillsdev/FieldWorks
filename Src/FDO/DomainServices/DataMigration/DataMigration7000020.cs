// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000020.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000020 : IDataMigration
	{
		readonly Guid m_guidNotebook = new Guid("39886581-4dd5-11d4-8078-0000c0fb81b5");
		readonly Guid m_guidListEditor = new Guid("5ea62d01-7a78-11d4-8078-0000c0fb81b5");

		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000019);

			var viewsToDelete = new List<DomainObjectDTO>();
			foreach (var dtoView in repoDto.AllInstancesSansSubclasses("UserView"))
			{
				var xeView = XElement.Parse(dtoView.Xml);
				var xeApp = xeView.Element("App");
				if (xeApp == null)
					continue;
				var val = xeApp.Attribute("val");
				if (val == null)
					continue;
				var guidApp = new Guid(val.Value);
				if (guidApp == m_guidNotebook || guidApp == m_guidListEditor)
					viewsToDelete.Add(dtoView);
			}
			foreach (var dto in viewsToDelete)
				DataMigrationServices.RemoveIncludingOwnedObjects(repoDto, dto, false);
			viewsToDelete.Clear();

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		#endregion
	}
}
