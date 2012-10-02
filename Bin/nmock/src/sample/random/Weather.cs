namespace NMockSample.Random
{

	public class Weather
	{
		private WeatherRandom random;
		private bool isRaining = false;
		private double temperature = 0.0;

		public Weather( WeatherRandom random )
		{
			this.random = random;
		}

		public bool IsRaining
		{
			get { return isRaining; }
		}

		public double Temperature
		{
			get { return temperature; }
		}

		public void Randomize()
		{
			temperature = random.NextTemperature();
			isRaining = random.NextIsRaining();
			if( isRaining ) temperature *= 0.5;
		}

	}

	public interface WeatherRandom
	{
		bool NextIsRaining();
		double NextTemperature();
	}

	public class DefaultWeatherRandom : WeatherRandom
	{
		public const double CHANCE_OF_RAIN = 0.2;
		public const double MIN_TEMPERATURE = 20;
		public const double MAX_TEMPERATURE = 30;

		private const double TEMPERATURE_RANGE = (MAX_TEMPERATURE-MIN_TEMPERATURE);

		private System.Random rng;

		public DefaultWeatherRandom( System.Random rng )
		{
			this.rng = rng;
		}

		public bool NextIsRaining() {
			return rng.NextDouble() < CHANCE_OF_RAIN;
		}

		public double NextTemperature() {
			return MIN_TEMPERATURE + rng.NextDouble() * TEMPERATURE_RANGE;
		}
	}

}
