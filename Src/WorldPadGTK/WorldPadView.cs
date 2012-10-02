/*
 *    WorldPadView.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.GtkCustomWidget;


// Was SIL.FieldWorks.WorldPad
namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// FwView which is aggregated by WorldPadDocView
	/// </summary>
	public class WorldPadView : SimpleRootSite, IWorldPadView
	{
		public const int ktagProp = 99;
		public const int kfrText = 0;

		private StVc m_vVc;
		/// <value>
		/// The data access cache where document contents are stored. Stores a COMInterfaces.WpDa.
		/// </value>
		private ISilDataAccess m_sda;
		private ILgWritingSystemFactory m_wsf;

		private IWorldPadDocModel docModel;
		private IWorldPadDocController docController;

		private WpStylesheet styleSheet;

		public WpStylesheet StyleSheet
		{
			get
			{
				return styleSheet;
			}

		}

		/// <value>
		/// The data access cache where document contents are stored.
		/// </value>
		public ISilDataAccess DataAccess
		{
			get
			{
				return m_sda;
			}
		}

		public WorldPadView()
		{
			this.ShowAll();
		}

		protected void Dispose()
		{
			base.Dispose();
			m_wsf.Shutdown(); // Not normally in View Dispose, but after closing ALL views.
			GC.SuppressFinalize(this);
		}

		public void SetDocumentCallbacks(IWorldPadDocModel model, IWorldPadDocController controler)
		{
			docModel = model;
			docController = controler;

		}

		public void DumpAllFonts() // Granada
		{
			ILgFontManager qfm = new LgFontManager();
			string s = "";
			qfm.AvailableFonts(out s);
			Console.WriteLine("Font List:");
			Console.WriteLine(s);
		}


		// Virtual Method in simpleRootSite that is called when ever there is a possiblity that the IP has moved
		// this changing the text selection.
		public override void CheckSelection()
		{
			if (GetRootBox() == null)
				return;

			if (GetRootBox().Selection == null)
				return;

			// Start test implementation

			string style;
			if (PropertiesHelper.GetSingleProperty(GetRootBox().Selection, FwTextPropType.ktptNamedStyle, out style))
			{
				docModel.SetStyle(style);

				// We have a style, set out combos to reflect the style. This may get overriten later on in this method.

				// TODO Review, maybe, if use a GetSingleProperty that querys
				// both the TsTextProperties and the VwPropertyStore
				// then this code would be uneccessary.
				ITsTextProps ttp;
				ttp = StyleSheet.GetStyleRgch(0, style);
				if (ttp != null)
				{
					// Temp variable used Int type from GetIntPropValues
					int type;

					string fontFamily = ttp.GetStrPropValue((int)FwTextPropType.ktptFontFamily);
					if (fontFamily != null)
						docModel.SetFontFamily(fontFamily);

					int fontSize = ttp.GetIntPropValues((int)FwTextPropType.ktptFontSize, out type);
					if (fontSize != -1)
						docModel.SetFontSize((fontSize / 1000).ToString());

					int bold = ttp.GetIntPropValues((int)FwTextPropType.ktptBold, out type);

					switch (bold)
					{
						case -1:
							docModel.SetBold(ThreeState.Indeterminate);
							break;
						case (int)FwTextToggleVal.kttvForceOn:
							docModel.SetBold(ThreeState.True);
							break;
						default:
							docModel.SetBold(ThreeState.False);
							break;
					}

					int italic = ttp.GetIntPropValues((int)FwTextPropType.ktptItalic, out type);

					switch (italic)
					{
						case -1:
							docModel.SetItalic(ThreeState.Indeterminate);
							break;
						case (int)FwTextToggleVal.kttvForceOn:
							docModel.SetItalic(ThreeState.True);
							break;
						default:
							docModel.SetItalic(ThreeState.False);
							break;
					}

					int align = ttp.GetIntPropValues((int)FwTextPropType.ktptAlign, out type);
					Console.WriteLine("Align Style Align = {0}", (FwTextAlign)align);
					docModel.SetAlign((FwTextAlign)align);

				}
				else
				{
#if false
					docModel.SetStyle(String.Empty);
#else
					// This is currently a hack for the CTC demo
					docModel.SetStyle("Normal");
#endif
					docModel.SetFontFamily(String.Empty);
					docModel.SetFontSize(String.Empty);
					docModel.SetBold(ThreeState.Indeterminate);
					docModel.SetItalic(ThreeState.Indeterminate);
				}
			}
			else
			{
					// More then one prop exists
					docModel.SetStyle(String.Empty);
			}

			int intProptype;
			int ws;
			if (PropertiesHelper.GetSingleProperty(GetRootBox().Selection, FwTextPropType.ktptWs, out ws, out intProptype))
			{
				if (ws != -1)
				{
					ILgWritingSystemFactory wsf = new LgWritingSystemFactory();
					string wsAbrStr = wsf.GetStrFromWs(ws);

					// Lookup Ws from Abr
					string wsStr = String.Empty;
					wsStr = (string)docModel.WritingSystems[wsAbrStr];
					docModel.SetWritingSystem(wsStr);
				}
				else
				{
					docModel.SetWritingSystem(String.Empty);
				}
			}
			else
			{
				// More then one prop exists
				docModel.SetWritingSystem(String.Empty);
			}

			string family;
			if (PropertiesHelper.GetSingleProperty(GetRootBox().Selection, FwTextPropType.ktptFontFamily, out family))
			{

				if (family != null)
					docModel.SetFontFamily(family);
				else
					docModel.SetFontFamily(String.Empty);
			}
			else
			{
				// More then one prop exists
				docModel.SetFontFamily(String.Empty);
			}

			int fontsize;
			if (PropertiesHelper.GetSingleProperty(GetRootBox().Selection, FwTextPropType.ktptFontSize, out fontsize, out intProptype))
			{
				// TODO Review we are currently assuming the FontSize is in millipoints hence the / 1000 - GetSignleProperty should return us a unit type when it is used in its integer form...
				if (fontsize != -1)
					docModel.SetFontSize((fontsize / 1000).ToString());
				else
					docModel.SetFontSize(String.Empty);
			}
			else
			{
				// More then one prop exists
				docModel.SetFontSize(String.Empty);
			}

			int toggleVal;
			if (PropertiesHelper.GetSingleProperty(GetRootBox().Selection, FwTextPropType.ktptBold, out toggleVal, out intProptype))
			{
				switch (toggleVal)
				{

					case -1:
						break; // inherit style's value
					case (int)FwTextToggleVal.kttvForceOn:
						docModel.SetBold(ThreeState.True);
						break;
					default:
						docModel.SetBold(ThreeState.False);
						break;
				}
			}
			else
			{
				// More then one prop exists
				docModel.SetBold(ThreeState.Indeterminate);
			}

			if (PropertiesHelper.GetSingleProperty(GetRootBox().Selection, FwTextPropType.ktptItalic, out toggleVal, out intProptype))
			{
				// TODO Review we are currently assuming the FontSize is in millipoints hence the / 1000 - GetSignleProperty should return us a unit type when it is used in its integer form...
				switch (toggleVal)
				{

					case -1:
						break; // inherit style's value
					case (int)FwTextToggleVal.kttvForceOn:
						docModel.SetItalic(ThreeState.True);
						break;
					default:
						docModel.SetItalic(ThreeState.False);
						break;
				}
			}
			else
			{
				// More then one prop exists
				docModel.SetItalic(ThreeState.Indeterminate);
			}

			// Alignment are not Text Properties so use GetSingleNonTextProperty instread of GetSingleProperty
			if (PropertiesHelper.GetSingleNonTextProperty(GetRootBox().Selection, FwTextPropType.ktptAlign, out toggleVal, out intProptype))
			{
				if (toggleVal != -1)
				{
					Console.WriteLine("Align - Read Property = {0}", toggleVal);
					docModel.SetAlign((FwTextAlign)toggleVal);
				}
			}
			else
			{
				// More then one prop exists
				docModel.SetAlign((FwTextAlign)0);
			}

			docModel.ActionPerformed();
		}


		public IVwRootBox GetRootBox()
		{
			return m_rootb;
		}

		/// <summary>Create and initialize the rootbox</summary>
		public override void MakeRoot()
		{
			int rootHvo = 1;

			base.MakeRoot();
			m_rootb = (IVwRootBox) new VwRootBox();
			m_rootb.SetSite(this.CastAsIVwRootSite());


			// Setup the data access cache
			m_sda = (ISilDataAccess) new WpDa();
			m_wsf = (ILgWritingSystemFactory) new LgWritingSystemFactory();
			m_sda.WritingSystemFactory = m_wsf;

			// Setup the Style sheet.
			styleSheet = new WpStylesheet();
			styleSheet.Init(m_sda);


			m_vVc = new StVc("Normal", m_wsf.UserWs);
			// m_vVc.Lazy = true;
			m_vVc.Cache = styleSheet.Cache;

			m_rootb.DataAccess = (ISilDataAccess) m_sda;
			m_rootb.SetRootObject(rootHvo, m_vVc, kfrText,  styleSheet);

			/// Attempt 1 as setting font info.
			TsPropsBldr qtpb = new TsPropsBldr();

			qtpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, Global.g_pszwStyleNormal);

			m_fRootboxMade = true;
			m_dxdLayoutWidth = -50000; // Don't try to draw until we get OnSize and do layout

			DumpAllFonts();
		}
	}
}
