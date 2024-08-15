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
using System.Linq;
//using Penumbra.Api.IpcSubscribers.Legacy;
//using Glamourer.Api.IpcSubscribers.Legacy;
using Microsoft.Extensions.Logging;
using Glamourer.Api.IpcSubscribers;
using Glamourer.Api.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Utility;

namespace Snapper.Managers;

public delegate void PenumbraRedrawEvent(IntPtr address, int objTblIdx);
public delegate void HeelsOffsetChange(float change);
public delegate void PenumbraResourceLoadEvent(IntPtr drawObject, string gamePath, string filePath);
public delegate void CustomizePlusScaleChange(string? scale);




public class IpcManager : IDisposable
{
    private readonly IDalamudPluginInterface _pi;
    private const string TempCollectionPrefix = "Snap_";

    private readonly Glamourer.Api.IpcSubscribers.ApiVersion _glamourerApiVersion;
    private readonly ApplyState _glamourerApplyAll;
    private readonly GetStateBase64 _glamourerGetAllCustomization;
    private readonly GetStateBase64Name _glamourerGetAllCustomizationName;
    private readonly RevertState _glamourerRevertCustomization;

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
    private readonly RedrawObject _penumbraRedraw;
    private readonly Penumbra.Api.IpcSubscribers.Legacy.RedrawObject _penumbraRedrawObject;
    private readonly GetMetaManipulations _penumbraGetGameObjectMetaManipulations;
    private readonly AddTemporaryMod _penumbraAddTemporaryMod;
    private readonly CreateTemporaryCollection _penumbraCreateTemporaryCollection;
    //private readonly RemoveTemporaryCollection _penumbraRemoveTemporaryCollection;
    private readonly Penumbra.Api.IpcSubscribers.Legacy.RemoveTemporaryMod _penumbraRemoveTemporaryMod;
    private readonly AssignTemporaryCollection _penumbraAssignTemporaryCollection;
    private readonly ReverseResolvePlayerPath _reverseResolvePlayer;

    private readonly ICallGateSubscriber<string> _customizePlusApiVersion;
    private readonly ICallGateSubscriber<string> _customizePlusBranch;
    private readonly ICallGateSubscriber<string, string> _customizePlusGetBodyScale;
    private readonly ICallGateSubscriber<ICharacter?, string> _customizePlusGetBodyScaleFromCharacter; 
    private readonly ICallGateSubscriber<string, ICharacter?, object> _customizePlusSetBodyScaleToCharacter;
    private readonly ICallGateSubscriber<ICharacter?, object> _customizePlusRevert;
    private readonly ICallGateSubscriber<string?, object> _customizePlusOnScaleUpdate;

    private readonly DalamudUtil _dalamudUtil;
    private readonly ConcurrentQueue<Action> actionQueue = new();

    private Configuration _configuration;
    private string backupBase64 = "Bh+LCAAAAAAAAArEV9ly2jAU/Rc/Mxkv8pa3hoRAJ6QZoMmzAhfQRNiuJSdDM/n3XsmYeMNl2nr6Joujc+7mY/FujBiHR0gFiyPj0hoYNz8yluwgksbluzGlLBrTaKXWEwm7Ca4cYrr2wBimIBCzplzAwPiSJHxvXMo0Kx7mEs9WdqonDr+bh5WNy4+B8W29rusROySh54WW9Rei+U675hhoi6BveaQnwat4tS8L2mHg95UbFlNUuufapK/m3cGmquWYZl9aIwBZ0fLd3vK6oaloGxA36EvxHpYvrYp+X4pPKROyPUuvL83ZiEUbSFtF3d6G9H+Ijqke1vk2fquczR8Q8MhEnMckFvFmw2HVjnsCmiijLriacX5og4ky3ctbToUAvdR7KluzAR9mQsY79hO05ccr4AfcjC713iPlGS6c40ktiydvIVrltTxAauS51y32SZnGqmPGwDZbWUYo36hihpxGJYRb/31UDbWpQVkq5J53gzAOrmIRZZQd1HHzFxYNY553rIApD6/CbvagUbN6ek7QFl6d0fNPx9dQJy0FYZSPgMosBaurRRUkDuyZSOdsJDkb6Z6N9M5G+l1I/GTR5X5BpYzjLlyOqFfdbOn4cxq/Vcbn1FTcwfo3Q4HI+ZYm7a9gMYo7yvkEzbsrrPtYdM79V/rWdXwaZ3LbWUWWCMnyT9bptwZRjbH1m2KZWHKYom9194Pxem0ar8AV+tr8YGsdowIPaNTlVpwGzeAVr8rKnM8AN5JtluQJZASVTKuGnvssAh9oSncgUVxhjxLfX6cZlyzhrOLC1oXZsOrSGbxki/z2VOTQgs/7sIgjXb4HSJf4n4BuTrCrYb5ju2fKJ5GESDC5rx9rE9HO+AfnlAFfs/U6y+d6pjpiXniBjdfbEO8rtymA+gpfuIHphDb+Xbk6JOqaodpyG5TKgZuUvuPbLglLjJ7pB8SxvE9KojlJK+PRsUucrmmFnhmUOImOm5TCdCzfI77ptxYafaFE52hkOWmHWOp48EnntVf+XxAdXPY47EXhGmTHnYLruFFPkSXNRhCsOfaiTGjnncDCF4xYwC882VK9PtGVa1hSXo82tE1H3eaP3MVGwVw8H+ktPVhqDOqv6pTia4qfH/Wmfnz8AgAA//8=";

    public IpcManager(IDalamudPluginInterface pi, DalamudUtil dalamudUtil)
    {
        Logger.Verbose("Creating " + nameof(IpcManager));
        _configuration = (Configuration?)pi.GetPluginConfig();
        _pi = pi;

        _penumbraInit = Penumbra.Api.IpcSubscribers.Initialized.Subscriber(pi, () => PenumbraInit());
        _penumbraDispose = Penumbra.Api.IpcSubscribers.Disposed.Subscriber(pi, () => PenumbraDispose());
        _penumbraResolvePlayer = new ResolvePlayerPath(pi);
        _penumbraResolvePlayerObject = new ResolveGameObjectPath(pi);
        _penumbraReverseResolvePlayerObject = new ReverseResolveGameObjectPath(pi);
        _penumbraResolveModDir = new GetModDirectory(pi);
        _penumbraRedraw = new RedrawObject(pi);
        _penumbraRedrawObject = new Penumbra.Api.IpcSubscribers.Legacy.RedrawObject(pi);
        _reverseResolvePlayer = new ReverseResolvePlayerPath(pi);
        _penumbraObjectIsRedrawn = Penumbra.Api.IpcSubscribers.GameObjectRedrawn.Subscriber(pi, (ptr, idx) => RedrawEvent((IntPtr)ptr, idx));
        _penumbraGetGameObjectMetaManipulations = new GetMetaManipulations(pi);
        _penumbraAddTemporaryMod = new AddTemporaryMod(pi);
        _penumbraCreateTemporaryCollection = new CreateTemporaryCollection(pi);
        //_penumbraRemoveTemporaryCollection = new RemoveTemporaryCollectionByName(pi);
        _penumbraRemoveTemporaryMod = new Penumbra.Api.IpcSubscribers.Legacy.RemoveTemporaryMod(pi);
        _penumbraAssignTemporaryCollection = new AssignTemporaryCollection(pi);
        _penumbraEnabled = new GetEnabledState(pi);

        _penumbraGameObjectResourcePathResolved = Penumbra.Api.IpcSubscribers.GameObjectResourcePathResolved.Subscriber(pi, (ptr, arg1, arg2) => ResourceLoaded((IntPtr)ptr, arg1, arg2));
        _penumbraModSettingChanged = Penumbra.Api.IpcSubscribers.ModSettingChanged.Subscriber(pi, (modsetting, a, b, c) => PenumbraModSettingChangedHandler());

        _glamourerApiVersion = new Glamourer.Api.IpcSubscribers.ApiVersion(pi);
        _glamourerGetAllCustomization = new GetStateBase64(pi);
        _glamourerGetAllCustomizationName = new GetStateBase64Name(pi);

        _glamourerApplyAll = new ApplyState(pi);
        _glamourerRevertCustomization = new RevertState(pi);


        _customizePlusApiVersion = pi.GetIpcSubscriber<string>("CustomizePlus.GetApiVersion");
        _customizePlusBranch = pi.GetIpcSubscriber<string>("CustomizePlus.GetBranch");
        _customizePlusGetBodyScale = pi.GetIpcSubscriber<string, string>("CustomizePlus.GetTemporaryScale");
        _customizePlusGetBodyScaleFromCharacter = pi.GetIpcSubscriber<ICharacter?, string>("CustomizePlus.GetBodyScaleFromCharacter");
        _customizePlusRevert = pi.GetIpcSubscriber<ICharacter?, object>("CustomizePlus.RevertCharacter");
        _customizePlusSetBodyScaleToCharacter = pi.GetIpcSubscriber<string, ICharacter?, object>("CustomizePlus.SetBodyScaleToCharacter");
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
            return _glamourerApiVersion.Invoke() is { Major: 1, Minor: >= 1 };
        }
        catch
        {
            Logger.Warn("Glamourer API was not available");
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
            Logger.Warn("Penumbra API was not available");
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

    public string GetCustomizePlusScaleFromCharacter(ICharacter character)
    {
        if (!CheckCustomizePlusApi()) return string.Empty;
        var scale = _customizePlusGetBodyScale.InvokeFunc(character.Name.ToString());
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
            if (gameObj is ICharacter c)
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
            if (gameObj is ICharacter c)
            {
                Logger.Verbose("CustomizePlus reverting for " + c.Address.ToString("X"));
                _customizePlusRevert!.InvokeAction(c);
            }
        });
    }

    public void GlamourerApplyAll(string? customization, ICharacter obj)
    {
        if (!CheckGlamourerApi() || string.IsNullOrEmpty(customization)) return;
        Logger.Verbose("Glamourer applying for " + obj.Address.ToString("X"));
        _glamourerApplyAll!.Invoke(customization, obj.ObjectIndex);
    }

    public string GlamourerGetCharacterCustomization(IntPtr character)
    {
        object temp = "";
        object tempGameObj = "";
        Logger.Debug("Getting character customization");
        if (!CheckGlamourerApi()) return string.Empty;
        try
        {
            var gameObj = _dalamudUtil.CreateGameObject(character);
            if (gameObj is ICharacter c)
            {
                Logger.Debug($"Attempting to get customizations for {c.Name} with ObjectIndex {c.ObjectIndex}");
                (GlamourerApiEc apiec, string glamourerString) = _glamourerGetAllCustomization!.Invoke(c.ObjectIndex);
                temp = glamourerString;
                tempGameObj = c.Name;
                Logger.Debug($"Got glamourer customizations {glamourerString} for {c.Name}");
                if (glamourerString.IsNullOrEmpty())
                {
                    glamourerString = _configuration.FallBackGlamourerString;
                }

                byte[] bytes;

                try
                {
                    bytes = Convert.FromBase64String(glamourerString);
                }
                catch
                {
                    //if your backup string is not valid, you are getting my shitty lala.
                    bytes = Convert.FromBase64String(backupBase64);
                }
                
                return Convert.ToBase64String(bytes);
            }
            Logger.Warn("Game object is not an ICharacter or could not retrieve customization data.");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error occurred while getting customizations for {tempGameObj}. GlamourerString = '{temp}', Exception: {ex.Message}");
            throw;
            return string.Empty;
        }
    }

    public void GlamourerRevertCharacterCustomization(IGameObject character)
    {
        if (!CheckGlamourerApi()) return;
        if(character is ICharacter c)
        {
            actionQueue.Enqueue(() => _glamourerRevertCustomization!.Invoke(c.ObjectIndex));
        }
        else
        {
            Logger.Error("Tried to revert a non-Character game object and failed");
        }
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
            //var ret2 = _penumbraRemoveTemporaryCollection.Invoke(collName);
            //Logger.Verbose("RemoveTemporaryCollection: " + ret2);
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

    public void PenumbraSetTemporaryMods(ICharacter character, int? idx, Dictionary<string, string> modPaths, string manipulationData)
    {
        if (!CheckPenumbraApi()) return;
        if (idx == null)
        {
            return;
        }
        var collName = TempCollectionPrefix + character.Name.TextValue;
        var ret = _penumbraCreateTemporaryCollection.Invoke(collName);
        Logger.Verbose("Creating Temp Collection " + collName + ", Success: " + ret);
        var retAssign = _penumbraAssignTemporaryCollection.Invoke(ret, idx.Value, true);
        Logger.Verbose("Assigning Temp Collection " + collName + " to index " + idx.Value);
        Logger.Verbose("Penumbra response" + retAssign);
        foreach (var mod in modPaths)
        {
            Logger.Verbose(mod.Key + " => " + mod.Value);
        }

        var ret2 = _penumbraAddTemporaryMod.Invoke("Snap", ret, modPaths, manipulationData, 0);
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
