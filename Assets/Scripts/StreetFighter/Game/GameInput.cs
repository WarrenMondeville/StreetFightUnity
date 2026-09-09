using System.Collections.Generic;
using StreetFighter.Core;
using StreetFighter.Gameplay;
using UnityEngine.InputSystem;

namespace StreetFighter.Game
{
    /// <summary>
    /// 全局输入管理（Input System）：
    /// 系统动作（暂停 / 模式切换）由这里统一提供，
    /// 并按玩家序号把键盘与手柄分配给各个 <see cref="FighterInput"/>。
    /// 键盘两个玩家共用（各用一组按键），手柄按序号分给 P1 / P2。
    /// </summary>
    public static class GameInput
    {
        private const string SystemMapName = "System";

        private static readonly List<FighterInput> Players = new List<FighterInput>();

        private static InputActionMap _system;
        private static InputAction _pause;
        private static InputAction _versusAi;
        private static InputAction _versusPlayer;

        /// <summary>是否已经初始化。</summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>本帧是否按下了暂停键（F2 / 手柄 Start）。</summary>
        public static bool WasPausePressed => WasPressed(_pause);

        /// <summary>本帧是否按下了人机模式键（主键盘 1）。</summary>
        public static bool WasVersusAiPressed => WasPressed(_versusAi);

        /// <summary>本帧是否按下了双人模式键（主键盘 2）。</summary>
        public static bool WasVersusPlayerPressed => WasPressed(_versusPlayer);

        /// <summary>创建系统动作并开始监听设备变化，可重复调用。</summary>
        public static void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            _system = new InputActionMap(SystemMapName);

            _pause = _system.AddAction(InputActionNames.Pause, InputActionType.Button, "<Keyboard>/f2");
            _pause.AddBinding("<Gamepad>/start");

            _versusAi = _system.AddAction(InputActionNames.VersusAi, InputActionType.Button, "<Keyboard>/1");
            _versusPlayer = _system.AddAction(InputActionNames.VersusPlayer, InputActionType.Button, "<Keyboard>/2");

            _system.Enable();
            InputSystem.onDeviceChange += OnDeviceChange;
            IsInitialized = true;
        }

        /// <summary>释放系统动作与监听。</summary>
        public static void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            InputSystem.onDeviceChange -= OnDeviceChange;
            IsInitialized = false;

            _system.Disable();
            _system = null;
            _pause = null;
            _versusAi = null;
            _versusPlayer = null;

            Players.Clear();
        }

        /// <summary>登记一个玩家输入，并按序号重新分配设备。</summary>
        internal static void Register(FighterInput player)
        {
            Initialize();

            if (Players.Contains(player))
            {
                return;
            }

            Players.Add(player);
            AssignDevices();
        }

        /// <summary>注销一个玩家输入。</summary>
        internal static void Unregister(FighterInput player)
        {
            if (Players.Remove(player))
            {
                AssignDevices();
            }
        }

        private static bool WasPressed(InputAction action) => action != null && action.WasPressedThisFrame();

        private static void AssignDevices()
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].SetDevices(DevicesOf(i));
            }
        }

        private static InputDevice[] DevicesOf(int playerIndex)
        {
            var devices = new List<InputDevice>(2);

            if (Keyboard.current != null)
            {
                devices.Add(Keyboard.current);
            }

            if (playerIndex < Gamepad.all.Count)
            {
                devices.Add(Gamepad.all[playerIndex]);
            }

            return devices.ToArray();
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change) => AssignDevices();
    }
}
