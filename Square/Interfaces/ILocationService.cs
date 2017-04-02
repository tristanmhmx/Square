using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms.Maps;

namespace Square
{
	public interface ILocationService
	{
		bool IsListening { get; }
		bool IsGeolocationAvailable { get; }
		bool IsGeolocationEnabled { get; }
		Task<Position> GetLocationAsync(TimeSpan? timeout, CancellationToken? cancelToken = null, bool includeHeading = false);
		event EventHandler<PositionEventArgs> PositionChanged;
		Task<bool> StartListeningAsync(TimeSpan minimumTime, double minimumDistance, bool includeHeading = false, ListenerSettings listenerSettings = null);
		Task<bool> StopListeningAsync();
	}
}
