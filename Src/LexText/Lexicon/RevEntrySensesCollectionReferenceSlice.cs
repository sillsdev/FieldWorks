// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RevEntrySensesCollectionReferenceSlice.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Xml;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Provides a class that uses the Go dlg for selecting entries for subentries.
	/// </summary>
	public class RevEntrySensesCollectionReferenceSlice : ReferenceVectorSlice
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public RevEntrySensesCollectionReferenceSlice() : base()
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
				RevEntrySensesCollectionReferenceLauncher launcher = Control as RevEntrySensesCollectionReferenceLauncher;
				launcher.ViewSizeChanged -= new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
				VectorReferenceView view = (VectorReferenceView)launcher.MainControl;
				view.ViewSizeChanged -= new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Override method and assert, since it shouldn't be called, as this class is a custom created slice.
		/// </summary>
		/// <param name="persistenceProvider"></param>
		protected override void SetupControls(IPersistenceProvider persistenceProvider, Mediator mediator, StringTable stringTbl)
		{
			Debug.Assert(false, "This should never be called.");
		}

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Debug.Assert(m_cache != null);
			Debug.Assert(m_configurationNode != null);

			base.FinishInit();
			RevEntrySensesCollectionReferenceLauncher launcher =
				new RevEntrySensesCollectionReferenceLauncher();
			string className = m_cache.MetaDataCacheAccessor.GetClassName((uint)m_obj.ClassID);
			m_fieldName = XmlUtils.GetManditoryAttributeValue(m_configurationNode, "field");
			m_flid = AutoDataTreeMenuHandler.ContextMenuHelper.GetFlid(m_cache.MetaDataCacheAccessor,
				className, m_fieldName);
			launcher.Initialize(m_cache, m_obj, m_flid, m_fieldName,
				null, Mediator,
				DisplayNameProperty,
				BestWsName); // TODO: Get better default 'best ws'.
			// We don't want to be visible until later, since otherwise we get a temporary
			// display in the wrong place with the wrong size that serves only to annoy the
			// user.  See LT-1518 "The drawing of the DataTree for Lexicon/Advanced Edit draws
			// some initial invalid controls."  Becoming visible when we set the width and
			// height seems to delay things enough to avoid this visual clutter.
			launcher.Visible = false;
			Control = launcher;
			launcher.ViewSizeChanged += new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
			VectorReferenceView view = (VectorReferenceView)launcher.MainControl;
			view.ViewSizeChanged += new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
		}
	}
}
