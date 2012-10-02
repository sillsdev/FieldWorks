/*
 *    WorldPadDocController.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.Collections;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.WorldPad
{
	public class WorldPadDocController : IWorldPadDocController
	{
		private IWorldPadAppController appController;
		private IWorldPadDocModel docModel;
		private IWorldPadDocView docView;
		private ArrayList paneViews = new ArrayList();
		private DialogFactory dialogFactory;

		public IWorldPadAppController AppController
		{
			get
			{
				return appController;
			}
		}

		public IWorldPadDocModel DocModel
		{
			get
			{
				return docModel;
			}
		}

		public IWorldPadPaneView UpperPane
		{
			get
			{
				return (IWorldPadPaneView)paneViews[0];
			}
		}

		public IWorldPadPaneView LowerPane
		{
			get
			{
				return (IWorldPadPaneView)paneViews[1];
			}
		}

		public WorldPadDocController()
		{ // TODO: Is this constructor needed?
			Console.WriteLine("WorldPadDocController() invoked");
		}

		public WorldPadDocController(IWorldPadDocModel docModel,
			IWorldPadAppController appController)
		{
			Console.WriteLine("WorldPadDocController.ctor invoked");

			this.docModel = docModel;
			this.appController = appController;

			/*IWorldPadPaneModel paneModel = docModel.AddPane();

			paneControllers.Add(new WorldPadPaneController(paneModel, this));*/

			WorldPadPaneView wpPaneView = new WorldPadPaneView(this, docModel);
			//docModel.DefaultAttributes = wpPaneView.TextView1.DefaultAttributes;
			paneViews.Add(wpPaneView);
			docView = (IWorldPadDocView) new WorldPadDocView(this, docModel);

			string inpath = docModel.FileName;
			docView.LoadFromXml(inpath);

			dialogFactory = new DialogFactory(docModel);
			// The following statement tests the model change handlers
			//docModel.ActionPerformed();
		}

		public void SplitPane()
		{
			Console.WriteLine("WorldPadDocController.SplitPane() invoked");

			/*IWorldPadPaneModel paneModel = docModel.AddPane();

			paneControllers.Add(new WorldPadPaneController(paneModel, this));*/
			paneViews.Add(new WorldPadPaneView(this, docModel));
			docView.SplitPane();
		}

		public void ClosePane()
		{
			Console.WriteLine("WorldPadDocController.ClosePane() invoked");

			docView.ClosePane();
			/*paneControllers.RemoveAt(1);*/
			paneViews.RemoveAt(1);
		}

		/// <summary>
		/// Implements IWorldPadDocController method.
		/// </summary>
		public void SetWritingSystem(string writingSystem)
		{
			Console.WriteLine("WorldPadDocController.SetWritingSystem() invoked");

			Console.WriteLine("WritingSystem: {0}", writingSystem);

			docModel.SetWritingSystem(writingSystem);
		}

		public void SetFontFamily(string fontFamily)
		{
			Console.WriteLine("WorldPadDocController.SetFontFamily() invoked");

			Console.WriteLine("FontFamily: {0}", fontFamily);

			docModel.SetFontFamily(fontFamily);

			PropertiesHelper.ChangeSelectionProperties((WorldPadView)UpperPane.View,
				delegate(ITsPropsBldr qtpb)
				{
					qtpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, fontFamily);
				});
		}

		public void SetFontSize(string fontSize)
		{
			Console.WriteLine("WorldPadDocController.SetFontSize() invoked");

			Console.WriteLine("FontSize: {0}", fontSize);

			docModel.SetFontSize(fontSize);

			PropertiesHelper.ChangeSelectionProperties((WorldPadView)UpperPane.View,
				delegate(ITsPropsBldr qtpb)
				{
					qtpb.SetIntPropValues((int)FwTextPropType.ktptFontSize,
						(int)FwTextPropVar.ktpvMilliPoint, Convert.ToInt32(fontSize) * 1000);
				});
		}

		public void SetStyle(string style)
		{
			Console.WriteLine("WorldPadDocController.SetStyle() invoked");

			Console.WriteLine("FontSize: {0}", style);

			docModel.SetStyle(style);
		}

		public void SetBold(ThreeState boldState)
		{
			Console.WriteLine("WorldPadDocController.SetBold() invoked");

			Console.WriteLine("Bold on: {0}", boldState);

			docModel.SetBold(boldState);

			PropertiesHelper.ChangeSelectionProperties((WorldPadView)UpperPane.View,
			delegate(ITsPropsBldr qtpb)
			{
				if (boldState == ThreeState.True)
					qtpb.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				else if (boldState == ThreeState.False)
					qtpb.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
			});
		}

		public void SetItalic(ThreeState italicState)
		{
			Console.WriteLine("WorldPadDocController.SetItalic() invoked");

			Console.WriteLine("Italic on: {0}", italicState);

			docModel.SetItalic(italicState);

			PropertiesHelper.ChangeSelectionProperties((WorldPadView)UpperPane.View,
			 delegate(ITsPropsBldr qtpb)
			 {
			  if (italicState == ThreeState.True)
				qtpb.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			  else if (italicState == ThreeState.False)
				qtpb.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
			 });

		}

		public void SetAlign(FwTextAlign textAlign)
		{
			Console.WriteLine("WorldPadDocController.SetIAlign() invoked");

			Console.WriteLine("Align : {0}", textAlign);

			docModel.SetAlign(textAlign);
		}

		public void Init()
		{
			Console.WriteLine("WorldPadDocController.Init() invoked");
		}

		public void Quit()
		{
			Console.WriteLine("WorldPadDocController.Quit() invoked");

			appController.Quit();
		}

		public void FileNew()
		{
			Console.WriteLine("WorldPadDocController.FileNew() invoked");

			appController.FileNew();
		}

		public void FileOpen(string filename)
		{
			Console.WriteLine("WorldPadDocController.FileOpen({0}) invoked", filename);
			appController.FileOpen(filename);
		}

		public void FileClose()
		{
			Console.WriteLine("WorldPadDocController.FileClose() invoked");

			//appController.FileClose();
			appController.FileClose(this);
		}

		public DialogController ShowDialog(DialogFactory.DialogType type)
		{
			DialogController dialog = dialogFactory.CreateDialog(type);
			try {
				dialog.Show(docView);
				return dialog;
			} catch (NullReferenceException ex) {
				Console.Error.WriteLine(ex.ToString());
				return null;
			}
		}



		public System.Windows.Forms.Form ShowSWFDialog(DialogFactory.DialogType type)
		{
			return ShowSWFDialog(type, null, null);
		}

		public System.Windows.Forms.Form ShowSWFDialog(DialogFactory.DialogType type,
				BeforeShow before, AfterShow after)
		{
			System.Windows.Forms.Form dialog = dialogFactory.CreateSWFDialog(type);
			try {
				before(dialog);
				System.Windows.Forms.DialogResult result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK)
					after(dialog);
				return dialog;
			} catch (NullReferenceException ex) {
				Console.Error.WriteLine(ex.ToString());
				return null;
			}
		}
	}
}
