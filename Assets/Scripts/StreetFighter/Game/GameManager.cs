using UnityEngine;
using UnityEngine.UI;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 main.js / game.js：资源加载、开局、统一帧驱动、胜负与重开、模式切换。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        private const float P1X = 280f;
        private const float P2X = 480f;
        private const float GY = 240f;

        private GameClock _clock;
        private Spirit _p1, _p2;
        private BloodBar _bar1, _bar2;
        private SpriteView _bgBehind, _bgFront;
        private Camera _cam;
        private Sfx _music;

        private int _mode = 1;
        private bool _paused;
        private bool _modeLock;
        private float _acc;

        private void Awake()
        {
            Instance = this;

            var text = Resources.Load<TextAsset>("config").text;
            Cfg.Load(text);
            Art.Init();

            _clock = new GameClock();
            Collider.Clear();

            SetupCamera();
            SetupBackground();
            SetupHud();
            StartMatch();

            _music = new Sfx();
            _music.Loop("sound/china.mp3");
        }

        private void Update()
        {
            HandleGlobalKeys();

            if (!_paused)
            {
                _acc += Time.deltaTime * 1000f;
                int guard = 0;
                while (_acc >= GameClock.TickMs && guard < 8)
                {
                    _acc -= GameClock.TickMs;
                    _clock.Tick();
                    guard++;
                }
                if (_acc > 200f) _acc = 0f;
            }

            Render();
        }

        // ---------------- 初始化 ----------------

        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                _cam = go.AddComponent<Camera>();
            }
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color32(0xA0, 0xAA, 0xB2, 0xFF);
            _cam.orthographic = true;
            _cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        /// <summary>保持 900x490 的画面比例，多余部分留黑边。</summary>
        private void FitCamera()
        {
            if (_cam == null) return;
            float target = Cfg.MapWidth / Cfg.MapHeight;
            float screen = (float)Screen.width / Screen.height;
            float w = 1f, h = 1f;
            if (screen > target) w = target / screen;
            else h = screen / target;

            _cam.rect = new Rect((1f - w) / 2f, (1f - h) / 2f, w, h);
            _cam.aspect = target;
            _cam.orthographicSize = Cfg.MapHeight / 2f;
        }

        private void SetupBackground()
        {
            _bgBehind = new SpriteView("bg_behind", -20);
            _bgFront = new SpriteView("bg_front", -10);
        }

        private void SetupHud()
        {
            var canvasGo = new GameObject("Hud");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(Cfg.MapWidth, Cfg.MapHeight);
            canvasGo.transform.SetParent(_cam.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, 5f);

            var white = WhiteSprite();

            MakeImage(canvasGo.transform, "bar", 96f, 35f, 720f, 32f, Color.white, Art.Whole("bar"));

            var img1 = MakeImage(canvasGo.transform, "blood_left", 96f, 40f, 322f, 21f, Color.blue, white);
            var img2 = MakeImage(canvasGo.transform, "blood_right", 493f, 40f, 320f, 21f, Color.yellow, white);

            _bar1 = new BloodBar(img1, true, 96f, 322f);
            _bar2 = new BloodBar(img2, false, 493f, 320f);
        }

        private static Image MakeImage(Transform parent, string name, float x, float y, float w, float h, Color color, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return img;
        }

        private static Sprite _white;

        private static Sprite WhiteSprite()
        {
            if (_white != null) return _white;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
            tex.Apply();
            _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1f);
            return _white;
        }

        private void StartMatch()
        {
            _p1 = new Spirit(_clock, "RYU1", Cfg.Spirit("RYU1"));
            _p2 = new Spirit(_clock, "RYU2", Cfg.Spirit("RYU2"));

            _p1.SetEnemy(_p2);
            _p2.SetEnemy(_p1);

            _p1.BloodBar = _bar1;
            _p2.BloodBar = _bar2;

            _p1.AttachView(new SpiritView("RYU1", 10));
            _p2.AttachView(new SpiritView("RYU2", 12));

            _p1.Init(P1X, GY, 1);
            _p2.Init(P2X, GY, -1);

            _p2.Keys.Stop();
            _p2.Ai = new Ai(_clock, _p2);
            _p2.Ai.Start();

            _p2.Enemy.BloodBar.Event.Listen("empty", () => _p2.Ai.Stop());
        }

        // ---------------- 每帧 ----------------

        private void Render()
        {
            FitCamera();

            float x = -Stage.Bg.ScrollLeft;
            _bgBehind.ShowStretched(Cfg.BgBehind, x, 0f, StageScroll.ContentWidth, 400f);
            _bgFront.ShowStretched(Cfg.BgFront, x, 0f, StageScroll.ContentWidth, Cfg.MapHeight);

            _p1.Render(Cfg.Zoom, 10);
            _p2.Render(Cfg.Zoom, 12);

            _bar1.Render();
            _bar2.Render();
        }

        private void HandleGlobalKeys()
        {
            if (Input.GetKeyDown(KeyCode.F2)) _paused = !_paused;

            if (!_modeLock && (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2)))
            {
                _modeLock = true;
                _mode = Input.GetKeyDown(KeyCode.Alpha1) ? 1 : 2;
                _p2.Ai.Stop();
                Reload();
                _clock.Timeout(() => _modeLock = false, 1000);
            }
        }

        /// <summary>复刻 Game.reload：回满血、复位、恢复输入。</summary>
        public void Reload()
        {
            _p1.Keys.Stop();
            _p2.Keys.Stop();
            _bar1.Reload();
            _bar2.Reload();

            _clock.Timeout(() =>
            {
                _p1.Play("force_wait", true);
                _clock.Timeout(() =>
                {
                    _p1.Ani.Moveto(P1X, GY);
                    _p1.Keys.Start();
                    _p1.Direction = 1;
                }, 30);

                _p2.Play("force_wait", true);
                _clock.Timeout(() =>
                {
                    _p2.Ani.Moveto(P2X, GY);
                    _p2.Keys.Start();
                    _p2.Direction = -1;
                    if (_mode == 1) _p2.Ai.Start();
                }, 30);
            }, 1000);
        }
    }
}
