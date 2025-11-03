using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;

namespace AdvancedEntry.Avalonia.Models
{
    // Keep DTOs simple and PropertyGrid-friendly
    public partial class EntryModel : ObservableValidator
    {
        [ObservableProperty]
        [Required]
        private string? lexicalForm;

        [ObservableProperty]
        [Required]
        private string? morphType;

        [ObservableProperty]
        private string? partOfSpeech;

        public static EntryModel CreateSample()
        {
            var m = new EntryModel();
            m.LexicalForm = "demo";
            m.MorphType = "root";
            m.PartOfSpeech = "Noun";
            m.ValidateAllProperties();
            return m;
        }
    }
}
