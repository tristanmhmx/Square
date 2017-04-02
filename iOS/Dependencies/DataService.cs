using System;
using System.IO;
using SQLite;
using Square.iOS;
using Xamarin.Forms;

[assembly: Dependency(typeof(DataService))]
namespace Square.iOS
{
	public class DataService : IDataService
	{
		public SQLiteAsyncConnection GetConnection()
		{
			const string fileName = "ERMXBEN.db";
			var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			var libraryPath = Path.Combine(documentsPath, "..", "Library");
			var path = Path.Combine(libraryPath, fileName);
			var connection = new SQLiteAsyncConnection(path);
			return connection;
		}
	}
}
