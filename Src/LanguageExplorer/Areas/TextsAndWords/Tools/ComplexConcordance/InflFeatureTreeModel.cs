// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using Aga.Controls.Tree;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	public class InflFeatureTreeModel : TreeModel
	{
		private readonly IFsFeatureSystem m_lcmFeatSys;
		private readonly Image m_complexImage;
		private readonly Image m_closedImage;

		public InflFeatureTreeModel(IFsFeatureSystem lcmFeatSys, IDictionary<IFsFeatDefn, object> inflFeats, Image complexImage, Image closedImage)
		{
			m_lcmFeatSys = lcmFeatSys;
			m_complexImage = complexImage;
			m_closedImage = closedImage;
			AddFeatures(Root, m_lcmFeatSys.FeaturesOC, inflFeats);
		}

		private void AddFeatures(Node parent, IEnumerable<IFsFeatDefn> features, IDictionary<IFsFeatDefn, object> values)
		{
			foreach (var feature in features)
			{
				var complexFeat = feature as IFsComplexFeature;
				if (complexFeat != null)
				{
					// skip complex features without a type specified
					if (complexFeat.TypeRA == null)
					{
						continue;
					}

					var node = new ComplexFeatureNode(complexFeat) { Image = m_complexImage };
					object value;
					if (values == null || !values.TryGetValue(complexFeat, out value))
					{
						value = null;
					}
					AddFeatures(node, complexFeat.TypeRA.FeaturesRS, (IDictionary<IFsFeatDefn, object>)value);
					parent.Nodes.Add(node);
				}
				else
				{
					var closedFeat = feature as IFsClosedFeature;
					if (closedFeat == null)
					{
						continue;
					}
					var node = new ClosedFeatureNode(closedFeat) { Image = m_closedImage };
					object value;
					if (values != null && values.TryGetValue(closedFeat, out value))
					{
						var closedVal = (ClosedFeatureValue)value;
						node.IsChecked = closedVal.Negate;
						node.Value = new SymbolicValue(closedVal.Symbol);
					}
					parent.Nodes.Add(node);
				}
			}
		}

		public void AddInflFeatures(IDictionary<IFsFeatDefn, object> inflFeatures)
		{
			inflFeatures.Clear();
			AddInflFeatures(Root, inflFeatures);
		}

		private static void AddInflFeatures(Node node, IDictionary<IFsFeatDefn, object> values)
		{
			foreach (var child in node.Nodes)
			{
				var complexNode = child as ComplexFeatureNode;
				if (complexNode != null)
				{
					var newValues = new Dictionary<IFsFeatDefn, object>();
					AddInflFeatures(complexNode, newValues);
					if (newValues.Count > 0)
					{
						values[complexNode.Feature] = newValues;
					}
				}
				else
				{
					var closedNode = child as ClosedFeatureNode;
					if (closedNode?.Value.FeatureValue != null)
					{
						values[closedNode.Feature] = new ClosedFeatureValue(closedNode.Value.FeatureValue, closedNode.IsChecked);
					}
				}
			}
		}
	}
}