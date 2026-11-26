using p5rpc.tool.skill_effect_override.Template;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

#if DEBUG
using System.Diagnostics;
#endif

namespace p5rpc.tool.skill_effect_override
{
    public class Mod : ModBase
    {
        private readonly IModLoader _modLoader;
        private readonly IReloadedHooks? _hooks;
        private readonly ILogger _logger;
        private readonly IMod _owner;
        private readonly IModConfig _modConfig;

        [DllImport("SkillLogicHook.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AddSkillPair(ushort original, ushort replacement);
        [DllImport("SkillLogicHook.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Init();

        public nint _logicHookHandle;
        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _modConfig = context.ModConfig;

            string asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string nativePath = Path.Combine(asmDir, "SkillLogicHook.dll");

            _logicHookHandle = NativeLibrary.Load(nativePath);

            if (_logicHookHandle == IntPtr.Zero)
            {
                _logger.WriteLine("[Skill Effect Override] Failed to resolve DLL path.", _logger.ColorRedLight);
            }
            else
            {
                Init();
            }

                _modLoader.ModLoading += ModLoading;
            _modLoader.OnModLoaderInitialized += ModLoaderInit;
        }

        private void ModLoaderInit()
        {
            _modLoader.ModLoaded -= ModLoading;
        }

        private void ModLoading(IModV1 mod, IModConfigV1 modConfig)
        {
            if (_logicHookHandle == IntPtr.Zero)
                return;

            if (modConfig.ModDependencies.Contains(_modConfig.ModId))
            {
                string directory = _modLoader.GetDirectoryForModId(modConfig.ModId);
                string config = directory + $"{Path.DirectorySeparatorChar}skill_overrides.json";
                if (File.Exists(config))
                {
                    _logger.WriteLine($"[Skill Effect Override] Config file found at: {config}", _logger.ColorBlueLight);

                    string fileText = File.ReadAllText(config);
                    JsonNode? configNode = JsonNode.Parse(fileText);

                    if (configNode == null)
                    {
                        _logger.WriteLine($"[Skill Effect Override] config is null!", _logger.ColorRedLight);
                        return;
                    }

                    JsonObject? jObject = configNode.AsObject() ?? null;

                    if (jObject == null)
                    {
                        _logger.WriteLine($"[Skill Effect Override] json object is null!", _logger.ColorRedLight);
                        return;
                    }

                    foreach(var pair in jObject)
                    {
                        if (!ushort.TryParse(pair.Key, out var original))
                        {
                            _logger.WriteLine($"[Skill Effect Override] Key {pair.Key} is not valid as type ushort!", _logger.ColorRedLight);
                            continue;
                        }

                        ushort? replacement = pair.Value?.GetValue<ushort>();
                        if (replacement == null)
                        {
                            _logger.WriteLine($"[Skill Effect Override] Value {pair.Value} is not valid as type ushort!", _logger.ColorRedLight);
                            continue;
                        }

                        AddSkillPair(original, (ushort)replacement);
                        _logger.WriteLine($"[Skill Effect Override] Successfully added [{original}, {replacement}] pair to list.", _logger.ColorBlueLight);
                    }
                }
                else
                {
                    _logger.WriteLine($"[Skill Effect Override] No config specified at {config}", _logger.ColorRedLight);
                }
            }
        }

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}