// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;
using System.Xml.Linq;
using SIL.FieldWorks.Common.RootSites;
using LanguageExplorer.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// SortMethodFinder is an implementation of StringFinder that finds a sort string based
	/// on a given sort method defined on the class of the objects being sorted.  If the
	/// method does not exist, the finder tries the standard method 'SortKey'.  If that
	/// does not exist, fall back to the string derived from the layout.
	/// The string derived from the layout is still used for filtering.
	/// </summary>
	internal class SortMethodFinder : LayoutFinder
	{
		private string m_sMethodName;
		private string m_wsName;
		private int m_ws;

		/// <summary>
		/// normal constructor.
		/// </summary>
		public SortMethodFinder(LcmCache cache, string methodName, string layoutName, XElement colSpec, IApp app)
			: base(cache, layoutName, colSpec, app)
		{
			SortMethod = methodName;
			WritingSystemName = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(colSpec, "ws"));
		}

		/// <summary>
		/// Default constructor for persistence.
		/// </summary>
		public SortMethodFinder()
		{
		}

		private string SortMethod
		{
			set
			{
				m_sMethodName = value ?? string.Empty;
			}
		}

		private string WritingSystemName
		{
			get { return m_wsName; }
			set
			{
				m_wsName = value == string.Empty ? null : value;
			}
		}

		/// <summary>
		/// Gets the sort key by traversing the part tree, calling the sort method at the leaves.
		/// </summary>
		private string[] GetKey(XElement layout, ICmObject cmo, IManyOnePathSortItem item, int pathIndex, bool sortedFromEnd)
		{
			if (layout == null)
			{
				return null;
			}

			switch (layout.Name.LocalName)
			{
				case "obj":
				{
					var flid = GetFlid(layout, cmo.Hvo);
					if (pathIndex != -1 && (pathIndex == item.PathLength || flid != item.PathFlid(pathIndex)))
					{
						// we are now off of the path
						pathIndex = -1;
					}

					var objHvo = m_cache.MainCacheAccessor.get_ObjectProp(cmo.Hvo, flid);
					if (objHvo != 0)
					{
						if (pathIndex != -1 && (pathIndex < item.PathLength - 1 && objHvo == item.PathObject(pathIndex + 1)) || (pathIndex == item.PathLength - 1 && objHvo == item.KeyObject))
						{
							return GetChildObjKey(layout, objHvo, item, pathIndex + 1, sortedFromEnd);
						}

						// we are off of the path
						return GetChildObjKey(layout, objHvo, item, -1, sortedFromEnd);
					}
				}
				break;

				case "seq":
				{
					var flid = GetFlid(layout, cmo.Hvo);
					if (pathIndex != -1 && (pathIndex == item.PathLength || flid != item.PathFlid(pathIndex)))
					{
						// we are now off of the path
						pathIndex = -1;
					}

					var size = m_cache.MainCacheAccessor.get_VecSize(cmo.Hvo, flid);
					StringBuilder sb = null;
					for (var i = 0; i < size; i++)
					{
						var objHvo = m_cache.MainCacheAccessor.get_VecItem(cmo.Hvo, flid, i);
						if (pathIndex != -1
						    && (pathIndex < item.PathLength - 1 && objHvo == item.PathObject(pathIndex + 1))
						    || (pathIndex == item.PathLength - 1 && objHvo == item.KeyObject))
						{
							return GetChildObjKey(layout, objHvo, item, pathIndex + 1, sortedFromEnd);
						}

						// if we are off of the path, we concatenate all vector keys to create an
						// aggregate key
						var childObjKey = GetChildObjKey(layout, objHvo, item, -1, sortedFromEnd);
						if (childObjKey != null)
						{
							if (sb == null)
							{
								sb = new StringBuilder();
							}
							foreach (var subKey in childObjKey)
							{
								sb.Append(subKey);
							}
						}
					}
					if (sb != null)
					{
						return new [] {sb.ToString()};
					}
				}
				break;

				case "layout":
				case "part":
				{
					var partref = XmlUtils.GetOptionalAttributeValue(layout, "ref");
					if (partref != null)
					{
						var part = XmlVc.GetNodeForPart(cmo.Hvo, partref, true, m_sda, m_layouts);
						return GetKey(part, cmo, item, pathIndex, sortedFromEnd);
					}

					foreach (var child in layout.Elements())
					{
						var key = GetKey(child, cmo, item, pathIndex, sortedFromEnd);
						if (key != null)
						{
							return key;
						}
					}
				}
				break;
			}
			return null;
		}

		private string[] GetChildObjKey(XElement layout, int hvo, IManyOnePathSortItem item, int pathIndex, bool sortedFromEnd)
		{
			var childObj = m_cache.ServiceLocator.ObjectRepository.GetObject(hvo);
			var layoutName = XmlUtils.GetOptionalAttributeValue(layout, "layout");
			var part = XmlVc.GetNodeForPart(hvo, layoutName, true, m_sda, m_layouts);
			var key = GetKey(part, childObj, item, pathIndex, sortedFromEnd);
			return key ?? CallSortMethod(childObj, sortedFromEnd);
		}

		/// <summary>
		/// This is a simplified version of XmlVc.GetFlid.
		/// It does not look for a flid attr, nor try to cache the result.
		/// It looks for a "field" property, and optionally a "class" one, and uses them
		/// (or the class of hvo, if "class" is missing) to figure the flid.
		/// Virtual properties are assumed already created.
		/// </summary>
		private int GetFlid(XElement frag, int hvo)
		{
			var stClassName = XmlUtils.GetOptionalAttributeValue(frag, "class");
			var stFieldName = XmlUtils.GetMandatoryAttributeValue(frag, "field");
			if (!string.IsNullOrEmpty(stClassName))
			{
				return m_mdc.GetFieldId(stClassName, stFieldName, true);
			}
			var classId = m_sda.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
			return m_mdc.GetFieldId2(classId, stFieldName, true);
		}

		#region StringFinder Members

		/// <summary>
		/// Get a key from the item.
		/// </summary>
		public override string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			if (item.KeyObject == 0)
			{
				return new string[0];
			}

			// traverse the part tree from the root, the root object and the root layout node should
			// be compatible
			var layout = XmlVc.GetNodeForPart(item.RootObjectHvo, m_layoutName, true, m_sda, m_layouts);
			var rootObject = item.RootObjectUsing(m_cache);
			var key = GetKey(layout, rootObject, item, 0, sortedFromEnd);
			if (key != null)
			{
				return key;
			}
			// the root object sort method is not tried in GetKey
			key = CallSortMethod(rootObject, sortedFromEnd);
			if (key != null)
			{
				return key;
			}
			// try calling the sort method on the key object
			var keyCmObjectUsing = item.KeyObjectUsing(m_cache);
			key = CallSortMethod(keyCmObjectUsing, sortedFromEnd);
			if (key != null)
			{
				return key;
			}
			// Try the default fallback if we can't find the method.
			var firstKey = keyCmObjectUsing.SortKey ?? "";
			if (sortedFromEnd)
			{
				firstKey = TsStringUtils.ReverseString(firstKey);
			}

			return new [] {firstKey, keyCmObjectUsing.SortKey2Alpha};
		}

		/// <summary>
		/// Calls the sort method.
		/// </summary>
		private string[] CallSortMethod(ICmObject cmo, bool sortedFromEnd)
		{
			var typeCmo = cmo.GetType();
			try
			{
				var mi = typeCmo.GetMethod(m_sMethodName);
				if (mi == null)
				{
					return null;
				}

				object obj;
				if (mi.GetParameters().Length == 2)
				{
					// Enhance JohnT: possibly we should seek to evaluate this every time, in case it is a magic WS like
					// "best vernacular". But interpreting those requires a flid, and we don't have one; indeed, the
					// method may retrieve information from several. So we may as well just accept that the fancy ones
					// won't work.
					if (m_ws == 0 && WritingSystemName != null)
					{
						m_ws = WritingSystemServices.InterpretWsLabel(m_cache, WritingSystemName, null, 0, 0, null);
					}
					obj = mi.Invoke(cmo, new object[] { sortedFromEnd, m_ws });
				}
				else
				{
					obj = mi.Invoke(cmo, new object[] { sortedFromEnd });
				}
				if (obj is string)
				{
					return new [] {(string) obj};
				}
				// otherwise assume it already is a string array.
				return (string[]) obj;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			if (!(other is SortMethodFinder))
			{
				return false;
			}
			var smf = (SortMethodFinder)other;
			return m_sMethodName == smf.m_sMethodName && base.SameFinder(other) && m_wsName == smf.m_wsName;
		}

		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "sortmethod", m_sMethodName);
			if (!string.IsNullOrEmpty(m_wsName))
			{
				XmlUtils.SetAttribute(node, "ws", m_wsName);
			}
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			SortMethod = XmlUtils.GetMandatoryAttributeValue(node, "sortmethod");
			WritingSystemName = XmlUtils.GetOptionalAttributeValue(node, "ws", null);
			// Enhance JohnT: if we start using string tables for browse views,
			// we will need a better way to provide one to the Vc we make here.
			// Note: we don't need a top-level spec because we're only going to process one
			// column's worth.
			// we won't dispose of it, so it mustn't make pictures (which we don't need)
			m_vc = new XmlBrowseViewBaseVc
			{
				SuppressPictures = true
			};
		}
		#endregion
	}
}