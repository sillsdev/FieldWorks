// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;

namespace LanguageExplorer.Areas.Grammar
{
	/// <summary />
	internal class MsaInflectionFeatureListDlgLauncherSlice : Slice
	{
		private System.ComponentModel.IContainer components = null;
		protected IFsFeatStruc m_fs;
		protected int m_flid;

		/// <summary />
		public MsaInflectionFeatureListDlgLauncherSlice()
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

			if( disposing )
			{
				if (m_fs != null && m_fs.IsValidObject && ((m_fs.FeatureSpecsOC == null) || (m_fs.FeatureSpecsOC.Count < 1)) )
				{
					// At some point we will hopefully be able to convert this slice to a true ghost slice
					// so that we aren't creating and deleting database objects unless needed. At that
					// point this can be removed as well as removing the kludge in
					// CreateModifyTimeManager PropChanged that was needed to keep this trick from
					// messing up modify times on entries.
					RemoveFeatureStructureFromMSA(); // it's empty so don't bother keeping it
				}
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// MsaInflectionFeatureListDlgLauncherSlice
			//
			this.Name = "MsaInflectionFeatureListDlgLauncherSlice";
			this.Size = new System.Drawing.Size(208, 32);

		}
		#endregion

		/// <summary />
		public override void Install(DataTree parent)
		{
			CheckDisposed();

			base.Install(parent);

			MsaInflectionFeatureListDlgLauncher ctrl = (MsaInflectionFeatureListDlgLauncher)Control;

			m_flid = MsaInflectionFeatureListDlgLauncherSlice.GetFlid(m_configurationNode, m_obj);
			if (m_flid != 0)
				m_fs = MsaInflectionFeatureListDlgLauncherSlice.GetFeatureStructureFromMSA(m_obj, m_flid);
			else
			{
				m_fs = m_obj as IFsFeatStruc;
				m_flid = FsFeatStrucTags.kflidFeatureSpecs;
			}

			ctrl.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			ctrl.Initialize(PropertyTable.GetValue<FdoCache>("cache"),
				m_fs,
				m_flid,
				"Name",
				ContainingDataTree.PersistenceProvder,
				"Name",
				XmlUtils.GetOptionalAttributeValue(m_configurationNode, "ws", "analysis")); // TODO: Get better default 'best ws'.
		}

		private void RemoveFeatureStructureFromMSA()
		{
			if (m_obj != null)
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					switch (m_obj.ClassID)
					{
						case MoStemMsaTags.kClassId:
							IMoStemMsa stem = m_obj as IMoStemMsa;
							stem.MsFeaturesOA = null;
							break;
						case MoInflAffMsaTags.kClassId:
							IMoInflAffMsa infl = m_obj as IMoInflAffMsa;
							infl.InflFeatsOA = null;
							break;
						case MoDerivAffMsaTags.kClassId:
							IMoDerivAffMsa derv = m_obj as IMoDerivAffMsa;
							if (m_flid == MoDerivAffMsaTags.kflidFromMsFeatures)
							{
								derv.FromMsFeaturesOA = null;
							}
							else
							{ // assume it's the to features
								derv.ToMsFeaturesOA = null;
							}
							break;
					}
				});
			}
		}

		/// <summary />
		protected static int GetFlid(XmlNode node, ICmObject obj)
		{
			string attrName = XmlUtils.GetOptionalAttributeValue(node, "field");
			int flid = 0;
			if (attrName != null)
			{
				try
				{
					flid = obj.Cache.DomainDataByFlid.MetaDataCache.GetFieldId2(obj.ClassID, attrName, true);
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

		/// <summary />
		protected static IFsFeatStruc GetFeatureStructureFromMSA(ICmObject obj, int flid)
		{
			//IFsFeatStruc fs = obj.GetObjectInAtomicField(flid) as IFsFeatStruc;
			IFsFeatStruc fs = obj.Cache.GetAtomicPropObject(obj.Cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid)) as IFsFeatStruc;
			return fs;
		}

		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Control = new MsaInflectionFeatureListDlgLauncher();
		}

		/// <summary>
		/// Determine if the object really has data to be shown in the slice
		/// </summary>
		/// <param name="node"></param>
		/// <param name="obj">object to check; should be an IFsFeatStruc</param>
		/// <returns>true if the feature structure has content in FeatureSpecs; false otherwise</returns>
		public static bool ShowSliceForVisibleIfData(XmlNode node, ICmObject obj)
		{

			//FDO.Cellar.IFsFeatStruc fs = obj as FDO.Cellar.IFsFeatStruc;
			int flid = GetFlid(node, obj);
			IFsFeatStruc fs;
			if (flid != 0)
				fs = GetFeatureStructureFromMSA(obj, flid);
			else
				fs = obj as IFsFeatStruc;
			if (fs != null)
			{
				if (fs.FeatureSpecsOC.Count > 0)
					return true;
			}
			return false;
		}

		/// <summary />
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
