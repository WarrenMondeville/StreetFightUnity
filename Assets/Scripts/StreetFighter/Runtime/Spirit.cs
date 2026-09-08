using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 main.js 的 Block + Spirit：角色总控。
    /// 组合了帧动画、位移插值、动作队列、锁、输入、碰撞、近战判定、波动拳、状态机与舞台推挤。
    /// </summary>
    public class Spirit : IBody, IAniOwner
    {
        /// <summary>动作队列元素：原版是 states 的浅拷贝 + currState 标记。</summary>
        public class Act
        {
            public string Name;
            public JVal Def;
        }

        public string Key;
        public JVal States;
        public JVal RawStates;

        public string StateName;
        public string CurrState;

        public float Left { get; set; }
        public float Top { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float FTop { get; set; }
        public float FLeft;
        public float FHeight;
        public int Position;
        public int Direction = 1;
        public string Border;
        public float Speed;

        public Spirit Enemy;
        public BloodBar BloodBar;

        public Ani Ani;
        public SpiritFrames Frames;
        public Q<Act> Queue;
        public Lock Lock;
        public KeyInput Keys;
        public Collider Col;
        public Melee Attack;
        public Wave WaveBoxing;
        public Status St;
        public Stage Stage;
        public Ai Ai;

        public readonly float[] AniDirection = new float[2];

        // 阴影（每帧刷新，交给 View 绘制）
        public float ShadowLeft;
        public float ShadowWidth;

        private readonly GameClock _clock;
        private SpiritView _view;

        public string DefaultState => States.Get("default").S;

        public Spirit(GameClock clock, string key, JVal spiritCfg)
        {
            _clock = clock;
            Key = key;
            RawStates = spiritCfg;
            States = spiritCfg.Get("states");
        }

        public object Master => null;

        public void SetEnemy(Spirit enemy) => Enemy = enemy;

        public void AttachView(SpiritView view) => _view = view;

        public void Init(float fLeft, float fTop, int direction)
        {
            FLeft = fLeft;
            FTop = fTop;
            var def = States.Get(DefaultState);
            FHeight = Art.Size(Cfg.Bg(def)).y;
            Direction = direction;

            Frames = new SpiritFrames(_clock);
            Ani = new Ani(_clock, this);
            Queue = new Q<Act>();
            Lock = new Lock();
            Keys = new KeyInput(_clock, this, RawStates.Get("keyMap"));
            Col = new Collider(this);
            Attack = new Melee(_clock, this);
            WaveBoxing = new Wave(_clock, this);
            St = new Status(this, _clock);
            Stage = new Stage(this, _clock);

            BindEvents();

            ChangeBg(Cfg.Bg(def), Cfg.FramesNum(def), 0);
            Play(DefaultState);
            Ani.Moveto(fLeft, fTop);
        }

        // ---------------- 状态调度 ----------------

        public void Play(string state, bool force = false)
        {
            if (Cfg.Play(state).IsNull) return;

            if (St.IsCrouch() && (state == "wait" || state == "force_wait"))
            {
                Play("stand_up", true);
                return;
            }

            if (JumpCombo(state) && state != "force_wait") return;
            if (Defense(state)) return;

            int lockLevel = Cfg.PlayLock(state);
            int oldLevel = Lock.Level;

            if (!force)
            {
                if (state == StateName || (Lock.Locked && oldLevel >= lockLevel)) return;
            }

            Lock.Set(lockLevel);
            Queue.Clean();

            var compose = Cfg.Play(state).Get("compose");
            for (int i = 0; i < compose.Count; i++)
            {
                string c = compose.Get(i).S;
                Queue.Add(new Act { Name = c, Def = States.Get(c) });
            }

            StateName = state;

            if (state == "light_wave_boxing" || state == "heavy_wave_boxing")
            {
                if (WaveBoxing.Firing)
                {
                    Queue.Clean();
                    Lock.Set(0);
                    Play(state == "light_wave_boxing" ? "light_boxing" : "heavy_boxing");
                    return;
                }
                WaveBoxing.Start(Direction, States.Get(state == "light_wave_boxing" ? "light_wave" : "heavy_wave"));
            }

            FireFrames();
            Ani.Event.Fire("playStart");
        }

        private bool JumpCombo(string state)
        {
            if (StateName == null || !St.IsJump()) return false;

            if (state == "jump_fall_down" || state == "jump_dead" || state == "after_dead2"
                || state == "heavy_attacked_fall_down" || state == "crouch_kick_attacked_fall")
            {
                Frames.Combo.Stop();
                return false;
            }

            var table = States.Get("combo");
            var combo = table.IsNull ? JVal.NIL : table.Get(StateName + "_" + state);
            if (combo.IsNull) return true;

            bool flag = Frames.Combo.Start(Cfg.Bg(combo), Cfg.FramesNum(combo), Cfg.Repeat(combo), (int)combo.Get("afterFrame").F);

            St.SetAttackType(Cfg.AttackType(combo));

            var ac = combo.Get("attack_config");
            var num = new float[ac.Count];
            var str = new string[ac.Count];
            for (int i = 0; i < ac.Count; i++)
            {
                num[i] = ac.Get(i).F;
                str[i] = ac.Get(i).S;
            }

            St.SetAttackPower(Cfg.NumArray(combo, "attack_power"));

            if (flag)
            {
                Attack.StickStart(num, str, Cfg.NumArray(combo, "effect_position"), Cfg.StrArray(combo, "sound"));
                Frames.Combo.Done(() =>
                {
                    St.SetAttackType(0);
                    Attack.Stop();
                });
            }

            return true;
        }

        private bool Defense(string state)
        {
            if (state == "back")
            {
                if (Enemy.WaveBoxing.Firing || (Enemy.St.AttackType == "attack" && St.DistanceType != "furthest"))
                {
                    if (St.AttackType == "wait")
                    {
                        Play("stand_up_defense");
                        St.SetAttackType(1);
                    }
                    return true;
                }
            }
            else if (state == "stand_crouch_defense")
            {
                if (!Enemy.WaveBoxing.Firing && (Enemy.St.AttackType != "attack" || St.DistanceType == "furthest"))
                {
                    Play("crouch");
                    St.SetAttackType(0);
                    return true;
                }
            }
            return false;
        }

        public void FireFrames()
        {
            var act = Queue.Dequeue();

            if (act == null)
            {
                int oldDir = Direction;
                int newDir = Left > Enemy.Left ? -1 : 1;
                if (newDir != oldDir) Left += (Border != null ? 18f : 105f) * newDir;

                Direction = Left > Enemy.Left ? -1 : 1;
                Ani.Mirror(Direction);
                Keys.Mirror(Direction);

                if (St.IsCrouch())
                {
                    Play("stand_up", true);
                    return;
                }

                string defName = DefaultState;
                Queue.Add(new Act { Name = null, Def = States.Get(defName) });
                Lock.Set(0);
                StateName = defName;
                Frames.Combo.Stop();
                if (Attack.AnimateType == "stick") Attack.Stop();

                FireFrames();
                return;
            }

            var d = act.Def;
            string bg = Cfg.Bg(d);
            int framesNum = Cfg.FramesNum(d);
            int position = (int)d.Get("position").F;

            ChangeBg(bg, framesNum, position);

            float? topVal = null;
            if (Cfg.EaseTopIsNull(d)) topVal = FTop - Top + (FHeight - Height) * Cfg.Zoom;
            float top = topVal ?? Cfg.EaseNum(d, 1);

            CurrState = act.Name;

            Frames.Start(bg, framesNum, (int)Cfg.EaseNum(d, 2), Cfg.Repeat(d), position, Direction);
            Ani.Start(Cfg.EaseNum(d, 0) * Direction, top, Cfg.EaseNum(d, 2) * Cfg.Fps * framesNum, Cfg.EaseFn(d));

            float l = Cfg.EaseNum(d, 0) * Direction;
            AniDirection[0] = l == 0 ? 0 : Mathf.Sign(l);
            AniDirection[1] = top == 0 ? 0 : Mathf.Sign(top);

            int attackType = Cfg.AttackType(d);
            St.SetAttackType(attackType);
            St.SetAttackPower(Cfg.NumArray(d, "attack_power"));

            // 原版是 attack_config && this.attack.start(...)：部分动作（如波动拳）只有 attack_type 没有判定配置
            var ac = d.Get("attack_config");
            if (attackType == 2 && !ac.IsNull)
            {
                var easing = new float[7];
                var es = new string[7];
                for (int i = 0; i < 7; i++)
                {
                    easing[i] = ac.Get(i + 2).F;
                    es[i] = ac.Get(i + 2).S;
                }
                easing[0] = ac.Get(2).F * Direction;
                Attack.Start(ac.Get(0).F, ac.Get(1).F, easing, es,
                    Cfg.NumArray(d, "effect_position"), Cfg.StrArray(d, "sound"));
            }

            string special = d.Get("specialSound").S;
            if (!string.IsNullOrEmpty(special)) Attack.Audio.Play(special);

            if (CurrState != "wait") Enemy.Ani.Event.Fire("stopPush");

            Ani.Event.Fire("framesStart");
        }

        public void ChangeBg(string bg, int framesNum, int position)
        {
            float oWidth = Width;
            Width = Art.Size(bg).x / framesNum;
            float dW = (Width - oWidth) * Cfg.Zoom;

            if (Direction == -1) Left -= dW;
            if (CurrState == "somesault_up" && Border == "right") Left -= 60f;

            float oHeight = Height;
            Height = Art.Size(bg).y;
            float dH = (Height - oHeight) * Cfg.Zoom;
            Top -= dH;

            Position = position;
        }

        public float CrossBorder(float left)
        {
            float maxX = Cfg.MapWidth;
            float right = StateName == "jump_back" ? 130f : 85f;
            if (left < 15f)
            {
                Border = "left";
                return 15f;
            }
            if (left + Width > maxX - right)
            {
                Border = "right";
                return maxX - Width - right;
            }
            Border = null;
            return left;
        }

        // ---------------- 事件 ----------------

        private void BindEvents()
        {
            Frames.Event.Listen("framesDone", () =>
            {
                if (CurrState == "crouch_kick_attacked_fall" || CurrState == "after_heavy_attacked_fall_down" || CurrState == "dead")
                    Attack.Audio.Play("sound/fall.mp3");

                if (CurrState == "somesault_up") St.SetInvincible(100);

                FireFrames();
            });

            Frames.Event.Listen("framesStart", () =>
            {
                Ani.Unlock();
                if (StateName == "wait") Ani.Correct();
            });

            Frames.Event.Listen("frameStart", () =>
            {
                if (StateName == "wait" || StateName == "crouch")
                    Direction = Left > Enemy.Left ? -1 : 1;

                Col.Check();
                Ani.Move();

                bool jump = St.IsJump();
                ShadowLeft = jump && Direction == -1 ? Left + (Width - 62f) * Cfg.Zoom : Left;
                ShadowWidth = jump ? 62f : Width;

                St.CheckEnemyDistance(Enemy);
            });

            Ani.Event.Listen("stopPush", () => SetForwardEasing(150f));

            Ani.Event.Listen("frameStart", () =>
            {
                Enemy.Ani.Event.RemoveListen("frameDone");

                if ((Border == "left" && AniDirection[0] == -1f) || (Border == "right" && AniDirection[0] == 1f))
                {
                    Enemy.Stage.Begin();

                    if (St.DistanceType == "furthest") return;

                    Stage.Scroll(Border);

                    Enemy.Ani.Event.Listen("frameDone", () =>
                    {
                        if (Enemy.AniDirection[0] == 0 && Enemy.AniDirection[1] == 0)
                            Stage.PushEnemy();
                        else if (Stage.IsScrolling())
                            Enemy.Ani.StagePush(Stage.ScrollValue());
                        else
                            Enemy.Ani.StopStagePush();
                    }, 1);
                }
                else
                {
                    Stage.End();
                }
            });

            Col.Event.Listen("affirm", () =>
            {
                var obj = Col.Other;
                string dir = Col.Dir;
                if (!ReferenceEquals(obj, Enemy)) return;

                if (StateName == "forward" && (Enemy.StateName == "wait" || Enemy.StateName == "crouch"))
                {
                    Enemy.Ani.Push(3f);
                    SetForwardEasing(90f);
                    if (Enemy.Border != null) Ani.Lock(Enemy.Border);
                }

                if (CurrState == "jump_forward_down" || CurrState == "jumpDown")
                    Enemy.Ani.Push((234f - St.Distance) / 40f);

                if (StateName == "forward" && Enemy.StateName == "forward")
                    Ani.Lock(dir);
            });

            Keys.Match(state =>
            {
                if (St.IsCrouch() && !States.Get("crouch_" + state).IsNull)
                {
                    Play("crouch_" + state);
                    return;
                }

                if (!St.IsJump())
                {
                    var near = States.Get("near_" + state);
                    if (!near.IsNull && St.Distance <= near.Get("near").F)
                    {
                        Play("near_" + state);
                        return;
                    }
                }

                Play(state);
            });

            Keys.Unmatch(state =>
            {
                if (StateName == "stand_up" && state == "wait") return;

                if (StateName == "back" || StateName == "forward" || StateName == "stand_up_defense"
                    || StateName == "stand_crouch_defense" || StateName == "crouch")
                    Play(DefaultState);
            });
        }

        private void SetForwardEasing(float v)
        {
            var f = States.Get("forward");
            if (f.IsNull) return;
            var e = f.Get("easing").Get(0);
            e.Type = JVal.Kind.Number;
            e.Number = v;
        }

        // ---------------- 渲染同步 ----------------

        public void Render(float zoom, int order)
        {
            if (_view == null) return;
            _view.Render(this, zoom, order);
            Attack.Render(zoom);
            WaveBoxing.Render(zoom, order + 5);
        }
    }
}
