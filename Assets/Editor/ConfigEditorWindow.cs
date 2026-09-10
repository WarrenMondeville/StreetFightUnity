using System;
using System.Collections.Generic;
using StreetFighter.Config;
using StreetFighter.Core;
using StreetFighter.Gameplay;
using UnityEditor;
using UnityEngine;

namespace StreetFighter.Editor
{
    /// <summary>
    /// 配置编辑器：直接编辑 <c>Resources/Config</c> 下的 ScriptableObject 资产。
    /// 角色页可以逐个改招式（图集、帧数、重复帧、位移、判定、音效），并增删状态与空中组合技。
    /// </summary>
    public sealed class ConfigEditorWindow : EditorWindow
    {
        private const string ConfigRoot = "Assets/Resources/Config";
        private const string FightersFolder = ConfigRoot + "/Fighters";

        private const string StatesPath = "_states";
        private const string CombosPath = "_combos";
        private const string ArtFolder = "Assets/Resources/Art";
        private const float ListWidth = 220f;
        private const float PreviewWidth = 340f;

        private static readonly Color BodyColor = new Color(0.25f, 1f, 0.35f);
        private static readonly Color AttackColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color EffectColor = new Color(1f, 0.9f, 0.2f);
        private static readonly Color WaveColor = new Color(0.4f, 0.7f, 1f);

        private enum Page
        {
            Fighter = 0,
            Play = 1,
            Global = 2,
        }

        private enum FighterTab
        {
            States = 0,
            Combos = 1,
            KeyMap = 2,
        }

        [SerializeField] private Page _page;
        [SerializeField] private FighterTab _fighterTab;
        [SerializeField] private FighterAsset _fighter;
        [SerializeField] private PlayAsset _play;
        [SerializeField] private GameSettingsAsset _settings;

        [SerializeField] private int _selectedIndex;
        [SerializeField] private string _search = string.Empty;
        [SerializeField] private Vector2 _listScroll;
        [SerializeField] private Vector2 _detailScroll;
        [SerializeField] private Vector2 _pageScroll;

        [SerializeField] private bool _playing = true;
        [SerializeField] private bool _showBody = true;
        [SerializeField] private bool _showAttack = true;
        [SerializeField] private bool _showEffect = true;
        [SerializeField] private int _direction = 1;
        [SerializeField] private float _previewScale = 1f;
        [SerializeField] private float _previewFrame;

        private SerializedObject _serialized;
        private SerializedProperty _list;
        private readonly List<int> _visible = new List<int>();
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private GUIStyle _selectedStyle;
        private GUIStyle _centeredStyle;
        private double _lastTime;
        private string _lastSelection;

        /// <summary>打开配置编辑器。</summary>
        [MenuItem("StreetFighter/Config Editor")]
        public static void Open()
        {
            var window = GetWindow<ConfigEditorWindow>("招式配置");
            window.minSize = new Vector2(1080f, 620f);
        }

        /// <summary>选中某份角色资产时打开编辑器并定位到它。</summary>
        public static void Open(FighterAsset fighter)
        {
            var window = GetWindow<ConfigEditorWindow>("招式配置");
            window.minSize = new Vector2(1080f, 620f);
            window._page = Page.Fighter;
            window._fighter = fighter;
            window.Rebuild();
        }

        #region 生命周期

        private void OnEnable()
        {
            _lastTime = EditorApplication.timeSinceStartup;

            if (_fighter == null)
            {
                _fighter = FindFirst<FighterAsset>(FightersFolder);
            }

            if (_play == null)
            {
                _play = AssetDatabase.LoadAssetAtPath<PlayAsset>(ConfigRoot + "/Play.asset");
            }

            if (_settings == null)
            {
                _settings = AssetDatabase.LoadAssetAtPath<GameSettingsAsset>(ConfigRoot + "/Global.asset");
            }

            Rebuild();
        }

        private void Update()
        {
            if (!_playing)
            {
                _lastTime = EditorApplication.timeSinceStartup;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - _lastTime) * 1000f;
            _lastTime = now;

            // 切回窗口时会有一个很大的时间差，直接丢弃，避免跳帧
            if (delta <= 0f || delta > 250f)
            {
                return;
            }

            var state = SelectedState;
            if (state == null)
            {
                return;
            }

            int total = ExpandedFrameCount(state);
            if (total <= 0)
            {
                return;
            }

            _previewFrame += delta / FrameDurationMs(state);
            if (_previewFrame >= total)
            {
                _previewFrame %= total;
            }

            Repaint();
        }

        private void OnGUI()
        {
            if (_serialized != null)
            {
                _serialized.Update();
            }

            DrawPageBar();

            switch (_page)
            {
                case Page.Play:
                    DrawSimplePage("动作表", ref _play, "Play.asset", "_actions", "动作");
                    break;
                case Page.Global:
                    DrawGlobalPage();
                    break;
                default:
                    DrawFighterPage();
                    break;
            }

            if (_serialized != null)
            {
                _serialized.ApplyModifiedProperties();
            }
        }

        #endregion

        #region 页面

        private void DrawPageBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (ToolbarButton("角色", _page == Page.Fighter))
            {
                _page = Page.Fighter;
                Rebuild();
            }

            if (ToolbarButton("动作表", _page == Page.Play))
            {
                _page = Page.Play;
                Rebuild();
            }

            if (ToolbarButton("全局设置", _page == Page.Global))
            {
                _page = Page.Global;
                Rebuild();
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFighterPage()
        {
            DrawAssetPicker(ref _fighter, "角色资产", FightersFolder);
            if (_fighter == null || _serialized == null)
            {
                EditorGUILayout.HelpBox("没有找到角色资产，请在 Resources/Config/Fighters 下创建。", MessageType.Info);
                return;
            }

            Field(_serialized.FindProperty("_fighterName"));
            Field(_serialized.FindProperty("_defaultState"));

            EditorGUILayout.BeginHorizontal();
            if (TabButton("状态", _fighterTab == FighterTab.States))
            {
                _fighterTab = FighterTab.States;
                Rebuild();
            }

            if (TabButton("空中组合技", _fighterTab == FighterTab.Combos))
            {
                _fighterTab = FighterTab.Combos;
                Rebuild();
            }

            if (TabButton("按键表", _fighterTab == FighterTab.KeyMap))
            {
                _fighterTab = FighterTab.KeyMap;
                Rebuild();
            }

            EditorGUILayout.EndHorizontal();

            if (_fighterTab == FighterTab.KeyMap)
            {
                DrawKeyMap();
                return;
            }

            EditorGUILayout.HelpBox(_fighterTab == FighterTab.Combos
                ? "空中组合技：键名形如「跳状态_攻击」（如 jump_light_boxing），在原动作上叠加一段序列帧，判定体每帧贴在角色身上。"
                : "状态：play 表 compose 引用的最小单位。改完用右侧预览确认帧序、判定框落点与特效点。",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            DrawStateList();
            DrawStateDetail();
            DrawPreview();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGlobalPage()
        {
            DrawAssetPicker(ref _settings, "全局设置", ConfigRoot);
            if (_settings == null || _serialized == null)
            {
                EditorGUILayout.HelpBox("没有找到 Global.asset。", MessageType.Info);
                return;
            }

            _pageScroll = EditorGUILayout.BeginScrollView(_pageScroll);
            Field(_serialized.FindProperty("_fps"));
            Field(_serialized.FindProperty("_keyFps"));
            Field(_serialized.FindProperty("_spiritShadow"));

            BeginGroup("舞台");
            Field(_serialized.FindProperty("_map"), true);
            EndGroup();

            BeginGroup("命中特效");
            Field(_serialized.FindProperty("_hitEffects"), true);
            EndGroup();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSimplePage<T>(string title, ref T asset, string fileName, string listPath, string label)
            where T : ScriptableObject
        {
            DrawAssetPicker(ref asset, title, ConfigRoot);
            if (asset == null || _serialized == null)
            {
                EditorGUILayout.HelpBox($"没有找到 {fileName}。", MessageType.Info);
                return;
            }

            _pageScroll = EditorGUILayout.BeginScrollView(_pageScroll);
            var list = _serialized.FindProperty(listPath);
            if (list != null)
            {
                Field(list, true);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawKeyMap()
        {
            var keyMap = _serialized.FindProperty("_keyMap");
            if (keyMap == null)
            {
                return;
            }

            _pageScroll = EditorGUILayout.BeginScrollView(_pageScroll);
            BeginGroup("按键映射（keyCode → 令牌）");
            Field(keyMap.FindPropertyRelative("_mappings"), true);
            EndGroup();

            BeginGroup("移动表");
            Field(keyMap.FindPropertyRelative("_moves"), true);
            Field(keyMap.FindPropertyRelative("_movesMirrored"), true);
            EndGroup();

            BeginGroup("出招表");
            Field(keyMap.FindPropertyRelative("_normalAttacks"), true);
            Field(keyMap.FindPropertyRelative("_specialAttacks"), true);
            EndGroup();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region 状态列表

        private void DrawStateList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();

            RefreshVisible();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUI.skin.box);
            for (int i = 0; i < _visible.Count; i++)
            {
                int index = _visible[i];
                var element = _list.GetArrayElementAtIndex(index);
                string name = element.FindPropertyRelative("_name").stringValue;
                bool selected = index == _selectedIndex;

                if (GUILayout.Button(name, SelectedStyle(selected)) && !selected)
                {
                    _selectedIndex = index;
                    Repaint();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("新建"))
            {
                AppendNew();
            }

            if (GUILayout.Button("复制"))
            {
                DuplicateSelected();
            }

            if (GUILayout.Button("删除"))
            {
                DeleteSelected();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStateDetail()
        {
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            EditorGUILayout.BeginVertical();

            if (_list == null || _list.arraySize == 0)
            {
                EditorGUILayout.HelpBox("这个角色还没有状态。", MessageType.Info);
            }
            else if (_selectedIndex < 0 || _selectedIndex >= _list.arraySize)
            {
                EditorGUILayout.HelpBox("请在左侧选择一个状态。", MessageType.Info);
            }
            else
            {
                DrawState(_list.GetArrayElementAtIndex(_selectedIndex), _fighterTab == FighterTab.Combos);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawState(SerializedProperty state, bool isCombo)
        {
            BeginGroup("说明");
            Field(state.FindPropertyRelative("_description"));
            EditorGUILayout.Space(2f);
            EditorGUILayout.HelpBox(BuildSummary(state, isCombo), MessageType.None);
            EndGroup();

            BeginGroup("序列帧");
            Field(state.FindPropertyRelative("_name"));
            Field(state.FindPropertyRelative("_background"));
            Field(state.FindPropertyRelative("_frameCount"));
            Field(state.FindPropertyRelative("_repeat"), true);
            EndGroup();

            if (!isCombo)
            {
                var easing = state.FindPropertyRelative("_easing");
                BeginGroup("位移");
                Field(easing.FindPropertyRelative("_dx"));
                Field(easing.FindPropertyRelative("_autoTop"));
                if (!easing.FindPropertyRelative("_autoTop").boolValue)
                {
                    Field(easing.FindPropertyRelative("_top"));
                }

                Field(easing.FindPropertyRelative("_step"));
                Field(easing.FindPropertyRelative("_ease"));
                EndGroup();

                BeginGroup("其它");
                Field(state.FindPropertyRelative("_position"));
                EndGroup();
            }

            BeginGroup("攻防");
            Field(state.FindPropertyRelative("_attackType"));
            Field(state.FindPropertyRelative("_near"));
            Field(state.FindPropertyRelative("_attackPower"), true);
            Field(state.FindPropertyRelative("_defenseBlood"));
            EndGroup();

            BeginGroup("判定");
            var kind = state.FindPropertyRelative("_attack");
            Field(kind);

            switch ((AttackConfigKind)kind.intValue)
            {
                case AttackConfigKind.Melee:
                    Field(state.FindPropertyRelative("_meleeAttack"), true);
                    break;
                case AttackConfigKind.Wave:
                    Field(state.FindPropertyRelative("_waveAttack"), true);
                    break;
                case AttackConfigKind.Combo:
                    Field(state.FindPropertyRelative("_comboAttack"), true);
                    break;
            }

            EndGroup();

            BeginGroup("表现");
            Field(state.FindPropertyRelative("_effectPosition"), true);
            Field(state.FindPropertyRelative("_sounds"), true);
            Field(state.FindPropertyRelative("_specialSound"));
            EndGroup();

            if (isCombo)
            {
                BeginGroup("组合技");
                Field(state.FindPropertyRelative("_afterFrame"));
                EndGroup();
            }
        }

        /// <summary>把当前招式的参数翻译成一段人话，方便核对数值。</summary>
        private static string BuildSummary(SerializedProperty state, bool isCombo)
        {
            var lines = new List<string>();

            int frameCount = Mathf.Max(1, state.FindPropertyRelative("_frameCount").intValue);
            int total = ExpandedFrameCount(state);
            string frames = total == frameCount
                ? $"{frameCount} 帧"
                : $"{frameCount} 帧（重复帧展开 {total} 帧）";

            lines.Add($"序列帧：{state.FindPropertyRelative("_background").stringValue}，{frames}，"
                      + $"每帧约 {FrameDurationMs(state):0} 毫秒");

            if (!isCombo)
            {
                var easing = state.FindPropertyRelative("_easing");
                string vertical = easing.FindPropertyRelative("_autoTop").boolValue
                    ? "自动贴地"
                    : easing.FindPropertyRelative("_top").floatValue.ToString();

                lines.Add($"位移：横向 {easing.FindPropertyRelative("_dx").floatValue}（乘朝向），"
                          + $"纵向 {vertical}，缓动 {easing.FindPropertyRelative("_ease").stringValue}");
            }

            lines.Add($"姿态：{AttackTypeText(state.FindPropertyRelative("_attackType").intValue)}");

            var power = state.FindPropertyRelative("_attackPower");
            if (power.arraySize > 0)
            {
                lines.Add($"攻击等级：{(int)power.GetArrayElementAtIndex(0).floatValue}");
            }

            float near = state.FindPropertyRelative("_near").floatValue;
            if (near > 0f)
            {
                lines.Add($"近身触发：与对手距离 ≤ {near} 时改用 near_ 前缀的变体");
            }

            switch ((AttackConfigKind)state.FindPropertyRelative("_attack").intValue)
            {
                case AttackConfigKind.Melee:
                {
                    var config = state.FindPropertyRelative("_meleeAttack");
                    lines.Add($"判定：近身 {MeleeAttack.DefaultSize}×{MeleeAttack.DefaultSize}，"
                              + $"起点 ({config.FindPropertyRelative("_offsetX").floatValue}, "
                              + $"{config.FindPropertyRelative("_offsetY").floatValue})，"
                              + $"位移 ({config.FindPropertyRelative("_moveX").floatValue}, "
                              + $"{config.FindPropertyRelative("_moveY").floatValue}) / "
                              + $"{config.FindPropertyRelative("_duration").floatValue} 毫秒，"
                              + $"{config.FindPropertyRelative("_ease").stringValue}");
                    lines.Add($"命中：伤害 {config.FindPropertyRelative("_damage").floatValue}，"
                              + $"特效 {config.FindPropertyRelative("_effect").stringValue}，"
                              + $"对方进入「{config.FindPropertyRelative("_beatState").stringValue}」");
                    break;
                }

                case AttackConfigKind.Wave:
                {
                    var config = state.FindPropertyRelative("_waveAttack");
                    lines.Add($"判定：飞行道具 {WaveProjectile.ColliderWidth}×{WaveProjectile.ColliderHeight}"
                              + $"（绘制 {WaveProjectile.DefaultWidth}×{WaveProjectile.DefaultHeight}），"
                              + $"尺寸偏移 ({config.FindPropertyRelative("_sizeOffsetX").floatValue}, "
                              + $"{config.FindPropertyRelative("_sizeOffsetY").floatValue})");
                    lines.Add($"命中：伤害 {config.FindPropertyRelative("_damage").floatValue}，"
                              + $"防御削血 {config.FindPropertyRelative("_defenseDamage").floatValue}，"
                              + $"消失特效 {config.FindPropertyRelative("_disappearEffect").stringValue}，"
                              + $"对方进入「{config.FindPropertyRelative("_beatState").stringValue}」");
                    break;
                }

                case AttackConfigKind.Combo:
                {
                    var config = state.FindPropertyRelative("_comboAttack");
                    float size = config.FindPropertyRelative("_size").floatValue;
                    lines.Add($"判定：贴身 {size}×{size}，"
                              + $"偏移 ({config.FindPropertyRelative("_offsetX").floatValue}, "
                              + $"{config.FindPropertyRelative("_offsetY").floatValue})，每帧跟随角色");
                    lines.Add($"命中：伤害 {config.FindPropertyRelative("_damage").floatValue}，"
                              + $"特效 {config.FindPropertyRelative("_effect").stringValue}，"
                              + $"对方进入「{config.FindPropertyRelative("_beatState").stringValue}」");
                    break;
                }

                default:
                    lines.Add("判定：无（这个状态不带攻击判定体）");
                    break;
            }

            float defenseBlood = state.FindPropertyRelative("_defenseBlood").floatValue;
            if (defenseBlood > 0f)
            {
                lines.Add($"防御削血：{defenseBlood}");
            }

            var position = state.FindPropertyRelative("_effectPosition");
            if (position.arraySize >= 2)
            {
                lines.Add($"特效偏移：({position.GetArrayElementAtIndex(0).floatValue}, "
                          + $"{position.GetArrayElementAtIndex(1).floatValue})，相对对方左上角");
            }

            var sounds = state.FindPropertyRelative("_sounds");
            if (sounds.arraySize >= 2)
            {
                lines.Add($"音效：出招 {sounds.GetArrayElementAtIndex(0).stringValue}，"
                          + $"受击 {sounds.GetArrayElementAtIndex(1).stringValue}");
            }
            else if (sounds.arraySize == 1)
            {
                lines.Add($"音效：{sounds.GetArrayElementAtIndex(0).stringValue}");
            }

            string special = state.FindPropertyRelative("_specialSound").stringValue;
            if (!string.IsNullOrEmpty(special))
            {
                lines.Add($"招式音：{special}");
            }

            if (isCombo)
            {
                lines.Add($"组合技回退帧：{state.FindPropertyRelative("_afterFrame").intValue}");
            }

            return string.Join("\n", lines.ToArray());
        }

        private static string AttackTypeText(int value)
        {
            switch (value)
            {
                case 0: return "空闲（attack_type 0）";
                case 1: return "防御中（attack_type 1）";
                case 2: return "攻击判定中（attack_type 2）";
                case 3: return "被击硬直（attack_type 3）";
                case 4: return "倒地（attack_type 4）";
                default: return $"未知（attack_type {value}）";
            }
        }

        #endregion

        #region 预览

        /// <summary>当前选中的状态（列表里的元素）。</summary>
        private SerializedProperty SelectedState =>
            _list != null && _selectedIndex >= 0 && _selectedIndex < _list.arraySize
                ? _list.GetArrayElementAtIndex(_selectedIndex)
                : null;

        private void DrawPreview()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PreviewWidth));
            EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);

            var state = SelectedState;
            if (state == null)
            {
                EditorGUILayout.HelpBox("请在左侧选择一个状态。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            ResetFrameOnSelectionChange();

            int total = ExpandedFrameCount(state);
            int frameCount = Mathf.Max(1, state.FindPropertyRelative("_frameCount").intValue);

            EditorGUILayout.BeginHorizontal();
            _showBody = EditorGUILayout.ToggleLeft("受击框", _showBody, GUILayout.Width(62f));
            _showAttack = EditorGUILayout.ToggleLeft("攻击框", _showAttack, GUILayout.Width(62f));
            _showEffect = EditorGUILayout.ToggleLeft("特效点", _showEffect, GUILayout.Width(62f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _direction = EditorGUILayout.IntPopup(_direction, new[] { "朝右", "朝左" }, new[] { 1, -1 },
                GUILayout.Width(72f));
            _previewScale = EditorGUILayout.Slider(_previewScale, 0.25f, 3f);
            EditorGUILayout.EndHorizontal();

            var area = GUILayoutUtility.GetRect(PreviewWidth - 24f, 280f, GUILayout.ExpandHeight(true));
            DrawPreviewArea(area, state);

            int frame = Mathf.Clamp(Mathf.RoundToInt(_previewFrame), 0, Mathf.Max(0, total - 1));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_playing ? "暂停" : "播放", GUILayout.Width(48f)))
            {
                _playing = !_playing;
            }

            int next = EditorGUILayout.IntSlider(frame, 0, Mathf.Max(0, total - 1));
            if (next != frame)
            {
                _previewFrame = next;
                _playing = false;
            }

            EditorGUILayout.EndHorizontal();

            int source = FrameAnimator.ResolveFrame(frame, RepeatPattern(state.FindPropertyRelative("_repeat")));
            EditorGUILayout.LabelField($"展开帧 {frame + 1}/{total}　源帧 {source + 1}/{frameCount}",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(BoxSummary(state), EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewArea(Rect area, SerializedProperty state)
        {
            EditorGUI.DrawRect(area, new Color(0.11f, 0.11f, 0.13f));

            string background = state.FindPropertyRelative("_background").stringValue;
            var texture = GetTexture(background);
            if (texture == null)
            {
                EditorGUI.LabelField(area, $"找不到图集 {background}", CenteredStyle);
                return;
            }

            int frameCount = Mathf.Max(1, state.FindPropertyRelative("_frameCount").intValue);
            int total = ExpandedFrameCount(state);
            int frame = Mathf.Clamp(Mathf.RoundToInt(_previewFrame), 0, Mathf.Max(0, total - 1));
            int source = FrameAnimator.ResolveFrame(frame, RepeatPattern(state.FindPropertyRelative("_repeat")));

            float frameWidth = texture.width / (float)frameCount;
            float frameHeight = texture.height;
            float scale = Mathf.Max(0.1f,
                Mathf.Min((area.width - 24f) / frameWidth, (area.height - 34f) / frameHeight) * _previewScale);

            float width = frameWidth * scale;
            float height = frameHeight * scale;
            float groundY = area.yMax - 18f;
            float x = area.x + (area.width - width) / 2f;
            float y = groundY - height;

            EditorGUI.DrawRect(new Rect(area.x, groundY, area.width, 1f), new Color(0.45f, 0.45f, 0.45f));

            DrawSprite(new Rect(x, y, width, height), texture, source, frameCount);

            if (_showBody)
            {
                DrawBox(new Rect(x, y, width, height), BodyColor);
            }

            if (_showAttack)
            {
                DrawAttackBox(state, x, y, scale, frameWidth, frameHeight);
            }

            if (_showEffect)
            {
                DrawEffectMark(state, x, y, scale, frameWidth);
            }

            EditorGUI.LabelField(new Rect(area.x + 4f, area.y + 2f, area.width - 8f, 16f),
                $"{background}　{texture.width}×{texture.height} / {frameCount}", EditorStyles.miniLabel);
        }

        /// <summary>只画序列帧中的一帧：整条图集左移后用 group 裁掉多余部分；朝左时整体镜像。</summary>
        private void DrawSprite(Rect rect, Texture2D texture, int frame, int frameCount)
        {
            GUI.BeginGroup(rect);
            var matrix = GUI.matrix;
            if (_direction == -1)
            {
                GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), new Vector2(rect.width / 2f, rect.height / 2f));
            }

            float stripWidth = rect.width * frameCount;
            GUI.DrawTexture(new Rect(-frame * rect.width, 0f, stripWidth, rect.height), texture,
                ScaleMode.StretchToFill, true);

            GUI.matrix = matrix;
            GUI.EndGroup();
        }

        private void DrawAttackBox(SerializedProperty state, float x, float y, float scale, float frameWidth,
            float frameHeight)
        {
            float elapsed = _previewFrame * FrameDurationMs(state);

            switch ((AttackConfigKind)state.FindPropertyRelative("_attack").intValue)
            {
                case AttackConfigKind.Melee:
                {
                    var config = state.FindPropertyRelative("_meleeAttack");
                    float offsetX = config.FindPropertyRelative("_offsetX").floatValue;
                    float offsetY = config.FindPropertyRelative("_offsetY").floatValue;
                    float moveX = config.FindPropertyRelative("_moveX").floatValue;
                    float moveY = config.FindPropertyRelative("_moveY").floatValue;
                    float duration = config.FindPropertyRelative("_duration").floatValue;
                    string ease = config.FindPropertyRelative("_ease").stringValue;

                    // 与 Mover 一致：sinease 的横向位移仍然走线性
                    string horizontalEase = ease == EasingNames.SineaseIn || ease == EasingNames.SineaseOut
                        ? EasingNames.Linear
                        : ease;

                    float t = duration > 0f ? Mathf.Min(elapsed / duration, 1f) : 1f;
                    float dx = Easing.Evaluate(horizontalEase, t, 0f, moveX * _direction, 1f);
                    float dy = Easing.Evaluate(ease, t, 0f, moveY, 1f);

                    float size = MeleeAttack.DefaultSize;
                    float left = _direction == 1 ? offsetX : 2f * frameWidth - offsetX - 2f * size;
                    DrawBox(new Rect(x + (left + dx) * scale, y + (offsetY + dy) * scale,
                        size * scale, size * scale), AttackColor);
                    break;
                }

                case AttackConfigKind.Combo:
                {
                    var config = state.FindPropertyRelative("_comboAttack");
                    float offsetX = config.FindPropertyRelative("_offsetX").floatValue;
                    float offsetY = config.FindPropertyRelative("_offsetY").floatValue;
                    float size = config.FindPropertyRelative("_size").floatValue;
                    float left = _direction == 1 ? offsetX : frameWidth - offsetX + size;
                    DrawBox(new Rect(x + left * scale, y + offsetY * scale, size * scale, size * scale), AttackColor);
                    break;
                }

                case AttackConfigKind.Wave:
                {
                    float width = WaveProjectile.ColliderWidth * scale;
                    float height = WaveProjectile.ColliderHeight * scale;
                    DrawBox(new Rect(x + (frameWidth * scale - width) / 2f,
                        y + (frameHeight * scale - height) / 2f, width, height), WaveColor);
                    break;
                }
            }
        }

        /// <summary>特效生成点。配置里的 effect_position 是相对「对方左上角」的偏移，这里按同样规则画。</summary>
        private void DrawEffectMark(SerializedProperty state, float x, float y, float scale, float frameWidth)
        {
            var position = state.FindPropertyRelative("_effectPosition");
            if (position.arraySize < 2)
            {
                return;
            }

            float offsetX = position.GetArrayElementAtIndex(0).floatValue;
            float offsetY = position.GetArrayElementAtIndex(1).floatValue;
            float left = _direction == 1 ? offsetX : frameWidth + offsetX;
            DrawMark(new Rect(x + left * scale - 1f, y + offsetY * scale - 1f, 2f, 2f), EffectColor);
        }

        private static void DrawBox(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, new Color(color.r, color.g, color.b, 0.16f));
            DrawOutline(rect, color, 2f);
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawMark(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x - 6f, rect.center.y - 1f, 13f, 2f), color);
            EditorGUI.DrawRect(new Rect(rect.center.x - 1f, rect.y - 6f, 2f, 13f), color);
        }

        private Texture2D GetTexture(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            Texture2D cached;
            if (_textures.TryGetValue(name, out cached) && cached != null)
            {
                return cached;
            }

            var guids = AssetDatabase.FindAssets(name, new[] { ArtFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (texture != null && texture.name == name)
                {
                    _textures[name] = texture;
                    return texture;
                }
            }

            return null;
        }

        private void ResetFrameOnSelectionChange()
        {
            string key = (_fighter != null ? _fighter.name : "?") + "|" + _fighterTab + "|" + _selectedIndex;
            if (key != _lastSelection)
            {
                _lastSelection = key;
                _previewFrame = 0f;
            }
        }

        private static int ExpandedFrameCount(SerializedProperty state)
        {
            var repeat = state.FindPropertyRelative("_repeat");
            int total = 0;
            for (int i = 0; i < repeat.arraySize; i++)
            {
                total += repeat.GetArrayElementAtIndex(i).intValue;
            }

            return total > 0 ? total : Mathf.Max(1, state.FindPropertyRelative("_frameCount").intValue);
        }

        private static int[] RepeatPattern(SerializedProperty repeat)
        {
            if (repeat.arraySize == 0)
            {
                return null;
            }

            var pattern = new int[repeat.arraySize];
            for (int i = 0; i < pattern.Length; i++)
            {
                pattern[i] = repeat.GetArrayElementAtIndex(i).intValue;
            }

            return pattern;
        }

        /// <summary>一个展开帧持续多久（毫秒）。与 FrameAnimator 一致：每 step 个逻辑帧推进一帧。</summary>
        private static float FrameDurationMs(SerializedProperty state)
        {
            var easing = state.FindPropertyRelative("_easing");
            float step = easing != null ? easing.FindPropertyRelative("_step").floatValue : 3f;
            return Mathf.Max(1f, step) * GameClock.TickMilliseconds;
        }

        private static string BoxSummary(SerializedProperty state)
        {
            switch ((AttackConfigKind)state.FindPropertyRelative("_attack").intValue)
            {
                case AttackConfigKind.Melee:
                    return $"近身判定 {MeleeAttack.DefaultSize}×{MeleeAttack.DefaultSize}（跟随位移）";
                case AttackConfigKind.Wave:
                    return $"波动判定 {WaveProjectile.ColliderWidth}×{WaveProjectile.ColliderHeight}（居中）";
                case AttackConfigKind.Combo:
                    float size = state.FindPropertyRelative("_comboAttack").FindPropertyRelative("_size").floatValue;
                    return $"组合技判定 {size}×{size}（贴身）";
                default:
                    return "无判定框";
            }
        }

        #endregion

        #region 增删

        private void AppendNew()
        {
            if (_list == null)
            {
                return;
            }

            _list.arraySize++;
            var element = _list.GetArrayElementAtIndex(_list.arraySize - 1);
            ResetState(element, NewName("new_state"));
            _selectedIndex = _list.arraySize - 1;
        }

        private void DuplicateSelected()
        {
            if (_list == null || !HasSelection())
            {
                return;
            }

            _list.InsertArrayElementAtIndex(_selectedIndex);
            var copy = _list.GetArrayElementAtIndex(_selectedIndex + 1);
            copy.FindPropertyRelative("_name").stringValue =
                NewName(copy.FindPropertyRelative("_name").stringValue);
            _selectedIndex++;
        }

        private void DeleteSelected()
        {
            if (_list == null || !HasSelection())
            {
                return;
            }

            if (!UnityEditor.EditorUtility.DisplayDialog("删除状态",
                    $"确定删除「{_list.GetArrayElementAtIndex(_selectedIndex).FindPropertyRelative("_name").stringValue}」？",
                    "删除", "取消"))
            {
                return;
            }

            _list.DeleteArrayElementAtIndex(_selectedIndex);
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _list.arraySize - 1));
        }

        private static void ResetState(SerializedProperty state, string name)
        {
            state.FindPropertyRelative("_name").stringValue = name;
            state.FindPropertyRelative("_background").stringValue = string.Empty;
            state.FindPropertyRelative("_frameCount").intValue = 1;
            state.FindPropertyRelative("_position").intValue = 0;
            state.FindPropertyRelative("_attackType").intValue = 0;
            state.FindPropertyRelative("_near").floatValue = 0f;
            state.FindPropertyRelative("_attack").intValue = (int)AttackConfigKind.None;
            state.FindPropertyRelative("_specialSound").stringValue = string.Empty;
            state.FindPropertyRelative("_defenseBlood").floatValue = 0f;
            state.FindPropertyRelative("_afterFrame").intValue = 0;

            Clear(state.FindPropertyRelative("_repeat"));
            Clear(state.FindPropertyRelative("_attackPower"));
            Clear(state.FindPropertyRelative("_effectPosition"));
            Clear(state.FindPropertyRelative("_sounds"));

            var easing = state.FindPropertyRelative("_easing");
            easing.FindPropertyRelative("_dx").floatValue = 0f;
            easing.FindPropertyRelative("_autoTop").boolValue = false;
            easing.FindPropertyRelative("_top").floatValue = 0f;
            easing.FindPropertyRelative("_step").floatValue = 3f;
            easing.FindPropertyRelative("_ease").stringValue = EasingNames.Linear;
        }

        private static void Clear(SerializedProperty array)
        {
            if (array != null)
            {
                array.arraySize = 0;
            }
        }

        private string NewName(string basis)
        {
            var used = new HashSet<string>();
            for (int i = 0; i < _list.arraySize; i++)
            {
                used.Add(_list.GetArrayElementAtIndex(i).FindPropertyRelative("_name").stringValue);
            }

            if (!used.Contains(basis))
            {
                return basis;
            }

            for (int i = 2; ; i++)
            {
                string candidate = $"{basis}_{i}";
                if (!used.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        #endregion

        #region 工具

        private bool HasSelection() => _list != null && _selectedIndex >= 0 && _selectedIndex < _list.arraySize;

        private void RefreshVisible()
        {
            _visible.Clear();
            if (_list == null)
            {
                return;
            }

            string filter = _search == null ? string.Empty : _search.Trim();
            for (int i = 0; i < _list.arraySize; i++)
            {
                string name = _list.GetArrayElementAtIndex(i).FindPropertyRelative("_name").stringValue;
                if (filter.Length == 0 || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _visible.Add(i);
                }
            }
        }

        private void DrawAssetPicker<T>(ref T asset, string label, string folder) where T : UnityEngine.Object
        {
            EditorGUILayout.BeginHorizontal();
            var picked = (T)UnityEditor.EditorGUILayout.ObjectField(label, asset, typeof(T), false);
            if (picked != asset)
            {
                asset = picked;
                Rebuild();
            }

            var options = FindAll<T>(folder);
            if (options.Count > 0)
            {
                int current = options.IndexOf(asset);
                int next = UnityEditor.EditorGUILayout.Popup(current < 0 ? 0 : current,
                    Names(options).ToArray(), GUILayout.Width(140f));
                if (next != (current < 0 ? 0 : current))
                {
                    asset = options[next];
                    Rebuild();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void Rebuild()
        {
            _serialized = null;
            _list = null;

            UnityEngine.Object target;
            switch (_page)
            {
                case Page.Play:
                    target = _play;
                    break;
                case Page.Global:
                    target = _settings;
                    break;
                default:
                    target = _fighter;
                    break;
            }

            if (target == null)
            {
                return;
            }

            _serialized = new SerializedObject(target);

            if (_page == Page.Fighter && _fighterTab != FighterTab.KeyMap)
            {
                _list = _serialized.FindProperty(_fighterTab == FighterTab.Combos ? CombosPath : StatesPath);
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _list.arraySize - 1));
            }
        }

        private static List<T> FindAll<T>(string folder) where T : UnityEngine.Object
        {
            var result = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return result;
            }

            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset != null)
                {
                    result.Add(asset);
                }
            }

            return result;
        }

        private static T FindFirst<T>(string folder) where T : UnityEngine.Object
        {
            var all = FindAll<T>(folder);
            return all.Count == 0 ? null : all[0];
        }

        private static List<string> Names<T>(List<T> assets) where T : UnityEngine.Object
        {
            var names = new List<string>(assets.Count);
            for (int i = 0; i < assets.Count; i++)
            {
                names.Add(assets[i].name);
            }

            return names;
        }

        private static bool ToolbarButton(string text, bool active) =>
            GUILayout.Toggle(active, text, EditorStyles.toolbarButton) != active;

        private static bool TabButton(string text, bool active)
        {
            var style = new GUIStyle(EditorStyles.miniButtonMid);
            style.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            return GUILayout.Button(text, style, GUILayout.Height(22f)) && !active;
        }

        /// <summary>画一个配置字段，用中文标签并带上悬浮说明。</summary>
        private static void Field(SerializedProperty property) => Field(property, false);

        /// <summary>画一个配置字段，<c>includeChildren</c> 为真时连同子字段一起展开。</summary>
        private static void Field(SerializedProperty property, bool includeChildren)
        {
            if (property == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(property, Label(property.name), includeChildren);
        }

        private static GUIContent Label(string fieldName)
        {
            GUIContent content;
            if (Labels.TryGetValue(fieldName, out content))
            {
                return content;
            }

            content = new GUIContent(ObjectNames.NicifyVariableName(fieldName));
            Labels[fieldName] = content;
            return content;
        }

        private static GUIContent Tip(string label, string tooltip) => new GUIContent(label, tooltip);

        /// <summary>字段名 → 中文标签 + 悬浮说明。</summary>
        private static readonly Dictionary<string, GUIContent> Labels = new Dictionary<string, GUIContent>
        {
            // 资产顶层
            { "_fighterName", Tip("角色键", "如 RYU1；运行时用它在 Resources/Config/Fighters 下找这份资产") },
            { "_defaultState", Tip("默认状态", "一段动作播完后回到的状态，通常是 wait") },
            { "_fps", Tip("逻辑帧时长", "毫秒；GameClock 的步长，也是运动时长计算的基准") },
            { "_keyFps", Tip("输入采样周期", "毫秒；输入缓冲每隔这么久清一次") },
            { "_spiritShadow", Tip("脚下阴影图", "Resources/Art 下的图名") },
            { "_map", Tip("舞台", "远近两层背景图与角色缩放") },
            { "_hitEffects", Tip("命中特效表", "每类特效的帧数与绘制高度") },
            { "_actions", Tip("动作列表", "play 表：动作名 → 状态序列 + 优先级锁") },
            { "_keyMap", Tip("按键表", "令牌映射、移动表与出招表") },
            { "_mappings", Tip("按键映射", "键盘 keyCode → 令牌；顺序决定移动键的枚举顺序") },
            { "_moves", Tip("移动表（朝右）", "令牌组合 → 移动动作，如 wa → jump_back") },
            { "_movesMirrored", Tip("移动表（朝左）", "朝向翻转后前后互换的那一份") },
            { "_normalAttacks", Tip("普攻", "单个按键 → 招式名") },
            { "_specialAttacks", Tip("必杀", "方向序列 + 攻击键 → 招式名，如 crouch,forward,light_boxing") },

            // 状态
            { "_name", Tip("状态名", "play 的 compose 里引用的名字，一个角色内必须唯一") },
            { "_background", Tip("图集", "Resources/Art 下的横向条带图名，按帧数横向均分") },
            { "_frameCount", Tip("帧数", "图集横向切片帧数，至少 1") },
            { "_repeat", Tip("重复帧", "每一项是对应源帧显示多少个逻辑帧；为空则每源帧显示 1 次") },
            { "_easing", Tip("位移", "整段动作的位移与推进参数") },
            { "_dx", Tip("横向位移", "像素；实际位移 = dx × 朝向") },
            { "_autoTop", Tip("纵向自动", "勾选后忽略下方「纵向位移」，按包围盒自动补差使脚底贴地") },
            { "_top", Tip("纵向位移", "像素，正数向下；配置里为 null 时就是自动模式") },
            { "_step", Tip("推进间隔", "每几个逻辑帧推进一帧，同时作为运动时长系数（step × fps × 帧数）") },
            { "_ease", Tip("缓动", "Easing 里按名字实现的缓动函数") },
            { "_position", Tip("位置标记", "仅透传给表现层，不参与任何逻辑") },
            { "_attackType", Tip("攻防类型", "0 空闲 / 1 防御 / 2 攻击判定 / 3 被击硬直 / 4 倒地") },
            { "_near", Tip("近身距离", "与对手距离 ≤ 该值时改用 near_ 前缀的同名状态，0 表示不启用") },
            { "_attack", Tip("判定类型", "决定下面哪一份判定配置生效") },
            { "_attackPower", Tip("攻击等级", "[攻击等级, 无敌标记]；等级用于双方招式互拼") },
            { "_effectPosition", Tip("特效偏移", "[x, y]，命中特效相对对方左上角的偏移") },
            { "_sounds", Tip("音效", "[出招音, 受击音]") },
            { "_specialSound", Tip("招式音", "独立于出招音之外的招式音，如波动拳的发声") },
            { "_defenseBlood", Tip("防御削血", "对方防御成功时仍然削减的血量") },
            { "_afterFrame", Tip("回退帧", "组合技播完后回退到基础动作的第几帧") },
            { "_description", Tip("说明", "只给编辑器里的人看，不参与逻辑") },

            // 判定配置
            { "_meleeAttack", Tip("近身攻击", "判定体从角色身上生成后按配置独立位移") },
            { "_waveAttack", Tip("飞行道具", "判定体飞出去，可与对方飞行道具相消") },
            { "_comboAttack", Tip("空中组合技", "判定体每帧贴在角色身上，不独立位移") },
            { "_offsetX", Tip("横向偏移", "相对角色左边的偏移；朝左时自动镜像") },
            { "_offsetY", Tip("纵向偏移", "相对角色顶边的偏移，正数向下") },
            { "_moveX", Tip("横向位移", "判定体的横向位移，实际值 = moveX × 朝向") },
            { "_moveY", Tip("纵向位移", "判定体的纵向位移，正数向下") },
            { "_duration", Tip("位移时长", "毫秒") },
            { "_effect", Tip("命中特效", "hitEffect 里的类型名，同时也是图集名") },
            { "_beatState", Tip("受击状态", "命中后对方进入的 play 动作名") },
            { "_damage", Tip("伤害", "扣掉的血量") },
            { "_sizeOffsetX", Tip("尺寸偏移 X", "判定尺寸的横向偏移") },
            { "_sizeOffsetY", Tip("尺寸偏移 Y", "判定尺寸的纵向偏移") },
            { "_colliderSize", Tip("判定尺寸", "判定体边长") },
            { "_disappearEffect", Tip("消失特效", "命中或被相消时播放的特效类型") },
            { "_defenseDamage", Tip("防御伤害", "对方防御成功时削减的血量") },
            { "_size", Tip("判定边长", "判定体是正方形") },
        };

        private static void BeginGroup(string title)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void EndGroup() => EditorGUILayout.EndVertical();

        private GUIStyle CenteredStyle
        {
            get
            {
                if (_centeredStyle == null)
                {
                    _centeredStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                }

                return _centeredStyle;
            }
        }

        private GUIStyle SelectedStyle(bool selected)
        {
            if (_selectedStyle == null)
            {
                _selectedStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            }

            _selectedStyle.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            return _selectedStyle;
        }

        #endregion
    }
}
