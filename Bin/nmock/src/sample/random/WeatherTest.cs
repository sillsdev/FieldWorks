using NUnit.Framework;
using NMock;

namespace NMockSample.Random
{

	[TestFixture]
	public class WeatherTest
	{

		private IMock random;
		private Weather weather;

		[SetUp]
		public void SetUp()
		{
			random = new DynamicMock(typeof(WeatherRandom));
			weather = new Weather((WeatherRandom)random.MockInstance);
		}

		[Test]
		public void RandomRaining()
		{
			random.SetupResult("NextTemperature", 1.0);
			random.SetupResult("NextIsRaining", true);
			weather.Randomize();
			Assertion.Assert("is raining", weather.IsRaining);
		}

		[Test]
		public void RandomNotRaining()
		{
			random.SetupResult("NextTemperature", 1.0);
			random.SetupResult("NextIsRaining", false);
			weather.Randomize();
			Assertion.Assert("is not raining", !weather.IsRaining);
		}

		[Test]
		public void RandomTemperatureSunny()
		{
			double TEMPERATURE = 20.0;
			random.SetupResult("NextTemperature", TEMPERATURE);
			random.SetupResult("NextIsRaining", false);
			weather.Randomize();
			Assertion.AssertEquals("temperature", TEMPERATURE, weather.Temperature);
		}

		[Test]
		public void RandomTemperatureRaining()
		{
			double TEMPERATURE = 20.0;
			random.SetupResult("NextTemperature", TEMPERATURE);
			random.SetupResult("NextIsRaining", true);
			weather.Randomize();
			Assertion.AssertEquals("temperature", TEMPERATURE / 2.0, weather.Temperature);
		}

	}

	[TestFixture]
	public class DefaultWeatherRandomTest
	{

		[Test]
		public void NextIsRaining()
		{
			IMock random = new DynamicMock(typeof(System.Random));
			WeatherRandom weather = new DefaultWeatherRandom((System.Random)random.MockInstance);

			random.SetupResult("NextDouble", 0.0);
			Assertion.Assert("is raining", weather.NextIsRaining());

			random.SetupResult("NextDouble", DefaultWeatherRandom.CHANCE_OF_RAIN);
			Assertion.Assert("is not raining", !weather.NextIsRaining());

			random.SetupResult("NextDouble", 1.0);
			Assertion.Assert("is not raining", !weather.NextIsRaining());
		}

		[Test]
		public void NextTemperature()
		{
			IMock random = new DynamicMock(typeof(System.Random));
			WeatherRandom weather = new DefaultWeatherRandom((System.Random)random.MockInstance);

			random.SetupResult("NextDouble", 0.0);
			Assertion.AssertEquals("should be min temperature",
				DefaultWeatherRandom.MIN_TEMPERATURE,
				weather.NextTemperature()
			);

			random.SetupResult("NextDouble", 0.5);
			Assertion.AssertEquals("should be average temperature",
				0.5 * (DefaultWeatherRandom.MIN_TEMPERATURE + DefaultWeatherRandom.MAX_TEMPERATURE),
				weather.NextTemperature()
			);

			random.SetupResult("NextDouble", 1.0);
			Assertion.AssertEquals("should be max temperature",
				DefaultWeatherRandom.MAX_TEMPERATURE,
				weather.NextTemperature()
			);
		}

	}
}
