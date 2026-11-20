using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public CanvasGroup panelCg;
    public TMP_Text resolutionSubText;
    public TMP_Text volumeSubText;
    public TMP_Text fullScreenSubText;

    public UnityEvent onOpened = new UnityEvent();
    public UnityEvent onClosed = new UnityEvent();

    private int tempResolutionIndex;
    private int tempVolume;
    private bool tempFullScreen;
    private int ResolutionIndex
    {
        get{return tempResolutionIndex;}
        set{tempResolutionIndex = value;HandleResolution();}
    }
    private int Volume
    {
        get{return tempVolume;}
        set{tempVolume = value;HandleVolume();}
    }
    private bool FullScreen
    {
        get{return tempFullScreen;}
        set{tempFullScreen = value; HandleFullScreen();}
    }

    public void Open()
    {
        SyncTempFromSettings();
        Sequence s = DOTween.Sequence();
        s.Append(ToggleCg(panelCg,true));
        s.SetUpdate(true).Play().OnComplete(()=>onOpened.Invoke());
    }

    public void Close()
    {
        Sequence s = DOTween.Sequence();
        s.Append(ToggleCg(panelCg,false));
        s.SetUpdate(true).Play().OnComplete(()=>onClosed.Invoke());
    }

    public void OnResolution()
    {
        ResolutionIndex = (ResolutionIndex+1)%GameManager.instance.resolutions.Length;
    }

    public void OnVolume()
    {
        Volume = (Volume+1)%10;
    }

    public void OnFullScreen()
    {
        FullScreen = !FullScreen;
    }

    public void OnApplySettings()
    {
        GameManager.instance.ApplySettings(tempResolutionIndex,tempVolume,tempFullScreen);
        Close();
    }

    public void OnExitSettings()
    {
        Close();
    }

    private void SyncTempFromSettings()
    {
        ResolutionIndex = GameManager.instance.resolutionIndex;
        Volume = GameManager.instance.volume;
        FullScreen = GameManager.instance.fullScreen;
    }

    private void HandleResolution()
    {
        Resolution res = GameManager.instance.resolutions[tempResolutionIndex];
        resolutionSubText.text = $"{res.width}x{res.height}";
    }

    private void HandleVolume()
    {
        volumeSubText.text = $"{tempVolume}";
    }

    private void HandleFullScreen()
    {
        fullScreenSubText.text = tempFullScreen?"On":"Off";
    }

    private static Tween ToggleCg(CanvasGroup cg,bool open)
    {
        return cg.DOFade(open? 1f : 0f, 0.2f).OnComplete(() =>
        {
            cg.blocksRaycasts = open;
            cg.interactable = open;
        });
    }
}
