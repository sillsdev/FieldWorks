// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.MGA
{
	internal class MasterInflectionFeature : MasterItem
	{

		internal MasterInflectionFeature(XmlNode node, ImageKind kind, string sTerm)
			: base(node, kind, sTerm)
		{
		}

		/// <summary>
		/// figure out if the feature represented by the node is already in the database
		/// </summary>
		/// <param name="cache">database cache</param>
		public override void DetermineInDatabase(LcmCache cache)
		{
			var item = m_node.SelectSingleNode(".");
			var sId = XmlUtils.GetOptionalAttributeValue(item, "id");
			if (m_eKind == ImageKind.closedFolder || m_eKind == ImageKind.openFolder)
			{
				m_fInDatabase = false;
			}
			if (!KindCanBeInDatabase())
			{
				return;
			}
			var featSys = cache.LanguageProject.MsFeatureSystemOA;
			switch (m_eKind)
			{
				case ImageKind.radio: // fall through
				case ImageKind.radioSelected: // fall through
				case ImageKind.checkBox: // fall through
				case ImageKind.checkedBox:
					// these are all feature values
					m_fInDatabase = featSys.GetSymbolicValue(sId) != null;
					break;
				case ImageKind.complex:
					m_fInDatabase = featSys.GetFeature(sId) != null;
					break;
				case ImageKind.userChoice: // closed feature
					var sStatus = XmlUtils.GetOptionalAttributeValue(m_node, "status");
					m_fInDatabase = featSys.GetFeature(sId) != null;
					if (sStatus == "proxy")
					{
						var xnType = m_node.SelectSingleNode("ancestor::item[@type='fsType']/@id");
						if (xnType != null)
						{
							var type = featSys.GetFeatureType(xnType.InnerText);
							m_fInDatabase = type?.GetFeature(sId) != null && m_fInDatabase;
						}
					}
					break;
			}
		}
		public override void AddToDatabase(LcmCache cache)
		{
			if (m_fInDatabase)
			{
				return; // It's already in the database, so nothing more can be done.
			}

			using (var undoHelper = new UndoableUnitOfWorkHelper(
				cache.ServiceLocator.GetInstance<IActionHandler>(),
				MGAStrings.ksUndoCreateInflectionFeature,
				MGAStrings.ksRedoCreateInflectionFeature))
			{
				m_featDefn = cache.LanguageProject.MsFeatureSystemOA.AddFeatureFromXml(m_node);

				// Attempt to add feature to category as an inflectable feature
				var sPosId = XmlUtils.GetOptionalAttributeValue(m_node, "posid");
				var node = m_node;
				while (node.ParentNode != null && sPosId == null)
				{
					node = node.ParentNode;
					sPosId = XmlUtils.GetOptionalAttributeValue(node, "posid");
				}
				foreach (IPartOfSpeech pos in cache.LanguageProject.PartsOfSpeechOA.ReallyReallyAllPossibilities)
				{
					if (pos.CatalogSourceId == sPosId)
					{
						pos.InflectableFeatsRC.Add(m_featDefn);
						break;
					}
				}
				undoHelper.RollBack = false;
			}
		}
	}
}
