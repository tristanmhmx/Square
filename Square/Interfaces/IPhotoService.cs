using System;
using System.Threading.Tasks;

namespace Square
{
	public interface IPhotoService
	{
		/// <summary>
		/// Property to check if camera is available
		/// </summary>
		bool IsCameraAvailable { get; }
		/// <summary>
		/// Takes a live Photo
		/// </summary>
		/// <returns>Photo</returns>
		/// <exception cref="NotSupportedException">Exception thrown if camera is not available, supported products is null or hashset count is 0</exception>
		Task<byte[]> TakePhotoAsync();
	}
}
