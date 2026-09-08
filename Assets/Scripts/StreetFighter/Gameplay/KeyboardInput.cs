using System;
using System.Collections.Generic;
using System.Linq;
using StreetFighter.Core;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 复刻 interface.js 的 KeyManage：
    /// 移动键持续采样 + 攻击键边缘触发 + 短动作序列匹配出招表。
    /// </summary>
    public sealed class KeyboardInput
    {
        /// <summary>攻击键按下后延迟多久再判定，用于攒出招序列。</summary>
        private const float AttackScheduleDelayMs = 50f;

        private static readonly Dictionary<string, KeyCode> KeyBindings = new Dictionary<string, KeyCode>
        {
            { "a", KeyCode.A },
            { "s", KeyCode.S },
            { "d", KeyCode.D },
            { "w", KeyCode.W },
            { "j", KeyCode.J },
            { "k", KeyCode.K },
            { "u", KeyCode.U },
            { "i", KeyCode.I },
            { "left", KeyCode.LeftArrow },
            { "right", KeyCode.RightArrow },
            { "up", KeyCode.UpArrow },
            { "down", KeyCode.DownArrow },

            // 副机用小键盘（1 轻拳 / 2 重拳 / 4 轻腿 / 5 重腿），
            // 主键盘数字 1、2 留给模式切换，不参与攻击
            { "1", KeyCode.Keypad1 },
            { "2", KeyCode.Keypad2 },
            { "4", KeyCode.Keypad4 },
            { "5", KeyCode.Keypad5 },
        };

        private readonly GameClock _clock;
        private readonly GameClock.TimerHandle _timer;

        private readonly List<string> _moveKeys = new List<string>();
        private readonly Dictionary<string, string> _moveForward = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _moveMirrored = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _normalAttacks = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _specialAttacks = new Dictionary<string, string>();

        private readonly Dictionary<string, bool> _held = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _attackLatched = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _previousHeld = new Dictionary<string, bool>();
        private readonly Queue<string> _buffer = new Queue<string>();

        private readonly int _sampleInterval;
        private int _sampleCount;
        private bool _isLocked;

        private Dictionary<string, string> _activeMoves;

        /// <summary>匹配到动作（含出招）时派发，参数为动作名。</summary>
        public event Action<string> Matched;

        /// <summary>本帧没有匹配到任何移动 / 攻击时派发，参数为 null。</summary>
        public event Action<string> Unmatched;

        public KeyboardInput(GameClock clock, JVal keyMap)
        {
            _clock = clock;
            _sampleInterval = Mathf.Max(1, (int)(GameConfig.KeyFps / GameConfig.Fps));

            var mapping = keyMap.Get("mapping");
            for (int i = 0; i < mapping.Values.Count; i++)
            {
                string letter = mapping.Values[i].AsString;
                bool isAttack = keyMap.Get("attack").Get("normal").Has(letter);
                if (!isAttack && !_moveKeys.Contains(letter))
                {
                    _moveKeys.Add(letter);
                }
            }

            Fill(_moveForward, keyMap.Get("move"));
            Fill(_moveMirrored, keyMap.Get("move_mirror"));
            Fill(_normalAttacks, keyMap.Get("attack").Get("normal"));
            Fill(_specialAttacks, keyMap.Get("attack").Get("special"));
            _activeMoves = _moveForward;

            _timer = clock.Add(Tick);
            clock.Start(_timer);
        }

        /// <summary>朝向翻转后切换前后方向的含义。</summary>
        public void Mirror(int direction) => _activeMoves = direction == 1 ? _moveForward : _moveMirrored;

        /// <summary>恢复输入。</summary>
        public void Start() => _isLocked = false;

        /// <summary>暂停输入并清空缓冲。</summary>
        public void Stop()
        {
            _held.Clear();
            _buffer.Clear();
            _isLocked = true;
        }

        /// <summary>某个按键是否处于按下状态。</summary>
        public static bool IsHeld(string letter)
        {
            KeyCode code;
            return KeyBindings.TryGetValue(letter, out code) && Input.GetKey(code);
        }

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
