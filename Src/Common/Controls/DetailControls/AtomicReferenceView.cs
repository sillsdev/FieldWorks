// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AtomicReferenceView.cs
// Responsibility: Steve McConnel
// Last reviewed:
//
// <remarks>
// borrowed and hacked from PhoneEnvReferenceView
// </remarks>

using System;
using System.Drawing;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Main class for displaying the AtomicReferenceSlice.
	/// </summary>
	public class AtomicReferenceView : ReferenceViewBase
	{
		#region Constants and data members

		// View frags.
		public const int kFragAtomicRef = 1;
		public const int kFragObjName = 2;

		protected AtomicReferenceVc m_atomicReferenceVc;
		// this is used to guarantee correct initial size.
		private int m_hOld;
		protected string m_displayWs;

		/// <summary>
		/// This allows the view to communicate size changes to the embedding slice.
		/// </summary>
		public event FwViewSizeChangedEventHandler ViewSizeChanged;

		#endregion // Constants and data members

		#region Construction, initialization, and disposal

		public AtomicReferenceView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
			}
			m_atomicReferenceVc = null;
			m_rootObj = null;
			m_displayNameProperty = null;
		}

		public void Initialize(ICmObject rootObj, int rootFlid, string rootFieldName, FdoCache cache, string displayNameProperty, string displayWs)
		{
			CheckDisposed();
			m_displayWs = displayWs;
			Initialize(rootObj, rootFlid, rootFieldName, cache, displayNameProperty);
		}

		/// <summary>
		/// Set the item from the chooser.
		/// </summary>
		/// <param name="obj">the object from the chooser.</param>
		public void SetObject(ICmObject obj)
		{
			CheckDisposed();
			m_rootObj = obj;
			if (m_rootb != null)
			{
				SetRootBoxObj();
				int h2 = m_rootb.Height;
				if (m_hOld != h2)
				{
					m_hOld = h2;
					if (ViewSizeChanged != null)
						ViewSizeChanged(this, new FwViewSizeEventArgs(h2, m_rootb.Width));
				}
			}
		}

		public ICmObject Object
		{
			get
			{
				CheckDisposed();
				return m_rootObj;
			}
		}

		protected virtual void SetRootBoxObj()
		{
			if (m_rootObj != null && m_rootObj.IsValidObject) // if not, hopefully the parent slice eventually gets deleted!
			{
				// The ViewSizeChanged logic should be triggered automatically by a notification
				// from the rootbox.
				int h1 = m_rootb.Height;
				m_rootb.SetRootObject(m_rootObj.Hvo, m_atomicReferenceVc, kFragAtomicRef, m_rootb.Stylesheet);
				if (h1 != m_rootb.Height)
				{
					if (ViewSizeChanged != null)
						ViewSizeChanged(this, new FwViewSizeEventArgs(m_rootb.Height, m_rootb.Width));
				}
			}
		}

		/// <summary>
		/// Get any text styles from configuration node (which is now available; it was not at construction)
		/// </summary>
		/// <param name="configurationNode"></param>
		public void FinishInit(XmlNode configurationNode)
		{
			if (configurationNode.Attributes != null)
			{
				var textStyle = configurationNode.Attributes["textStyle"];
				if (textStyle != null)
				{
					TextStyle = textStyle.Value;
					if (m_atomicReferenceVc != null)
					{
						m_atomicReferenceVc.TextStyle = textStyle.Value;
					}
				}
			}
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			SetReferenceVc();
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = GetDataAccess();
			SetRootBoxObj();
		}

		protected virtual ISilDataAccess GetDataAccess()
		{
			return m_fdoCache.DomainDataByFlid;
		}

		public virtual void SetReferenceVc()
		{
			CheckDisposed();
			m_atomicReferenceVc = new AtomicReferenceVc(m_fdoCache, m_rootFlid, m_displayNameProperty);
		}

		#endregion // RootSite required methods

		#region other overrides

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the focus leaves the control. We want to hide the selection.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);

			if (RootBox != null)
				RootBox.DestroySelection();
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			if (vwselNew == null)
				return;
			int cvsli = vwselNew.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			cvsli--;
			if (cvsli == 0)
			{
				// No objects in selection: don't allow a selection.
				m_rootb.DestroySelection();
				// Enhance: invoke launcher's selection dialog.
				return;
			}
			ITsString tss;
			int ichAnchor;
			int ichEnd;
			bool fAssocPrev;
			int hvoObj;
			int hvoObjEnd;
			int tag;
			int ws;
			vwselNew.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj,
				out tag, out ws);
			vwselNew.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjEnd,
				out tag, out ws);
			if (hvoObj != hvoObjEnd)
				return;
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ihvoEnd;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);
			IVwSelection vwselWhole = null;
			Debug.Assert(m_rootb != null);
			// Create a selection that covers the entire target object.  If it differs from
			// the new selection, we'll install it (which will recurse back to this method).
			vwselWhole = m_rootb.MakeTextSelInObj(ihvoRoot, cvsli, rgvsli, 0, null,
				false, false, false, true, false);
			if (vwselWhole != null)
			{
				ITsString tssWhole;
				int ichAnchorWhole;
				int ichEndWhole;
				int hvoObjWhole;
				int hvoObjEndWhole;
				bool fAssocPrevWhole;
				int tagWhole;
				int wsWhole;
				vwselWhole.TextSelInfo(false, out tssWhole, out ichAnchorWhole,
					out fAssocPrevWhole, out hvoObjWhole, out tagWhole, out wsWhole);
				vwselWhole.TextSelInfo(true, out tssWhole, out ichEndWhole,
					out fAssocPrevWhole, out hvoObjEndWhole, out tagWhole, out wsWhole);
				if (hvoObj == hvoObjWhole && hvoObjEnd == hvoObjEndWhole &&
					(ichAnchor != ichAnchorWhole || ichEnd != ichEndWhole))
				{
					// Install it this time!
					m_rootb.MakeTextSelInObj(ihvoRoot, cvsli, rgvsli, 0, null,
						false, false, false, true, true);
				}
			}
		}

		#endregion

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// AtomicReferenceView
			//
			this.Name = "AtomicReferenceView";
			this.Size = new System.Drawing.Size(232, 18);
		}
		#endregion

		#region Properties

		#endregion
	}

	#region AtomicReferenceVc class

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class AtomicReferenceVc : FwBaseVc
	{
		protected int m_flid;
		protected string m_displayNameProperty;
		private string m_textStyle;

		public AtomicReferenceVc(FdoCache cache, int flid, string displayNameProperty)
		{
			Debug.Assert(cache != null);
			Cache = cache;
			m_flid = flid;
			m_displayNameProperty = displayNameProperty;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case AtomicReferenceView.kFragAtomicRef:
					// Display a paragraph with a single item.
					int hvoProp = HvoOfObjectToDisplay(vwenv, hvo);
					if (hvoProp == 0)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
							(int)FwTextPropVar.ktpvDefault,
							(int)ColorUtil.ConvertColorToBGR(Color.Gray));
						vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent,
							(int)FwTextPropVar.ktpvMilliPoint, 18000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
							(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);

						//vwenv.AddString(m_cache.MakeUserTss("Click to select -->"));
						vwenv.NoteDependency(new int[] {hvo}, new int[] {m_flid}, 1);
					}
					else
					{
						vwenv.OpenParagraph();		// vwenv.OpenMappedPara();
						DisplayObjectProperty(vwenv, hvoProp);
						vwenv.CloseParagraph();
					}
					break;
				case AtomicReferenceView.kFragObjName:
					// Display one reference.
				{
					ILgWritingSystemFactory wsf =
						m_cache.WritingSystemFactory;

					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvDefault,
						(int)TptEditable.ktptNotEditable);
					ITsString tss;
					ITsStrFactory tsf = m_cache.TsStrFactory;
					Debug.Assert(hvo != 0);
					// Use reflection to get a prebuilt name if we can.  Otherwise
					// settle for piecing together a string.
					Debug.Assert(m_cache != null);
					var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					Debug.Assert(obj != null);
					System.Type type = obj.GetType();
					System.Reflection.PropertyInfo pi = type.GetProperty("TsName",
						System.Reflection.BindingFlags.Instance |
						System.Reflection.BindingFlags.Public |
						System.Reflection.BindingFlags.FlattenHierarchy);
					if (pi != null)
					{
						tss = (ITsString)pi.GetValue(obj, null);
					}
					else
					{
						if (!string.IsNullOrEmpty(m_displayNameProperty))
						{
							pi = type.GetProperty(m_displayNameProperty,
								System.Reflection.BindingFlags.Instance |
								System.Reflection.BindingFlags.Public |
								System.Reflection.BindingFlags.FlattenHierarchy);
						}
						int ws = wsf.GetWsFromStr(obj.SortKeyWs);
						if (ws == 0)
							ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
						if (pi != null)
						{
							object info = pi.GetValue(obj, null);
							// handle the object type
							if (info is String)
								tss = tsf.MakeString((string)info, ws);
							else if (info is IMultiUnicode)
							{
								var accessor = info as IMultiUnicode;
								tss = accessor.get_String(ws); // try the requested one (or default analysis)
								if (tss == null || tss.Length == 0)
									tss = accessor.BestAnalysisVernacularAlternative; // get something
							}
							else if (info is ITsString)
								tss = (ITsString)info;
							else
								tss = null;
						}
						else
						{
							tss = obj.ShortNameTSS; // prefer this, which is hopefully smart about wss.
							if (tss == null || tss.Length == 0)
							{
								tss = tsf.MakeString(obj.ShortName, ws);
							}
						}
					}
					if (!string.IsNullOrEmpty(TextStyle))
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, TextStyle);

					}
					vwenv.AddString(tss);
				}
					break;
				default:
					throw new ArgumentException(
						"Don't know what to do with the given frag.", "frag");
			}
		}

		protected virtual void DisplayObjectProperty(IVwEnv vwenv, int hvo)
		{
			vwenv.AddObjProp(m_flid, this, AtomicReferenceView.kFragObjName);
		}

		protected virtual int HvoOfObjectToDisplay(IVwEnv vwenv, int hvo)
		{
			ISilDataAccess sda = vwenv.DataAccess;
			return sda.get_ObjectProp(hvo, m_flid);
		}
		public string TextStyle
		{
			get
			{
				string sTextStyle = "Default Paragraph Characters";
				if (!string.IsNullOrEmpty(m_textStyle))
				{
					sTextStyle = m_textStyle;
				}
				return sTextStyle;
			}
			set
			{
					m_textStyle = value;
			}
		}

	}

	#endregion // AtomicReferenceVc class
}
