// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InflAffixTemplateSlice.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for InflAffixTemplateSlice.
	/// </summary>
	public class InflAffixTemplateSlice : ViewSlice
	{
		/// <summary>
		/// handles creating the context menus for the inflectional affix template and funneling commands to the control.
		/// </summary>
		InflAffixTemplateMenuHandler m_menuHandler;
		/// <summary>
		/// This must have a zero-argument constructor, because it is designed to be created
		/// from a custom XDE node by reflection.
		/// </summary>
		public InflAffixTemplateSlice()
		{
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_menuHandler != null)
					m_menuHandler.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_menuHandler = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Therefore this method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			IWritingSystemContainer wsContainer = Cache.ServiceLocator.WritingSystems;
			bool fVernRTL = wsContainer.DefaultVernacularWritingSystem.RightToLeftScript;
			bool fAnalRTL = wsContainer.DefaultAnalysisWritingSystem.RightToLeftScript;
			System.Xml.XmlAttribute xa = ConfigurationNode.Attributes["layout"];
			// To properly fix LT-6239, we need to consider all four mixtures of directionality
			// involving the vernacular (table) and analysis (slot name) writing systems.
			// These four possibilities are marked RTL, LTRinRTL, RTLinLTR, and <nothing>.
			if (fVernRTL && fAnalRTL)
			{
				if (xa.Value.EndsWith("RTLinLTR") || xa.Value.EndsWith("LTRinRTL"))
					xa.Value = xa.Value.Substring(0, xa.Value.Length - 8);
				if (!xa.Value.EndsWith("RTL"))
					xa.Value += "RTL";		// both vern and anal are RTL
			}
			else if (fVernRTL && !fAnalRTL)
			{
				if (xa.Value.EndsWith("RTLinLTR"))
					xa.Value = xa.Value.Substring(0, xa.Value.Length - 8);
				else if (xa.Value.EndsWith("RTL") && !xa.Value.EndsWith("LTRinRTL"))
					xa.Value = xa.Value.Substring(0, xa.Value.Length - 3);
				if (!xa.Value.EndsWith("LTRinRTL"))
					xa.Value += "LTRinRTL";		// LTR anal name in RTL vern table
			}
			else if (!fVernRTL && fAnalRTL)
			{
				if (xa.Value.EndsWith("LTRinRTL"))
					xa.Value = xa.Value.Substring(0, xa.Value.Length - 8);
				else if (xa.Value.EndsWith("RTL"))
					xa.Value = xa.Value.Substring(0, xa.Value.Length - 3);
				if (!xa.Value.EndsWith("RTLinLTR"))
					xa.Value += "RTLinLTR";		// RTL anal name in LTR vern table
			}
			else
			{
				if (xa.Value.EndsWith("RTLinLTR") || xa.Value.EndsWith("LTRinRTL"))
					xa.Value = xa.Value.Substring(0, xa.Value.Length - 8);
				else if (xa.Value.EndsWith("RTL"))
					xa.Value = xa.Value.Substring(0, xa.Value.Length - 3);
				// both vern and anal are LTR (unmarked case)
			}
			var ctrl = new InflAffixTemplateControl((FdoCache)Mediator.PropertyTable.GetValue("cache"),
				Object.Hvo, ConfigurationNode, StringTbl);
			Control = ctrl;
			m_menuHandler = InflAffixTemplateMenuHandler.Create(ctrl, ConfigurationNode);
#if !Want
			m_menuHandler.Init(Mediator, null);
#else
			m_menuHandler.Init(null, null);
#endif
			ctrl.SetContextMenuHandler(m_menuHandler.ShowSliceContextMenu);
			ctrl.Mediator = Mediator;
			ctrl.SetStringTableValues(Mediator.StringTbl);
			if (ctrl.RootBox == null)
				ctrl.MakeRoot();
		}

		public InflAffixTemplateSlice(SimpleRootSite ctrlT): base(ctrlT)
		{
			CheckDisposed();
		}
	}
}
