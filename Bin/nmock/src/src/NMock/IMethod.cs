using System;

namespace NMock
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Describes a method
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IMethod : IVerifiable
	{
		/// <summary>Name of the method</summary>
		string Name { get; }
		/// <summary>Calls the method</summary>
		object Call(params object[] parameters);
		/// <summary>Set the expecations</summary>
		void SetExpectation(MockCall call);
	}

}
