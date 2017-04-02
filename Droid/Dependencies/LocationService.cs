using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;

namespace Square.Droid
{
	public class LocationService : ILocationService
	{
		string[] allProviders;
		LocationManager locationManager;

		GeolocationContinuousListener listener;

		string[] Providers => Manager.GetProviders(false).ToArray();
		string[] IgnoredProviders => new string[] { LocationManager.PassiveProvider, "local_database" };

		readonly object positionSync = new object();
		Position lastPosition;

		LocationManager Manager
		{
			get
			{
				if (locationManager == null)
					locationManager = (LocationManager)Application.Context.GetSystemService(Context.LocationService);

				return locationManager;
			}
		}

		public bool IsGeolocationAvailable => Providers.Length > 0;

		public bool IsGeolocationEnabled => Providers.Any(p => !IgnoredProviders.Contains(p) && Manager.IsProviderEnabled(p));

		/// <inheritdoc/>
		public event EventHandler<PositionErrorEventArgs> PositionError;
		/// <inheritdoc/>
		public event EventHandler<PositionEventArgs> PositionChanged;
		/// <inheritdoc/>
		public bool IsListening => listener != null;

		public async Task<Position> GetLocationAsync(TimeSpan? timeout, CancellationToken? cancelToken = default(CancellationToken?), bool includeHeading = false)
		{
			var timeoutMilliseconds = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : Timeout.Infinite;

			if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
				throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0");

			if (!cancelToken.HasValue)
				cancelToken = CancellationToken.None;

			var hasPermission = await CheckPermissions();
			if (!hasPermission)
				return null;

			var tcs = new TaskCompletionSource<Position>();

			if (!IsListening)
			{
				var providers = Providers;
				GeolocationSingleListener singleListener = null;
				singleListener = new GeolocationSingleListener(Manager, 100, timeoutMilliseconds, providers.Where(Manager.IsProviderEnabled),
					finishedCallback: () =>
				{
					for (int i = 0; i < providers.Length; ++i)
						Manager.RemoveUpdates(singleListener);
				});

				if (cancelToken != CancellationToken.None)
				{
					cancelToken.Value.Register(() =>
					{
						singleListener.Cancel();

						for (int i = 0; i < providers.Length; ++i)
							Manager.RemoveUpdates(singleListener);
					}, true);
				}

				try
				{
					var looper = Looper.MyLooper() ?? Looper.MainLooper;

					int enabled = 0;
					for (int i = 0; i < providers.Length; ++i)
					{
						if (Manager.IsProviderEnabled(providers[i]))
							enabled++;

						Manager.RequestLocationUpdates(providers[i], 0, 0, singleListener, looper);
					}

					if (enabled == 0)
					{
						for (int i = 0; i < providers.Length; ++i)
							Manager.RemoveUpdates(singleListener);

						tcs.SetException(new GeolocationException(GeolocationError.PositionUnavailable));
						return await tcs.Task;
					}
				}
				catch (Java.Lang.SecurityException ex)
				{
					tcs.SetException(new GeolocationException(GeolocationError.Unauthorized, ex));
					return await tcs.Task;
				}

				return await singleListener.Task;
			}

			// If we're already listening, just use the current listener
			lock (positionSync)
			{
				if (lastPosition == null)
				{
					if (cancelToken != CancellationToken.None)
					{
						cancelToken.Value.Register(() => tcs.TrySetCanceled());
					}

					EventHandler<PositionEventArgs> gotPosition = null;
					gotPosition = (s, e) =>
					{
						tcs.TrySetResult(e.Position);
						PositionChanged -= gotPosition;
					};

					PositionChanged += gotPosition;
				}
				else
				{
					tcs.SetResult(lastPosition);
				}
			}

			return await tcs.Task;
		}

		public async Task<bool> StartListeningAsync(TimeSpan minTime, double minDistance, bool includeHeading = false, ListenerSettings listenerSettings = null)
		{
			var hasPermission = await CheckPermissions();
			if (!hasPermission)
				return false;


			var minTimeMilliseconds = minTime.TotalMilliseconds;
			if (minTimeMilliseconds < 0)
				throw new ArgumentOutOfRangeException("minTime");
			if (minDistance < 0)
				throw new ArgumentOutOfRangeException("minDistance");
			if (IsListening)
				throw new InvalidOperationException("This Geolocator is already listening");

			var providers = Providers;
			listener = new GeolocationContinuousListener(Manager, minTime, providers);
			listener.PositionChanged += OnListenerPositionChanged;
			listener.PositionError += OnListenerPositionError;

			Looper looper = Looper.MyLooper() ?? Looper.MainLooper;
			for (int i = 0; i < providers.Length; ++i)
				Manager.RequestLocationUpdates(providers[i], (long)minTimeMilliseconds, (float)minDistance, listener, looper);

			return true;
		}

		public Task<bool> StopListeningAsync()
		{
			if (listener == null)
				return Task.FromResult(true);

			var providers = Providers;
			listener.PositionChanged -= OnListenerPositionChanged;
			listener.PositionError -= OnListenerPositionError;

			for (int i = 0; i < providers.Length; ++i)
				Manager.RemoveUpdates(listener);

			listener = null;
			return Task.FromResult(true);
		}

		async Task<bool> CheckPermissions()
		{
			//TODO: Request Permissions
			return true;
		}

		private void OnListenerPositionChanged(object sender, PositionEventArgs e)
		{
			if (!IsListening) // ignore anything that might come in afterwards
				return;

			lock (positionSync)
			{
				lastPosition = e.Position;

				PositionChanged?.Invoke(this, e);
			}
		}
		/// <inheritdoc/>
		private async void OnListenerPositionError(object sender, PositionErrorEventArgs e)
		{
			await StopListeningAsync();

			PositionError?.Invoke(this, e);
		}
	}

	#region Listener
	internal class GeolocationContinuousListener
	  : Java.Lang.Object, ILocationListener
	{
		IList<string> providers;
		readonly HashSet<string> activeProviders = new HashSet<string>();
		readonly LocationManager manager;

		string activeProvider;
		Android.Locations.Location lastLocation;
		TimeSpan timePeriod;

		public GeolocationContinuousListener(LocationManager manager, TimeSpan timePeriod, IList<string> providers)
		{
			this.manager = manager;
			this.timePeriod = timePeriod;
			this.providers = providers;

			foreach (string p in providers)
			{
				if (manager.IsProviderEnabled(p))
					activeProviders.Add(p);
			}
		}

		public event EventHandler<PositionErrorEventArgs> PositionError;
		public event EventHandler<PositionEventArgs> PositionChanged;

		public void OnLocationChanged(Android.Locations.Location location)
		{
			if (location.Provider != activeProvider)
			{
				if (activeProvider != null && manager.IsProviderEnabled(activeProvider))
				{
					var pr = manager.GetProvider(location.Provider);
					var lapsed = GetTimeSpan(location.Time) - GetTimeSpan(lastLocation.Time);

					if (pr.Accuracy > manager.GetProvider(activeProvider).Accuracy
					  && lapsed < timePeriod.Add(timePeriod))
					{
						location.Dispose();
						return;
					}
				}

				activeProvider = location.Provider;
			}

			var previous = Interlocked.Exchange(ref lastLocation, location);
			if (previous != null)
				previous.Dispose();


			PositionChanged?.Invoke(this, new PositionEventArgs(location.ToPosition()));
		}

		public void OnProviderDisabled(string provider)
		{
			if (provider == LocationManager.PassiveProvider)
				return;

			lock (activeProviders)
			{
				if (activeProviders.Remove(provider) && activeProviders.Count == 0)
					OnPositionError(new PositionErrorEventArgs(GeolocationError.PositionUnavailable));
			}
		}

		public void OnProviderEnabled(string provider)
		{
			if (provider == LocationManager.PassiveProvider)
				return;

			lock (activeProviders)
				activeProviders.Add(provider);
		}

		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			switch (status)
			{
				case Availability.Available:
					OnProviderEnabled(provider);
					break;

				case Availability.OutOfService:
					OnProviderDisabled(provider);
					break;
			}
		}

		private TimeSpan GetTimeSpan(long time) => new TimeSpan(TimeSpan.TicksPerMillisecond * time);


		private void OnPositionError(PositionErrorEventArgs e) => PositionError?.Invoke(this, e);

	}
	internal class GeolocationSingleListener
	   : Java.Lang.Object, ILocationListener
	{

		readonly object locationSync = new object();
		Android.Locations.Location bestLocation;


		readonly Action finishedCallback;
		readonly float desiredAccuracy;
		readonly Timer timer;
		readonly TaskCompletionSource<Position> completionSource = new TaskCompletionSource<Position>();
		HashSet<string> activeProviders = new HashSet<string>();

		public GeolocationSingleListener(LocationManager manager, float desiredAccuracy, int timeout, IEnumerable<string> activeProviders, Action finishedCallback)
		{
			this.desiredAccuracy = desiredAccuracy;
			this.finishedCallback = finishedCallback;

			this.activeProviders = new HashSet<string>(activeProviders);

			foreach (var provider in activeProviders)
			{
				var location = manager.GetLastKnownLocation(provider);
				if (location != null && GeolocationUtils.IsBetterLocation(location, bestLocation))
					bestLocation = location;
			}


			if (timeout != Timeout.Infinite)
				timer = new Timer(TimesUp, null, timeout, 0);
		}

		public Task<Position> Task => completionSource.Task;


		public void OnLocationChanged(Android.Locations.Location location)
		{
			if (location.Accuracy <= desiredAccuracy)
			{
				Finish(location);
				return;
			}

			lock (locationSync)
			{
				if (GeolocationUtils.IsBetterLocation(location, bestLocation))
					bestLocation = location;
			}
		}



		public void OnProviderDisabled(string provider)
		{
			lock (activeProviders)
			{
				if (activeProviders.Remove(provider) && activeProviders.Count == 0)
					completionSource.TrySetException(new GeolocationException(GeolocationError.PositionUnavailable));
			}
		}

		public void OnProviderEnabled(string provider)
		{
			lock (activeProviders)
				activeProviders.Add(provider);
		}

		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			switch (status)
			{
				case Availability.Available:
					OnProviderEnabled(provider);
					break;

				case Availability.OutOfService:
					OnProviderDisabled(provider);
					break;
			}
		}

		public void Cancel() => completionSource.TrySetCanceled();

		private void TimesUp(object state)
		{
			lock (locationSync)
			{
				if (bestLocation == null)
				{
					if (completionSource.TrySetCanceled())
						finishedCallback?.Invoke();
				}
				else
				{
					Finish(bestLocation);
				}
			}
		}

		private void Finish(Android.Locations.Location location)
		{
			finishedCallback?.Invoke();
			completionSource.TrySetResult(location.ToPosition());
		}
	}
	public static class GeolocationUtils
	{

		static int TwoMinutes = 120000;

		internal static bool IsBetterLocation(Android.Locations.Location location, Android.Locations.Location bestLocation)
		{

			if (bestLocation == null)
				return true;

			var timeDelta = location.Time - bestLocation.Time;
			var isSignificantlyNewer = timeDelta > TwoMinutes;
			var isSignificantlyOlder = timeDelta < -TwoMinutes;
			var isNewer = timeDelta > 0;

			if (isSignificantlyNewer)
				return true;

			if (isSignificantlyOlder)
				return false;

			var accuracyDelta = (int)(location.Accuracy - bestLocation.Accuracy);
			var isLessAccurate = accuracyDelta > 0;
			var isMoreAccurate = accuracyDelta < 0;
			var isSignificantlyLessAccurage = accuracyDelta > 200;

			var isFromSameProvider = IsSameProvider(location.Provider, bestLocation.Provider);

			if (isMoreAccurate)
				return true;

			if (isNewer && !isLessAccurate)
				return true;

			if (isNewer && !isSignificantlyLessAccurage && isFromSameProvider)
				return true;

			return false;


		}

		internal static bool IsSameProvider(string provider1, string provider2)
		{
			if (provider1 == null)
				return provider2 == null;

			return provider1.Equals(provider2);
		}

		internal static Position ToPosition(this Android.Locations.Location location)
		{
			var p = new Position();
			if (location.HasAccuracy)
				p.Accuracy = location.Accuracy;
			if (location.HasAltitude)
				p.Altitude = location.Altitude;
			if (location.HasBearing)
				p.Heading = location.Bearing;
			if (location.HasSpeed)
				p.Speed = location.Speed;

			p.Longitude = location.Longitude;
			p.Latitude = location.Latitude;
			p.Timestamp = location.GetTimestamp();
			return p;
		}



		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		internal static DateTimeOffset GetTimestamp(this Android.Locations.Location location)
		{
			try
			{
				return new DateTimeOffset(Epoch.AddMilliseconds(location.Time));
			}
			catch (Exception e)
			{
				return new DateTimeOffset(Epoch);
			}
		}
	}
	#endregion
}
