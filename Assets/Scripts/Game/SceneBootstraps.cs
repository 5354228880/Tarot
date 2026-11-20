using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Tarot.Core;

namespace Tarot.Game
{
    public sealed class LayerBootstrap : MonoBehaviour
    {
        void Start()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            UiFactory.EnsureCamera();
            UiFactory.EnsureEventSystem();
            var canvas = UiFactory.CreateCanvas("LayerCanvas");
            var root = (RectTransform)canvas.transform;

            var background = UiFactory.CreatePanel(root, "Background", UiFactory.Bg, false);
            UiFactory.Stretch(background);

            var session = GameSession.Instance;
            session.EnsureRun();
            var run = session.Run;

            var title = UiFactory.CreateText(root, "Title", "LAYER 1", 52, UiFactory.Text);
            UiFactory.SetAnchored((RectTransform)title.transform, new Vector2(0, 360), new Vector2(900, 80));

            var subtitle = UiFactory.CreateText(root,
                "Subtitle",
                "HP " + run.Hp + "/" + run.MaxHp + "    Progress " + Mathf.Min(run.LevelIndex, 4) + "/4",
                22, UiFactory.Muted);
            UiFactory.SetAnchored((RectTransform)subtitle.transform, new Vector2(0, 300), new Vector2(900, 40));

            if (run.IsComplete)
            {
                title.text = "RUN COMPLETE";
                subtitle.text = "The layer has been cleared.";
            }

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                string label = i == 3 ? "BOSS" : "LEVEL " + (i + 1);
                bool cleared = i < run.LevelIndex;
                bool current = i == run.LevelIndex && !run.IsComplete;
                bool locked = i > run.LevelIndex || run.IsComplete;

                if (cleared) label += "   (CLEARED)";
                else if (current) label += "   >";
                else if (locked) label += "   (LOCKED)";

                var button = UiFactory.CreateButton(root, "Level" + i, label,
                    current ? UiFactory.Accent : UiFactory.PanelAlt,
                    new Vector2(0, 160 - i * 90), new Vector2(460, 70),
                    current ? (UnityEngine.Events.UnityAction)(() => SceneManager.LoadScene("Combat")) : null);
                button.interactable = current;
                var buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonLabel != null) buttonLabel.color = current ? Color.black : UiFactory.Text;
            }

            UiFactory.CreateButton(root, "Back", "BACK TO MENU", new Color(0.45f, 0.28f, 0.30f),
                new Vector2(0, -380), new Vector2(300, 60), () =>
                {
                    GameSession.Instance.DiscardRun();
                    SceneManager.LoadScene("Menu");
                });
        }
    }

    public sealed class CombatBootstrap : MonoBehaviour
    {
        void Start()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            UiFactory.EnsureCamera();
            UiFactory.EnsureEventSystem();
            gameObject.AddComponent<CombatController>();
        }
    }
}
