using System;
using System.Collections.Generic;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 ai.js：规则驱动的电脑决策。
    /// 输入 = 距离分段 + 敌方动作状态 + 是否飞行道具 + 是否无敌；输出 = 高概率正确 / 低概率错误的动作。
    /// </summary>
    public class Ai
    {
        private class Response
        {
            public object[] Correct;
            public object[] Wrong;
        }

        private readonly Spirit _self;
        private readonly Spirit _enemy;
        private readonly GameClock _clock;
        private readonly GameClock.Handle _timer;
        private readonly Q<object> _queue = new Q<object>();
        private readonly System.Random _rnd = new Random();
        private readonly int _level = 11;

        public Ai(GameClock clock, Spirit self)
        {
            _clock = clock;
            _self = self;
            _enemy = self.Enemy;
            _timer = clock.Add(Tick);
        }

        public void Start() => _clock.Start(_timer);
        public void Stop() => _clock.Stop(_timer);

        private int Rand(int n) => _rnd.Next(n);

        private Response Respond(string distance)
        {
            string state = _enemy.StateName;
            var es = _enemy.St;
            string attackType = es.AttackType;
            bool invincible = es.Invincible;

            if (attackType == "attack" && _self.St.AttackType == "defense")
                return new Response { Correct = new object[] { _self.StateName }, Wrong = new object[] { "force_wait", "force_wait" } };

            if (attackType == "fall_down" || invincible)
                return new Response
                {
                    Correct = new object[] { "jump_back", "force_back", "heavy_wave_boxing" },
                    Wrong = new object[] { "force_wait", "force_wait" }
                };

            if (state == "jump_whirl_kick" || state == "light_jump_whirl_kick")
            {
                if (distance == "near" || distance == "middle")
                    return new Response { Correct = new object[] { "jump_heavy_impact_boxing" }, Wrong = new object[] { "force_wait", "force_wait" } };
                return new Response { Correct = new object[] { "crouch" }, Wrong = new object[] { "force_wait", "force_wait" } };
            }

            if (distance == "near")
            {
                if (_enemy.WaveBoxing.Firing)
                    return new Response
                    {
                        Correct = new object[] { "jump_whirl_kick", "jump_heavy_impact_boxing", "jump_whirl_kick", "jump_whirl_kick", "jump_whirl_kick" },
                        Wrong = new object[] { "force_wait", "force_wait" }
                    };

                if (es.IsJump())
                    return new Response
                    {
                        Correct = new object[] { new[] { "jump", "heavy_kick" }, "jump_heavy_impact_boxing", "jump_light_impact_boxing" },
                        Wrong = new object[] { "force_wait", "crouch_heavy_kick" }
                    };

                if (attackType == "attack")
                    return new Response
                    {
                        Correct = new object[]
                        {
                            "crouch_heavy_kick",
                            es.IsCrouch() ? "force_stand_crouch_defense" : "force_stand_up_defense",
                            "jump_whirl_kick", "jump_light_impact_boxing", "jump_light_impact_boxing", "jump_light_impact_boxing"
                        },
                        Wrong = new object[] { "force_wait", "force_wait" }
                    };

                return new Response
                {
                    Correct = new object[] { "crouch_heavy_kick", "heavy_boxing", "light_boxing", "crouch_light_kick" },
                    Wrong = new object[] { "force_wait", "crouch_heavy_kick" }
                };
            }

            if (distance == "middle")
            {
                if (_enemy.WaveBoxing.Firing)
                    return new Response
                    {
                        Correct = new object[] { new[] { "jump_forward", "heavy_kick" }, "jump_whirl_kick", "jump_whirl_kick", "jump_whirl_kick" },
                        Wrong = new object[] { "force_wait", "force_wait" }
                    };

                if (es.IsJump())
                    return new Response
                    {
                        Correct = new object[] { new[] { "jump", "heavy_kick" } },
                        Wrong = new object[] { "force_wait", "crouch_heavy_kick" }
                    };

                if (attackType == "attack")
                    return new Response
                    {
                        Correct = new object[]
                        {
                            es.IsCrouch() ? "stand_crouch_defense" : "stand_crouch_defense",
                            "crouch_heavy_kick", "jump_whirl_kick", "jump_heavy_impact_boxing", "crouch_heavy_boxing"
                        },
                        Wrong = new object[] { "force_wait", "force_wait" }
                    };

                return new Response
                {
                    Correct = new object[] { "force_back", "light_wave_boxing", new[] { "jump_back", "heavy_kick" } },
                    Wrong = new object[] { "force_wait", "crouch_heavy_kick" }
                };
            }

            if (distance == "far")
            {
                if (_enemy.WaveBoxing.Firing)
                    return new Response
                    {
                        Correct = new object[] { "jump_whirl_kick", "jump_whirl_kick", new[] { "jump_forward", "heavy_kick" } },
                        Wrong = new object[] { "force_wait", "force_wait" }
                    };

                if (es.IsJump())
                    return new Response
                    {
                        Correct = new object[] { "jump_heavy_impact_boxing", new[] { "jump_forward", "heavy_kick" } },
                        Wrong = new object[] { "force_wait", "crouch_heavy_kick" }
                    };

                if (attackType == "attack")
                    return new Response
                    {
                        Correct = new object[]
                        {
                            es.IsCrouch() ? "stand_crouch_defense" : "stand_crouch_defense",
                            "jump_whirl_kick", "jump_back", new[] { "jump_forward", "heavy_kick" }
                        },
                        Wrong = new object[] { "force_wait", "force_wait" }
                    };

                return new Response
                {
                    Correct = new object[] { "force_forward", "jump_back", "heavy_wave_boxing", new[] { "force_back", "force_back" } },
                    Wrong = new object[] { "force_wait", "crouch_heavy_kick" }
                };
            }

            if (_enemy.WaveBoxing.Firing)
                return new Response
                {
                    Correct = new object[] { "light_wave_boxing" },
                    Wrong = new object[] { "force_wait", "force_wait" }
                };

            return new Response
            {
                Correct = new object[]
                {
                    "force_forward", "force_forward", "force_forward", "force_forward",
                    "light_wave_boxing", "heavy_wave_boxing", new[] { "jump_back", "heavy_wave_boxing" }
                },
                Wrong = new object[] { "force_wait", "crouch_heavy_kick" }
            };
        }

        private void Tick()
        {
            if (_self.Queue.IsEmpty && !_queue.IsEmpty)
            {
                _self.Play((string)_queue.Dequeue() ?? "force_wait");
                return;
            }

            string distance = _self.St.DistanceType;
            var re = Respond(distance);
            if (re == null)
                re = new Response { Correct = new object[] { "force_wait" }, Wrong = new object[] { "force_wait" } };

            object[] pick = Rand(10) < _level ? re.Correct : re.Wrong;
            object item = pick[Rand(pick.Length)];
            AddItem(item);

            _self.Play((string)_queue.Dequeue() ?? "wait");
        }

        private void AddItem(object item)
        {
            var arr = item as string[];
            if (arr != null) _queue.AddRange(arr);
            else _queue.Add(item);
        }
    }
}
