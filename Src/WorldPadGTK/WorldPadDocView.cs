/*
 *    WorldPadDocView.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Gtk;
using Glade;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FDO.Cellar;

// For FwTextAlign type
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.WorldPad
{

	public class WorldPadDocView : IWorldPadDocView, IWindow
	{
		public const string MAIN_GLADE_FILE = "glade/main.glade";
		public const string FILTER = "Text files (*.txt)|*.txt|WorldPad XML (*.wpx)|*.wpx|All Files (*.*)|*.*";
		public Gtk.Window Window
		{
			get { return window1; }
		}

		[Widget]
		private Gtk.Window window1;
		[Widget]
		private Gtk.Notebook notebook1;
		[Widget]
		private Gtk.HBox unsplitHBox;
		[Widget]
		private Gtk.HBox upperHBox;
		[Widget]
		private Gtk.HBox lowerHBox;
		[Widget]
		private Gtk.ComboBox comboboxStyle;  // Style
		[Widget]
		private Gtk.ComboBox comboboxWS;  // Writing System
		[Widget]
		private Gtk.ComboBox comboboxFamily;  // Font Family
		[Widget]
		private Gtk.ComboBox comboboxSize;  // Font Size
		[Widget]
		private Gtk.ToggleToolButton toggletoolbuttonBold;  // Bold
		[Widget]
		private Gtk.ToggleToolButton toggletoolbuttonItalic;  // Italic
		[Widget]
		private Gtk.ToggleToolButton toggletoolbuttonLeft;  // Left
		[Widget]
		private Gtk.ToggleToolButton toggletoolbuttonCenter;  // Center
		[Widget]
		private Gtk.ToggleToolButton toggletoolbuttonRight;  // Right

		private IWorldPadDocModel docModel;
		private IWorldPadDocController docController;
		private WorldPadView upperView;
		private WorldPadView lowerView;
		private Gtk.ListStore styles;
		private Gtk.ListStore writingSystems;
		private Gtk.ListStore families;
		private Gtk.ListStore sizes;

		private bool modelChange;

		public WorldPadDocView(IWorldPadDocController docController, IWorldPadDocModel docModel)
		{
			Console.WriteLine("WorldPadDocView.ctor invoked");

			this.docModel = docModel;
			this.docController = docController;

			// register observer (view) with subject (model)
			docModel.Subscribe(new DocModelChangedEventHandler(on_modelchanged_event));

			/*Catalog.Init("csdialogdemo", "./locale");*/

			/*// Note: "using" will automatically release resources
			using (FileStream stream = new FileStream("glade/wpmain.glade", FileMode.Open))
			{
				// Note: use of "stream" constructor
				Glade.XML gxml = new Glade.XML(stream, "window1", null);
				gxml.Autoconnect(this);
			}*/
			// Note: The icon file must be in the same directory as the .glade file
			Glade.XML gxml =
				new Glade.XML(SIL.FieldWorks.Common.Utils.Utils.GetProgramDirectory() + MAIN_GLADE_FILE, "window1", null);
			gxml.Autoconnect(this);

			// Initialize the Styles ComboBox
			//ListStore styles = new ListStore(typeof(string));
			styles = new ListStore(typeof(string));
			comboboxStyle.Model = styles;
			TreeIter iter = new TreeIter();
			if (docModel.Styles != null)  // null when default window is presented
			{
				IDictionaryEnumerator enumerator = docModel.Styles.GetEnumerator();
				while (enumerator.MoveNext())
				{
					//Console.WriteLine("Processing style: {0}", enumerator.Key);

					iter = styles.AppendValues((string)enumerator.Key);
				}
			}
			// Set initially selected style to first in list
			styles.GetIterFirst(out iter);

			/*comboboxStyle.Model = (Gtk.ListStore)docModel.Styles;*/

			// Initialize the Writing Systems ComboBox
			writingSystems = new ListStore(typeof(string));
			comboboxWS.Model = writingSystems;
			TreeIter wsIter = new TreeIter();
			if (docModel.WritingSystems != null)  // null when default window is presented
			{
				IDictionaryEnumerator enumerator = docModel.WritingSystems.GetEnumerator();
				while (enumerator.MoveNext())
				{
					//Console.WriteLine("Writing System: {0}", enumerator.Key);

					wsIter = writingSystems.AppendValues((string)enumerator.Value);
				}
			}
			// Set initially selected WS to first in list
			writingSystems.GetIterFirst(out wsIter);

			// Initialize the Font Family ComboBox
			CreateFontList();
			// Set initially selected family to first in list
			TreeIter familiesIter = new TreeIter();
			families.GetIterFirst(out familiesIter);

			// Initialize the Font Size ComboBox
			sizes = new ListStore(typeof(string));
			comboboxSize.Model = sizes;
			TreeIter sizesIter = new TreeIter();
			string allSizes =
				"8 9 10 11 12 13 14 16 18 20 22 24 26 28 32 36 40 48 56 64 72";
			foreach (string size in allSizes.Split(new char[]{' '}))
			{
				sizesIter = sizes.AppendValues(size);
			}
			// Set initially selected size to first in list
			sizes.GetIterFirst(out sizesIter);

			/*window1.Icon = new Gdk.Pixbuf("WorldPad.ico");*/

			window1.Title = docModel.DocName;

			Console.WriteLine("File is default?: {0}", docModel.IsDefault);
			/*Console.WriteLine("File is empty?: {0}", docModel.IsEmpty);*/

			IWorldPadPaneView paneView = docController.UpperPane;
			upperView = (WorldPadView)paneView.View;
			upperView.Reparent(unsplitHBox);
			notebook1.Page = 0;
			//initializing = false;

			docModel.MainWnd = new WpgtkMainWnd();
			docModel.MainWnd.Stylesheet = upperView.StyleSheet;
		}

		public void SplitPane()
		{
			Console.WriteLine("WorldPadDocView.SplitPane() invoked");

			upperView.Reparent(upperHBox);

			IWorldPadPaneView paneView = docController.LowerPane;

			lowerView = (WorldPadView)paneView./*WorldPad*/View;
			lowerView.Reparent(lowerHBox);

			notebook1.Page = 1;
		}

		public void ClosePane()
		{
			Console.WriteLine("WorldPadDocView.ClosePane() invoked");

			lowerHBox.Remove(lowerView);
			lowerView = null;
			upperView.Reparent(unsplitHBox);

			notebook1.Page = 0;
		}

		/// <summary>
		/// Load a file into fwviews from a WPX or WPT file. This method only meaningfully works
		/// once per instance of a document.
		/// </summary>
		/// <param name="path">Input WPX or WPT file.</param>
		/// <throws>IOException if path is null or empty</throws>
		public void LoadFromXml(string path)
		{
			Console.WriteLine("DEBUG: WorldPadDocView.LoadFromXml: path="+path);
			if (null == path || String.Empty == path)
				throw new IOException("In WorldPadDocView.LoadFromXml(path), path is empty or null.");
			WpgtkMainWnd mainWnd = docModel.MainWnd;
			upperView.VwGraphicsGTK.BeginDraw();
			int parsingResult = ((IWpDa)upperView.DataAccess).LoadXml(path, mainWnd, mainWnd); // TODO pass mainWnd for both, or what?
//			((IWpDa)upperView.DataAccess).PropChanged(rootBox, (int)PropChangeType.kpctNotifyMeThenAll, hvoText, tagText, ihvoFirstMod, chvoChanged, chvoChanged);
			upperView.VwGraphicsGTK.EndDraw();
			Console.WriteLine("DEBUG: WorldPadDocView.LoadFromXml: parsingResult="+parsingResult);
		}

		/// <summary>Save a file to WPX</summary>
		/// <param name="path">output file path</param>
		/// <throws>IOException if path is null or empty</throws>
		public void SaveToXml(string path)
		{
			Console.WriteLine("DEBUG: WorldPadDocView.SaveToXml: path="+path);
			if (null == path || String.Empty == path)
				throw new IOException("In WorldPadDocView.SaveToXml(path), path is empty or null.");
			WpgtkMainWnd mainWnd = docModel.MainWnd;
			upperView.VwGraphicsGTK.BeginDraw();
			((IWpDa)upperView.DataAccess).SaveXml(path, mainWnd, false); // TODO should fDtd be true or false?
			upperView.VwGraphicsGTK.EndDraw();
		}

		/// <summary>
		/// Reponds to changing of the document model by updating the state of some combo boxes.
		/// TODO REVIEW a lot of this function could be functionalized to reduce the copy & pasting.
		///
		/// </summary>
		public void on_modelchanged_event(object obj, IDocModelChangedEventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_modelchanged_event() invoked");

			modelChange = true;

			this.setAlignToggleState(docModel.JustificationLeft, docModel.JustificationCenter, docModel.JustificationRight);

			Console.WriteLine("Paragraph Style: {0}", docModel.ParagraphStyle);

			TreeIter iter;

			// Set Styles ComboBox
			styles.GetIterFirst(out iter);
			do
			{
				Console.WriteLine("Style: {0}", (string)styles.GetValue(iter, 0));

				if ((string)styles.GetValue(iter, 0) == docModel.ParagraphStyle)
				{
					comboboxStyle.SetActiveIter(iter);
				}
			} while (styles.IterNext(ref iter));

			Console.WriteLine("Writing System: {0}", docModel.SelectionWritingSystem);

			// Set Writing Systems ComboBox
			if (docModel.SelectionWritingSystem == String.Empty)
			{
				TreeIter wsIter;
				writingSystems.GetIterFirst(out wsIter);
				// Insert a blank first ComboBox entry if there isn't one already
				if ((string)writingSystems.GetValue(wsIter, 0) != String.Empty)
				{
					wsIter = writingSystems.Prepend();
					writingSystems.SetValue(wsIter, 0, String.Empty);
				}
			}

			writingSystems.GetIterFirst(out iter);
			do
			{
				Console.WriteLine("Writing System: {0}", (string)writingSystems.GetValue(iter, 0));

				if ((string)writingSystems.GetValue(iter, 0) == docModel.SelectionWritingSystem)
				{
					comboboxWS.SetActiveIter(iter);
				}
			} while (writingSystems.IterNext(ref iter));

			// Set FontButton
			//bool successful = fontbutton1.SetFontName(docModel.FontName);
			//Console.WriteLine("SetFontName succeeded?: {0}", successful);

			// Set Font Family ComboBox
			if (docModel.SelectionFontFamily == String.Empty)  // A 'mixed' selection
			{
				TreeIter familiesIter;
				families.GetIterFirst(out familiesIter);
				// Insert a blank first ComboBox entry if there isn't one already
				if ((string)families.GetValue(familiesIter, 0) != String.Empty)
				{
					familiesIter = families.Prepend();
					families.SetValue(familiesIter, 0, String.Empty);
				}
			}

			families.GetIterFirst(out iter);
			do
			{
				//Console.WriteLine("Family: {0}", (string)families.GetValue(iter, 0));

				if ((string)families.GetValue(iter, 0) == docModel.SelectionFontFamily)
				{
					comboboxFamily.SetActiveIter(iter);
				}
			} while (families.IterNext(ref iter));


			// Set Named style ComboBox
			if (docModel.SelectionStyle == String.Empty)  // A 'mixed' selection
			{
				TreeIter stylesIter;
				styles.GetIterFirst(out stylesIter);
				// Insert a blank first ComboBox entry if there isn't one already
				if ((string)styles.GetValue(stylesIter, 0) != String.Empty)
				{
					stylesIter = styles.Prepend();
					styles.SetValue(stylesIter, 0, String.Empty);
				}
			}

			styles.GetIterFirst(out iter);
			do
			{
				if ((string)styles.GetValue(iter, 0) == docModel.SelectionStyle)
				{
					comboboxStyle.SetActiveIter(iter);
				}
			} while (styles.IterNext(ref iter));

			// Set Font Size ComboBox
			if (docModel.SelectionFontSize == String.Empty)  // A 'mixed' selection
			{
				TreeIter sizesIter;
				sizes.GetIterFirst(out sizesIter);
				// Insert a blank first ComboBox entry if there isn't one already
				if ((string)sizes.GetValue(sizesIter, 0) != String.Empty)
				{
					sizesIter = sizes.Prepend();
					sizes.SetValue(sizesIter, 0, String.Empty);
				}
			}

			sizes.GetIterFirst(out iter);
			do
			{
				if ((string)sizes.GetValue(iter, 0) == docModel.SelectionFontSize)
				{
					comboboxSize.SetActiveIter(iter);
				}
			} while (sizes.IterNext(ref iter));

			this.toggletoolbuttonBold.Active = docModel.SelectionBold == ThreeState.True;
			this.toggletoolbuttonItalic.Active = docModel.SelectionItalic == ThreeState.True;



			modelChange = false;
		}

		public void Init()
		{
			Console.WriteLine("WorldPadDocView.Init() invoked");
		}

		private void CreateFontList()
		{
			Console.WriteLine("WorldPadDocView.CreateFontList() invoked");

			families = new ListStore(typeof(string));
			families.SetSortColumnId(0, SortType.Ascending);

			comboboxFamily.Model = families;

			Pango.Context context = comboboxFamily.CreatePangoContext();

			foreach (Pango.FontFamily family in context.Families)
			{
				families.AppendValues(family.Name);
			}
		}

		// Handle window closing

		private void on_window1_delete_event(object obj, DeleteEventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_window1_delete_event() invoked");

			docController.FileClose();
			//args.RetVal = true;
		}

		// Event handlers for File menu items

		/// <summary>
		/// Event handler for File->New. Opens the default (blank) document in a new window.
		/// </summary>
		private void on_new1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_new1_activate() invoked");

			docController.FileNew();
		}

		/// <summary>
		/// Event handler for File->Open. Opens a specified document in a new window.
		/// </summary>
		private void on_open1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_open1_activate() invoked");

			/*
			FileOpenDlgController opener = (FileOpenDlgController)docController.ShowDialog(DialogFactory.DialogType.FileOpen);
			opener.DocController = docController;
			*/
			OpenFileDialog ofd = new OpenFileDialog();
			// Don't permanently change the generic OpenFileDialog's filter.
			string oldFilter = ofd.Filter;
			ofd.Filter = FILTER;
			ofd.FilterIndex = 2;
			if (ofd.ShowDialog() == DialogResult.OK)
				if (null != ofd.FileName && String.Empty != ofd.FileName)
					docController.FileOpen(ofd.FileName);
			ofd.Filter = oldFilter;
		}

		private void on_close1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_close1_activate() invoked");
			// TODO implement
		}

		/// <summary>
		/// Event handler for File->Save. Saves document to current filename.
		/// </summary>
		private void on_save1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_save1_activate() invoked");
			SaveToXml(docModel.FileName);
		}

		/// <summary>
		/// Event handler for File->Save As. Saves document to a specified filename.
		/// </summary>
		private void on_save_as1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_save_as1_activate() invoked");
			SaveFileDialog sfd = new SaveFileDialog();
			// Don't permanently change the generic SaveFileDialog's filter.
			string oldFilter = sfd.Filter;
			sfd.Filter = FILTER;
			sfd.FilterIndex = 2;
			//FileSaveDlgController saver = (FileSaveDlgController)docController.ShowDialog(DialogFactory.DialogType.FileSave);
			if (sfd.ShowDialog() == DialogResult.OK)
				if (null != sfd.FileName && String.Empty != sfd.FileName)
					SaveToXml(sfd.FileName);
			sfd.Filter = oldFilter;
		}

		private void on_page_setup1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_page_setup1_activate() invoked");

			docController.ShowDialog(DialogFactory.DialogType.PageFormat);
		}

		private void on_print1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_print1_activate() invoked");
			IPrintDialog pd = PrintDialogFactory.GetPrintDialog(PrintDialogFactory.PrintDialogType.Gtk);
			PrintResponse response = pd.Show();
			if (response == PrintResponse.Print)
				pd.Print();
			else if (response == PrintResponse.Preview)
				pd.ShowPreview();
			else
				pd.Close();
		}

		private void on_quit1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_quit1_activate() invoked");

			docController.Quit();
		}

		// Event handlers for Edit menu items

		private void on_undo1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_undo1_activate() invoked");

			if (((VwUndoDa)upperView.DataAccess).GetActionHandler().CanUndo())
			{
				upperView.VwGraphicsGTK.BeginDraw();
				((VwUndoDa)upperView.DataAccess).GetActionHandler().Undo();
				upperView.VwGraphicsGTK.EndDraw();
			}

		}

		private void on_redo1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_redo1_activate() invoked");

			if (((VwUndoDa)upperView.DataAccess).GetActionHandler().CanRedo())
			{
				upperView.VwGraphicsGTK.BeginDraw();
				((VwUndoDa)upperView.DataAccess).GetActionHandler().Redo();
				upperView.VwGraphicsGTK.EndDraw();
			}
		}

		private void on_cut1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_cut1_activate() invoked");
		}

		private void on_copy1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_copy1_activate() invoked");
		}

		private void on_paste1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_paste1_activate() invoked");
		}

		private void on_delete1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_delete1_activate() invoked");
		}

		private void on_select_all1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_select_all1_activate() invoked");
		}

		private void on_find1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_find1_activate() invoked");
		}

		private void on_find_next1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_find_next1_activate() invoked");
		}

		private void on_find_previous1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_find_previous1_activate() invoked");
		}

		private void on_replace1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_replace1_activate() invoked");
			docController.ShowDialog(DialogFactory.DialogType.FindReplace);
		}

		// Event handlers for Format menu items


		/// <summary>
		/// convert colorref to Color
		/// TODO REVIEW Delete all uses of this method and use ConvertColorToBGR
		/// </summary>
		public System.Drawing.Color Convert(int colorRef)
		{
				return ColorUtil.ConvertBGRtoColor((uint)colorRef);
		}

		/// <summary>
		/// convert Color to ColorRef
		/// TODO REVIEW Delete all uses of this method and use ColorUtil.ConvertColorToBGR instead.
		/// </summary>
		public int /*colorRef*/ Convert(System.Drawing.Color c)
		{
			return (int)ColorUtil.ConvertColorToBGR(c);
		}

		private void on_font1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_font1_activate() invoked");

			FontInfo fontInfo = new FontInfo();
			fontInfo.IsDirty = false;

			// Retrieve font name from Views
			if (docModel.SelectionFontFamily != String.Empty)
			{
				fontInfo.m_fontName = new InheritableStyleProp<string>(docModel.SelectionFontFamily);
			}
			else
			{
				fontInfo.m_fontName = new InheritableStyleProp<string>();
				fontInfo.m_fontName.SetDefaultValue(String.Empty);
			}

			// Retrieve font size from Views
			if (docModel.SelectionFontSize != String.Empty)
			{
				fontInfo.m_fontSize = new InheritableStyleProp<int>(
					System.Convert.ToInt32(docModel.SelectionFontSize) * 1000);
			}
			else
			{
				fontInfo.m_fontSize = new InheritableStyleProp<int>();
				fontInfo.m_fontSize.SetDefaultValue(0);
			}


			// Retrieve bold attribute from Views
			if (docModel.SelectionBold != ThreeState.Indeterminate)
			{
				bool bold = docModel.SelectionBold == ThreeState.True;
				fontInfo.m_bold = new InheritableStyleProp<bool>(bold);
			}
			else
			{
				fontInfo.m_bold = new InheritableStyleProp<bool>();
				fontInfo.m_bold.SetDefaultValue(false);
			}

			// Retrieve italic attribute from Views
			if (docModel.SelectionItalic != ThreeState.Indeterminate)
			{
				bool italic = docModel.SelectionItalic == ThreeState.True;
				fontInfo.m_italic = new InheritableStyleProp<bool>(italic);
			}
			else
			{
				fontInfo.m_italic = new InheritableStyleProp<bool>();
				fontInfo.m_italic.SetDefaultValue(false);
			}

			fontInfo.m_superSub = new InheritableStyleProp<FwSuperscriptVal>(FwSuperscriptVal.kssvOff);

			int intProptype;
			int color;
			// Retrieve font color from Views
			if (PropertiesHelper.GetSingleProperty(upperView.GetRootBox().Selection, FwTextPropType.ktptForeColor, out color, out intProptype) && color != -1)
			{
				fontInfo.m_fontColor = new InheritableStyleProp<Color>(Convert(color));
			}
			else
			{
				fontInfo.m_fontColor = new InheritableStyleProp<Color>();
				fontInfo.m_fontColor.SetDefaultValue(System.Drawing.Color.Black);
			}

			// Retrieve background color from Views
			if (PropertiesHelper.GetSingleProperty(upperView.GetRootBox().Selection, FwTextPropType.ktptBackColor, out color, out intProptype) && color != -1)
			{
				fontInfo.m_backColor = new InheritableStyleProp<Color>(Convert(color));
			}
			else
			{
				fontInfo.m_backColor = new InheritableStyleProp<Color>();
				fontInfo.m_backColor.SetDefaultValue(System.Drawing.Color.White);
			}

			// Retrieve underline color from Views
			if (PropertiesHelper.GetSingleProperty(upperView.GetRootBox().Selection, FwTextPropType.ktptUnderColor, out color, out intProptype) && color != -1)
			{
				fontInfo.m_underlineColor = new InheritableStyleProp<Color>(Convert(color));
			}
			else
			{
				fontInfo.m_underlineColor = new InheritableStyleProp<Color>();
				fontInfo.m_underlineColor.SetDefaultValue(System.Drawing.Color.Black);
			}

			fontInfo.m_offset = new InheritableStyleProp<int>(0);
			fontInfo.m_underline = new InheritableStyleProp<FwUnderlineType>(FwUnderlineType.kuntNone);
			fontInfo.m_features = new InheritableStyleProp<string>(String.Empty);

			//docController.ShowDialog(DialogFactory.DialogType.Fonts);
			docController.ShowSWFDialog(DialogFactory.DialogType.Fonts,
				delegate(System.Windows.Forms.Form beforeDialog)
				{
					IFontDialog dlg = (IFontDialog)beforeDialog;

					LgWritingSystemFactory wsf = new LgWritingSystemFactory();
					dlg.Initialize(fontInfo, true, 1, wsf, false);

					return true;
				},
				delegate(System.Windows.Forms.Form afterDialog)
				{
					IFontDialog dlg = (IFontDialog)afterDialog;
					dlg.SaveFontInfo(fontInfo);

					// Set font (family) name
					if (fontInfo.m_fontSize.IsExplicit)
						docController.SetFontFamily(fontInfo.m_fontName.Value);

					// Set font size

					if (fontInfo.m_fontSize.IsExplicit)
					{
						int fontSize = fontInfo.m_fontSize.Value / 1000;
						docController.SetFontSize(fontSize.ToString());
					}

					// Set font color
					if (fontInfo.m_fontColor.IsExplicit)
					{
						PropertiesHelper.ChangeSelectionProperties(upperView,
							delegate(ITsPropsBldr qtpb)
							{
								qtpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)		FwTextPropVar.ktpvDefault, Convert(fontInfo.m_fontColor.Value));
							});
					}

					// Set background color
					if (fontInfo.m_backColor.IsExplicit)
					{
						PropertiesHelper.ChangeSelectionProperties(upperView,
							delegate(ITsPropsBldr qtpb)
							{
#if !__MonoCS__
								// TODO REVIEW Fixme - setting BackColor causes drawing problems.
								qtpb.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(fontInfo.m_backColor.Value));
#endif
							});
					}

					// Set bold
					if (fontInfo.m_bold.IsExplicit)
					{
						docController.SetBold(fontInfo.m_bold.Value ? ThreeState.True : ThreeState.False);
					}

					// Set italic
					if (fontInfo.m_italic.IsExplicit)
					{
						docController.SetItalic(fontInfo.m_italic.Value ? ThreeState.True : ThreeState.False);
					}

					// Set superscript, subscript, or normal

					// Set underline

					// Set underline color
					if (fontInfo.m_underlineColor.IsExplicit)
					{
						PropertiesHelper.ChangeSelectionProperties(upperView,
							delegate(ITsPropsBldr qtpb)
							{
								qtpb.SetIntPropValues((int)FwTextPropType.ktptUnderColor, (int)		FwTextPropVar.ktpvDefault, Convert(fontInfo.m_underlineColor.Value));
							});
					}

					// Set vertical offset

					// Set font features

					return false;
				});
		}

		private void on_demo(object obj, EventArgs args)
		{
			// GladeWin w = new GladeWin();
		}

		private void on_writing_system1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_writing_system1_activate() invoked");

			docController.ShowDialog(DialogFactory.DialogType.WritingSystem);
		}

		private void on_paragraph1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_paragraph1_activate() invoked");

			docController.ShowDialog(DialogFactory.DialogType.Paragraph);
		}

		private void on_bullets_and_numbering1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_bullets_and_numbering1_activate() invoked");

			docController.ShowDialog(DialogFactory.DialogType.BulletNumbering);
		}

		private void on_border1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_border1_activate() invoked");

			docController.ShowDialog(DialogFactory.DialogType.Borders);
		}

		private void on_style1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_style1_activate() invoked");

			docController.ShowDialog(DialogFactory.DialogType.Styles);
		}

		// Event handlers for Tools menu items

		private void on_options1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_options1_activate() invoked");

			docController.ShowDialog(DialogFactory.DialogType.Options);
		}

		private void on_writing_system_properties1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_writing_system_properties1_activate() invoked");

			docController.ShowDialog(DialogFactory.DialogType.OldWritingSystems);
		}

		// Event handlers for Window menu

		private void on_menuWindow_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_menuWindow_activate() invoked");

			/*Window[] mainWindows = Window.ListToplevels();
			foreach (Window w in mainWindows)
			{
				Console.WriteLine("Window title: {0}", w.Title);
				MenuItem item = new MenuItem("abc");
				MenuItem windowMenu = (MenuItem)obj;
				//windowMenu.Append(item);
			}*/
		}

		/*private void on_item1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_item1_activate() invoked");
		}*/

		// Event handlers for Help menu items

		private void on_about1_activate(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_about1_activate() invoked");

			docController.ShowDialog(DialogFactory.DialogType.About);
		}

		// Event handlers for toolbar items

		/// <summary>
		/// Event handler for toolbar button New. Opens the default (blank) document in a new window.
		/// </summary>
		private void on_toolbuttonNew_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_toolbuttonNew_clicked");
			on_new1_activate(obj, args);
		}

		/// <summary>
		/// Event handler for toolbar button Open. Opens a specified document in a new window.
		/// </summary>
		private void on_toolbuttonOpen_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_toolbuttonOpen_clicked");
			on_open1_activate(obj, args);
		}

		/// <summary>
		/// Event handler for toolbar button Save. Saves document to current filename.
		/// </summary>
		private void on_toolbuttonSave_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_toolbuttonSave_clicked");
			on_save1_activate(obj, args);
		}

		/// <summary>
		/// Event handler for toolbar button Cut.
		/// </summary>
		private void on_toolbuttonCut_clicked(object obj, EventArgs args)
		{
			on_cut1_activate(obj, args);
		}

		/// <summary>
		/// Event handler for toolbar button Copy.
		/// </summary>
		private void on_toolbuttonCopy_clicked(object obj, EventArgs args)
		{
			on_copy1_activate(obj, args);
		}

		/// <summary>
		/// Event handler for toolbar button Paste.
		/// </summary>
		private void on_toolbuttonPaste_clicked(object obj, EventArgs args)
		{
			on_paste1_activate(obj, args);
		}

		/// <summary>
		/// Event handler for toolbar button Print.
		/// </summary>
		private void on_toolbuttonPrint_clicked(object obj, EventArgs args)
		{
			on_print1_activate(obj, args);
		}

		private void on_toolbuttonSplit_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_toolbuttonSplit_toggled() invoked");

			Gtk.ToggleButton toolbuttonSplit = (Gtk.ToggleButton) obj;

			if (toolbuttonSplit.Active)
			{
				docController.SplitPane();
			}
			else
			{
				docController.ClosePane();
			}
		}

		private void on_comboboxFamily_changed(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_comboboxFamily_changed() invoked");

			TreeIter iter;

			if (comboboxFamily.GetActiveIter(out iter))
			{
				string family = (string)comboboxFamily.Model.GetValue(iter, 0);
				if (family != String.Empty)
				{
					// Remove the blank first entry if there is one
					TreeIter familiesIter;
					families.GetIterFirst(out familiesIter);
					if ((string)comboboxFamily.Model.GetValue(familiesIter, 0) == String.Empty)
					{
						bool removed = families.Remove(ref familiesIter);
					}

					// Set the Font Family according to the ComboBox selection
					if (!modelChange) // Is the user explicitly changing the ComboBox?
					{
						docController.SetFontFamily(family);
					}
					else
					{
						docModel.SetFontFamily(family);
					}
				}
			}
		}

		private void on_comboboxSize_changed(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_comboboxSize_changed() invoked");

			TreeIter iter;

			if (comboboxSize.GetActiveIter(out iter))
			{
				string sizeStr = (string)comboboxSize.Model.GetValue(iter, 0);
				if (sizeStr != String.Empty)
				{
					// Remove the blank first entry if there is one
					TreeIter sizesIter;
					sizes.GetIterFirst(out sizesIter);
					if ((string)comboboxSize.Model.GetValue(sizesIter, 0) == String.Empty)
					{
						bool removed = sizes.Remove(ref sizesIter);
					}
					// Set the Font Size according to the ComboBox selection

					if (!modelChange)
					{
						docController.SetFontSize(sizeStr);
					}
					else
					{
						docModel.SetFontSize(sizeStr);
					}

				}
			}
		}

		private void on_comboboxStyle_changed(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_comboboxStyle_changed() invoked");

			TreeIter iter;

			if (comboboxStyle.GetActiveIter(out iter))
			{
				string styleStr = (string)comboboxStyle.Model.GetValue(iter, 0);
				if (styleStr != String.Empty)
				{
					// Remove the blank first entry if there is one
					TreeIter stylesIter;
					styles.GetIterFirst(out stylesIter);
					if ((string)comboboxStyle.Model.GetValue(stylesIter, 0) == String.Empty)
					{
						bool removed = sizes.Remove(ref stylesIter);
					}
					// Set the Font Size according to the ComboBox selection
					docController.SetStyle(styleStr);

					// modelChange is false if the user is explicitly changing the ComboBoxes
					if (!modelChange)
					{
						// TODO This is a test implementation of the font name changing.
						PropertiesHelper.ChangeSelectionProperties(upperView,
							delegate(ITsPropsBldr qtpb)
							{
								qtpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleStr);
							});
						// TODO End Test implementation.
					}

				}
			}
		}

		private void on_comboboxWs_changed(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_comboboxWs_changed() invoked");

			TreeIter iter;

			if (this.comboboxWS.GetActiveIter(out iter))
			{
				string wsStr = (string)comboboxWS.Model.GetValue(iter, 0);
				if (wsStr != String.Empty)
				{
					// Remove the blank first entry if there is one
					TreeIter wsIter;

					writingSystems.GetIterFirst(out wsIter);
					if ((string)comboboxWS.Model.GetValue(wsIter, 0) == String.Empty)
					{
						bool removed = sizes.Remove(ref wsIter);
					}
					// Set the Writing System according to the ComboBox selection
					docController.SetWritingSystem(wsStr);

					// Lookup Ws Abr from full name
					string wsAbrStr = string.Empty;
					IDictionaryEnumerator enumerator = docModel.WritingSystems.GetEnumerator();
					while (enumerator.MoveNext())
					{
						if ((string)enumerator.Value == wsStr)
						{
							wsAbrStr = (string)enumerator.Key;
						}
					}

					// modelChange is false if the user is explicitly changing the ComboBoxes
					if (!modelChange)
					{
						ILgWritingSystemFactory wsf = new LgWritingSystemFactory();
						int ws = wsf.GetWsFromStr(wsAbrStr);

						PropertiesHelper.ChangeSelectionProperties(upperView,
							delegate(ITsPropsBldr qtpb)
							{
								qtpb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
							});
					}
				}
			}

		}

		private void on_toggletoolbuttonBold_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_toggletoolbuttonBold_toggled() invoked");

			//docController.SetBold(toggletoolbuttonBold.Active);

			if (!modelChange)
			{
				docController.SetBold(toggletoolbuttonBold.Active ? ThreeState.True :
					ThreeState.False);
			}
			else
			{
				docModel.SetBold(ThreeState.Indeterminate);
			}

			Console.WriteLine("WorldPadDocView.on_toggletoolbuttonBold_toggled() exited");
		}

		private void on_toggletoolbuttonItalic_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_toggletoolbuttonItalic_toggled() invoked");

			if (!modelChange)
			{
				docController.SetItalic(toggletoolbuttonItalic.Active ? ThreeState.True :
				ThreeState.False);
			}
			else
			{
				docModel.SetItalic(ThreeState.Indeterminate);
			}

			Console.WriteLine("WorldPadDocView.on_toggletoolbuttonItalic_toggled() exited");
		}


		// prevents recustion in managing the toggle states.
		private bool inAlignUpdate = false;


		private void setAlignToggleState(FwTextAlign alignState)
		{
			if (inAlignUpdate)
				return;

			inAlignUpdate = true;

			switch(alignState)
			{
			case FwTextAlign.ktalLeft:
				setAlignToggleState(true, false, false);
				break;
			case FwTextAlign.ktalCenter:
				setAlignToggleState(false, true, false);
				break;
			case FwTextAlign.ktalRight:
				setAlignToggleState(false, false, true);
				break;
			default: // Should not occur.
				setAlignToggleState(false, false, false); // We don't know how to display this!
				break;
			}

			if (!modelChange)
			{

				if (upperView.GetRootBox() != null)
				{
					// Inform VwGraphicGTK that we are going to modify it.
					upperView.VwGraphicsGTK.BeginDraw();
					PropertiesHelper.FormatParas(upperView.GetRootBox(),
						delegate(ITsPropsBldr tpb) { tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)alignState); return tpb; });
					upperView.VwGraphicsGTK.EndDraw(); // modification finished.
				}

			}

			inAlignUpdate = false;
		}


		// prevents recustion in managing the Display of Align Toggle States states.
		private bool inAlignDisplayUpdate = false;

		// Don't call this method directly call SetAlignTogglestate with the enum
		// As this Method Only changes the combo boxes state.
		private void setAlignToggleState(bool left, bool center, bool right)
		{
			if (inAlignDisplayUpdate)
				return;

			inAlignDisplayUpdate = true;

			if (left != toggletoolbuttonLeft.Active)
				toggletoolbuttonLeft.Active = left;

			if (center != toggletoolbuttonCenter.Active)
				toggletoolbuttonCenter.Active = center;

			if (right != toggletoolbuttonRight.Active)
				toggletoolbuttonRight.Active = right;

			inAlignDisplayUpdate = false;

		}

		private void on_toggletoolbuttonLeft_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_toggletoolbuttonLeft_toggled() invoked");

			docController.SetAlign(FwTextAlign.ktalLeft);

			setAlignToggleState(FwTextAlign.ktalLeft);
		}

		private void on_toggletoolbuttonCenter_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_toggletoolbuttonCenter_toggled() invoked");
			docController.SetAlign(FwTextAlign.ktalCenter);

			setAlignToggleState(FwTextAlign.ktalCenter);
		}

		private void on_toggletoolbuttonRight_toggled(object obj, EventArgs args)
		{
			Console.WriteLine("WorldPadDocView.on_toggletoolbuttonRight_toggled() invoked");
			docController.SetAlign(FwTextAlign.ktalRight);

			setAlignToggleState(FwTextAlign.ktalRight);
		}
	}
}
