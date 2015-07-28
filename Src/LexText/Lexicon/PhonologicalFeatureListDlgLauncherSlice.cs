// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class PhonologicalFeatureListDlgLauncherSlice : ViewSlice
	{
		private int m_flid;
		IFsFeatStruc m_fs;

		public PhonologicalFeatureListDlgLauncherSlice()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_fs != null && m_fs.IsValidObject && ((m_fs.FeatureSpecsOC == null) || (m_fs.FeatureSpecsOC.Count < 1)) )
				{
					// At some point we will hopefully be able to convert this slice to a true ghost slice
					// so that we aren't creating and deleting database objects unless needed. At that
					// point this can be removed as well as removing the kludge in
					// CreateModifyTimeManager PropChanged that was needed to keep this trick from
					// messing up modify times on entries.
					RemoveFeatureStructureFromOwner(); // it's empty so don't bother keeping it
				}
			}
			base.Dispose(disposing);
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// PhonologicalFeatureListDlgLauncherSlice
			//
			this.Name = "PhonologicalFeatureListDlgLauncherSlice";
			this.Size = new System.Drawing.Size(208, 32);

		}
		#endregion

		public override RootSite RootSite
		{
			get
			{
				return ((PhonologicalFeatureListDlgLauncher)Control).MainControl as RootSite;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="parent"></param>
		public override void Install(DataTree parent)
		{
			CheckDisposed();

			base.Install(parent);

			var ctrl = (PhonologicalFeatureListDlgLauncher)Control;

			m_flid = GetFlid(m_configurationNode, m_obj);
			if (m_flid != 0)
				m_fs = GetFeatureStructureFromOwner(m_obj, m_flid);
			else
			{
				m_fs = m_obj as IFsFeatStruc;
				m_flid = FsFeatStrucTags.kflidFeatureSpecs;
			}

			ctrl.Initialize((FdoCache)Mediator.PropertyTable.GetValue("cache"),
				m_fs,
				m_flid,
				"Name",
				m_persistenceProvider,
				Mediator,
				"Name",
				XmlUtils.GetOptionalAttributeValue(m_configurationNode, "ws", "analysis")); // TODO: Get better default 'best ws'.
		}

		protected override int DesiredHeight(RootSite rs)
		{
			return Math.Max(base.DesiredHeight(rs), ((PhonologicalFeatureListDlgLauncher)Control).LauncherButton.Height);
		}

		private void RemoveFeatureStructureFromOwner()
		{
			if (m_obj != null)
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					switch (m_obj.ClassID)
					{
						case PhPhonemeTags.kClassId:
							var phoneme = (IPhPhoneme) m_obj;
							phoneme.FeaturesOA = null;
							break;
						case PhNCFeaturesTags.kClassId:
							var features = (IPhNCFeatures) m_obj;
							features.FeaturesOA = null;
							break;
					}
				});
			}
		}

		private static int GetFlid(XmlNode node, ICmObject obj)
		{
			string attrName = XmlUtils.GetOptionalAttributeValue(node, "field");
			int flid = 0;
			if (attrName != null)
			{
				try
				{
					flid = obj.Cache.MetaDataCacheAccessor.GetFieldId2(obj.ClassID, attrName, true);
				}
				catch
				{
					throw new ApplicationException(
						"DataTree could not find the flid for attribute '" + attrName +
						"' of class '" + obj.ClassID + "'.");
				}
			}
			return flid;
		}

		private static IFsFeatStruc GetFeatureStructureFromOwner(ICmObject obj, int flid)
		{
			int hvoFs = obj.Cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid);
			if (hvoFs == 0)
				return null;
			return obj.Services.GetInstance<IFsFeatStrucRepository>().GetObject(hvoFs);
		}

		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Control = new PhonologicalFeatureListDlgLauncher();
		}
		/// <summary>
		/// Determine if the object really has data to be shown in the slice
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="obj">object to check; should be an IFsFeatStruc</param>
		/// <returns>
		/// true if the feature structure has content in FeatureSpecs; false otherwise
		/// </returns>
		public static bool ShowSliceForVisibleIfData(XmlNode node, ICmObject obj)
		{

			//FDO.Cellar.IFsFeatStruc fs = obj as FDO.Cellar.IFsFeatStruc;
			int flid = GetFlid(node, obj);
			IFsFeatStruc fs;
			if (flid != 0)
				fs = GetFeatureStructureFromOwner(obj, flid);
			else
				fs = obj as IFsFeatStruc;
			if (fs != null)
			{
				if (fs.FeatureSpecsOC.Count > 0)
					return true;
			}
			return false;
		}

		public override int Flid
		{
			get
			{
				CheckDisposed();
				return m_flid;
			}
		}
	}
}
