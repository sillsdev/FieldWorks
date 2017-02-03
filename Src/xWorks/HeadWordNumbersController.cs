// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class is responsible for manipulating the model and view related to HeadwordNumbers when configuring
	/// a dictionary or reversal index.
	/// </summary>
	class HeadwordNumbersController
	{
		private IHeadwordNumbersView _view;
		private DictionaryConfigurationModel _model;
		private FdoCache _cache;
		private DictionaryHomographConfiguration _homographConfig;

		public HeadwordNumbersController(IHeadwordNumbersView view, DictionaryConfigurationModel model, FdoCache cache)
		{
			if(view == null || model == null || cache == null)
				throw new ArgumentNullException();
			_view = view;
			_model = model;
			_cache = cache;
			_homographConfig = GetHeadwordConfiguration();
			_view.Description = string.Format(xWorksStrings.ConfigureHomograph_ConfigDescription,
				model.IsReversal ? xWorksStrings.ReversalIndex : xWorksStrings.Dictionary,
				Environment.NewLine,
				model.Label);
			_view.Shown += OnShowDialog;
		}

		/// <summary>
		/// Retrieves the configuration from the model, or builds it from the singleton.
		/// </summary>
		private DictionaryHomographConfiguration GetHeadwordConfiguration()
		{
			if (_model.HomographNumbers != null)
			{
				return _model.HomographNumbers;
			}
			return new DictionaryHomographConfiguration(_cache.ServiceLocator.GetInstance<HomographConfiguration>());
		}

		/// <summary>
		/// Set the model values in the view after layout is complete
		/// </summary>
		private void OnShowDialog(object sender, EventArgs eventArgs)
		{
			_view.HomographBefore = _homographConfig.HomographNumberBefore;
			_view.ShowHomograph = _homographConfig.ShowHwNumber;
			_view.ShowHomographOnCrossRef = _model.IsReversal ? _homographConfig.ShowHwNumInReversalCrossRef : _homographConfig.ShowHwNumInCrossRef;
			_view.ShowSenseNumber = _model.IsReversal ? _homographConfig.ShowSenseNumberReversal : _homographConfig.ShowSenseNumber;
		}

		/// <summary>
		/// Save any changes the user made into the model and update the singleton.
		/// The values from the dialog map to different model parts depending on the configuration type (Dictionary, Reversal Index, etc.)
		/// </summary>
		public void Save()
		{
			_homographConfig.HomographNumberBefore = _view.HomographBefore;
			_homographConfig.ShowHwNumber = _view.ShowHomograph;
			if (_model.IsReversal)
			{
				_homographConfig.ShowHwNumInReversalCrossRef = _view.ShowHomographOnCrossRef;
				_homographConfig.ShowSenseNumberReversal = _view.ShowSenseNumber;
			}
			else
			{
				_homographConfig.ShowHwNumInCrossRef = _view.ShowHomographOnCrossRef;
				_homographConfig.ShowSenseNumber = _view.ShowSenseNumber;
			}
			_model.HomographNumbers = _homographConfig;
			_homographConfig.ExportToHomographConfiguration(_cache.ServiceLocator.GetInstance<HomographConfiguration>());
		}
	}
}
