using System;
using Xamarin.Forms;

namespace Square
{
	public class BorderedEntry : Entry
	{
		public BindableProperty BorderColorProperty = BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(BorderedEntry), Color.Navy);

		public Color BorderColor
		{
			get { return (Color)GetValue(BorderColorProperty); }
			set { SetValue(BorderColorProperty, value); }
		}
	}
}
