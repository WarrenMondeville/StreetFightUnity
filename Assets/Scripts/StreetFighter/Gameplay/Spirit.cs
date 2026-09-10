using System;
using System.Collections.Generic;
using StreetFighter.Config;
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
        /// <summary>动作队列元素：状态名 + 配置里的状态定义。</summary>
        public sealed class SpiritAction
        {
            public SpiritAction(string name, StateConfig definition)
            {
                Name = name;
                Definition = definition;
            }

            /// <summary>状态名，回到默认状态时为 null。</summary>
            public string Name { get; }

            /// <summary>状态定义。</summary>
            public StateConfig Definition { get; }
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

        private readonly GameClock _clock;
        private SpiritView _view;

        /// <summary><see cref="StateName"/> 的后备字段。</summary>
        private string _stateName;

        /// <summary>前进步伐的横向位移，推挤时会在运行时微调（不写回配置资产）。</summary>
        private float _forwardSpeed;

        /// <summary>本帧是否正在顶边推挤对手（决定对手位移回调要不要生效）。</summary>
        private bool _isPushingEnemy;

        /// <summary>对手位移结束回调，构造期建好一次，避免每帧 new 闭包。</summary>
        private readonly Action _onEnemyFrameDone;

        public Spirit(GameClock clock, string key, FighterAsset config)
        {
            _clock = clock;
            Key = key;
            Config = config;
            _onEnemyFrameDone = OnEnemyFrameDone;
        }

        #region 配置与标识

        /// <summary>角色键，如 RYU1。</summary>
        public string Key { get; }

        /// <summary>角色配置资产（状态表、组合技表、按键表）。</summary>
        public FighterAsset Config { get; }

        /// <summary>
        /// 当前动作名（play 的键）。
        /// 赋值时同步刷新 <see cref="FighterStatus"/> 的姿态标记，所以姿态判断不必每帧做字符串匹配。
        /// </summary>
        public string StateName
        {
            get => _stateName;
            private set
            {
                if (_stateName == value)
                {
                    return;
                }

                _stateName = value;

                // Status 在 Initialize 中创建，创建前的赋值由它自己构造时刷新一次补上
                Status?.RefreshPoseFlags();
            }
        }

        /// <summary>当前正在播放的组合动作名（compose 的键）。</summary>
        public string CurrentState { get; private set; }

        /// <summary>默认状态名。</summary>
        public string DefaultState => Config.DefaultState;

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

            var definition = Config.GetState(DefaultState);
            FloorHeight = SpriteLibrary.GetSize(definition.Background).y;
            Direction = direction;

            var forward = Config.GetState(StateNames.Forward);
            _forwardSpeed = forward != null ? forward.Easing.Dx : 0f;

            Frames = new FrameAnimator(_clock);
            Motion = new Mover(_clock, this);
            Actions = new Queue<SpiritAction>();
            Lock = new ActionLock();
            Keys = new FighterInput(_clock, Config.KeyMap);
            Collider = new BodyCollider(this);
            Attack = new MeleeAttack(_clock, this);
            Wave = new WaveProjectile(_clock, this);
            Status = new FighterStatus(this, _clock);
            Stage = new Stage(this);

            BindEvents();

            // 对手血条监听放在这里：此时双方的 BloodBar 都已由 GameManager 赋值
            Attack.BindEnemyBloodBar();

            ChangeBackground(definition.Background, definition.FrameCount, 0);
            Play(DefaultState);
            Motion.MoveTo(floorLeft, floorTop);
        }

        #region 状态调度

        /// <summary>播放一个动作。</summary>
        /// <param name="state">play 表中的动作名。</param>
        /// <param name="force">为 true 时无视锁与重复判断。</param>
        public void Play(string state, bool force = false)
        {
            var play = GameConfig.GetPlay(state);
            if (play == null)
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

            var compose = play.Compose;
            for (int i = 0; i < compose.Length; i++)
            {
                Actions.Enqueue(new SpiritAction(compose[i], Config.GetState(compose[i])));
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

                Wave.Start(Direction, Config.GetState(state == StateNames.LightWaveBoxing
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

            var combo = Config.GetCombo($"{StateName}_{state}");
            if (combo == null)
            {
                return true;
            }

            bool started = Frames.Combo.Start(combo.Background, combo.FrameCount,
                combo.RepeatPattern, combo.AfterFrame);

            Status.SetAttackType(combo.AttackType);
            Status.SetAttackPower(combo.AttackPower);

            if (started)
            {
                Attack.StickStart(combo.ComboAttack, combo.EffectPosition, combo.Sounds);

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
                Actions.Enqueue(new SpiritAction(null, Config.GetState(defaultState)));
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
            var easing = definition.Easing;
            string background = definition.Background;
            int frameCount = definition.FrameCount;
            int position = definition.Position;

            ChangeBackground(background, frameCount, position);

            CurrentState = action.Name;

            // top 为 null 时按包围盒自动补齐，保证换图集后脚底仍然贴地
            float top = easing.AutoTop
                ? FloorTop - Top + (FloorHeight - Height) * GameConfig.Zoom
                : easing.Top;

            Frames.Start(background, frameCount, (int)easing.Step, definition.RepeatPattern, position, Direction);

            float horizontal = (CurrentState == StateNames.Forward ? _forwardSpeed : easing.Dx) * Direction;
            Motion.Start(horizontal, top, easing.Step * GameConfig.Fps * frameCount, easing.Ease);

            HorizontalMoveSign = horizontal == 0f ? 0f : Mathf.Sign(horizontal);
            VerticalMoveSign = top == 0f ? 0f : Mathf.Sign(top);

            int attackType = definition.AttackType;
            Status.SetAttackType(attackType);
            Status.SetAttackPower(definition.AttackPower);

            // 只有近身攻击才有跟随判定体，波动拳等状态只有 attack_type 没有近身配置
            if (attackType == MeleeAttackType && definition.Attack == AttackConfigKind.Melee)
            {
                Attack.Start(definition.MeleeAttack, Direction, definition.EffectPosition, definition.Sounds);
            }

            string specialSound = definition.SpecialSound;
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

        /// <summary>
        /// 监听对手的位移结束事件（顶边推挤时反推对手）。
        /// 必须在双方都 <see cref="Initialize"/> 之后调用：对手的 <see cref="Mover"/> 那时才创建出来。
        /// </summary>
        public void BindEnemyMotion() => Enemy.Motion.Events.AddListener(GameEvents.FrameDone, _onEnemyFrameDone);

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
            bool pushingBorder = Border == Side.Left && HorizontalMoveSign == -1f
                                 || Border == Side.Right && HorizontalMoveSign == 1f;

            _isPushingEnemy = false;

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
            _isPushingEnemy = true;
        }

        /// <summary>对手位移结束后按本帧滚动量反推；没有在推挤时什么都不做。</summary>
        private void OnEnemyFrameDone()
        {
            if (!_isPushingEnemy)
            {
                return;
            }

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
            if (Status.IsCrouch() && Config.GetState(StateNames.CrouchPrefix + state) != null)
            {
                Play(StateNames.CrouchPrefix + state);
                return;
            }

            if (!Status.IsJump())
            {
                var near = Config.GetState(StateNames.NearPrefix + state);
                if (near != null && Status.Distance <= near.Near)
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

        /// <summary>推挤时动态改写 forward 的横向位移量（只改运行时副本，不写回配置资产）。</summary>
        private void SetForwardEasing(float value) => _forwardSpeed = value;

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
