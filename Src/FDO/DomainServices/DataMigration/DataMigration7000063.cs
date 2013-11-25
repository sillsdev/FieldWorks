// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000063.cs
// Responsibility: lastufka

using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000062 to 7000063.
	///
	/// Add the LangProject HomographWs property to older projects.
	/// </summary>
	/// <remarks>
	/// N/A
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000063 : IDataMigration
	{
		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000062);

			var wmbLangProjList = domainObjectDtoRepository.AllInstancesSansSubclasses("LangProject");
			var wmbLangProj = wmbLangProjList.First();
			var wmbLangProjElt = XElement.Parse(wmbLangProj.Xml);
			// get the default vernacular ws - it's the 1st in the list of current ones.
			var vernWss = wmbLangProjElt.Element("CurVernWss"); // has to be only one
			// a new project migrates before adding writing systems to the cache, so if there are no CurVernWss, bail out
			if (vernWss != null)
			{
				var vernWssUni = vernWss.Element("Uni"); // only one
				var vernWsList = vernWssUni.Value.Split(' '); // at least one
				var vernDefault = vernWsList[0]; // the default

				// Create the new property
				var sb = new StringBuilder();
				sb.Append("<HomographWs>");
				sb.AppendFormat("<Uni>{0}</Uni>", vernDefault);
				sb.Append("</HomographWs>");
				var hgWsElt = XElement.Parse(sb.ToString());
				wmbLangProjElt.Add(hgWsElt);
				DataMigrationServices.UpdateDTO(domainObjectDtoRepository, wmbLangProj, wmbLangProjElt.ToString());
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		#endregion
	}
}
