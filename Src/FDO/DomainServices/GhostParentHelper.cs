using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// This class helps manage "ghost" virtual properties. The characteristic of such a property is that
	/// it contains a mixture of objects of a 'signature' class (or a subclass) and objects of a 'parent'
	/// class, which are put into the list when they have no children of the signature class in a specified
	/// owning property. Bulk edit operations may insert a suitable child if necessary to set a value for
	/// a parent-type object.
	/// </summary>
	public class GhostParentHelper
	{
		internal IFdoServiceLocator m_services; // think of it as protected, but limited to this assembly.

		// The class of objects that are considered parents (they don't have the basic property we
		// try to set).
		private int m_parentClsid;
		// The property of m_parentClsid that owns signature objects.
		private int m_flidOwning;
		// Index at which to insert a new child; hence indicates type of m_flidOwning.
		private int m_indexToCreate;

		/// <summary>
		/// Returns GPHs for the four properties we currently know about.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="classDotMethod"></param>
		/// <returns></returns>
		public static GhostParentHelper Create(IFdoServiceLocator services, string classDotMethod)
		{
			var result = CreateIfPossible(services, classDotMethod);
			if (result == null)
				throw new ArgumentException("Unexpected field request to GhostParentHelper.Create", "classDotMethod");
			return result;
		}
		/// <summary>
		/// Returns GPHs for the four properties we currently know about, or null if not a known property that has ghosts.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="classDotMethod"></param>
		/// <returns></returns>
		public static GhostParentHelper CreateIfPossible(IFdoServiceLocator services, string classDotMethod)
		{
			switch (classDotMethod)
			{
				case "LexDb.AllPossiblePronunciations":
					return new GhostParentHelper(services, LexEntryTags.kClassId, LexEntryTags.kflidPronunciations);
				case "LexDb.AllPossibleAllomorphs":
					return new GphAllPossibleAllomorphs(services, LexEntryTags.kClassId, LexEntryTags.kflidAlternateForms);
				case "LexDb.AllExampleSentenceTargets":
					return new GhostParentHelper(services, LexSenseTags.kClassId, LexSenseTags.kflidExamples);
				case "LexDb.AllExampleTranslationTargets":
					return new GhostParentHelper(services, LexExampleSentenceTags.kClassId, LexExampleSentenceTags.kflidTranslations);
				case "LexDb.AllComplexEntryRefPropertyTargets":
					return new GphComplexEntries(services);
				case "LexDb.AllVariantEntryRefPropertyTargets":
					return new GphVariants(services);
				default:
					return null;
			}
		}

		/// <summary>
		/// Get the destination class for the specified flid, as used in bulk edit. For this purpose
		/// we need to override the destination class of the fields that have ghost parent helpers,
		/// since the properties hold a mixture of classes and therefore have CmObject as their
		/// signature, but the bulk edit code needs to treat them as having the class they primarily contain.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="listFlid"></param>
		/// <returns></returns>
		public static int GetBulkEditDestinationClass(FdoCache cache, int listFlid)
		{
			int destClass = cache.GetDestinationClass(listFlid);
			if (destClass == 0)
			{
				// May be a special "ghost" property used for bulk edit operations which primarily contains,
				// say, example sentences, but also contains senses and entries so we can bulk edit to senses
				// with no examples and entries with no senses.
				// We don't want to lie to the MDC, but here, we need to treat these properties as having the
				// primary destination class.
				switch (cache.MetaDataCacheAccessor.GetFieldName(listFlid))
				{
					case "AllExampleSentenceTargets":
						return LexExampleSentenceTags.kClassId;
					case "AllPossiblePronunciations":
						return LexPronunciationTags.kClassId;
					case "AllPossibleAllomorphs":
						return MoFormTags.kClassId;
					case "AllExampleTranslationTargets":
						return CmTranslationTags.kClassId;
					case "AllComplexEntryRefPropertyTargets":
					case "AllVariantEntryRefPropertyTargets":
						return LexEntryRefTags.kClassId;
				}
			}
			return destClass;
		}

		/// <summary>
		/// Return a ghost parent helper based on a flid, or null if this flid does not need one.
		/// </summary>
		public static GhostParentHelper CreateIfPossible(IFdoServiceLocator services, int flid)
		{
			var mdc = services.MetaDataCache;
			return CreateIfPossible(services, mdc.GetOwnClsName(flid) + "." + mdc.GetFieldName(flid));
		}

		internal GhostParentHelper(IFdoServiceLocator services, int parentClsid, int flidOwning)
		{
			m_services = services;
			m_parentClsid = parentClsid;
			m_flidOwning = flidOwning;
			var mdc = m_services.GetInstance<IFwMetaDataCacheManaged>();
			TargetClass = mdc.GetDstClsId(flidOwning);
			switch ((CellarPropertyType)mdc.GetFieldType(flidOwning))
			{
				case CellarPropertyType.OwningAtomic:
					m_indexToCreate = -2;
					break;
				case CellarPropertyType.OwningCollection:
					m_indexToCreate = -1;
					break;
				case CellarPropertyType.OwningSequence:
					m_indexToCreate = 0;
					break;
				default:
					throw new InvalidOperationException("can only create objects in owning properties");
			}
		}

		/// <summary>
		/// The class of objects we expect to be the children; the destination class of FlidOwning.
		/// </summary>
		public int TargetClass { get; private set; }

		/// <summary>
		/// Get the object related to hvo that has the basic properties of interest: that is, a child object.
		/// hvo is assumed to be the desired object unless it is of the parent class, in which case,
		/// we return its first child if any, or zero if it has no relevant children.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public int GetOwnerOfTargetProperty(int hvo)
		{
			int hvoTargetOwner = hvo; // by default, we assume hvo is the owner
			if (IsGhostOwnerClass(hvo))
			{
				if (IsGhostOwnerChildless(hvo))
					return 0;
				// this owning object has a child, so get the property from it
				hvoTargetOwner = GetFirstChildFromParent(hvo);
			}
			return hvoTargetOwner;
		}

		/// <summary>
		/// Override if you should always create a particular class.
		/// </summary>
		/// <returns></returns>
		internal virtual int ClassToCreate(int hvoItem, int flidBasicProp)
		{
			var mdc = m_services.GetInstance<IFwMetaDataCacheManaged>();
			return mdc.GetOwnClsId((int)flidBasicProp);
		}

		/// <summary>
		/// Like GetOwnerOfTargetProperty, but will create a child if necessary so as never to return zero.
		/// Caller must ensure we are in a UOW.
		/// </summary>
		public int FindOrCreateOwnerOfTargetProp(int hvoItem, int flidBasicProp)
		{
			int hvoOwnerOfTargetProp = GetOwnerOfTargetProperty(hvoItem);
			if (hvoOwnerOfTargetProp == 0)
			{
				hvoOwnerOfTargetProp = CreateOwnerOfTargetProp(hvoItem, flidBasicProp);
			}
			return hvoOwnerOfTargetProp;
		}

		/// <summary>
		/// create the first child for this ghost owner
		/// </summary>
		internal virtual int CreateOwnerOfTargetProp(int hvoItem, int flidBasicProp)
		{
			int clidCreate = ClassToCreate(hvoItem, flidBasicProp);
			return GetSda().MakeNewObject(clidCreate, hvoItem, m_flidOwning, m_indexToCreate);
		}

		internal ISilDataAccessManaged GetSda()
		{
			return m_services.GetInstance<ISilDataAccessManaged>();
		}

		/// <summary>
		/// Return true if the target object (which must be of the owner class) has no children in the relevant property.
		/// </summary>
		public virtual bool IsGhostOwnerChildless(int hvoItem)
		{
			return IsOwningPropVector() ? GetSda().get_VecSize(hvoItem, m_flidOwning) == 0 :
					GetSda().get_ObjectProp(hvoItem, m_flidOwning) == 0;
		}

		private bool IsOwningPropVector()
		{
			return m_indexToCreate != -2;
		}

		internal virtual int GetFirstChildFromParent(int hvoParent)
		{
			if (IsOwningPropVector())
				return GetSda().get_VecItem(hvoParent, m_flidOwning, 0);
			else
				return GetSda().get_ObjectProp(hvoParent, m_flidOwning);
		}

		/// <summary>
		/// Return true if the object represented by the HVO is of the parent object class.
		/// Enhance JohnT: improve name!
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public bool IsGhostOwnerClass(int hvo)
		{
			return m_parentClsid == m_services.GetObject(hvo).ClassID;
		}

		/// <summary>
		/// Answer the class of parent objects.
		/// </summary>
		public int GhostOwnerClass
		{
			get { return m_parentClsid; }
		}
	}

	/// <summary>
	/// Subclass for LexDb.AllPossibleAllomorphs.
	/// </summary>
	internal class GphAllPossibleAllomorphs : GhostParentHelper
	{
		internal GphAllPossibleAllomorphs(IFdoServiceLocator services, int parentClsid, int flidOwning)
			: base(services, parentClsid, flidOwning)
		{
		}

		/// <summary>
		/// In the case of AllPossibleAllomorphs, the class to create is determined by the owning entry.
		/// </summary>
		internal override int ClassToCreate(int hvoItem, int flidBasicProp)
		{
			var entry = m_services.GetObject(hvoItem) as ILexEntry;
			return entry.GetDefaultClassForNewAllomorph();
		}
	}

	/// <summary>
	/// GhostParentHelper subclass for the complex entry type field.
	/// - a ghost owner is considered childless although it may have variant EntryRefs if it has no complex form ones.
	/// </summary>
	internal class GphComplexEntries : GhostParentHelper
	{
		internal GphComplexEntries(IFdoServiceLocator services)
			: base(services, LexEntryTags.kClassId, LexEntryTags.kflidEntryRefs)
		{
		}

		/// <summary>
		/// Return true if we have no complex form EntryRef.
		/// Although the property for which this GPH is used initially contains only entries
		/// that have no complex form LER, a previous bulk edit might have created one.
		/// </summary>
		public override bool IsGhostOwnerChildless(int hvoItem)
		{
			var le = m_services.GetInstance<ILexEntryRepository>().GetObject(hvoItem);
			return le.EntryRefsOS.Where(ler => ler.RefType == LexEntryRefTags.krtComplexForm).Take(1).Count() == 0;
		}

		/// <summary>
		/// We want specifically the first EntryRef of type complex form.
		/// </summary>
		internal override int GetFirstChildFromParent(int hvoParent)
		{
			var le = m_services.GetInstance<ILexEntryRepository>().GetObject(hvoParent);
			return le.EntryRefsOS.Where(ler => ler.RefType == LexEntryRefTags.krtComplexForm).First().Hvo;
		}

		/// <summary>
		/// Override to make the new object a complex one.
		/// </summary>
		/// <param name="hvoItem"></param>
		/// <param name="flidBasicProp"></param>
		/// <returns></returns>
		internal override int CreateOwnerOfTargetProp(int hvoItem, int flidBasicProp)
		{
			var result = base.CreateOwnerOfTargetProp(hvoItem, flidBasicProp);
			GetSda().SetInt(result, LexEntryRefTags.kflidRefType, LexEntryRefTags.krtComplexForm);
			return result;
		}
	}
	/// <summary>
	/// GhostParentHelper subclass for the complex entry type field.
	/// - a ghost owner is considered childless although it may have variant EntryRefs if it has no complex form ones.
	/// </summary>
	internal class GphVariants : GhostParentHelper
	{
		internal GphVariants(IFdoServiceLocator services)
			: base(services, LexEntryTags.kClassId, LexEntryTags.kflidEntryRefs)
		{
		}

		/// <summary>
		/// Return true if we have no complex form EntryRef.
		/// Although the property for which this GPH is used initially contains only entries
		/// that have no complex form LER, a previous bulk edit might have created one.
		/// </summary>
		public override bool IsGhostOwnerChildless(int hvoItem)
		{
			var le = m_services.GetInstance<ILexEntryRepository>().GetObject(hvoItem);
			return le.EntryRefsOS.Where(ler => ler.RefType == LexEntryRefTags.krtVariant).Take(1).Count() == 0;
		}

		/// <summary>
		/// We want specifically the first EntryRef of type variant.
		/// </summary>
		internal override int GetFirstChildFromParent(int hvoParent)
		{
			var le = m_services.GetInstance<ILexEntryRepository>().GetObject(hvoParent);
			return le.EntryRefsOS.Where(ler => ler.RefType == LexEntryRefTags.krtVariant).First().Hvo;
		}

		/// <summary>
		/// Override to make the new object a complex one.
		/// </summary>
		/// <param name="hvoItem"></param>
		/// <param name="flidBasicProp"></param>
		/// <returns></returns>
		internal override int CreateOwnerOfTargetProp(int hvoItem, int flidBasicProp)
		{
			var result = base.CreateOwnerOfTargetProp(hvoItem, flidBasicProp);
			GetSda().SetInt(result, LexEntryRefTags.kflidRefType, LexEntryRefTags.krtVariant);
			return result;
		}
	}
}
