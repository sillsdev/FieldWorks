// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

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
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				if (m_fs != null && m_fs.IsValidObject && ((m_fs.FeatureSpecsOC == null) || m_fs.FeatureSpecsOC.Count < 1))
				{
					// At some point we will hopefully be able to convert this slice to a true ghost slice
					// so that we aren't creating and deleting database objects unless needed. At that
					// point this can be removed as well as removing the kludge in
					// CreateModifyTimeManager PropChanged that was needed to keep this trick from
					// messing up modify times on entries.
					RemoveFeatureStructureFromMSA(); // it's empty so don't bother keeping it
				}
				components?.Dispose();
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
			// MsaInflectionFeatureListDlgLauncherSlice
			//
			this.Name = "MsaInflectionFeatureListDlgLauncherSlice";
			this.Size = new System.Drawing.Size(208, 32);

		}
		#endregion

		/// <summary />
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);

			var ctrl = (MsaInflectionFeatureListDlgLauncher)Control;
			m_flid = GetFlid(ConfigurationNode, MyCmObject);
			if (m_flid != 0)
			{
				m_fs = GetFeatureStructureFromMSA(MyCmObject, m_flid);
			}
			else
			{
				m_fs = MyCmObject as IFsFeatStruc;
				m_flid = FsFeatStrucTags.kflidFeatureSpecs;
			}
			ctrl.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			ctrl.Initialize(PropertyTable.GetValue<LcmCache>(FwUtils.cache), m_fs, m_flid, "Name", ContainingDataTree.PersistenceProvder,
				"Name", XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "ws", "analysis")); // TODO: Get better default 'best ws'.
		}

		private void RemoveFeatureStructureFromMSA()
		{
			if (MyCmObject != null)
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					switch (MyCmObject.ClassID)
					{
						case MoStemMsaTags.kClassId:
							var stem = (IMoStemMsa)MyCmObject;
							stem.MsFeaturesOA = null;
							break;
						case MoInflAffMsaTags.kClassId:
							var infl = (IMoInflAffMsa)MyCmObject;
							infl.InflFeatsOA = null;
							break;
						case MoDerivAffMsaTags.kClassId:
							var derv = (IMoDerivAffMsa)MyCmObject;
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
		protected static int GetFlid(XElement node, ICmObject obj)
		{
			var attrName = XmlUtils.GetOptionalAttributeValue(node, "field");
			var flid = 0;
			if (attrName != null)
			{
				if (!obj.Cache.GetManagedMetaDataCache().TryGetFieldId(obj.ClassID, attrName, out flid))
				{
					throw new ApplicationException($"DataTree could not find the flid for attribute '{attrName}' of class '{obj.ClassID}'.");
				}
			}
			return flid;
		}

		/// <summary />
		protected static IFsFeatStruc GetFeatureStructureFromMSA(ICmObject obj, int flid)
		{
			return obj.Cache.GetAtomicPropObject(obj.Cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid)) as IFsFeatStruc;
		}

		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			Control = new MsaInflectionFeatureListDlgLauncher();
		}

		/// <summary>
		/// Determine if the object really has data to be shown in the slice
		/// </summary>
		/// <param name="node"></param>
		/// <param name="obj">object to check; should be an IFsFeatStruc</param>
		/// <returns>true if the feature structure has content in FeatureSpecs; false otherwise</returns>
		/// <remarks>Called by DataTree via reflection.</remarks>
		public static bool ShowSliceForVisibleIfData(XElement node, ICmObject obj)
		{
			var flid = GetFlid(node, obj);
			var fs = flid != 0 ? GetFeatureStructureFromMSA(obj, flid) : obj as IFsFeatStruc;
			return fs?.FeatureSpecsOC.Count > 0;
		}

		/// <summary />
		public override int Flid => m_flid;
	}
}