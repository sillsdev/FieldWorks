using Avalonia.Controls;
using Avalonia.PropertyGrid.Controls;
using AdvancedEntry.Avalonia.Models;

namespace AdvancedEntry.Avalonia.Views
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			// Seed a trivial DTO into the PropertyGrid for a visible demo
			if (this.FindControl<PropertyGrid>("EntryPropertyGrid") is { } grid)
			{
				var sample = EntryModel.CreateSample();
				var t = grid.GetType();
				var p = t.GetProperty("SelectedObject") ?? t.GetProperty("ObjectInstance");
				p?.SetValue(grid, sample);
			}
		}
	}
}