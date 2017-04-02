using System;
using AVFoundation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Square.iOS
{
	public partial class CameraController : UIViewController
	{
		private AVCaptureSession captureSession;
		private AVCaptureDeviceInput captureDeviceInput;
		private UIView liveCameraStream;
		private AVCaptureStillImageOutput stillImageOutput;
		private UIButton takePhotoButton;
		private readonly string apiKey;
		private readonly string[] supportedProducts;
		internal event EventHandler<PhotoEventArgs> PhotoRead;
		private readonly int requestCode;
		private UIView helperView;

		/// <summary>
		/// Initializes Controller
		/// </summary>
		/// <param name="products">Supported Products</param>
		/// <param name="api">Cognitive Api Key</param>
		/// <param name="request">Id of the read request</param>
		public CameraController(int request)
		{
			requestCode = request;
		}

		/// <summary>Finalizer for the NSObject object</summary>
		~CameraController()
		{
			takePhotoButton.TouchUpInside -= CapturePhoto;
		}

		/// <summary>Called after the controller’s <see cref="P:UIKit.UIViewController.View" /> is loaded into memory.</summary>
		/// <remarks>
		///   <para>This method is called after <c>this</c> <see cref="T:UIKit.UIViewController" />'s <see cref="P:UIKit.UIViewController.View" /> and its entire view hierarchy have been loaded into memory. This method is called whether the <see cref="T:UIKit.UIView" /> was loaded from a .xib file or programmatically.</para>
		/// </remarks>
		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			SetupUserInterface();
			SetupEventHandlers();

			AuthorizeCameraUse();
			SetupLiveCameraStream();
		}

		private async void AuthorizeCameraUse()
		{
			var authorizationStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);

			if (authorizationStatus != AVAuthorizationStatus.Authorized)
			{
				await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);
			}
		}

		private void SetupLiveCameraStream()
		{
			captureSession = new AVCaptureSession();

			var videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession)
			{
				Frame = liveCameraStream.Bounds
			};

			liveCameraStream.Layer.AddSublayer(videoPreviewLayer);

			var captureDevice = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);

			ConfigureCameraForDevice(captureDevice);
			captureDeviceInput = AVCaptureDeviceInput.FromDevice(captureDevice);

			stillImageOutput = new AVCaptureStillImageOutput
			{
				OutputSettings = new NSDictionary()
			};

			captureSession.AddOutput(stillImageOutput);
			captureSession.AddInput(captureDeviceInput);
			captureSession.StartRunning();
		}

		private async void CapturePhoto(object sender, EventArgs eventArgs)
		{

			var videoConnection = stillImageOutput.ConnectionFromMediaType(AVMediaType.Video);
			var sampleBuffer = await stillImageOutput.CaptureStillImageTaskAsync(videoConnection);

			var jpegImageAsNsData = AVCaptureStillImageOutput.JpegStillToNSData(sampleBuffer);

			OnPhotoRead(new PhotoEventArgs(requestCode, false, jpegImageAsNsData.ToArray()));

		}

		private static void ConfigureCameraForDevice(AVCaptureDevice device)
		{
			NSError error;
			if (device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
			{
				device.LockForConfiguration(out error);
				device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
				device.UnlockForConfiguration();
			}
			else if (device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
			{
				device.LockForConfiguration(out error);
				device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
				device.UnlockForConfiguration();
			}
			else if (device.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance))
			{
				device.LockForConfiguration(out error);
				device.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
				device.UnlockForConfiguration();
			}
		}


		private void SetupUserInterface()
		{
			var centerButtonX = View.Bounds.GetMidX() - 35f;
			var bottomButtonY = View.Bounds.Bottom - 150;
			var buttonWidth = 70;
			var buttonHeight = 70;

			liveCameraStream = new UIView
			{
				Frame = new CGRect(0f, 0f, View.Bounds.Width, View.Bounds.Height)
			};

			takePhotoButton = new UIButton
			{
				Frame = new CGRect(centerButtonX, bottomButtonY, buttonWidth, buttonHeight)
			};

			takePhotoButton.SetBackgroundImage(UIImage.FromFile("TakePhotoButton.png"), UIControlState.Normal);

			Add(liveCameraStream);
			Add(takePhotoButton);
		}

		private void SetupEventHandlers()
		{
			takePhotoButton.TouchUpInside += CapturePhoto;
		}

		private void OnPhotoRead(PhotoEventArgs e)
		{
			var picked = PhotoRead;
			picked?.Invoke(null, e);
		}
	}
	internal class PhotoEventArgs : EventArgs
	{
		public PhotoEventArgs(int id, Exception error)
		{
			if (error == null)
				throw new ArgumentNullException(nameof(error));

			RequestId = id;
			Error = error;
		}

		public PhotoEventArgs(int id, bool isCanceled, byte[] photo = null)
		{
			RequestId = id;
			IsCanceled = isCanceled;
			if (!IsCanceled && photo == null)
				throw new ArgumentNullException(nameof(photo));

			Photo = photo;
		}

		public int RequestId
		{
			get;
			private set;
		}

		public bool IsCanceled
		{
			get;
		}

		public Exception Error
		{
			get;
		}

		public byte[] Photo
		{
			get;
		}

	}
}

