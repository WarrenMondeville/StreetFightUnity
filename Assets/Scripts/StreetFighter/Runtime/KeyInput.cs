using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 interface.js 的 KeyManage：
    /// 移动键持续采样 + 攻击键边缘触发 + 短动作序列匹配出招表。
    /// </summary>
    public class KeyInput
    {
        private readonly GameClock _clock;
        private readonly GameClock.Handle _timer;
        private readonly Spirit _self;

        private readonly List<string> _order = new List<string>();
        private readonly Dictionary<string, string> _move1 = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _move2 = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _normal = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _special = new Dictionary<string, string>();

        private Dictionary<string, string> _move;

        private readonly Dictionary<string, bool> _map = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _attackMap = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _prev = new Dictionary<string, bool>();
        private readonly Q<string> _queue = new Q<string>();

        private readonly int _keyFps;
        private int _count;
        private bool _lock;

        private System.Action<string> _keydown;
        private System.Action<string> _keyup;

        public KeyInput(GameClock clock, Spirit self, JVal keyMap)
        {
            _clock = clock;
            _self = self;
            _keyFps = Mathf.Max(1, (int)(Cfg.KeyFps / Cfg.Fps));

            var mapping = keyMap.Get("mapping");
            for (int i = 0; i < mapping.Values.Count; i++)
            {
                string letter = mapping.Values[i].S;
                bool isAttack = keyMap.Get("attack").Get("normal").Has(letter);
                if (!isAttack && !_order.Contains(letter)) _order.Add(letter);
            }

            Fill(_move1, keyMap.Get("move"));
            Fill(_move2, keyMap.Get("move_mirror"));
            Fill(_normal, keyMap.Get("attack").Get("normal"));
            Fill(_special, keyMap.Get("attack").Get("special"));
            _move = _move1;

            _timer = clock.Add(Tick);
            clock.Start(_timer);
        }

        private static void Fill(Dictionary<string, string> dict, JVal src)
        {
            if (src.IsNull) return;
            for (int i = 0; i < src.Count; i++) dict[src.Keys[i]] = src.Values[i].S;
        }

        public void Match(System.Action<string> fn) => _keydown = fn;
        public void Unmatch(System.Action<string> fn) => _keyup = fn;

        public void Mirror(int dir) => _move = dir == 1 ? _move1 : _move2;

        public void Start() => _lock = false;

        public void Stop()
        {
            _map.Clear();
            _queue.Clean();
            _lock = true;
        }

        /// <summary>原版 DOM keyCode -> Unity 按键。</summary>
        public static bool Held(string letter)
        {
            switch (letter)
            {
                case "a": return Input.GetKey(KeyCode.A);
                case "s": return Input.GetKey(KeyCode.S);
                case "d": return Input.GetKey(KeyCode.D);
                case "w": return Input.GetKey(KeyCode.W);
                case "j": return Input.GetKey(KeyCode.J);
                case "k": return Input.GetKey(KeyCode.K);
                case "u": return Input.GetKey(KeyCode.U);
                case "i": return Input.GetKey(KeyCode.I);
                case "left": return Input.GetKey(KeyCode.LeftArrow);
                case "right": return Input.GetKey(KeyCode.RightArrow);
                case "up": return Input.GetKey(KeyCode.UpArrow);
                case "down": return Input.GetKey(KeyCode.DownArrow);
                // 副机用小键盘（1 轻拳 / 2 重拳 / 4 轻腿 / 5 重腿），主键盘数字 1、2 留给模式切换
                case "1": return Input.GetKey(KeyCode.Keypad1);
                case "2": return Input.GetKey(KeyCode.Keypad2);
                case "4": return Input.GetKey(KeyCode.Keypad4);
                case "5": return Input.GetKey(KeyCode.Keypad5);
                default: return false;
            }
        }

        private string GetKeyMap()
        {
            string key = "";
            for (int i = 0; i < _order.Count; i++)
                if (Held(_order[i])) key += _order[i];
            return key;
        }

        private string GetKeyMapFirst()
        {
            for (int i = 0; i < _order.Count; i++)
                if (Held(_order[i])) return _order[i];
            return null;
        }

        private string LookupMove()
        {
            string op;
            if (_move.TryGetValue(GetKeyMap(), out op)) return op;
            string first = GetKeyMapFirst();
            if (first != null && _move.TryGetValue(first, out op)) return op;
            return null;
        }

        private void Tick()
        {
            if (_lock) return;

            foreach (var kv in _normal)
            {
                string letter = kv.Key, atk = kv.Value;
                bool down = Held(letter);
                bool was = _prev.ContainsKey(letter) && _prev[letter];

                if (down && !was && !(_attackMap.ContainsKey(atk) && _attackMap[atk]))
                    ScheduleAttack(atk);
                if (!down && was) _attackMap[atk] = false;

                _prev[letter] = down;
            }

            for (int i = 0; i < _order.Count; i++) _map[_order[i]] = Held(_order[i]);

            string op = LookupMove();
            if (op != null)
            {
                if (_keydown != null) _keydown(op);
                string last = _queue.Last;
                if (last == null || op != last) _queue.Add(op);
            }
            else
            {
                if (_keyup != null) _keyup(null);
            }

            if (++_count % _keyFps == 0)
            {
                _count = 0;
                _queue.Clean();
            }
        }

        private void ScheduleAttack(string atk)
        {
            string pending = atk;
            _clock.Timeout(() =>
            {
                _attackMap[pending] = true;

                if (_queue.IsEmpty)
                {
                    string m = LookupMove();
                    if (m != null) _queue.Add(m);
                }
                _queue.Add(pending);

                var list = _queue.Get();
                string keys = string.Join(",", list.ToArray());
                _queue.Clean();

                string special;
                if (_special.TryGetValue(keys, out special))
                {
                    if (_keydown != null) _keydown(special);
                    return;
                }
                if (_keydown != null) _keydown(pending);
            }, 50);
        }
    }
}
