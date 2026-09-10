using System;
using System.Collections.Generic;
using StreetFighter.Core;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 规则驱动的电脑决策。
    /// 输入 = 距离分段 + 敌方动作状态 + 是否飞行道具 + 是否无敌；输出 = 高概率正确 / 低概率错误的动作。
    /// 决策表在静态构造期一次性建好，每帧只是按条件取引用，不产生任何分配。
    /// </summary>
    public sealed class AiController
    {
        /// <summary>失误判定的分母。</summary>
        private const int SkillRoll = 10;

        /// <summary>高水平判定的分子：命中率 = SkillLevel / SkillRoll，值越大 AI 越强。</summary>
        private const int SkillLevel = 8;

        private const string FallbackAction = StateNames.ForceWait;

        private sealed class Response
        {
            /// <summary>高水平时的候选动作，元素可以是 string 或 string[]（连续动作）。</summary>
            public object[] CorrectActions;

            /// <summary>失误时的候选动作。</summary>
            public object[] WrongActions;
        }

        #region 静态决策表

        // 失误动作：原表里大量重复，这里各复用一份
        private static readonly object[] WrongWait = { StateNames.ForceWait, StateNames.ForceWait };
        private static readonly object[] WrongWaitKick = { StateNames.ForceWait, StateNames.CrouchHeavyKick };

        // 连续动作：必须是 string[]，Enqueue 靠 as string[] 识别
        private static readonly string[] JumpThenHeavyKick = { StateNames.Jump, StateNames.HeavyKick };
        private static readonly string[] JumpForwardThenHeavyKick = { StateNames.JumpForward, StateNames.HeavyKick };
        private static readonly string[] JumpBackThenHeavyKick = { StateNames.JumpBack, StateNames.HeavyKick };
        private static readonly string[] JumpBackThenHeavyWave = { StateNames.JumpBack, StateNames.HeavyWaveBoxing };
        private static readonly string[] ForceBackTwice = { StateNames.ForceBack, StateNames.ForceBack };

        /// <summary>对手倒地或无敌：拉开距离或发波。</summary>
        private static readonly Response FallDownResponse = Create(
            new object[] { StateNames.JumpBack, StateNames.ForceBack, StateNames.HeavyWaveBoxing }, WrongWait);

        /// <summary>对手旋风腿，近 / 中距离：用升龙对空。</summary>
        private static readonly Response WhirlKickNearResponse = Create(
            new object[] { StateNames.JumpHeavyImpactBoxing }, WrongWait);

        /// <summary>对手旋风腿，远 / 最远距离：蹲下躲开。</summary>
        private static readonly Response WhirlKickFarResponse = Create(
            new object[] { StateNames.Crouch }, WrongWait);

        private static readonly Response NearWaveResponse = Create(
            new object[]
            {
                StateNames.JumpWhirlKick, StateNames.JumpHeavyImpactBoxing,
                StateNames.JumpWhirlKick, StateNames.JumpWhirlKick, StateNames.JumpWhirlKick,
            },
            WrongWait);

        private static readonly Response NearJumpResponse = Create(
            new object[] { JumpThenHeavyKick, StateNames.JumpHeavyImpactBoxing, StateNames.JumpLightImpactBoxing },
            WrongWaitKick);

        private static readonly Response NearAttackStandResponse = Create(
            new object[]
            {
                StateNames.CrouchHeavyKick, StateNames.ForceStandUpDefense,
                StateNames.JumpWhirlKick, StateNames.JumpLightImpactBoxing,
                StateNames.JumpLightImpactBoxing, StateNames.JumpLightImpactBoxing,
            },
            WrongWait);

        private static readonly Response NearAttackCrouchResponse = Create(
            new object[]
            {
                StateNames.CrouchHeavyKick, StateNames.ForceStandCrouchDefense,
                StateNames.JumpWhirlKick, StateNames.JumpLightImpactBoxing,
                StateNames.JumpLightImpactBoxing, StateNames.JumpLightImpactBoxing,
            },
            WrongWait);

        private static readonly Response NearIdleResponse = Create(
            new object[]
            {
                StateNames.CrouchHeavyKick, StateNames.HeavyBoxing,
                StateNames.LightBoxing, StateNames.CrouchLightKick,
            },
            WrongWaitKick);

        private static readonly Response MiddleWaveResponse = Create(
            new object[]
            {
                JumpForwardThenHeavyKick,
                StateNames.JumpWhirlKick, StateNames.JumpWhirlKick, StateNames.JumpWhirlKick,
            },
            WrongWait);

        private static readonly Response MiddleJumpResponse = Create(
            new object[] { JumpThenHeavyKick }, WrongWaitKick);

        /// <summary>原表里蹲 / 站两个分支取的是同一个动作，这里合成一份。</summary>
        private static readonly Response MiddleAttackResponse = Create(
            new object[]
            {
                StateNames.StandCrouchDefense, StateNames.CrouchHeavyKick, StateNames.JumpWhirlKick,
                StateNames.JumpHeavyImpactBoxing, StateNames.CrouchHeavyBoxing,
            },
            WrongWait);

        private static readonly Response MiddleIdleResponse = Create(
            new object[] { StateNames.ForceBack, StateNames.LightWaveBoxing, JumpBackThenHeavyKick },
            WrongWaitKick);

        private static readonly Response FarWaveResponse = Create(
            new object[]
            {
                StateNames.JumpWhirlKick, StateNames.JumpWhirlKick, JumpForwardThenHeavyKick,
            },
            WrongWait);

        private static readonly Response FarJumpResponse = Create(
            new object[] { StateNames.JumpHeavyImpactBoxing, JumpForwardThenHeavyKick }, WrongWait);

        /// <summary>同 <see cref="MiddleAttackResponse"/>，蹲 / 站两个分支取值相同。</summary>
        private static readonly Response FarAttackResponse = Create(
            new object[]
            {
                StateNames.StandCrouchDefense, StateNames.JumpWhirlKick, StateNames.JumpBack,
                JumpForwardThenHeavyKick,
            },
            WrongWait);

        private static readonly Response FarIdleResponse = Create(
            new object[]
            {
                StateNames.ForceForward, StateNames.JumpBack, StateNames.HeavyWaveBoxing, ForceBackTwice,
            },
            WrongWaitKick);

        private static readonly Response FurthestWaveResponse = Create(
            new object[] { StateNames.LightWaveBoxing }, WrongWait);

        private static readonly Response FurthestIdleResponse = Create(
            new object[]
            {
                StateNames.ForceForward, StateNames.ForceForward,
                StateNames.ForceForward, StateNames.ForceForward,
                StateNames.LightWaveBoxing, StateNames.HeavyWaveBoxing, JumpBackThenHeavyWave,
            },
            WrongWaitKick);

        private static Response Create(object[] correct, object[] wrong) =>
            new Response { CorrectActions = correct, WrongActions = wrong };

        #endregion

        private readonly GameClock _clock;
        private readonly GameClock.TimerHandle _timer;
        private readonly Spirit _self;
        private readonly Spirit _enemy;
        private readonly Queue<object> _pending = new Queue<object>();
        private readonly Random _random = new Random();

        /// <summary>
        /// 唯一带动态内容的决策：维持防御时沿用当前状态名。
        /// CorrectActions 是长度 1 的复用数组，每帧只改元素而不重新分配。
        /// </summary>
        private readonly Response _holdDefenseResponse = new Response
        {
            CorrectActions = new object[1],
            WrongActions = WrongWait,
        };

        public AiController(GameClock clock, Spirit self)
        {
            _clock = clock;
            _self = self;
            _enemy = self.Enemy;
            _timer = clock.Add(Think);
        }

        public void Start() => _clock.Start(_timer);

        /// <summary>停止决策并丢弃尚未打出的连续技，避免下次开局立刻放出上一局的残留动作。</summary>
        public void Stop()
        {
            _clock.Stop(_timer);
            _pending.Clear();
        }

        private int Roll(int count) => _random.Next(count);

        private Response Respond(DistanceBand band)
        {
            string state = _enemy.StateName;
            var enemyStatus = _enemy.Status;
            var attack = enemyStatus.Attack;

            if (attack == AttackState.Attack && _self.Status.Attack == AttackState.Defense)
            {
                _holdDefenseResponse.CorrectActions[0] = _self.StateName;
                return _holdDefenseResponse;
            }

            if (attack == AttackState.FallDown || enemyStatus.IsInvincible)
            {
                return FallDownResponse;
            }

            if (state == StateNames.JumpWhirlKick || state == StateNames.LightJumpWhirlKick)
            {
                return band == DistanceBand.Near || band == DistanceBand.Middle
                    ? WhirlKickNearResponse
                    : WhirlKickFarResponse;
            }

            switch (band)
            {
                case DistanceBand.Near:
                    return RespondNear(enemyStatus, attack);

                case DistanceBand.Middle:
                    return RespondMiddle(enemyStatus, attack);

                case DistanceBand.Far:
                    return RespondFar(attack);

                default:
                    return RespondFurthest();
            }
        }

        private Response RespondNear(FighterStatus enemyStatus, AttackState attack)
        {
            if (_enemy.Wave.IsFiring)
            {
                return NearWaveResponse;
            }

            if (enemyStatus.IsJump())
            {
                return NearJumpResponse;
            }

            if (attack == AttackState.Attack)
            {
                return enemyStatus.IsCrouch() ? NearAttackCrouchResponse : NearAttackStandResponse;
            }

            return NearIdleResponse;
        }

        private Response RespondMiddle(FighterStatus enemyStatus, AttackState attack)
        {
            if (_enemy.Wave.IsFiring)
            {
                return MiddleWaveResponse;
            }

            if (enemyStatus.IsJump())
            {
                return MiddleJumpResponse;
            }

            return attack == AttackState.Attack ? MiddleAttackResponse : MiddleIdleResponse;
        }

        private Response RespondFar(AttackState attack)
        {
            if (_enemy.Wave.IsFiring)
            {
                return FarWaveResponse;
            }

            if (_enemy.Status.IsJump())
            {
                return FarJumpResponse;
            }

            return attack == AttackState.Attack ? FarAttackResponse : FarIdleResponse;
        }

        private Response RespondFurthest() =>
            _enemy.Wave.IsFiring ? FurthestWaveResponse : FurthestIdleResponse;

        private void Think()
        {
            if (_self.Actions.Count == 0 && _pending.Count > 0)
            {
                _self.Play((string)_pending.Dequeue() ?? FallbackAction);
                return;
            }

            var response = Respond(_self.Status.DistanceBand);
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
