// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwBulletsPreview.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	#region BulletsPreview class
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Preview view in the Bullets and Numbering tab
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	internal class BulletsPreview : SimpleRootSite
	{
		#region Data members

		// This 'view' displays the string m_tssData by pretending it is property ktagText of
		// object khvoRoot.
		protected internal const int ktagText = 9001; // completely arbitrary, but recognizable.
		protected const int kfragRoot = 8002; // likewise.
		protected const int khvoRoot = 7003; // likewise.

		// Neither of these caches are used by FdoCache.
		// They are only used here.
		protected IVwCacheDa m_CacheDa; // Main cache object
		protected ISilDataAccess m_DataAccess; // Another interface on m_CacheDa.
		BulletsPreviewVc m_vc;

		//protected int m_WritingSystem; // Writing system to use when Text is set.
		protected bool m_fUsingTempWsFactory;

//		private ITsTextProps m_propsFirst;
//		private ITsTextProps m_propsOther;

		private int m_WritingSystem;
		#endregion // Data members

		#region Constructor/destructor
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public BulletsPreview()
		{
			m_CacheDa = VwCacheDaClass.Create();
			m_DataAccess = (ISilDataAccess)m_CacheDa;
			m_vc = new BulletsPreviewVc();

			// So many things blow up so badly if we don't have one of these that I finally decided to just
			// make one, even though it won't always, perhaps not often, be the one we want.
			CreateTempWritingSystemFactory();
			m_DataAccess.WritingSystemFactory = WritingSystemFactory;
			VScroll = false; // no vertical scroll bar visible.
			AutoScroll = false; // not even if the root box is bigger than the window.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				ShutDownTempWsFactory(); // Must happen before call to base.
			}

			base.Dispose(disposing);

			if (disposing)
			{
				if (m_CacheDa != null)
					m_CacheDa.ClearAllData();
			}

			m_vc = null;
			m_DataAccess = null;
			m_wsf = null;
			if (m_CacheDa != null && Marshal.IsComObject(m_CacheDa))
				Marshal.ReleaseComObject(m_CacheDa);
			m_CacheDa = null;
		}
		#endregion

		#region Temporary writing system factory methods
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Make a writing system factory that is based on the Languages folder (ICU-based).
		/// This is only used in Designer, tests, and momentarily (during construction) in
		/// production, until the client sets supplies a real one.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		private void CreateTempWritingSystemFactory()
		{
			m_wsf = new PalasoWritingSystemManager();
			m_fUsingTempWsFactory = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shut down the writing system factory and release it explicitly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ShutDownTempWsFactory()
		{
			if (m_fUsingTempWsFactory)
			{
				var disposable = m_wsf as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				m_wsf = null;
				m_fUsingTempWsFactory = false;
			}
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The writing system that should be used to construct a TsString out of a string in Text.set.
		/// If one has not been supplied use the User interface writing system from the factory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false),
			DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get
			{
				CheckDisposed();

				if (m_WritingSystem == 0)
					m_WritingSystem = WritingSystemFactory.UserWs;
				return m_WritingSystem;
			}
			set
			{
				CheckDisposed();
				m_WritingSystem = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For this class, if we haven't been given a WSF we create a default one (based on
		/// the registry). (Note this is kind of overkill, since the constructor does this too.
		/// But I left it here in case we change our minds about the constructor.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();

				if (base.WritingSystemFactory == null)
				{
					CreateTempWritingSystemFactory();
				}
				return base.WritingSystemFactory;
			}
			set
			{
				CheckDisposed();

				if (base.WritingSystemFactory != value)
				{
					ShutDownTempWsFactory();
					// when the writing system factory changes, delete any string that was there
					// and reconstruct the root box.
					base.WritingSystemFactory = value;
					// Enhance JohnT: Base class should probably do this.
					if (m_DataAccess != null)
						m_DataAccess.WritingSystemFactory = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to display the preview right to left.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsRightToLeft
		{
			get
			{
				if (m_vc != null)
					return m_vc.IsRightToLeft;
				return false;
			}
			set
			{
				if (m_vc != null)
					m_vc.IsRightToLeft = value;
			}
		}
		#endregion

		#region Overridden rootsite methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate infinite width if needed.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int GetAvailWidth(IVwRootBox prootb)
		{
			CheckDisposed();

			return ClientRectangle.Width - (HorizMargin * 2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the root box and initialize it. We want this one to work even in design mode,
		/// and since we supply the cache and data ourselves, that's possible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			//if (DesignMode)
			//    return;
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_DataAccess;
			m_rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, null);
			base.MakeRoot();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the root can be constructed in design mode.
		/// </summary>
		/// <value>Always returns <c>true</c>.</value>
		/// ------------------------------------------------------------------------------------
		protected override bool AllowPaintingInDesigner
		{
			get { return true; }
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the text properties.
		/// </summary>
		/// <param name="propsFirst">The properties for the first preview paragraph.</param>
		/// <param name="propsOther">The properties for following preview paragraphs.</param>
		/// ------------------------------------------------------------------------------------
		internal void SetProps(ITsTextProps propsFirst, ITsTextProps propsOther)
		{
//			m_propsFirst = propsFirst;
//			m_propsOther = propsOther;

			if (m_vc != null)
			{
				m_vc.SetProps(propsFirst, propsOther);
				RefreshDisplay();
			}
		}
	}
	#endregion

	#region BulletsPreviewVc class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// View constructor for the bullets preview view
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class BulletsPreviewVc : FwBaseVc
	{
		#region Data members
		private const int kdmpFakeHeight = 5000; // height for the "fake text" rectangles
		private bool m_fRtl;
		private ITsTextProps m_propsFirst;
		private ITsTextProps m_propsOther;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main method just displays the text with the appropriate properties.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// Make a "context" paragraph before the numbering starts.
			vwenv.set_IntProperty((int)FwTextPropType.ktptSpaceBefore,
				(int)FwTextPropVar.ktpvMilliPoint, 10000);
			AddPreviewPara(vwenv, null, false);

			// Make the first numbered paragraph.
			// (It's not much use if we don't have properties, but that may happen while we're starting
			// up so we need to cover it.)
			AddPreviewPara(vwenv, m_propsFirst, true);

			// Make two more numbered paragraphs.
			AddPreviewPara(vwenv, m_propsOther, true);
			AddPreviewPara(vwenv, m_propsOther, true);

			// Make a "context" paragraph after the numbering ends.
			AddPreviewPara(vwenv, null, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a paragraph (gray line) to the  preview.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="props">Text props, or <c>null</c>.</param>
		/// <param name="fAddSpaceBefore"><c>true</c> to add 6pt space before the paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void AddPreviewPara(IVwEnv vwenv, ITsTextProps props, bool fAddSpaceBefore)
		{
			// (width is -1, meaning "use the rest of the line")

			if (props != null)
				vwenv.Props = props;
			else if (fAddSpaceBefore)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptSpaceBefore,
					(int)FwTextPropVar.ktpvMilliPoint, 6000);
			}

			vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
				(int)FwTextPropVar.ktpvEnum, m_fRtl ? -1 : 0);
			vwenv.OpenParagraph();
			vwenv.AddSimpleRect(Color.LightGray.ToArgb(), -1, kdmpFakeHeight, 0);
			vwenv.CloseParagraph();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the text properties.
		/// </summary>
		/// <param name="propsFirst">The properties for the first preview paragraph.</param>
		/// <param name="propsOther">The properties for following preview paragraphs.</param>
		/// ------------------------------------------------------------------------------------
		internal void SetProps(ITsTextProps propsFirst, ITsTextProps propsOther)
		{
			m_propsFirst = propsFirst;
			m_propsOther = propsOther;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance is right to left.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsRightToLeft
		{
			get { return m_fRtl; }
			set { m_fRtl = value; }
		}
	}
	#endregion
}
