using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using System;
using TMPro;
using DG.Tweening.Core.Easing;
using UnityEngine.PlayerLoop;

public class LevelSelect : MonoBehaviour
{
    public CanvasGroup backToMenuCg;
    public CanvasGroup levelCg;
    public CanvasGroup pageCg;

    public RectTransform levelSelectHolder;
    public RectTransform pageHolder;

    public int buttonCountPerPage = 10;
    public GameObject buttonPrefab;
    private int pageNumber;

    void Start()
    {
        pageNumber = 0;
        Sequence s = DOTween.Sequence();
        InitLevelSelectHolderChildren();
        InitLevelSelectBinding();
        s.Join(ToggleCg(pageCg,true));
        s.Join(ToggleCg(backToMenuCg,true));
        s.Join(FlashPages());
        s.Join(FlashLevelSelect());
    }


    public void OnBackToMenu()
    {
        ToggleAllCg_LevelSelect(false).Play().OnComplete(() =>
        {
            GameManager.instance.CurrentState = GameState.MAINMENU;
            SceneManager.LoadScene(GameManager.SCENE_MENU);
        });
    }

    public void OnLeftPage()
    {
        pageNumber = (pageNumber - 1) < 0 ? pageNumber : pageNumber - 1;
        Sequence s = DOTween.Sequence();
        s.Append(FlashLevelSelect());
        s.Join(FlashPages());
        s.Play();
    }

    public void OnRightPage()
    {
        pageNumber = (pageNumber + 1) > Math.Ceiling(1f*GameManager.instance.LevelScenesName.Length/buttonCountPerPage) - 1 ? pageNumber : pageNumber + 1;
        Sequence s = DOTween.Sequence();
        s.Append(FlashLevelSelect());
        s.Join(FlashPages());
        s.Play();
    }

    private void InitLevelSelectHolderChildren()
    {
        int childCnt = levelSelectHolder.childCount;
        if(childCnt == buttonCountPerPage)return;
        if(childCnt > buttonCountPerPage)
        {
            for(int i=buttonCountPerPage ; i<childCnt ; i++)
            {
                Destroy(levelSelectHolder.GetChild(i).gameObject);
            }
        }
        if(childCnt < buttonCountPerPage)
        {
            for(int i=childCnt ; i<buttonCountPerPage ; i++)
            {
                Instantiate(buttonPrefab,levelSelectHolder);
            }
        }
    }

    private void InitLevelSelectBinding()
    {
        for(int i=0 ; i<buttonCountPerPage ; i++)
        {
            int index = i;
            int levelIndex = pageNumber*buttonCountPerPage + index;
            Button btn = levelSelectHolder.GetChild(index).GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                ToggleAllCg_LevelSelect(false).OnComplete(() =>
                {
                    GameManager.instance.CurrentState = GameState.INGAME;
                    GameManager.instance.thisLevelIndex = index;
                    SceneManager.LoadScene(GameManager.instance.LevelScenesName[levelIndex]);
                });
            });
        }
    }

    private Tween FlashLevelSelect()
    {
        Sequence s = DOTween.Sequence();
        s.Append(ToggleCg(levelCg,false));
        s.AppendCallback(()=>{SetButtonText(); SetButtons();});
        s.Append(ToggleCg(levelCg,true));
        return s;
    }

    private Tween FlashPages()
    {
        Sequence s = DOTween.Sequence();
        s.AppendCallback(InitLevelSelectBinding);
        s.Join(pageHolder.GetChild(0).GetComponent<Image>().DOFade(pageNumber > 0 ? 1f : 0f, 0.2f).OnComplete(() =>
        {
            pageHolder.GetChild(0).gameObject.SetActive(pageNumber > 0);
        }));
        s.Join(pageHolder.GetChild(1).GetComponent<Image>().DOFade(pageNumber < Math.Ceiling(1f*GameManager.instance.LevelScenesName.Length/buttonCountPerPage) - 1 ? 1f : 0f, 0.2f).OnComplete(() =>
        {
            pageHolder.GetChild(1).gameObject.SetActive(pageNumber < Math.Ceiling(1f*GameManager.instance.LevelScenesName.Length/buttonCountPerPage) - 1);
        }));
        return s;
    }

    private void SetButtonText()
    {
        for(int i=0 ; i<buttonCountPerPage ; i++)
        {
            levelSelectHolder.GetChild(i).GetComponentInChildren<TMP_Text>().text = $"{pageNumber*buttonCountPerPage+i+1}";
        }
    }

    private void SetButtons()
    {
        int thisPageButtonCount = GameManager.instance.LevelScenesName.Length - pageNumber * buttonCountPerPage;
        for(int i=0 ; i<buttonCountPerPage ; i++)
        {
            int index = i;
            bool show = (index < thisPageButtonCount);
            levelSelectHolder.GetChild(index).gameObject.SetActive(show);
        }
    }

    private Tween SetOneLevelSelectButton(int i,bool show)
    {
        Sequence s = DOTween.Sequence();
        s.Join(levelSelectHolder.GetChild(i).GetChild(0).GetComponent<TMP_Text>().DOFade(show ? 1f : 0f, 0.2f));
        s.Join(levelSelectHolder.GetChild(i).GetComponent<Image>().DOFade(show ? 1f : 0f, 0.2f));
        return s;
    }

    public Tween ToggleAllCg_LevelSelect(bool open)
    {
        Sequence s = DOTween.Sequence();
        s.Join(ToggleCg(levelCg,open));
        s.Join(ToggleCg(pageCg,open));
        s.Join(ToggleCg(backToMenuCg,open));
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
}
