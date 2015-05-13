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