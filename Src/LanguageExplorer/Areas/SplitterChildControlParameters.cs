using System.Windows.Forms;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Class that contains the control and related inforamtion used to set up a main (left/top or right/bottom) control in a splitter control
	/// </summary>
	internal class SplitterChildControlParameters
	{
		/// <summary />
		internal Control Control { get; set; }
		/// <summary />
		internal string Label { get; set; }
	}
}