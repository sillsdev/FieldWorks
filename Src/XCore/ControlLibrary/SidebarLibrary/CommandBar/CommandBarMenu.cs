// Original author or copyright holder unknown.

using System.Windows.Forms;

namespace SidebarLibrary.CommandBars
{
	public class CommandBarMenu : ContextMenu
	{
		// This is just to keep track of the selected menu as well as hold the menuitems in the menubar
		internal Menu SelectedMenuItem { set; get; }

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			base.Dispose(disposing);
		}
	}
}
