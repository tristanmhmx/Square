using System;

namespace Square
{
	public class Position
	{
		public Position()
		{
		}

		public Position(Position position)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			Timestamp = position.Timestamp;
			Latitude = position.Latitude;
			Longitude = position.Longitude;
			Altitude = position.Altitude;
			AltitudeAccuracy = position.AltitudeAccuracy;
			Accuracy = position.Accuracy;
			Heading = position.Heading;
			Speed = position.Speed;
		}

		public DateTimeOffset Timestamp
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the latitude.
		/// </summary>
		public double Latitude
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the longitude.
		/// </summary>
		public double Longitude
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the altitude in meters relative to sea level.
		/// </summary>
		public double Altitude
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the potential position error radius in meters.
		/// </summary>
		public double Accuracy
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the potential altitude error range in meters.
		/// </summary>
		/// <remarks>
		/// Not supported on Android, will always read 0.
		/// </remarks>
		public double AltitudeAccuracy
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the heading in degrees relative to true North.
		/// </summary>
		public double Heading
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the speed in meters per second.
		/// </summary>
		public double Speed
		{
			get;
			set;
		}
	}

	/// <summary>
	/// Position args
	/// </summary>
	public class PositionEventArgs
	  : EventArgs
	{
		/// <summary>
		/// Position args
		/// </summary>
		/// <param name="position"></param>
		public PositionEventArgs(Position position)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			Position = position;
		}

		/// <summary>
		/// The Position
		/// </summary>
		public Position Position
		{
			get;
			private set;
		}
	}

	/// <summary>
	/// Location exception
	/// </summary>
	public class GeolocationException
	  : Exception
	{
		/// <summary>
		/// Location exception
		/// </summary>
		/// <param name="error"></param>
		public GeolocationException(GeolocationError error)
		  : base("A geolocation error occured: " + error)
		{
			if (!Enum.IsDefined(typeof(GeolocationError), error))
				throw new ArgumentException("error is not a valid GelocationError member", "error");

			Error = error;
		}

		/// <summary>
		/// Geolocation error
		/// </summary>
		/// <param name="error"></param>
		/// <param name="innerException"></param>
		public GeolocationException(GeolocationError error, Exception innerException)
		  : base("A geolocation error occured: " + error, innerException)
		{
			if (!Enum.IsDefined(typeof(GeolocationError), error))
				throw new ArgumentException("error is not a valid GelocationError member", "error");

			Error = error;
		}

		//The error
		public GeolocationError Error
		{
			get;
			private set;
		}
	}

	/// <summary>
	/// Error ARgs
	/// </summary>
	public class PositionErrorEventArgs
	  : EventArgs
	{
		/// <summary>
		/// Constructor for event error args
		/// </summary>
		/// <param name="error"></param>
		public PositionErrorEventArgs(GeolocationError error)
		{
			Error = error;
		}

		/// <summary>
		/// The Error
		/// </summary>
		public GeolocationError Error
		{
			get;
			private set;
		}
	}

	/// <summary>
	/// Error for geolocator
	/// </summary>
	public enum GeolocationError
	{
		/// <summary>
		/// The provider was unable to retrieve any position data.
		/// </summary>
		PositionUnavailable,

		/// <summary>
		/// The app is not, or no longer, authorized to receive location data.
		/// </summary>
		Unauthorized
	}
	public class ListenerSettings
	{
		/// <summary>
		/// Gets or sets whether background location updates should be allowed (>= iOS 9). Default:  false
		/// </summary>
		public bool AllowBackgroundUpdates { get; set; } = false;

		/// <summary>
		/// Gets or sets whether location updates should be paused automatically when the location is unlikely to change (>= iOS 6). Default:  true
		/// </summary>
		public bool PauseLocationUpdatesAutomatically { get; set; } = true;

		/// <summary>
		/// Gets or sets the activity type that should be used to determine when to automatically pause location updates (>= iOS 6). Default:  ActivityType.Other
		/// </summary>
		public ActivityType ActivityType { get; set; } = ActivityType.Other;

		/// <summary>
		/// Gets or sets whether the location manager should only listen for significant changes in location, rather than continuous listening (>= iOS 4). Default:  false
		/// </summary>
		public bool ListenForSignificantChanges { get; set; } = false;

		/// <summary>
		/// Gets or sets whether the location manager should defer location updates until an energy efficient time arrives, or distance and time criteria are met (>= iOS 6). Default:  false
		/// </summary>
		public bool DeferLocationUpdates { get; set; } = false;

		/// <summary>
		/// If deferring location updates, the minimum distance to travel before updates are delivered (>= iOS 6). Set to null for indefinite wait. Default:  500
		/// </summary>
		public double? DeferralDistanceMeters { get; set; } = 500;

		/// <summary>
		/// If deferring location updates, the minimum time that should elapse before updates are delivered (>= iOS 6). Set to null for indefinite wait. Default:  5 minutes
		/// </summary>
		/// <value>The time between updates (default:  5 minutes).</value>
		public TimeSpan? DeferralTime { get; set; } = TimeSpan.FromMinutes(5);
	}
	public enum ActivityType
	{
		/// <summary>
		/// GPS is being used for an unknown activity.
		/// </summary>
		Other,

		/// <summary>
		/// GPS is being used specifically during vehicular navigation to track location changes to the automobile. This activity might cause location updates to be paused only when the vehicle does not move for an extended period of time.
		/// </summary>
		AutomotiveNavigation,

		/// <summary>
		/// GPS is being used to track any pedestrian-related activity. This activity might cause location updates to be paused only when the user does not move a significant distance over a period of time.
		/// </summary>
		Fitness,

		/// <summary>
		/// GPS is being used to track movements for other types of vehicular navigation that are not automobile related. For example, you would use this to track navigation by boat, train, or plane. Do not use this type for pedestrian navigation tracking. This activity might cause location updates to be paused only when the vehicle does not move a significant distance over a period of time.
		/// </summary>
		OtherNavigation
	}
}
