// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwEditingHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extended version of EditingHelper that uses some framework features.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwEditingHelper : RootSiteEditingHelper
	{
		#region Data members
		private static bool s_fIgnoreSelectionChanges = false;
		/// <summary>
		/// Styles that should be allowed to be applied in the Apply Styles dialog
		/// </summary>
		private List<ContextValues> m_applicableStylesContexts =
			new List<ContextValues>(new ContextValues[] { ContextValues.General });
		/// <summary>Represents a style context that can be applied anywhere in the view that
		/// this EditingHelper belongs to</summary>
		private ContextValues m_internalContext = ContextValues.General;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwEditingHelper"/> class.
		/// </summary>
		/// <param name="cache">The DB connection</param>
		/// <param name="callbacks">implementation of <see cref="IEditingCallbacks"/></param>
		/// ------------------------------------------------------------------------------------
		public FwEditingHelper(FdoCache cache, IEditingCallbacks callbacks)
			: base(cache, callbacks)
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

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
		}

		#endregion IDisposable override

		#region Public methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Change the system keyboard when the selection changes.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew">Selection</param>
		/// <remarks>When overriding you should call the base class first.</remarks>
		/// -----------------------------------------------------------------------------------
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			if (s_fIgnoreSelectionChanges)
				return;
			try
			{
				base.SelectionChanged(prootb, vwselNew);
				// JohnT: it's remotely possible that the base, in calling commit, made this
				// selection no longer useable.
				if (!vwselNew.IsValid || TheMainWnd == null)
					return;
				TheMainWnd.UpdateStyleComboBoxValue(Callbacks as IRootSite);
				TheMainWnd.UpdateWritingSystemSelectorForSelection(Callbacks.EditedRootBox);
			}
			catch(Exception e)
			{
				Debug.WriteLine("Got exception in FwRootSite.HandleSelectionChanged: "
					+ e.Message);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a picture at the current selection of the active rootsite.
		/// </summary>
		/// <param name="initialPicture">The existing CmPicture being modified, if any</param>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// ------------------------------------------------------------------------------------
		public void UpdatePicture(CmPicture initialPicture, string srcFilename,
			ITsString captionTss, string sFolder)
		{
			CheckDisposed();

			initialPicture.UpdatePicture(srcFilename, captionTss, sFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a picture at the current selection of the active rootsite.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// ------------------------------------------------------------------------------------
		public CmPicture InsertPicture(string srcFilename, ITsString captionTss, string sFolder)
		{
			CheckDisposed();

			CmPicture pict = new CmPicture(m_cache, srcFilename, captionTss, sFolder);
			// Add the ORC to the text at the insertion point.
			InsertPicture(pict);
			return pict;
		}
		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the containing Main Window, an FwMainWnd.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual FwMainWnd TheMainWnd
		{
			get
			{
				CheckDisposed();
				return Control.FindForm() as FwMainWnd;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the containing Client Window, as an ISelectableView.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ISelectableView TheClientWnd
		{
			get
			{
				CheckDisposed();

				// In TeMainWnd draft views, the individual panes are created and then they are
				// added to the client window and their ownership reassigned to the client window.
				// This property is called before ownership is reassigned, so attempting to use a
				// saved m_theClientWnd here produces an incorrect result.
				//if (m_theClientWnd != null)
				//	return m_theClientWnd;

				Control ctrl = Control;
				while (ctrl != null)
				{
					if (ctrl is ISelectableView && !(ctrl.Parent is ISelectableView))
						return (ISelectableView)ctrl;

					ctrl = ctrl.Parent;
				}

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not a picture is selected. This version of the
		/// method also answers true if the selection is in a picture caption.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsPictureSelected
		{
			get
			{
				CheckDisposed();

				SelectionHelper helper = CurrentSelection;
				return (helper != null && helper.LevelInfo.Length > 0 &&
					m_cache.GetClassOfObject(helper.LevelInfo[0].hvo) == CmPicture.kClassId);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not a picture is selected. This version answers
		/// true only if the selection is truly of the picture itself.
		/// Todo TETeam(JohnT): most of the calls to IsPictureSelected (but maybe not the
		/// ones that enable commands like insert verse ref and insert section?) should
		/// probably use this rather than the other one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsPictureReallySelected
		{
			get
			{
				CheckDisposed();

				//note: IsPictureSelected() checks that CurrentSelection in not null
				return IsPictureSelected && CurrentSelection.Selection.SelType ==
					VwSelType.kstPicture;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Represents a style context that can be applied anywhere in the view that
		/// this EditingHelper belongs to
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ContextValues InternalContext
		{
			get
			{
				CheckDisposed();
				return m_internalContext;
			}
			set
			{
				CheckDisposed();
				m_internalContext = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the applicable style contexts which get passed to the styles dialog.
		/// </summary>
		/// <remarks>Returning <c>null</c> means that the applicable contexts won't be set,
		///  i.e. all styles allowed.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual List<ContextValues> ApplicableStyleContexts
		{
			get
			{
				CheckDisposed();
				return m_applicableStylesContexts;
			}
			set
			{
				CheckDisposed();
				m_applicableStylesContexts = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set to true to ignore selection changes.
		/// NOTE: This is mainly needed for syncronizing scrolling of the draft view with the
		/// footnote view which causes the styles combobox to show the wrong style name.
		/// (TE-1325)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IgnoreSelectionChanges
		{
			get { return s_fIgnoreSelectionChanges; }
			set { s_fIgnoreSelectionChanges = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the char style combo box should always be refreshed on selection
		/// changes.
		/// </summary>
		/// <value>
		/// 	Always <c>false</c>.
		/// </value>
		/// <remarks>Override if your editing helper ever needs to force this</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool ForceCharStyleComboRefresh
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a ORC pointing to hvoObjToInsert at the current selection location.
		/// Enhance JohnT: should move this to RootSite or similar location where all clients
		/// can readily use it.
		/// </summary>
		/// <param name="pict">The picture to insert</param>
		/// ------------------------------------------------------------------------------------
		private void InsertPicture(CmPicture pict)
		{
			// get selection information
			ITsString tss;
			int ich;
			bool fAssocPrev;
			int hvoObj;
			int ws;
			int propTag;
			CurrentSelection.Selection.TextSelInfo(true, out tss, out ich, out fAssocPrev,
				out hvoObj, out propTag, out ws);
			SelectionHelper oldSelection = CurrentSelection;

			// If inserting a picture over a user prompt, need to set up info for a proper insertion
			// in the empty paragraph.
			if (propTag == SimpleRootSite.kTagUserPrompt)
			{
				ich = 0;
				ITsStrFactory factory = TsStrFactoryClass.Create();
				tss = factory.MakeString(string.Empty, m_cache.DefaultVernWs);
				propTag = (int)StTxtPara.StTxtParaTags.kflidContents;
			}

			InsertPictureOrc(pict, tss, ich, hvoObj, propTag, ws);
			SelectionHelper helper = CurrentSelection;
			if (helper == null)
			{
				oldSelection.SetTextPropId(SelectionHelper.SelLimitType.Anchor, (int)StTxtPara.StTxtParaTags.kflidContents);
				oldSelection.SetTextPropId(SelectionHelper.SelLimitType.End, (int)StTxtPara.StTxtParaTags.kflidContents);
				oldSelection.IchAnchor = 0;
				oldSelection.IchEnd = 0;
				oldSelection.SetSelection(true);
				Debug.Assert(CurrentSelection != null);
			}
			else
			{
				helper.IchAnchor = helper.IchEnd = ich + 1;
				helper.SetSelection(true);
			}
		}

		/// <summary>
		/// This method is broken out so TeEditingHelper can override and adjust annotations.
		/// Probably anything else that does it will need to adjust annotations, too, but nothing
		/// else yet uses this kind of picture in an StText.
		/// </summary>
		protected virtual void InsertPictureOrc(CmPicture pict, ITsString tss, int ich, int hvoObj, int propTag, int ws)
		{
			pict.InsertORCAt(tss, ich, hvoObj, propTag, ws);
		}

		#endregion

	}
}
