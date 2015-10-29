// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	public class MasterPhonologicalFeature : MasterItem
	{
		public MasterPhonologicalFeature(XmlNode node, GlossListTreeView.ImageKind kind, string sTerm)
			: base(node, kind, sTerm)
		{
		}

		public override bool KindCanBeInDatabase()
		{
			return !IsAGroup();
		}

		private bool IsAGroup()
		{
			string sId = XmlUtils.GetManditoryAttributeValue(m_node, "id");
			if (sId.StartsWith("g"))
				return true;
			return false;
		}

		/// <summary>
		/// figure out if the feature represented by the node is already in the database
		/// </summary>
		/// <param name="cache">database cache</param>
		public override void DetermineInDatabase(FdoCache cache)
		{
			//XmlNode item = m_node.SelectSingleNode(".");
			string sId = XmlUtils.GetOptionalAttributeValue(m_node, "id");
			if (IsAGroup())
				m_fInDatabase = false;
			else
				m_fInDatabase = cache.LanguageProject.PhFeatureSystemOA.GetFeature(sId) != null;
		}
		public override void AddToDatabase(FdoCache cache)
		{
			if (m_fInDatabase)
				return; // It's already in the database, so nothing more can be done.

			string sType = XmlUtils.GetManditoryAttributeValue(m_node, "type");
			if (sType == "value")
			{
				UndoableUnitOfWorkHelper.Do(MGAStrings.ksUndoCreatePhonologicalFeature, MGAStrings.ksRedoCreatePhonologicalFeature,
					cache.ActionHandlerAccessor, () =>
					{
						m_featDefn = cache.LangProject.PhFeatureSystemOA.AddFeatureFromXml(m_node);
					});
			}
		}

	}
}
