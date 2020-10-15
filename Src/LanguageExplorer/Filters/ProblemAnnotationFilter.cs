// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// this filter passes CmAnnotations which are pointing at objects of the class listed
	/// in the targetClasses attribute.
	/// </summary>
	internal sealed class ProblemAnnotationFilter : RecordFilter
	{
		private LcmCache m_cache;

		/// <summary />
		internal ProblemAnnotationFilter()
		{
			ClassIds = new List<int>();
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal ProblemAnnotationFilter(XElement element)
			: base(element)
		{
			ClassIds = new List<int>(XmlUtils.GetMandatoryIntegerListAttributeValue(element, "classIds"));
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			XmlUtils.SetAttribute(element, "classIds", XmlUtils.MakeStringFromList(ClassIds));
		}

		/// <summary>
		/// Gets the class ids.
		/// </summary>
		internal List<int> ClassIds { get; }

		public override LcmCache Cache
		{
			set
			{
				m_cache = value;
				base.Cache = value;
			}
		}

		/// <summary>
		/// Initialize the filter
		/// </summary>
		public override void Init(LcmCache cache, XElement filterNode)
		{
			base.Init(cache, filterNode);
			m_cache = cache;
			//enhance: currently, this will require that we name every subclass as well.
			foreach (var name in XmlUtils.GetMandatoryAttributeValue(filterNode, "targetClasses").Split(','))
			{
				var cls = cache.DomainDataByFlid.MetaDataCache.GetClassId(name.Trim());
				if (cls <= 0)
				{
					throw new FwConfigurationException($"The class name '{name}' is not valid");
				}
				ClassIds.Add(cls);
			}
		}

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept(IManyOnePathSortItem item)
		{
			var obj = item.KeyObjectUsing(m_cache);
			var annotation = obj as ICmBaseAnnotation;
			if (annotation?.BeginObjectRA == null)
			{
				return false;
			}
			var cls = annotation.BeginObjectRA.ClassID;
			return ClassIds.Any(i => i == cls);
		}
	}
}