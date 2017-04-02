using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Square
{
	public class BaseViewModel<T> : INotifyPropertyChanged where T : new()
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public T Model { get; set; }
		public BaseViewModel()
		{
			Model = new T();
		}
		internal void SetPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
