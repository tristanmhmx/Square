using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Square
{
	public class HomeViewModel : BaseViewModel<HomeModel>
	{
		public ObservableCollection<CustomPin> Locations 
		{ 
			get { return Model.Locations; }
			set 
			{ 
				if (value != null)
				{
					Model.Locations = value;
					SetPropertyChanged();
				}
			}
		}
		private string searchCriteria;
		public string SearchCriteria
		{
			get
			{
				return searchCriteria;
			}
			set
			{
				if (value != null)
				{
					searchCriteria = value;
					SetPropertyChanged();
				}
			}
		}

		public ICommand SearchCommand { get; set; }
		public HomeViewModel()
		{
			Locations = new ObservableCollection<CustomPin>();
			SearchCommand = new Command(SearchPins);
			App.CurrentApp.DataBase.CreateTableAsync<Location>();
			App.CurrentApp.DataBase.InsertAsync(new Location
			{
				MapId = "1",
				Label = "Plaza de la constitución",
				Address = "Plaza de la constitución, Centro, Ciudad de México",
				Latitude = 19.4326,
				Longitude = -99.1332
			});
			App.CurrentApp.DataBase.InsertAsync(new Location
			{
				MapId = "2",
				Label = "Metro Allende",
				Address = "Tacuba 9, Centro, Ciudad de México",
				Latitude = 19.4330,
				Longitude = -99.1333
			});
			App.CurrentApp.DataBase.InsertAsync(new Location
			{
				MapId = "3",
				Label = "Pasaje Pino Suarez",
				Address = "Pino Suarez 10, Centro, Ciudad de México",
				Latitude = 19.4325, 
				Longitude = -99.1331
			});
			App.CurrentApp.DataBase.InsertAsync(new Location
			{
				MapId = "4",
				Label = "Joyeria Madero",
				Address = "Madero 35, Centro, Ciudad de México",
				Latitude = 19.4327, 
				Longitude = -99.1335
			});
		}

		async void SearchPins(object obj)
		{
			var pin = await App.CurrentApp.DataBase.Table<Location>().Where(p => p.Label.Contains(SearchCriteria)).FirstOrDefaultAsync();
			if (pin != null)
			{
				Locations.Add(new CustomPin
				{
					Id = pin.MapId,
					Pin = new Xamarin.Forms.Maps.Pin
					{
						Label = pin.Label,
						Address = pin.Address,
						Position = new Xamarin.Forms.Maps.Position(pin.Latitude, pin.Longitude)
					}
				});
			}
		}

	}
}
