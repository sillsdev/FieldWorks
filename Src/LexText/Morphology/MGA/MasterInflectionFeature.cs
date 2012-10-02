using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	public class MasterInflectionFeature : MasterItem
	{

		public MasterInflectionFeature(XmlNode node, GlossListTreeView.ImageKind kind, string sTerm)
			: base(node, kind, sTerm)
		{
		}

		/// <summary>
		/// figure out if the feature represented by the node is already in the database
		/// </summary>
		/// <param name="cache">database cache</param>
		public override void DetermineInDatabase(FdoCache cache)
		{
			CheckDisposed();

			XmlNode item = m_node.SelectSingleNode(".");
			string sId = XmlUtils.GetOptionalAttributeValue(item, "id");
			if (m_eKind == GlossListTreeView.ImageKind.closedFolder || m_eKind == GlossListTreeView.ImageKind.openFolder)
				m_fInDatabase = false;
			if (KindCanBeInDatabase())
			{
				switch (m_eKind)
				{
					case GlossListTreeView.ImageKind.radio: // fall through
					case GlossListTreeView.ImageKind.radioSelected: // fall through
					case GlossListTreeView.ImageKind.checkBox: // fall through
					case GlossListTreeView.ImageKind.checkedBox:
						// these are all feature values
						m_fInDatabase = FsFeatureSystem.HasSymbolicValue(cache, sId);
						break;
					case GlossListTreeView.ImageKind.complex:
						m_fInDatabase = FsFeatureSystem.HasComplexFeature(cache, sId);
						break;
					case GlossListTreeView.ImageKind.userChoice: // closed feature
						string sStatus = XmlUtils.GetAttributeValue(m_node, "status");
						if (sStatus == "proxy")
						{
							XmlNode xnType = this.m_node.SelectSingleNode("ancestor::item[@type='fsType']/@id");
							if (xnType != null)
							{
								m_fInDatabase = FsFeatureSystem.FsFeatStrucTypeHasFeature(cache, xnType.InnerText, sId) &&
												FsFeatureSystem.HasClosedFeature(cache, sId);
							}
							else
								m_fInDatabase = FsFeatureSystem.HasClosedFeature(cache, sId);
						}
						else
							m_fInDatabase = FsFeatureSystem.HasClosedFeature(cache, sId);
						break;
				}
			}
		}
		public override void AddToDatabase(FdoCache cache)
		{
			CheckDisposed();

			if (m_fInDatabase)
				return; // It's already in the database, so nothing more can be done.

			cache.BeginUndoTask(MGAStrings.ksUndoCreateInflectionFeature,
				MGAStrings.ksRedoCreateInflectionFeature);
			m_featDefn = FsFeatureSystem.AddFeatureAsXml(cache, m_node);
			cache.EndUndoTask();
		}
	}
}
