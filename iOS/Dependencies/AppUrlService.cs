using System;
using Square.iOS;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(AppUrlService))]
namespace Square.iOS
{
	public class AppUrlService : IAppUrlService
	{
		public void OpenUrl(string url)
		{
			if (UIApplication.SharedApplication.CanOpenUrl(new Foundation.NSUrl(url)))
			{
				UIApplication.SharedApplication.OpenUrl(new Foundation.NSUrl(url));
			}
		}
	}
}
