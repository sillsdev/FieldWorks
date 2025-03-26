// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Aga.Controls.Tree;
using SIL.LCModel;

namespace SIL.FieldWorks.IText
{
	public class InflFeatureTreeModel : TreeModel
	{
		private readonly IFsFeatureSystem m_fdoFeatSys;
		private readonly Image m_complexImage;
		private readonly Image m_closedImage;

		public InflFeatureTreeModel(IFsFeatureSystem fdoFeatSys, IDictionary<IFsFeatDefn, object> inflFeats, Image complexImage, Image closedImage)
		{
			m_fdoFeatSys = fdoFeatSys;
			m_complexImage = complexImage;
			m_closedImage = closedImage;
			AddFeatures(Root, m_fdoFeatSys.FeaturesOC, inflFeats);
		}

		private void AddFeatures(Node parent, IEnumerable<IFsFeatDefn> features, IDictionary<IFsFeatDefn, object> values)
		{
			foreach (IFsFeatDefn feature in features)
			{
				var complexFeat = feature as IFsComplexFeature;
				if (complexFeat != null)
				{
					// skip complex features without a type specified
					if (complexFeat.TypeRA == null)
						continue;

					var node = new ComplexFeatureNode(complexFeat) {Image = m_complexImage};
					object value;
					if (values == null || !values.TryGetValue(complexFeat, out value))
						value = null;
					AddFeatures(node, complexFeat.TypeRA.FeaturesRS.Where(f => f != feature), (IDictionary<IFsFeatDefn, object>)value);
					parent.Nodes.Add(node);
				}
				else
				{
					var closedFeat = feature as IFsClosedFeature;
					if (closedFeat != null)
					{
						var node = new ClosedFeatureNode(closedFeat) {Image = m_closedImage};
						object value;
						if (values != null && values.TryGetValue(closedFeat, out value))
						{
							var closedVal = (ClosedFeatureValue) value;
							node.IsChecked = closedVal.Negate;
							node.Value = new SymbolicValue(closedVal.Symbol);
						}
						parent.Nodes.Add(node);
					}
				}
			}
		}

		public void AddInflFeatures(IDictionary<IFsFeatDefn, object> inflFeatures)
		{
			inflFeatures.Clear();
			AddInflFeatures(Root, inflFeatures);
		}

		private void AddInflFeatures(Node node, IDictionary<IFsFeatDefn, object> values)
		{
			foreach (Node child in node.Nodes)
			{
				var complexNode = child as ComplexFeatureNode;
				if (complexNode != null)
				{
					var newValues = new Dictionary<IFsFeatDefn, object>();
					AddInflFeatures(complexNode, newValues);
					if (newValues.Count > 0)
						values[complexNode.Feature] = newValues;
				}
				else
				{
					var closedNode = child as ClosedFeatureNode;
					if (closedNode != null && closedNode.Value.FeatureValue != null)
						values[closedNode.Feature] = new ClosedFeatureValue(closedNode.Value.FeatureValue, closedNode.IsChecked);
				}
			}
		}
	}
}
