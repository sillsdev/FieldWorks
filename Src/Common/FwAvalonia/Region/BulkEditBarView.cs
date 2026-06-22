// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// One bulk-edit target column the List Choice tab can write across the checked rows (3c, Phase 1):
	/// the column INDEX the table identifies it by and a display LABEL. LCModel-free — the product edge
	/// (the clerk-backed row source) decides which columns are eligible (an unambiguous, entry-anchored
	/// possibility target such as Morph Type) and supplies these; the bar never inspects the model.
	/// </summary>
	public sealed class BulkEditTarget
	{
		public BulkEditTarget(int column, string label)
		{
			Column = column;
			Label = label;
		}

		/// <summary>The table column index this target writes (the same index PreviewBulkEdit/ApplyBulkEdit take).</summary>
		public int Column { get; }

		/// <summary>The user-facing column name shown in the target dropdown.</summary>
		public string Label { get; }

		public override string ToString() => Label;
	}

	/// <summary>
	/// The non-empty-target behavior of a Bulk Copy (Phase 2), mirroring the legacy bar's NonEmptyTarget
	/// options: <see cref="Append"/> (target + separator + source when the target already has text, else just
	/// source), <see cref="Replace"/> (source overwrites the target unconditionally), and
	/// <see cref="DoNothingIfNonEmpty"/> (fill only empty targets — leave non-empty ones untouched).
	/// </summary>
	public enum BulkCopyMode
	{
		/// <summary>Append the source to a non-empty target (with the separator); set it directly when empty.</summary>
		Append,
		/// <summary>The source overwrites the target unconditionally.</summary>
		Replace,
		/// <summary>Only fill empty targets; skip any target that already has text.</summary>
		DoNothingIfNonEmpty
	}

	/// <summary>
	/// The word-vs-whole-field granularity of an interactive Click Copy (mirroring the legacy bar's
	/// click-copy word/reorder radios): <see cref="Word"/> copies just the clicked word from the source cell;
	/// <see cref="Reorder"/> copies the WHOLE source cell, reordered so the clicked word leads (the legacy
	/// "reorder" mode — see <c>BulkEditBar.xbv_ClickCopy</c>). The producer interprets the mode over the
	/// clicked cell's text and (when available) the clicked word offset.
	/// </summary>
	public enum ClickCopyMode
	{
		/// <summary>Copy just the clicked word from the source cell.</summary>
		Word,
		/// <summary>Copy the whole source cell, reordered to lead with the clicked word.</summary>
		Reorder
	}

	/// <summary>
	/// An LCModel-free, FwAvaloniaDialogs-free Find/Replace pattern as the bulk-edit bar and the row source see
	/// it (Find/Replace Phase 1). It carries the same fields as the dialog kit's <c>FindReplacePattern</c>
	/// (which lives in FwAvaloniaDialogs and cannot be referenced from this foundation layer); the product host
	/// (RecordBrowseView) translates between the two when it opens the dialog and runs the replace. The bar
	/// holds the user's last-edited spec so the summary and Preview/Apply have something to act on; the producer
	/// (the clerk-backed row source) interprets it over each target cell. <see cref="MatchDiacritics"/>/
	/// <see cref="MatchWritingSystem"/> are P1 no-ops (see the producer); they are carried for parity/P2.
	/// </summary>
	public sealed class BulkReplaceSpec
	{
		public string FindText { get; set; } = string.Empty;
		public string ReplaceText { get; set; } = string.Empty;
		public bool MatchCase { get; set; }
		public bool MatchDiacritics { get; set; }
		public bool MatchWholeWord { get; set; }
		public bool MatchWritingSystem { get; set; }
		public bool UseRegularExpressions { get; set; }
	}

	/// <summary>
	/// An LCModel-free, EncConverters-free view of one selectable converter for the Process/Transduce tab:
	/// a display <see cref="Name"/> and a <see cref="Convert"/> transform over a cell's plain text. The
	/// product edge (RecordBrowseView) wraps each Unicode-to-Unicode <c>IEncConverter</c> from the
	/// EncConverters pool in one of these (mirroring the legacy bar's <c>InitConverterCombo</c> filter), so
	/// the bar/VM and the headless row source never reference <c>SilEncConverters40</c>. Tests substitute a
	/// deterministic transform (e.g. upper-casing) for the same shape.
	/// </summary>
	public interface IBulkTransduceConverter
	{
		/// <summary>The converter's display name (shown in the picker and used to look it up in the pool).</summary>
		string Name { get; }

		/// <summary>Runs the converter over a cell's plain text and returns the converted text.</summary>
		string Convert(string input);
	}

	/// <summary>
	/// The host seam the bulk-edit bar drives (3c, Phase 1 List Choice + Phase 2 Bulk Copy). Keeps the
	/// bar/VM LCModel-free: the product edge (RecordBrowseView, over the clerk-backed
	/// <c>ClerkBrowseRowSource</c> and the owned <see cref="LexicalBrowseView"/>) supplies the eligible
	/// columns/options and runs the actual preview/apply over the table's CHECKED rows. The bar holds none of
	/// the domain. Phase 2 Bulk Copy goes through these direct host methods (not the generic
	/// IBrowseBulkEditSource contract) because copy preview/apply need (source, target, mode), not a single
	/// value — the host routes them straight to the row source's copy methods.
	/// </summary>
	public interface IBulkEditBarHost
	{
		/// <summary>The columns eligible as a List-Choice bulk target (index + label); empty when none.</summary>
		IReadOnlyList<BulkEditTarget> ListChoiceTargets();

		/// <summary>The selectable options (key + display name) for a target column; empty when none.</summary>
		IReadOnlyList<RegionChoiceOption> OptionsFor(int column);

		/// <summary>The number of rows currently checked — the bar gates Apply on this being &gt; 0.</summary>
		int CheckedRowCount { get; }

		/// <summary>Previews <paramref name="option"/>'s display name across the checked rows (no model mutation).</summary>
		void Preview(int column, RegionChoiceOption option);

		/// <summary>Clears any pending bulk-edit preview overlay.</summary>
		void ClearPreview();

		/// <summary>Applies <paramref name="option"/>'s key across the checked rows as ONE undoable change.</summary>
		void Apply(int column, RegionChoiceOption option);

		// ----- Phase 2: Bulk Copy -----

		/// <summary>Every column as a copy SOURCE candidate (any readable column); empty when no overlay active.</summary>
		IReadOnlyList<BulkEditTarget> CopySourceColumns();

		/// <summary>The columns eligible as a copy TARGET (entry-anchored editable text); empty when none.</summary>
		IReadOnlyList<BulkEditTarget> CopyTargets();

		/// <summary>Previews the computed copied value into the target column across the checked rows (no mutation).</summary>
		void PreviewCopy(int sourceColumn, int targetColumn, BulkCopyMode mode);

		/// <summary>Applies the Bulk Copy across the checked rows as ONE undoable change.</summary>
		void ApplyCopy(int sourceColumn, int targetColumn, BulkCopyMode mode);

		// ----- Phase 3: Bulk Clear -----

		/// <summary>The columns eligible as a clear TARGET (entry-anchored editable text); empty when none.</summary>
		IReadOnlyList<BulkEditTarget> ClearTargets();

		/// <summary>Previews emptying the target column across the checked rows (no mutation).</summary>
		void PreviewClear(int targetColumn);

		/// <summary>Applies the Bulk Clear across the checked rows as ONE undoable change.</summary>
		void ApplyClear(int targetColumn);

		// ----- Find/Replace Phase 1: Bulk Replace -----

		/// <summary>The columns eligible as a replace TARGET (entry-anchored editable text); empty when none.</summary>
		IReadOnlyList<BulkEditTarget> ReplaceTargets();

		/// <summary>
		/// Opens the modal Find/Replace pattern-setup dialog seeded with <paramref name="current"/> and returns
		/// the edited spec, or null when the user cancels. The product host launches the dialog (translating to
		/// the dialog kit's FindReplacePattern); the bar never windows. Inert (returns null) when no overlay.
		/// </summary>
		BulkReplaceSpec ShowFindReplaceSetup(BulkReplaceSpec current);

		/// <summary>Previews the find/replace result into the target column across the checked rows (no mutation).</summary>
		void PreviewReplace(int targetColumn, BulkReplaceSpec spec);

		/// <summary>Applies the Bulk Replace across the checked rows as ONE undoable change.</summary>
		void ApplyReplace(int targetColumn, BulkReplaceSpec spec);

		// ----- Process / Transduce -----

		/// <summary>Every column as a transduce SOURCE candidate (any readable column); empty when no overlay active.</summary>
		IReadOnlyList<BulkEditTarget> TransduceSourceColumns();

		/// <summary>The columns eligible as a transduce TARGET (entry-anchored editable text); empty when none.</summary>
		IReadOnlyList<BulkEditTarget> TransduceColumns();

		/// <summary>The Unicode-to-Unicode converters available in the EncConverters pool; empty when none.</summary>
		IReadOnlyList<IBulkTransduceConverter> AvailableConverters();

		/// <summary>
		/// Opens the EncConverters management dialog ("Setup…") and returns the refreshed converter list (so a
		/// newly-added converter appears in the picker). Returns null when the launch is unavailable / cancelled.
		/// </summary>
		IReadOnlyList<IBulkTransduceConverter> LaunchConverterSetup();

		/// <summary>Previews the converted source value into the target column across the checked rows (no mutation).</summary>
		void PreviewTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter, BulkCopyMode mode);

		/// <summary>Applies the Bulk Transduce across the checked rows as ONE undoable change.</summary>
		void ApplyTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter, BulkCopyMode mode);

		// ----- Click Copy (interactive per-click copy of a clicked source cell into a target column) -----

		/// <summary>The columns eligible as a Click Copy TARGET (entry-anchored editable text); empty when none.</summary>
		IReadOnlyList<BulkEditTarget> ClickCopyTargets();

		/// <summary>
		/// Copies the text of the clicked SOURCE cell at (<paramref name="rowIndex"/>, <paramref name="sourceColumn"/>)
		/// into the Click Copy <paramref name="targetColumn"/> on the SAME row, per the <paramref name="mode"/>
		/// (word vs reorder/whole-field), joining the existing target value per <paramref name="append"/> (append
		/// with <paramref name="separator"/> vs overwrite). Commits as ONE undoable change — the per-click unit.
		/// Inert when no overlay is active or the target column is not a safe Click Copy target.
		/// </summary>
		void ApplyClickCopy(int sourceColumn, int targetColumn, int rowIndex, ClickCopyMode mode, string separator,
			bool append);

		// ----- Delete Rows (the destructive mode of the legacy Delete tab) -----

		/// <summary>
		/// Whether the host can offer the destructive Delete-Rows mode at all (the clerk-backed product edge
		/// can; an in-memory/test host without an LCModel cache reports false). The bar hides/disables the
		/// Delete-Rows mode when this is false so a non-deletable surface never shows the destructive option.
		/// </summary>
		bool CanDeleteRows { get; }

		/// <summary>
		/// Previews which CHECKED rows the Delete-Rows mode would delete vs block: stages a per-row delete-preview
		/// marking (deletable vs blocked by a guard such as the only-sense / ghost / bulkDeleteIfZero rule) over the
		/// checked rows, then refreshes so the table shows it. No model mutation. Returns the count that WOULD be
		/// deleted (the deletable, checked rows) so the bar can surface it; 0 when nothing is deletable.
		/// </summary>
		int PreviewDeleteRows();

		/// <summary>
		/// Deletes the checked, allowed objects as ONE undoable change after a confirmation: the host counts the
		/// deletable rows (applying the per-row guards), shows the confirmation dialog reporting that count
		/// (Cancel aborts and deletes nothing), then deletes them in one UOW, runs orphan cleanup, clears the
		/// delete preview, and refreshes the row set. A no-op when nothing is deletable or the user cancels.
		/// Returns the number of objects actually deleted (0 on cancel / nothing deletable).
		/// </summary>
		int ApplyDeleteRows();
	}

	/// <summary>
	/// View-model for the bulk-edit bar's List Choice tab (3c, Phase 1), MVVM and LCModel-free: it holds
	/// the selected target column and chosen option, exposes Apply enablement gated on a non-empty checked
	/// set, and routes Preview/Apply to the <see cref="IBulkEditBarHost"/> (the product edge over the
	/// owned table). Changing the target or the value CLEARS any pending preview — matching the legacy bar,
	/// where re-targeting or re-choosing invalidates the staged preview before it is applied.
	/// </summary>
	public sealed class BulkEditBarViewModel : INotifyPropertyChanged
	{
		private readonly IBulkEditBarHost _host;
		private BulkEditTarget _selectedTarget;
		private RegionChoiceOption _selectedOption;

		public BulkEditBarViewModel(IBulkEditBarHost host)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
			Targets = _host.ListChoiceTargets() ?? Array.Empty<BulkEditTarget>();
			_selectedTarget = Targets.FirstOrDefault();
			BulkCopy = new BulkCopyTabViewModel(host);
			BulkClear = new BulkClearTabViewModel(host);
			BulkReplace = new BulkReplaceTabViewModel(host);
			BulkTransduce = new BulkTransduceTabViewModel(host);
			ClickCopy = new BulkClickCopyTabViewModel(host);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>The Bulk Copy tab's view-model (Phase 2): source/target/mode + Preview/Apply routing.</summary>
		public BulkCopyTabViewModel BulkCopy { get; }

		/// <summary>The Bulk Clear tab's view-model (Phase 3): target + Preview/Apply routing.</summary>
		public BulkClearTabViewModel BulkClear { get; }

		/// <summary>The Bulk Replace tab's view-model (Find/Replace Phase 1): target + pattern + Preview/Apply routing.</summary>
		public BulkReplaceTabViewModel BulkReplace { get; }

		/// <summary>The Process/Transduce tab's view-model: source + converter + target + mode + Preview/Apply routing.</summary>
		public BulkTransduceTabViewModel BulkTransduce { get; }

		/// <summary>The Click Copy tab's view-model: target + mode + separator + append/overwrite + per-click copy.</summary>
		public BulkClickCopyTabViewModel ClickCopy { get; }

		/// <summary>The eligible target columns (snapshot at construction).</summary>
		public IReadOnlyList<BulkEditTarget> Targets { get; }

		/// <summary>The options for the selected target (empty when no target / no options).</summary>
		public IReadOnlyList<RegionChoiceOption> Options =>
			_selectedTarget == null ? Array.Empty<RegionChoiceOption>() : _host.OptionsFor(_selectedTarget.Column);

		/// <summary>
		/// The currently selected target column. Switching target clears any pending preview and resets the
		/// chosen value (the prior value belongs to the prior column's option set), then re-publishes the
		/// new column's options.
		/// </summary>
		public BulkEditTarget SelectedTarget
		{
			get => _selectedTarget;
			set
			{
				if (ReferenceEquals(_selectedTarget, value))
					return;
				_host.ClearPreview();
				_selectedTarget = value;
				_selectedOption = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Options));
				OnPropertyChanged(nameof(SelectedOption));
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>The chosen option to preview/apply. Re-choosing clears any pending preview.</summary>
		public RegionChoiceOption SelectedOption
		{
			get => _selectedOption;
			set
			{
				if (ReferenceEquals(_selectedOption, value))
					return;
				_host.ClearPreview();
				_selectedOption = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>
		/// Whether Preview/Apply may run: a target and a value are chosen AND at least one row is checked.
		/// The view re-reads this whenever the checked set may have changed (<see cref="RefreshEnablement"/>).
		/// </summary>
		public bool CanApply =>
			_selectedTarget != null && _selectedOption != null && _host.CheckedRowCount > 0;

		/// <summary>Re-publishes <see cref="CanApply"/> (both tabs) after the checked set changed outside the bar.</summary>
		public void RefreshEnablement()
		{
			OnPropertyChanged(nameof(CanApply));
			BulkCopy.RefreshEnablement();
			BulkClear.RefreshEnablement();
			BulkReplace.RefreshEnablement();
			BulkTransduce.RefreshEnablement();
		}

		/// <summary>Previews the chosen value across the checked rows (no-op when nothing is chosen).</summary>
		public void Preview()
		{
			if (_selectedTarget == null || _selectedOption == null)
				return;
			_host.Preview(_selectedTarget.Column, _selectedOption);
		}

		/// <summary>Applies the chosen value across the checked rows as one undoable change.</summary>
		public void Apply()
		{
			if (!CanApply)
				return;
			_host.Apply(_selectedTarget.Column, _selectedOption);
		}

		private void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// View-model for the bulk-edit bar's Bulk Copy tab (Phase 2), MVVM and LCModel-free: it holds the chosen
	/// SOURCE column (any column), the chosen TARGET column (the host's eligible writable subset), and the
	/// non-empty-target <see cref="BulkCopyMode"/>, exposes Apply enablement gated on a non-empty checked set
	/// AND source != target, and routes Preview/Apply to the <see cref="IBulkEditBarHost"/>. Any input change
	/// (source, target, or mode) CLEARS the pending preview — matching the legacy bar, where re-choosing
	/// invalidates the staged preview before it is applied.
	/// </summary>
	public sealed class BulkCopyTabViewModel : INotifyPropertyChanged
	{
		private readonly IBulkEditBarHost _host;
		private BulkEditTarget _selectedSource;
		private BulkEditTarget _selectedTarget;
		private BulkCopyMode _mode = BulkCopyMode.Append;

		public BulkCopyTabViewModel(IBulkEditBarHost host)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
			Sources = _host.CopySourceColumns() ?? Array.Empty<BulkEditTarget>();
			Targets = _host.CopyTargets() ?? Array.Empty<BulkEditTarget>();
			_selectedTarget = Targets.FirstOrDefault();
			// Seed the source to the first column that is NOT the target, so the initial pair is usable.
			_selectedSource = Sources.FirstOrDefault(s => _selectedTarget == null || s.Column != _selectedTarget.Column)
				?? Sources.FirstOrDefault();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Every column as a copy source candidate (snapshot at construction).</summary>
		public IReadOnlyList<BulkEditTarget> Sources { get; }

		/// <summary>The eligible writable target columns (snapshot at construction).</summary>
		public IReadOnlyList<BulkEditTarget> Targets { get; }

		/// <summary>The chosen source column (read-only cell read). Changing it clears any pending preview.</summary>
		public BulkEditTarget SelectedSource
		{
			get => _selectedSource;
			set
			{
				if (ReferenceEquals(_selectedSource, value))
					return;
				_host.ClearPreview();
				_selectedSource = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>The chosen writable target column. Changing it clears any pending preview.</summary>
		public BulkEditTarget SelectedTarget
		{
			get => _selectedTarget;
			set
			{
				if (ReferenceEquals(_selectedTarget, value))
					return;
				_host.ClearPreview();
				_selectedTarget = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>The non-empty-target mode (Append/Replace/DoNothingIfNonEmpty). Changing it clears the preview.</summary>
		public BulkCopyMode Mode
		{
			get => _mode;
			set
			{
				if (_mode == value)
					return;
				_host.ClearPreview();
				_mode = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Whether Preview/Apply may run: a source and a (distinct) target are chosen AND at least one row is
		/// checked. Source == target is disallowed (a self-copy is a no-op / nonsense). The view re-reads this
		/// whenever the checked set may have changed (<see cref="RefreshEnablement"/>).
		/// </summary>
		public bool CanApply =>
			_selectedSource != null && _selectedTarget != null
			&& _selectedSource.Column != _selectedTarget.Column
			&& _host.CheckedRowCount > 0;

		/// <summary>Re-publishes <see cref="CanApply"/> after the checked set changed outside the bar.</summary>
		public void RefreshEnablement() => OnPropertyChanged(nameof(CanApply));

		/// <summary>Previews the computed copied value into the target column across the checked rows.</summary>
		public void Preview()
		{
			if (!CanApply)
				return;
			_host.PreviewCopy(_selectedSource.Column, _selectedTarget.Column, _mode);
		}

		/// <summary>Applies the Bulk Copy across the checked rows as one undoable change.</summary>
		public void Apply()
		{
			if (!CanApply)
				return;
			_host.ApplyCopy(_selectedSource.Column, _selectedTarget.Column, _mode);
		}

		private void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// The two modes of the legacy bulk-edit Delete tab, mirroring BulkEditBar's dual-mode <c>m_deleteWhatCombo</c>:
	/// <see cref="ClearField"/> empties a target text column across the checked rows (the non-destructive half,
	/// Phase 3) and <see cref="DeleteRows"/> deletes the checked objects (the destructive half). Switching to
	/// Delete Rows swaps the tab's controls (a target column is irrelevant) and turns Apply into "Delete".
	/// </summary>
	public enum BulkDeleteMode
	{
		/// <summary>Empty a target text column across the checked rows (non-destructive).</summary>
		ClearField,
		/// <summary>Delete the checked objects (destructive — guarded + confirmed).</summary>
		DeleteRows
	}

	/// <summary>
	/// View-model for the bulk-edit bar's Delete tab, MVVM and LCModel-free. It is DUAL-MODE, mirroring the
	/// legacy bar's <c>m_deleteWhatCombo</c>: <see cref="Mode"/> <see cref="BulkDeleteMode.ClearField"/> empties a
	/// chosen TARGET column across the checked rows (the non-destructive half, Phase 3); <see cref="BulkDeleteMode.DeleteRows"/>
	/// deletes the checked OBJECTS (the destructive half). In Clear-Field mode it holds the chosen target and gates
	/// Apply on a non-empty checked set AND a chosen target, routing Preview/Apply to <see cref="IBulkEditBarHost.PreviewClear"/>/
	/// <see cref="IBulkEditBarHost.ApplyClear"/>. In Delete-Rows mode there is no target — Apply ("Delete") gates only
	/// on a non-empty checked set and the host being able to delete (<see cref="IBulkEditBarHost.CanDeleteRows"/>),
	/// and Preview/Apply route to <see cref="IBulkEditBarHost.PreviewDeleteRows"/>/<see cref="IBulkEditBarHost.ApplyDeleteRows"/>
	/// (the host applies the per-row guards, shows the confirmation, and deletes in one UOW). Changing the target or
	/// the mode CLEARS any pending preview — matching the legacy bar, where re-targeting/mode-switching invalidates
	/// the staged preview before it is applied.
	/// </summary>
	public sealed class BulkClearTabViewModel : INotifyPropertyChanged
	{
		private readonly IBulkEditBarHost _host;
		private BulkEditTarget _selectedTarget;
		private BulkDeleteMode _mode = BulkDeleteMode.ClearField;

		public BulkClearTabViewModel(IBulkEditBarHost host)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
			Targets = _host.ClearTargets() ?? Array.Empty<BulkEditTarget>();
			_selectedTarget = Targets.FirstOrDefault();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>The eligible writable target columns to clear (snapshot at construction).</summary>
		public IReadOnlyList<BulkEditTarget> Targets { get; }

		/// <summary>Whether the host can offer the destructive Delete-Rows mode at all (gates the mode toggle).</summary>
		public bool CanDeleteRows => _host.CanDeleteRows;

		/// <summary>
		/// The Delete tab's mode (Clear Field vs Delete Rows). Switching CLEARS any pending preview (the staged
		/// clear/delete overlay belongs to the prior mode) and re-publishes the mode-dependent state so the view
		/// can swap controls (the target combo is irrelevant in Delete-Rows mode) and re-caption Apply.
		/// </summary>
		public BulkDeleteMode Mode
		{
			get => _mode;
			set
			{
				if (_mode == value)
					return;
				_host.ClearPreview();
				_mode = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsDeleteRows));
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>Convenience: whether <see cref="Mode"/> is <see cref="BulkDeleteMode.DeleteRows"/> (the destructive mode).</summary>
		public bool IsDeleteRows => _mode == BulkDeleteMode.DeleteRows;

		/// <summary>The chosen target column to empty (Clear-Field mode). Changing it clears any pending preview.</summary>
		public BulkEditTarget SelectedTarget
		{
			get => _selectedTarget;
			set
			{
				if (ReferenceEquals(_selectedTarget, value))
					return;
				_host.ClearPreview();
				_selectedTarget = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>
		/// Whether Preview/Apply may run. Clear-Field mode: a target is chosen AND at least one row is checked.
		/// Delete-Rows mode: the host can delete AND at least one row is checked (no target needed). The view
		/// re-reads this whenever the checked set may have changed (<see cref="RefreshEnablement"/>).
		/// </summary>
		public bool CanApply => _mode == BulkDeleteMode.DeleteRows
			? _host.CanDeleteRows && _host.CheckedRowCount > 0
			: _selectedTarget != null && _host.CheckedRowCount > 0;

		/// <summary>Re-publishes <see cref="CanApply"/> after the checked set changed outside the bar.</summary>
		public void RefreshEnablement() => OnPropertyChanged(nameof(CanApply));

		/// <summary>
		/// Previews the staged operation across the checked rows: in Clear-Field mode, emptying the target column;
		/// in Delete-Rows mode, marking which checked rows would be deleted vs are blocked by a guard.
		/// </summary>
		public void Preview()
		{
			if (!CanApply)
				return;
			if (_mode == BulkDeleteMode.DeleteRows)
				_host.PreviewDeleteRows();
			else
				_host.PreviewClear(_selectedTarget.Column);
		}

		/// <summary>
		/// Applies the staged operation across the checked rows as one undoable change: in Clear-Field mode,
		/// emptying the target column; in Delete-Rows mode, deleting the checked, allowed objects after a
		/// confirmation (the host owns the guard + confirmation + one-UOW delete + orphan cleanup).
		/// </summary>
		public void Apply()
		{
			if (!CanApply)
				return;
			if (_mode == BulkDeleteMode.DeleteRows)
				_host.ApplyDeleteRows();
			else
				_host.ApplyClear(_selectedTarget.Column);
		}

		private void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// View-model for the bulk-edit bar's Bulk Replace tab (Find/Replace Phase 1), MVVM and LCModel-free: it
	/// holds the chosen TARGET column (the host's eligible writable text subset) and the last-edited
	/// <see cref="BulkReplaceSpec"/> (authored through the modal Find/Replace pattern dialog, opened via
	/// <c>Setup…</c>). It exposes Apply enablement gated on a non-empty checked set AND a chosen target AND a
	/// non-empty find text, and routes Preview/Apply to the <see cref="IBulkEditBarHost"/>. Changing the target
	/// or re-running Setup CLEARS any pending preview — matching the legacy bar, where re-targeting/re-spec'ing
	/// invalidates the staged preview before it is applied. There is no find engine here (the actual replace
	/// reuses the legacy ReplaceWithMethod semantics in the producer); this tab only edits the PATTERN.
	/// </summary>
	public sealed class BulkReplaceTabViewModel : INotifyPropertyChanged
	{
		private readonly IBulkEditBarHost _host;
		private BulkEditTarget _selectedTarget;
		private BulkReplaceSpec _spec = new BulkReplaceSpec();

		public BulkReplaceTabViewModel(IBulkEditBarHost host)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
			Targets = _host.ReplaceTargets() ?? Array.Empty<BulkEditTarget>();
			_selectedTarget = Targets.FirstOrDefault();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>The eligible writable target columns to replace within (snapshot at construction).</summary>
		public IReadOnlyList<BulkEditTarget> Targets { get; }

		/// <summary>The chosen target column. Changing it clears any pending preview.</summary>
		public BulkEditTarget SelectedTarget
		{
			get => _selectedTarget;
			set
			{
				if (ReferenceEquals(_selectedTarget, value))
					return;
				_host.ClearPreview();
				_selectedTarget = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>The last-edited Find/Replace spec (find/replace text + match options); never null.</summary>
		public BulkReplaceSpec Spec => _spec;

		/// <summary>A one-line summary of the current pattern for the bar (e.g. "find → replace"); empty when no find text.</summary>
		public string Summary => string.IsNullOrEmpty(_spec.FindText)
			? FwAvaloniaStrings.BulkReplaceNoPattern
			: $"“{_spec.FindText}” → “{_spec.ReplaceText ?? string.Empty}”";

		/// <summary>
		/// Opens the modal Find/Replace pattern dialog through the host (seeded with the current spec) and, when
		/// the user accepts, records the edited spec — clearing any pending preview and re-publishing the summary
		/// and Apply enablement. A cancel leaves the spec untouched.
		/// </summary>
		public void Setup()
		{
			var edited = _host.ShowFindReplaceSetup(_spec);
			if (edited == null)
				return;
			_host.ClearPreview();
			_spec = edited;
			OnPropertyChanged(nameof(Spec));
			OnPropertyChanged(nameof(Summary));
			OnPropertyChanged(nameof(CanApply));
		}

		/// <summary>
		/// Whether Preview/Apply may run: a target is chosen, the find text is non-empty, AND at least one row is
		/// checked. The view re-reads this whenever the checked set may have changed (<see cref="RefreshEnablement"/>).
		/// </summary>
		public bool CanApply =>
			_selectedTarget != null && !string.IsNullOrEmpty(_spec.FindText) && _host.CheckedRowCount > 0;

		/// <summary>Re-publishes <see cref="CanApply"/> after the checked set changed outside the bar.</summary>
		public void RefreshEnablement() => OnPropertyChanged(nameof(CanApply));

		/// <summary>Previews the find/replace result into the target column across the checked rows.</summary>
		public void Preview()
		{
			if (!CanApply)
				return;
			_host.PreviewReplace(_selectedTarget.Column, _spec);
		}

		/// <summary>Applies the Bulk Replace across the checked rows as one undoable change.</summary>
		public void Apply()
		{
			if (!CanApply)
				return;
			_host.ApplyReplace(_selectedTarget.Column, _spec);
		}

		private void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// View-model for the bulk-edit bar's Process/Transduce tab, MVVM and LCModel-free: it holds the chosen
	/// SOURCE column (any column), the chosen CONVERTER (an <see cref="IBulkTransduceConverter"/> from the
	/// host's EncConverters pool), the chosen TARGET column (the host's eligible writable subset), and the
	/// non-empty-target <see cref="BulkCopyMode"/> (the SAME Append/Replace/Skip-non-empty semantics Bulk Copy
	/// reuses). It exposes Apply enablement gated on a non-empty checked set AND a chosen converter AND source
	/// != target, and routes Preview/Apply to the <see cref="IBulkEditBarHost"/>. Any input change (source,
	/// converter, target, or mode) CLEARS the pending preview — matching the legacy bar, where re-choosing
	/// invalidates the staged preview before it is applied. <see cref="Setup"/> launches the EncConverters
	/// management dialog through the host and re-publishes the refreshed converter list.
	/// </summary>
	public sealed class BulkTransduceTabViewModel : INotifyPropertyChanged
	{
		private readonly IBulkEditBarHost _host;
		private BulkEditTarget _selectedSource;
		private BulkEditTarget _selectedTarget;
		private IBulkTransduceConverter _selectedConverter;
		private IReadOnlyList<IBulkTransduceConverter> _converters;
		private BulkCopyMode _mode = BulkCopyMode.Append;

		public BulkTransduceTabViewModel(IBulkEditBarHost host)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
			Sources = _host.TransduceSourceColumns() ?? Array.Empty<BulkEditTarget>();
			Targets = _host.TransduceColumns() ?? Array.Empty<BulkEditTarget>();
			_converters = _host.AvailableConverters() ?? Array.Empty<IBulkTransduceConverter>();
			_selectedTarget = Targets.FirstOrDefault();
			// Seed the source to the first column that is NOT the target, so the initial pair is usable.
			_selectedSource = Sources.FirstOrDefault(s => _selectedTarget == null || s.Column != _selectedTarget.Column)
				?? Sources.FirstOrDefault();
			_selectedConverter = _converters.FirstOrDefault();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Every column as a transduce source candidate (snapshot at construction).</summary>
		public IReadOnlyList<BulkEditTarget> Sources { get; }

		/// <summary>The eligible writable target columns (snapshot at construction).</summary>
		public IReadOnlyList<BulkEditTarget> Targets { get; }

		/// <summary>The available converters (Unicode-to-Unicode); re-published after <see cref="Setup"/>.</summary>
		public IReadOnlyList<IBulkTransduceConverter> Converters => _converters;

		/// <summary>The chosen source column (read-only cell read). Changing it clears any pending preview.</summary>
		public BulkEditTarget SelectedSource
		{
			get => _selectedSource;
			set
			{
				if (ReferenceEquals(_selectedSource, value))
					return;
				_host.ClearPreview();
				_selectedSource = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>The chosen writable target column. Changing it clears any pending preview.</summary>
		public BulkEditTarget SelectedTarget
		{
			get => _selectedTarget;
			set
			{
				if (ReferenceEquals(_selectedTarget, value))
					return;
				_host.ClearPreview();
				_selectedTarget = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>The chosen converter. Changing it clears any pending preview.</summary>
		public IBulkTransduceConverter SelectedConverter
		{
			get => _selectedConverter;
			set
			{
				if (ReferenceEquals(_selectedConverter, value))
					return;
				_host.ClearPreview();
				_selectedConverter = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CanApply));
			}
		}

		/// <summary>The non-empty-target mode (Append/Replace/DoNothingIfNonEmpty). Changing it clears the preview.</summary>
		public BulkCopyMode Mode
		{
			get => _mode;
			set
			{
				if (_mode == value)
					return;
				_host.ClearPreview();
				_mode = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Launches the EncConverters management dialog through the host and, when it returns a (refreshed)
		/// converter list, re-publishes it — preserving the current selection by name when it still exists,
		/// else seeding the first converter. Clears any pending preview. A null result (unavailable/cancel)
		/// leaves the list untouched.
		/// </summary>
		public void Setup()
		{
			var refreshed = _host.LaunchConverterSetup();
			if (refreshed == null)
				return;
			_host.ClearPreview();
			_converters = refreshed;
			var priorName = _selectedConverter?.Name;
			_selectedConverter = _converters.FirstOrDefault(c => c.Name == priorName) ?? _converters.FirstOrDefault();
			OnPropertyChanged(nameof(Converters));
			OnPropertyChanged(nameof(SelectedConverter));
			OnPropertyChanged(nameof(CanApply));
		}

		/// <summary>
		/// Whether Preview/Apply may run: a source, a converter, and a (distinct) target are chosen AND at least
		/// one row is checked. Source == target is disallowed (a self-transduce-over-different-column is the
		/// point; writing back onto the source is a no-op / nonsense). The view re-reads this whenever the
		/// checked set may have changed (<see cref="RefreshEnablement"/>).
		/// </summary>
		public bool CanApply =>
			_selectedSource != null && _selectedTarget != null && _selectedConverter != null
			&& _selectedSource.Column != _selectedTarget.Column
			&& _host.CheckedRowCount > 0;

		/// <summary>Re-publishes <see cref="CanApply"/> after the checked set changed outside the bar.</summary>
		public void RefreshEnablement() => OnPropertyChanged(nameof(CanApply));

		/// <summary>Previews the converted source value into the target column across the checked rows.</summary>
		public void Preview()
		{
			if (!CanApply)
				return;
			_host.PreviewTransduce(_selectedSource.Column, _selectedTarget.Column, _selectedConverter, _mode);
		}

		/// <summary>Applies the Bulk Transduce across the checked rows as one undoable change.</summary>
		public void Apply()
		{
			if (!CanApply)
				return;
			_host.ApplyTransduce(_selectedSource.Column, _selectedTarget.Column, _selectedConverter, _mode);
		}

		private void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// View-model for the bulk-edit bar's Click Copy tab, MVVM and LCModel-free. Unlike the other tabs there is
	/// NO Preview/Apply button: with this tab active the user CLICKS a source cell in the browse table and that
	/// text is copied into the chosen TARGET column on the SAME row, per click. So the VM just holds the
	/// interaction settings — the TARGET column (the host's eligible writable text subset), the word-vs-reorder
	/// <see cref="ClickCopyMode"/>, the <see cref="Separator"/> text, and the append-vs-overwrite directivity —
	/// and routes each click through to <see cref="IBulkEditBarHost.ApplyClickCopy"/>. The owned table consults
	/// <see cref="IsActive"/> to decide whether a data-cell click is a click-copy gesture; the host wires the
	/// table's cell-click signal to <see cref="Copy"/>. Mirrors the legacy bar's click-copy tab (target combo,
	/// word/reorder radios, separator box, append/overwrite radios — <c>BulkEditBar.InitClickCopyTab</c>).
	/// </summary>
	public sealed class BulkClickCopyTabViewModel : INotifyPropertyChanged
	{
		private readonly IBulkEditBarHost _host;
		private BulkEditTarget _selectedTarget;
		private ClickCopyMode _mode = ClickCopyMode.Word;
		private string _separator = " ";
		private bool _append = true;
		private bool _isActive;

		public BulkClickCopyTabViewModel(IBulkEditBarHost host)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
			Targets = _host.ClickCopyTargets() ?? Array.Empty<BulkEditTarget>();
			_selectedTarget = Targets.FirstOrDefault();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>The eligible writable target columns a click copies into (snapshot at construction).</summary>
		public IReadOnlyList<BulkEditTarget> Targets { get; }

		/// <summary>The chosen target column a clicked source cell copies into.</summary>
		public BulkEditTarget SelectedTarget
		{
			get => _selectedTarget;
			set
			{
				if (ReferenceEquals(_selectedTarget, value))
					return;
				_selectedTarget = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsReady));
			}
		}

		/// <summary>The word-vs-reorder granularity of the copy (defaults to Word, matching the legacy bar).</summary>
		public ClickCopyMode Mode
		{
			get => _mode;
			set
			{
				if (_mode == value)
					return;
				_mode = value;
				OnPropertyChanged();
			}
		}

		/// <summary>The text inserted between a non-empty target and the appended source (append mode only).</summary>
		public string Separator
		{
			get => _separator;
			set
			{
				var v = value ?? string.Empty;
				if (_separator == v)
					return;
				_separator = v;
				OnPropertyChanged();
			}
		}

		/// <summary>Whether a copy APPENDS to a non-empty target (true, with the separator) or OVERWRITES it (false).</summary>
		public bool Append
		{
			get => _append;
			set
			{
				if (_append == value)
					return;
				_append = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Whether the Click Copy tab is the active interaction mode — the owned table reads this to decide
		/// whether a data-cell click is a click-copy gesture (vs normal selection/editing). Set by the view when
		/// the tab is selected/deselected.
		/// </summary>
		public bool IsActive
		{
			get => _isActive;
			set
			{
				if (_isActive == value)
					return;
				_isActive = value;
				OnPropertyChanged();
			}
		}

		/// <summary>Whether a click can copy: a target column is chosen (the per-click write needs one).</summary>
		public bool IsReady => _selectedTarget != null;

		/// <summary>
		/// Handles one click-copy gesture: copies the clicked SOURCE cell at
		/// (<paramref name="rowIndex"/>, <paramref name="sourceColumn"/>) into the chosen target column on the
		/// SAME row, honoring the mode + separator + append/overwrite. A no-op when no target is chosen or the
		/// click hit the target column itself (self-copy).
		/// </summary>
		public void Copy(int rowIndex, int sourceColumn)
		{
			if (_selectedTarget == null || sourceColumn == _selectedTarget.Column)
				return;
			_host.ApplyClickCopy(sourceColumn, _selectedTarget.Column, rowIndex, _mode, _separator, _append);
		}

		private void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// The docked bulk-edit bar (3c): a compact <see cref="TabControl"/> with a List Choice tab (Phase 1) and
	/// a Bulk Copy tab (Phase 2). List Choice has a target-column dropdown (only the columns the host reports
	/// as eligible list-choice targets), an <see cref="FwOptionPicker"/> value selector, and Preview/Apply.
	/// Bulk Copy has a source-column dropdown (any column), a target-column dropdown (eligible writable text
	/// targets), an Append/Replace/DoNothingIfNonEmpty mode radio, and Preview/Apply. Each tab disables Apply
	/// when nothing is checked / chosen (Bulk Copy also when source == target). Follows the established
	/// native-composite region pattern (like <see cref="LexicalBrowseView"/>/<see cref="FwOptionPicker"/>): no
	/// XAML, bindings in code, stable automation ids on every interactive control, compact density tokens.
	/// </summary>
	public sealed class BulkEditBarView : UserControl
	{
		private readonly BulkEditBarViewModel _vm;
		private readonly ComboBox _targetCombo;
		private readonly Button _valueButton;
		private readonly Button _previewButton;
		private readonly Button _applyButton;
		private Flyout _valueFlyout;

		// Phase 2 (Bulk Copy) controls.
		private readonly ComboBox _copySourceCombo;
		private readonly ComboBox _copyTargetCombo;
		private readonly RadioButton _copyAppend;
		private readonly RadioButton _copyReplace;
		private readonly RadioButton _copyDoNothing;
		private readonly Button _copyPreviewButton;
		private readonly Button _copyApplyButton;

		// Phase 3 (Bulk Clear) + Delete Rows (destructive) controls — the dual-mode Delete tab.
		private readonly ComboBox _deleteWhatCombo;
		private readonly TextBlock _clearTargetLabel;
		private readonly ComboBox _clearTargetCombo;
		private readonly Button _clearPreviewButton;
		private readonly Button _clearApplyButton;

		// Find/Replace Phase 1 (Bulk Replace) controls.
		private readonly ComboBox _replaceTargetCombo;
		private readonly TextBlock _replaceSummary;
		private readonly Button _replaceSetupButton;
		private readonly Button _replacePreviewButton;
		private readonly Button _replaceApplyButton;

		// Process / Transduce controls.
		private readonly ComboBox _transduceSourceCombo;
		private readonly ComboBox _transduceConverterCombo;
		private readonly ComboBox _transduceTargetCombo;
		private readonly RadioButton _transduceAppend;
		private readonly RadioButton _transduceReplace;
		private readonly RadioButton _transduceDoNothing;
		private readonly Button _transduceSetupButton;
		private readonly Button _transducePreviewButton;
		private readonly Button _transduceApplyButton;

		// Click Copy controls.
		private readonly ComboBox _clickCopyTargetCombo;
		private readonly RadioButton _clickCopyWord;
		private readonly RadioButton _clickCopyReorder;
		private readonly TextBox _clickCopySeparator;
		private readonly RadioButton _clickCopyAppend;
		private readonly RadioButton _clickCopyOverwrite;
		private readonly TabControl _tabs;
		private readonly TabItem _clickCopyTab;

		/// <summary>
		/// Raised when the Click Copy tab becomes (true) or stops being (false) the selected tab. The host wires
		/// this to the owned table's click-copy mode so a data-cell click is a copy gesture ONLY while the tab is
		/// active — and does not interfere with normal selection/editing otherwise.
		/// </summary>
		public event EventHandler<bool> ClickCopyActiveChanged;

		public BulkEditBarView(BulkEditBarViewModel viewModel)
		{
			_vm = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
			Name = "BulkEditBarView";
			AutomationProperties.SetAutomationId(this, "BulkEditBar");
			AutomationProperties.SetName(this, FwAvaloniaStrings.BulkEditBarName);

			// Apply the shared WinForms-density baseline (font + compact mode-tabs) so the bar's TabControl
			// uses a single compact tab row instead of Fluent's large, wrapping tab headers.
			FwSurfaceStyles.Apply(this);

			var targetLabel = new TextBlock
			{
				Text = FwAvaloniaStrings.BulkEditTarget,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 4, 0)
			};

			_targetCombo = new ComboBox
			{
				ItemsSource = _vm.Targets,
				SelectedItem = _vm.SelectedTarget,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 140
			};
			AutomationProperties.SetAutomationId(_targetCombo, "BulkEditBar.Target");
			AutomationProperties.SetName(_targetCombo, FwAvaloniaStrings.BulkEditTarget);
			_targetCombo.SelectionChanged += (_, __) =>
			{
				_vm.SelectedTarget = _targetCombo.SelectedItem as BulkEditTarget;
				RebuildValueFlyout();
				UpdateValueButtonText();
			};

			// The value selector reuses the shared FwOptionPicker (the one compact, filterable list control
			// for every option surface): a button labelled with the chosen option opens a flyout hosting the
			// picker over the target column's options.
			_valueButton = new Button
			{
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 140,
				HorizontalContentAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_valueButton, "BulkEditBar.Value");
			UpdateValueButtonText();

			_previewButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditPreview,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_previewButton, "BulkEditBar.Preview");
			AutomationProperties.SetName(_previewButton, FwAvaloniaStrings.BulkEditPreview);
			_previewButton.Click += (_, __) => _vm.Preview();

			_applyButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditApply,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(4, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_applyButton, "BulkEditBar.Apply");
			AutomationProperties.SetName(_applyButton, FwAvaloniaStrings.BulkEditApply);
			_applyButton.Click += (_, __) => _vm.Apply();

			RebuildValueFlyout();

			var listChoiceRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 2, FwAvaloniaDensity.EditorPadding.Right, 2),
				Children = { targetLabel, _targetCombo, _valueButton, _previewButton, _applyButton }
			};

			// ----- Phase 2: Bulk Copy tab -----
			var copyVm = _vm.BulkCopy;

			_copySourceCombo = new ComboBox
			{
				ItemsSource = copyVm.Sources,
				SelectedItem = copyVm.SelectedSource,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 140
			};
			AutomationProperties.SetAutomationId(_copySourceCombo, "BulkEditBar.CopySource");
			AutomationProperties.SetName(_copySourceCombo, FwAvaloniaStrings.BulkCopySource);
			_copySourceCombo.SelectionChanged += (_, __) =>
				copyVm.SelectedSource = _copySourceCombo.SelectedItem as BulkEditTarget;

			_copyTargetCombo = new ComboBox
			{
				ItemsSource = copyVm.Targets,
				SelectedItem = copyVm.SelectedTarget,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 140,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_copyTargetCombo, "BulkEditBar.CopyTarget");
			AutomationProperties.SetName(_copyTargetCombo, FwAvaloniaStrings.BulkEditTarget);
			_copyTargetCombo.SelectionChanged += (_, __) =>
				copyVm.SelectedTarget = _copyTargetCombo.SelectedItem as BulkEditTarget;

			_copyAppend = MakeModeRadio(FwAvaloniaStrings.BulkCopyAppend, "BulkEditBar.CopyMode.Append",
				BulkCopyMode.Append, copyVm);
			_copyAppend.IsChecked = true;
			_copyReplace = MakeModeRadio(FwAvaloniaStrings.BulkCopyReplace, "BulkEditBar.CopyMode.Replace",
				BulkCopyMode.Replace, copyVm);
			_copyDoNothing = MakeModeRadio(FwAvaloniaStrings.BulkCopyDoNothing, "BulkEditBar.CopyMode.DoNothing",
				BulkCopyMode.DoNothingIfNonEmpty, copyVm);

			_copyPreviewButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditPreview,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_copyPreviewButton, "BulkEditBar.CopyPreview");
			AutomationProperties.SetName(_copyPreviewButton, FwAvaloniaStrings.BulkEditPreview);
			_copyPreviewButton.Click += (_, __) => copyVm.Preview();

			_copyApplyButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditApply,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(4, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_copyApplyButton, "BulkEditBar.CopyApply");
			AutomationProperties.SetName(_copyApplyButton, FwAvaloniaStrings.BulkEditApply);
			_copyApplyButton.Click += (_, __) => copyVm.Apply();

			var copyRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 2, FwAvaloniaDensity.EditorPadding.Right, 2),
				Children =
				{
					new TextBlock { Text = FwAvaloniaStrings.BulkCopySource, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) },
					_copySourceCombo,
					new TextBlock { Text = FwAvaloniaStrings.BulkEditTarget, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 4, 0) },
					_copyTargetCombo,
					_copyAppend, _copyReplace, _copyDoNothing,
					_copyPreviewButton, _copyApplyButton
				}
			};

			// ----- Phase 3 / Delete Rows: the dual-mode Delete tab -----
			var clearVm = _vm.BulkClear;

			// The "Delete what?" mode toggle, mirroring the legacy m_deleteWhatCombo: Clear Field (non-destructive)
			// vs Delete Rows (destructive). Delete Rows is only offered when the host can delete (CanDeleteRows).
			var deleteModeItems = new List<BulkDeleteMode> { BulkDeleteMode.ClearField };
			if (clearVm.CanDeleteRows)
				deleteModeItems.Add(BulkDeleteMode.DeleteRows);
			_deleteWhatCombo = new ComboBox
			{
				ItemsSource = deleteModeItems,
				SelectedItem = clearVm.Mode,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 120,
				Margin = new Thickness(0, 0, 8, 0)
			};
			// Render the enum as the localized mode label rather than the raw enum name.
			_deleteWhatCombo.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<BulkDeleteMode>(
				(mode, _) => new TextBlock
				{
					Text = mode == BulkDeleteMode.DeleteRows
						? FwAvaloniaStrings.BulkDeleteModeDeleteRows
						: FwAvaloniaStrings.BulkDeleteModeClearField,
					VerticalAlignment = VerticalAlignment.Center
				});
			AutomationProperties.SetAutomationId(_deleteWhatCombo, "BulkEditBar.DeleteWhat");
			AutomationProperties.SetName(_deleteWhatCombo, FwAvaloniaStrings.BulkDeleteWhatLabel);
			_deleteWhatCombo.SelectionChanged += (_, __) =>
			{
				if (_deleteWhatCombo.SelectedItem is BulkDeleteMode mode)
					clearVm.Mode = mode;
				UpdateDeleteModeUi();
			};

			_clearTargetLabel = new TextBlock
			{
				Text = FwAvaloniaStrings.BulkEditTarget,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 4, 0)
			};

			_clearTargetCombo = new ComboBox
			{
				ItemsSource = clearVm.Targets,
				SelectedItem = clearVm.SelectedTarget,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 140
			};
			AutomationProperties.SetAutomationId(_clearTargetCombo, "BulkEditBar.ClearTarget");
			AutomationProperties.SetName(_clearTargetCombo, FwAvaloniaStrings.BulkEditTarget);
			_clearTargetCombo.SelectionChanged += (_, __) =>
				clearVm.SelectedTarget = _clearTargetCombo.SelectedItem as BulkEditTarget;

			_clearPreviewButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditPreview,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_clearPreviewButton, "BulkEditBar.ClearPreview");
			AutomationProperties.SetName(_clearPreviewButton, FwAvaloniaStrings.BulkEditPreview);
			_clearPreviewButton.Click += (_, __) => clearVm.Preview();

			_clearApplyButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditApply,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(4, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_clearApplyButton, "BulkEditBar.ClearApply");
			AutomationProperties.SetName(_clearApplyButton, FwAvaloniaStrings.BulkEditApply);
			_clearApplyButton.Click += (_, __) => clearVm.Apply();

			var clearRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 2, FwAvaloniaDensity.EditorPadding.Right, 2),
				Children =
				{
					new TextBlock { Text = FwAvaloniaStrings.BulkDeleteWhatLabel, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) },
					_deleteWhatCombo,
					_clearTargetLabel,
					_clearTargetCombo,
					_clearPreviewButton, _clearApplyButton
				}
			};
			UpdateDeleteModeUi();

			// ----- Find/Replace Phase 1: Bulk Replace tab -----
			var replaceVm = _vm.BulkReplace;

			_replaceTargetCombo = new ComboBox
			{
				ItemsSource = replaceVm.Targets,
				SelectedItem = replaceVm.SelectedTarget,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 140
			};
			AutomationProperties.SetAutomationId(_replaceTargetCombo, "BulkEditBar.ReplaceTarget");
			AutomationProperties.SetName(_replaceTargetCombo, FwAvaloniaStrings.BulkEditTarget);
			_replaceTargetCombo.SelectionChanged += (_, __) =>
				replaceVm.SelectedTarget = _replaceTargetCombo.SelectedItem as BulkEditTarget;

			_replaceSetupButton = new Button
			{
				Content = FwAvaloniaStrings.BulkReplaceSetup,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_replaceSetupButton, "BulkEditBar.ReplaceSetup");
			AutomationProperties.SetName(_replaceSetupButton, FwAvaloniaStrings.BulkReplaceSetup);
			_replaceSetupButton.Click += (_, __) =>
			{
				replaceVm.Setup();
				UpdateReplaceSummary();
			};

			_replaceSummary = new TextBlock
			{
				Text = replaceVm.Summary,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_replaceSummary, "BulkEditBar.ReplaceSummary");

			_replacePreviewButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditPreview,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_replacePreviewButton, "BulkEditBar.ReplacePreview");
			AutomationProperties.SetName(_replacePreviewButton, FwAvaloniaStrings.BulkEditPreview);
			_replacePreviewButton.Click += (_, __) => replaceVm.Preview();

			_replaceApplyButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditApply,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(4, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_replaceApplyButton, "BulkEditBar.ReplaceApply");
			AutomationProperties.SetName(_replaceApplyButton, FwAvaloniaStrings.BulkEditApply);
			_replaceApplyButton.Click += (_, __) => replaceVm.Apply();

			var replaceRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 2, FwAvaloniaDensity.EditorPadding.Right, 2),
				Children =
				{
					new TextBlock { Text = FwAvaloniaStrings.BulkEditTarget, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) },
					_replaceTargetCombo,
					_replaceSetupButton,
					_replaceSummary,
					_replacePreviewButton, _replaceApplyButton
				}
			};

			// ----- Process / Transduce tab -----
			var transduceVm = _vm.BulkTransduce;

			_transduceSourceCombo = new ComboBox
			{
				ItemsSource = transduceVm.Sources,
				SelectedItem = transduceVm.SelectedSource,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 140
			};
			AutomationProperties.SetAutomationId(_transduceSourceCombo, "BulkEditBar.TransduceSource");
			AutomationProperties.SetName(_transduceSourceCombo, FwAvaloniaStrings.BulkCopySource);
			_transduceSourceCombo.SelectionChanged += (_, __) =>
				transduceVm.SelectedSource = _transduceSourceCombo.SelectedItem as BulkEditTarget;

			_transduceConverterCombo = new ComboBox
			{
				ItemsSource = transduceVm.Converters,
				SelectedItem = transduceVm.SelectedConverter,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 160,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_transduceConverterCombo, "BulkEditBar.TransduceConverter");
			AutomationProperties.SetName(_transduceConverterCombo, FwAvaloniaStrings.BulkTransduceConverter);
			_transduceConverterCombo.SelectionChanged += (_, __) =>
				transduceVm.SelectedConverter = _transduceConverterCombo.SelectedItem as IBulkTransduceConverter;

			_transduceSetupButton = new Button
			{
				Content = FwAvaloniaStrings.BulkTransduceSetup,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(4, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_transduceSetupButton, "BulkEditBar.TransduceSetup");
			AutomationProperties.SetName(_transduceSetupButton, FwAvaloniaStrings.BulkTransduceSetup);
			_transduceSetupButton.Click += (_, __) =>
			{
				transduceVm.Setup();
				// The Setup result is a (possibly new) converter list; rebind the combo to the refreshed set.
				_transduceConverterCombo.ItemsSource = transduceVm.Converters;
				_transduceConverterCombo.SelectedItem = transduceVm.SelectedConverter;
			};

			_transduceTargetCombo = new ComboBox
			{
				ItemsSource = transduceVm.Targets,
				SelectedItem = transduceVm.SelectedTarget,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 140,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_transduceTargetCombo, "BulkEditBar.TransduceTarget");
			AutomationProperties.SetName(_transduceTargetCombo, FwAvaloniaStrings.BulkEditTarget);
			_transduceTargetCombo.SelectionChanged += (_, __) =>
				transduceVm.SelectedTarget = _transduceTargetCombo.SelectedItem as BulkEditTarget;

			_transduceAppend = MakeTransduceModeRadio(FwAvaloniaStrings.BulkCopyAppend, "BulkEditBar.TransduceMode.Append",
				BulkCopyMode.Append, transduceVm);
			_transduceAppend.IsChecked = true;
			_transduceReplace = MakeTransduceModeRadio(FwAvaloniaStrings.BulkCopyReplace, "BulkEditBar.TransduceMode.Replace",
				BulkCopyMode.Replace, transduceVm);
			_transduceDoNothing = MakeTransduceModeRadio(FwAvaloniaStrings.BulkCopyDoNothing, "BulkEditBar.TransduceMode.DoNothing",
				BulkCopyMode.DoNothingIfNonEmpty, transduceVm);

			_transducePreviewButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditPreview,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_transducePreviewButton, "BulkEditBar.TransducePreview");
			AutomationProperties.SetName(_transducePreviewButton, FwAvaloniaStrings.BulkEditPreview);
			_transducePreviewButton.Click += (_, __) => transduceVm.Preview();

			_transduceApplyButton = new Button
			{
				Content = FwAvaloniaStrings.BulkEditApply,
				MinHeight = 0,
				Padding = new Thickness(10, 2, 10, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(4, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_transduceApplyButton, "BulkEditBar.TransduceApply");
			AutomationProperties.SetName(_transduceApplyButton, FwAvaloniaStrings.BulkEditApply);
			_transduceApplyButton.Click += (_, __) => transduceVm.Apply();

			var transduceRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 2, FwAvaloniaDensity.EditorPadding.Right, 2),
				Children =
				{
					new TextBlock { Text = FwAvaloniaStrings.BulkCopySource, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) },
					_transduceSourceCombo,
					new TextBlock { Text = FwAvaloniaStrings.BulkTransduceConverter, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 4, 0) },
					_transduceConverterCombo,
					_transduceSetupButton,
					new TextBlock { Text = FwAvaloniaStrings.BulkEditTarget, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 4, 0) },
					_transduceTargetCombo,
					_transduceAppend, _transduceReplace, _transduceDoNothing,
					_transducePreviewButton, _transduceApplyButton
				}
			};

			// ----- Click Copy tab -----
			var clickCopyVm = _vm.ClickCopy;

			_clickCopyTargetCombo = new ComboBox
			{
				ItemsSource = clickCopyVm.Targets,
				SelectedItem = clickCopyVm.SelectedTarget,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 140
			};
			AutomationProperties.SetAutomationId(_clickCopyTargetCombo, "BulkEditBar.ClickCopyTarget");
			AutomationProperties.SetName(_clickCopyTargetCombo, FwAvaloniaStrings.BulkEditTarget);
			_clickCopyTargetCombo.SelectionChanged += (_, __) =>
				clickCopyVm.SelectedTarget = _clickCopyTargetCombo.SelectedItem as BulkEditTarget;

			_clickCopyWord = MakeClickCopyModeRadio(FwAvaloniaStrings.BulkClickCopyWord,
				"BulkEditBar.ClickCopyMode.Word", ClickCopyMode.Word, clickCopyVm);
			_clickCopyWord.IsChecked = true;
			_clickCopyReorder = MakeClickCopyModeRadio(FwAvaloniaStrings.BulkClickCopyReorder,
				"BulkEditBar.ClickCopyMode.Reorder", ClickCopyMode.Reorder, clickCopyVm);

			_clickCopyAppend = new RadioButton
			{
				Content = FwAvaloniaStrings.BulkClickCopyAppend,
				GroupName = "BulkEditBar.ClickCopyDirectivity",
				MinHeight = 0,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0),
				IsChecked = true
			};
			AutomationProperties.SetAutomationId(_clickCopyAppend, "BulkEditBar.ClickCopyAppend");
			AutomationProperties.SetName(_clickCopyAppend, FwAvaloniaStrings.BulkClickCopyAppend);
			_clickCopyAppend.IsCheckedChanged += (_, __) =>
			{
				if (_clickCopyAppend.IsChecked == true)
					clickCopyVm.Append = true;
			};

			_clickCopyOverwrite = new RadioButton
			{
				Content = FwAvaloniaStrings.BulkClickCopyOverwrite,
				GroupName = "BulkEditBar.ClickCopyDirectivity",
				MinHeight = 0,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_clickCopyOverwrite, "BulkEditBar.ClickCopyOverwrite");
			AutomationProperties.SetName(_clickCopyOverwrite, FwAvaloniaStrings.BulkClickCopyOverwrite);
			_clickCopyOverwrite.IsCheckedChanged += (_, __) =>
			{
				if (_clickCopyOverwrite.IsChecked == true)
					clickCopyVm.Append = false;
			};

			_clickCopySeparator = new TextBox
			{
				Text = clickCopyVm.Separator,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				VerticalAlignment = VerticalAlignment.Center,
				MinWidth = 40,
				Margin = new Thickness(4, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(_clickCopySeparator, "BulkEditBar.ClickCopySeparator");
			AutomationProperties.SetName(_clickCopySeparator, FwAvaloniaStrings.BulkClickCopySeparator);
			_clickCopySeparator.TextChanged += (_, __) => clickCopyVm.Separator = _clickCopySeparator.Text;

			var clickCopyRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 2, FwAvaloniaDensity.EditorPadding.Right, 2),
				Children =
				{
					new TextBlock { Text = FwAvaloniaStrings.BulkEditTarget, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) },
					_clickCopyTargetCombo,
					new TextBlock { Text = FwAvaloniaStrings.BulkClickCopyModeLabel, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) },
					_clickCopyWord, _clickCopyReorder,
					_clickCopyAppend, _clickCopySeparator, _clickCopyOverwrite
				}
			};

			var listChoiceTab = new TabItem
			{
				Header = FwAvaloniaStrings.BulkEditListChoice,
				Content = listChoiceRow
			};
			AutomationProperties.SetAutomationId(listChoiceTab, "BulkEditBar.ListChoiceTab");
			var copyTab = new TabItem
			{
				Header = FwAvaloniaStrings.BulkCopyTab,
				Content = copyRow
			};
			AutomationProperties.SetAutomationId(copyTab, "BulkEditBar.BulkCopyTab");
			var clearTab = new TabItem
			{
				Header = FwAvaloniaStrings.BulkClearTab,
				Content = clearRow
			};
			AutomationProperties.SetAutomationId(clearTab, "BulkEditBar.BulkClearTab");
			var replaceTab = new TabItem
			{
				Header = FwAvaloniaStrings.BulkReplaceTab,
				Content = replaceRow
			};
			AutomationProperties.SetAutomationId(replaceTab, "BulkEditBar.BulkReplaceTab");
			var transduceTab = new TabItem
			{
				Header = FwAvaloniaStrings.BulkTransduceTab,
				Content = transduceRow
			};
			AutomationProperties.SetAutomationId(transduceTab, "BulkEditBar.BulkTransduceTab");
			_clickCopyTab = new TabItem
			{
				Header = FwAvaloniaStrings.BulkClickCopyTab,
				Content = clickCopyRow
			};
			AutomationProperties.SetAutomationId(_clickCopyTab, "BulkEditBar.ClickCopyTab");

			_tabs = new TabControl
			{
				Padding = new Thickness(0),
				Items = { listChoiceTab, copyTab, clearTab, replaceTab, transduceTab, _clickCopyTab }
			};
			AutomationProperties.SetAutomationId(_tabs, "BulkEditBar.Tabs");
			// Click Copy is active ONLY while its tab is the selected one — so a data-cell click is a copy
			// gesture then, and normal selection/editing the rest of the time. Publish the active state to the
			// VM (the table reads it) and raise the event the host wires to the table's click-copy mode.
			_tabs.SelectionChanged += (_, __) =>
			{
				var active = ReferenceEquals(_tabs.SelectedItem, _clickCopyTab);
				clickCopyVm.IsActive = active;
				ClickCopyActiveChanged?.Invoke(this, active);
			};

			Content = new Border
			{
				Child = _tabs,
				Background = FwAvaloniaDensity.BrowseBackgroundBrush,
				BorderBrush = FwAvaloniaDensity.PickerBorderBrush,
				BorderThickness = new Thickness(0, 1, 0, 0)
			};

			_vm.PropertyChanged += OnViewModelPropertyChanged;
			copyVm.PropertyChanged += OnCopyViewModelPropertyChanged;
			clearVm.PropertyChanged += OnClearViewModelPropertyChanged;
			replaceVm.PropertyChanged += OnReplaceViewModelPropertyChanged;
			transduceVm.PropertyChanged += OnTransduceViewModelPropertyChanged;
			SyncEnablement();
			SyncCopyEnablement();
			SyncClearEnablement();
			SyncReplaceEnablement();
			SyncTransduceEnablement();
		}

		// A mode radio button bound to the copy VM: checking it sets the VM's Mode (which clears any pending
		// preview). All three share a group so exactly one is selected.
		private static RadioButton MakeModeRadio(string text, string automationId, BulkCopyMode mode,
			BulkCopyTabViewModel copyVm)
		{
			var radio = new RadioButton
			{
				Content = text,
				GroupName = "BulkEditBar.CopyMode",
				MinHeight = 0,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(radio, automationId);
			AutomationProperties.SetName(radio, text);
			radio.IsCheckedChanged += (_, __) =>
			{
				if (radio.IsChecked == true)
					copyVm.Mode = mode;
			};
			return radio;
		}

		// A mode radio button bound to the transduce VM (own group, separate from Bulk Copy's).
		private static RadioButton MakeTransduceModeRadio(string text, string automationId, BulkCopyMode mode,
			BulkTransduceTabViewModel transduceVm)
		{
			var radio = new RadioButton
			{
				Content = text,
				GroupName = "BulkEditBar.TransduceMode",
				MinHeight = 0,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(radio, automationId);
			AutomationProperties.SetName(radio, text);
			radio.IsCheckedChanged += (_, __) =>
			{
				if (radio.IsChecked == true)
					transduceVm.Mode = mode;
			};
			return radio;
		}

		// A word/reorder mode radio bound to the click-copy VM (its own group, separate from the copy/transduce
		// non-empty-target groups).
		private static RadioButton MakeClickCopyModeRadio(string text, string automationId, ClickCopyMode mode,
			BulkClickCopyTabViewModel clickCopyVm)
		{
			var radio = new RadioButton
			{
				Content = text,
				GroupName = "BulkEditBar.ClickCopyMode",
				MinHeight = 0,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 0, 0, 0)
			};
			AutomationProperties.SetAutomationId(radio, automationId);
			AutomationProperties.SetName(radio, text);
			radio.IsCheckedChanged += (_, __) =>
			{
				if (radio.IsChecked == true)
					clickCopyVm.Mode = mode;
			};
			return radio;
		}

		/// <summary>The bar's view-model (state + Preview/Apply routing).</summary>
		public BulkEditBarViewModel ViewModel => _vm;

		/// <summary>The target-column dropdown.</summary>
		public ComboBox TargetCombo => _targetCombo;

		/// <summary>The Preview button (disabled with Apply when nothing is checked/chosen).</summary>
		public Button PreviewButton => _previewButton;

		/// <summary>The Apply button (enabled only when a target+value are chosen and rows are checked).</summary>
		public Button ApplyButton => _applyButton;

		/// <summary>The value-selector launcher button (opens the FwOptionPicker flyout).</summary>
		public Button ValueButton => _valueButton;

		// ----- Phase 2: Bulk Copy controls (test surface) -----

		/// <summary>The copy SOURCE column dropdown.</summary>
		public ComboBox CopySourceCombo => _copySourceCombo;

		/// <summary>The copy TARGET column dropdown.</summary>
		public ComboBox CopyTargetCombo => _copyTargetCombo;

		/// <summary>The Append-mode radio (checked by default).</summary>
		public RadioButton CopyAppendRadio => _copyAppend;

		/// <summary>The Replace-mode radio.</summary>
		public RadioButton CopyReplaceRadio => _copyReplace;

		/// <summary>The DoNothingIfNonEmpty-mode radio.</summary>
		public RadioButton CopyDoNothingRadio => _copyDoNothing;

		/// <summary>The Bulk Copy Preview button (disabled with Apply when nothing checked / source==target).</summary>
		public Button CopyPreviewButton => _copyPreviewButton;

		/// <summary>The Bulk Copy Apply button (enabled only with checked rows and a distinct source/target).</summary>
		public Button CopyApplyButton => _copyApplyButton;

		// ----- Phase 3 / Delete Rows: the dual-mode Delete tab controls (test surface) -----

		/// <summary>The "Delete what?" mode toggle (Clear Field vs Delete Rows).</summary>
		public ComboBox DeleteWhatCombo => _deleteWhatCombo;

		/// <summary>The clear TARGET column dropdown (hidden in Delete-Rows mode).</summary>
		public ComboBox ClearTargetCombo => _clearTargetCombo;

		/// <summary>The Delete-tab Preview button (disabled with Apply when nothing checked / no target in Clear mode).</summary>
		public Button ClearPreviewButton => _clearPreviewButton;

		/// <summary>The Delete-tab Apply button (caption "Apply" in Clear-Field mode, "Delete" in Delete-Rows mode).</summary>
		public Button ClearApplyButton => _clearApplyButton;

		// ----- Find/Replace Phase 1: Bulk Replace controls (test surface) -----

		/// <summary>The replace TARGET column dropdown.</summary>
		public ComboBox ReplaceTargetCombo => _replaceTargetCombo;

		/// <summary>The Setup… button (opens the modal Find/Replace pattern dialog through the host).</summary>
		public Button ReplaceSetupButton => _replaceSetupButton;

		/// <summary>The one-line pattern summary shown on the Bulk Replace tab.</summary>
		public TextBlock ReplaceSummary => _replaceSummary;

		/// <summary>The Bulk Replace Preview button (disabled with Apply when nothing checked / no find text).</summary>
		public Button ReplacePreviewButton => _replacePreviewButton;

		/// <summary>The Bulk Replace Apply button (enabled only with checked rows, a target, and a find text).</summary>
		public Button ReplaceApplyButton => _replaceApplyButton;

		// ----- Process / Transduce controls (test surface) -----

		/// <summary>The transduce SOURCE column dropdown.</summary>
		public ComboBox TransduceSourceCombo => _transduceSourceCombo;

		/// <summary>The converter-picker dropdown.</summary>
		public ComboBox TransduceConverterCombo => _transduceConverterCombo;

		/// <summary>The transduce TARGET column dropdown.</summary>
		public ComboBox TransduceTargetCombo => _transduceTargetCombo;

		/// <summary>The Append-mode radio (checked by default).</summary>
		public RadioButton TransduceAppendRadio => _transduceAppend;

		/// <summary>The Replace-mode radio.</summary>
		public RadioButton TransduceReplaceRadio => _transduceReplace;

		/// <summary>The DoNothingIfNonEmpty-mode radio.</summary>
		public RadioButton TransduceDoNothingRadio => _transduceDoNothing;

		/// <summary>The Setup… button (opens the EncConverters management dialog through the host).</summary>
		public Button TransduceSetupButton => _transduceSetupButton;

		/// <summary>The Process Preview button (disabled with Apply when nothing checked / no converter / source==target).</summary>
		public Button TransducePreviewButton => _transducePreviewButton;

		/// <summary>The Process Apply button (enabled only with checked rows, a converter, and a distinct source/target).</summary>
		public Button TransduceApplyButton => _transduceApplyButton;

		// ----- Click Copy controls (test surface) -----

		/// <summary>The Click Copy TARGET column dropdown.</summary>
		public ComboBox ClickCopyTargetCombo => _clickCopyTargetCombo;

		/// <summary>The Word-mode radio (checked by default).</summary>
		public RadioButton ClickCopyWordRadio => _clickCopyWord;

		/// <summary>The Reorder (whole-field)-mode radio.</summary>
		public RadioButton ClickCopyReorderRadio => _clickCopyReorder;

		/// <summary>The separator text box (text inserted between an existing target and the appended source).</summary>
		public TextBox ClickCopySeparatorBox => _clickCopySeparator;

		/// <summary>The Append-directivity radio (checked by default).</summary>
		public RadioButton ClickCopyAppendRadio => _clickCopyAppend;

		/// <summary>The Overwrite-directivity radio.</summary>
		public RadioButton ClickCopyOverwriteRadio => _clickCopyOverwrite;

		/// <summary>The Click Copy tab item (selecting it activates the per-click copy gesture on the table).</summary>
		public TabItem ClickCopyTab => _clickCopyTab;

		/// <summary>The bar's tab control (test seam to drive/observe the selected tab).</summary>
		public TabControl Tabs => _tabs;

		/// <summary>
		/// Re-reads Apply enablement after the table's checked set changed outside the bar (a row checkbox
		/// toggled, check-all). The host calls this so the buttons reflect the current checked count.
		/// </summary>
		public void RefreshEnablement() => _vm.RefreshEnablement();

		private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BulkEditBarViewModel.CanApply))
				SyncEnablement();
			else if (e.PropertyName == nameof(BulkEditBarViewModel.SelectedOption))
				UpdateValueButtonText();
		}

		private void OnCopyViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BulkCopyTabViewModel.CanApply))
				SyncCopyEnablement();
		}

		private void OnClearViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BulkClearTabViewModel.CanApply))
				SyncClearEnablement();
		}

		private void OnReplaceViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BulkReplaceTabViewModel.CanApply))
				SyncReplaceEnablement();
			else if (e.PropertyName == nameof(BulkReplaceTabViewModel.Summary))
				UpdateReplaceSummary();
		}

		private void OnTransduceViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BulkTransduceTabViewModel.CanApply))
				SyncTransduceEnablement();
		}

		private void SyncEnablement()
		{
			var canApply = _vm.CanApply;
			_applyButton.IsEnabled = canApply;
			_previewButton.IsEnabled = canApply;
		}

		private void SyncCopyEnablement()
		{
			var canApply = _vm.BulkCopy.CanApply;
			_copyApplyButton.IsEnabled = canApply;
			_copyPreviewButton.IsEnabled = canApply;
		}

		private void SyncClearEnablement()
		{
			var canApply = _vm.BulkClear.CanApply;
			_clearApplyButton.IsEnabled = canApply;
			_clearPreviewButton.IsEnabled = canApply;
		}

		private void SyncReplaceEnablement()
		{
			var canApply = _vm.BulkReplace.CanApply;
			_replaceApplyButton.IsEnabled = canApply;
			_replacePreviewButton.IsEnabled = canApply;
		}

		private void SyncTransduceEnablement()
		{
			var canApply = _vm.BulkTransduce.CanApply;
			_transduceApplyButton.IsEnabled = canApply;
			_transducePreviewButton.IsEnabled = canApply;
		}

		private void UpdateReplaceSummary() => _replaceSummary.Text = _vm.BulkReplace.Summary;

		// Swaps the Delete tab's controls for the current mode (mirroring the legacy m_deleteWhatCombo handler):
		// in Clear-Field mode the target column label/combo show and Apply reads "Apply"; in Delete-Rows mode the
		// target column is irrelevant (hidden) and Apply re-captions to "Delete" (the destructive action).
		private void UpdateDeleteModeUi()
		{
			var deleteRows = _vm.BulkClear.IsDeleteRows;
			_clearTargetLabel.IsVisible = !deleteRows;
			_clearTargetCombo.IsVisible = !deleteRows;
			_clearApplyButton.Content = deleteRows ? FwAvaloniaStrings.BulkDeleteApply : FwAvaloniaStrings.BulkEditApply;
			AutomationProperties.SetName(_clearApplyButton,
				deleteRows ? FwAvaloniaStrings.BulkDeleteApply : FwAvaloniaStrings.BulkEditApply);
		}

		private void UpdateValueButtonText()
			=> _valueButton.Content = _vm.SelectedOption?.Name ?? FwAvaloniaStrings.SearchPrompt;

		// Rebuilds the value picker flyout for the current target's options. The picker commits the chosen
		// option back to the VM (which clears any pending preview and re-publishes enablement) and hides the
		// flyout. Re-created on target change so the option set always matches the selected column.
		private void RebuildValueFlyout()
		{
			var options = _vm.Options;
			var picker = new FwOptionPicker(options, null, "BulkEditBar.Value");
			picker.OptionCommitted += option =>
			{
				_vm.SelectedOption = option;
				_valueFlyout?.Hide();
			};
			picker.Dismissed += (_, __) => _valueFlyout?.Hide();
			_valueFlyout = FwOptionPicker.CreateOptionFlyout(picker, PlacementMode.Bottom);
			_valueButton.Flyout = _valueFlyout;
		}
	}
}
