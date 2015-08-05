using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class exists to decorate the main SDA for concordance views.
	/// It implements the Occurrences and OccurrencesCount properties for WfiWordform.
	/// </summary>
	public class InterestingTextsDecorator : DomainDataByFlidDecoratorBase, ISetMediator, ISetRootHvo
	{
		private InterestingTextList m_interestingTexts;
		private IFdoServiceLocator m_services;
		private int m_notifieeCount;
		// The object our property belongs to. We consider any object for which we are asked our special
		// property to be the root object.
		private int m_rootHvo;
		public InterestingTextsDecorator(ISilDataAccessManaged domainDataByFlid, XmlNode configurationNode,
			IFdoServiceLocator services)
			: base(domainDataByFlid)
		{
			SetOverrideMdc(new InterestingTextsMdc(base.MetaDataCache as IFwMetaDataCacheManaged));
			m_services = services;
		}

		// Override these methods to notice when we are disconnected and stop receiving notifications
		// from the interesting texts object.
		public override void RemoveNotification(IVwNotifyChange nchng)
		{
			base.RemoveNotification(nchng);
			m_notifieeCount--;
			if (m_notifieeCount <= 0 && m_interestingTexts != null)
			{
				m_interestingTexts.InterestingTextsChanged -= m_interestingTexts_InterestingTextsChanged;
				// Also we need to make sure the InterestingTextsList doesn't do propchanges for us anymore
				// N.B. This avoids LT-12437, but we are assuming that this only gets triggered during Refresh or
				// shutting down the main window, when all the Clerks are being disposed.
				// If a clerk were to be disposed some other time when another clerk was still using the ITL,
				// this would be a bad thing to do.
				base.RemoveNotification(m_interestingTexts);
			}
		}

		public override void AddNotification(IVwNotifyChange nchng)
		{
			base.AddNotification(nchng);
			m_notifieeCount++;
		}

		public void SetMediator(Mediator mediator, IPropertyTable propertyTable)
		{
			m_interestingTexts = GetInterestingTextList(mediator, propertyTable, m_services);
			m_interestingTexts.InterestingTextsChanged += m_interestingTexts_InterestingTextsChanged;
		}

		static string InterestingTextKey = "InterestingTexts";
		static public InterestingTextList GetInterestingTextList(Mediator mediator, IPropertyTable propertyTable, IFdoServiceLocator services)
		{
			var interestingTextList = propertyTable.GetValue<InterestingTextList>(InterestingTextKey, null);
			if (interestingTextList == null)
			{
				interestingTextList = new InterestingTextList(mediator, propertyTable, services.GetInstance<ITextRepository>(),
					services.GetInstance<IStTextRepository>(), services.GetInstance<IScrBookRepository>().AllInstances().Any());
				// Make this list available for other tools in this window, but don't try to persist it.
				propertyTable.SetProperty(InterestingTextKey, interestingTextList, false, false);
				// Since the list hangs around indefinitely, it indefinitely monitors prop changes.
				// I can't find any way to make sure it eventually gets removed from the notification list.
				services.GetInstance<ISilDataAccessManaged>().AddNotification(interestingTextList);
			}
			return interestingTextList;
		}

		void m_interestingTexts_InterestingTextsChanged(object sender, InterestingTextsChangedArgs e)
		{
			if (m_rootHvo != 0)
			{
				m_interestingHvos = null; // recompute on next call
				SendPropChanged(m_rootHvo, kflidInterestingTexts, e.InsertedAt, e.NumberInserted, e.NumberDeleted);
			}
		}

		internal const int kflidInterestingTexts = 899800;

		internal int[] m_interestingHvos;

		int[] GetInterestingTexts()
		{
			if (m_interestingHvos == null)
			{
				//if (m_interestingTexts == null || m_interestingTexts.InterestingTexts.Count() == 0)
				//{
				//    m_interestingHvos = new int[0];
				//    return m_interestingHvos;
				//}
				m_interestingHvos = (from text in m_interestingTexts.InterestingTexts select text.Hvo).ToArray();
			}
			return m_interestingHvos;
		}

		public override int[] VecProp(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidInterestingTexts:
					SetRootHvo(hvo);
					return GetInterestingTexts();
			}
			return base.VecProp(hvo, tag);
		}

		public override int get_VecItem(int hvo, int tag, int index)
		{
			switch (tag)
			{
				case kflidInterestingTexts:
					// Could set m_rootHvo = hvo here, but nothing should call this without first checking the size.
					return GetInterestingTexts()[index];
			}
			return base.get_VecItem(hvo, tag, index);
		}

		public IEnumerable<IStText> ScriptureTexts
		{
			get { return m_interestingTexts.ScriptureTexts; }
		}

		public void SetInterestingTexts(IEnumerable<IStText> newTexts)
		{
			m_interestingTexts.SetInterestingTexts(newTexts);
		}

		public override int get_VecSize(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidInterestingTexts:
					SetRootHvo(hvo);
					return GetInterestingTexts().Length;
			}
			return base.get_VecSize(hvo, tag);
		}

		public void SetRootHvo(int hvo)
		{
			m_rootHvo = hvo;
		}
	}

	public class InterestingTextsMdc : FdoMetaDataCacheDecoratorBase
	{
		public InterestingTextsMdc(IFwMetaDataCacheManaged metaDataCache)
			: base(metaDataCache)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotImplementedException();
		}

		// Not sure which of these we need, do both.
		public override int GetFieldId2(int luClid, string bstrFieldName, bool fIncludeBaseClasses)
		{
			switch (luClid)
			{
				case LangProjectTags.kClassId:
					{
						switch (bstrFieldName)
						{
							case "InterestingTexts":
								return InterestingTextsDecorator.kflidInterestingTexts;
						}
					}
					break;
			}
			return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
		}

		public override int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
		{
			switch (bstrClassName)
			{
				case "LangProject":
					return GetFieldId2(LangProjectTags.kClassId, bstrFieldName, fIncludeBaseClasses);
			}

			return base.GetFieldId(bstrClassName, bstrFieldName, fIncludeBaseClasses);
		}

		public override string GetOwnClsName(int flid)
		{
			switch (flid)
			{
				case InterestingTextsDecorator.kflidInterestingTexts: return "LangProject";
			}
			return base.GetOwnClsName(flid);
		}

		public override int GetDstClsId(int flid)
		{
			switch (flid)
			{
				case InterestingTextsDecorator.kflidInterestingTexts:
					return StTextTags.kClassId;
			}
			return base.GetDstClsId(flid);
		}

		public override string GetFieldName(int flid)
		{
			switch (flid)
			{
				case InterestingTextsDecorator.kflidInterestingTexts: return "InterestingTexts";
			}
			return base.GetFieldName(flid);
		}

		public override int GetFieldType(int flid)
		{
			switch (flid)
			{
				case InterestingTextsDecorator.kflidInterestingTexts:
					return (int)CellarPropertyType.ReferenceSequence;
			}
			return base.GetFieldType(flid);
		}
	}
}
