using System;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using XCore;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for ViewPropertyItem.
	/// </summary>
	public class StringSlice : ViewPropertySlice
	{
		protected int m_ws = -1;

		public StringSlice(ICmObject obj, int flid)
			: base(new StringSliceView(obj.Hvo, flid, -1), obj, flid)
		{
		}

		public StringSlice(ICmObject obj, int flid, int ws)
			: base(new StringSliceView(obj.Hvo, flid, ws), obj, flid)
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
			StringSliceView ssv = new StringSliceView(m_obj.Hvo, m_flid, m_ws);
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

		bool m_fShowWsLabel;
		/// <summary>
		/// Get/set flag whether to display writing system label even for monolingual string.
		/// </summary>
		public bool ShowWsLabel
		{
			get
			{
				CheckDisposed();
				return m_fShowWsLabel;
			}
			set
			{
				CheckDisposed();
				m_fShowWsLabel = value;
				if (Control is StringSliceView)
					(Control as StringSliceView).ShowWsLabel = value;
			}
		}

		int m_wsDefault;
		/// <summary>
		/// Get/set the default writing system associated with this string.
		/// </summary>
		public int DefaultWs
		{
			get
			{
				CheckDisposed();
				return m_wsDefault;
			}
			set
			{
				CheckDisposed();
				m_wsDefault = value;
				if (Control is StringSliceView)
					(Control as StringSliceView).DefaultWs = value;
			}
		}

		#region View Constructors

		public class StringSliceVc: FwBaseVc
		{
			int m_flid;
			Mediator m_mediator;
			private bool m_fMultilingual;
			bool m_fShowWsLabel;
			int m_wsEn;
			/// <summary>most recently displayed label's writing system</summary>
			int m_wsLabel;

			public StringSliceVc()
			{
			}

			/// <summary>
			/// Create one that is NOT multilingual.
			/// </summary>
			/// <param name="flid"></param>
			/// <param name="cache"></param>
			/// <param name="mediator"></param>
			public StringSliceVc(int flid, FdoCache cache, Mediator mediator)
			{
				m_flid = flid;
// ReSharper disable DoNotCallOverridableMethodsInConstructor
				Cache = cache;
// ReSharper restore DoNotCallOverridableMethodsInConstructor
				m_mediator = mediator;
				m_wsEn = cache.WritingSystemFactory.GetWsFromStr("en");
				if (m_wsEn == 0)
					m_wsEn = cache.DefaultUserWs;
			}

			public StringSliceVc(int flid, int ws, FdoCache cache, Mediator mediator)
				:this(flid, cache, mediator)
			{
				m_wsDefault = ws;
				m_fMultilingual = true;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				if (m_fMultilingual)
				{
					SetParaRtlIfNeeded(vwenv, m_wsDefault);
					vwenv.AddStringAltMember(m_flid, m_wsDefault, this);
				}
				else
				{
					// Set the underlying paragraph to RTL if the first writing system in the
					// string is RTL.
					if (m_cache != null)
					{
						ITsString tss = m_cache.DomainDataByFlid.get_StringProp(hvo, m_flid);
						ITsTextProps ttp = tss.get_Properties(0);
						int var;
						int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
						if (ws == 0)
							ws = m_wsDefault;
						if (ws == 0)
							ws = m_cache.DefaultAnalWs;
						if (ws != 0)
						{
							SetParaRtlIfNeeded(vwenv, ws);
							if (m_fShowWsLabel)
							{
								DisplayWithWritingSystemLabel(vwenv, ws);
								return;
							}
						}
					}
					vwenv.AddStringProp(m_flid, this);
				}
			}

			private void DisplayWithWritingSystemLabel(IVwEnv vwenv, int ws)
			{
				ITsString tssLabel = NameOfWs(ws);
				// We use a table to display
				// encodings in column one and the strings in column two.
				// The table uses 100% of the available width.
				VwLength vlTable;
				vlTable.nVal = 10000;
				vlTable.unit = VwUnit.kunPercent100;

				int dxs;	// Width of displayed string.
				int dys;	// Height of displayed string (not used here).
				vwenv.get_StringWidth(tssLabel, null, out dxs, out dys);
				VwLength vlColWs; // 5-pt space plus max label width.
				vlColWs.nVal = dxs + 5000;
				vlColWs.unit = VwUnit.kunPoint1000;

				// The Main column is relative and uses the rest of the space.
				VwLength vlColMain;
				vlColMain.nVal = 1;
				vlColMain.unit = VwUnit.kunRelative;

				// Enhance JohnT: possibly allow for right-to-left UI by reversing columns?

				vwenv.OpenTable(2, // Two columns.
					vlTable, // Table uses 100% of available width.
					0, // Border thickness.
					VwAlignment.kvaLeft, // Default alignment.
					VwFramePosition.kvfpVoid, // No border.
					VwRule.kvrlNone, // No rules between cells.
					0, // No forced space between cells.
					0, // No padding inside cells.
					false);
				// Specify column widths. The first argument is the number of columns,
				// not a column index. The writing system column only occurs at all if its
				// width is non-zero.
				vwenv.MakeColumns(1, vlColWs);
				vwenv.MakeColumns(1, vlColMain);

				vwenv.OpenTableBody();
				vwenv.OpenTableRow();

				// First cell has writing system abbreviation displayed using m_ttpLabel.
				//vwenv.Props = m_ttpLabel;
				vwenv.OpenTableCell(1, 1);
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
				vwenv.AddString(tssLabel);
				vwenv.CloseTableCell();

				// Second cell has the string contents for the alternative.
				// DN version has some property setting, including trailing margin and RTL.
				if (m_fRtlScript)
				{
					vwenv.set_IntProperty((int) FwTextPropType.ktptRightToLeft,
										  (int) FwTextPropVar.ktpvEnum,
										  (int) FwTextToggleVal.kttvForceOn);
					vwenv.set_IntProperty((int) FwTextPropType.ktptAlign,
										  (int) FwTextPropVar.ktpvEnum,
										  (int) FwTextAlign.ktalTrailing);
				}
				//if (!m_editable)
				//{
				//    vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
				//        (int)TptEditable.ktptNotEditable);
				//}

				vwenv.set_IntProperty((int) FwTextPropType.ktptPadTop, (int) FwTextPropVar.ktpvMilliPoint, 2000);
				vwenv.OpenTableCell(1, 1);
				vwenv.AddStringProp(m_flid, this);
				vwenv.CloseTableCell();
				vwenv.CloseTableRow();
				vwenv.CloseTableBody();
				vwenv.CloseTable();
			}

			private ITsString NameOfWs(int ws)
			{
				m_wsLabel = ws;
				var sWs = m_cache.WritingSystemFactory.GetStrFromWs(ws);
				IWritingSystem wsys;
				WritingSystemServices.FindOrCreateWritingSystem(m_cache, sWs, false, false, out wsys);
				var result = wsys.Abbreviation;
				if (string.IsNullOrEmpty(result))
					result = "??";
				ITsStrBldr tsb = TsStrBldrClass.Create();
				tsb.Replace(0, 0, result, WritingSystemServices.AbbreviationTextProperties);
				tsb.SetIntPropValues(0, tsb.Length, (int)FwTextPropType.ktptWs, 0, m_wsEn);
				return tsb.GetString();
			}

			bool m_fRtlScript;

			private void SetParaRtlIfNeeded(IVwEnv vwenv, int ws)
			{
				if (m_cache == null)
					return;
				IWritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
				if (wsObj != null && wsObj.RightToLeftScript)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvForceOn);
					vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextAlign.ktalTrailing);
					m_fRtlScript = true;
				}
				else
				{
					m_fRtlScript = false;
				}
			}

			/// <summary>
			/// Get/set flag whether to display writing system label even for monolingual string.
			/// </summary>
			public bool ShowWsLabel
			{
				get { return m_fShowWsLabel; }
				set { m_fShowWsLabel = value; }
			}

			/// <summary>
			/// Get the ws for the most recently displayed writing system label.
			/// </summary>
			internal int WsLabel
			{
				get { return m_wsLabel; }
			}

			/// <summary>
			/// We may have a link embedded here.
			/// </summary>
			public override void DoHotLinkAction(string strData, ISilDataAccess sda)
			{
				if (strData.Length > 0 && strData[0] == (int)FwObjDataTypes.kodtExternalPathName)
				{
					string url = strData.Substring(1); // may also be just a file name, launches default app.
					try
					{
						if (url.StartsWith(FwLinkArgs.kFwUrlPrefix))
						{
							m_mediator.SendMessage("FollowLink", new FwLinkArgs(url));
							return;
						}
					}
					catch
					{
						// REVIEW: Why are we catching all errors?
						// JohnT: one reason might be that the above will fail if the link is to another project.
						// Review: would we be better to use the default? That is now smart about
						// local links, albeit by a rather more awkward route because of dependency problems.
					}
				}
				base.DoHotLinkAction(strData, sda);
			}
		}

		public class UnicodeStringSliceVc: FwBaseVc
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
					m_wsDefault = fdoCache.WritingSystemFactory.UserWs;
				}
				else
				{
					m_wsDefault = ws;
				}
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				vwenv.AddUnicodeProp(m_flid, m_wsDefault, this);
			}
		}
		#endregion // View Constructors

		#region RootSite implementation
		class StringSliceView : RootSiteControl, INotifyControlInCurrentSlice
		{
			ICmObject m_obj;
			readonly int m_hvoObj;
			readonly int m_flid;
			readonly int m_ws = -1; // -1 signifies not a multilingual property
			IVwViewConstructor m_vc;

			public StringSliceView(int hvo, int flid, int ws)
			{
				m_hvoObj = hvo;
				m_flid = flid;
				m_ws = ws;
				DoSpellCheck = true;
			}

			bool m_fShowWsLabel;
			/// <summary>
			/// Set the flag to display writing system labels even for monolingual strings.
			/// </summary>
			public bool ShowWsLabel
			{
				set
				{
					CheckDisposed();
					m_fShowWsLabel = value;
					if (m_vc is StringSliceVc)
						(m_vc as StringSliceVc).ShowWsLabel = value;
				}
			}

			/// <summary>
			/// Set the default writing system for this string.
			/// </summary>
			public int DefaultWs
			{
				set
				{
					CheckDisposed();
					if (m_vc is StringSliceVc)
						(m_vc as StringSliceVc).DefaultWs = value;
				}
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
				try
				{
					RootBox.MakeTextSelection(0, 0, null, m_flid, 0, ich, ich, 0, true, -1, null, true);
				}
				catch
				{
				}
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
						DoValidation();
					}
				}
			}

			private void DoValidation()
			{
				// This may be called in the process of deleting the object after the object
				// has been partially cleared out and thus would certainly fail the constraint
				// check, then try to instantiate an error annotation which wouldn't have an
				// owner, causing bad things to happen.
				if (m_obj != null && m_obj.IsValidObject)
				{
					ConstraintFailure failure;
					if (m_obj is IPhEnvironment)
					{
						(m_obj as IPhEnvironment).CheckConstraints(m_flid, true, out failure, /* adjust squiggly line */ true);
					}
					else
						m_obj.CheckConstraints(m_flid, true, out failure);
				}
			}

			/// <summary>
			/// This method seems to get called when we are switching to another tool (or area, or slice) AND when the
			/// program is shutting down. This makes it a good point to check constraints, since in some of these
			/// cases, SliceIsCurrent may not get set false.
			/// </summary>
			protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
			{
				base.OnValidating(e);
				DoValidation();
			}

			#endregion INotifyControlInCurrentSlice implementation

			/// <summary>
			/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
			/// (See LT-8656 and LT-9119.)
			/// </summary>
			protected override void OnKeyPress(KeyPressEventArgs e)
			{
				if (m_obj.IsValidObject)
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
				m_obj = m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoObj);

				CellarPropertyType type = (CellarPropertyType)m_fdoCache.DomainDataByFlid.MetaDataCache.GetFieldType(m_flid);
				if (type == CellarPropertyType.Unicode
					|| type == CellarPropertyType.BigUnicode)
				{
					m_vc = new UnicodeStringSliceVc(m_flid, m_ws, m_fdoCache);
				}
				else if (type == CellarPropertyType.String
					|| type == CellarPropertyType.BigString)
				{
					// Even if we were given a writing system, we must not use it if not a multistring,
					// otherwise the VC crashes when it tries to read the property as multilingual.
					m_vc = new StringSliceVc(m_flid, m_fdoCache, m_mediator);
					(m_vc as StringSliceVc).ShowWsLabel = m_fShowWsLabel;
				}
				else
				{
					m_vc = new StringSliceVc(m_flid, m_ws, m_fdoCache, m_mediator);
					(m_vc as StringSliceVc).ShowWsLabel = m_fShowWsLabel;
				}

				// Review JohnT: why doesn't the base class do this??
				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);

				// And maybe this too, at least by default?
				m_rootb.DataAccess = m_fdoCache.DomainDataByFlid;

				// arg3 is a meaningless initial fragment, since this VC only displays one thing.
				// arg4 could be used to supply a stylesheet.
				m_rootb.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);
			}

			static bool s_fProcessingSelectionChanged;
			/// <summary>
			/// Try to keep the selection from including any of the characters in a writing system label.
			/// Also update the writing system label if needed.
			/// </summary>
			protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
			{
				base.HandleSelectionChange(prootb, vwselNew);

				// 1) We don't want to recurse into here.
				// 2) If the selection is invalid we can't use it.
				if (s_fProcessingSelectionChanged || !vwselNew.IsValid)
					return;
				try
				{
					s_fProcessingSelectionChanged = true;

					// If the selection is entirely formattable ("IsSelectionInOneFormattableProp"), we don't need to do
					// the following selection truncation.
					var hlpr = SelectionHelper.Create(vwselNew, this);
					if (!EditingHelper.IsSelectionInOneFormattableProp())
					{
						var fRange = hlpr.IsRange;
						var fChangeRange = false;
						if (fRange)
						{
							var fAnchorEditable = vwselNew.IsEditable;
							hlpr.GetIch(SelectionHelper.SelLimitType.Anchor);
							var tagAnchor = hlpr.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
							hlpr.GetIch(SelectionHelper.SelLimitType.End);
							var tagEnd = hlpr.GetTextPropId(SelectionHelper.SelLimitType.End);
							var fEndBeforeAnchor = vwselNew.EndBeforeAnchor;
							if (fEndBeforeAnchor)
							{
								if (fAnchorEditable && tagAnchor > 0 && tagEnd < 0)
								{
									hlpr.SetTextPropId(SelectionHelper.SelLimitType.End, tagAnchor);
									hlpr.SetIch(SelectionHelper.SelLimitType.End, 0);
									fChangeRange = true;
								}
							}
							else
							{
								if (!fAnchorEditable && tagAnchor < 0 && tagEnd > 0)
								{
									hlpr.SetTextPropId(SelectionHelper.SelLimitType.Anchor, tagEnd);
									hlpr.SetIch(SelectionHelper.SelLimitType.Anchor, 0);
									fChangeRange = true;
								}
							}
						}
						if (fChangeRange)
							hlpr.SetSelection(true);
					}
					if (m_fShowWsLabel)
					{
						// Might not be, especially when messing with the selection during Undoing the creation of a record.
						if (Cache.ServiceLocator.IsValidObjectId(m_hvoObj))
						{
							var tss = m_rootb.DataAccess.get_StringProp(m_hvoObj, m_flid);
							var ttp = tss.get_Properties(0);
							int var;
							var ws = ttp.GetIntPropValues((int) FwTextPropType.ktptWs, out var);
							if (ws != 0 && m_vc is StringSliceVc && ws != (m_vc as StringSliceVc).WsLabel)
							{
								m_rootb.Reconstruct();
								hlpr.SetSelection(true);
							}
						}
					}
				}
				finally
				{
					s_fProcessingSelectionChanged = false;
				}
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
			((StringSliceView) Control).SelectAt(ich);
		}
	}
}
