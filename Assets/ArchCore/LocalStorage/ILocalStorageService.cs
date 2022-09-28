namespace ArchCore.LocalStorage
{
	public interface ILocalStorageService
	{
		void Save<T>(string path, T data);
		T Load<T>(string path);

		void ClearAll();
		void Delete(string path);
		bool Has(string path);
	}
}