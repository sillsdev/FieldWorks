// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainImpl;

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
		private LcmCache _cache;
		private DictionaryHomographConfiguration _homographConfig;

		public HeadwordNumbersController(IHeadwordNumbersView view, DictionaryConfigurationModel model, LcmCache cache)
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
			_view.AvailableWritingSystems = cache.LangProject.CurrentAnalysisWritingSystems.Union(cache.LangProject.CurrentVernacularWritingSystems);
			if (_cache.LangProject.AllWritingSystems.Any(ws => ws.Id == _homographConfig.HomographWritingSystem))
			{
				_view.HomographWritingSystem = string.IsNullOrEmpty(_homographConfig.HomographWritingSystem)
					? null
					: _cache.LangProject.AllWritingSystems.First(ws => ws.Id == _homographConfig.HomographWritingSystem).DisplayLabel;
			}
			else
			{
				_view.HomographWritingSystem = _cache.LangProject.AllWritingSystems.First().DisplayLabel;
			}
			// If possible, get digits from the writing system's numbering system,
			// otherwise use an empty list (which will be treated as default digits in the view).
			IEnumerable<string> wsCustomDigits = new List<string>();
			CoreWritingSystemDefinition writingSystem = null;
			try
			{
				writingSystem = cache.ServiceLocator.WritingSystemManager?.Get(_homographConfig.HomographWritingSystem);
			}
			catch(KeyNotFoundException)
			{
				// Do nothing; writingSystem is already null.
			}
			var unicodeCharacters = HeadWordNumbersHelper.GetUnicodeCharacters(writingSystem?.NumberingSystem?.Digits);
			if (unicodeCharacters != null)
			{
				wsCustomDigits = unicodeCharacters;
			}
			_view.CustomDigits = wsCustomDigits;
			_view.HomographBefore = _homographConfig.HomographNumberBefore;
			_view.ShowHomograph = _homographConfig.ShowHwNumber;
			_view.ShowHomographOnCrossRef = _model.IsReversal ? _homographConfig.ShowHwNumInReversalCrossRef : _homographConfig.ShowHwNumInCrossRef;
			_view.ShowSenseNumber = _model.IsReversal ? _homographConfig.ShowSenseNumberReversal : _homographConfig.ShowSenseNumber;
			_view.OkButtonEnabled = true;
		}

		/// <summary>
		/// Retrieves the configuration from the model, or builds it from the defaults.
		/// </summary>
		private DictionaryHomographConfiguration GetHeadwordConfiguration()
		{
			if (_model.HomographConfiguration != null)
			{
				return _model.HomographConfiguration;
			}
			return new DictionaryHomographConfiguration(new HomographConfiguration());
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
			_model.HomographConfiguration = _homographConfig;
			_homographConfig.ExportToHomographConfiguration(_cache.ServiceLocator.GetInstance<HomographConfiguration>());
		}
	}
}
