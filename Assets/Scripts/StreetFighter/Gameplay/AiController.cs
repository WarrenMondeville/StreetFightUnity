using System;
using System.Collections.Generic;
using StreetFighter.Core;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 复刻 ai.js：规则驱动的电脑决策。
    /// 输入 = 距离分段 + 敌方动作状态 + 是否飞行道具 + 是否无敌；输出 = 高概率正确 / 低概率错误的动作。
    /// </summary>
    public sealed class AiController
    {
        private const int SkillRoll = 10;
        private const int SkillLevel = 11;
        private const string FallbackAction = StateNames.ForceWait;

        private sealed class Response
        {
            /// <summary>高水平时的候选动作，元素可以是 string 或 string[]（连续动作）。</summary>
            public object[] CorrectActions;

            /// <summary>失误时的候选动作。</summary>
            public object[] WrongActions;
        }

        private readonly GameClock _clock;
        private readonly GameClock.TimerHandle _timer;
        private readonly Spirit _self;
        private readonly Spirit _enemy;
        private readonly Queue<object> _pending = new Queue<object>();
        private readonly Random _random = new Random();

        public AiController(GameClock clock, Spirit self)
        {
            _clock = clock;
            _self = self;
            _enemy = self.Enemy;
            _timer = clock.Add(Think);
        }

        public void Start() => _clock.Start(_timer);

        public void Stop() => _clock.Stop(_timer);

        private int Roll(int count) => _random.Next(count);

        private Response Respond(DistanceBand band)
        {
            string state = _enemy.StateName;
            var enemyStatus = _enemy.Status;
            var attack = enemyStatus.Attack;

            if (attack == AttackState.Attack && _self.Status.Attack == AttackState.Defense)
            {
                return new Response
                {
                    CorrectActions = new object[] { _self.StateName },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                };
            }

            if (attack == AttackState.FallDown || enemyStatus.IsInvincible)
            {
                return new Response
                {
                    CorrectActions = new object[] { StateNames.JumpBack, StateNames.ForceBack, StateNames.HeavyWaveBoxing },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                };
            }

            if (state == StateNames.JumpWhirlKick || state == StateNames.LightJumpWhirlKick)
            {
                return band == DistanceBand.Near || band == DistanceBand.Middle
                    ? new Response
                    {
                        CorrectActions = new object[] { StateNames.JumpHeavyImpactBoxing },
                        WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                    }
                    : new Response
                    {
                        CorrectActions = new object[] { StateNames.Crouch },
                        WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                    };
            }

            switch (band)
            {
                case DistanceBand.Near:
                    return RespondNear(enemyStatus, attack);

                case DistanceBand.Middle:
                    return RespondMiddle(enemyStatus, attack);

                case DistanceBand.Far:
                    return RespondFar(enemyStatus, attack);

                default:
                    return RespondFurthest();
            }
        }

        private Response RespondNear(FighterStatus enemyStatus, AttackState attack)
        {
            if (_enemy.Wave.IsFiring)
            {
                return new Response
                {
                    CorrectActions = new object[]
                    {
                        StateNames.JumpWhirlKick, StateNames.JumpHeavyImpactBoxing,
                        StateNames.JumpWhirlKick, StateNames.JumpWhirlKick, StateNames.JumpWhirlKick,
                    },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                };
            }

            if (enemyStatus.IsJump())
            {
                return new Response
                {
                    CorrectActions = new object[]
                    {
                        new[] { StateNames.Jump, StateNames.HeavyKick },
                        StateNames.JumpHeavyImpactBoxing,
                        StateNames.JumpLightImpactBoxing,
                    },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.CrouchHeavyKick },
                };
            }

            if (attack == AttackState.Attack)
            {
                return new Response
                {
                    CorrectActions = new object[]
                    {
                        StateNames.CrouchHeavyKick,
                        enemyStatus.IsCrouch() ? StateNames.ForceStandCrouchDefense : StateNames.ForceStandUpDefense,
                        StateNames.JumpWhirlKick, StateNames.JumpLightImpactBoxing,
                        StateNames.JumpLightImpactBoxing, StateNames.JumpLightImpactBoxing,
                    },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                };
            }

            return new Response
            {
                CorrectActions = new object[]
                {
                    StateNames.CrouchHeavyKick, StateNames.HeavyBoxing,
                    StateNames.LightBoxing, StateNames.CrouchLightKick,
                },
                WrongActions = new object[] { StateNames.ForceWait, StateNames.CrouchHeavyKick },
            };
        }

        private Response RespondMiddle(FighterStatus enemyStatus, AttackState attack)
        {
            if (_enemy.Wave.IsFiring)
            {
                return new Response
                {
                    CorrectActions = new object[]
                    {
                        new[] { StateNames.JumpForward, StateNames.HeavyKick },
                        StateNames.JumpWhirlKick, StateNames.JumpWhirlKick, StateNames.JumpWhirlKick,
                    },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                };
            }

            if (enemyStatus.IsJump())
            {
                return new Response
                {
                    CorrectActions = new object[] { new[] { StateNames.Jump, StateNames.HeavyKick } },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.CrouchHeavyKick },
                };
            }

            if (attack == AttackState.Attack)
            {
                return new Response
                {
                    CorrectActions = new object[]
                    {
                        enemyStatus.IsCrouch() ? StateNames.StandCrouchDefense : StateNames.StandCrouchDefense,
                        StateNames.CrouchHeavyKick, StateNames.JumpWhirlKick,
                        StateNames.JumpHeavyImpactBoxing, StateNames.CrouchHeavyBoxing,
                    },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                };
            }

            return new Response
            {
                CorrectActions = new object[]
                {
                    StateNames.ForceBack, StateNames.LightWaveBoxing,
                    new[] { StateNames.JumpBack, StateNames.HeavyKick },
                },
                WrongActions = new object[] { StateNames.ForceWait, StateNames.CrouchHeavyKick },
            };
        }

        private Response RespondFar(FighterStatus enemyStatus, AttackState attack)
        {
            if (_enemy.Wave.IsFiring)
            {
                return new Response
                {
                    CorrectActions = new object[]
                    {
                        StateNames.JumpWhirlKick, StateNames.JumpWhirlKick,
                        new[] { StateNames.JumpForward, StateNames.HeavyKick },
                    },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                };
            }

            if (enemyStatus.IsJump())
            {
                return new Response
                {
                    CorrectActions = new object[]
                    {
                        StateNames.JumpHeavyImpactBoxing,
                        new[] { StateNames.JumpForward, StateNames.HeavyKick },
                    },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.CrouchHeavyKick },
                };
            }

            if (attack == AttackState.Attack)
            {
                return new Response
                {
                    CorrectActions = new object[]
                    {
                        enemyStatus.IsCrouch() ? StateNames.StandCrouchDefense : StateNames.StandCrouchDefense,
                        StateNames.JumpWhirlKick, StateNames.JumpBack,
                        new[] { StateNames.JumpForward, StateNames.HeavyKick },
                    },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                };
            }

            return new Response
            {
                CorrectActions = new object[]
                {
                    StateNames.ForceForward, StateNames.JumpBack, StateNames.HeavyWaveBoxing,
                    new[] { StateNames.ForceBack, StateNames.ForceBack },
                },
                WrongActions = new object[] { StateNames.ForceWait, StateNames.CrouchHeavyKick },
            };
        }

        private Response RespondFurthest()
        {
            if (_enemy.Wave.IsFiring)
            {
                return new Response
                {
                    CorrectActions = new object[] { StateNames.LightWaveBoxing },
                    WrongActions = new object[] { StateNames.ForceWait, StateNames.ForceWait },
                };
            }

            return new Response
            {
                CorrectActions = new object[]
                {
                    StateNames.ForceForward, StateNames.ForceForward, StateNames.ForceForward, StateNames.ForceForward,
                    StateNames.LightWaveBoxing, StateNames.HeavyWaveBoxing,
                    new[] { StateNames.JumpBack, StateNames.HeavyWaveBoxing },
                },
                WrongActions = new object[] { StateNames.ForceWait, StateNames.CrouchHeavyKick },
            };
        }

        private void Think()
        {
            if (_self.Actions.Count == 0 && _pending.Count > 0)
            {
                _self.Play((string)_pending.Dequeue() ?? FallbackAction);
                return;
            }

            var response = Respond(_self.Status.DistanceBand)
                           ?? new Response
                           {
                               CorrectActions = new object[] { FallbackAction },
                               WrongActions = new object[] { FallbackAction },
                           };

            object[] candidates = Roll(SkillRoll) < SkillLevel ? response.CorrectActions : response.WrongActions;
            Enqueue(candidates[Roll(candidates.Length)]);

            _self.Play((string)_pending.Dequeue() ?? StateNames.Wait);
        }

        private void Enqueue(object action)
        {
            var sequence = action as string[];
            if (sequence != null)
            {
                foreach (var step in sequence)
                {
                    _pending.Enqueue(step);
                }

                return;
            }

            _pending.Enqueue(action);
        }
    }
}
