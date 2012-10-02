/*
 *    WorldPadPaneView.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.IO;
using Gtk;
using Glade;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.WorldPad
{
	public class WorldPadPaneView : IWorldPadPaneView
	{
		[Widget]
		private WorldPadView worldPadView;

		private IWorldPadDocModel docModel;
		private IWorldPadDocController docController;

		public IWorldPadView View
		{
			get
			{
				return (IWorldPadView)worldPadView;
			}
			set
			{
				worldPadView = (WorldPadView)value;
				worldPadView.SetDocumentCallbacks(docModel, docController);
			}
		}

		public WorldPadPaneView(IWorldPadDocController docController,
			IWorldPadDocModel docModel)
		{
			Console.WriteLine("WorldPadPaneView.ctor invoked");

			this.docModel = docModel;
			this.docController = docController;
/*
			// register observer (view) with subject (model)
			docModel.Subscribe(
				new WorldPadDocModel.ModelInfoEventHandler(on_modelinfochange_event));*/

//			Catalog.Init("csdialogdemo", "./locale");

			CustomWidgetHandler.Prepare();


			/*// Note: "using" will automatically release resources
			using (FileStream stream = new FileStream("glade/wpmain.glade", FileMode.Open))
			{
				// Note: use of "stream" constructor
				Glade.XML gxml = new Glade.XML(stream, "window2", null);
				gxml.Autoconnect(this);
			}*/
			Glade.XML gxml =
				new Glade.XML(Utils.GetProgramDirectory() + WorldPadDocView.MAIN_GLADE_FILE, "window2", null);
			gxml.Autoconnect(this);

			worldPadView.SetDocumentCallbacks(docModel, docController);
		}

		public void on_modelchanged_event(object o, IDocModelChangedEventArgs args)
		{
			Console.WriteLine("WorldPadPaneView.on_modelchanged_event() invoked");

//			lblReturn.Text = args.hour.ToString() + ":" + args.minute.ToString() + ":"
//				+ args.second.ToString();
		}

		public void Init()
		{
			Console.WriteLine("WorldPadPaneView.Init() invoked");
		}
	}
}
