// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
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
			_view.SetWsFactoryForCustomDigits(cache.WritingSystemFactory);
			_view.AvailableWritingSystems = cache.LangProject.AllWritingSystems;
			_view.CustomDigits = _homographConfig.CustomHomographNumberList;
			_view.HomographWritingSystem = string.IsNullOrEmpty(_homographConfig.HomographWritingSystem)
				? null
				: _cache.LangProject.AllWritingSystems.First(ws => ws.Id == _homographConfig.HomographWritingSystem).DisplayLabel;
			_view.HomographBefore = _homographConfig.HomographNumberBefore;
			_view.ShowHomograph = _homographConfig.ShowHwNumber;
			_view.ShowHomographOnCrossRef = _model.IsReversal ? _homographConfig.ShowHwNumInReversalCrossRef : _homographConfig.ShowHwNumInCrossRef;
			_view.ShowSenseNumber = _model.IsReversal ? _homographConfig.ShowSenseNumberReversal : _homographConfig.ShowSenseNumber;
			_view.OkButtonEnabled = _homographConfig.CustomHomographNumberList == null
				|| !_homographConfig.CustomHomographNumberList.Any() || _homographConfig.CustomHomographNumberList.Count == 10;
			_view.CustomDigitsChanged += OnViewCustomDigitsChanged;
		}

		private void OnViewCustomDigitsChanged(object sender, EventArgs eventArgs)
		{
			_view.OkButtonEnabled = !_view.CustomDigits.Any()
				|| _view.CustomDigits.Count(digit => !string.IsNullOrWhiteSpace(digit)) == 10;
		}

		/// <summary>
		/// Retrieves the configuration from the model, or builds it from the singleton.
		/// </summary>
		private DictionaryHomographConfiguration GetHeadwordConfiguration()
		{
			if (_model.HomographConfiguration != null)
			{
				return _model.HomographConfiguration;
			}
			return new DictionaryHomographConfiguration(_cache.ServiceLocator.GetInstance<HomographConfiguration>());
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
			_homographConfig.HomographWritingSystem = string.IsNullOrEmpty(_view.HomographWritingSystem) ? null :
				_cache.LangProject.AllWritingSystems.First(ws => ws.DisplayLabel == _view.HomographWritingSystem).Id;
			_homographConfig.CustomHomographNumberList = _view.CustomDigits == null ? new List<string>() : new List<string>(_view.CustomDigits);
			_model.HomographConfiguration = _homographConfig;
			_homographConfig.ExportToHomographConfiguration(_cache.ServiceLocator.GetInstance<HomographConfiguration>());
		}
	}
}
