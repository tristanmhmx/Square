using System;
using Android.App;
using Android.Content;
using Android.Gms.Iid;

namespace Square.Droid
{
	[Service(Exported = false), IntentFilter(new[] { "com.google.android.gms.iid.InstanceID" })]
	public class NotificationsInstanceIDListenerService : InstanceIDListenerService
	{
		public override void OnTokenRefresh()
		{
			var intent = new Intent(this, typeof(NotificationsIntentService));
			StartService(intent);   
		}
	}
}
