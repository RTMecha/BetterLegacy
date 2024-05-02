
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Components
{
    public class HoverUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
    {
        public float size = 1.1f;
        public Vector3 ogPos;
        public Vector3 animPos;
        public bool animatePos = false;
        public bool animateSca = false;
        public bool setPos = false;

        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            if (!GetComponent<Button>() && !GetComponent<Toggle>() || GetComponent<Button>() && GetComponent<Button>().interactable || GetComponent<Toggle>() && GetComponent<Toggle>().interactable)
            {
                if (animatePos)
                {
                    if (!setPos)
                    {
                        setPos = true;
                        ogPos = transform.localPosition;
                    }
                    transform.DOLocalMove(new Vector3(ogPos.x + animPos.x, ogPos.y + animPos.y, 0f), 0.2f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
                }
                if (animateSca)
                {
                    transform.DOScale(new Vector3(size, size, size), 0.2f).SetEase(DataManager.inst.AnimationList[3].Animation).Play();
                }
            }
        }

        public void OnPointerExit(PointerEventData pointerEventData)
        {
            if (animatePos)
            {
                transform.DOLocalMove(ogPos, 0.2f).SetEase(DataManager.inst.AnimationList[2].Animation);
                //e.OnComplete(delegate ()
                //{
                //    setPos = false;
                //});
            }
            if (animateSca)
            {
                transform.DOScale(new Vector3(1f, 1f, 1f), 0.2f).SetEase(DataManager.inst.AnimationList[2].Animation).Play();
            }
        }
    }
}
