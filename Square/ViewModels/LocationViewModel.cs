namespace Square
{
	public class LocationViewModel : BaseViewModel<LocationModel>
	{
		public CustomPin Pin
		{
			get
			{
				return Model.pin;
			}
			set
			{
				if (value != null)
				{
					Model.pin = value;
					SetPropertyChanged();
				}
			}
		}

		public LocationViewModel(string id)
		{
			var pin = App.CurrentApp.DataBase.Table<Location>().Where(l => l.MapId == id).FirstOrDefaultAsync().Result;
			if (pin != null)
			{
				Pin = new CustomPin
				{
					Id = pin.MapId,
					Pin = new Xamarin.Forms.Maps.Pin
					{
						Label = pin.Label,
						Address = pin.Address,
						Position = new Xamarin.Forms.Maps.Position(pin.Latitude, pin.Longitude)
					}
				};				
			}
		}
	}
}

