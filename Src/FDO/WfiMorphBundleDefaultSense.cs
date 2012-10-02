using System;
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Xml;

namespace SIL.FieldWorks.FDO
{
	class WfiMorphBundleDefaultSenseHandler : BaseFDOPropertyVirtualHandler
	{

		//static string kClassName = "WfiMorphBundle";

		//static string kFieldName = "DefaultSense";

		public WfiMorphBundleDefaultSenseHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
			this.Type = (int)CellarModuleDefns.kcptReferenceAtom;
		}

		///// <summary>
		///// Return the WfiMorphBundleDefaultSenseHandler for the supplied cache, creating it if needed.
		///// </summary>
		///// <param name="cda"></param>
		///// <returns></returns>
		//public static WfiMorphBundleDefaultSenseHandler InstallHandler(IVwCacheDa cda)
		//{
		//    WfiMorphBundleDefaultSenseHandler vh = (WfiMorphBundleDefaultSenseHandler)cda.GetVirtualHandlerName(kClassName, kFieldName);
		//    if (vh == null)
		//    {
		//        vh = new WfiMorphBundleDefaultSenseHandler();
		//        vh.Type = (int)CellarModuleDefns.kcptReferenceAtom;
		//        vh.ClassName = kClassName;
		//        vh.FieldName = kFieldName;
		//        cda.InstallVirtual(vh);
		//    }
		//    return vh;
		//}
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			ISilDataAccess sda = cda as ISilDataAccess;
			int hvoSense = sda.get_ObjectProp(hvo, (int)WfiMorphBundle.WfiMorphBundleTags.kflidSense);
			if (hvoSense == 0)
			{
				// Try for a default.
				int hvoMsa = sda.get_ObjectProp(hvo, (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa);
				if (hvoMsa != 0)
				{
					int hvoEntry = sda.get_ObjectProp(hvoMsa, (int)CmObjectFields.kflidCmObject_Owner);
					if (hvoEntry != 0)
					{
						hvoSense = SenseWithMsa(sda, hvoEntry, (int)LexEntry.LexEntryTags.kflidSenses, hvoMsa);
						if (hvoSense == 0)
						{
							// no sense has right MSA...go for the first sense of any kind.
							int csense = sda.get_VecSize(hvoEntry, (int)LexEntry.LexEntryTags.kflidSenses);
							if (csense > 0)
								hvoSense = sda.get_VecItem(hvoEntry, (int)LexEntry.LexEntryTags.kflidSenses, 0);
						}
					}
				}
			}
			cda.CacheObjProp(hvo, tag, hvoSense);
		}

		/// <summary>
		/// Starting from hvoBase, which may be a LexEntry or LexSense, find the most desirable sense that has
		/// the requested MSA. flidSense should be LexEntry.kflidSenses or LexSense.kflidSenses as
		/// appropriate.
		/// First tries all the top-level senses, if none matches, try children of each sense recursively.
		/// Note: arguably, should try all level-two senses before trying level 3. But level-3 senses are
		/// vanishingly rare; a level 3 sense with a different msa from its parent is so rare that it isn't
		/// worth the effort to be more precise.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvoBase"></param>
		/// <param name="flidSenses"></param>
		/// <param name="hvoMsa"></param>
		/// <returns></returns>
		int SenseWithMsa(ISilDataAccess sda, int hvoBase, int flidSenses, int hvoMsa)
		{
			int csense = sda.get_VecSize(hvoBase, flidSenses);
			for (int i = 0; i < csense; i++)
			{
				int hvoSense = sda.get_VecItem(hvoBase, flidSenses, i);
				int hvoThisMsa = sda.get_ObjectProp(hvoSense, (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis);
				if (hvoThisMsa == hvoMsa)
					return hvoSense;
			}
			for (int i = 0; i < csense; i++)
			{
				int hvoSense = sda.get_VecItem(hvoBase, flidSenses, i);
				int hvoSubSense = SenseWithMsa(sda, hvoSense, (int)LexSense.LexSenseTags.kflidSenses, hvoMsa);
				if (hvoSubSense != 0)
					return hvoSubSense;
			}
			return 0; // no suitable sense found.
		}
	}
}
