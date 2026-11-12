// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Asks the user for one of the project's categories with the chooser dialog, shown as
	/// the category tree with nothing selected.
	/// </summary>
	public sealed class LcmCategoryChooserLauncher
		: AvaloniaDialogLauncher<ChooserDialogInput, ChooserDialogViewModel, IPartOfSpeech>
	{
		private readonly LcmCache _cache;
		private readonly string _title;
		private ChooserDialogViewModel _viewModel;

		private LcmCategoryChooserLauncher(LcmCache cache, string title)
		{
			_cache = cache;
			_title = title;
		}

		/// <summary>
		/// Shows the project's categories over <paramref name="owner"/>.
		/// </summary>
		/// <param name="title">The dialog's title.</param>
		/// <returns>The chosen category; null when the user cancels.</returns>
		public static IPartOfSpeech Choose(LcmCache cache, IWin32Window owner, string title)
		{
			if (cache == null)
				throw new ArgumentNullException(nameof(cache));
			var outcome = new LcmCategoryChooserLauncher(cache, title).Run(owner);
			return outcome.Accepted ? outcome.Payload : null;
		}

		protected override string DialogTitle => _title;
		protected override bool Resizable => true;
		protected override int DialogWidth => 420;
		protected override int DialogHeight => 460;

		protected override ChooserDialogInput BuildState() => BuildInput(_cache);

		/// <summary>
		/// The chooser's input: every category as a tree, with one choice required before OK.
		/// </summary>
		internal static ChooserDialogInput BuildInput(LcmCache cache)
		{
			return new ChooserDialogInput
			{
				Candidates = LcmInsertEntryDialogLauncher.BuildPosNodes(cache)
					.Select(n => new DetailChoiceOption(n.Id, n.Name, n.Depth)).ToList(),
				SelectionMode = ChooserSelectionMode.Single,
				InitialSelectedKeys = Array.Empty<string>(),
				AllowEmpty = false,
				Hierarchical = true,
				ForbidEmptySelection = true
			};
		}

		/// <summary>The category a chosen key names; null for no key or one that names
		/// none.</summary>
		internal static IPartOfSpeech Resolve(LcmCache cache, string key)
		{
			return Guid.TryParse(key, out var guid)
				&& cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().TryGetObject(guid, out var category)
				? category
				: null;
		}

		protected override ChooserDialogViewModel CreateViewModel(ChooserDialogInput state)
		{
			_viewModel = new ChooserDialogViewModel(state);
			return _viewModel;
		}

		protected override AvControl CreateView(ChooserDialogViewModel viewModel)
			=> new ChooserDialogView { DataContext = viewModel };

		protected override IPartOfSpeech Apply(ChooserDialogInput state)
			=> Resolve(_cache, _viewModel?.ChosenKeys?.FirstOrDefault());
	}
}
