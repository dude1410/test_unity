﻿using System.Collections;
using System.Collections.Generic;
using ArchCore.Networking.Rest.Converter;
using UnityEngine;

namespace ArchCore.LocalStorage
{
	public class PlayerPrefsService : ILocalStorageService
	{
		private readonly IObjectConverter converter;

		public PlayerPrefsService(IObjectConverter converter)
		{
			this.converter = converter;
		}
		
		public void Save<T>(string path, T data)
		{
			PlayerPrefs.SetString(path, converter.ToString(data));
		}

		public T Load<T>(string path)
		{
			var data = PlayerPrefs.GetString(path);
			return converter.ToObject<T>(data);
		}

		public void ClearAll()
		{
			PlayerPrefs.DeleteAll();
		}

		public void Delete(string path)
		{
			PlayerPrefs.DeleteKey(path);
		}

		public bool Has(string path)
		{
			return PlayerPrefs.HasKey(path);
		}
	}


}
