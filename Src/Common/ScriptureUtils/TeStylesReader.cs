// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeStylesReader.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
#if NOTUSED
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Read the TeStyles.xml file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeStylesReader
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the TEStyles.xml file to get the default marker mappings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ReadDefaultMappings(
			Dictionary<string, string> mappings,
			Dictionary<string, string> properties,
			Dictionary<string, string> exclusions)
		{
			XmlDocument doc = new XmlDocument();
			string xmlFileName = Path.Combine(Utils.DirectoryFinder.GetFWCodeSubDirectory("Translation Editor"),
				"TEStyles.xml");
			doc.Load(xmlFileName);
			XmlNode mappingNode = doc.SelectSingleNode("Styles/ImportMappingSets/ImportMapping[@name='TE Default']");
			foreach (XmlNode mapNode in mappingNode.SelectNodes("mapping"))
			{
				string marker = @"\" + mapNode.Attributes["id"].Value;
				string type = mapNode.Attributes["type"].Value;
				if (type == "style")
				{
					string styleName = mapNode.Attributes["styleName"].Value.Replace("_", " ");
					mappings.Add(marker, styleName);
				}
				else if (type == "property")
					properties.Add(marker, mapNode.Attributes["propertyName"].Value);
				else if (type == "excluded")
					exclusions.Add(marker, string.Empty);
			}
		}
	}
#endif
}
