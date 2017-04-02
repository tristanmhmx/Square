using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Square
{
	public partial class LocationPage : ContentPage
	{
		public LocationPage(string id)
		{
			BindingContext = new LocationViewModel(id);
			InitializeComponent();
		}
	}
}
