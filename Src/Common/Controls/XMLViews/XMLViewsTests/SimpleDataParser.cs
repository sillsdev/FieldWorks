using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace XMLViewsTests
{
	/// <summary>
	/// SimpleDataParser parses a simple XML representation of some FieldWorks-type data
	/// into an IVwCacheDa, with minimal checking.
	/// </summary>
	public class SimpleDataParser
	{
		IFwMetaDataCache m_mdc;
		IVwCacheDa m_cda;
		ISilDataAccess m_sda;
		ILgWritingSystemFactory m_wsf;
		ITsStrFactory m_tsf = TsStrFactoryClass.Create();

		public SimpleDataParser(IFwMetaDataCache mdc, IVwCacheDa cda)
		{
			m_mdc = mdc;
			m_cda = cda;
			m_sda = cda as ISilDataAccess;
			m_wsf = m_sda.WritingSystemFactory;
		}

		public void Parse(string pathname)
		{
			XmlDocument docSrc = new XmlDocument();
			docSrc.Load(pathname);
			Parse(docSrc.DocumentElement);
		}

		public List<int> Parse(XmlNode root)
		{
			List<int> result = new List<int>(root.ChildNodes.Count);
			foreach(XmlNode elt in root.ChildNodes)
			{
				if (elt is XmlComment)
					continue;
				switch(elt.Name)
				{
					case "relatomic":
						SetAtomicRef(elt);
						break;
					case "relseq":
						SetSeqRef(elt);
						break;
					default:
						result.Add(MakeObject(elt));
						break;
				}
			}
			return result;
		}

		void SetAtomicRef(XmlNode elt)
		{
			int src = GetSource(elt);
			int dst = GetDst(elt);
			int flid = GetProp(src, elt);
			m_cda.CacheObjProp(src, flid, dst);
		}

		void SetSeqRef(XmlNode elt)
		{
			int src = GetSource(elt);
			int flid = GetProp(src, elt);
			List<int> dst = new List<int>();
			foreach (XmlNode child in elt.ChildNodes)
			{
				if (child is XmlComment)
					continue;
				dst.Add(GetDst(child));
			}
			m_cda.CacheVecProp(src, flid, dst.ToArray(), dst.Count);
		}

		int GetSource(XmlNode elt)
		{
			return XmlUtils.GetMandatoryIntegerAttributeValue(elt, "src");
		}

		int GetDst(XmlNode elt)
		{
			return XmlUtils.GetMandatoryIntegerAttributeValue(elt, "dst");
		}

		int GetId(XmlNode elt)
		{
			return XmlUtils.GetMandatoryIntegerAttributeValue(elt, "id");
		}

		int GetProp(int hvo, XmlNode elt)
		{
			string propName = XmlUtils.GetManditoryAttributeValue(elt, "prop");
			int clsid = m_sda.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
			return (int)m_mdc.GetFieldId2(clsid, propName, true);
		}

		int MakeObject(XmlNode elt)
		{
			string className = elt.Name;
			int clid = m_mdc.GetClassId(className);
			if (clid == 0)
				throw new Exception("class not found " + className);
			int hvo = GetId(elt);
			m_cda.CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
			foreach (XmlNode child in elt.ChildNodes)
			{
				if (child is XmlComment)
					continue;
				switch(child.Name)
				{
					case "seq":
						AddOwningSeqProp(hvo, child);
						break;
					case "ms":
						AddMultiStringProp(hvo, child);
						break;
					case "obj":
						AddOwningAtomicProp(hvo, child);
						break;
					default:
						throw new Exception("unexpected element " + child.Name + " found in " + className);
				}
			}
			return hvo;
		}

		void AddOwningSeqProp(int hvo, XmlNode seq)
		{
			List<int> items = Parse(seq);
			m_cda.CacheVecProp(hvo, GetProp(hvo, seq), items.ToArray(), items.Count);
		}

		void AddOwningAtomicProp(int hvo, XmlNode objElt)
		{
			List<int> items = Parse(objElt);
			if (items.Count > 1)
				throw new Exception("<obj> element may only contain one object");
			int hvoVal = 0;
			if (items.Count > 0)
				hvoVal = items[0];

			m_cda.CacheObjProp(hvo, GetProp(hvo, objElt), hvoVal);
		}

		ITsString MakeString(int ws, XmlNode elt)
		{
			string val = XmlUtils.GetManditoryAttributeValue(elt, "val");
			return m_tsf.MakeString(val, ws);
		}

		int GetWritingSystem(XmlNode elt)
		{
			string wsId = XmlUtils.GetManditoryAttributeValue(elt, "ws");
			int ws = m_wsf.get_Engine(wsId).Handle;
			if (ws == 0)
				throw new Exception("writing system " + wsId + " not recognized");
			return ws;
		}

		void AddMultiStringProp(int hvo, XmlNode elt)
		{
			int ws = GetWritingSystem(elt);
			m_cda.CacheStringAlt(hvo, GetProp(hvo, elt), ws, MakeString(ws, elt));
		}
	}
}
