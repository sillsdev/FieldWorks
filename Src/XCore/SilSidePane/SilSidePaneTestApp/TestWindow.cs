// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System.Drawing;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Client to test SidePane
	/// </summary>
	public partial class TestWindow : Form
	{
		private SidePane sidePane;

		public TestWindow()
		{
			InitializeComponent();
			sidePane = new SidePane(this.splitContainer1.Panel1, SidePaneItemAreaStyle.List);

			// Add some tabs

			Tab scriptureTab = new Tab("Scripture");
			Tab backTab = new Tab("Back");
			Tab printTab = new Tab("Print");

			sidePane.AddTab(scriptureTab);
			sidePane.AddTab(backTab);
			sidePane.AddTab(printTab);

			// Add items to certain tabs

			var itemIcon = new Bitmap(32, 32);
			for (int x = 0; x < itemIcon.Width; ++x)
				for (int y = 0; y < itemIcon.Height; ++y)
					itemIcon.SetPixel(x, y, Color.Blue);

			Item scriptureDraftItem = new Item("Draft")
				{
					Icon = itemIcon,
				};
			Item scripturePrintItem = new Item("Print Scr Layout")
				{
					Icon = itemIcon,
				};
			Item backDraftItem = new Item("Draft")
				{
					Icon = itemIcon,
				};
			Item backPrintItem = new Item("Print Back Layout")
				{
					Icon = itemIcon,
				};

			sidePane.AddItem(scriptureTab, scriptureDraftItem);
			sidePane.AddItem(scriptureTab, scripturePrintItem);
			sidePane.AddItem(backTab, backDraftItem);
			sidePane.AddItem(backTab, backPrintItem);

			// Set up click handling - make main pane look different upon item click
			sidePane.ItemClicked += new SidePane.ItemClickedEventHandler(sidePane_ItemClicked);
		}

		void sidePane_ItemClicked(Item itemClicked)
		{
			// When an item is clicked, change the label in the main pane.
			this.label1.Text = "Current View: " + itemClicked.Text;
		}
	}
}
