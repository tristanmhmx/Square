using System;
using System.Threading;
using System.Threading.Tasks;
using Square.iOS;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(PhotoService))]
namespace Square.iOS
{
	public class PhotoService : IPhotoService
	{
		
		private UINavigationController navigationController;
		private int requestId;
		private TaskCompletionSource<byte[]> completionSource;
		private CameraController cameraController;

		public PhotoService()
		{
			navigationController = FindNavigationController();
			IsCameraAvailable = UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera);
		}

		public bool IsCameraAvailable
		{
			get;
		}

		public async Task<byte[]> TakePhotoAsync()
		{
			if (!IsCameraAvailable)
				throw new NotSupportedException("This device does not has a camera available");
			return await TakeAsync();
		}

		private Task<byte[]> TakeAsync()
		{
			var window = UIApplication.SharedApplication.KeyWindow;

			if (window == null)
			{
				throw new InvalidOperationException("There's no current active window");
			}

			if (navigationController == null)
			{
				throw new InvalidOperationException("There's no current navigation controller");
			}

			var id = GetRequestId();
			var ntcs = new TaskCompletionSource<byte[]>(id);
			if (Interlocked.CompareExchange(ref completionSource, ntcs, null) != null)
				throw new InvalidOperationException("Only one operation can be active at a time");

			cameraController = new CameraController(id);

			navigationController.PresentModalViewController(cameraController, true);

			EventHandler<PhotoEventArgs> handler = null;
			handler = (s, e) =>
			{
				var tcs = Interlocked.Exchange(ref completionSource, null);
				cameraController.PhotoRead -= handler;
				if (e.RequestId != id)
				{
					navigationController.DismissModalViewController(true);
					return;
				}
				if (e.IsCanceled)
				{
					navigationController.DismissModalViewController(true);
					tcs.SetResult(null);
				}
				else if (e.Error != null)
				{
					navigationController.DismissModalViewController(true);
					tcs.SetException(e.Error);
				}
				else
				{
					navigationController.DismissModalViewController(true);
					tcs.SetResult(e.Photo);
				}
			};

			cameraController.PhotoRead += handler;

			return completionSource.Task;
		}

		private int GetRequestId()
		{
			var id = requestId;
			if (requestId == int.MaxValue)
				requestId = 0;
			else
				requestId++;

			return id;
		}

		private UINavigationController FindNavigationController()
		{
			//Check to see if the roomviewcontroller is the navigationcontroller.
			foreach (var window in UIApplication.SharedApplication.Windows)
			{
				if (window.RootViewController.NavigationController != null)
					return window.RootViewController.NavigationController;
				var val = CheckSubs(window.RootViewController.ChildViewControllers);
				if (val != null)
					return val;
			}

			return null;
		}

		private UINavigationController CheckSubs(UIViewController[] controllers)
		{
			foreach (var controller in controllers)
			{
				//Check to see if the one of the childs is the navigationcontroller.
				if (controller.NavigationController != null)
					return controller.NavigationController;
				var val = CheckSubs(controller.ChildViewControllers);
				if (val != null)
					return val;
			}
			return null;
		}
	}
}
