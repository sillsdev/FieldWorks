// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Works
{
	public delegate void SwitchConfigurationEvent(object sender, SwitchConfigurationEventArgs args);

	/// <summary>
	/// The arguments for a SwitchConfigurationEvent. Includes the configuration selected as a property.
	/// </summary>
	public class SwitchConfigurationEventArgs
	{
		public DictionaryConfigurationModel ConfigurationPicked { get; set; }
	}
}