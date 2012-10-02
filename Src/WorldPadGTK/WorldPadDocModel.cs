/*
 *    WorldPadDocModel.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using System;
using System.Collections;
using System.IO;
using SIL.FieldWorks.Common.COMInterfaces;


namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// Replaces bool for storing bold and italic state.
	/// </summary>
	public enum ThreeState
	{
		False = 0,
		True = 1,
		Indeterminate = 2,
	}

	public class DocModelChangedEventArgs : EventArgs, IDocModelChangedEventArgs {

		public readonly int hour;
		public readonly int minute;
		public readonly int second;

		public DocModelChangedEventArgs(int hour, int minute, int second) {

			this.hour = hour;
			this.minute = minute;
			this.second = second;

		}
	}

	/// <summary>
	/// MVC-Model of a WorldPad Document (though much of the document contents are
	/// stored in WpDa).
	/// </summary>
	public class WorldPadDocModel : IWorldPadDocModel
	{
		public string DocName
		{
			get
			{
				return docName;
			}
		}

		public string FileName
		{
			get
			{
				return fileName;
			}
		}

		public bool IsDefault
		{
			get
			{
				return isDefault;
			}
		}

		public Hashtable Styles
		/*public object Styles*/
		{
			get
			{
				return styles;
			}
		}

		public string ParagraphStyle
		{
			get
			{
				return paragraphStyle;
			}
		}

		public Hashtable WritingSystems
		{
			get
			{
				return writingSystems;
			}
		}

		public string SelectionWritingSystem
		{
			get
			{
				return selectionWritingSystem;
			}
		}


		public string SelectionFontFamily
		{
			get
			{
				return selectionFontFamily;
			}
		}

		public string SelectionFontSize
		{
			get
			{
				return selectionFontSize;
			}
		}

		public string SelectionStyle
		{
			get
			{
				return selectionStyle;
			}
		}

		public ThreeState SelectionBold
		{
			get
			{
				return selectionBold;
			}
		}

		public ThreeState SelectionItalic
		{
			get
			{
				return selectionItalic;
			}
		}

		public bool JustificationLeft
		{
			get
			{
				return justificationLeft;
			}
		}

		public bool JustificationRight
		{
			get
			{
				return justificationRight;
			}
		}

		/// <value>
		/// Implements IWorldPadDocModel property
		/// </value>
		public WpgtkMainWnd MainWnd
		{
			get
			{
				return mainWnd;
			}
			set
			{
				mainWnd = value;
			}
		}

		public bool JustificationCenter
		{
			get
			{
				return justificationCenter;
			}
		}

		public static int newDocSfx = 0;

		public event DocModelChangedEventHandler ModelChanged;

		private IWorldPadAppModel appModel;

		private string docName;

		private string fileName;

		private bool isDefault;

		private WpDoc document;
		/*protected WpDoc document;*/

		private Hashtable styles;
		/*protected Hashtable styles;*/

		private string paragraphStyle;

		private Hashtable writingSystems;

		private string selectionWritingSystem;

		private string selectionFontFamily = String.Empty;

		private string selectionFontSize;

		private string selectionStyle;

		private ThreeState selectionBold = ThreeState.Indeterminate;

		private ThreeState selectionItalic = ThreeState.Indeterminate;

		private bool justificationLeft;

		private bool justificationRight;

		private WpgtkMainWnd mainWnd;

		private bool justificationCenter;

		public WorldPadDocModel(IWorldPadAppModel appModel)
		{
			Console.WriteLine("WorldPadDocModel.ctor(appModel) invoked");

			this.appModel = appModel;

			if (newDocSfx == 0)
			{
				isDefault = true;
				docName = "Unsaved Document";
			}
			else
			{
				docName = "Unsaved Document " + newDocSfx;
			}

			newDocSfx++;
		}

		/// <summary>
		/// Constructs a WorldPadDocModel. Queries styles and writing systems for GUI.
		/// Note that the more meaningful loading of the WPX file happens later on in
		/// WorldPadDocController.ctor by calling WorldPadDocView.LoadFromXml().
		/// </summary>
		/// <param name="appModel">Application model holding this document model.</param>
		/// <param name="fileName">Path to WPX or WPT file to load.</param>
		public WorldPadDocModel(IWorldPadAppModel appModel, string fileName)
		{
			Console.WriteLine("WorldPadDocModel.ctor(appModel, string) invoked");

			this.appModel = appModel;

			this.fileName = fileName;

			int docNameIndex = fileName.LastIndexOf(Path.DirectorySeparatorChar);
			docName = fileName.Substring(docNameIndex + 1);

			if (docName == "default.wpt")  // TODO: there must be a better way!
			{
				if (newDocSfx == 0)
				{
					isDefault = true;
					docName = "Unsaved Document";
				}
				else
				{
					docName = "Unsaved Document " + newDocSfx;
				}
				newDocSfx++;
			}

			document = WpDoc.Deserialize(fileName);  // TODO: Check that document is non-null

			CreateStyles();

			GetWritingSystems();
		}

		public void ActionPerformed()
		{
			Console.WriteLine("WorldPadDocModel.ActionPerformed() invoked");

			/*if (OnModelInfoChange != null)
			{*/
				System.DateTime dt = System.DateTime.Now;
				/*ModelInfoEventArgs modelInformation =*/
				DocModelChangedEventArgs e =
					new DocModelChangedEventArgs(dt.Hour, dt.Minute, dt.Second);
				/*OnModelInfoChange(this, modelInformation);*/
				OnModelChanged(e);
			/*}*/
		}

		protected virtual void OnModelChanged(DocModelChangedEventArgs e)
		{
			Console.WriteLine("WorldPadDocModel.OnModelChanged() invoked");

			if (ModelChanged != null)
			{
				ModelChanged(this, e);  // Raise the event
			}
		}

		public void Init()
		{
			Console.WriteLine("WorldPadDocModel.Init() invoked");
		}

		public void Subscribe(DocModelChangedEventHandler handler)
		{
			Console.WriteLine("WorldPadDocModel.Subscribe() invoked");

			/*OnModelInfoChange += handler;*/
			ModelChanged += handler;
		}

		/// <summary>
		/// Implements IWorldPadDocModel method.
		/// </summary>
		public void SetWritingSystem(string writingSystem)
		{
			Console.WriteLine("WorldPadDocModel.SetWritingSystem() invoked");

			Console.WriteLine("WritingSystem: {0}", writingSystem);

			selectionWritingSystem = writingSystem;
		}

		public void SetFontFamily(string fontFamily)
		{
			Console.WriteLine("WorldPadDocModel.SetFontFamily() invoked");

			Console.WriteLine("FontFamily: {0}", fontFamily);

			selectionFontFamily = fontFamily;
		}

		public void SetFontSize(string fontSize)
		{
			Console.WriteLine("WorldPadDocModel.SetFontSize() invoked");

			Console.WriteLine("FontSize: {0}", fontSize);

			selectionFontSize = fontSize;
		}

		public void SetStyle(string style)
		{
			Console.WriteLine("WorldPadDocModel.SetStyle() invoked");

			Console.WriteLine("Style: " +  style);

			selectionStyle = style;
		}

		public void SetBold(ThreeState boldOn)
		{
			Console.WriteLine("WorldPadDocModel.SetBold() invoked");

			Console.WriteLine("Bold on: {0}", boldOn);

			selectionBold = boldOn;

		}

		public void SetItalic(ThreeState italicOn)
		{
			Console.WriteLine("WorldPadDocModel.SetItalic() invoked");

			Console.WriteLine("Italic on: {0}", italicOn);

			selectionItalic = italicOn;
		}

		public void SetAlign(FwTextAlign textAlign)
		{
			Console.WriteLine("WorldPadDocModel.SetAligb() invoked");

			Console.WriteLine("Align: {0}", textAlign);

			justificationCenter = (textAlign == FwTextAlign.ktalCenter);
			justificationLeft = (textAlign == FwTextAlign.ktalLeft);
			justificationRight = (textAlign == FwTextAlign.ktalRight);

		}

		private void CreateStyles()
		/*public virtual void CreateStyles()*/
		{
			Console.WriteLine("CreateStyles() invoked");

			//Hashtable styles = document.GetStyles();
			styles = document.GetStyles();
		}

		private void GetWritingSystems()
		{
			Console.WriteLine("GetWritingSystems() invoked");

			this.writingSystems = new Hashtable();

			ArrayList writingSystems = document.GetWritingSystems();

			foreach (LgWritingSystem lgWritingSystem in writingSystems)
			{
				//Console.WriteLine("Processing Writing System: {0}", lgWritingSystem.id);

				string wsName = lgWritingSystem.id;

				if (lgWritingSystem.name24 != null && lgWritingSystem.name24.aUni != null)
				{
					wsName = lgWritingSystem.name24.aUni[0].text;
				}

				this.writingSystems.Add(lgWritingSystem.id, wsName);
				Console.WriteLine("Writing System id: {0}, name: {1}",
					lgWritingSystem.id, wsName);
			}
		}
	}

	/*public class WorldPadDocModelGtk : WorldPadDocModel
	{
		public new Gtk.ListStore Styles
		{
			get
			{
				return styles;
			}
		}

		private new Gtk.ListStore styles;

		public WorldPadDocModelGtk(IWorldPadAppModel appModel) : base(appModel)
		{
			Console.WriteLine("WorldPadDocModelGtk.ctor(appModel) invoked");
		}

		public WorldPadDocModelGtk(IWorldPadAppModel appModel, string fileName)
			: base(appModel, fileName)
		{
			Console.WriteLine("WorldPadDocModelGtk.ctor(appModel, string) invoked");
		}

		public override void CreateStyles()
		{
			Console.WriteLine("WorldPadDocModelGtk.CreateStyles() invoked");

			styles = new Gtk.ListStore(typeof(string));

			Gtk.TreeIter iter = new Gtk.TreeIter();
			IDictionaryEnumerator enumerator = document.GetStyles().GetEnumerator();
			while (enumerator.MoveNext())
			{
				//Console.WriteLine("Processing style: {0}", enumerator.Key);

				iter = styles.AppendValues((string)enumerator.Key);
			}
		}
	}*/
}
