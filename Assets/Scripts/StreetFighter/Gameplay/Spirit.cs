using System.Collections.Generic;
using StreetFighter.Core;
using StreetFighter.Game;
using StreetFighter.View;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 角色总控。
    /// 组合了帧动画、位移插值、动作队列、锁、输入、碰撞、近战判定、波动拳、状态机与舞台推挤。
    /// </summary>
    public sealed class Spirit : ICollidable, IMovable
    {
        /// <summary>动作队列元素：动作名 + states 中的定义（原版是 states 的浅拷贝 + currState 标记）。</summary>
        public sealed class SpiritAction
        {
            public SpiritAction(string name, JVal definition)
            {
                Name = name;
                Definition = definition;
            }

            public string Name { get; }

            public JVal Definition { get; }
        }

        private const float MinLeft = 15f;
        private const float RightPadding = 85f;
        private const float JumpBackRightPadding = 130f;

        /// <summary>贴边状态下转身的微调距离。</summary>
        private const float TurnShiftAtBorder = 18f;

        private const float TurnShiftFree = 105f;

        /// <summary>跳跃时的阴影宽度。</summary>
        private const float JumpShadowWidth = 62f;

        private const float SomesaultRightShift = 60f;

        /// <summary>attack_type = 2 表示带近身判定框。</summary>
        private const int MeleeAttackType = 2;

        private const int AttackConfigValueCount = 7;

        private readonly GameClock _clock;
        private SpiritView _view;

        public Spirit(GameClock clock, string key, JVal spiritConfig)
        {
            _clock = clock;
            Key = key;
            RawStates = spiritConfig;
            States = spiritConfig.Get("states");
        }

        #region 配置与标识

        /// <summary>角色键，如 RYU1。</summary>
        public string Key { get; }

        /// <summary>states 表。</summary>
        public JVal States { get; }

        /// <summary>完整角色配置（含 keyMap）。</summary>
        public JVal RawStates { get; }

        /// <summary>当前动作名（play 的键）。</summary>
        public string StateName { get; private set; }

        /// <summary>当前正在播放的组合动作名（compose 的键）。</summary>
        public string CurrentState { get; private set; }

        public string DefaultState => States.Get(StateNames.Default).AsString;

        #endregion

        #region 位置与包围盒

        public float Left { get; set; }

        public float Top { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        /// <summary>站立时的 left 基准值。</summary>
        public float FloorLeft { get; set; }

        /// <summary>站立时的 top 基准值。</summary>
        public float FloorTop { get; set; }

        /// <summary>站立时的高度基准值。</summary>
        public float FloorHeight { get; set; }

        public int Position { get; set; }

        public int Direction { get; set; } = 1;

        /// <summary>当前是否贴在屏幕某一侧。</summary>
        public Side Border { get; set; }

        /// <summary>本段位移的横向符号，用于判断顶到边界的方向。</summary>
        public float HorizontalMoveSign { get; private set; }

        /// <summary>本段位移的纵向符号。</summary>
        public float VerticalMoveSign { get; private set; }

        /// <summary>脚下阴影的 left（每帧刷新，交给表现层绘制）。</summary>
        public float ShadowLeft { get; private set; }

        /// <summary>脚下阴影的宽度。</summary>
        public float ShadowWidth { get; private set; }

        #endregion

        #region 子系统

        public Spirit Enemy { get; private set; }

        public BloodBar BloodBar { get; set; }

        public FrameAnimator Frames { get; private set; }

        public Mover Motion { get; private set; }

        /// <summary>待播放的动作队列。</summary>
        public Queue<SpiritAction> Actions { get; private set; }

        public ActionLock Lock { get; private set; }

        public FighterInput Keys { get; private set; }

        public BodyCollider Collider { get; private set; }

        public MeleeAttack Attack { get; private set; }

        public WaveProjectile Wave { get; private set; }

        public FighterStatus Status { get; private set; }

        public Stage Stage { get; private set; }

        public AiController Ai { get; set; }

        #endregion

        public object Master => null;

        /// <summary>设置对手（必须在 <see cref="Initialize"/> 之前调用）。</summary>
        public void SetEnemy(Spirit enemy) => Enemy = enemy;

        /// <summary>绑定表现层。</summary>
        public void AttachView(SpiritView view) => _view = view;

        public void Initialize(float floorLeft, float floorTop, int direction)
        {
            FloorLeft = floorLeft;
            FloorTop = floorTop;

            var definition = States.Get(DefaultState);
            FloorHeight = SpriteLibrary.GetSize(GameConfig.GetBackground(definition)).y;
            Direction = direction;

            Frames = new FrameAnimator(_clock);
            Motion = new Mover(_clock, this);
            Actions = new Queue<SpiritAction>();
            Lock = new ActionLock();
            Keys = new FighterInput(_clock, RawStates.Get("keyMap"));
            Collider = new BodyCollider(this);
            Attack = new MeleeAttack(_clock, this);
            Wave = new WaveProjectile(_clock, this);
            Status = new FighterStatus(this, _clock);
            Stage = new Stage(this);

            BindEvents();

            ChangeBackground(GameConfig.GetBackground(definition), GameConfig.GetFrameCount(definition), 0);
            Play(DefaultState);
            Motion.MoveTo(floorLeft, floorTop);
        }

        #region 状态调度

        /// <summary>播放一个动作。</summary>
        /// <param name="state">play 表中的动作名。</param>
        /// <param name="force">为 true 时无视锁与重复判断。</param>
        public void Play(string state, bool force = false)
        {
            if (GameConfig.GetPlay(state).IsNull)
            {
                return;
            }

            if (Status.IsCrouch() && (state == StateNames.Wait || state == StateNames.ForceWait))
            {
                Play(StateNames.StandUp, true);
                return;
            }

            if (JumpCombo(state) && state != StateNames.ForceWait)
            {
                return;
            }

            if (Defense(state))
            {
                return;
            }

            int lockLevel = GameConfig.GetPlayLock(state);
            int oldLevel = Lock.Level;

            if (!force)
            {
                if (state == StateName || (Lock.IsLocked && oldLevel >= lockLevel))
                {
                    return;
                }
            }

            Lock.Set(lockLevel);
            Actions.Clear();

            var compose = GameConfig.GetPlay(state).Get("compose");
            for (int i = 0; i < compose.Count; i++)
            {
                string actionName = compose.Get(i).AsString;
                Actions.Enqueue(new SpiritAction(actionName, States.Get(actionName)));
            }

            StateName = state;

            if (state == StateNames.LightWaveBoxing || state == StateNames.HeavyWaveBoxing)
            {
                if (Wave.IsFiring)
                {
                    Actions.Clear();
                    Lock.Set(0);
                    Play(state == StateNames.LightWaveBoxing ? StateNames.LightBoxing : StateNames.HeavyBoxing);
                    return;
                }

                Wave.Start(Direction, States.Get(state == StateNames.LightWaveBoxing
                    ? StateNames.LightWave
                    : StateNames.HeavyWave));
            }

            FireFrames();
            Motion.Events.Invoke(GameEvents.PlayStart);
        }

        /// <summary>空中组合技：在当前动作上叠加一段序列帧。</summary>
        /// <returns>true 表示已被空中逻辑接管，调用方不应继续普通播放。</returns>
        private bool JumpCombo(string state)
        {
            if (StateName == null || !Status.IsJump())
            {
                return false;
            }

            if (state == StateNames.JumpFallDown || state == StateNames.JumpDead || state == StateNames.AfterDead2
                || state == StateNames.HeavyAttackedFallDown || state == StateNames.CrouchKickAttackedFall)
            {
                Frames.Combo.Stop();
                return false;
            }

            var table = States.Get(StateNames.Combo);
            var combo = table.IsNull ? JVal.Nil : table.Get($"{StateName}_{state}");
            if (combo.IsNull)
            {
                return true;
            }

            bool started = Frames.Combo.Start(GameConfig.GetBackground(combo), GameConfig.GetFrameCount(combo),
                GameConfig.GetRepeatPattern(combo), combo.Get("afterFrame").AsInt);

            Status.SetAttackType(GameConfig.GetAttackType(combo));

            var attackConfig = combo.Get("attack_config");
            var values = new float[attackConfig.Count];
            var texts = new string[attackConfig.Count];
            for (int i = 0; i < attackConfig.Count; i++)
            {
                values[i] = attackConfig.Get(i).AsFloat;
                texts[i] = attackConfig.Get(i).AsString;
            }

            Status.SetAttackPower(GameConfig.ReadFloatArray(combo, "attack_power"));

            if (started)
            {
                Attack.StickStart(values, texts,
                    GameConfig.ReadFloatArray(combo, "effect_position"),
                    GameConfig.ReadStringArray(combo, "sound"));

                Frames.Combo.OnFinished(() =>
                {
                    Status.SetAttack(AttackState.Wait);
                    Attack.Stop();
                });
            }

            return true;
        }

        /// <summary>后撤 / 蹲防的姿态切换。</summary>
        private bool Defense(string state)
        {
            if (state == StateNames.Back)
            {
                if (Enemy.Wave.IsFiring
                    || (Enemy.Status.Attack == AttackState.Attack && Status.DistanceBand != DistanceBand.Furthest))
                {
                    if (Status.Attack == AttackState.Wait)
                    {
                        Play(StateNames.StandUpDefense);
                        Status.SetAttack(AttackState.Defense);
                    }

                    return true;
                }
            }
            else if (state == StateNames.StandCrouchDefense)
            {
                if (!Enemy.Wave.IsFiring
                    && (Enemy.Status.Attack != AttackState.Attack || Status.DistanceBand == DistanceBand.Furthest))
                {
                    Play(StateNames.Crouch);
                    Status.SetAttack(AttackState.Wait);
                    return true;
                }
            }

            return false;
        }

        /// <summary>取出队列里的下一个动作并真正起播。</summary>
        public void FireFrames()
        {
            var action = Actions.Count > 0 ? Actions.Dequeue() : null;

            if (action == null)
            {
                int oldDirection = Direction;
                int newDirection = Left > Enemy.Left ? -1 : 1;
                if (newDirection != oldDirection)
                {
                    Left += (Border != Side.None ? TurnShiftAtBorder : TurnShiftFree) * newDirection;
                }

                Direction = Left > Enemy.Left ? -1 : 1;
                Motion.Mirror(Direction);
                Keys.Mirror(Direction);

                if (Status.IsCrouch())
                {
                    Play(StateNames.StandUp, true);
                    return;
                }

                string defaultState = DefaultState;
                Actions.Enqueue(new SpiritAction(null, States.Get(defaultState)));
                Lock.Set(0);
                StateName = defaultState;
                Frames.Combo.Stop();

                if (Attack.Mode == MeleeMode.Stick)
                {
                    Attack.Stop();
                }

                FireFrames();
                return;
            }

            var definition = action.Definition;
            string background = GameConfig.GetBackground(definition);
            int frameCount = GameConfig.GetFrameCount(definition);
            int position = definition.Get("position").AsInt;

            ChangeBackground(background, frameCount, position);

            float? autoTop = null;
            if (GameConfig.HasAutoTop(definition))
            {
                autoTop = FloorTop - Top + (FloorHeight - Height) * GameConfig.Zoom;
            }

            float top = autoTop ?? GameConfig.GetEaseValue(definition, 1);

            CurrentState = action.Name;

            Frames.Start(background, frameCount, (int)GameConfig.GetEaseValue(definition, 2),
                GameConfig.GetRepeatPattern(definition), position, Direction);
            Motion.Start(GameConfig.GetEaseValue(definition, 0) * Direction, top,
                GameConfig.GetEaseValue(definition, 2) * GameConfig.Fps * frameCount,
                GameConfig.GetEaseFunction(definition));

            float horizontal = GameConfig.GetEaseValue(definition, 0) * Direction;
            HorizontalMoveSign = horizontal == 0f ? 0f : Mathf.Sign(horizontal);
            VerticalMoveSign = top == 0f ? 0f : Mathf.Sign(top);

            int attackType = GameConfig.GetAttackType(definition);
            Status.SetAttackType(attackType);
            Status.SetAttackPower(GameConfig.ReadFloatArray(definition, "attack_power"));

            // 部分动作（如波动拳）只有 attack_type 没有判定配置，需要判空
            var attackConfig = definition.Get("attack_config");
            if (attackType == MeleeAttackType && !attackConfig.IsNull)
            {
                var easingValues = new float[AttackConfigValueCount];
                var easingTexts = new string[AttackConfigValueCount];
                for (int i = 0; i < AttackConfigValueCount; i++)
                {
                    easingValues[i] = attackConfig.Get(i + 2).AsFloat;
                    easingTexts[i] = attackConfig.Get(i + 2).AsString;
                }

                easingValues[0] = attackConfig.Get(2).AsFloat * Direction;

                Attack.Start(attackConfig.Get(0).AsFloat, attackConfig.Get(1).AsFloat, easingValues, easingTexts,
                    GameConfig.ReadFloatArray(definition, "effect_position"),
                    GameConfig.ReadStringArray(definition, "sound"));
            }

            string specialSound = definition.Get("specialSound").AsString;
            if (!string.IsNullOrEmpty(specialSound))
            {
                Attack.Audio.Play(specialSound);
            }

            if (CurrentState != StateNames.Wait)
            {
                Enemy.Motion.Events.Invoke(GameEvents.StopPush);
            }

            Motion.Events.Invoke(GameEvents.FramesStart);
        }

        /// <summary>切换图集并按新尺寸补偿包围盒。</summary>
        public void ChangeBackground(string background, int frameCount, int position)
        {
            float oldWidth = Width;
            Width = SpriteLibrary.GetSize(background).x / frameCount;
            float deltaWidth = (Width - oldWidth) * GameConfig.Zoom;

            if (Direction == -1)
            {
                Left -= deltaWidth;
            }

            if (CurrentState == StateNames.SomesaultUp && Border == Side.Right)
            {
                Left -= SomesaultRightShift;
            }

            float oldHeight = Height;
            Height = SpriteLibrary.GetSize(background).y;
            Top -= (Height - oldHeight) * GameConfig.Zoom;

            Position = position;
        }

        public float CrossBorder(float left)
        {
            float rightPadding = StateName == StateNames.JumpBack ? JumpBackRightPadding : RightPadding;

            if (left < MinLeft)
            {
                Border = Side.Left;
                return MinLeft;
            }

            if (left + Width > GameConfig.MapWidth - rightPadding)
            {
                Border = Side.Right;
                return GameConfig.MapWidth - Width - rightPadding;
            }

            Border = Side.None;
            return left;
        }

        #endregion

        #region 事件绑定

        private void BindEvents()
        {
            Frames.Events.AddListener(GameEvents.FramesDone, OnFramesDone);
            Frames.Events.AddListener(GameEvents.FramesStart, OnFramesStart);
            Frames.Events.AddListener(GameEvents.FrameStart, OnFrameStart);

            Motion.Events.AddListener(GameEvents.StopPush, () => SetForwardEasing(150f));
            Motion.Events.AddListener(GameEvents.FrameStart, OnMotionFrameStart);

            Collider.Hit += OnCollision;

            Keys.Matched += OnInputMatched;
            Keys.Unmatched += OnInputUnmatched;
        }

        private void OnFramesDone()
        {
            if (CurrentState == StateNames.CrouchKickAttackedFall
                || CurrentState == StateNames.AfterHeavyAttackedFallDown
                || CurrentState == StateNames.Dead)
            {
                Attack.Audio.Play(SoundPaths.Fall);
            }

            if (CurrentState == StateNames.SomesaultUp)
            {
                Status.SetInvincible(100);
            }

            FireFrames();
        }

        private void OnFramesStart()
        {
            Motion.Unlock();
            if (StateName == StateNames.Wait)
            {
                Motion.Correct();
            }
        }

        private void OnFrameStart()
        {
            if (StateName == StateNames.Wait || StateName == StateNames.Crouch)
            {
                Direction = Left > Enemy.Left ? -1 : 1;
            }

            Collider.Check();
            Motion.Move();

            bool isJumping = Status.IsJump();
            ShadowLeft = isJumping && Direction == -1 ? Left + (Width - JumpShadowWidth) * GameConfig.Zoom : Left;
            ShadowWidth = isJumping ? JumpShadowWidth : Width;

            Status.CheckEnemyDistance(Enemy);
        }

        private void OnMotionFrameStart()
        {
            Enemy.Motion.Events.RemoveListener(GameEvents.FrameDone);

            bool pushingBorder = Border == Side.Left && HorizontalMoveSign == -1f
                                 || Border == Side.Right && HorizontalMoveSign == 1f;

            if (!pushingBorder)
            {
                Stage.End();
                return;
            }

            Enemy.Stage.Begin();

            if (Status.DistanceBand == DistanceBand.Furthest)
            {
                return;
            }

            Stage.Scroll(Border);

            Enemy.Motion.Events.AddListener(GameEvents.FrameDone, () =>
            {
                if (Enemy.HorizontalMoveSign == 0f && Enemy.VerticalMoveSign == 0f)
                {
                    Stage.PushEnemy();
                }
                else if (Stage.IsScrolling)
                {
                    Enemy.Motion.StagePush(Stage.ScrollValue);
                }
                else
                {
                    Enemy.Motion.StopStagePush();
                }
            }, true);
        }

        private void OnCollision(ICollidable other, Side side)
        {
            if (!ReferenceEquals(other, Enemy))
            {
                return;
            }

            if (StateName == StateNames.Forward
                && (Enemy.StateName == StateNames.Wait || Enemy.StateName == StateNames.Crouch))
            {
                Enemy.Motion.Push(3f);
                SetForwardEasing(90f);

                if (Enemy.Border != Side.None)
                {
                    Motion.Lock(Enemy.Border);
                }
            }

            if (CurrentState == StateNames.JumpForwardDown || CurrentState == StateNames.JumpDown)
            {
                Enemy.Motion.Push((234f - Status.Distance) / 40f);
            }

            if (StateName == StateNames.Forward && Enemy.StateName == StateNames.Forward)
            {
                Motion.Lock(side);
            }
        }

        private void OnInputMatched(string state)
        {
            if (Status.IsCrouch() && !States.Get(StateNames.CrouchPrefix + state).IsNull)
            {
                Play(StateNames.CrouchPrefix + state);
                return;
            }

            if (!Status.IsJump())
            {
                var near = States.Get(StateNames.NearPrefix + state);
                if (!near.IsNull && Status.Distance <= near.Get("near").AsFloat)
                {
                    Play(StateNames.NearPrefix + state);
                    return;
                }
            }

            Play(state);
        }

        private void OnInputUnmatched(string state)
        {
            if (StateName == StateNames.StandUp && state == StateNames.Wait)
            {
                return;
            }

            if (StateName == StateNames.Back || StateName == StateNames.Forward
                || StateName == StateNames.StandUpDefense || StateName == StateNames.StandCrouchDefense
                || StateName == StateNames.Crouch)
            {
                Play(DefaultState);
            }
        }

        /// <summary>原版在推挤时动态改写 forward 的横向位移量。</summary>
        private void SetForwardEasing(float value)
        {
            var forward = States.Get(StateNames.Forward);
            if (forward.IsNull)
            {
                return;
            }

            forward.Get("easing").Get(0).SetNumber(value);
        }

        #endregion

        #region 渲染同步

        public void Render(float zoom, int order)
        {
            if (_view == null)
            {
                return;
            }

            _view.Render(this, zoom, order);
            Attack.Render(zoom);
            Wave.Render(zoom, order + 5);
        }

        #endregion
    }
}
