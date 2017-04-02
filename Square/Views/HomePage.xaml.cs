using Xamarin.Forms;

namespace Square
{
	public partial class HomePage : ContentPage
	{
		public HomePage()
		{
			InitializeComponent();
			MyMap.MoveToRegion(new Xamarin.Forms.Maps.MapSpan(new Xamarin.Forms.Maps.Position(App.CurrentApp.CurrentPosition.Latitude, App.CurrentApp.CurrentPosition.Longitude), 0.2, 0.2));
		}
	}
}
