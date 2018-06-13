// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Areas.Grammar.Tools.PhonologicalFeaturesAdvancedEdit
{
	/// <summary />
	internal sealed class PhonologicalFeatureListDlgLauncherSlice : ViewSlice
	{
		private int m_flid;
		private IFsFeatStruc m_fs;

		/// <summary />
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
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

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

		/// <summary />
		public override RootSite RootSite => ((PhonologicalFeatureListDlgLauncher)Control).MainControl as RootSite;

		/// <summary />
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);

			var ctrl = (PhonologicalFeatureListDlgLauncher)Control;

			m_flid = GetFlid(ConfigurationNode, MyCmObject);
			if (m_flid != 0)
			{
				m_fs = GetFeatureStructureFromOwner(MyCmObject, m_flid);
			}
			else
			{
				m_fs = MyCmObject as IFsFeatStruc;
				m_flid = FsFeatStrucTags.kflidFeatureSpecs;
			}

			ctrl.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			ctrl.Initialize(PropertyTable.GetValue<LcmCache>("cache"),
				m_fs,
				m_flid,
				"Name",
				PersistenceProvider,
				"Name",
				XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "ws", "analysis")); // TODO: Get better default 'best ws'.
		}

		/// <summary />
		protected override int DesiredHeight(RootSite rs)
		{
			return Math.Max(base.DesiredHeight(rs), ((PhonologicalFeatureListDlgLauncher)Control).LauncherButton.Height);
		}

		private void RemoveFeatureStructureFromOwner()
		{
			if (MyCmObject != null)
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					switch (MyCmObject.ClassID)
					{
						case PhPhonemeTags.kClassId:
							var phoneme = (IPhPhoneme)MyCmObject;
							phoneme.FeaturesOA = null;
							break;
						case PhNCFeaturesTags.kClassId:
							var features = (IPhNCFeatures)MyCmObject;
							features.FeaturesOA = null;
							break;
					}
				});
			}
		}

		private static int GetFlid(XElement node, ICmObject obj)
		{
			var attrName = XmlUtils.GetOptionalAttributeValue(node, "field");
			var flid = 0;
			if (attrName == null)
			{
				return flid;
			}
			if (!obj.Cache.GetManagedMetaDataCache().TryGetFieldId(obj.ClassID, attrName, out flid))
			{
				throw new ApplicationException($"DataTree could not find the flid for attribute '{attrName}' of class '{obj.ClassID}'.");
			}
			return flid;
		}

		private static IFsFeatStruc GetFeatureStructureFromOwner(ICmObject obj, int flid)
		{
			var hvoFs = obj.Cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid);
			return hvoFs == 0 ? null : obj.Services.GetInstance<IFsFeatStrucRepository>().GetObject(hvoFs);
		}

		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
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
		public static bool ShowSliceForVisibleIfData(XElement node, ICmObject obj)
		{
			var flid = GetFlid(node, obj);
			var fs = flid != 0 ? GetFeatureStructureFromOwner(obj, flid) : obj as IFsFeatStruc;

			return fs?.FeatureSpecsOC.Count > 0;
		}

		/// <summary />
		public override int Flid => m_flid;
	}
}