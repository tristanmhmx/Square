using System;
using Android.App;
using Android.Content;
using Android.Gms.Gcm;
using Android.Gms.Iid;

namespace Square.Droid
{
	[Service(Exported = false)]
	public class NotificationsIntentService : IntentService
	{
		private static object locker = new object();

		public NotificationsIntentService() : base("RegistrationIntentService") { }

		protected override void OnHandleIntent(Intent intent)
		{
			try
			{
				lock (locker)
				{
					var instanceId = InstanceID.GetInstance(this);
					var token = instanceId.GetToken(
						"792341316392", GoogleCloudMessaging.InstanceIdScope, null);

					SendRegistrationToAppServer(token);

					Subscribe(token);
				}
			}
			catch (System.Exception e)
			{

			}
		}

		private static void SendRegistrationToAppServer(string token)
		{
			// Add custom implementation here as needed.
		}

		void Subscribe(string token)
		{
			var pubSub = GcmPubSub.GetInstance(this);
			pubSub.Subscribe(token, "/topics/global", null);

							 }
	}
	
}
