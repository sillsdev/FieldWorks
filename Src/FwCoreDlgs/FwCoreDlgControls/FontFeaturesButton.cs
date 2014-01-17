using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Font Features button
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FontFeaturesButton : Button, IFWDisposable
	{
		#region Member variables and constants
		/// <summary></summary>
		public const int kGrLangFeature = 1; // See FmtFntDlg.h for real defn.
		/// <summary></summary>
		public const int kMaxValPerFeat = 32; // See FmtFntDlg.h for real defn.
		// This is copied from nLang in FmtFntDlg.cpp, FmtFntDlg::CreateFeaturesMenu.
		/// <summary></summary>
		public const int kUiCodePage = 0x00000409;	// for now the UI language is US English
		// If this ever changes, some constant strings below may need to change also.
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components;
		private string m_fontName; // The font for which we are editing the features.
		private string m_fontFeatures; // The font feature string stored in the writing system.
		private IRenderingFeatures m_featureEngine;
		private ILgWritingSystemFactory m_wsf;
		private int[] m_values;	// The actual list of values we're editing.
		private int[] m_ids;		// The corresponding ids.
		private bool m_isGraphiteFont;
		#endregion

		#region Constructor and dispose stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FontFeaturesButton"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FontFeaturesButton()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			this.Image = SIL.FieldWorks.Resources.ResourceHelper.ButtonMenuArrowIcon;
			this.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.Text = FwCoreDlgControls.kstidFontFeatures;
			this.Enabled = false;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}
		#endregion

		#region Class HoldDummyGraphics
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class HoldDummyGraphics: IDisposable
		{

			/// <summary></summary>
			public IVwGraphics m_vwGraphics;
			/// <summary></summary>
			public Graphics m_graphics;
			/// <summary></summary>
			private IntPtr m_hdc;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:HoldDummyGraphics"/> class.
			/// </summary>
			/// <param name="fontName">Name of the font.</param>
			/// <param name="fBold">if set to <c>true</c> [f bold].</param>
			/// <param name="fItalic">if set to <c>true</c> [f italic].</param>
			/// <param name="ctrl">The parent control</param>
			/// --------------------------------------------------------------------------------
			public HoldDummyGraphics(string fontName, bool fBold, bool fItalic, Control ctrl)
			{
				// Make a VwGraphics and initialize it.
				IVwGraphicsWin32 vwGraphics32 = VwGraphicsWin32Class.Create();
				m_vwGraphics = (IVwGraphics)vwGraphics32;
				m_graphics = ctrl.CreateGraphics();
				m_hdc = m_graphics.GetHdc();
				((IVwGraphicsWin32)m_vwGraphics).Initialize(m_hdc);

				// Select our font into it.
				LgCharRenderProps chrp = new LgCharRenderProps();
				chrp.szFaceName = new ushort[32];
				for (int ich = 0; ich < fontName.Length; ++ich)
				{
					if (ich < 32)
						chrp.szFaceName[ich] = (ushort)fontName[ich];
				}
				if (fontName.Length < 32)
					chrp.szFaceName[fontName.Length] = 0;
				else
					chrp.szFaceName[31] = 0;
				chrp.ttvBold = (int)(fBold ? FwTextToggleVal.kttvForceOn
					: FwTextToggleVal.kttvOff);
				chrp.ttvItalic = (int)(fItalic ? FwTextToggleVal.kttvForceOn
					: FwTextToggleVal.kttvOff);
				m_vwGraphics.SetupGraphics(ref chrp);
			}

			#region Disposable stuff
			#if DEBUG
			/// <summary/>
			~HoldDummyGraphics()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public bool IsDisposed
			{
				get;
				private set;
			}

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					if (m_vwGraphics != null)
						m_vwGraphics.ReleaseDC();
					if (m_graphics != null)
					{
						m_graphics.ReleaseHdc(m_hdc);
						m_graphics.Dispose();
					}
				}
				m_vwGraphics = null;
				m_graphics = null;
				m_hdc = IntPtr.Zero;
				IsDisposed = true;
			}
			#endregion

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Closes this instance.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void Close()
			{
				Dispose();
			}
		}
		#endregion // class HoldDummyGraphics

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the writing system factory.
		/// </summary>
		/// <value>The writing system factory.</value>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();
				return m_wsf;
			}
			set
			{
				CheckDisposed();
				m_wsf = value;
			}
		}

		/// <summary>Event that occurs when the user chooses a font feature.</summary>
		public event EventHandler FontFeatureSelected;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the FontFeatureSelected event; by default just calls delegates.
		/// </summary>
		/// <param name="ea">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnFontFeatureSelected(EventArgs ea)
		{
			if (FontFeatureSelected != null)
				FontFeatureSelected(this, ea);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the font for which we are selecting features.
		/// </summary>
		/// <value>The name of the font.</value>
		/// ------------------------------------------------------------------------------------
		public string FontName
		{
			get
			{
				CheckDisposed();
				return m_fontName;
			}
			set
			{
				CheckDisposed();
				if (m_fontName == value)
					return;
				m_fontName = value;
				SetupFontFeatures();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/Set the actual feature string we are editing.
		/// </summary>
		/// <value>The font features.</value>
		/// ------------------------------------------------------------------------------------
		public string FontFeatures
		{
			get
			{
				CheckDisposed();
				return m_fontFeatures;
			}
			set
			{
				CheckDisposed();
				m_fontFeatures = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the currently selected font is a Graphite font.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the font is a Graphite font, otherwise <c>false</c>.
		/// </value>
		public bool IsGraphiteFont
		{
			get
			{
				CheckDisposed();

				return m_isGraphiteFont;
			}
		}

#if !__MonoCS__
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fonts the has graphite tables.
		/// </summary>
		/// <param name="fontName">Name of the font.</param>
		/// <param name="fBold">if set to <c>true</c> [f bold].</param>
		/// <param name="fItalic">if set to <c>true</c> [f italic].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool FontHasGraphiteTables(string fontName, bool fBold, bool fItalic)
		{
			try
			{
				using (HoldDummyGraphics hdg = new HoldDummyGraphics(fontName, fBold, fItalic, this))
				{
					// Ask it whether that font has the main Graphite data table. If it does,
					// we assume it is a Graphite font.
					int tblSize;
					const int tag_Silf = 0x666c6953;
					hdg.m_vwGraphics.GetFontData(tag_Silf, out tblSize);
					if (tblSize > 0)
						return true;
				}
			}
			catch
			{
				return false;
			}

			// This has not yet been fully tested, should be equivalent to the following C++.

			//			StrApp str("Software\\SIL\\GraphiteFonts");
			//			bool f;
			//			RegKey hkey;
			//			f = ::RegOpenKeyEx(HKEY_LOCAL_MACHINE, str, 0, katRead, &hkey);
			//			if (f != ERROR_SUCCESS)
			//				return false;
			//
			//			DWORD dwIndex = 0;
			//			for ( ; ; )
			//			{
			//				achar rgch[256];
			//				DWORD cb = isizeof(rgch);
			//				LONG l = ::RegEnumKeyEx(hkey, dwIndex, rgch, &cb, NULL, NULL, NULL, NULL);
			//				if (_tcscmp(pszFontKey, rgch) == 0)
			//					return true;
			//				else if (l == ERROR_NO_MORE_ITEMS)
			//					return false;
			//				else if (l != ERROR_SUCCESS)
			//					return false;
			//				dwIndex++;
			//			}

			// This code is a fallback for finding old Graphite fonts installed using the
			// deprecated GrFontInst program.
			// On a few broken Windows machines, normal registry access to HKLM returns null (LT-15158).
			if (RegistryHelper.CompanyKeyLocalMachine == null)
				return false;

			using (RegistryKey regKey = RegistryHelper.CompanyKeyLocalMachine.OpenSubKey("GraphiteFonts"))
			{
				if (regKey == null)
					return false;
				string[] subkeys = regKey.GetSubKeyNames();
				foreach (string key in subkeys)
					if (key == fontName)
						return true;
				return false;
			}
		}
#endif
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setups the font features.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetupFontFeatures()
		{
			CheckDisposed();

#if __MonoCS__
			// TODO-Linux: Neither Graphite or UniscribeEngine Avaliable
			m_featureEngine = null;
			return;
#else
			if (m_fontName == null || m_fontName == "")
			{
				Enabled = false;
				m_isGraphiteFont = false;
				return;
			}
			IRenderEngine renderer;
			if (FontHasGraphiteTables(m_fontName, false, false))
			{
				renderer = FwGrEngineClass.Create();
				m_isGraphiteFont = true;
			}
			else
			{
				renderer = UniscribeEngineClass.Create();
				m_isGraphiteFont = false;
			}
			renderer.WritingSystemFactory = m_wsf;
			using (HoldDummyGraphics hdg = new HoldDummyGraphics(m_fontName, false, false, this))
			{
			renderer.InitRenderer(hdg.m_vwGraphics, m_fontFeatures);
			m_featureEngine = renderer as IRenderingFeatures;
			if (m_featureEngine == null)
			{
				Enabled = false;
				return;
			}
			int cfid;
			m_featureEngine.GetFeatureIDs(0, null, out cfid);
			if (cfid == 0)
			{
				Enabled = false;
				return;
			}
			if (cfid == 1)
			{
				// What if it's the dummy built-in graphite feature that we ignore?
				// Get the list of features (only 1).
				using (ArrayPtr idsM = MarshalEx.ArrayToNative<int>(cfid))
				{
					m_featureEngine.GetFeatureIDs(cfid, idsM, out cfid);
					int [] ids = MarshalEx.NativeToArray<int>(idsM, cfid);
					if (ids[0] == kGrLangFeature)
					{
						Enabled = false;
						return;
					}
				}
			}
			Enabled = true;
			}
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse a feature string to find the next feature id value, skipping any leading
		/// spaces.  Also skip past the trailing equal sign, and any surrounding spaces.
		/// </summary>
		/// <param name="stFeatures">feature string to part</param>
		/// <param name="ichMin">starting index into the feature string</param>
		/// <param name="ichLim">receives the index of the first character of the feature's
		/// value.</param>
		/// <returns>
		/// Feature id, or -1 if a syntax error occurs.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		static private int ParseNextFid(string stFeatures, int ichMin, out int ichLim)
		{
			int ich;
			// skip any leading spaces.
			for (ich = ichMin; ich < stFeatures.Length && stFeatures[ich] == ' '; ++ich)
				;
			// Check for finding the beginning of a field id (at least one decimal digit).
			if (ich >= stFeatures.Length || stFeatures[ich] < '0' || stFeatures[ich] > '9')
			{
				ichLim = ich;
				return -1;
			}
			// convert any number of decimal digits into the corresponding integer.
			int fid = 0;
			for (;
				ich < stFeatures.Length && stFeatures[ich] >= '0' && stFeatures[ich] <= '9';
				++ich)
			{
				fid = fid * 10 + (stFeatures[ich] - '0');
			}
			// Skip any trailing spaces after the field id.
			for ( ; ich < stFeatures.Length && stFeatures[ich] == ' '; ++ich)
				;
			// Check for the mandatory equal sign.
			if (ich >= stFeatures.Length || stFeatures[ich] != '=')
			{
				ichLim = ich;
				return -1;
			}
			// skip any trailing spaces after the equal sign.
			for (++ich; ich < stFeatures.Length && stFeatures[ich] == ' '; ++ich)
				;
			ichLim = ich;
			return fid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the value, which is a quoted string.  The first four characters are packed
		/// into a 32-bit integer by taking the low byte of each character.
		/// </summary>
		/// <param name="stFeatures">The st features.</param>
		/// <param name="ichMin">The ich min.</param>
		/// <param name="ichLim">Receives the index of the first char past the value.</param>
		/// <returns>
		/// The parsed value, or Int32.MaxValue if an error occurs.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		static private int ParseQuotedValue(string stFeatures, int ichMin, out int ichLim)
		{
			if (stFeatures[ichMin] != '"')
			{
				ichLim = ichMin;
				return Int32.MaxValue;
			}
			byte[] bVals = new byte[4] { 0, 0, 0, 0 };
			int ich;
			int ib;
			for (ib = 0, ich = ichMin + 1; ich < stFeatures.Length; ++ich, ++ib)
			{
				if (stFeatures[ich] == '"')
					break;
				if (ib < 4)
					bVals[ib] = (byte)stFeatures[ich];
			}
			if (ich >= stFeatures.Length)
			{
				ichLim = ich;
				return Int32.MaxValue;
			}
			ichLim = ich + 1;
			return bVals[0] << 24 | bVals[1] << 16 | bVals[2] << 8 | bVals[3];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the value, which is a number (digit string).
		/// </summary>
		/// <param name="stFeatures">The st features.</param>
		/// <param name="ichMin">The ich min.</param>
		/// <param name="ichLim">Receives the index of the first char past the value.</param>
		/// <returns>
		/// The parsed value, or Int32.MaxValue if an error occurs.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		static private int ParseNumericValue(string stFeatures, int ichMin, out int ichLim)
		{
			ichLim = stFeatures.IndexOfAny(new char[] { ',', ' '}, ichMin);
			if (ichLim < 0)
				ichLim = stFeatures.Length;
			try
			{
				return Int32.Parse(stFeatures.Substring(ichMin, ichLim - ichMin));
			}
			catch
			{
				return Int32.MaxValue;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scan for another feature value, which is marked by a comma.
		/// </summary>
		/// <param name="stFeatures">The st features.</param>
		/// <param name="ichMin">The ich min.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static private int FindNextFeature(string stFeatures, int ichMin)
		{
			bool fInQuote = false;
			int ich;
			for (ich = ichMin; ich < stFeatures.Length; ++ich)
			{
				if (stFeatures[ich] == '"')
					fInQuote = !fInQuote;
				else if (stFeatures[ich] == ',' && !fInQuote)
					return ich + 1;
			}
			return ich;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse a feature string from a renderer. The feature string is of the form
		/// 1=12,2=23,3="abcd"
		/// where the first number in each pair is a feature ID, the second is the value.
		/// The quoted form represents a special trick form (which this class never outputs,
		/// but apparently may be found in existing engines), where the low bytes of up to
		/// four characters are combined to make the value.
		///
		/// The routine returns an array the same length as ids[]. By default all values
		/// are the largest possible integer, signifying the default value of the property.
		/// For each id in ids that is matched by an id in the string, we record the
		/// corresponding valuein values.
		/// </summary>
		/// <param name="ids"></param>
		/// <param name="stFeatures"></param>
		/// ------------------------------------------------------------------------------------
		static public int[] ParseFeatureString(int[] ids, string stFeatures)
		{
			int[] result = new int[ids.Length];
			for (int ifeat = 0; ifeat < result.Length; ++ifeat)
				result[ifeat] = Int32.MaxValue;		// Signal "undefined" for each feature value.

			if (stFeatures == null)
				return result;
			for (int ich = 0; ich < stFeatures.Length; )
			{
				// Parse the next feature id.
				int fid = ParseNextFid(stFeatures, ich, out ich);
				if (fid >= 0)
				{
					// Parse the corresponding value.
					int val;
					if (stFeatures[ich] == '"')
						val = ParseQuotedValue(stFeatures, ich, out ich);
					else
						val = ParseNumericValue(stFeatures, ich, out ich);
					if (val != Int32.MaxValue && fid != kGrLangFeature)
					{
						// Everything parsed okay, and it's not the built-in graphite 'lang'
						// feature, so store the value if the feature is in the input array.
						int ifeatFound = -1;
						for (int ifeat = 0; ifeat < ids.Length; ++ifeat)
						{
							if (ids[ifeat] == fid)
							{
								ifeatFound = ifeat;
								break;
							}
						}
						if (ifeatFound > -1)
							result[ifeatFound] = val;
					}
				}
				ich = FindNextFeature(stFeatures, ich);
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the inverse operation of ParseFeatureString.
		/// </summary>
		/// <param name="ids">The ids.</param>
		/// <param name="values">The values.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public string GenerateFeatureString(int[] ids, int[] values)
		{
			Debug.Assert(ids.Length == values.Length);
			string stFeatures = "";
			for (int ifeat = 0; ifeat < ids.Length; ++ifeat)
			{
				int id = ids[ifeat];
				if (id == kGrLangFeature)
					continue;
				if (values[ifeat] == Int32.MaxValue)
					continue;
				if (stFeatures.Length != 0)
					stFeatures = stFeatures + ",";
				stFeatures = stFeatures + ids[ifeat] + "=" + values[ifeat];
			}
			return stFeatures;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class FontFeatureMenuItem : MenuItem
		{
			private int m_featureIndex;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:FontFeatureMenuItem"/> class.
			/// </summary>
			/// <param name="label">The label.</param>
			/// <param name="featureIndex">Index of the feature.</param>
			/// <param name="ffbtn">The FFBTN.</param>
			/// --------------------------------------------------------------------------------
			internal FontFeatureMenuItem(string label, int featureIndex,
				FontFeaturesButton ffbtn) :
				base(label, new System.EventHandler(ffbtn.ItemClickHandler))
			{
				m_featureIndex = featureIndex;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the index of the feature.
			/// </summary>
			/// <value>The index of the feature.</value>
			/// --------------------------------------------------------------------------------
			internal int FeatureIndex
			{
				get { return m_featureIndex; }
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the click event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClick(EventArgs e)
		{
			var menu = components.ContextMenu("ContextMenu");
			int cfid;
			m_featureEngine.GetFeatureIDs(0, null, out cfid);

			// Get the list of features.
			using (ArrayPtr idsM = MarshalEx.ArrayToNative<int>(cfid))
			{
				m_featureEngine.GetFeatureIDs(cfid, idsM, out cfid);
				m_ids = MarshalEx.NativeToArray<int>(idsM, cfid);
			}
			m_values = ParseFeatureString(m_ids, m_fontFeatures);
			Debug.Assert(m_ids.Length == m_values.Length);

			for (int ifeat = 0; ifeat < m_ids.Length; ++ifeat)
			{
				int id = m_ids[ifeat];
				if (id == kGrLangFeature)
					continue; // Don't show Graphite built-in 'lang' feature.
				string label;
				m_featureEngine.GetFeatureLabel(id, kUiCodePage, out label);
				if (label.Length == 0)
				{
					//Create backup default string, ie, "Feature #1".
					label = string.Format(FwCoreDlgControls.kstidFeature, id);
				}
				int cValueIds;
				int nDefault;
				int [] valueIds = new int[0];
				using (ArrayPtr valueIdsM = MarshalEx.ArrayToNative<int>(kMaxValPerFeat))
				{
					m_featureEngine.GetFeatureValues(id, kMaxValPerFeat, valueIdsM,
						out cValueIds, out nDefault);
					valueIds = MarshalEx.NativeToArray<int>(valueIdsM, cValueIds);
				}
				// If we know a value for this feature, use it. Otherwise init to default.
				int featureValue = nDefault;
				if (m_values[ifeat] != Int32.MaxValue)
					featureValue = m_values[ifeat];

				// Decide whether to just use a check mark, or have a submenu. Default is sub.
				bool fBinary = false;
				if (cValueIds == 2 &&
					(valueIds[0] == 0 || valueIds[1] == 0) &&
					valueIds[0] + valueIds[1] == 1)
				{
					// Minimum requirement is that there are two states and the values have
					// ids of 0 and 1. We further require that the actual values belong to a
					// natural boolean set.
					string valueLabelT; // Label corresponding to 'true' etc, the checked value
					m_featureEngine.GetFeatureValueLabel(id, 1, kUiCodePage, out valueLabelT);
					string valueLabelF; // Label corresponding to 'false' etc, the unchecked val.
					m_featureEngine.GetFeatureValueLabel(id, 0, kUiCodePage, out valueLabelF);

					// Enhance: these should be based on a resource, or something that depends
					// on the code page, if the code page is ever not constant.
					switch (valueLabelT.ToLowerInvariant())
					{
						case "true":
						case "yes":
						case "on":
						case "":
						{
							switch (valueLabelF.ToLowerInvariant())
							{
								case "false":
								case "no":
								case "off":
								case "":
									fBinary = true;
									break;
							}
						}
							break;
					}
				}
				if (fBinary)
				{
					FontFeatureMenuItem item = new FontFeatureMenuItem(label, ifeat, this);
					item.Checked = featureValue == 1;
					menu.MenuItems.Add(item);
				}
				else if (cValueIds > 0)
				{
					FontFeatureMenuItem menuSub = new FontFeatureMenuItem(label, ifeat, this);
					for (int ival = 0; ival < valueIds.Length; ++ival)
					{
						string valueLabel;
						m_featureEngine.GetFeatureValueLabel(id, valueIds[ival],
							kUiCodePage, out valueLabel);
						if (valueLabel.Length == 0)
						{
							// Create backup default string.
							valueLabel = string.Format(FwCoreDlgControls.kstidFeatureValue,
								valueIds[ival]);
						}
						FontFeatureMenuItem itemSub =
							new FontFeatureMenuItem(valueLabel, valueIds[ival], this);
						itemSub.Checked = valueIds[ival] == featureValue;
						menuSub.MenuItems.Add(itemSub);
					}
					menu.MenuItems.Add(menuSub);
				}
				//				if (fBinary)
				//				{
				//					...
				//					Assert(vnMenuMap.Size() == cItems);
				//					vnMenuMap.Push((ifeat << 16) | 0x0000FFFF);
				//					cItems++;
				//				}
				//				else if (cn > 0)
				//				{
				//					Assert(cn < 0x0000FFFF);
				//					HMENU hmenuSub = ::CreatePopupMenu();
				//					::AppendMenu(hmenu, MF_POPUP, (UINT_PTR)hmenuSub, strFeat.Chars());
				//					for (int in = 0; in < cn; in++)
				//					{
				//
				//						Assert(vnMenuMap.Size() == cItems);
				//						vnMenuMap.Push((ifeat << 16) | in);
				//						cItems++;
				//					}
				//				}
				//				else
				//				}
			}
			menu.Show(this, new Point(0, Height));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Items the click handler.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void ItemClickHandler(Object sender, System.EventArgs e)
		{
			FontFeatureMenuItem item = sender as FontFeatureMenuItem;
			FontFeatureMenuItem parent = item.Parent as FontFeatureMenuItem;
			if (parent == null)
			{
				// top-level (checked) item
				item.Checked = ! item.Checked;
				m_values[item.FeatureIndex] = item.Checked ? 1 : 0;
			}
			else
			{
				// The 'feature index' of the subitem is actually the id of the appropriate one
				// of the possible values.
				m_values[parent.FeatureIndex] = item.FeatureIndex;
				// Check only the subitem that is the one clicked.
				foreach (FontFeatureMenuItem subitem in parent.MenuItems)
				{
					subitem.Checked = (subitem == item);
				}
			}
			m_fontFeatures = GenerateFeatureString(m_ids, m_values);
			OnFontFeatureSelected(new EventArgs());
		}
	}
}
