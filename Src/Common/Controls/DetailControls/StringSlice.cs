using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Collections;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Controls;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for ViewPropertyItem.
	/// </summary>
	public class StringSlice : ViewPropertySlice
	{
		int m_ws = -1;

		public StringSlice(int hvoObj, int flid)
			: base(new StringSliceView(hvoObj, flid, -1), hvoObj, flid)
		{
		}

		public StringSlice(int hvoObj, int flid, int ws)
			: base(new StringSliceView(hvoObj, flid, ws), hvoObj, flid)
		{
			m_ws = ws;
		}

		/// <summary>
		/// This constructor is mainly intended for subclasses in other DLLs created using the 'custom' element.
		/// Such subclasses must set the ContextObject, the FieldId, and if relevant the Ws, and then call
		/// CreateView(), typically from an override of FinishInit().
		/// </summary>
		public StringSlice()
		{
		}

		/// <summary>
		/// See comments on no-arg constructor. Call only if using that constructor.
		/// </summary>
		public void CreateView()
		{
			CheckDisposed();
			StringSliceView ssv = new StringSliceView(m_hvoContext, m_flid, m_ws);
			ssv.Cache = Cache;
			Control = ssv;
		}

		/// <summary>
		/// Get/set the writing system ID. If -1, signifies a non-multilingual property.
		/// </summary>
		public int WritingSystemId
		{
			get
			{
				CheckDisposed();
				return m_ws;
			}
			set
			{
				CheckDisposed();
				m_ws = value;
			}
		}

		#region View Constructors

		public class StringSliceVc: VwBaseVc
		{
			int m_flid;
			FdoCache m_cache;
			public StringSliceVc()
			{
				m_wsDefault = -1;	// -1 signifies not a multilingual property
			}
			public StringSliceVc(int flid, int ws, FdoCache cache)
			{
				m_flid = flid;
				m_wsDefault = ws;
				m_cache = cache;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();
				if (m_wsDefault == -1)
				{
					// Set the underlying paragraph to RTL if the first writing system in the
					// string is RTL.
					if (m_cache != null)
					{
						ITsString tss = m_cache.MainCacheAccessor.get_StringProp(hvo, m_flid);
						ITsTextProps ttp = tss.get_Properties(0);
						int var;
						int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
						if (ws != 0)
							SetParaRTLIfNeeded(vwenv, ws);
					}
					vwenv.AddStringProp(m_flid, this);
				}
				else
				{
					SetParaRTLIfNeeded(vwenv, m_wsDefault);
					vwenv.AddStringAltMember(m_flid, m_wsDefault, this);
				}
			}

			private void SetParaRTLIfNeeded(IVwEnv vwenv, int ws)
			{
				if (m_cache == null)
					return;
				IWritingSystem lgws = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws);
				if (lgws != null && lgws.RightToLeft)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvForceOn);
					vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextAlign.ktalTrailing);
				}
			}
		}

		public class UnicodeStringSliceVc: VwBaseVc
		{
			int m_flid;
			public UnicodeStringSliceVc()
			{
				m_wsDefault = -1;
			}
			public UnicodeStringSliceVc(int flid, int ws, FdoCache fdoCache)
			{
				m_flid = flid;
				if (ws == -1)
				{
					// not specified, use the user interface ws.
					m_wsDefault = fdoCache.LanguageWritingSystemFactoryAccessor.UserWs;
				}
				else
				{
					m_wsDefault = ws;
				}
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();
				vwenv.AddUnicodeProp(m_flid, m_wsDefault, this);
			}
		}
		#endregion // View Constructors

		#region RootSite implementation
		class StringSliceView : RootSiteControl, INotifyControlInCurrentSlice
		{
			ICmObject m_obj;
			int m_hvoObj;
			int m_flid;
			int m_ws = -1; // -1 signifies not a multilingual property
			IVwViewConstructor m_vc = null;

			public StringSliceView(int hvo, int flid, int ws)
			{
				m_hvoObj = hvo;
				m_flid = flid;
				m_ws = ws;
				DoSpellCheck = true;
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

				base.Dispose(disposing);

				if (disposing)
				{
					// Dispose managed resources here.
					if (m_vc != null && m_vc is IDisposable)
						(m_vc as IDisposable).Dispose();
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_obj = null;
				m_vc = null;
			}

			#endregion IDisposable override

			/// <summary>
			/// Make a selection at the specified character offset.
			/// </summary>
			/// <param name="ich"></param>
			public void SelectAt(int ich)
			{
				CheckDisposed();
				RootBox.MakeTextSelection(0, 0, null, m_flid, 0, ich, ich, 0, true, -1, null, true);
			}

			#region INotifyControlInCurrentSlice implementation

			/// <summary>
			/// Adjust controls based on whether the slice is the current slice.
			/// </summary>
			public bool SliceIsCurrent
			{
				set
				{
					CheckDisposed();
					if (!value)
					{
						// This may be called in the process of deleting the object after the object
						// has been partially cleared out and thus would certainly fail the constraint
						// check, then try to instantiate an error annotation which wouldn't have an
						// owner, causing bad things to happen.
						if (m_obj != null && m_obj.IsValidObject())
						{
							ConstraintFailure failure;
							if (m_obj is FDO.Ling.PhEnvironment)
							{
								(m_obj as FDO.Ling.PhEnvironment).CheckConstraints(m_flid, out failure, /* adjust squiggly line */ true);
							}
							else
								m_obj.CheckConstraints(m_flid, out failure);
						}
					}
				}
			}

			#endregion INotifyControlInCurrentSlice implementation

			/// <summary>
			/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
			/// (See LT-8656 and LT-9119.)
			/// </summary>
			protected override void OnKeyPress(KeyPressEventArgs e)
			{
				if (m_fdoCache.VerifyValidObject(m_obj))
					base.OnKeyPress(e);
				else
					e.Handled = true;
			}

			public override void MakeRoot()
			{
				CheckDisposed();
				base.MakeRoot();

				if (m_fdoCache == null || DesignMode)
					return;

				// A crude way of making sure the property we want is loaded into the cache.
				m_obj = CmObject.CreateFromDBObject(m_fdoCache, m_hvoObj);

				FieldType type = m_fdoCache.GetFieldType(m_flid);
				if (type == FieldType.kcptUnicode
					|| type == FieldType.kcptBigUnicode)
				{
					m_vc = new UnicodeStringSliceVc(m_flid, m_ws, m_fdoCache);
				}
				else if (type == FieldType.kcptString
					|| type == FieldType.kcptBigString)
				{
					// Even if we were given a writing system, we must not use it if not a multistring,
					// otherwise the VC crashes when it tries to read the property as multilingual.
					m_vc = new StringSliceVc(m_flid, -1, m_fdoCache);
				}
				else
				{
					m_vc = new StringSliceVc(m_flid, m_ws, m_fdoCache);
				}

				// Review JohnT: why doesn't the base class do this??
				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);

				// And maybe this too, at least by default?
				m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

				// arg3 is a meaningless initial fragment, since this VC only displays one thing.
				// arg4 could be used to supply a stylesheet.
				m_rootb.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);
			}
		}

		#endregion // RootSite implementation
		/// <summary>
		/// Make a selection at the specified character offset.
		/// </summary>
		/// <param name="ich"></param>
		public void SelectAt(int ich)
		{
			CheckDisposed();
			(Control as StringSliceView).SelectAt(ich);
		}
	}
}
