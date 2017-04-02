using System;
using System.IO;
using SQLite;
using Square.Droid;
using Xamarin.Forms;

[assembly: Dependency(typeof(DataService))]
namespace Square.Droid
{
	public class DataService : IDataService
	{
		public SQLiteAsyncConnection GetConnection()
		{
			const string fileName = "ERMXBEN.db";
			var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			var path = Path.Combine(documentsPath, fileName);
			var connection = new SQLiteAsyncConnection(path);
			return connection;
		}
	}
}
