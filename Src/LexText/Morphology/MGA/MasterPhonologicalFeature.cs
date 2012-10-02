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
	public class MasterPhonologicalFeature : MasterItem
	{
		public MasterPhonologicalFeature(XmlNode node, GlossListTreeView.ImageKind kind, string sTerm)
			: base(node, kind, sTerm)
		{
		}

		public override bool KindCanBeInDatabase()
		{
			CheckDisposed();
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
			CheckDisposed();

			//XmlNode item = m_node.SelectSingleNode(".");
			string sId = XmlUtils.GetOptionalAttributeValue(m_node, "id");
			if (IsAGroup())
				m_fInDatabase = false;
			else
				m_fInDatabase = FsFeatureSystem.HasClosedFeature(cache, sId);
		}
		public override void AddToDatabase(FdoCache cache)
		{
			CheckDisposed();

			if (m_fInDatabase)
				return; // It's already in the database, so nothing more can be done.

			string sType = XmlUtils.GetManditoryAttributeValue(m_node, "type");
			if (sType == "feature")
			{
				cache.BeginUndoTask(MGAStrings.ksUndoCreatePhonologicalFeature,
					MGAStrings.ksRedoCreatePhonologicalFeature);
				ILangProject lp = cache.LangProject;
				IFsFeatureSystem featsys = lp.PhFeatureSystemOA as IFsFeatureSystem;
				// Since phonological features in the chooser only have features and no values,
				// we need to create the positive and negative value nodes
				string sName = XmlUtils.GetManditoryAttributeValue(m_node, "id");
				const string sTemplate =
					"<item id='v{0}Positive' type='value'><abbrev ws='en'>+</abbrev><term ws='en'>positive</term>" +
					"<fs id='v{0}PositiveFS' type='Phon'><f name='{0}'><sym value='+'/></f></fs></item>" +
					"<item id='v{0}Negative' type='value'><abbrev ws='en'>-</abbrev><term ws='en'>negative</term>" +
					"<fs id='v{0}NegativeFS' type='Phon'><f name='{0}'><sym value='-'/></f></fs></item>";
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat(sTemplate, sName.Substring(1));
				m_node.InnerXml += sb.ToString();
				// have to use a ndw document or, for some odd reason, it keeps on using an old value and not the new one...
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(m_node.OuterXml);
				// add positive value; note that the FsFeatDefn will be the same for both
				XmlNode valueNode = doc.SelectSingleNode("//item[contains(@id,'Positive')]");
				m_featDefn = FsFeatureSystem.AddFeatureAsXml(cache, featsys, valueNode);
				// add negative value
				valueNode = doc.SelectSingleNode("//item[contains(@id,'Negative')]");
				m_featDefn = FsFeatureSystem.AddFeatureAsXml(cache, featsys, valueNode);
				cache.EndUndoTask();
			}
		}

	}
}
