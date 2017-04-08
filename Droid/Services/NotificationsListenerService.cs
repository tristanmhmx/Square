using System;
using Android.App;
using Android.Content;
using Android.Gms.Gcm;
using Android.OS;
using Xamarin.Forms;

namespace Square.Droid
{
	[Service(Exported = false), IntentFilter(new[] { "com.google.android.c2dm.intent.RECEIVE" })]
	public class NotificationsListenerService : GcmListenerService
	{
		public override void OnMessageReceived(string from, Bundle data)
		{
			var message = data.GetString("message");
			SendNotification(message);
		}

		void SendNotification(string message)
		{
			var intent = new Intent(this, typeof(MainActivity));
			var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

			var notificationBuilder = new Notification.Builder(this)
				.SetSmallIcon(Resource.Drawable.icon)
				.SetContentTitle("Square")
				.SetContentText(message)
				.SetAutoCancel(true)
				.SetContentIntent(pendingIntent);

			var notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
			notificationManager.Notify(0, notificationBuilder.Build());

			MessagingCenter.Send(new LocationPage("1"), "Navigation");
		}
	}
}
