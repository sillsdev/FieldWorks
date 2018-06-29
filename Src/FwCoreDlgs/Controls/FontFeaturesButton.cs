// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// Font Features button
	/// </summary>
	public class FontFeaturesButton : Button
	{
		#region Member variables and constants
		/// <summary />
		public const int kGrLangFeature = 1; // See FmtFntDlg.h for real defn.
		/// <summary />
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
		private IRenderingFeatures m_featureEngine;
		private int[] m_values;	// The actual list of values we're editing.
		private int[] m_ids;		// The corresponding ids.
		#endregion

		#region Constructor and dispose stuff
		/// <summary>
		/// Initializes a new instance of the <see cref="FontFeaturesButton"/> class.
		/// </summary>
		public FontFeaturesButton()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			Image = Resources.ResourceHelper.ButtonMenuArrowIcon;
			ImageAlign = ContentAlignment.MiddleRight;
			Text = Strings.kstidFontFeatures;
			Enabled = false;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Class HoldDummyGraphics

		/// <summary />
		private sealed class HoldDummyGraphics: IDisposable
		{
			/// <summary />
			public IVwGraphics m_vwGraphics;
			/// <summary />
			public Graphics m_graphics;
			/// <summary />
			private IntPtr m_hdc;

			/// <summary>
			/// Initializes a new instance of the <see cref="T:HoldDummyGraphics"/> class.
			/// </summary>
			/// <param name="fontName">Name of the font.</param>
			/// <param name="fBold">if set to <c>true</c> [f bold].</param>
			/// <param name="fItalic">if set to <c>true</c> [f italic].</param>
			/// <param name="ctrl">The parent control</param>
			public HoldDummyGraphics(string fontName, bool fBold, bool fItalic, Control ctrl)
			{
				// Make a VwGraphics and initialize it.
				IVwGraphicsWin32 vwGraphics32 = VwGraphicsWin32Class.Create();
				m_vwGraphics = vwGraphics32;
				m_graphics = ctrl.CreateGraphics();
				m_hdc = m_graphics.GetHdc();
				((IVwGraphicsWin32)m_vwGraphics).Initialize(m_hdc);

				// Select our font into it.
				var chrp = new LgCharRenderProps { szFaceName = new ushort[32] };
				for (var ich = 0; ich < fontName.Length; ++ich)
				{
					if (ich < 32)
					{
						chrp.szFaceName[ich] = fontName[ich];
				}
				}

				if (fontName.Length < 32)
				{
					chrp.szFaceName[fontName.Length] = 0;
				}
				else
				{
					chrp.szFaceName[31] = 0;
				}
				chrp.ttvBold = (int)(fBold ? FwTextToggleVal.kttvForceOn : FwTextToggleVal.kttvOff);
				chrp.ttvItalic = (int)(fItalic ? FwTextToggleVal.kttvForceOn : FwTextToggleVal.kttvOff);
				m_vwGraphics.SetupGraphics(ref chrp);
			}

			#region Disposable stuff

			/// <summary/>
			~HoldDummyGraphics()
			{
				Dispose(false);
			}

			/// <summary/>
			private bool IsDisposed
			{
				get;
				set;
			}

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool fDisposing)
			{
				Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					m_vwGraphics?.ReleaseDC();
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

		/// <summary>
		/// Gets or sets the writing system factory.
		/// </summary>
		public ILgWritingSystemFactory WritingSystemFactory { get; set; }

		/// <summary>Event that occurs when the user chooses a font feature.</summary>
		public event EventHandler FontFeatureSelected;

		/// <summary>
		/// Handle the FontFeatureSelected event; by default just calls delegates.
		/// </summary>
		protected virtual void OnFontFeatureSelected(EventArgs ea)
		{
			FontFeatureSelected?.Invoke(this, ea);
		}

		/// <summary>
		/// Gets or sets the font for which we are selecting features.
		/// </summary>
		public string FontName
		{
			get
			{
				return m_fontName;
			}
			set
			{
				if (m_fontName == value)
				{
					return;
				}
				m_fontName = value;
				SetupFontFeatures();
			}
		}

		/// <summary>
		/// Get/Set the actual feature string we are editing.
		/// </summary>
		public string FontFeatures { get; set; }

		/// <summary>
		/// Gets a value indicating whether the currently selected font is a Graphite font.
		/// </summary>
		public bool IsGraphiteFont { get; private set; }

		/// <summary>
		/// Setups the font features.
		/// </summary>
		public void SetupFontFeatures()
		{
			if (string.IsNullOrEmpty(m_fontName))
			{
				Enabled = false;
				IsGraphiteFont = false;
				return;
			}

			using (var hdg = new HoldDummyGraphics(m_fontName, false, false, this))
			{
				IRenderEngine renderer = GraphiteEngineClass.Create();
				renderer.InitRenderer(hdg.m_vwGraphics, FontFeatures);
				// check if the font is a valid Graphite font
				if (!renderer.FontIsValid)
				{
					IsGraphiteFont = false;
					Enabled = false;
					return;
				}
				renderer.WritingSystemFactory = WritingSystemFactory;
				IsGraphiteFont = true;
				m_featureEngine = renderer as IRenderingFeatures;
				if (m_featureEngine == null)
				{
					Enabled = false;
					return;
				}
				int cfid;
				m_featureEngine.GetFeatureIDs(0, null, out cfid);
				switch (cfid)
				{
					case 0:
					Enabled = false;
					return;
					case 1:
					// What if it's the dummy built-in graphite feature that we ignore?
					// Get the list of features (only 1).
						using (var idsM = MarshalEx.ArrayToNative<int>(cfid))
					{
						m_featureEngine.GetFeatureIDs(cfid, idsM, out cfid);
							var ids = MarshalEx.NativeToArray<int>(idsM, cfid);
						if (ids[0] == kGrLangFeature)
						{
							Enabled = false;
							return;
						}
					}

						break;
				}

				Enabled = true;
			}
		}

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
		private static int ParseNextFid(string stFeatures, int ichMin, out int ichLim)
		{
			int ich;
			// skip any leading spaces.
			for (ich = ichMin; ich < stFeatures.Length && stFeatures[ich] == ' '; ++ich)
			{
			}
			// Check for finding the beginning of a field id (at least one decimal digit).
			if (ich >= stFeatures.Length || stFeatures[ich] < '0' || stFeatures[ich] > '9')
			{
				ichLim = ich;
				return -1;
			}
			// convert any number of decimal digits into the corresponding integer.
			var fid = 0;
			for (; ich < stFeatures.Length && stFeatures[ich] >= '0' && stFeatures[ich] <= '9'; ++ich)
			{
				fid = fid * 10 + (stFeatures[ich] - '0');
			}
			// Skip any trailing spaces after the field id.
			for ( ; ich < stFeatures.Length && stFeatures[ich] == ' '; ++ich)
			{
			}
			// Check for the mandatory equal sign.
			if (ich >= stFeatures.Length || stFeatures[ich] != '=')
			{
				ichLim = ich;
				return -1;
			}
			// skip any trailing spaces after the equal sign.
			for (++ich; ich < stFeatures.Length && stFeatures[ich] == ' '; ++ich)
			{
			}
			ichLim = ich;
			return fid;
		}

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
		static private int ParseQuotedValue(string stFeatures, int ichMin, out int ichLim)
		{
			if (stFeatures[ichMin] != '"')
			{
				ichLim = ichMin;
				return int.MaxValue;
			}
			var bVals = new byte[] { 0, 0, 0, 0 };
			int ich;
			int ib;
			for (ib = 0, ich = ichMin + 1; ich < stFeatures.Length; ++ich, ++ib)
			{
				if (stFeatures[ich] == '"')
				{
					break;
				}

				if (ib < 4)
				{
					bVals[ib] = (byte)stFeatures[ich];
			}
			}
			if (ich >= stFeatures.Length)
			{
				ichLim = ich;
				return int.MaxValue;
			}
			ichLim = ich + 1;
			return bVals[0] << 24 | bVals[1] << 16 | bVals[2] << 8 | bVals[3];
		}

		/// <summary>
		/// Parse the value, which is a number (digit string).
		/// </summary>
		/// <param name="stFeatures">The st features.</param>
		/// <param name="ichMin">The ich min.</param>
		/// <param name="ichLim">Receives the index of the first char past the value.</param>
		/// <returns>
		/// The parsed value, or Int32.MaxValue if an error occurs.
		/// </returns>
		private static int ParseNumericValue(string stFeatures, int ichMin, out int ichLim)
		{
			ichLim = stFeatures.IndexOfAny(new[] { ',', ' '}, ichMin);
			if (ichLim < 0)
			{
				ichLim = stFeatures.Length;
			}
			try
			{
				return int.Parse(stFeatures.Substring(ichMin, ichLim - ichMin));
			}
			catch
			{
				return int.MaxValue;
			}
		}

		/// <summary>
		/// Scan for another feature value, which is marked by a comma.
		/// </summary>
		private static int FindNextFeature(string stFeatures, int ichMin)
		{
			var fInQuote = false;
			int ich;
			for (ich = ichMin; ich < stFeatures.Length; ++ich)
			{
				if (stFeatures[ich] == '"')
				{
					fInQuote = !fInQuote;
				}
				else if (stFeatures[ich] == ',' && !fInQuote)
				{
					return ich + 1;
			}
			}
			return ich;
		}

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
		public static int[] ParseFeatureString(int[] ids, string stFeatures)
		{
			var result = new int[ids.Length];
			for (var ifeat = 0; ifeat < result.Length; ++ifeat)
			{
				result[ifeat] = int.MaxValue; // Signal "undefined" for each feature value.
			}

			if (stFeatures == null)
			{
				return result;
			}
			for (var ich = 0; ich < stFeatures.Length; )
			{
				// Parse the next feature id.
				var fid = ParseNextFid(stFeatures, ich, out ich);
				if (fid >= 0)
				{
					// Parse the corresponding value.
					var val = stFeatures[ich] == '"' ? ParseQuotedValue(stFeatures, ich, out ich) : ParseNumericValue(stFeatures, ich, out ich);
					if (val != int.MaxValue && fid != kGrLangFeature)
					{
						// Everything parsed okay, and it's not the built-in graphite 'lang'
						// feature, so store the value if the feature is in the input array.
						var ifeatFound = -1;
						for (var ifeat = 0; ifeat < ids.Length; ++ifeat)
						{
							if (ids[ifeat] == fid)
							{
								ifeatFound = ifeat;
								break;
							}
						}

						if (ifeatFound > -1)
						{
							result[ifeatFound] = val;
					}
				}
				}
				ich = FindNextFeature(stFeatures, ich);
			}
			return result;
		}

		/// <summary>
		/// This is the inverse operation of ParseFeatureString.
		/// </summary>
		private static string GenerateFeatureString(int[] ids, int[] values)
		{
			Debug.Assert(ids.Length == values.Length);
			var stFeatures = string.Empty;
			for (var ifeat = 0; ifeat < ids.Length; ++ifeat)
			{
				var id = ids[ifeat];
				if (id == kGrLangFeature)
				{
					continue;
				}

				if (values[ifeat] == int.MaxValue)
				{
					continue;
				}

				if (stFeatures.Length != 0)
				{
					stFeatures = stFeatures + ",";
				}
				stFeatures = stFeatures + ConvertFontFeatureIdToCode(ids[ifeat]) + "=" + values[ifeat];
			}
			return stFeatures;
		}

		static private string ConvertFontFeatureIdToCode(int fontFeatureId)
		{
			byte[] bytes = BitConverter.GetBytes(fontFeatureId);
			string result = String.Empty;
			foreach (int value in bytes.Reverse())
			{
				result += Convert.ToChar(value);
			}
			return result;
		}

		static private int ConvertFontFeatureCodeToId(string fontFeature)
		{
			fontFeature = new string(fontFeature.ToCharArray().Reverse().ToArray());
			byte[] numbers = fontFeature.Select(x => Convert.ToByte(x)).ToArray();
			int fontFeatureId = BitConverter.ToInt32(numbers, 0);
			return fontFeatureId;
		}

		static private string ConvertFontFeatureCodesToIds(string features)
		{
			// If the feature is empty or has already been converted just return
			if (features.Length < 1 || !Char.IsLetter(features[0]))
				return features;
			var feature = features.Split(',');
			foreach (var value in feature)
			{
				var keyValuePair = value.Split('=');
				var key = ConvertFontFeatureCodeToId(keyValuePair[0]);
				features = features.Replace(keyValuePair[0], key.ToString());
			}
			return features;
		}

		/// <summary />
		private sealed class FontFeatureMenuItem : MenuItem
		{
			/// <summary>
			/// Initializes a new instance of the class.
			/// </summary>
			internal FontFeatureMenuItem(string label, int featureIndex, FontFeaturesButton ffbtn)
				: base(label, ffbtn.ItemClickHandler)
			{
				FeatureIndex = featureIndex;
			}

			/// <summary />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
				base.Dispose(disposing);
			}

			/// <summary>
			/// Gets the index of the feature.
			/// </summary>
			internal int FeatureIndex { get; }
		}

		/// <summary>
		/// Raises the click event.
		/// </summary>
		protected override void OnClick(EventArgs e)
		{
			var menu = components.ContextMenu("ContextMenu");
			int cfid;
			m_featureEngine.GetFeatureIDs(0, null, out cfid);

			// Get the list of features.
			using (var idsM = MarshalEx.ArrayToNative<int>(cfid))
			{
				m_featureEngine.GetFeatureIDs(cfid, idsM, out cfid);
				m_ids = MarshalEx.NativeToArray<int>(idsM, cfid);
			}
			FontFeatures = ConvertFontFeatureCodesToIds(FontFeatures);
			m_values = ParseFeatureString(m_ids, FontFeatures);
			Debug.Assert(m_ids.Length == m_values.Length);

			for (var ifeat = 0; ifeat < m_ids.Length; ++ifeat)
			{
				var id = m_ids[ifeat];
				if (id == kGrLangFeature)
				{
					continue; // Don't show Graphite built-in 'lang' feature.
				}
				string label;
				m_featureEngine.GetFeatureLabel(id, kUiCodePage, out label);
				if (label.Length == 0)
				{
					//Create backup default string, ie, "Feature #1".
					label = string.Format(Strings.kstidFeature, id);
				}
				int cValueIds;
				int nDefault;
				int [] valueIds;
				using (var valueIdsM = MarshalEx.ArrayToNative<int>(kMaxValPerFeat))
				{
					m_featureEngine.GetFeatureValues(id, kMaxValPerFeat, valueIdsM, out cValueIds, out nDefault);
					valueIds = MarshalEx.NativeToArray<int>(valueIdsM, cValueIds);
				}
				// If we know a value for this feature, use it. Otherwise init to default.
				var featureValue = nDefault;
				if (m_values[ifeat] != Int32.MaxValue)
				{
					featureValue = m_values[ifeat];
				}

				// Decide whether to just use a check mark, or have a submenu. Default is sub.
				var fBinary = false;
				if (cValueIds == 2 && (valueIds[0] == 0 || valueIds[1] == 0) && valueIds[0] + valueIds[1] == 1)
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
					var item = new FontFeatureMenuItem(label, ifeat, this) { Checked = featureValue == 1 };
					menu.MenuItems.Add(item);
				}
				else if (cValueIds > 0)
				{
					var menuSub = new FontFeatureMenuItem(label, ifeat, this);
					foreach (var valueId in valueIds)
					{
						string valueLabel;
						m_featureEngine.GetFeatureValueLabel(id, valueId, kUiCodePage, out valueLabel);
						if (valueLabel.Length == 0)
						{
							// Create backup default string.
							valueLabel = string.Format(Strings.kstidFeatureValue, valueId);
						}
						var itemSub = new FontFeatureMenuItem(valueLabel, valueId, this) { Checked = valueId == featureValue };
						menuSub.MenuItems.Add(itemSub);
					}
					menu.MenuItems.Add(menuSub);
				}
			}
			menu.Show(this, new Point(0, Height));
		}

		/// <summary>
		/// Items the click handler.
		/// </summary>
		private void ItemClickHandler(Object sender, EventArgs e)
		{
			var item = (FontFeatureMenuItem) sender;
			var parent = item.Parent as FontFeatureMenuItem;
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
			FontFeatures = GenerateFeatureString(m_ids, m_values);
			OnFontFeatureSelected(new EventArgs());
		}
	}
}
