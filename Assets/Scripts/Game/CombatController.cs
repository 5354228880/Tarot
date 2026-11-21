using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Tarot.Core;

namespace Tarot.Game
{
    public sealed class CombatController : MonoBehaviour
    {
        public static CombatController Instance;

        public Canvas Canvas;
        public RectTransform CanvasRect;
        public RectTransform SequenceRoot;
        public RectTransform HandRoot;
        public RectTransform DragLayer;
        public TextMeshProUGUI StatusText;
        public TextMeshProUGUI LogText;
        public TextMeshProUGUI AntiDefyText;
        public Button StartButton;
        public Button PauseButton;
        public Button DirectionButton;
        public Button ResultButton;
        public GameObject PausePanel;
        public GameObject ResultPanel;
        public TextMeshProUGUI ResultText;
        public TextMeshProUGUI ResultSubText;
        public GameObject SettingsPanel;
        public Button SettingsResolutionButton;
        public Button SettingsVolumeButton;
        public Button SettingsFullscreenButton;
        public CombatState State;
        public bool IsResolving;

        readonly List<SlotView> m_Slots = new List<SlotView>();
        Resolution[] m_Resolutions;
        int m_ResolutionIndex;
        int m_Volume;
        bool m_Fullscreen;

        void Start()
        {
            Instance = this;
            Application.runInBackground = true;
            Time.timeScale = 1f;
            m_Resolutions = Screen.resolutions;
            if (m_Resolutions == null || m_Resolutions.Length == 0)
            {
                m_Resolutions = new[] { new Resolution { width = Screen.width, height = Screen.height } };
            }
            m_ResolutionIndex = 0;
            for (int i = 0; i < m_Resolutions.Length; i++)
            {
                if (m_Resolutions[i].width == Screen.width && m_Resolutions[i].height == Screen.height)
                {
                    m_ResolutionIndex = i;
                    break;
                }
            }
            m_Volume = Mathf.RoundToInt(AudioListener.volume * 10f);
            m_Fullscreen = Screen.fullScreen;

            UiFactory.EnsureCamera();
            UiFactory.EnsureEventSystem();
            Canvas = UiFactory.CreateCanvas("CombatCanvas");
            CanvasRect = (RectTransform)Canvas.transform;
            BuildUi();

            var session = GameSession.Instance;
            session.EnsureRun();
            State = CombatEngine.CreateCombat(session.Run, session.CurrentLevel);
            RenderAll();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void BuildUi()
        {
            var background = UiFactory.CreatePanel(CanvasRect, "Background", UiFactory.Bg, false);
            UiFactory.Stretch(background);

            var topBar = UiFactory.CreatePanel(CanvasRect, "TopBar", UiFactory.Panel, false);
            UiFactory.SetAnchored(topBar, new Vector2(0, 505), new Vector2(1880, 70));
            var title = UiFactory.CreateText(topBar, "Title", "FATE SEQUENCE", 26, UiFactory.Text, TextAlignmentOptions.Left);
            UiFactory.SetAnchored((RectTransform)title.transform, new Vector2(-300, 0), new Vector2(1000, 50));
            PauseButton = UiFactory.CreateButton(topBar, "Pause", "PAUSE", UiFactory.PanelAlt,
                new Vector2(840, 0), new Vector2(120, 48), TogglePause);

            SequenceRoot = UiFactory.CreatePanel(CanvasRect, "Sequence", UiFactory.Panel, false);
            UiFactory.SetAnchored(SequenceRoot, new Vector2(0, 220), new Vector2(1500, 260));
            const int slotCount = 9;
            const float slotWidth = 150f;
            const float slotGap = 8f;
            float totalWidth = slotCount * slotWidth + (slotCount - 1) * slotGap;
            float startX = -totalWidth * 0.5f + slotWidth * 0.5f;
            for (int i = 0; i < slotCount; i++)
            {
                var slot = UiFactory.CreatePanel(SequenceRoot, "Slot" + i, UiFactory.PanelAlt);
                UiFactory.SetAnchored(slot, new Vector2(startX + i * (slotWidth + slotGap), 0), new Vector2(slotWidth, 210));
                var view = slot.gameObject.AddComponent<SlotView>();
                view.Init(i, true);
                m_Slots.Add(view);
            }

            StatusText = UiFactory.CreateText(CanvasRect, "Status", "", 20, UiFactory.Text);
            UiFactory.SetAnchored((RectTransform)StatusText.transform, new Vector2(0, 60), new Vector2(1500, 40));

            AntiDefyText = UiFactory.CreateText(CanvasRect, "AntiDefyHint", "", 15, UiFactory.Accent);
            UiFactory.SetAnchored((RectTransform)AntiDefyText.transform, new Vector2(0, -5), new Vector2(1500, 30));

            HandRoot = UiFactory.CreatePanel(CanvasRect, "HandZone", UiFactory.Panel);
            UiFactory.SetAnchored(HandRoot, new Vector2(0, -330), new Vector2(1400, 240));
            HandRoot.gameObject.AddComponent<DropZoneView>().Type = DropZoneType.Hand;

            var discard = UiFactory.CreatePanel(CanvasRect, "DiscardZone", UiFactory.Panel);
            UiFactory.SetAnchored(discard, new Vector2(800, -330), new Vector2(260, 190));
            discard.gameObject.AddComponent<DropZoneView>().Type = DropZoneType.Discard;
            var discardLabel = UiFactory.CreateText(discard, "Label", "DISCARD\nANTI-DEFY", 18, UiFactory.Muted);
            UiFactory.Stretch((RectTransform)discardLabel.transform, 8, 8, 8, 8);

            var logPanel = UiFactory.CreatePanel(CanvasRect, "LogPanel", UiFactory.Panel, false);
            UiFactory.SetAnchored(logPanel, new Vector2(800, -120), new Vector2(320, 180));
            LogText = UiFactory.CreateText(logPanel, "Log", "", 13, UiFactory.Muted, TextAlignmentOptions.TopLeft);
            UiFactory.Stretch((RectTransform)LogText.transform, 12, 12, 12, 12);

            StartButton = UiFactory.CreateButton(CanvasRect, "Start", "START RESOLUTION",
                new Color(0.28f, 0.48f, 0.34f), new Vector2(0, -500), new Vector2(360, 62), OnStartResolution);
            DirectionButton = UiFactory.CreateButton(CanvasRect, "Direction", "DIR: FWD", UiFactory.PanelAlt,
                new Vector2(320, -500), new Vector2(220, 62), ToggleDirection);

            DragLayer = UiFactory.CreatePanel(CanvasRect, "DragLayer", Color.clear, false);
            UiFactory.Stretch(DragLayer);

            BuildPausePanel();
            BuildSettingsPanel();
            BuildResultPanel();
        }

        void BuildPausePanel()
        {
            var overlay = UiFactory.CreatePanel(CanvasRect, "PausePanel", new Color(0f, 0f, 0f, 0.78f));
            UiFactory.Stretch(overlay);
            var panel = UiFactory.CreatePanel(overlay, "Panel", UiFactory.Panel);
            UiFactory.SetAnchored(panel, Vector2.zero, new Vector2(520, 380));
            var title = UiFactory.CreateText(panel, "Title", "PAUSED", 30, UiFactory.Text);
            UiFactory.SetAnchored((RectTransform)title.transform, new Vector2(0, 120), new Vector2(420, 50));
            UiFactory.CreateButton(panel, "Resume", "RESUME", UiFactory.PanelAlt, new Vector2(0, 50), new Vector2(300, 60),
                () => PausePanel.SetActive(false));
            UiFactory.CreateButton(panel, "Settings", "SETTINGS", UiFactory.PanelAlt, new Vector2(0, -20), new Vector2(300, 60),
                OpenSettings);
            UiFactory.CreateButton(panel, "Menu", "RETURN TO MENU", new Color(0.5f, 0.3f, 0.3f), new Vector2(0, -90),
                new Vector2(300, 60), () =>
                {
                    GameSession.Instance.DiscardRun();
                    SceneManager.LoadScene("Menu");
                });
            PausePanel = overlay.gameObject;
            PausePanel.SetActive(false);
        }

        void BuildSettingsPanel()
        {
            var overlay = UiFactory.CreatePanel(CanvasRect, "SettingsPanel", new Color(0f, 0f, 0f, 0.86f));
            UiFactory.Stretch(overlay);
            var panel = UiFactory.CreatePanel(overlay, "Panel", UiFactory.Panel);
            UiFactory.SetAnchored(panel, Vector2.zero, new Vector2(600, 400));
            var title = UiFactory.CreateText(panel, "Title", "SETTINGS", 28, UiFactory.Text);
            UiFactory.SetAnchored((RectTransform)title.transform, new Vector2(0, 140), new Vector2(500, 50));
            SettingsResolutionButton = UiFactory.CreateButton(panel, "Resolution", "", UiFactory.PanelAlt,
                new Vector2(0, 70), new Vector2(480, 60), CycleResolution);
            SettingsVolumeButton = UiFactory.CreateButton(panel, "Volume", "", UiFactory.PanelAlt,
                new Vector2(0, 0), new Vector2(480, 60), CycleVolume);
            SettingsFullscreenButton = UiFactory.CreateButton(panel, "Fullscreen", "", UiFactory.PanelAlt,
                new Vector2(0, -70), new Vector2(480, 60), CycleFullscreen);
            UiFactory.CreateButton(panel, "Back", "BACK", UiFactory.PanelAlt, new Vector2(0, -150), new Vector2(220, 55),
                () => SettingsPanel.SetActive(false));
            SettingsPanel = overlay.gameObject;
            SettingsPanel.SetActive(false);
            RefreshSettingsLabels();
        }

        void OpenSettings()
        {
            SettingsPanel.SetActive(true);
            PausePanel.SetActive(true);
            RefreshSettingsLabels();
        }

        void CycleResolution()
        {
            if (m_Resolutions == null || m_Resolutions.Length == 0) return;
            m_ResolutionIndex = (m_ResolutionIndex + 1) % m_Resolutions.Length;
            var res = m_Resolutions[m_ResolutionIndex];
            Screen.SetResolution(res.width, res.height, m_Fullscreen);
            RefreshSettingsLabels();
        }

        void CycleVolume()
        {
            m_Volume = (m_Volume + 1) % 11;
            AudioListener.volume = m_Volume / 10f;
            RefreshSettingsLabels();
        }

        void CycleFullscreen()
        {
            m_Fullscreen = !m_Fullscreen;
            Screen.fullScreen = m_Fullscreen;
            RefreshSettingsLabels();
        }

        void RefreshSettingsLabels()
        {
            if (SettingsResolutionButton != null)
            {
                var res = m_Resolutions[Mathf.Clamp(m_ResolutionIndex, 0, m_Resolutions.Length - 1)];
                UiFactory.SetButtonLabel(SettingsResolutionButton, "RESOLUTION: " + res.width + "x" + res.height);
            }
            if (SettingsVolumeButton != null) UiFactory.SetButtonLabel(SettingsVolumeButton, "VOLUME: " + m_Volume);
            if (SettingsFullscreenButton != null) UiFactory.SetButtonLabel(SettingsFullscreenButton, "FULLSCREEN: " + (m_Fullscreen ? "ON" : "OFF"));
        }

        void BuildResultPanel()
        {
            var overlay = UiFactory.CreatePanel(CanvasRect, "ResultPanel", new Color(0f, 0f, 0f, 0.82f));
            UiFactory.Stretch(overlay);
            var panel = UiFactory.CreatePanel(overlay, "Panel", UiFactory.Panel);
            UiFactory.SetAnchored(panel, Vector2.zero, new Vector2(620, 380));
            ResultText = UiFactory.CreateText(panel, "Result", "", 40, UiFactory.Accent);
            UiFactory.SetAnchored((RectTransform)ResultText.transform, new Vector2(0, 100), new Vector2(560, 70));
            ResultSubText = UiFactory.CreateText(panel, "Sub", "", 18, UiFactory.Muted);
            UiFactory.SetAnchored((RectTransform)ResultSubText.transform, new Vector2(0, 30), new Vector2(560, 70));
            ResultButton = UiFactory.CreateButton(panel, "Continue", "CONTINUE", UiFactory.Accent,
                new Vector2(0, -100), new Vector2(300, 64), null);
            ResultPanel = overlay.gameObject;
            ResultPanel.SetActive(false);
        }

        public void RenderAll()
        {
            if (State == null) return;

            foreach (var slotView in m_Slots)
            {
                var slot = State.Sequence.Slots[slotView.Index];
                slotView.IsPlayerSlot = slot.IsPlayerSlot;
                Color color;
                if (slot.Card == null)
                    color = slot.IsPlayerSlot ? new Color(0.17f, 0.19f, 0.28f) : new Color(0.11f, 0.11f, 0.16f);
                else
                    color = slot.Card.Owner == Owner.Player ? new Color(0.30f, 0.42f, 0.58f) : new Color(0.52f, 0.28f, 0.30f);
                slotView.SetColor(color);

                UiFactory.ClearChildren(slotView.transform);
                if (slot.Card != null)
                {
                    var cardRt = UiFactory.CreatePanel(slotView.transform, "Card", Color.clear);
                    UiFactory.SetAnchored(cardRt, Vector2.zero, new Vector2(150, 210));
                    var view = cardRt.gameObject.AddComponent<CardView>();
                    view.Setup(slot.Card, this, new Vector2(150, 210));
                }
            }

            UiFactory.ClearChildren(HandRoot);
            var hand = State.Deck.Hand;
            float cardWidth = 78f;
            float cardHeight = 110f;
            float gap = 4f;
            float total = hand.Count * cardWidth + Mathf.Max(0, hand.Count - 1) * gap;
            float start = -total * 0.5f + cardWidth * 0.5f;
            for (int i = 0; i < hand.Count; i++)
            {
                var cardRt = UiFactory.CreatePanel(HandRoot, "HandCard" + i, Color.clear);
                UiFactory.SetAnchored(cardRt, new Vector2(start + i * (cardWidth + gap), 0), new Vector2(cardWidth, cardHeight));
                var view = cardRt.gameObject.AddComponent<CardView>();
                view.Setup(hand[i], this, new Vector2(cardWidth, cardHeight));
            }

            StatusText.text = "HP " + State.Player.Hp + "/" + State.Player.MaxHp +
                              "    Shield " + State.Player.Shield +
                              "    Draw " + State.Deck.DrawPile.Count +
                              "    Discard " + State.Deck.Discard.Count +
                              "    Turn " + State.Turn;

            if (State.AntiDefyTargeting)
            {
                AntiDefyText.text = "ANTI-DEFY: double-click an enemy card (all enemies) or a hand card (level flip).";
                AntiDefyText.color = UiFactory.Accent;
            }
            else
            {
                AntiDefyText.text = State.AntiDefyUsed
                    ? "Anti-defy already used this turn."
                    : "Drag a hand card into the discard zone to use anti-defy.";
                AntiDefyText.color = UiFactory.Muted;
            }

            if (State.Log.Count > 0)
            {
                int from = Mathf.Max(0, State.Log.Count - 8);
                LogText.text = string.Join("\n", State.Log.Skip(from));
            }
            else LogText.text = "";

            var label = DirectionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = State.PlayerBackward ? "DIR: BACKWARD" : "DIR: FORWARD";
            DirectionButton.gameObject.SetActive(CombatEngine.HasReversedWheel(State) && State.Phase == CombatPhase.Decision);
            StartButton.interactable = State.Phase == CombatPhase.Decision && !IsResolving && !State.AntiDefyTargeting;
            PauseButton.interactable = !IsResolving;
        }

        public bool CanDrag(CardInstance card)
        {
            return State != null && !IsResolving && State.Phase == CombatPhase.Decision && card != null &&
                   card.Owner == Owner.Player && (card.Zone == Zone.Hand || card.Zone == Zone.Sequence);
        }

        public void HandleDrop(CardInstance card, PointerEventData eventData)
        {
            if (State == null || card == null) return;
            SlotView slot = null;
            DropZoneView zone = null;
            var results = new List<RaycastResult>();
            if (EventSystem.current != null) EventSystem.current.RaycastAll(eventData, results);
            foreach (var result in results)
            {
                if (slot == null) slot = result.gameObject.GetComponentInParent<SlotView>();
                if (zone == null) zone = result.gameObject.GetComponentInParent<DropZoneView>();
            }

            string error = null;
            if (slot != null)
            {
                if (card.Zone == Zone.Hand)
                {
                    if (!CombatEngine.PlaceCard(State, card, slot.Index, out error)) AddError(error);
                }
            }
            else if (zone != null)
            {
                if (zone.Type == DropZoneType.Hand && card.Zone == Zone.Sequence)
                {
                    var source = State.Sequence.Slots.FirstOrDefault(x => x.PlayerCard == card);
                    if (source != null && !CombatEngine.ReturnCard(State, source.Index, out error)) AddError(error);
                }
                else if (zone.Type == DropZoneType.Discard && card.Zone == Zone.Hand)
                {
                    if (!CombatEngine.TryBeginAntiDefy(State, card, out error)) AddError(error);
                }
            }
            RenderAll();
        }

        public void HandleDoubleClick(CardInstance card)
        {
            if (State == null || !State.AntiDefyTargeting || card == null) return;
            string error = null;
            if (card.Owner == Owner.Enemy)
            {
                if (!CombatEngine.ApplyAntiDefyAll(State, out error)) AddError(error);
            }
            else if (card.Owner == Owner.Player && card.Zone == Zone.Hand)
            {
                if (!CombatEngine.ApplyAntiDefyHand(State, card, out error)) AddError(error);
            }
            RenderAll();
        }

        void ToggleDirection()
        {
            if (State == null || State.Phase != CombatPhase.Decision) return;
            State.PlayerBackward = !State.PlayerBackward;
            RenderAll();
        }

        void TogglePause()
        {
            if (IsResolving) return;
            PausePanel.SetActive(!PausePanel.activeSelf);
        }

        void OnStartResolution()
        {
            if (State == null || IsResolving || State.Phase != CombatPhase.Decision || State.AntiDefyTargeting) return;
            StartCoroutine(ResolveRoutine());
        }

        IEnumerator ResolveRoutine()
        {
            IsResolving = true;
            State.Phase = CombatPhase.Resolving;
            RenderAll();

            var plan = CombatEngine.BuildPlan(State);
            State.Log.Add("Direction: " + plan.DirectionLabel);
            var passes = new List<List<int>>();
            if (plan.UseBoth)
            {
                passes.Add(plan.Forward);
                passes.Add(plan.Backward);
            }
            else passes.Add(plan.Forward);

            foreach (var pass in passes)
            {
                foreach (int index in pass)
                {
                    if (State.Phase == CombatPhase.Lost) break;
                    var slot = State.Sequence.Slots[index];
                    if (slot.Card == null) continue;
                    State.Log.Add("-- Slot " + (index + 1) + " --");
                    CombatEngine.ResolveSlot(State, plan, index);
                    RenderAll();
                    yield return new WaitForSeconds(0.22f);
                    if (State.Phase == CombatPhase.Lost) break;
                    if (State.AllEnemiesDead) break;
                }
                if (State.Phase == CombatPhase.Lost || State.AllEnemiesDead) break;
            }

            if (State.Phase == CombatPhase.Lost)
            {
                ShowResult(false);
                yield break;
            }

            CombatEngine.EndTurn(State);
            RenderAll();

            if (State.AllEnemiesDead)
            {
                ShowResult(true);
                yield break;
            }

            if (State.Player.Hp <= 0)
            {
                ShowResult(false);
                yield break;
            }

            yield return new WaitForSeconds(0.25f);
            CombatEngine.StartTurn(State);
            IsResolving = false;
            RenderAll();
        }

        void ShowResult(bool win)
        {
            if (State == null) return;
            State.Phase = win ? CombatPhase.Won : CombatPhase.Lost;
            IsResolving = true;
            if (win) GameSession.Instance.SaveCombat(State);
            RenderAll();

            ResultPanel.SetActive(true);
            ResultText.text = win ? (State.IsBoss ? "RUN COMPLETE" : "LEVEL CLEAR") : "DEFEAT";
            ResultSubText.text = win
                ? "HP " + State.Player.Hp + "/" + State.Player.MaxHp
                : "The run ends here.";

            ResultButton.onClick.RemoveAllListeners();
            if (win && !State.IsBoss)
            {
                UiFactory.SetButtonLabel(ResultButton, "CONTINUE");
                ResultButton.onClick.AddListener(() =>
                {
                    GameSession.Instance.AdvanceLevel();
                    SceneManager.LoadScene("Layer");
                });
            }
            else
            {
                UiFactory.SetButtonLabel(ResultButton, "BACK TO MENU");
                ResultButton.onClick.AddListener(() =>
                {
                    GameSession.Instance.DiscardRun();
                    SceneManager.LoadScene("Menu");
                });
            }
        }

        void AddError(string error)
        {
            if (!string.IsNullOrEmpty(error)) State.Log.Add(error);
        }
    }
}
