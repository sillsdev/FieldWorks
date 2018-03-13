// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Xml;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// this filter passes CmAnnotations which are pointing at objects of the class listed
	/// in the targetClasses attribute.
	/// </summary>
	public class ProblemAnnotationFilter: RecordFilter
	{
		private LcmCache m_cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProblemAnnotationFilter"/> class.
		/// </summary>
		/// <remarks>must have a constructor with no parameters, to use with the dynamic loader
		/// or IPersistAsXml</remarks>
		public ProblemAnnotationFilter()
		{
			ClassIds = new List<int>();
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "classIds", XmlUtils.MakeStringFromList(ClassIds));
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			ClassIds = new List<int>(XmlUtils.GetMandatoryIntegerListAttributeValue(node, "classIds"));
		}

		/// <summary>
		/// Gets the class ids.
		/// </summary>
		public List<int> ClassIds { get; protected set; }

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
			var classList =XmlUtils.GetMandatoryAttributeValue(filterNode, "targetClasses");
			var classes= classList.Split(',');

			//enhance: currently, this will require that we name every subclass as well.
			foreach(var name in classes)
			{
				var cls = cache.DomainDataByFlid.MetaDataCache.GetClassId(name.Trim());
				if (cls <= 0)
				{
					throw new FwConfigurationException("The class name '" + name + "' is not valid");
				}
				ClassIds.Add(cls);
			}
		}

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept (IManyOnePathSortItem item)
		{
			var obj = item.KeyObjectUsing(m_cache);
			if (!(obj is ICmBaseAnnotation))
			{
				return false; // It's not a base annotation
			}

			var annotation = (ICmBaseAnnotation)obj;
			if (annotation.BeginObjectRA == null)
			{
				return false;
			}

			var cls = annotation.BeginObjectRA.ClassID;
			foreach (var i in ClassIds)
			{
				if (i == cls)
				{
					return true;
				}
			}
			return false;
		}
	}
}