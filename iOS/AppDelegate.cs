using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using Xamarin.Forms;

namespace Square.iOS
{
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			global::Xamarin.Forms.Forms.Init();
			Xamarin.FormsMaps.Init();

			// Code for starting up the Xamarin Test Cloud Agent
#if ENABLE_TEST_CLOUD
			Xamarin.Calabash.Start();
#endif

			LoadApplication(new App());

			if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
			{
				var pushSettings = UIUserNotificationSettings.GetSettingsForTypes(
								   UIUserNotificationType.Alert |
								   UIUserNotificationType.Badge | UIUserNotificationType.Sound,
								   new NSSet());

				UIApplication.SharedApplication.RegisterUserNotificationSettings(pushSettings);
				UIApplication.SharedApplication.RegisterForRemoteNotifications();
			}
			else
			{
				var notificationTypes = UIRemoteNotificationType.Alert |
										UIRemoteNotificationType.Badge |
										UIRemoteNotificationType.Sound;
				UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);
			}

			var keyName = new NSString("UIApplicationLaunchOptionsRemoteNotificationKey");
            if (options?.Keys != null && options.Keys.Length != 0 && options.ContainsKey(keyName))
            {
                var pushOptions = options.ObjectForKey(keyName) as NSDictionary;

				MessagingCenter.Send(new LocationPage("1"), "Navigation");
            }

			return base.FinishedLaunching(app, options);
		}

		/// <summary>
		/// Receiveds the remote notification when app is closed
		/// </summary>
		/// <param name="application">Application.</param>
		/// <param name="userInfo">User info.</param>
		public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
		{
			UIApplication.SharedApplication.ApplicationIconBadgeNumber = 1;
			base.ReceivedRemoteNotification(application, userInfo);
		}

		/// <summary>
		/// Registereds for remote notifications.
		/// </summary>
		/// <param name="application">Application.</param>
		/// <param name="deviceToken">Device token.</param>
		public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
		{
			// Get current device token
			var token = deviceToken.Description;
			if (!string.IsNullOrWhiteSpace(token))
			{
				token = token.Trim('<').Trim('>');
			}
			// Save new device token 
		}

		public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
		{
			base.FailedToRegisterForRemoteNotifications(application, error);
		}

		/// <summary>
		/// Dids the receive remote notification when app launched
		/// </summary>
		/// <param name="application">Application.</param>
		/// <param name="userInfo">User info.</param>
		/// <param name="completionHandler">Completion handler.</param>
		public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
		{
			UIApplication.SharedApplication.ApplicationIconBadgeNumber = 1;
			base.DidReceiveRemoteNotification(application, userInfo, completionHandler);
		}
	}
}
