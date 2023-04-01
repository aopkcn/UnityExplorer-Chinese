using UnityExplorer.UI;

namespace UnityExplorer.Config
{
    public static class ConfigManager
    {
        internal static readonly Dictionary<string, IConfigElement> ConfigElements = new();
        internal static readonly Dictionary<string, IConfigElement> InternalConfigs = new();

        // Each Mod Loader has its own ConfigHandler.
        // See the UnityExplorer.Loader namespace for the implementations.
        public static ConfigHandler Handler { get; private set; }

        // Actual UE Settings
        public static ConfigElement<KeyCode> Master_Toggle;
        public static ConfigElement<bool> Hide_On_Startup;
        public static ConfigElement<float> Startup_Delay_Time;
        public static ConfigElement<bool> Disable_EventSystem_Override;
        public static ConfigElement<int> Target_Display;
        public static ConfigElement<bool> Force_Unlock_Mouse;
        public static ConfigElement<KeyCode> Force_Unlock_Toggle;
        public static ConfigElement<string> Default_Output_Path;
        public static ConfigElement<string> DnSpy_Path;
        public static ConfigElement<bool> Log_Unity_Debug;
        public static ConfigElement<UIManager.VerticalAnchor> Main_Navbar_Anchor;
        public static ConfigElement<KeyCode> World_MouseInspect_Keybind;
        public static ConfigElement<KeyCode> UI_MouseInspect_Keybind;
        public static ConfigElement<string> CSConsole_Assembly_Blacklist;
        public static ConfigElement<string> Reflection_Signature_Blacklist;

        // internal configs
        internal static InternalConfigHandler InternalHandler { get; private set; }
        internal static readonly Dictionary<UIManager.Panels, ConfigElement<string>> PanelSaveData = new();

        internal static ConfigElement<string> GetPanelSaveData(UIManager.Panels panel)
        {
            if (!PanelSaveData.ContainsKey(panel))
                PanelSaveData.Add(panel, new ConfigElement<string>(panel.ToString(), string.Empty, string.Empty, true));
            return PanelSaveData[panel];
        }

        public static void Init(ConfigHandler configHandler)
        {
            Handler = configHandler;
            Handler.Init();

            InternalHandler = new InternalConfigHandler();
            InternalHandler.Init();

            CreateConfigElements();

            Handler.LoadConfig();
            InternalHandler.LoadConfig();

#if STANDALONE
            Loader.Standalone.ExplorerEditorBehaviour.Instance?.LoadConfigs();
#endif
        }

        internal static void RegisterConfigElement<T>(ConfigElement<T> configElement)
        {
            if (!configElement.IsInternal)
            {
                Handler.RegisterConfigElement(configElement);
                ConfigElements.Add(configElement.Name, configElement);
            }
            else
            {
                InternalHandler.RegisterConfigElement(configElement);
                InternalConfigs.Add(configElement.Name, configElement);
            }
        }

        private static void CreateConfigElements()
        {
            Master_Toggle = new("UnityExplorer 切换",
                "启用或禁用 UnityExplorer 的菜单和功能的键.",
                KeyCode.F7);

            Hide_On_Startup = new("启动时隐藏",
                "UnityExplorer 是否应该在启动时隐藏?",
                false);

            Startup_Delay_Time = new("启动延迟时间",
                "创建 UI 之前的启动延迟.",
                1f);

            Target_Display = new("目标显示器",
                "UnityExplorer 使用的监视器索引（如果有多个）。 0是默认显示, 1是次要的，等等. " +
                "更改此设置时建议重新启动。 确保您的额外显示器与主显示器的分辨率相同.",
                0);

            Force_Unlock_Mouse = new("强制解锁鼠标",
                "当 UnityExplorer 菜单打开时，强制解锁光标（可见）.",
                true);
            Force_Unlock_Mouse.OnValueChanged += (bool value) => UniverseLib.Config.ConfigManager.Force_Unlock_Mouse = value;

            Force_Unlock_Toggle = new("强制解锁切换键",
                "用于切换“强制解锁鼠标”设置的键绑定。 仅在 UnityExplorer 打开时可用。",
                KeyCode.None);

            Disable_EventSystem_Override = new("禁用事件系统覆盖",
                "如果启用，UnityExplorer 将不会覆盖游戏中的事件系统.\n<b>可能需要重启才能生效.</b>",
                false);
            Disable_EventSystem_Override.OnValueChanged += (bool value) => UniverseLib.Config.ConfigManager.Disable_EventSystem_Override = value;

            Default_Output_Path = new("默认输出路径",
                "从 UnityExplorer 导出东西时的默认输出路径.",
                Path.Combine(ExplorerCore.ExplorerFolder, "Output"));

            DnSpy_Path = new("dnSpy 路径",
                "dnSpy.exe（64 位）的完整路径.",
                @"C:/Program Files/dnspy/dnSpy.exe");

            Main_Navbar_Anchor = new("主要导航栏锚",
                "UnityExplorer 主导航栏的垂直锚点，如果你想移动它.",
                UIManager.VerticalAnchor.Top);

            Log_Unity_Debug = new("记录 Unity 调试",
                "UnityEngine.Debug.Log 消息是否应该打印到 UnityExplorer 的日志中?",
                false);

            World_MouseInspect_Keybind = new("世界鼠标检查键绑定",
                "可选的键绑定为世界模式鼠标检查.",
                KeyCode.None);

            UI_MouseInspect_Keybind = new("UI 鼠标检查键绑定",
                "用于开始 UI 模式鼠标检查的可选键绑定.",
                KeyCode.None);

            CSConsole_Assembly_Blacklist = new("CSharp 控制台组件黑名单",
                "使用它来将程序集名称列入黑名单，以防止 C# 控制台引用。 需要重置 C# 控制台.\n" +
                "用分号分隔每个程序集 ';'." +
                "例如，要将 Assembly-CSharp 列入黑名单，您可以添加“Assembly-CSharp;'",
                "");

            Reflection_Signature_Blacklist = new("成员签名黑名单",
                "如果已知某些成员签名会导致崩溃或其他问题，请使用它来将某些成员签名列入黑名单.\r\n" +
                "用分号分隔签名 ';'.\r\n" +
                "例如，要将 Camera.main 列入黑名单，您可以添加“UnityEngine.Camera.main;'",
                "");
        }
    }
}
