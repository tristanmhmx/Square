using System;
using System.Linq;
using MapKit;
using UIKit;
using Xamarin.Forms.Maps;

namespace Square.iOS
{
	public class MapDelegate : MKMapViewDelegate
	{
		CustomMap formsMap;
		MKMapView nativeMap;
		public MapDelegate(CustomMap formsMap, MKMapView nativeMap)
		{
			this.formsMap = formsMap;
			this.nativeMap = nativeMap;
		}

		public override MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
		{
			var customPin = GetCustomPin(annotation);

			if (customPin == null)
			{
				return new CustomMkAnnotationView(annotation, "")
				{
					Image = UIImage.FromFile("me"),
					Id = "me"
				};
			}

			var annotationView = mapView.DequeueReusableAnnotation(customPin.Id);

			if (annotationView == null)
			{

				annotationView = new CustomMkAnnotationView(annotation, customPin.Id)
				{
					Image = UIImage.FromFile("Pin")
				};

				((CustomMkAnnotationView)annotationView).Id = customPin.Id;

			}
			annotationView.CanShowCallout = false;

			return annotationView;
		}

		public override void DidSelectAnnotationView(MKMapView mapView, MKAnnotationView view)
		{
			var annotation = view as CustomMkAnnotationView;
			formsMap.Navigate.Invoke(annotation.Id);
		}

		CustomPin GetCustomPin(IMKAnnotation annotation)
		{
			try
			{
				var position = new Xamarin.Forms.Maps.Position(annotation.Coordinate.Latitude, annotation.Coordinate.Longitude);
				return formsMap?.ItemsSource?.FirstOrDefault(pin => pin.Pin.Position == position);
			}
			catch (Exception ex)
			{
				return null;
			}
		}
	}
	public class CustomMkAnnotationView : MKAnnotationView
	{
		public string Id { get; set; }

		public CustomMkAnnotationView(IMKAnnotation annotation, string id)
			: base(annotation, id)
		{
		}
	}
}
