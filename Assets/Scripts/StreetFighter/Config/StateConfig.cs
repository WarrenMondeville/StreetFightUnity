using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 一个角色状态的完整定义：图集、帧数、重复帧、位移、攻防类型与判定配置。
    /// states 表与空中组合技表共用这一个类型（组合技只用到其中的序列帧与 <c>afterFrame</c>）。
    /// </summary>
    [System.Serializable]
    public sealed class StateConfig
    {
        [SerializeField] private string _name;
        [SerializeField] private string _background;
        [SerializeField] private int _frameCount = 1;
        [SerializeField] private int[] _repeat;
        [SerializeField] private EasingConfig _easing = new EasingConfig();
        [SerializeField] private int _position;
        [SerializeField] private int _attackType;

        /// <summary>距离阈值：与对手距离小于等于它时改用近身变体，0 表示不启用。</summary>
        [SerializeField] private float _near;

        [SerializeField] private AttackConfigKind _attack;
        [SerializeField] private MeleeAttackConfig _meleeAttack = new MeleeAttackConfig();
        [SerializeField] private WaveAttackConfig _waveAttack = new WaveAttackConfig();
        [SerializeField] private ComboAttackConfig _comboAttack = new ComboAttackConfig();

        /// <summary>[攻击等级, 无敌标记]，缺失时为 null。</summary>
        [SerializeField] private float[] _attackPower;

        [SerializeField] private float[] _effectPosition;
        [SerializeField] private string[] _sounds;
        [SerializeField] private string _specialSound;
        [SerializeField] private float _defenseBlood;

        /// <summary>组合技结束后回退到的基础帧，仅组合技使用。</summary>
        [SerializeField] private int _afterFrame;

        /// <summary>招式说明，只给编辑器里的人看，不参与任何逻辑。</summary>
        [TextArea(2, 6)]
        [SerializeField] private string _description;

        /// <summary>状态名。</summary>
        public string Name => _name;

        /// <summary>图集名。</summary>
        public string Background => _background;

        /// <summary>图集横向切片帧数，至少为 1。</summary>
        public int FrameCount => _frameCount <= 0 ? 1 : _frameCount;

        /// <summary>重复帧模式，没有则为 null（每一项是该源帧的显示权重）。</summary>
        public int[] RepeatPattern => _repeat == null || _repeat.Length == 0 ? null : _repeat;

        /// <summary>位移与推进参数。</summary>
        public EasingConfig Easing => _easing;

        /// <summary>状态标记，仅透传给表现层。</summary>
        public int Position => _position;

        /// <summary>攻防类型，对应 <c>AttackState</c>。</summary>
        public int AttackType => _attackType;

        /// <summary>近身触发距离，0 表示不启用。</summary>
        public float Near => _near;

        /// <summary>判定配置种类，决定读下面哪一份。</summary>
        public AttackConfigKind Attack => _attack;

        /// <summary>近身判定配置。</summary>
        public MeleeAttackConfig MeleeAttack => _meleeAttack;

        /// <summary>飞行道具判定配置。</summary>
        public WaveAttackConfig WaveAttack => _waveAttack;

        /// <summary>空中组合技判定配置。</summary>
        public ComboAttackConfig ComboAttack => _comboAttack;

        /// <summary>攻击等级与无敌标记，缺失时为 null。</summary>
        public float[] AttackPower => Empty(_attackPower) ? null : _attackPower;

        /// <summary>命中特效相对对方的偏移，缺失时为 null。</summary>
        public float[] EffectPosition => Empty(_effectPosition) ? null : _effectPosition;

        /// <summary>[出招音, 受击音]，缺失时为 null。</summary>
        public string[] Sounds => Empty(_sounds) ? null : _sounds;

        /// <summary>独立于出招音之外的招式音，为空表示没有。</summary>
        public string SpecialSound => _specialSound;

        /// <summary>对方防御成功时削减的血量。</summary>
        public float DefenseBlood => _defenseBlood;

        /// <summary>组合技结束后回退到的基础帧。</summary>
        public int AfterFrame => _afterFrame;

        /// <summary>招式说明，只给编辑器里的人看。</summary>
        public string Description => _description;

        // Unity 会把缺失的数组反序列化成长度 0 的数组而不是 null，这里统一归一成 null
        private static bool Empty<T>(T[] array) => array == null || array.Length == 0;
    }
}
