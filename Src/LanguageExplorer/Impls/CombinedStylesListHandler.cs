// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Handler to enable/disable displaying the combined styles combobox by default.
	/// </summary>
	internal sealed class CombinedStylesListHandler : IApplicationIdleEventHandler, IDisposable
	{
		// Used to count the number of times we've been asked to suspend Idle processing.
		private int _countSuspendIdleProcessing;
		private IFwMainWnd _mainWnd;
		private ISubscriber _subscriber;
		private LcmStyleSheet _stylesheet;
		private ToolStripComboBox _formatToolStripComboBox;
		private Dictionary<string, BaseStyleInfo> _characterStyleInformation;
		private SortedSet<string> _sortedCharacterStyleInformation;
		private Dictionary<string, BaseStyleInfo> _allStyleInformation;
		private SortedSet<string> _sortedAllStyleInformation;
		private bool _lastSetupIncludedParagraphStyles;
		private bool _skipProcessingClickEvent;

		internal CombinedStylesListHandler(IFwMainWnd mainWnd, ISubscriber subscriber, LcmStyleSheet stylesheet, ToolStripComboBox formatToolStripComboBox)
		{
			Guard.AgainstNull(mainWnd, nameof(mainWnd));
			Guard.AgainstNull(subscriber, nameof(subscriber));
			Guard.AgainstNull(stylesheet, nameof(stylesheet));
			Guard.AgainstNull(formatToolStripComboBox, nameof(formatToolStripComboBox));

			_characterStyleInformation = new Dictionary<string, BaseStyleInfo>();
			_sortedCharacterStyleInformation = new SortedSet<string>();
			_allStyleInformation = new Dictionary<string, BaseStyleInfo>();
			_sortedAllStyleInformation = new SortedSet<string>();

			_mainWnd = mainWnd;
			_subscriber = subscriber;
			_stylesheet = stylesheet;
			_formatToolStripComboBox = formatToolStripComboBox;
			_formatToolStripComboBox.ComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			_formatToolStripComboBox.Enabled = false;
			_subscriber.Subscribe("ResetStyleSheet", ResetStyleSheet);

			CollectStyleInformation();

			Application.Idle += ApplicationOnIdle;
		}

		#region IDisposable Members

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		~CombinedStylesListHandler()
		{
			Dispose(false);
		}

		/// <summary>
		/// Clean up everything that we've been using.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				Application.Idle -= ApplicationOnIdle;
				// Dispose managed resources here.
				_subscriber.Unsubscribe("ResetStyleSheet", ResetStyleSheet);
				_characterStyleInformation.Clear();
				_sortedCharacterStyleInformation.Clear();
				_allStyleInformation.Clear();
				_sortedAllStyleInformation.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			_mainWnd = null;
			_stylesheet = null;
			_formatToolStripComboBox = null;
			_characterStyleInformation = null;
			_sortedCharacterStyleInformation = null;
			_allStyleInformation = null;
			_sortedAllStyleInformation = null;
			_subscriber = null;

			IsDisposed = true;
		}

		#endregion

		/// <summary>
		/// Update enabled status for the combobox on the toolbar and for the main menu on the Format menu.
		/// </summary>
		private void ApplicationOnIdle(object sender, EventArgs eventArgs)
		{
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"Start: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
			var activeView = _mainWnd.ActiveView as SimpleRootSite;
			if (activeView == null)
			{
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"End1: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
				return;
			}
			if (!activeView.Focused)
			{
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"End2: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
				return;
			}

			_skipProcessingClickEvent = true;

			var enabled = false;
			var newText = string.Empty;
			try
			{
				var originalEnabledState = _formatToolStripComboBox.Enabled;
				if (activeView is SandboxBase)
				{
					newText = StyleUtils.DefaultParaCharsStyleName;
				}
				else
				{
					enabled = activeView.CanApplyStyle;
					newText = activeView.BestSelectionStyle;
				}
				if (originalEnabledState == enabled)
				{
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"End3: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
					return; // No change.
				}

				SortedSet<string> newSet = null;
				if (enabled)
				{
					var canHandleParagraphStyles = activeView.IsSelectionInParagraph;
					if (_lastSetupIncludedParagraphStyles)
					{
						if (canHandleParagraphStyles)
						{
							// Normally do nothing, unless the combobox is empty.
							if (_formatToolStripComboBox.Items.Count == 0)
							{
								newSet = _sortedAllStyleInformation;
								_lastSetupIncludedParagraphStyles = true;
							}
						}
						else
						{
							// Swap out para styles for only character styles.
							newSet = _sortedCharacterStyleInformation;
							_lastSetupIncludedParagraphStyles = false;
						}
					}
					else
					{
						if (canHandleParagraphStyles)
						{
							// Swap out character styles for paragraph styles.
							newSet = _sortedAllStyleInformation;
							_lastSetupIncludedParagraphStyles = true;
						}
						else
						{
							newSet = _sortedCharacterStyleInformation;
							_lastSetupIncludedParagraphStyles = false;
						}
					}
				}
				if (newSet != null)
				{
					_formatToolStripComboBox.Items.Clear();
					_formatToolStripComboBox.Items.AddRange(newSet.ToArray());
				}
			}
			finally
			{
				_formatToolStripComboBox.Enabled = enabled;
				_formatToolStripComboBox.SelectedItem = enabled ? newText : string.Empty;
				_skipProcessingClickEvent = false;
			}
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"End4: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
		}

		private void CollectStyleInformation()
		{
			_characterStyleInformation.Clear();
			_sortedCharacterStyleInformation.Clear();

			_allStyleInformation.Clear();
			_sortedAllStyleInformation.Clear();

			_formatToolStripComboBox.Items.Clear();

			foreach (var styleInfo in _stylesheet.Styles)
			{
				if (styleInfo.RealStyle.Type == StyleType.kstCharacter)
				{
					_characterStyleInformation.Add(styleInfo.Name, styleInfo);
					_sortedCharacterStyleInformation.Add(styleInfo.Name);
				}
				_allStyleInformation.Add(styleInfo.Name, styleInfo);
				_sortedAllStyleInformation.Add(styleInfo.Name);
			}
			_sortedCharacterStyleInformation.Add(StyleUtils.DefaultParaCharsStyleName);
		}

		private void FormatToolStripComboBoxOnSelectedIndexChanged(object sender, EventArgs eventArgs)
		{
			if (_skipProcessingClickEvent)
			{
				return;
			}
			var selectedStyleName = (string)_formatToolStripComboBox.SelectedItem;
			BaseStyleInfo newlySelectedStyle;
			if (_lastSetupIncludedParagraphStyles)
			{
				_allStyleInformation.TryGetValue(selectedStyleName, out newlySelectedStyle);
			}
			else
			{
				_characterStyleInformation.TryGetValue(selectedStyleName, out newlySelectedStyle);
			}
			var actualStyleName = ((SimpleRootSite)_mainWnd.ActiveView).Style_Changed(newlySelectedStyle);
			if (string.IsNullOrWhiteSpace(actualStyleName))
			{
				// Code was confused, so leave what was selected.
				return;
			}

			// Someone wasn't happy with what the user selected and changed it
			// Unwire event handler, and reset selected style to returned value, and rewire event handler.
			_skipProcessingClickEvent = true;
			_formatToolStripComboBox.SelectedItem = actualStyleName;
			_skipProcessingClickEvent = false;
		}

		private void ResetStyleSheet(object newValue)
		{
			// It can be the same as the old one, but it may still alter the styles that are listed,
			// since in some cases only character stules are listed, but in other cases, all styles are listed.
			_stylesheet = (LcmStyleSheet)newValue;
			_formatToolStripComboBox.SelectedIndexChanged -= FormatToolStripComboBoxOnSelectedIndexChanged;
			_formatToolStripComboBox.Items.Clear();
			CollectStyleInformation();
		}

		#region Implementation of IApplicationIdleEventHandler
		/// <summary>
		/// Call this for the duration of a block of code where we don't want idle events.
		/// (Note that various things outside our control may pump events and cause the
		/// timer that fires the idle events to be triggered when we are not idle, even in the
		/// middle of processing another event.) Call ResumeIdleProcessing when done.
		/// </summary>
		public void SuspendIdleProcessing()
		{
			_countSuspendIdleProcessing++;
			if (_countSuspendIdleProcessing == 1)
			{
				Application.Idle -= ApplicationOnIdle;
			}
		}

		/// <summary>
		/// See SuspendIdleProcessing.
		/// </summary>
		public void ResumeIdleProcessing()
		{
			FwUtils.CheckResumeProcessing(_countSuspendIdleProcessing, GetType().Name);
			if (_countSuspendIdleProcessing > 0)
			{
				_countSuspendIdleProcessing--;
				if (_countSuspendIdleProcessing == 0)
				{
					Application.Idle += ApplicationOnIdle;
				}
			}
		}
		#endregion
	}
}
