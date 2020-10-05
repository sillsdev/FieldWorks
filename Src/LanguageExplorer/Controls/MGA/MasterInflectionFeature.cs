// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Controls.MGA
{
	internal sealed class MasterInflectionFeature : MasterItem
	{
		internal MasterInflectionFeature(XmlNode node, MGAImageKind kind, string sTerm)
			: base(node, kind, sTerm)
		{
		}

		/// <summary>
		/// figure out if the feature represented by the node is already in the database
		/// </summary>
		internal override void DetermineInDatabase(LcmCache cache)
		{
			var item = Node.SelectSingleNode(".");
			var sId = XmlUtils.GetOptionalAttributeValue(item, "id");
			if (m_eKind == MGAImageKind.closedFolder || m_eKind == MGAImageKind.openFolder)
			{
				_inDatabase = false;
			}
			if (!KindCanBeInDatabase())
			{
				return;
			}
			var featSys = cache.LanguageProject.MsFeatureSystemOA;
			switch (m_eKind)
			{
				case MGAImageKind.radio: // fall through
				case MGAImageKind.radioSelected: // fall through
				case MGAImageKind.checkBox: // fall through
				case MGAImageKind.checkedBox:
					// these are all feature values
					_inDatabase = featSys.GetSymbolicValue(sId) != null;
					break;
				case MGAImageKind.complex:
					_inDatabase = featSys.GetFeature(sId) != null;
					break;
				case MGAImageKind.userChoice: // closed feature
					var sStatus = XmlUtils.GetOptionalAttributeValue(Node, "status");
					_inDatabase = featSys.GetFeature(sId) != null;
					if (sStatus == "proxy")
					{
						var xnType = Node.SelectSingleNode("ancestor::item[@type='fsType']/@id");
						if (xnType != null)
						{
							var type = featSys.GetFeatureType(xnType.InnerText);
							_inDatabase = type?.GetFeature(sId) != null && InDatabase;
						}
					}
					break;
			}
		}
		internal override void AddToDatabase(LcmCache cache)
		{
			if (InDatabase)
			{
				return; // It's already in the database, so nothing more can be done.
			}
			using (var undoHelper = new UndoableUnitOfWorkHelper(cache.ServiceLocator.GetInstance<IActionHandler>(), MGAStrings.ksUndoCreateInflectionFeature, MGAStrings.ksRedoCreateInflectionFeature))
			{
				_featureDefn = cache.LanguageProject.MsFeatureSystemOA.AddFeatureFromXml(Node);
				// Attempt to add feature to category as an inflectable feature
				var sPosId = XmlUtils.GetOptionalAttributeValue(Node, "posid");
				var node = Node;
				while (node.ParentNode != null && sPosId == null)
				{
					node = node.ParentNode;
					sPosId = XmlUtils.GetOptionalAttributeValue(node, "posid");
				}
				foreach (IPartOfSpeech pos in cache.LanguageProject.PartsOfSpeechOA.ReallyReallyAllPossibilities)
				{
					if (pos.CatalogSourceId == sPosId)
					{
						pos.InflectableFeatsRC.Add(FeatureDefn);
						break;
					}
				}
				undoHelper.RollBack = false;
			}
		}
	}
}