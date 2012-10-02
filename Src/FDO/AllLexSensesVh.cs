using System;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// AllLexSensesVh is a virtual property of LexDb that holds all the
	/// senses (recursively, since senses can own more senses).
	/// </summary>
	public class AllLexSensesVh : BaseFDOPropertyVirtualHandler
	{
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public AllLexSensesVh(XmlNode configuration, FdoCache cache) : base(configuration, cache)
		{
			SetAndCheckNames(configuration, "LexDb", "AllSenses");
			Type = (int)CellarModuleDefns.kcptReferenceCollection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// Required method to load the data.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="_cda"></param>
		public override void Load(int hvo, int tag, int ws, SIL.FieldWorks.Common.COMInterfaces.IVwCacheDa _cda)
		{
			// Need to get stem.PartOfSpeech instead of cn.Obj because cn.Obj and cn.Txt may be null!  See LT-6828.
			string sql = "select ls.id, ls.MorphoSyntaxAnalysis, msao.Owner$, msao.class$, ms.txt, ms.fmt, stem.PartOfSpeech, cn.txt from LexSense ls"
				+ " left outer join MultiStr$ ms on ms.obj = ls.id and ms.flid = 5016005 and ms.ws = " + m_cache.DefaultAnalWs
				+ " left outer join MoStemMsa stem on stem.id = ls.MorphoSyntaxAnalysis"
				+ " left outer join CmPossibility_Name cn on stem.PartOfSpeech = cn.obj and cn.ws = " + m_cache.DefaultAnalWs
				+ " left outer join CmObject msao on msao.id = ls.MorphoSyntaxAnalysis";
			IVwOleDbDa dba = m_cache.VwOleDbDaAccessor;
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctObjVec, 0, this.Tag, 0);
			dcs.Push((int)DbColType.koctObj, 1, (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis, 0);
			dcs.Push((int)DbColType.koctObj, 2, (int)CmObjectFields.kflidCmObject_Owner, 0);
			dcs.Push((int)DbColType.koctInt, 2, (int)CmObjectFields.kflidCmObject_Class, 0);
			dcs.Push((int)DbColType.koctMlsAlt, 1, (int)LexSense.LexSenseTags.kflidDefinition, m_cache.DefaultAnalWs);
			dcs.Push((int)DbColType.koctFmt, 1, (int)LexSense.LexSenseTags.kflidDefinition, m_cache.DefaultAnalWs);
			dcs.Push((int)DbColType.koctObj, 2, (int)MoStemMsa.MoStemMsaTags.kflidPartOfSpeech, 0);
			dcs.Push((int)DbColType.koctMltAlt, 7, (int)CmPossibility.CmPossibilityTags.kflidName, m_cache.DefaultAnalWs);
			dba.Load(sql, dcs, hvo, 0, null, false);
		}

		/// <summary>
		/// The list of all senses depends (at least) on the total list of entries, on the senses of each entry,
		/// and on the senses of each sense.
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="hvoChange"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public override bool DoesResultDependOnProp(int hvoObj, int hvoChange, int tag, int ws)
		{
			return (hvoObj == hvoChange && tag == (int)LexDb.LexDbTags.kflidEntries)
				|| tag == (int)LexEntry.LexEntryTags.kflidSenses
				|| tag == (int)LexSense.LexSenseTags.kflidSenses;
		}

	}
}
