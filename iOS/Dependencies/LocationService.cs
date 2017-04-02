using System;
using System.Threading;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;
using Square.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
[assembly: Dependency(typeof(LocationService))]
namespace Square.iOS
{
	public class LocationService : ILocationService
	{
		bool deferringUpdates;
		bool isListening;
		Position position;
		ListenerSettings listenerSettings;

		public LocationService()
		{
			Manager.AuthorizationChanged += OnAuthorizationChanged;
			Manager.Failed += OnFailed;
			if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
				Manager.LocationsUpdated += OnLocationsUpdated;
			else
				Manager.UpdatedLocation += OnUpdatedLocation;

			Manager.UpdatedHeading += OnUpdatedHeading;
			Manager.DeferredUpdatesFinished += OnDeferredUpdatedFinished;
			RequestAuthorization();
		}

		void OnDeferredUpdatedFinished(object sender, NSErrorEventArgs e) => deferringUpdates = false;

		public event EventHandler<PositionErrorEventArgs> PositionError;

		bool CanDeferLocationUpdate => CLLocationManager.DeferredLocationUpdatesAvailable && UIDevice.CurrentDevice.CheckSystemVersion(6, 0);

		public bool IsListening => isListening;

		public bool SupportsHeading => CLLocationManager.HeadingAvailable;

		public event EventHandler<PositionEventArgs> PositionChanged;

		public bool IsGeolocationAvailable => true;

		public bool IsGeolocationEnabled
		{
			get
			{
				var status = CLLocationManager.Status;

				if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
				{
					return CLLocationManager.LocationServicesEnabled && (status == CLAuthorizationStatus.AuthorizedAlways
					|| status == CLAuthorizationStatus.AuthorizedWhenInUse);
				}

				return CLLocationManager.LocationServicesEnabled && status == CLAuthorizationStatus.Authorized;
			}
		}

		public Task<Position> GetLocationAsync(TimeSpan? timeout, CancellationToken? cancelToken = null, bool includeHeading = false)
		{
			var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

			if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive or Timeout.Infinite");

			if (!cancelToken.HasValue)
				cancelToken = CancellationToken.None;

			TaskCompletionSource<Position> tcs;
			if (!IsListening)
			{
				// permit background updates if background location mode is enabled
				if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
				{
					NSArray backgroundModes = NSBundle.MainBundle.InfoDictionary[(NSString)"UIBackgroundModes"] as NSArray;
					Manager.AllowsBackgroundLocationUpdates = backgroundModes != null && (backgroundModes.Contains((NSString)"Location") || backgroundModes.Contains((NSString)"location"));
				}

				// always prevent location update pausing since we're only listening for a single update.
				if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
					Manager.PausesLocationUpdatesAutomatically = false;

				tcs = new TaskCompletionSource<Position>(Manager);
				var singleListener = new GeolocationSingleUpdateDelegate(Manager, 500, includeHeading, timeoutMilliseconds, cancelToken.Value);
				Manager.Delegate = singleListener;

				Manager.StartUpdatingLocation();


				if (includeHeading && SupportsHeading)
					Manager.StartUpdatingHeading();

				return singleListener.Task;
			}


			tcs = new TaskCompletionSource<Position>();
			if (position == null)
			{
				if (cancelToken != CancellationToken.None)
				{
					cancelToken.Value.Register(() => tcs.TrySetCanceled());
				}

				EventHandler<PositionErrorEventArgs> gotError = null;
				gotError = (s, e) =>
				{
					tcs.TrySetException(new GeolocationException(e.Error));
					PositionError -= gotError;
				};

				PositionError += gotError;

				EventHandler<PositionEventArgs> gotPosition = null;
				gotPosition = (s, e) =>
				{
					tcs.TrySetResult(e.Position);
					PositionChanged -= gotPosition;
				};

				PositionChanged += gotPosition;
			}
			else
				tcs.SetResult(position);


			return tcs.Task;
		}

		CLLocationManager Manager
		{
			get
			{
				CLLocationManager m = null;
				new NSObject().InvokeOnMainThread(() => m = new CLLocationManager());
				return m;
			}
		}

		void OnUpdatedLocation(object sender, CLLocationUpdatedEventArgs e) => UpdatePosition(e.NewLocation);

		void UpdatePosition(CLLocation location)
		{
			var p = (position == null) ? new Position() : new Position(this.position);

			if (location.HorizontalAccuracy > -1)
			{
				p.Accuracy = location.HorizontalAccuracy;
				p.Latitude = location.Coordinate.Latitude;
				p.Longitude = location.Coordinate.Longitude;
			}

			if (location.VerticalAccuracy > -1)
			{
				p.Altitude = location.Altitude;
				p.AltitudeAccuracy = location.VerticalAccuracy;
			}

			if (location.Speed > -1)
				p.Speed = location.Speed;

			try
			{
				var date = location.Timestamp.ToDateTime();
				p.Timestamp = new DateTimeOffset(date);
			}
			catch (Exception ex)
			{
				p.Timestamp = DateTimeOffset.UtcNow;
			}


			position = p;

			OnPositionChanged(new PositionEventArgs(p));

			location.Dispose();
		}



		public Task<bool> StartListeningAsync(TimeSpan minimumTime, double minDistance, bool includeHeading = false, ListenerSettings settings = null)
		{
			if (minDistance < 0)
				throw new ArgumentOutOfRangeException("minDistance");
			if (isListening)
				throw new InvalidOperationException("Already listening");

			// if no settings were passed in, instantiate the default settings. need to check this and create default settings since
			// previous calls to StartListeningAsync might have already configured the location manager in a non-default way that the
			// caller of this method might not be expecting. the caller should expect the defaults if they pass no settings.
			if (settings == null)
				settings = new ListenerSettings();

			// keep reference to settings so that we can stop the listener appropriately later
			listenerSettings = settings;

			var desiredAccuracy = 500;

			if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
				Manager.AllowsBackgroundLocationUpdates = settings.AllowBackgroundUpdates;

			// configure location update pausing
			if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
			{
				Manager.PausesLocationUpdatesAutomatically = settings.PauseLocationUpdatesAutomatically;

				switch (settings.ActivityType)
				{
					case ActivityType.AutomotiveNavigation:
						Manager.ActivityType = CLActivityType.AutomotiveNavigation;
						break;
					case ActivityType.Fitness:
						Manager.ActivityType = CLActivityType.Fitness;
						break;
					case ActivityType.OtherNavigation:
						Manager.ActivityType = CLActivityType.OtherNavigation;
						break;
					default:
						Manager.ActivityType = CLActivityType.Other;
						break;
				}
			}

			// to use deferral, CLLocationManager.DistanceFilter must be set to CLLocationDistance.None, and CLLocationManager.DesiredAccuracy must be 
			// either CLLocation.AccuracyBest or CLLocation.AccuracyBestForNavigation. deferral only available on iOS 6.0 and above.
			if (CanDeferLocationUpdate && settings.DeferLocationUpdates)
			{
				minDistance = CLLocationDistance.FilterNone;
				desiredAccuracy = (int)CLLocation.AccuracyBest;
			}

			isListening = true;
			Manager.DesiredAccuracy = desiredAccuracy;
			Manager.DistanceFilter = minDistance;

			if (settings.ListenForSignificantChanges)
				Manager.StartMonitoringSignificantLocationChanges();
			else
				Manager.StartUpdatingLocation();
			if (includeHeading && CLLocationManager.HeadingAvailable)
				Manager.StartUpdatingHeading();

			return Task.FromResult(true);
		}

		public Task<bool> StopListeningAsync()
		{
			if (!isListening)
				return Task.FromResult(true);

			isListening = false;
			if (CLLocationManager.HeadingAvailable)
				Manager.StopUpdatingHeading();

			// it looks like deferred location updates can apply to the standard service or significant change service. disallow deferral in either case.
			if ((listenerSettings?.DeferLocationUpdates ?? false) && CanDeferLocationUpdate)
				Manager.DisallowDeferredLocationUpdates();
			
			if (listenerSettings?.ListenForSignificantChanges ?? false)
				Manager.StopMonitoringSignificantLocationChanges();
			else
				Manager.StopUpdatingLocation();

			listenerSettings = null;
			position = null;

			return Task.FromResult(true);
		}

		void OnUpdatedHeading(object sender, CLHeadingUpdatedEventArgs e)
		{
			if (e.NewHeading.TrueHeading == -1)
				return;

			var p = (position == null) ? new Position() : new Position(position);

			p.Heading = e.NewHeading.TrueHeading;

			position = p;

			OnPositionChanged(new PositionEventArgs(p));
		}

		void OnLocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
		{
			foreach (CLLocation location in e.Locations)
				UpdatePosition(location);

			// defer future location updates if requested
			if ((listenerSettings?.DeferLocationUpdates ?? false) && !deferringUpdates && CanDeferLocationUpdate)
			{
				Manager.AllowDeferredLocationUpdatesUntil(listenerSettings.DeferralDistanceMeters == null ? CLLocationDistance.MaxDistance : listenerSettings.DeferralDistanceMeters.GetValueOrDefault(),
					listenerSettings.DeferralTime == null ? CLLocationManager.MaxTimeInterval : listenerSettings.DeferralTime.GetValueOrDefault().TotalSeconds);

				deferringUpdates = true;
			}
		}

		void OnPositionChanged(PositionEventArgs e) => PositionChanged?.Invoke(this, e);

		void RequestAuthorization()
		{
			var info = NSBundle.MainBundle.InfoDictionary;

			if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
			{
				if (info.ContainsKey(new NSString("NSLocationAlwaysUsageDescription")))
					Manager.RequestAlwaysAuthorization();
				else if (info.ContainsKey(new NSString("NSLocationWhenInUseUsageDescription")))
					Manager.RequestWhenInUseAuthorization();
				else
					throw new UnauthorizedAccessException("On iOS 8.0 and higher you must set either NSLocationWhenInUseUsageDescription or NSLocationAlwaysUsageDescription in your Info.plist file to enable Authorization Requests for Location updates!");
			}
		}
		async void OnPositionError(PositionErrorEventArgs e)
		{
			await StopListeningAsync();
			PositionError?.Invoke(this, e);
		}
		void OnFailed(object sender, NSErrorEventArgs e)
		{
			if ((CLError)(int)e.Error.Code == CLError.Network)
				OnPositionError(new PositionErrorEventArgs(GeolocationError.PositionUnavailable));
		}

		void OnAuthorizationChanged(object sender, CLAuthorizationChangedEventArgs e)
		{
			if (e.Status == CLAuthorizationStatus.Denied || e.Status == CLAuthorizationStatus.Restricted)
				OnPositionError(new PositionErrorEventArgs(GeolocationError.Unauthorized));
		}
	}
}
