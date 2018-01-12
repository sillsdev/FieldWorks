// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.MGA
{
	internal class MasterPhonologicalFeature : MasterItem
	{
		internal MasterPhonologicalFeature(XmlNode node, MGAImageKind kind, string sTerm)
			: base(node, kind, sTerm)
		{
		}

		public override bool KindCanBeInDatabase()
		{
			return !IsAGroup();
		}

		private bool IsAGroup()
		{
			var sId = XmlUtils.GetMandatoryAttributeValue(m_node, "id");
			return sId.StartsWith("g");
		}

		/// <summary>
		/// figure out if the feature represented by the node is already in the database
		/// </summary>
		/// <param name="cache">database cache</param>
		public override void DetermineInDatabase(LcmCache cache)
		{
			var sId = XmlUtils.GetOptionalAttributeValue(m_node, "id");
			m_fInDatabase = !IsAGroup() && cache.LanguageProject.PhFeatureSystemOA.GetFeature(sId) != null;
		}
		public override void AddToDatabase(LcmCache cache)
		{
			if (m_fInDatabase)
			{
				return; // It's already in the database, so nothing more can be done.
			}

			var sType = XmlUtils.GetMandatoryAttributeValue(m_node, "type");
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
