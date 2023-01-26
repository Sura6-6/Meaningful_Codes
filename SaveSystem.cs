using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem {
    public static void SaveOptions(OptionsMenu optionsMenu) {
        OptionsData optionsData = new OptionsData(optionsMenu);
        File.WriteAllText(Application.dataPath + "/Options.json", JsonUtility.ToJson(optionsData));
    }

    public static OptionsData LoadOptions() {
        string path = Application.dataPath + "/Options.json";
        if (File.Exists(path)) {
            return JsonUtility.FromJson<OptionsData>(File.ReadAllText(path));
        }
        else {
            return null;
        }
    }

    public static void SavePlayer(PlayerController playerController) {
        string path = Application.persistentDataPath + "/Player.btk";

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Create);

        PlayerData playerData = new PlayerData(playerController);

        formatter.Serialize(stream, playerData);
        stream.Close();
    }

    public static PlayerData LoadPlayer() {
        string path = Application.persistentDataPath + "/Player.btk";
        if (File.Exists(path)) {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            PlayerData playerData = formatter.Deserialize(stream) as PlayerData;
            stream.Close();

            return playerData;
        }
        else {
            return null;
        }
    }

    public static void DeletePlayer() {
        string path = Application.persistentDataPath + "/Player.btk";
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    public static bool ExistsPlayer() {
        return File.Exists(Application.persistentDataPath + "/Player.btk");
    }
}