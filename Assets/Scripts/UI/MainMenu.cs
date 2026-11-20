using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    [Header("MainMenu")]
    public CanvasGroup menuCg;
    public  TMP_Text titleText;

    [Header("Setting")]
    public SettingsPanel settingsPanel;

    void Start()
    {
        ToggleCg(menuCg,true);
        ToggleTxt(titleText,true);
        if(settingsPanel != null)
        {
            settingsPanel.onClosed.AddListener(OnSettingsClosed);
        }
    }

    public void OnPlay()
    {
        ToggleAllCg_Menu(false).Play().OnComplete(() =>
        {
            GameManager.instance.CurrentState = GameState.LEVELSELECT;
            SceneManager.LoadScene(GameManager.SCENE_LEVELSELECT);
        });
    }

    public void OnSettings()
    {
        Sequence s = DOTween.Sequence();
        s.Append(ToggleCg(menuCg,false));
        s.OnComplete(()=>settingsPanel.Open());
        s.Play();
    }

    public void OnQuit()
    {
        Application.Quit();
    }

    private void OnSettingsClosed()
    {
        ToggleCg(menuCg,true);
    }

    //切换画布组
    private Tween ToggleAllCg_Menu(bool open)
    {
        Sequence s = DOTween.Sequence();
        s.Join(ToggleCg(menuCg,open));
        s.Join(ToggleTxt(titleText,open));
        return s;
    }

    public static Tween ToggleCg(CanvasGroup cg,bool open)
    {
        return cg.DOFade(open? 1f : 0f, 0.2f).OnComplete(() =>
        {
            cg.blocksRaycasts = open;
            cg.interactable = open;
        });
    }

    public static Tween ToggleTxt(TMP_Text txt,bool open)
    {
        return txt.DOFade(open? 1f : 0f, 0.2f);
    }

}
