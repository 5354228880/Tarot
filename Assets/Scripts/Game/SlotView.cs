using UnityEngine;
using UnityEngine.UI;

namespace Tarot.Game
{
    public sealed class SlotView : MonoBehaviour
    {
        public int Index;
        public bool IsPlayerSlot;
        public Image Background;

        public void Init(int index, bool isPlayerSlot)
        {
            Index = index;
            IsPlayerSlot = isPlayerSlot;
            Background = GetComponent<Image>();
        }

        public void SetColor(Color color)
        {
            if (Background != null) Background.color = color;
        }
    }

    public enum DropZoneType
    {
        Hand,
        Discard
    }

    public sealed class DropZoneView : MonoBehaviour
    {
        public DropZoneType Type;
    }
}
