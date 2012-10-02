// OptionsDlgModel.cs
// User: Jean-Marc Giffin at 2:19 P 18/06/2008

using System;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public class OptionsDlgModel : IDialogModel
	{
		public enum ArrowKeyBehaviourKind
		{
			Visual,
			Logical
		}

		ArrowKeyBehaviourKind behaviour_;
		bool outputGraphiteDebug_;

		public OptionsDlgModel()
		{
			behaviour_ = ArrowKeyBehaviourKind.Visual;
			outputGraphiteDebug_ = false;
		}

		public ArrowKeyBehaviourKind ArrowKeyBehaviour
		{
			get { return behaviour_; }
			set { behaviour_ = value; }
		}

		public bool OutputGraphiteDebug
		{
			get { return outputGraphiteDebug_; }
			set { outputGraphiteDebug_ = value; }
		}
	}
}
