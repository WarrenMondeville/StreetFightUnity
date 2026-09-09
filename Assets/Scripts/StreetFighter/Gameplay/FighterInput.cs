using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StreetFighter.Core;
using StreetFighter.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 复刻 interface.js 的 KeyManage：
    /// 移动键持续采样 + 攻击键边缘触发 + 短动作序列匹配出招表。
    /// 输入来自 Unity Input System，键盘与手柄共用同一套判定逻辑。
    /// </summary>
    public sealed class FighterInput
    {
        /// <summary>攻击键按下后延迟多久再判定，用于攒出招序列。</summary>
        private const float AttackScheduleDelayMs = 50f;

        /// <summary>摇杆 / 十字键判定为某个方向的阈值。</summary>
        private const float StickThreshold = 0.5f;

        private const string FighterMapName = "Fighter";

        /// <summary>招式名到手柄按键的映射，键盘那一路仍然由 配置 决定。</summary>
        private static readonly Dictionary<string, string> GamepadAttackPaths = new Dictionary<string, string>
        {
            { "light_boxing", "<Gamepad>/buttonWest" },
            { "heavy_boxing", "<Gamepad>/buttonNorth" },
            { "light_kick", "<Gamepad>/buttonSouth" },
            { "heavy_kick", "<Gamepad>/buttonEast" },
        };

        /// <summary>配置 的 move 表中，单键动作名对应的绝对方向。</summary>
        private static readonly Dictionary<string, MoveDirection> DirectionsByMove = new Dictionary<string, MoveDirection>
        {
            { "jump", MoveDirection.Up },
            { "crouch", MoveDirection.Down },
            { "back", MoveDirection.Left },
            { "forward", MoveDirection.Right },
        };

        private readonly GameClock _clock;
        private readonly GameClock.TimerHandle _timer;
        private readonly InputActionMap _map;

        private readonly List<string> _moveKeys = new List<string>();
        private readonly Dictionary<string, string> _moveForward = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _moveMirrored = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _normalAttacks = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _specialAttacks = new Dictionary<string, string>();

        /// <summary>方向字母到绝对方向的映射（朝向翻转不影响按键本身的含义）。</summary>
        private readonly Dictionary<string, MoveDirection> _directions = new Dictionary<string, MoveDirection>();

        /// <summary>攻击字母到 Input System 动作的映射。</summary>
        private readonly Dictionary<string, InputAction> _attacks = new Dictionary<string, InputAction>();

        private readonly Dictionary<string, bool> _held = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _attackLatched = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _previousHeld = new Dictionary<string, bool>();
        private readonly Queue<string> _buffer = new Queue<string>();

        private readonly int _sampleInterval;
        private int _sampleCount;
        private bool _isLocked;

        private Dictionary<string, string> _activeMoves;
        private InputAction _move;

        /// <summary>匹配到动作（含出招）时派发，参数为动作名。</summary>
        public event Action<string> Matched;

        /// <summary>本帧没有匹配到任何移动 / 攻击时派发，参数为 null。</summary>
        public event Action<string> Unmatched;

        /// <summary>摇杆 / 十字键的四个绝对方向。</summary>
        private enum MoveDirection
        {
            Up,
            Down,
            Left,
            Right,
        }

        public FighterInput(GameClock clock, JVal keyMap)
        {
            _clock = clock;
            _sampleInterval = Mathf.Max(1, (int)(GameConfig.KeyFps / GameConfig.Fps));

            var mapping = keyMap.Get("mapping");
            var normalAttacks = keyMap.Get("attack").Get("normal");
            var keyboardPaths = ReadKeyboardPaths(mapping);

            for (int i = 0; i < mapping.Values.Count; i++)
            {
                string letter = mapping.Values[i].AsString;
                bool isAttack = normalAttacks.Has(letter);
                if (!isAttack && !_moveKeys.Contains(letter))
                {
                    _moveKeys.Add(letter);
                }
            }

            Fill(_moveForward, keyMap.Get("move"));
            Fill(_moveMirrored, keyMap.Get("move_mirror"));
            Fill(_normalAttacks, normalAttacks);
            Fill(_specialAttacks, keyMap.Get("attack").Get("special"));
            _activeMoves = _moveForward;

            ReadDirections(keyMap.Get("move"));

            _map = new InputActionMap(FighterMapName);
            BuildMoveAction(keyboardPaths);
            BuildAttackActions(normalAttacks, keyboardPaths);
            _map.Enable();

            GameInput.Register(this);

            _timer = clock.Add(Tick);
            clock.Start(_timer);
        }

        /// <summary>朝向翻转后切换前后方向的含义。</summary>
        public void Mirror(int direction) => _activeMoves = direction == 1 ? _moveForward : _moveMirrored;

        /// <summary>恢复输入。</summary>
        public void Start()
        {
            _isLocked = false;
            _map.Enable();
        }

        /// <summary>暂停输入并清空缓冲。</summary>
        public void Stop()
        {
            _held.Clear();
            _buffer.Clear();
            _previousHeld.Clear();
            _isLocked = true;
            _map.Disable();
        }

        /// <summary>限制该玩家只接收指定设备的输入（由 <see cref="GameInput"/> 分配）。</summary>
        internal void SetDevices(InputDevice[] devices) => _map.devices = new ReadOnlyArray<InputDevice>(devices);

        private static void Fill(Dictionary<string, string> target, JVal source)
        {
            if (source.IsNull)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                target[source.Keys[i]] = source.Values[i].AsString;
            }
        }

        /// <summary>把 配置 的 keyCode 映射翻译成 字母 -> 键盘控件路径。</summary>
        private static Dictionary<string, string> ReadKeyboardPaths(JVal mapping)
        {
            var paths = new Dictionary<string, string>();
            for (int i = 0; i < mapping.Count; i++)
            {
                string letter = mapping.Values[i].AsString;
                int keyCode;
                if (!int.TryParse(mapping.Keys[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out keyCode))
                {
                    Debug.LogWarning($"[SF] keyMap.mapping 里的按键码不是数字: {mapping.Keys[i]}");
                    continue;
                }

                string path = KeyboardPaths.Get(keyCode);
                if (path == null)
                {
                    Debug.LogWarning($"[SF] 无法识别的按键码 {keyCode}（{letter}），该键将不参与输入");
                    continue;
                }

                paths[letter] = path;
            }

            return paths;
        }

        /// <summary>从 move 表取出单键条目，确定每个字母对应的绝对方向。</summary>
        private void ReadDirections(JVal move)
        {
            for (int i = 0; i < move.Count; i++)
            {
                string letters = move.Keys[i];
                if (string.IsNullOrEmpty(letters) || letters.Length != 1)
                {
                    continue;
                }

                MoveDirection direction;
                if (DirectionsByMove.TryGetValue(move.Values[i].AsString, out direction))
                {
                    _directions[letters] = direction;
                }
            }
        }

        private void BuildMoveAction(Dictionary<string, string> keyboardPaths)
        {
            _move = _map.AddAction(InputActionNames.Move, InputActionType.Value, "<Gamepad>/leftStick",
                expectedControlLayout: "Vector2");

            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Gamepad>/dpad/up")
                .With("Down", "<Gamepad>/dpad/down")
                .With("Left", "<Gamepad>/dpad/left")
                .With("Right", "<Gamepad>/dpad/right");

            var keyboard = _move.AddCompositeBinding("2DVector");
            foreach (var pair in _directions)
            {
                string path;
                if (keyboardPaths.TryGetValue(pair.Key, out path))
                {
                    keyboard.With(pair.Value.ToString(), path);
                }
            }
        }

        private void BuildAttackActions(JVal normalAttacks, Dictionary<string, string> keyboardPaths)
        {
            for (int i = 0; i < normalAttacks.Count; i++)
            {
                string letter = normalAttacks.Keys[i];
                string attack = normalAttacks.Values[i].AsString;

                var action = _map.FindAction(attack);
                if (action == null)
                {
                    action = _map.AddAction(attack, InputActionType.Button);

                    string gamepadPath;
                    if (GamepadAttackPaths.TryGetValue(attack, out gamepadPath))
                    {
                        action.AddBinding(gamepadPath);
                    }
                    else
                    {
                        Debug.LogWarning($"[SF] 招式 {attack} 没有配置手柄按键，只能用键盘出招");
                    }
                }

                string keyboardPath;
                if (keyboardPaths.TryGetValue(letter, out keyboardPath))
                {
                    action.AddBinding(keyboardPath);
                }

                _attacks[letter] = action;
            }
        }

        /// <summary>某个按键（字母）当前是否处于按下状态。</summary>
        private bool IsHeld(string letter)
        {
            MoveDirection direction;
            if (_directions.TryGetValue(letter, out direction) && _move != null)
            {
                var axis = _move.ReadValue<Vector2>();
                switch (direction)
                {
                    case MoveDirection.Up:
                        return axis.y >= StickThreshold;
                    case MoveDirection.Down:
                        return axis.y <= -StickThreshold;
                    case MoveDirection.Left:
                        return axis.x <= -StickThreshold;
                    default:
                        return axis.x >= StickThreshold;
                }
            }

            InputAction attack;
            return _attacks.TryGetValue(letter, out attack) && attack.IsPressed();
        }

        private string ReadHeldCombination()
        {
            string combination = string.Empty;
            for (int i = 0; i < _moveKeys.Count; i++)
            {
                if (IsHeld(_moveKeys[i]))
                {
                    combination += _moveKeys[i];
                }
            }

            return combination;
        }

        private string ReadFirstHeld()
        {
            for (int i = 0; i < _moveKeys.Count; i++)
            {
                if (IsHeld(_moveKeys[i]))
                {
                    return _moveKeys[i];
                }
            }

            return null;
        }

        private string LookupMove()
        {
            string operation;
            if (_activeMoves.TryGetValue(ReadHeldCombination(), out operation))
            {
                return operation;
            }

            string first = ReadFirstHeld();
            if (first != null && _activeMoves.TryGetValue(first, out operation))
            {
                return operation;
            }

            return null;
        }

        private void Tick()
        {
            if (_isLocked)
            {
                return;
            }

            foreach (var pair in _normalAttacks)
            {
                string letter = pair.Key;
                string attack = pair.Value;
                bool isDown = IsHeld(letter);
                bool wasDown = _previousHeld.ContainsKey(letter) && _previousHeld[letter];

                if (isDown && !wasDown && !(_attackLatched.ContainsKey(attack) && _attackLatched[attack]))
                {
                    ScheduleAttack(attack);
                }

                if (!isDown && wasDown)
                {
                    _attackLatched[attack] = false;
                }

                _previousHeld[letter] = isDown;
            }

            for (int i = 0; i < _moveKeys.Count; i++)
            {
                _held[_moveKeys[i]] = IsHeld(_moveKeys[i]);
            }

            string move = LookupMove();
            if (move != null)
            {
                Matched?.Invoke(move);

                string last = _buffer.Count > 0 ? _buffer.Last() : null;
                if (last == null || move != last)
                {
                    _buffer.Enqueue(move);
                }
            }
            else
            {
                Unmatched?.Invoke(null);
            }

            if (++_sampleCount % _sampleInterval == 0)
            {
                _sampleCount = 0;
                _buffer.Clear();
            }
        }

        private void ScheduleAttack(string attack)
        {
            string pending = attack;
            _clock.Timeout(() =>
            {
                _attackLatched[pending] = true;

                if (_buffer.Count == 0)
                {
                    string move = LookupMove();
                    if (move != null)
                    {
                        _buffer.Enqueue(move);
                    }
                }

                _buffer.Enqueue(pending);

                string combination = string.Join(",", _buffer.ToArray());
                _buffer.Clear();

                string special;
                if (_specialAttacks.TryGetValue(combination, out special))
                {
                    Matched?.Invoke(special);
                    return;
                }

                Matched?.Invoke(pending);
            }, AttackScheduleDelayMs);
        }
    }
}
