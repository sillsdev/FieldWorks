using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using SIL.FieldWorks.Common.Utils;
using XCore;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class PhonologicalFeatureListDlgLauncherSlice : ViewSlice
	{
		private System.ComponentModel.IContainer components = null;
		private int m_flid;
		IFsFeatStruc m_fs;
		private IPersistenceProvider m_persistenceProvider;
		private int m_ws;
		private XmlNode m_node;

		public PhonologicalFeatureListDlgLauncherSlice()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

				/// <summary>
		/// We want the persistence provider, and the easiest way to get it is to get all
		/// this other stuff we don't need or use.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="editor"></param>
		/// <param name="flid"></param>
		/// <param name="node"></param>
		/// <param name="obj"></param>
		/// <param name="stringTbl"></param>
		/// <param name="persistenceProvider"></param>
		/// <param name="ws"></param>
		public PhonologicalFeatureListDlgLauncherSlice(FdoCache cache, string editor, int flid,
			System.Xml.XmlNode node, ICmObject obj, StringTable stringTbl,
			IPersistenceProvider persistenceProvider, int ws)
				{
					m_obj = obj; // is PhPhoneme
					m_persistenceProvider = persistenceProvider;
					m_ws = ws;
					m_node = node;
					m_configurationNode = node;
				}
		public PhonologicalFeatureListDlgLauncherSlice(FdoCache cache, ICmObject obj, int flid,
			System.Xml.XmlNode node, IPersistenceProvider persistenceProvider, Mediator mediator, StringTable stringTbl)
		{

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
				if (m_fs != null && ((m_fs.FeatureSpecsOC == null) || (m_fs.FeatureSpecsOC.Count < 1)) )
				{
					// At some point we will hopefully be able to convert this slice to a true ghost slice
					// so that we aren't creating and deleting database objects unless needed. At that
					// point this can be removed as well as removing the kludge in
					// CreateModifyTimeManager PropChanged that was needed to keep this trick from
					// messing up modify times on entries.
					RemoveFeatureStructureFromOwner(); // it's empty so don't bother keeping it
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
				return (Control as PhonologicalFeatureListDlgLauncher).MainControl as RootSite;
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

			PhonologicalFeatureListDlgLauncher ctrl = (PhonologicalFeatureListDlgLauncher)Control;

			m_flid = GetFlid(m_configurationNode, m_obj);
			if (m_flid != 0)
				m_fs = GetFeatureStructureFromOwner(m_obj, m_flid);
			else
			{
				m_fs = m_obj as FsFeatStruc;
				m_flid = (int)FsFeatStruc.FsFeatStrucTags.kflidFeatureSpecs;
			}

			ctrl.Initialize((FdoCache)Mediator.PropertyTable.GetValue("cache"),
				m_fs,
				m_flid,
				"Name",
				ContainingDataTree.PersistenceProvder,
				Mediator,
				"Name",
				XmlUtils.GetOptionalAttributeValue(m_configurationNode, "ws", "analysis")); // TODO: Get better default 'best ws'.
		}

		protected override int DesiredHeight(RootSite rs)
		{
			return Math.Max(base.DesiredHeight(rs), (Control as PhonologicalFeatureListDlgLauncher).LauncherButton.Height);
		}

		private void RemoveFeatureStructureFromOwner()
		{
			if (m_obj != null)
			{
				switch (m_obj.ClassID)
				{
					case PhPhoneme.kclsidPhPhoneme:
						IPhPhoneme phoneme = m_obj as IPhPhoneme;
						phoneme.FeaturesOA = null;
						break;
					case PhNCFeatures.kclsidPhNCFeatures:
						IPhNCFeatures features = m_obj as IPhNCFeatures;
						features.FeaturesOA = null;
						break;
				}
			}
		}

		private static int GetFlid(XmlNode node, ICmObject obj)
		{
			string attrName = XmlUtils.GetOptionalAttributeValue(node, "field");
			int flid = 0;
			if (attrName != null)
			{
				flid = (int)obj.Cache.MetaDataCacheAccessor.GetFieldId2((uint)obj.ClassID, attrName, true);
				if (flid == 0)
					throw new ApplicationException(
						"DataTree could not find the flid for attribute '" + attrName +
						"' of class '" + obj.ClassID + "'.");
			}
			return flid;
		}

		private static IFsFeatStruc GetFeatureStructureFromOwner(ICmObject obj, int flid)
		{
			IFsFeatStruc fs = obj.GetObjectInAtomicField(flid) as IFsFeatStruc;
			return fs;
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
		/// <param name="obj">object to check; should be an FsFeatStruc</param>
		/// <returns>true if the feature structure has content in FeatureSpecs; false otherwise</returns>
		public static bool ShowSliceForVisibleIfData(XmlNode node, ICmObject obj)
		{

			//FDO.Cellar.FsFeatStruc fs = obj as FDO.Cellar.FsFeatStruc;
			int flid = GetFlid(node, obj);
			IFsFeatStruc fs;
			if (flid != 0)
				fs = GetFeatureStructureFromOwner(obj, flid);
			else
				fs = obj as FsFeatStruc;
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
