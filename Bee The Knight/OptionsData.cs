[System.Serializable]
public class OptionsData {
    public int resolution;
    public int screen;
    public int language;
    public float masterVolume;
    public float bgmVolume;
    public float sfxVolume;
    public string rebinds;

    public OptionsData(OptionsMenu optionsMenu) {
        resolution = optionsMenu.resolution;
        screen = optionsMenu.screen;
        language = optionsMenu.language;
        masterVolume = optionsMenu.masterVolume;
        bgmVolume = optionsMenu.bgmVolume;
        sfxVolume = optionsMenu.sfxVolume;
        rebinds = optionsMenu.rebinds;
    }
}