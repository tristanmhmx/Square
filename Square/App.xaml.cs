using System;
using SQLite;
using Xamarin.Forms;

namespace Square
{
	public partial class App : Application
	{
		public ILocationService LocationService { get; set; }
		public SQLiteAsyncConnection DataBase { get; set; }
		public Position CurrentPosition { get; set; } = new Position { Latitude = 19.4326, Longitude = -99.1332 };
		public static App CurrentApp { get; set; }
		public App()
		{
			InitializeComponent();

			CurrentApp = this;

			LocationService = DependencyService.Get<ILocationService>();
			DataBase = DependencyService.Get<IDataService>().GetConnection();

			Initialize();

			MainPage = new HomePage();
		}

		async void Initialize()
		{
			try
			{
				var location = await LocationService.GetLocationAsync(TimeSpan.FromSeconds(20));
				CurrentPosition.Latitude = location.Latitude;
				CurrentPosition.Longitude = location.Longitude;
				LocationService.PositionChanged += Location_PositionChanged;
			}
			catch (Exception e)
			{
				
			}
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}

		void Location_PositionChanged(object sender, Square.PositionEventArgs e)
		{
			CurrentPosition.Latitude = e.Position.Latitude;
			CurrentPosition.Longitude = e.Position.Longitude;
		}
	}
}
