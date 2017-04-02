using System;
using System.ComponentModel;
using CoreAnimation;
using Square;
using Square.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(BorderedEntry), typeof(BorderedEntryRenderer))]
namespace Square.iOS
{
	public class BorderedEntryRenderer : EntryRenderer
	{
		CALayer borderLayer;

		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
		{
			base.OnElementChanged(e);
			if (Control != null)
			{
				Control.BorderStyle = UITextBorderStyle.None;
				var view = (Element as BorderedEntry);
				if (view != null)
				{
					DrawBorder(view);
				}
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);
			var view = (Element as BorderedEntry);
			if (view != null && e.PropertyName.Equals("Width"))
			{
				DrawBorder(view);
			}

		}

		void DrawBorder(BorderedEntry view)
		{
			if (borderLayer != null)
				borderLayer.RemoveFromSuperLayer();

			borderLayer = new CALayer
			{
				MasksToBounds = true,
				Frame = new CoreGraphics.CGRect(0f, (Frame.Height / 2) + 5, Frame.Width, 1f),
				BorderColor = view.BorderColor.ToCGColor(),
				BorderWidth = 1.0f,
			};

			Control.Layer.AddSublayer(borderLayer);

			Control.BorderStyle = UITextBorderStyle.None;
		}
	}
}
