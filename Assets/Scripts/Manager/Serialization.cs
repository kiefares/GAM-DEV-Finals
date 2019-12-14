using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
public class Save
{
	public Save()
	{
		// do stuff here
	}
}

namespace Manager
{
	public class Serialization : Singleton<Serialization>
	{
		public const string SAVENAME = "SaveData";

		public static void SaveGame(int n)
		{
			Save save = new Save();

			BinaryFormatter bf = new BinaryFormatter();
			FileStream fs = File.Create($"{Application.persistentDataPath}/{SAVENAME}{n}.save");

			bf.Serialize(fs, save);
			fs.Close();
		}

		public static void LoadGame(int n)
		{
			string path = $"{Application.persistentDataPath}/{SAVENAME}{n}.save";

			if (!File.Exists(path))
				return;

			BinaryFormatter bf = new BinaryFormatter();
			FileStream fs = File.Open(path, FileMode.Open);
			Save save = (Save)bf.Deserialize(fs);

			fs.Close();
		}
	}
}