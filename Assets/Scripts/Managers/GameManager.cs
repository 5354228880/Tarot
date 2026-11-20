using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MAINMENU,
    LEVELSELECT,
    INGAME,
    PAUSE,
    END
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public const string SCENE_MENU = "Menu";
    public const string SCENE_LEVELSELECT = "LevelSelect"; 
    public string[] LevelScenesName;
    public int thisLevelIndex;

    [Header("Settings")]
    public Resolution[] resolutions;
    public int resolutionIndex = 12;
    public int volume;
    public bool fullScreen;

    private GameState currentState;
    public GameState CurrentState
    {
        get{return currentState;}
        set
        {
            currentState = value;
            HandleCurrentState();
        }
    }

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);
        InitSettings();
    }

    private void Start()
    {
    }

    private void InitSettings()
    {
        resolutions = Screen.resolutions;

        if (resolutions == null || resolutions.Length == 0)
        {
            Resolution fallback = new Resolution();
            fallback.width = Screen.width;
            fallback.height = Screen.height;
            resolutions = new Resolution[] { fallback };
        }

        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);
        volume = 0;
        fullScreen = false;
    }

    public void ApplySettings(int resolutionIndex, int volume, bool fullScreen)
    {
        this.resolutionIndex = resolutionIndex;
        this.volume = volume;
        this.fullScreen = fullScreen;

        Resolution res = resolutions[this.resolutionIndex];
        Screen.SetResolution(res.width, res.height, this.fullScreen);
        //volume: display-only for now
    }

    private void HandleCurrentState()
    {
        HandleTimeScaleOnSceneLoaded();
    }

    private void HandleTimeScaleOnSceneLoaded()
    {
        switch (instance.currentState)
        {
            case GameState.PAUSE:
            Time.timeScale = 0f;
            break;
            case GameState.MAINMENU:
            case GameState.LEVELSELECT:
            case GameState.END:
            case GameState.INGAME:
            Time.timeScale = 1f;
            break;
        }
    }
}
