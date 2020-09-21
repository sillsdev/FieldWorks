// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace XCore
{
	public interface IPaneBar
	{
		string Text
		{
			set;
		}

		void Init(IImageCollection smallImages, IUIMenuAdapter menuBarAdapter, Mediator mediator);

		void RefreshPane();

		void  AddGroup(ChoiceGroup group);
	}

	public interface IPaneBarUser
	{
		IPaneBar MainPaneBar
		{
			get;
			set;
		}
	}
}