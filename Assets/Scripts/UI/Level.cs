using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Level : MonoBehaviour
{
    public CanvasGroup pausedCg;
    public CanvasGroup endCg;
    public RectTransform pausedButton;
    public SettingsPanel settingsPanel;

    void Start()
    {
        TogglePausedButton(true);
        if(settingsPanel != null)
        {
            settingsPanel.onClosed.AddListener(OnSettingsClosed);
        }
    }

    public void OnSettings()
    {
        Sequence s = DOTween.Sequence();
        s.Append(ToggleCg(pausedCg,false));
        s.OnComplete(()=>settingsPanel.Open());
        s.SetUpdate(true).Play();
    }

    private void OnSettingsClosed()
    {
        ToggleCg(pausedCg,true).SetUpdate(true);
    }

    public void OnPaused()
    {
        Sequence s = DOTween.Sequence();
        s.Join(ToggleCg(pausedCg,true));
        s.Join(TogglePausedButton(false));
        s.OnComplete(()=>{GameManager.instance.CurrentState = GameState.PAUSE;});
        s.Play().SetUpdate(true);
    }

    public void OnResume()
    {
        Debug.Log("Resum");
        Sequence s = DOTween.Sequence();
        s.Join(ToggleCg(pausedCg,false));
        s.Join(TogglePausedButton(true));
        s.OnComplete(()=>{GameManager.instance.CurrentState = GameState.INGAME;});
        s.Play().SetUpdate(true);
    }

    public void OnNext()
    {
        ToggleAllCg_Level(false).SetUpdate(true).OnComplete(() =>
        {
            GameManager.instance.CurrentState = GameState.INGAME;
            GameManager.instance.thisLevelIndex = GameManager.instance.thisLevelIndex + 1;
            if(GameManager.instance.thisLevelIndex < GameManager.instance.LevelScenesName.Length)
            {
                SceneManager.LoadScene(GameManager.instance.LevelScenesName[GameManager.instance.thisLevelIndex]);
            }
            else
            {
                SceneManager.LoadScene(GameManager.SCENE_MENU);
            }
        });
    }

    public void OnExit()
    {
        ToggleAllCg_Level(false).SetUpdate(true).OnComplete(()=>{
            GameManager.instance.CurrentState = GameState.LEVELSELECT;
            SceneManager.LoadScene(GameManager.SCENE_LEVELSELECT);
        });
    }

    private Tween ToggleAllCg_Level(bool open)
    {
        Sequence s = DOTween.Sequence();
        s.Join(ToggleCg(pausedCg,open));
        s.Join(ToggleCg(endCg,open));
        s.Join(TogglePausedButton(open));

        return s;
    }

    private Tween TogglePausedButton(bool open)
    {
        Tween t = pausedButton.gameObject.GetComponent<Image>().DOFade(open?1f:0f,0.2f);
        return open?
            t.OnStart   (()=>{pausedButton.gameObject.SetActive(open);}):
            t.OnComplete(()=>{pausedButton.gameObject.SetActive(open);});
    }

    public static Tween ToggleCg(CanvasGroup cg,bool open)
    {
        return cg.DOFade(open? 1f : 0f, 0.2f).OnComplete(() =>
        {
            cg.blocksRaycasts = open;
            cg.interactable = open;
        });
    }
}
