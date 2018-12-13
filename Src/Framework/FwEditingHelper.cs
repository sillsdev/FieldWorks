// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Extended version of EditingHelper that uses some framework features.
	/// </summary>
	public class FwEditingHelper : RootSiteEditingHelper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FwEditingHelper"/> class.
		/// </summary>
		public FwEditingHelper(LcmCache cache, IEditingCallbacks callbacks)
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
			if (IsDisposed)
			{
				// Must not be run more than once.
				return;
			}

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

		/// <summary>
		/// Change the system keyboard when the selection changes.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew">Selection</param>
		/// <remarks>When overriding you should call the base class first.</remarks>
		public override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			if (IgnoreSelectionChanges)
			{
				return;
			}
			try
			{
				base.HandleSelectionChange(prootb, vwselNew);
			}
			catch (COMException e)
			{
				Debug.WriteLine("Got exception in HandleSelectionChanged: " + e.Message);
			}
		}

		/// <summary>
		/// Insert a picture at the current selection of the active rootsite.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		public void InsertPicture(string srcFilename, ITsString captionTss, string sFolder)
		{
			// Create the picture and add the ORC to the text at the insertion point.
			InsertPicture(m_cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(srcFilename, captionTss, sFolder));
		}
		#endregion

		#region Hyperlink stuff

		/// <summary>
		/// Handle typed character
		/// </summary>
		protected override void OnCharAux(string stuInput, VwShiftStatus ss, Keys modifiers)
		{
			if (string.IsNullOrEmpty(stuInput))
			{
				return;
			}
			var currSel = CurrentSelection;
			if (modifiers != Keys.Control && modifiers != Keys.Alt && currSel != null)
			{
				var ttpTop = currSel.GetSelProps(SelLimitType.Top);
				if (ttpTop != null)
				{
					var sObjData = ttpTop.GetStrPropValue((int)FwTextPropType.ktptObjData);
					var urlTop = TsStringUtils.GetURL(sObjData);
					if (urlTop != null)
					{
						var ttpBottom = currSel.GetSelProps(SelLimitType.Bottom);
						if (ttpBottom != null)
						{
							sObjData = ttpBottom.GetStrPropValue((int)FwTextPropType.ktptObjData);
							var urlBottom = TsStringUtils.GetURL(sObjData);
							if (urlBottom != urlTop)
							{
								int nVar;
								var propsToUse = StyleUtils.CharStyleTextProps(null, ttpTop.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
								currSel.Selection.SetTypingProps(propsToUse);
							}
							else if (stuInput[0] == (char)VwSpecialChars.kscDelForward || stuInput[0] == (char)VwSpecialChars.kscBackspace)
							{
								if (currSel.IsRange)
								{
									var ttpBefore = currSel.PropsBefore;
									var urlBefore = (ttpBefore == null) ? null : TsStringUtils.GetURL(ttpBefore.GetStrPropValue((int)FwTextPropType.ktptObjData));
									var ttpAfter = currSel.PropsAfter;
									var urlAfter = (ttpAfter == null) ? null : TsStringUtils.GetURL(ttpAfter.GetStrPropValue((int)FwTextPropType.ktptObjData));

									if (urlBefore != urlTop && urlAfter != urlTop)
									{
										int nVar;
										var propsToUse = ttpBefore ?? StyleUtils.CharStyleTextProps(null, ttpTop.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
										currSel.Selection.SetTypingProps(propsToUse);
									}
								}
							}
							else if (!currSel.IsRange)
							{
								var ttpBefore = currSel.PropsBefore;
								var urlBefore = (ttpBefore == null) ? null : TsStringUtils.GetURL(ttpBefore.GetStrPropValue((int)FwTextPropType.ktptObjData));
								if (urlBefore != urlTop)
								{
									int nVar;
									var propsToUse = ttpBefore ?? StyleUtils.CharStyleTextProps(null, ttpTop.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
									currSel.Selection.SetTypingProps(propsToUse);
								}
								else
								{
									var ttpAfter = currSel.PropsAfter;
									var urlAfter = (ttpAfter == null) ? null : TsStringUtils.GetURL(ttpAfter.GetStrPropValue((int)FwTextPropType.ktptObjData));

									if (urlAfter != urlBottom)
									{
										int nVar;
										var propsToUse = ttpAfter ?? StyleUtils.CharStyleTextProps(null, ttpTop.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
										currSel.Selection.SetTypingProps(propsToUse);
									}
								}
							}
						}
					}
				}
			}
			base.OnCharAux(stuInput, ss, modifiers);
		}

		/// <summary>
		/// Appends the given text as a hyperlink to the given URL.
		/// </summary>
		/// <param name="strBldr">The string builder.</param>
		/// <param name="ws">The HVO of the writing system to use for the added text.</param>
		/// <param name="sLinkText">The text which should appear as the hyperlink text</param>
		/// <param name="sUrl">The URL that is the target of the hyperlink.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <returns><c>true</c> if the hyperlink was successfully inserted; <c>false</c>
		/// otherwise (indicating that the hyperlink style could not be found in the given
		/// stylesheet). In either case, the link text will be appended to the string builder.
		/// </returns>
		public static bool AddHyperlink(ITsStrBldr strBldr, int ws, string sLinkText, string sUrl, LcmStyleSheet stylesheet)
		{
			var hyperlinkStyle = stylesheet.FindStyle(StyleServices.Hyperlink);
			if (hyperlinkStyle == null)
			{
				return false;
			}
			if (stylesheet.Cache?.ProjectId != null)
			{
				sUrl = FwLinkArgs.FixSilfwUrlForCurrentProject(sUrl, stylesheet.Cache.ProjectId.Name);
			}
			var ichStart = strBldr.Length;
			strBldr.Replace(ichStart, ichStart, sLinkText, StyleUtils.CharStyleTextProps(null, ws));
			StringServices.MarkTextInBldrAsHyperlink(strBldr, ichStart, strBldr.Length, sUrl, hyperlinkStyle);
			return true;
		}
		#endregion

		#region Public Properties

		/// <summary>
		/// Gets the HVO of the selected picture. If a picture isn't selected, then zero is
		/// returned.
		/// </summary>
		public ICmPicture Picture
		{
			get
			{
				var helper = CurrentSelection;
				ICmPicture picture = null;
				if (helper != null && helper.LevelInfo.Length > 0)
				{
					m_cache.ServiceLocator.GetInstance<ICmPictureRepository>().TryGetObject(helper.LevelInfo[0].hvo, out picture);
				}
				return picture;
			}
		}

		/// <summary>
		/// Gets a value indicating whether or not a picture is selected. This version of the
		/// method also answers true if the selection is in a picture caption.
		/// </summary>
		public virtual bool IsPictureSelected => (Picture != null);

		/// <summary>
		/// Gets a value indicating whether or not a picture is selected. This version answers
		/// true only if the selection is truly of the picture itself.
		/// </summary>
		public virtual bool IsPictureReallySelected => IsPictureSelected && CurrentSelection.Selection.SelType == VwSelType.kstPicture;

		/// <summary>
		/// Represents a style context that can be applied anywhere in the view that
		/// this EditingHelper belongs to
		/// </summary>
		public virtual ContextValues InternalContext { get; set; } = ContextValues.General;

		/// <summary>
		/// Set to true to ignore selection changes.
		/// NOTE: This is mainly needed for synchronizing scrolling of the draft view with the
		/// footnote view which causes the styles combobox to show the wrong style name.
		/// (TE-1325)
		/// </summary>
		public static bool IgnoreSelectionChanges { get; set; }

		/// <summary>
		/// Determine if the char style combo box should always be refreshed on selection
		/// changes.
		/// </summary>
		/// <remarks>Override if your editing helper ever needs to force this</remarks>
		public virtual bool ForceCharStyleComboRefresh => false;
		#endregion

		#region Private methods

		/// <summary>
		/// Inserts an ORC pointing to hvoObjToInsert at the current selection location.
		/// Enhance JohnT: should move this to RootSite or similar location where all clients
		/// can readily use it.
		/// </summary>
		public void InsertPicture(ICmPicture pict)
		{
			// get selection information
			ITsString tss;
			int ich;
			bool fAssocPrev;
			int hvoObj;
			int ws;
			int propTag;
			var helper = CurrentSelection;
			var sel = helper.Selection;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out propTag, out ws);

			// If inserting a picture over a user prompt, need to set up info for a proper insertion
			// in the empty paragraph.
			if (propTag == SimpleRootSite.kTagUserPrompt)
			{
				ich = 0;
				tss = TsStringUtils.EmptyString(m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
				propTag = StTxtParaTags.kflidContents;
				helper.SetTextPropId(SelLimitType.Anchor, StTxtParaTags.kflidContents);
				helper.SetTextPropId(SelLimitType.End, StTxtParaTags.kflidContents);
			}
			else if (tss == null)
			{
				helper = GetSelectionReducedToIp(SelLimitType.Top);
				if (helper != null)
				{
					helper.Selection.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out propTag, out ws);
				}
			}
			if (tss == null)
			{
				throw new InvalidOperationException("Attempt to insert a picture in an invalid location.");
			}
			InsertPictureOrc(pict, tss, ich, hvoObj, propTag, ws);
			helper.IchAnchor = helper.IchEnd = ich + 1;
			helper.SetIPAfterUOW();
		}

		/// <summary>
		/// This method is broken out so TeEditingHelper can override and adjust annotations.
		/// Probably anything else that does it will need to adjust annotations, too, but nothing
		/// else yet uses this kind of picture in an StText.
		/// </summary>
		/// <param name="pict">The picture to insert an ORC for</param>
		/// <param name="tss">The initial value of the TsString into which the ORC is to be
		/// inserted.</param>
		/// <param name="ich">The character offset into the string.</param>
		/// <param name="hvoObj">The hvo of the object that owns the string property that will
		/// receive the tss after the ORC is inserted (typically a paragraph).</param>
		/// <param name="propTag">The property that holds the string.</param>
		/// <param name="ws">The writing system ID if the property is a multi-string.</param>
		protected virtual void InsertPictureOrc(ICmPicture pict, ITsString tss, int ich, int hvoObj, int propTag, int ws)
		{
			var newTss = pict.InsertORCAt(tss, ich);
			if (ws == 0)
			{
				m_cache.DomainDataByFlid.SetString(hvoObj, propTag, newTss);
			}
			else
			{
				m_cache.DomainDataByFlid.SetMultiStringAlt(hvoObj, propTag, ws, newTss);
			}
		}

		#endregion

		#region Overrides of SimpleRootSite's EditingHelper

		/// <summary>
		/// Determines whether the given TsString can be pasted in the context of the target
		/// selection. An old DN-style embedded picture can only be pasted into paragraph
		/// contents.
		/// </summary>
		/// <param name="vwselTargetLocation">selection to be replaced by paste operation</param>
		/// <param name="tss">The TsString from the clipboard</param>
		/// <returns><c>true</c> if the given string does not contain any embedded strings or if
		/// the target location is a paragraph contents field.</returns>
		protected override bool ValidToReplaceSelWithTss(IVwSelection vwselTargetLocation, ITsString tss)
		{
			// REVIEW (EberhardB): Do we still support DN-style embedded pictures?
			// Begin fix for Raid bug 897B
			// Check for an embedded picture.
			var crun = tss.RunCount;
			var fHasPicture = false;
			ITsTextProps ttp;
			for (var irun = 0; irun < crun; ++irun)
			{
				ttp = tss.get_Properties(irun);
				var str = ttp.GetStrPropValue((int)FwTextStringProp.kstpObjData);
				if (str != null)
				{
					var chType = str[0];
					if (chType == (int)FwObjDataTypes.kodtPictOddHot || chType == (int)FwObjDataTypes.kodtPictEvenHot)
					{
						fHasPicture = true;
						break;
					}
				}
			}

			if (fHasPicture)
			{
				// Vars to call TextSelInfo and find out whether it is a structured
				// text field.
				ITsString tssDummy;
				int ich;
				bool fAssocPrev;
				int hvoObj;
				int tag;
				int wsTmp;
				vwselTargetLocation.TextSelInfo(false, out tssDummy, out ich, out fAssocPrev, out hvoObj, out tag, out wsTmp);
				if (tag != StTxtParaTags.kflidContents)
				{
					// TODO (EberhardB): This seems to be Notebook specific!
					MessageBox.Show(ResourceHelper.GetResourceString("kstidPicsMultiPara"));
					return false;
				}
			}
			//- End fix for Raid bug 897B
			return base.ValidToReplaceSelWithTss(vwselTargetLocation, tss);
		}
		#endregion
	}
}