using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tarot.Core;

namespace Tarot.Game
{
    public sealed class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public CardInstance Card;
        public CombatController Controller;

        CanvasGroup m_Group;
        RectTransform m_Rect;
        bool m_Dragging;

        public void Setup(CardInstance card, CombatController controller, Vector2 size)
        {
            Card = card;
            Controller = controller;
            m_Rect = (RectTransform)transform;
            m_Rect.sizeDelta = size;
            m_Group = gameObject.GetComponent<CanvasGroup>();
            if (m_Group == null) m_Group = gameObject.AddComponent<CanvasGroup>();

            var image = GetComponent<Image>();
            var baseColor = card.Owner == Owner.Player ? UiFactory.PlayerCard : UiFactory.EnemyCard;
            image.color = card.IsReversed ? Color.Lerp(baseColor, new Color(0.28f, 0.28f, 0.34f), 0.35f) : baseColor;
            image.raycastTarget = true;

            var outline = gameObject.GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
            outline.effectDistance = new Vector2(2f, -2f);

            bool compact = size.y < 160f;
            float width = size.x;
            float height = size.y;

            var name = UiFactory.CreateText(transform, "Name", card.Def.Name, compact ? 10 : 16, Color.black,
                TextAlignmentOptions.TopLeft);
            UiFactory.SetAnchored((RectTransform)name.transform,
                new Vector2(0, compact ? 31f : 65f),
                new Vector2(width - (compact ? 8f : 12f), compact ? 18f : 40f));
            name.enableWordWrapping = true;

            var effect = UiFactory.CreateText(transform, "Effect", CombatEngine.CardText(card), compact ? 8 : 11,
                new Color(0.1f, 0.1f, 0.14f), TextAlignmentOptions.TopLeft);
            UiFactory.SetAnchored((RectTransform)effect.transform,
                new Vector2(0, compact ? -20f : -18f),
                new Vector2(width - (compact ? 10f : 12f), compact ? 58f : 96f));
            effect.overflowMode = TextOverflowModes.Overflow;

            var mark = UiFactory.CreateText(transform, "Mark", card.IsReversed ? "REV" : "UP", compact ? 8 : 11,
                new Color(0.15f, 0.15f, 0.2f), TextAlignmentOptions.TopRight);
            UiFactory.SetAnchored((RectTransform)mark.transform,
                new Vector2(width * 0.5f - (compact ? 16f : 20f), compact ? 46f : 95f),
                compact ? new Vector2(32f, 12f) : new Vector2(38f, 18f));

            if (card.Owner == Owner.Enemy)
            {
                var hpBg = UiFactory.CreatePanel(transform, "HpBg", new Color(0.15f, 0.1f, 0.1f), false);
                UiFactory.SetAnchored(hpBg, new Vector2(0, -height * 0.5f + (compact ? 10f : 14f)),
                    new Vector2(width - 12f, compact ? 8f : 10f));
                var hpFill = UiFactory.CreatePanel(hpBg, "HpFill", new Color(0.9f, 0.35f, 0.35f), false);
                hpFill.anchorMin = new Vector2(0, 0);
                hpFill.anchorMax = new Vector2(1, 1);
                hpFill.offsetMin = Vector2.zero;
                hpFill.offsetMax = Vector2.zero;
                float ratio = card.Owner == Owner.Enemy && Controller != null && Controller.State != null
                    ? Mathf.Clamp01(FindEnemyHp(card) / Mathf.Max(1f, FindEnemyMaxHp(card)))
                    : 1f;
                hpFill.anchorMax = new Vector2(ratio, 1);
                hpFill.offsetMin = Vector2.zero;
                hpFill.offsetMax = Vector2.zero;

                var hpText = UiFactory.CreateText(transform, "Hp", "HP", compact ? 8 : 10, Color.black,
                    TextAlignmentOptions.BottomLeft);
                UiFactory.SetAnchored((RectTransform)hpText.transform,
                    new Vector2(0, -height * 0.5f + (compact ? 22f : 30f)),
                    new Vector2(width - 12f, compact ? 14f : 18f));
                hpText.text = FindEnemyHp(card) + "/" + FindEnemyMaxHp(card);
            }
        }

        int FindEnemyHp(CardInstance card)
        {
            var enemy = Controller.State.Enemies.Find(e => e.Card == card);
            return enemy != null ? enemy.Stats.Hp : 0;
        }

        int FindEnemyMaxHp(CardInstance card)
        {
            var enemy = Controller.State.Enemies.Find(e => e.Card == card);
            return enemy != null ? enemy.Stats.MaxHp : 1;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Controller == null || !Controller.CanDrag(Card))
            {
                eventData.pointerDrag = null;
                return;
            }
            m_Dragging = true;
            m_Group.blocksRaycasts = false;
            m_Group.alpha = 0.92f;
            m_Rect.SetParent(Controller.DragLayer, true);
            m_Rect.SetAsLastSibling();
            m_Rect.localScale = Vector3.one * 1.05f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Controller.CanvasRect, eventData.position, null, out var local);
            m_Rect.localPosition = local;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_Dragging) return;
            m_Dragging = false;
            Controller.HandleDrop(Card, eventData);
            Destroy(gameObject);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2 && Controller != null)
            {
                Controller.HandleDoubleClick(Card);
            }
        }
    }
}
