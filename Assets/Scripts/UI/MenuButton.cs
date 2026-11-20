using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour,IPointerEnterHandler,IPointerClickHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
{
    public Image image;
    public float scale = 1.2f;

    public void OnScaleDown()
    {
        if(image != null)
        {
            image.transform.DOScale(Vector3.one,0.2f).SetLink(gameObject).SetUpdate(true);
        }
        else this.transform.DOScale(Vector3.one,0.2f).SetLink(gameObject).SetUpdate(true);
    }

    public void OnScalePress()
    {
        if(image != null)
        {
            image.transform.DOPunchScale(Vector3.one * -0.2f, 0.1f,10,0f).SetLink(gameObject).SetUpdate(true);
        }
        else this.transform.DOPunchScale(Vector3.one * -0.2f, 0.1f,10,0f).SetLink(gameObject).SetUpdate(true);
    }

    public void OnScaleUp()
    {
        if(image != null)
        {
            image.transform.DOScale(Vector3.one*scale,0.2f).SetLink(gameObject).SetUpdate(true);
        }
        else this.transform.DOScale(Vector3.one*scale,0.2f).SetLink(gameObject).SetUpdate(true);
    }


    public void OnPointerEnter(PointerEventData e)
    {
        OnScaleUp();
    }

    public void OnPointerExit(PointerEventData e)
    {
        OnScaleDown();
    }

    public void OnPointerClick(PointerEventData e)
    {
        OnScalePress();
    }

    public void OnSelect(BaseEventData e)
    {
        OnScaleUp();
    }

    public void OnDeselect(BaseEventData e)
    {
        OnScaleDown();
    }
}
