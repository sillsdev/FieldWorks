using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using XCore;

namespace RBRExtensions
{
	/// <summary>
	/// XCore listener for the Concorder dlg.
	/// </summary>
	public class ConcorderDlgListener : DlgListenerBase
	{
		/// <summary>
		/// Provide access to the Win32 ::PostMessage() function.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="msg"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		[System.Runtime.InteropServices.DllImport("User32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		public static extern bool PostMessage(IntPtr hWnd, int msg, uint wParam, uint lParam);
		/// <summary>
		/// Provide access to the Win32 ::IsWindow() function.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <returns></returns>
		[System.Runtime.InteropServices.DllImport("User32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		public static extern bool IsWindow(IntPtr hWnd);

		#region Properties

		/// <summary>
		/// Override to get a suitable label.
		/// </summary>
		protected override string PersistentLabel
		{
			get { return "ConcorderDlg"; }
		}

		#endregion Properties

		#region Construction and Initialization

		#endregion Construction and Initialization

		readonly List<IntPtr> m_rghwnd = new List<IntPtr>();

		#region XCORE Message Handlers

		/// <summary>
		/// Launch the Concorder dlg.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnViewConcorder(object argument)
		{
			CheckDisposed();

			// It is modeless, so we don't dispose it here.
			var dlg = new Concorder();
			var xwindow = (XWindow)m_mediator.PropertyTable.GetValue("window");
			dlg.SetDlgInfo(xwindow, m_configurationParameters);
			dlg.Show(xwindow);
			PruneDeadHandles();
			m_rghwnd.Add(dlg.Handle);

			return true;
		}

		/// <summary>
		/// Try to keep list from growing indefinitely as user creates and closes
		/// Concorder dlgs.
		/// </summary>
		private void PruneDeadHandles()
		{
			// deleting from the end of the list should be safe.
			for (var i = m_rghwnd.Count - 1; i >= 0; --i)
			{
				if (!IsWindow(m_rghwnd[i]))
					m_rghwnd.RemoveAt(i);
			}
		}

		/// <summary>
		/// Close any open Concorder dlg.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnCloseConcorder(object argument)
		{
			CheckDisposed();

			foreach (var hwnd in m_rghwnd)
			{
				if (IsWindow(hwnd))
					PostMessage(hwnd, Concorder.WM_BROADCAST_CLOSE_CONCORDER, 0, 0);
			}
			m_rghwnd.Clear();
			return true;
		}

		#endregion XCORE Message Handlers
	}

	/// <summary>
	/// Record list for the concorder dlg.
	/// </summary>
	public class ConcorderRecordList : RecordList
	{
		/// <summary>
		/// Initialze the instance.
		/// </summary>
		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			BaseInit(cache, mediator, recordListNode);
			var mdc = VirtualListPublisher.MetaDataCache;
			m_flid = mdc.GetFieldId2(
				mdc.GetClassId(XmlUtils.GetManditoryAttributeValue(recordListNode, "owner")),
				XmlUtils.GetManditoryAttributeValue(recordListNode, "property"),
				false);

			if (m_propertyName == "AllSenses")
			{
				// Virtual prop of Lex DB, rather than a constant.
				m_owningObject = cache.LanguageProject.LexDbOA;
				return;
			}

			switch (m_flid)
			{
				default:
					if (m_flid == cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries) // not constant, can't be case
					{
						m_owningObject = cache.LanguageProject.LexDbOA;
						break;
					}
					throw new InvalidOperationException("Flid not recognized.");
				case PhPhonDataTags.kflidEnvironments:
					m_owningObject = cache.LanguageProject.PhonologicalDataOA;
					break;
				case RBRDecorator.kflid_Ldb_AllAllomorphsList:
					m_owningObject = cache.LanguageProject.LexDbOA;
					break;
				case RBRDecorator.kflid_Sense_AllWordformClientIDs:
					m_owningObject = cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances().FirstOrDefault();
					break;
				case RBRDecorator.kflid_Cat_AllSenseClientIDs:
					m_owningObject = cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS[0];
					break;
				case RBRDecorator.kflid_Cat_AllMSAClientIDs:
					m_owningObject = cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS[0];
					break;
				case RBRDecorator.kflid_Cat_AllEntryClientIDs:
					m_owningObject = cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS[0];
					break;
				case RBRDecorator.kflid_Cat_AllWordformClientIDs:
					m_owningObject = cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS[0];
					break;
				case RBRDecorator.kflid_Cat_AllCompoundRuleClientIDs:
					m_owningObject = cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS[0];
					break;
				case RBRDecorator.kflid_MoForm_AllWordformClientIDs:
					m_owningObject = cache.ServiceLocator.GetInstance<IMoFormRepository>().AllInstances().FirstOrDefault();
					break;
				case RBRDecorator.kflid_MoForm_AllAdHocRuleClientIDs:
					m_owningObject = cache.ServiceLocator.GetInstance<IMoFormRepository>().AllInstances().FirstOrDefault();
					break;
				case RBRDecorator.kflid_Entry_AllWordformClientIDs:
					m_owningObject = cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().FirstOrDefault();
					break;
				case RBRDecorator.kflid_Env_AllAllomorphClientIDs:
					m_owningObject = cache.ServiceLocator.GetInstance<IPhEnvironmentRepository>().AllInstances().FirstOrDefault();
					break;
				case RBRDecorator.kflid_CmPossibility_AllSenseUsageTypes:
					m_owningObject = cache.LanguageProject.LexDbOA.UsageTypesOA.PossibilitiesOS[0];
					break;
				case RBRDecorator.kflid_CmPossibility_AllSenseStatuses:
					m_owningObject = cache.LanguageProject.StatusOA.PossibilitiesOS[0];
					break;
				case RBRDecorator.kflid_CmPossibility_AllSenseTypes:
					m_owningObject = cache.LanguageProject.LexDbOA.SenseTypesOA.PossibilitiesOS[0];
					break;
			}

			//Sorter = new OccurrenceSorter
			//{
			//    Cache = cache,
			//    SpecialDataAccess = VirtualListPublisher
			//};
		}

		/// <summary></summary>
		public override void ReloadList()
		{

			base.ReloadList();
			if (SortedObjects.Count > 0)
			{
				CurrentIndex = 0;
			}
		}

		/// <summary></summary>
		protected override IEnumerable<int> GetObjectSet()
		{
			return VirtualListPublisher.VecProp(m_owningObject.Hvo, m_flid);
		}

		#region IVwNotifyChange implementation

		/// <summary></summary>
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (m_owningObject != null && hvo == m_owningObject.Hvo && tag == m_flid)
			{
				ReloadList();
			}
		}

		#endregion IVwNotifyChange implementation
	}
}
