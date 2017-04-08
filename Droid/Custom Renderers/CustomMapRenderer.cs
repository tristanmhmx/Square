using System;
using System.Linq;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Square;
using Square.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Maps.Android;
[assembly: ExportRenderer(typeof(CustomMap), typeof(CustomMapRenderer))]
namespace Square.Droid
{
	public class CustomMapRenderer : MapRenderer, IOnMapReadyCallback
	{
		private GoogleMap nativeMap;
		private CustomMap formsMap;
		protected override void OnElementChanged(Xamarin.Forms.Platform.Android.ElementChangedEventArgs<Xamarin.Forms.Maps.Map> e)
		{

			formsMap = (CustomMap)Element;

			if (e.OldElement != null && nativeMap != null)
			{
				nativeMap.MarkerClick -= NativeMap_MarkerClick;
				formsMap.ItemsSource.CollectionChanged -= ItemsSource_CollectionChanged;
			}

			if (e.NewElement != null)
			{
				var mapView = Control as MapView;
				mapView.GetMapAsync(this);
				formsMap.ItemsSource.CollectionChanged += ItemsSource_CollectionChanged;
			}

		}
		public void OnMapReady(GoogleMap googleMap)
		{
			nativeMap = googleMap;
			nativeMap.MarkerClick += NativeMap_MarkerClick;
		}


		void NativeMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
		{
			var marker = e.Marker;
			var pin = GetCustomPin(marker);
			formsMap.Navigate.Invoke(pin.Id);
		}

		void ItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (CustomPin item in e.NewItems)
				{
					var markerWithIcon = new MarkerOptions();

					markerWithIcon.SetPosition(new LatLng(item.Pin.Position.Latitude,
						item.Pin.Position.Longitude));

					markerWithIcon.SetTitle(string.IsNullOrWhiteSpace(item.Pin.Label) ? "-" : item.Pin.Label);

					markerWithIcon.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.Pin));

					nativeMap.AddMarker(markerWithIcon);
				}
			}
		}

		private CustomPin GetCustomPin(Marker annotation)
		{
			try
			{
				var formsMap = (CustomMap)Element;
				return formsMap?.ItemsSource?.FirstOrDefault(pin => pin.Pin.Position.Latitude == annotation.Position.Latitude && pin.Pin.Position.Longitude == annotation.Position.Longitude);
			}
			catch (Exception ex)
			{
				return null;
			}
		}
	}
}
