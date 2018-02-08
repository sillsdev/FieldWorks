// Copyright (c) 2005-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Generate parts for each value of an object
	/// </summary>
	/// <remarks>Currently this has only been implemented for the phonological features in a phoneme bulk edit.
	/// That is, it generates a part based on a single layout for each item in LangProject.PhFeatureSystemOA.FeaturesOC.
	///  </remarks>
	internal class ObjectValuePartGenerator : PartGenerator
	{
		private ILcmOwningCollection<IFsFeatDefn> m_collectionToGeneratePartsFrom;
		private IOrderedEnumerable<IFsFeatDefn> m_sortedCollection;
		private string m_objectPath;

		public ObjectValuePartGenerator(LcmCache cache, XElement input, XmlVc vc, int rootClassId)
			: base(cache, input, vc, rootClassId)
		{
			m_objectPath = XmlUtils.GetOptionalAttributeValue(input, "objectPath");
			if (m_objectPath == null)
			{
				throw new ArgumentException("ObjectValuePartGenerator expects input to have objectPath attribute.");
			}
			// Enhance: generalize this
			if (m_objectPath == "PhFeatureSystem.Features")
			{
				m_collectionToGeneratePartsFrom = cache.LangProject.PhFeatureSystemOA.FeaturesOC;
				m_sortedCollection = m_collectionToGeneratePartsFrom.OrderBy(s => s.Abbreviation.BestAnalysisAlternative.Text);
			}
		}
		/// <summary>
		/// Generate the nodes that the constructor arguments indicate.
		/// </summary>
		public override XElement[] Generate()
		{
			var ids = FieldIds;
			var result = new XElement[m_collectionToGeneratePartsFrom.Count];
			var iresult = 0;
			// Enhance: generalize this
			foreach (var fsFeatDefn in m_sortedCollection)
			{
				var output = m_source.Clone();
				result[iresult] = output;
				var fieldName = fsFeatDefn.Abbreviation.BestAnalysisAlternative.Text;
				var className = fsFeatDefn.ClassName;
				var labelName = fieldName;
				// generate parts for any given custom layout
				// TODO: generalize the field ids
				GeneratePartsFromLayouts(m_rootClassId, fieldName, PhPhonemeTags.kflidFeatures, ref output);
				ReplaceParamsInAttributes(output, labelName, fieldName, FsFeatDefnTags.kflidName, className);
				iresult++;
			}
			return result;
		}
	}
}