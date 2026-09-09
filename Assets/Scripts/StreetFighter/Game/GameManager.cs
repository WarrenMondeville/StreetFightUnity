using StreetFighter.Core;
using StreetFighter.Gameplay;
using StreetFighter.View;
using UnityEngine;
using UnityEngine.UI;

namespace StreetFighter.Game
{
    /// <summary>
    /// 复刻 main.js / game.js：资源加载、开局、统一帧驱动、胜负与重开、模式切换。
    /// 场景中只需要挂这一个组件，角色、背景、血条、特效都在运行时创建。
    /// </summary>
    [AddComponentMenu("StreetFighter/Game Manager")]
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        private const string BarArtName = "bar";

        private const int PlayerOneSortingOrder = 10;
        private const int PlayerTwoSortingOrder = 12;

        private const float HudDepth = 5f;
        private const float CameraDepth = -10f;

        private const int BackgroundBehindOrder = -20;
        private const int BackgroundFrontOrder = -10;

        private const float BackgroundBehindHeight = 400f;

        #region Inspector

        [Header("出场位置")]
        [SerializeField] private string _playerOneKey = "RYU1";
        [SerializeField] private string _playerTwoKey = "RYU2";
        [SerializeField] private float _playerOneStartX = 280f;
        [SerializeField] private float _playerTwoStartX = 480f;
        [SerializeField] private float _groundY = 240f;

        [Header("时钟")]
        [SerializeField] private int _maxCatchUpTicks = 8;
        [SerializeField] private float _accumulatorDropThresholdMs = 200f;

        [Header("重开节奏（毫秒）")]
        [SerializeField] private float _reloadDelayMs = 1000f;
        [SerializeField] private float _respawnDelayMs = 30f;
        [SerializeField] private float _modeSwitchCooldownMs = 1000f;

        #endregion

        /// <summary>全局唯一实例。</summary>
        public static GameManager Instance { get; private set; }

        private GameClock _clock;
        private Spirit _playerOne;
        private Spirit _playerTwo;
        private BloodBar _barOne;
        private BloodBar _barTwo;
        private SpriteView _backgroundBehind;
        private SpriteView _backgroundFront;
        private Camera _camera;
        private AudioPlayer _music;

        private GameMode _mode = GameMode.VersusAi;
        private bool _isPaused;
        private bool _isModeLocked;
        private float _accumulator;

        private static Sprite _whiteSprite;

        #region Unity 生命周期

        private void Awake()
        {
            Instance = this;

            GameInput.Initialize();
            GameConfig.Load();
            SpriteLibrary.Initialize();

            _clock = new GameClock();
            BodyCollider.Clear();

            SetupCamera();
            SetupBackground();
            SetupHud();
            StartMatch();

            _music = new AudioPlayer();
            _music.PlayLoop(SoundPaths.BackgroundMusic);
        }

        private void Update()
        {
            HandleSystemInput();

            if (!_isPaused)
            {
                _accumulator += Time.deltaTime * 1000f;

                int ticks = 0;
                while (_accumulator >= GameClock.TickMilliseconds && ticks < _maxCatchUpTicks)
                {
                    _accumulator -= GameClock.TickMilliseconds;
                    _clock.Tick();
                    ticks++;
                }

                // 长时间卡顿后直接丢弃积压，避免追帧雪崩
                if (_accumulator > _accumulatorDropThresholdMs)
                {
                    _accumulator = 0f;
                }
            }

            Render();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            GameInput.Shutdown();
        }

        #endregion

        #region 初始化

        private void SetupCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                _camera = cameraObject.AddComponent<Camera>();
            }

            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color32(0xA0, 0xAA, 0xB2, 0xFF);
            _camera.orthographic = true;
            _camera.transform.position = new Vector3(0f, 0f, CameraDepth);
        }

        /// <summary>保持 900x490 的画面比例，多余部分留黑边。</summary>
        private void FitCamera()
        {
            if (_camera == null)
            {
                return;
            }

            float targetAspect = GameConfig.MapWidth / GameConfig.MapHeight;
            float screenAspect = (float)Screen.width / Screen.height;

            float width = 1f;
            float height = 1f;
            if (screenAspect > targetAspect)
            {
                width = targetAspect / screenAspect;
            }
            else
            {
                height = screenAspect / targetAspect;
            }

            _camera.rect = new Rect((1f - width) / 2f, (1f - height) / 2f, width, height);
            _camera.aspect = targetAspect;
            _camera.orthographicSize = GameConfig.MapHeight / 2f;
        }

        private void SetupBackground()
        {
            _backgroundBehind = new SpriteView("bg_behind", BackgroundBehindOrder);
            _backgroundFront = new SpriteView("bg_front", BackgroundFrontOrder);
        }

        private void SetupHud()
        {
            var canvasObject = new GameObject("Hud");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(GameConfig.MapWidth, GameConfig.MapHeight);
            canvasObject.transform.SetParent(_camera.transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, 0f, HudDepth);

            CreateImage(canvasObject.transform, "bar", 96f, 35f, 720f, 32f, Color.white,
                SpriteLibrary.GetWhole(BarArtName));

            var leftBlood = CreateImage(canvasObject.transform, "blood_left", 96f, 40f, 322f, 21f, Color.blue, WhiteSprite);
            var rightBlood = CreateImage(canvasObject.transform, "blood_right", 493f, 40f, 320f, 21f, Color.yellow, WhiteSprite);

            _barOne = new BloodBar(leftBlood, true, 96f, 322f);
            _barTwo = new BloodBar(rightBlood, false, 493f, 320f);
        }

        private void StartMatch()
        {
            _playerOne = new Spirit(_clock, _playerOneKey, GameConfig.GetSpirit(_playerOneKey));
            _playerTwo = new Spirit(_clock, _playerTwoKey, GameConfig.GetSpirit(_playerTwoKey));

            _playerOne.SetEnemy(_playerTwo);
            _playerTwo.SetEnemy(_playerOne);

            _playerOne.BloodBar = _barOne;
            _playerTwo.BloodBar = _barTwo;

            _playerOne.AttachView(new SpiritView(_playerOneKey, PlayerOneSortingOrder));
            _playerTwo.AttachView(new SpiritView(_playerTwoKey, PlayerTwoSortingOrder));

            _playerOne.Initialize(_playerOneStartX, _groundY, 1);
            _playerTwo.Initialize(_playerTwoStartX, _groundY, -1);

            _playerTwo.Keys.Stop();
            _playerTwo.Ai = new AiController(_clock, _playerTwo);
            _playerTwo.Ai.Start();

            _playerTwo.Enemy.BloodBar.Events.AddListener(GameEvents.Empty, () => _playerTwo.Ai.Stop());
        }

        #endregion

        #region 每帧

        private void Render()
        {
            FitCamera();

            float scrollX = -Stage.Background.ScrollLeft;
            _backgroundBehind.ShowStretched(GameConfig.BackgroundBehind, scrollX, 0f,
                StageScroll.ContentWidth, BackgroundBehindHeight);
            _backgroundFront.ShowStretched(GameConfig.BackgroundFront, scrollX, 0f,
                StageScroll.ContentWidth, GameConfig.MapHeight);

            _playerOne.Render(GameConfig.Zoom, PlayerOneSortingOrder);
            _playerTwo.Render(GameConfig.Zoom, PlayerTwoSortingOrder);

            _barOne.Render();
            _barTwo.Render();
        }

        /// <summary>暂停与模式切换，键盘 / 手柄均可触发。</summary>
        private void HandleSystemInput()
        {
            if (GameInput.WasPausePressed)
            {
                _isPaused = !_isPaused;
            }

            if (_isModeLocked)
            {
                return;
            }

            if (GameInput.WasVersusAiPressed)
            {
                SwitchMode(GameMode.VersusAi);
            }
            else if (GameInput.WasVersusPlayerPressed)
            {
                SwitchMode(GameMode.VersusPlayer);
            }
        }

        private void SwitchMode(GameMode mode)
        {
            _isModeLocked = true;
            _mode = mode;
            _playerTwo.Ai.Stop();
            Reload();
            _clock.Timeout(() => _isModeLocked = false, _modeSwitchCooldownMs);
        }

        #endregion

        #region 重开

        /// <summary>复刻 Game.reload：回满血、复位、恢复输入。</summary>
        public void Reload()
        {
            _playerOne.Keys.Stop();
            _playerTwo.Keys.Stop();
            _barOne.Reload();
            _barTwo.Reload();

            _clock.Timeout(() =>
            {
                _playerOne.Play(StateNames.ForceWait, true);
                _clock.Timeout(() =>
                {
                    _playerOne.Motion.MoveTo(_playerOneStartX, _groundY);
                    _playerOne.Keys.Start();
                    _playerOne.Direction = 1;
                }, _respawnDelayMs);

                _playerTwo.Play(StateNames.ForceWait, true);
                _clock.Timeout(() =>
                {
                    _playerTwo.Motion.MoveTo(_playerTwoStartX, _groundY);
                    _playerTwo.Keys.Start();
                    _playerTwo.Direction = -1;

                    if (_mode == GameMode.VersusAi)
                    {
                        _playerTwo.Ai.Start();
                    }
                }, _respawnDelayMs);
            }, _reloadDelayMs);
        }

        #endregion

        #region 工具

        private static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                {
                    return _whiteSprite;
                }

                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                for (int i = 0; i < 16; i++)
                {
                    texture.SetPixel(i % 4, i / 4, Color.white);
                }

                texture.Apply();
                _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 1f);
                return _whiteSprite;
            }
        }

        private static Image CreateImage(Transform parent, string name, float x, float y, float width, float height,
            Color color, Sprite sprite)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);

            var image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;

            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(x, -y);
            rectTransform.sizeDelta = new Vector2(width, height);
            return image;
        }

        #endregion
    }
}
