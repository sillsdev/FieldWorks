// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// This class is for areas that support custom fields.
	/// </summary>
	internal sealed class CustomFieldsMenuHelper : IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private readonly CustomFieldLocationType _customFieldLocationType;
		private IPropertyTable _propertyTable;
		private IPublisher _publisher;

		internal CustomFieldsMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, AreaUiWidgetParameterObject areaUiWidgetParameterObject)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));
			Guard.AgainstNull(area, nameof(areaUiWidgetParameterObject));
			var supportedAreas = new HashSet<string>
			{
				AreaServices.LexiconAreaMachineName,
				AreaServices.NotebookAreaMachineName,
				AreaServices.TextAndWordsAreaMachineName
			};
			Require.That(supportedAreas.Contains(area.MachineName), $"'{area.UiName}' does not allow custom fields.");

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_propertyTable = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			_publisher = _majorFlexComponentParameters.FlexComponentParameters.Publisher;
			switch (area.MachineName)
			{
				case AreaServices.LexiconAreaMachineName:
					_customFieldLocationType = CustomFieldLocationType.Lexicon;
					break;
				case AreaServices.NotebookAreaMachineName:
					_customFieldLocationType = CustomFieldLocationType.Notebook;
					break;
				case AreaServices.TextAndWordsAreaMachineName:
					_customFieldLocationType = CustomFieldLocationType.Interlinear;
					break;
				default:
					throw new NotSupportedException($"'{area.MachineName}' does not support custom fields.");
			}
			SetupUiWidgets(areaUiWidgetParameterObject);
		}

		/// <summary>
		/// Setup the Tools->Configure->CustomFields menu.
		/// </summary>
		private void SetupUiWidgets(AreaUiWidgetParameterObject areaUiWidgetParameterObject)
		{
			// Tools->Configure->CustomFields menu is visible and enabled in this area.
			areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.Tools].Add(Command.CmdAddCustomField, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(AddCustomField_Click, () => CanAddCustomField));
		}

		private Tuple<bool, bool> CanAddCustomField => new Tuple<bool, bool>(true, !SharedBackendServices.AreMultipleApplicationsConnected(_propertyTable.GetValue<LcmCache>(FwUtils.cache)));

		private void AddCustomField_Click(object sender, EventArgs e)
		{
			using (var dlg = new AddCustomFieldDlg(_propertyTable, _publisher, _customFieldLocationType))
			{
				var activeForm = _propertyTable.GetValue<Form>(FwUtils.window);
				if (dlg.ShowCustomFieldWarning(activeForm))
				{
					dlg.ShowDialog(activeForm);
				}
			}
		}

		#region IDisposable
		private bool _isDisposed;

		~CustomFieldsMenuHelper()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}
			_majorFlexComponentParameters = null;
			_propertyTable = null;
			_publisher = null;

			_isDisposed = true;
		}
		#endregion
	}
}