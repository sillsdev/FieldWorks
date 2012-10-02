using System;
using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a set of alpha variables. An alpha variable is a variable for a feature value.
	/// </summary>
	public class AlphaVariables
	{
		IDictionary<string, Feature> m_varFeatures;

		/// <summary>
		/// Initializes a new instance of the <see cref="AlphaVariables"/> class.
		/// </summary>
		/// <param name="varFeatures">The varFeats of variables and features.</param>
		/// <param name="featSys">The feature system.</param>
		public AlphaVariables(IDictionary<string, Feature> varFeatures)
		{
			m_varFeatures = varFeatures;
		}

		/// <summary>
		/// Gets the variables.
		/// </summary>
		/// <value>The variables.</value>
		public IEnumerable<string> Variables
		{
			get
			{
				return m_varFeatures.Keys;
			}
		}

		/// <summary>
		/// Gets the feature associated with the specified variable.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>The feature.</returns>
		public Feature GetFeature(string variable)
		{
			Feature feature;
			if (m_varFeatures.TryGetValue(variable, out feature))
				return feature;
			return null;
		}

		/// <summary>
		/// Gets a valid binding between the specified variables and the feature values
		/// currently set on the specified segment. It adds the variable values to the varFeats of
		/// instantiated variables.
		/// </summary>
		/// <param name="variables">The variables.</param>
		/// <param name="seg">The segment.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns><c>true</c> if a valid binding was found, otherwise <c>false</c></returns>
		public bool GetBinding(IDictionary<string, bool> variables, Segment seg, VariableValues instantiatedVars)
		{
			foreach (KeyValuePair<string, bool> varPolarity in variables)
			{
				if (!GetBinding(varPolarity.Key, varPolarity.Value, seg, instantiatedVars))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Gets a valid binding between the specified variable and the feature values
		/// currently set on the specified segment. It adds the variable value to the varFeats of
		/// instantiated variables.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <param name="polarity">The variable polarity.</param>
		/// <param name="seg">The segment.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns><c>true</c> if a valid binding was found, otherwise <c>false</c></returns>
		public bool GetBinding(string variable, bool polarity, Segment seg, VariableValues instantiatedVars)
		{
			bool match = false;
			foreach (FeatureValue value in GetVarFeatValue(variable, polarity, instantiatedVars))
			{
				if (seg.FeatureValues.Get(value))
				{
					instantiatedVars.Add(variable, value);
					match = true;
					break;
				}
			}

			return match;
		}

		/// <summary>
		/// Gets all valid bindings between the specified variables and the feature values
		/// currently set on the specified segment. It adds the variable values to the varFeats of
		/// instantiated variables.
		/// </summary>
		/// <param name="variables">The variables.</param>
		/// <param name="seg">The segment.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns><c>true</c> if a valid binding was found, otherwise <c>false</c></returns>
		public bool GetAllBindings(IDictionary<string, bool> variables, Segment seg, VariableValues instantiatedVars)
		{
			foreach (KeyValuePair<string, bool> varPolarity in variables)
			{
				bool match = false;
				foreach (FeatureValue value in GetVarFeatValue(varPolarity.Key, varPolarity.Value, instantiatedVars))
				{
					if (seg.FeatureValues.Get(value))
					{
						instantiatedVars.Add(varPolarity.Key, value);
						match = true;
					}
				}

				if (!match)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Applies the specified variables that occur in the instantiated variables to the specified feature bundle.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		/// <param name="variables">The variables.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		public void ApplyCurrent(FeatureBundle fb, IDictionary<string, bool> variables, VariableValues instantiatedVars)
		{
			foreach (KeyValuePair<string, bool> varPolarity in variables)
			{
				Feature feature = m_varFeatures[varPolarity.Key];
				ICollection<FeatureValue> varValues = instantiatedVars.GetValues(varPolarity.Key);
				if (varValues.Count > 0)
				{
					foreach (FeatureValue value in GetCurVarFeatValue(feature, varValues, varPolarity.Value))
						fb.Set(value, true);
				}
			}
		}

		/// <summary>
		/// Applies the specified variables to the specified feature bundle.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		/// <param name="variables">The variables.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <param name="val">if <c>true</c> the feature bundle values will be set, otherwise they will be unset.</param>
		public void Apply(FeatureBundle fb, IDictionary<string, bool> variables, VariableValues instantiatedVars, bool val)
		{
			foreach (KeyValuePair<string, bool> varPolarity in variables)
			{
				foreach (FeatureValue value in GetVarFeatValue(varPolarity.Key, varPolarity.Value, instantiatedVars))
					fb.Set(value, val);
			}
		}

		/// <summary>
		/// Enumerates thru all of the possible values for the specified variable.
		/// </summary>
		/// <param name="variableName">The variable.</param>
		/// <param name="polarity">The variable polarity.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns>An enumerable of feature values.</returns>
		IEnumerable<FeatureValue> GetVarFeatValue(string variableName, bool polarity, VariableValues instantiatedVars)
		{
			Feature feature = m_varFeatures[variableName];

			ICollection<FeatureValue> varValues = instantiatedVars.GetValues(variableName);
			if (varValues.Count > 0)
			{
				// variable is instantiated, so only check already instantiated values
				foreach (FeatureValue value in GetCurVarFeatValue(feature, varValues, polarity))
					yield return value;
			}
			else
			{
				foreach (FeatureValue value in feature.PossibleValues)
				{
					// if polarity is true, then values must be the same, otherwise they must be different
					if (polarity)
					{
						yield return value;
					}
					else
					{
						foreach (FeatureValue value2 in feature.PossibleValues)
						{
							if (value2 != value)
								yield return value2;
						}
					}
				}
			}
		}

		/// <summary>
		/// Enumerate thru all possible values that are currently instantiated.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <param name="varValues">The possible values.</param>
		/// <param name="polarity">The variable polarity.</param>
		/// <returns>An enumerable of feature values.</returns>
		IEnumerable<FeatureValue> GetCurVarFeatValue(Feature feature, ICollection<FeatureValue> varValues, bool polarity)
		{
			foreach (FeatureValue value in varValues)
			{
				// if polarity is true, then return current value, otherwise return all other values
				if (polarity)
				{
					yield return value;
				}
				else
				{
					foreach (FeatureValue value2 in feature.PossibleValues)
					{
						if (value2 != value)
							yield return value2;
					}
				}
			}
		}
	}

	/// <summary>
	/// This class represents a set of alpha variable values.
	/// </summary>
	public class VariableValues : ICloneable
	{
		Dictionary<string, List<FeatureValue>> m_values;

		/// <summary>
		/// Initializes a new instance of the <see cref="VariableValues"/> class.
		/// </summary>
		/// <param name="alphaVars">The alpha variables.</param>
		public VariableValues(AlphaVariables alphaVars)
		{
			m_values = new Dictionary<string, List<FeatureValue>>();
			foreach (string variableName in alphaVars.Variables)
				m_values[variableName] = new List<FeatureValue>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VariableValues"/> class.
		/// </summary>
		public VariableValues()
		{
			m_values = new Dictionary<string, List<FeatureValue>>();
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="varValues">The variable values.</param>
		public VariableValues(VariableValues varValues)
		{
			m_values = new Dictionary<string, List<FeatureValue>>(varValues.m_values);
		}

		/// <summary>
		/// Adds the specified value to the specified variable.
		/// </summary>
		/// <param name="variableName">Name of the variable.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="System.ArgumentException">Thrown when the specified variable is not defined.</exception>
		public void Add(string variableName, FeatureValue value)
		{
			List<FeatureValue> values;
			if (!m_values.TryGetValue(variableName, out values))
			{
				throw new ArgumentException(string.Format(HCStrings.kstidUnknownVar, variableName), "variableName");
			}
			values.Add(value);
		}

		/// <summary>
		/// Replaces all of the values with the specified values.
		/// </summary>
		/// <param name="varValues">The variable values.</param>
		public void ReplaceValues(VariableValues varValues)
		{
			m_values.Clear();
			foreach (KeyValuePair<string, List<FeatureValue>> kvp in varValues.m_values)
				m_values.Add(kvp.Key, kvp.Value);
		}

		/// <summary>
		/// Gets the values for the specified variable.
		/// </summary>
		/// <param name="variableName">Name of the variable.</param>
		/// <returns>All of the values.</returns>
		/// <exception cref="System.ArgumentException">Thrown when the specified variable is not defined.</exception>
		public ICollection<FeatureValue> GetValues(string variableName)
		{
			List<FeatureValue> values;
			if (!m_values.TryGetValue(variableName, out values))
			{
				throw new ArgumentException(string.Format(HCStrings.kstidUnknownVar, variableName), "variableName");
			}
			return new List<FeatureValue>(values);
		}

		/// <summary>
		/// Removes ambiguous variable values.
		/// </summary>
		public void RemoveAmbiguousVariables()
		{
			foreach (KeyValuePair<string, List<FeatureValue>> kvp in m_values)
			{
				if (kvp.Value.Count > 1)
					kvp.Value.Clear();
			}
		}

		public VariableValues Clone()
		{
			return new VariableValues(this);
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}
	}
}
