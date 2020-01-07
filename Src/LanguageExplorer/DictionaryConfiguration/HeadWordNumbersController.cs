// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Code;
using SIL.LCModel;
using SIL.LCModel.DomainImpl;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// This class is responsible for manipulating the model and view related to HeadwordNumbers when configuring
	/// a dictionary or reversal index.
	/// </summary>
	internal class HeadwordNumbersController
	{
		private IHeadwordNumbersView _view;
		private DictionaryConfigurationModel _model;
		private LcmCache _cache;
		private DictionaryHomographConfiguration _homographConfig;

		public HeadwordNumbersController(IHeadwordNumbersView view, DictionaryConfigurationModel model, LcmCache cache)
		{
			Guard.AgainstNull(view, nameof(view));
			Guard.AgainstNull(model, nameof(model));
			Guard.AgainstNull(cache, nameof(cache));

			_view = view;
			_model = model;
			_cache = cache;
			_homographConfig = GetHeadwordConfiguration();
			_view.Description = string.Format(DictionaryConfigurationStrings.ConfigureHomograph_ConfigDescription,
				model.IsReversal ? LanguageExplorerResources.ReversalIndex : LanguageExplorerResources.Dictionary, Environment.NewLine, model.Label);
			_view.SetWsFactoryForCustomDigits(cache.WritingSystemFactory);
			_view.AvailableWritingSystems = cache.LangProject.CurrentAnalysisWritingSystems.Union(cache.LangProject.CurrentVernacularWritingSystems);
			_view.CustomDigits = _homographConfig.CustomHomographNumberList;
			if (_cache.LangProject.AllWritingSystems.Any(ws => ws.Id == _homographConfig.HomographWritingSystem))
			{
				_view.HomographWritingSystem = string.IsNullOrEmpty(_homographConfig.HomographWritingSystem) ? null : _cache.LangProject.AllWritingSystems.First(ws => ws.Id == _homographConfig.HomographWritingSystem).DisplayLabel;
			}
			else
			{
				_view.HomographWritingSystem = _cache.LangProject.AllWritingSystems.First().DisplayLabel;
			}
			_view.HomographBefore = _homographConfig.HomographNumberBefore;
			_view.ShowHomograph = _homographConfig.ShowHwNumber;
			_view.ShowHomographOnCrossRef = _model.IsReversal ? _homographConfig.ShowHwNumInReversalCrossRef : _homographConfig.ShowHwNumInCrossRef;
			_view.ShowSenseNumber = _model.IsReversal ? _homographConfig.ShowSenseNumberReversal : _homographConfig.ShowSenseNumber;
			_view.OkButtonEnabled = _homographConfig.CustomHomographNumberList == null || !_homographConfig.CustomHomographNumberList.Any() || _homographConfig.CustomHomographNumberList.Count == 10;
			_view.CustomDigitsChanged += OnViewCustomDigitsChanged;
		}

		private void OnViewCustomDigitsChanged(object sender, EventArgs eventArgs)
		{
			_view.OkButtonEnabled = !_view.CustomDigits.Any() || _view.CustomDigits.Count(digit => !string.IsNullOrWhiteSpace(digit)) == 10;
		}

		/// <summary>
		/// Retrieves the configuration from the model, or builds it from the defaults.
		/// </summary>
		private DictionaryHomographConfiguration GetHeadwordConfiguration()
		{
			return _model.HomographConfiguration ?? new DictionaryHomographConfiguration(new HomographConfiguration());
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
			_homographConfig.HomographWritingSystem = string.IsNullOrEmpty(_view.HomographWritingSystem) ? null : _cache.LangProject.AllWritingSystems.First(ws => ws.DisplayLabel == _view.HomographWritingSystem).Id;
			_homographConfig.CustomHomographNumberList = _view.CustomDigits == null ? new List<string>() : new List<string>(_view.CustomDigits);
			_model.HomographConfiguration = _homographConfig;
			_homographConfig.ExportToHomographConfiguration(_cache.ServiceLocator.GetInstance<HomographConfiguration>());
		}
	}
}
