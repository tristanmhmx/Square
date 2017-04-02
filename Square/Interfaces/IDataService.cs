using System;
using SQLite;
namespace Square
{
	public interface IDataService
	{
		SQLiteAsyncConnection GetConnection();
	}
}
