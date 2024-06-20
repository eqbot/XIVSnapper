using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Action = System.Action;
using System.Collections.Concurrent;
using System.Text;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Snapper.Utils;
using System.Security;
using Glamourer.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using Dalamud.Interface.Internal.Notifications;
using System.Linq;
using Penumbra.Api.IpcSubscribers.Legacy;

namespace Snapper.Managers;

public delegate void PenumbraRedrawEvent(IntPtr address, int objTblIdx);
public delegate void HeelsOffsetChange(float change);
public delegate void PenumbraResourceLoadEvent(IntPtr drawObject, string gamePath, string filePath);
public delegate void CustomizePlusScaleChange(string? scale);
public class IpcManager : IDisposable
{
    private readonly DalamudPluginInterface _pi;
    private const string TempCollectionPrefix = "Snap_";

    private readonly ICallGateSubscriber<int> _glamourerApiVersion;
    private readonly ICallGateSubscriber<string, GameObject?, object>? _glamourerApplyAll;
    private readonly ICallGateSubscriber<GameObject?, string>? _glamourerGetAllCustomization;
    private readonly ICallGateSubscriber<GameObject?, object> _glamourerRevertCustomization;
    private readonly ICallGateSubscriber<string, GameObject?, object>? _glamourerApplyOnlyEquipment;
    private readonly ICallGateSubscriber<string, GameObject?, object>? _glamourerApplyOnlyCustomization;

    private readonly Penumbra.Api.Helpers.EventSubscriber _penumbraInit;
    private readonly Penumbra.Api.Helpers.EventSubscriber _penumbraDispose;
    private readonly Penumbra.Api.Helpers.EventSubscriber<nint, int> _penumbraObjectIsRedrawn;
    private readonly Penumbra.Api.Helpers.EventSubscriber<nint, string, string> _penumbraGameObjectResourcePathResolved;
    private readonly Penumbra.Api.Helpers.EventSubscriber<ModSettingChange, Guid, string, bool> _penumbraModSettingChanged;

    private readonly GetModDirectory _penumbraResolveModDir;
    private readonly ResolvePlayerPath _penumbraResolvePlayer;
    private readonly ResolveGameObjectPath _penumbraResolvePlayerObject;
    private readonly ReverseResolveGameObjectPath _penumbraReverseResolvePlayerObject;
    private readonly GetEnabledState _penumbraEnabled;
    private readonly RedrawObjectByIndex _penumbraRedraw;
    private readonly Penumbra.Api.IpcSubscribers.Legacy.RedrawObject _penumbraRedrawObject;
    private readonly GetGameObjectMetaManipulations _penumbraGetGameObjectMetaManipulations;
    private readonly Penumbra.Api.IpcSubscribers.Legacy.AddTemporaryMod _penumbraAddTemporaryMod;
    private readonly CreateNamedTemporaryCollection _penumbraCreateNamedTemporaryCollection;
    private readonly RemoveTemporaryCollectionByName _penumbraRemoveTemporaryCollection;
    private readonly Penumbra.Api.IpcSubscribers.Legacy.RemoveTemporaryMod _penumbraRemoveTemporaryMod;
    private readonly Penumbra.Api.IpcSubscribers.Legacy.AssignTemporaryCollection _penumbraAssignTemporaryCollection;
    private readonly ReverseResolvePlayerPath _reverseResolvePlayer;

    private readonly ICallGateSubscriber<string> _customizePlusApiVersion;
    private readonly ICallGateSubscriber<string> _customizePlusBranch;
    private readonly ICallGateSubscriber<string, string> _customizePlusGetBodyScale;
    private readonly ICallGateSubscriber<Character?, string> _customizePlusGetBodyScaleFromCharacter; 
    private readonly ICallGateSubscriber<string, Character?, object> _customizePlusSetBodyScaleToCharacter;
    private readonly ICallGateSubscriber<Character?, object> _customizePlusRevert;
    private readonly ICallGateSubscriber<string?, object> _customizePlusOnScaleUpdate;

    private readonly DalamudUtil _dalamudUtil;
    private readonly ConcurrentQueue<Action> actionQueue = new();

    public IpcManager(DalamudPluginInterface pi, DalamudUtil dalamudUtil)
    {
        Logger.Verbose("Creating " + nameof(IpcManager));

        _pi = pi;

        _penumbraInit = Penumbra.Api.IpcSubscribers.Initialized.Subscriber(pi, () => PenumbraInit());
        _penumbraDispose = Penumbra.Api.IpcSubscribers.Disposed.Subscriber(pi, () => PenumbraDispose());
        _penumbraResolvePlayer = new ResolvePlayerPath(pi);
        _penumbraResolvePlayerObject = new ResolveGameObjectPath(pi);
        _penumbraReverseResolvePlayerObject = new ReverseResolveGameObjectPath(pi);
        _penumbraResolveModDir = new GetModDirectory(pi);
        _penumbraRedraw = new RedrawObjectByIndex(pi);
        _penumbraRedrawObject = new Penumbra.Api.IpcSubscribers.Legacy.RedrawObject(pi);
        _reverseResolvePlayer = new ReverseResolvePlayerPath(pi);
        _penumbraObjectIsRedrawn = Penumbra.Api.IpcSubscribers.GameObjectRedrawn.Subscriber(pi, (ptr, idx) => RedrawEvent((IntPtr)ptr, idx));
        _penumbraGetGameObjectMetaManipulations = new GetGameObjectMetaManipulations(pi);
        _penumbraAddTemporaryMod = new Penumbra.Api.IpcSubscribers.Legacy.AddTemporaryMod(pi);
        _penumbraCreateNamedTemporaryCollection = new CreateNamedTemporaryCollection(pi);
        _penumbraRemoveTemporaryCollection = new RemoveTemporaryCollectionByName(pi);
        _penumbraRemoveTemporaryMod = new Penumbra.Api.IpcSubscribers.Legacy.RemoveTemporaryMod(pi);
        _penumbraAssignTemporaryCollection = new Penumbra.Api.IpcSubscribers.Legacy.AssignTemporaryCollection(pi);
        _penumbraEnabled = new GetEnabledState(pi);

        _penumbraGameObjectResourcePathResolved = Penumbra.Api.IpcSubscribers.GameObjectResourcePathResolved.Subscriber(pi, (ptr, arg1, arg2) => ResourceLoaded((IntPtr)ptr, arg1, arg2));
        _penumbraModSettingChanged = Penumbra.Api.IpcSubscribers.ModSettingChanged.Subscriber(pi, (modsetting, a, b, c) => PenumbraModSettingChangedHandler());

        _glamourerApiVersion = pi.GetIpcSubscriber<int>("Glamourer.ApiVersion");
        _glamourerGetAllCustomization = pi.GetIpcSubscriber<GameObject?, string>("Glamourer.GetAllCustomizationFromCharacter");
        _glamourerApplyAll = pi.GetIpcSubscriber<string, GameObject?, object>("Glamourer.ApplyAllToCharacter");
        _glamourerApplyOnlyCustomization = pi.GetIpcSubscriber<string, GameObject?, object>("Glamourer.ApplyOnlyCustomizationToCharacter");
        _glamourerApplyOnlyEquipment = pi.GetIpcSubscriber<string, GameObject?, object>("Glamourer.ApplyOnlyEquipmentToCharacter");
        _glamourerRevertCustomization = pi.GetIpcSubscriber<GameObject?, object>("Glamourer.RevertCharacter");

        _customizePlusApiVersion = pi.GetIpcSubscriber<string>("CustomizePlus.GetApiVersion");
        _customizePlusBranch = pi.GetIpcSubscriber<string>("CustomizePlus.GetBranch");
        _customizePlusGetBodyScale = pi.GetIpcSubscriber<string, string>("CustomizePlus.GetTemporaryScale");
        _customizePlusGetBodyScaleFromCharacter = pi.GetIpcSubscriber<Character?, string>("CustomizePlus.GetBodyScaleFromCharacter");
        _customizePlusRevert = pi.GetIpcSubscriber<Character?, object>("CustomizePlus.RevertCharacter");
        _customizePlusSetBodyScaleToCharacter = pi.GetIpcSubscriber<string, Character?, object>("CustomizePlus.SetBodyScaleToCharacter");
        _customizePlusOnScaleUpdate = pi.GetIpcSubscriber<string?, object>("CustomizePlus.OnScaleUpdate");

        _customizePlusOnScaleUpdate.Subscribe(OnCustomizePlusScaleChange);

        if (Initialized)
        {
            PenumbraInitialized?.Invoke();
        }

        _dalamudUtil = dalamudUtil;
        _dalamudUtil.FrameworkUpdate += HandleActionQueue;
        _dalamudUtil.ZoneSwitchEnd += ClearActionQueue;
    }

    private void PenumbraModSettingChangedHandler()
    {
        PenumbraModSettingChanged?.Invoke();
    }

    private void ClearActionQueue()
    {
        actionQueue.Clear();
    }

    private void ResourceLoaded(IntPtr ptr, string arg1, string arg2)
    {
        if (ptr != IntPtr.Zero && string.Compare(arg1, arg2, true, System.Globalization.CultureInfo.InvariantCulture) != 0)
        {
            PenumbraResourceLoadEvent?.Invoke(ptr, arg1, arg2);
        }
    }

    private void HandleActionQueue()
    {
        if (actionQueue.TryDequeue(out var action))
        {
            if (action == null) return;
            Logger.Debug("Execution action in queue: " + action.Method);
            action();
        }
    }

    public event VoidDelegate? PenumbraModSettingChanged;
    public event VoidDelegate? PenumbraInitialized;
    public event VoidDelegate? PenumbraDisposed;
    public event PenumbraRedrawEvent? PenumbraRedrawEvent;
    public event HeelsOffsetChange? HeelsOffsetChangeEvent;
    public event PenumbraResourceLoadEvent? PenumbraResourceLoadEvent;
    public event CustomizePlusScaleChange? CustomizePlusScaleChange;

    public bool Initialized => CheckPenumbraApi();
    public bool CheckGlamourerApi()
    {
        try
        {
            return _glamourerApiVersion.InvokeFunc() >= 0;
        }
        catch
        {
            return false;
        }
    }

    public bool CheckPenumbraApi()
    {
        bool penumbraAvailable = false;
        try
        {
            var penumbraVersion = (_pi.InstalledPlugins
                .FirstOrDefault(p => string.Equals(p.InternalName, "Penumbra", StringComparison.OrdinalIgnoreCase))
                ?.Version ?? new Version(0, 0, 0, 0));
            penumbraAvailable = penumbraVersion >= new Version(1, 1, 0, 0);
            penumbraAvailable &= _penumbraEnabled.Invoke();
            return penumbraAvailable;
        }
        catch
        {
            return false;
        }
    }

    public bool CheckCustomizePlusApi()
    {
        try
        {
            return string.Equals(_customizePlusApiVersion.InvokeFunc(), "1.0", StringComparison.Ordinal) && string.Equals(_customizePlusBranch.InvokeFunc(), "eqbot", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
    public bool CheckCustomizePlusBranch()
    {
        try
        {
            return string.Equals(_customizePlusApiVersion.InvokeFunc(), "1.0", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Logger.Verbose("Disposing " + nameof(IpcManager));

        int totalSleepTime = 0;
        while (actionQueue.Count > 0 && totalSleepTime < 2000)
        {
            Logger.Verbose("Waiting for actionqueue to clear...");
            HandleActionQueue();
            System.Threading.Thread.Sleep(16);
            totalSleepTime += 16;
        }

        if (totalSleepTime >= 2000)
        {
            Logger.Verbose("Action queue clear or not, disposing");
        }

        _dalamudUtil.FrameworkUpdate -= HandleActionQueue;
        _dalamudUtil.ZoneSwitchEnd -= ClearActionQueue;
        actionQueue.Clear();

        _penumbraGameObjectResourcePathResolved.Dispose();
        _penumbraDispose.Dispose();
        _penumbraInit.Dispose();
        _penumbraObjectIsRedrawn.Dispose();
        _penumbraModSettingChanged.Dispose();
    }

    public string GetCustomizePlusScale()
    {
        if (!CheckCustomizePlusApi()) return string.Empty;
        var scale = _customizePlusGetBodyScale.InvokeFunc(_dalamudUtil.PlayerName);
        if (string.IsNullOrEmpty(scale)) return string.Empty;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(scale));
    }

    public string GetCustomizePlusScaleFromCharacter(Character character)
    {
        if (!CheckCustomizePlusApi()) return string.Empty;
        var scale = _customizePlusGetBodyScale.InvokeFunc(character.Name.TextValue);
        if (string.IsNullOrEmpty(scale))
        {
            Logger.Debug("C+ returned null");
            return string.Empty;
        }
        return scale;
    }

    public void CustomizePlusSetBodyScale(IntPtr character, string scale)
    {
        if (!CheckCustomizePlusApi() || string.IsNullOrEmpty(scale)) return;
        actionQueue.Enqueue(() =>
        {
            var gameObj = _dalamudUtil.CreateGameObject(character);
            if (gameObj is Character c)
            {
                Logger.Verbose("CustomizePlus applying for " + c.Address.ToString("X"));
                _customizePlusSetBodyScaleToCharacter!.InvokeAction(scale, c);
            }
        });
    }

    public void CustomizePlusRevert(IntPtr character)
    {
        if (!CheckCustomizePlusApi()) return;
        actionQueue.Enqueue(() =>
        {
            var gameObj = _dalamudUtil.CreateGameObject(character);
            if (gameObj is Character c)
            {
                Logger.Verbose("CustomizePlus reverting for " + c.Address.ToString("X"));
                _customizePlusRevert!.InvokeAction(c);
            }
        });
    }

    public void GlamourerApplyAll(string? customization, Character obj)
    {
        if (!CheckGlamourerApi() || string.IsNullOrEmpty(customization)) return;
        Logger.Verbose("Glamourer applying for " + obj.Address.ToString("X"));
        _glamourerApplyAll!.InvokeAction(customization, obj);
    }

    public void GlamourerApplyOnlyEquipment(string customization, IntPtr character)
    {
        if (!CheckGlamourerApi() || string.IsNullOrEmpty(customization)) return;
        actionQueue.Enqueue(() =>
        {
            var gameObj = _dalamudUtil.CreateGameObject(character);
            if (gameObj is Character c)
            {
                Logger.Verbose("Glamourer apply only equipment to " + c.Address.ToString("X"));
                _glamourerApplyOnlyEquipment!.InvokeAction(customization, c);
            }
        });
    }

    public void GlamourerApplyOnlyCustomization(string customization, IntPtr character)
    {
        if (!CheckGlamourerApi() || string.IsNullOrEmpty(customization)) return;
        actionQueue.Enqueue(() =>
        {
            var gameObj = _dalamudUtil.CreateGameObject(character);
            if (gameObj is Character c)
            {
                Logger.Verbose("Glamourer apply only customization to " + c.Address.ToString("X"));
                _glamourerApplyOnlyCustomization!.InvokeAction(customization, c);
            }
        });
    }

    public string GlamourerGetCharacterCustomization(IntPtr character)
    {
        if (!CheckGlamourerApi()) return string.Empty;
        try
        {
            var gameObj = _dalamudUtil.CreateGameObject(character);
            if (gameObj is Character c)
            {
                var glamourerString = _glamourerGetAllCustomization!.InvokeFunc(c);
                byte[] bytes = Convert.FromBase64String(glamourerString);
                return Convert.ToBase64String(bytes);
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void GlamourerRevertCharacterCustomization(GameObject character)
    {
        if (!CheckGlamourerApi()) return;
        actionQueue.Enqueue(() => _glamourerRevertCustomization!.InvokeAction(character));
    }

    public string PenumbraGetGameObjectMetaManipulations(int objIdx)
    {
        if (!CheckPenumbraApi()) return string.Empty;
        return _penumbraGetGameObjectMetaManipulations.Invoke(objIdx);
    }

    public void PenumbraRedraw(IntPtr obj)
    {
        if (!CheckPenumbraApi()) return;
        actionQueue.Enqueue(() =>
        {
            var gameObj = _dalamudUtil.CreateGameObject(obj);
            if (gameObj != null)
            {
                Logger.Verbose("Redrawing " + gameObj);
                _penumbraRedrawObject!.Invoke(gameObj, RedrawType.Redraw);
            }
        });
    }

    public void PenumbraRedraw(int objIdx)
    {
        if (!CheckPenumbraApi()) return;
        _penumbraRedraw!.Invoke(objIdx, RedrawType.Redraw);
    }

    public void PenumbraRemoveTemporaryCollection(string characterName)
    {
        if (!CheckPenumbraApi()) return;
        actionQueue.Enqueue(() =>
        {
            var collName = TempCollectionPrefix + characterName;
            Logger.Verbose("Removing temp collection for " + collName);
            var ret = _penumbraRemoveTemporaryMod.Invoke("Snap", collName, 0);
            Logger.Verbose("RemoveTemporaryMod: " + ret);
            var ret2 = _penumbraRemoveTemporaryCollection.Invoke(collName);
            Logger.Verbose("RemoveTemporaryCollection: " + ret2);
        });
    }

    public string PenumbraResolvePath(string path)
    {
        if (!CheckPenumbraApi()) return path;
        var resolvedPath = _penumbraResolvePlayer!.Invoke(path);
        return resolvedPath ?? path;
    }

    public string[] PenumbraReverseResolvePlayer(string path)
    {
        if (!CheckPenumbraApi()) return new[] { path };
        var resolvedPaths = _reverseResolvePlayer.Invoke(path);
        if (resolvedPaths.Length == 0)
        {
            resolvedPaths = new[] { path };
        }
        return resolvedPaths;
    }

    public string PenumbraResolvePathObject(string path, int objIdx)
    {
        if (!CheckPenumbraApi()) return path;
        var resolvedPath = _penumbraResolvePlayerObject!.Invoke(path, objIdx);
        return resolvedPath ?? path;
    }

    public string[] PenumbraReverseResolveObject(string path, int objIdx)
    {
        if (!CheckPenumbraApi()) return new[] { path };
        var resolvedPaths = _penumbraReverseResolvePlayerObject.Invoke(path, objIdx);
        if (resolvedPaths.Length == 0)
        {
            resolvedPaths = new[] { path };
        }
        return resolvedPaths;
    }

    public void PenumbraSetTemporaryMods(Character character, int? idx, Dictionary<string, string> modPaths, string manipulationData)
    {
        if (!CheckPenumbraApi()) return;
        if (idx == null)
        {
            return;
        }
        var collName = TempCollectionPrefix + character.Name.TextValue;
        var ret = _penumbraCreateNamedTemporaryCollection.Invoke(collName);
        Logger.Verbose("Creating Temp Collection " + collName + ", Success: " + ret);
        var retAssign = _penumbraAssignTemporaryCollection.Invoke(collName, idx.Value, true);
        Logger.Verbose("Assigning Temp Collection " + collName + " to index " + idx.Value);
        Logger.Verbose("Penumbra response" + retAssign);
        foreach (var mod in modPaths)
        {
            Logger.Verbose(mod.Key + " => " + mod.Value);
        }

        var ret2 = _penumbraAddTemporaryMod.Invoke("Snap", collName, modPaths, manipulationData, 0);
        Logger.Verbose("Setting temp mods for " + collName + ", Success: " + ret2);
    }

    private void RedrawEvent(IntPtr objectAddress, int objectTableIndex)
    {
        PenumbraRedrawEvent?.Invoke(objectAddress, objectTableIndex);
    }

    private void PenumbraInit()
    {
        PenumbraInitialized?.Invoke();
        //_penumbraRedraw!.Invoke("self", RedrawType.Redraw);
    }

    private void OnCustomizePlusScaleChange(string? scale)
    {
        if (scale != null) scale = Convert.ToBase64String(Encoding.UTF8.GetBytes(scale));
        CustomizePlusScaleChange?.Invoke(scale);
    }

    private void PenumbraDispose()
    {
        PenumbraDisposed?.Invoke();
        actionQueue.Clear();
    }
}
