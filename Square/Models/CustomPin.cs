using System;
using SQLite;
using Xamarin.Forms.Maps;

namespace Square
{
	public class CustomPin
	{

		public int Identifier { get; set; }
		public string Id { get; set; }
		public Pin Pin { get; set; }
	}
	public class Location
	{
		[PrimaryKey, AutoIncrement]
		public int Id 
		{ 
			get; 
			set; 
		}
		public string MapId 
		{ 
			get; 
			set; 
		}
		public string Label
		{
			get;
			set;
		}
		public string Address
		{
			get;
			set;
		}
		public double Latitude
		{
			get;
			set;
		}
		public double Longitude
		{
			get;
			set;
		}
	}
}
