using System;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO.Ling;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// </summary>
	public class LexReferenceCollectionSlice : ReferenceVectorSlice, ILexReferenceSlice
	{
		private LexReferenceMultiSlice m_masterSlice = null;

		/// <summary>
		/// Constructor.
		/// </summary>
		public LexReferenceCollectionSlice() : base()
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
				LexReferenceCollectionLauncher launcher = Control as LexReferenceCollectionLauncher;
				launcher.ViewSizeChanged -= new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
				VectorReferenceView view = (VectorReferenceView)launcher.MainControl;
				view.ViewSizeChanged -= new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_masterSlice = null;

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
			string className = m_cache.MetaDataCacheAccessor.GetClassName((uint)m_obj.ClassID);
			m_fieldName = XmlUtils.GetManditoryAttributeValue(m_configurationNode, "field");
			m_flid = AutoDataTreeMenuHandler.ContextMenuHelper.GetFlid(m_cache.MetaDataCacheAccessor,
				className, m_fieldName);
			LexReferenceCollectionLauncher launcher = new LexReferenceCollectionLauncher();
			launcher.Initialize(m_cache, m_obj, m_flid, m_fieldName,
				null,
				Mediator,
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
			launcher.DisplayParentHvo = XmlUtils.GetMandatoryIntegerAttributeValue(
				m_configurationNode, "hvoDisplayParent");
		}



		#region ILexReferenceSlice Members

		/// <summary>
		/// This is the controlling LexReferenceMultiSlice.
		/// </summary>
		public override Slice MasterSlice
		{
			set
			{
				CheckDisposed();
				m_masterSlice = value as LexReferenceMultiSlice;
			}
			get
			{
				CheckDisposed();
				return m_masterSlice;
			}
		}

		public override void HandleDeleteCommand(Command cmd)
		{
			CheckDisposed();
			m_masterSlice.DeleteFromReference(GetObjectHvoForMenusToOperateOn());
		}


		/// <summary>
		/// This method is called when the user selects "Add Reference" or "Replace Reference" under the
		/// dropdown menu for a lexical relation
		/// </summary>
		public override void HandleLaunchChooser()
		{
			CheckDisposed();
			(Control as LexReferenceCollectionLauncher).LaunchChooser();
		}

		public override void HandleEditCommand()
		{
			CheckDisposed();
			m_masterSlice.EditReferenceDetails(GetObjectHvoForMenusToOperateOn());
		}

		#endregion
	}
}
