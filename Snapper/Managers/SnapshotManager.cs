using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Penumbra.Api;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource;
using Penumbra.Interop.Structs;
using Lumina.Models.Materials;
using Snapper.Models;
using Snapper.Utils;
using System.Threading;
using Dalamud.Game.ClientState.Objects.Enums;
using Penumbra.String;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.IO;
using System.Text.Json;
using Snapper.Interop;
using Dalamud.Utility;

namespace Snapper.Managers
{
    public class SnapshotManager
    {
        private Plugin Plugin;
        private List<Character> tempCollections = new();

        public SnapshotManager(Plugin plugin)
        {

            this.Plugin = plugin;
        }

        public void RevertAllSnapshots()
        {
            foreach(var character in tempCollections)
            {
                Plugin.IpcManager.PenumbraRemoveTemporaryCollection(character.Name.TextValue);
                Plugin.IpcManager.GlamourerRevertCharacterCustomization(character);
                Plugin.IpcManager.CustomizePlusRevert(character.Address);
            }
            tempCollections.Clear();
        }

        public bool AppendSnapshot(Character character)
        {
            var charaName = character.Name.TextValue;
            var path = Path.Combine(Plugin.Configuration.WorkingDirectory, charaName);
            string infoJson = File.ReadAllText(Path.Combine(path, "snapshot.json"));
            if (infoJson == null)
            {
                Logger.Warn("No snapshot json found, aborting");
                return false;
            }
            SnapshotInfo? snapshotInfo = JsonSerializer.Deserialize<SnapshotInfo>(infoJson);
            if (snapshotInfo == null)
            {
                Logger.Warn("Failed to deserialize snapshot json, aborting");
                return false;
            }

            if (!Directory.Exists(path))
            {
                //no existing snapshot for character, just use save mode
                this.SaveSnapshot(character);
            }

            //Get glamourer string
            //snapshotInfo.GlamourerString = Plugin.IpcManager.GlamourerGetCharacterCustomization(character.Address);
            //Logger.Debug($"Got glamourer string {snapshotInfo.GlamourerString}");

            //Save all file replacements

            List<FileReplacement> replacements = GetFileReplacementsForCharacter(character);

            Logger.Debug($"Got {replacements.Count} replacements");

            foreach (var replacement in replacements)
            {
                FileInfo replacementFile = new FileInfo(replacement.ResolvedPath);
                FileInfo fileToCreate = new FileInfo(Path.Combine(path, replacement.GamePaths[0]));
                if (!fileToCreate.Exists)
                {
                    //totally new file
                    fileToCreate.Directory.Create();
                    replacementFile.CopyTo(fileToCreate.FullName);
                    foreach (var gamePath in replacement.GamePaths)
                    {
                        var collisions = snapshotInfo.FileReplacements.Where(src => src.Value.Any(path => path == gamePath));
                        foreach (var collision in collisions)
                        {
                            collision.Value.Remove(gamePath);
                            if (collision.Value.Count == 0)
                            {
                                snapshotInfo.FileReplacements.Remove(collision.Key);
                                File.Delete(Path.Combine(path, collision.Key));
                            }
                        }
                    }
                    snapshotInfo.FileReplacements.Add(replacement.GamePaths[0], replacement.GamePaths);
                }
            }

            string infoJsonWrite = JsonSerializer.Serialize(snapshotInfo);
            File.WriteAllText(Path.Combine(path, "snapshot.json"), infoJson);

            return true;
        }

        public bool SaveSnapshot(Character character)
        {
            var charaName = character.Name.TextValue;
            var path = Path.Combine(Plugin.Configuration.WorkingDirectory,charaName);
            SnapshotInfo snapshotInfo = new();

            if (Directory.Exists(path))
            {
                Logger.Warn("Snapshot already existed, deleting");
                Directory.Delete(path, true);
            }
            Directory.CreateDirectory(path);

            //Get glamourer string
            snapshotInfo.GlamourerString = Plugin.IpcManager.GlamourerGetCharacterCustomization(character.Address);
            Logger.Debug($"Got glamourer string {snapshotInfo.GlamourerString}");

            //Save all file replacements
            
            List<FileReplacement> replacements = GetFileReplacementsForCharacter(character);

            Logger.Debug($"Got {replacements.Count} replacements");

            foreach(var replacement in replacements)
            {
                FileInfo replacementFile = new FileInfo(replacement.ResolvedPath);
                FileInfo fileToCreate = new FileInfo(Path.Combine(path, replacement.GamePaths[0]));
                fileToCreate.Directory.Create();
                replacementFile.CopyTo(fileToCreate.FullName);
                snapshotInfo.FileReplacements.Add(replacement.GamePaths[0], replacement.GamePaths);
            }

            snapshotInfo.ManipulationString = Plugin.IpcManager.PenumbraGetGameObjectMetaManipulations(character.ObjectIndex);


            //Get customize+ data, if applicable
            if (Plugin.IpcManager.CheckCustomizePlusApi())
            {
                Logger.Debug("C+ api loaded");
                var data = Plugin.IpcManager.GetCustomizePlusScaleFromCharacter(character);
                //Logger.Info(Plugin.DalamudUtil.PlayerName);
                //Logger.Info(character.Name.TextValue);
                //Logger.Info($"Cust+: {data}");
                if (!data.IsNullOrEmpty())
                {
                    File.WriteAllText(Path.Combine(path, "customizePlus.json"), data);
                }
            }

            string infoJson = JsonSerializer.Serialize(snapshotInfo);
            File.WriteAllText(Path.Combine(path, "snapshot.json"), infoJson);

            return true;
        }

        public bool LoadSnapshot(Character characterApplyTo, int objIdx, string path)
        {
            Logger.Info($"Applying snapshot to {characterApplyTo.Address}");
            string infoJson = File.ReadAllText(Path.Combine(path,"snapshot.json"));
            if (infoJson == null)
            {
                Logger.Warn("No snapshot json found, aborting");
                return false;
            }
            SnapshotInfo? snapshotInfo = JsonSerializer.Deserialize<SnapshotInfo>(infoJson);
            if(snapshotInfo == null)
            {
                Logger.Warn("Failed to deserialize snapshot json, aborting");
                return false;
            }

            //Apply mods
            Dictionary<string, string> moddedPaths = new();
            foreach(var replacement in snapshotInfo.FileReplacements)
            {
                foreach(var gamePath in replacement.Value)
                {
                    moddedPaths.Add(gamePath, Path.Combine(path, replacement.Key));
                }
            }
            Logger.Debug($"Applied {moddedPaths.Count} replacements");

            Plugin.IpcManager.PenumbraRemoveTemporaryCollection(characterApplyTo.Name.TextValue);
            Plugin.IpcManager.PenumbraSetTemporaryMods(characterApplyTo, objIdx, moddedPaths, snapshotInfo.ManipulationString);
            if (!tempCollections.Contains(characterApplyTo))
            {
                tempCollections.Add(characterApplyTo);
            }

            //Apply Customize+ if it exists and C+ is installed
            if (Plugin.IpcManager.CheckCustomizePlusApi())
            {
                if(File.Exists(Path.Combine(path, "customizePlus.json")))
                {
                    string custPlusData = File.ReadAllText(Path.Combine(path, "customizePlus.json"));
                    Plugin.IpcManager.CustomizePlusSetBodyScale(characterApplyTo.Address, custPlusData);
                }
            }

            //Apply glamourer string
            Plugin.IpcManager.GlamourerApplyAll(snapshotInfo.GlamourerString, characterApplyTo);

            return true;
        }

        private int? GetObjIDXFromCharacter(Character character)
        {
            for (var i = 0; i <= Plugin.Objects.Length; i++)
            {
                global::Dalamud.Game.ClientState.Objects.Types.GameObject current = Plugin.Objects[i];
                if (!(current == null) && current.ObjectId == character.ObjectId)
                {
                    return i;
                }
            }
            return null;
        }

        public unsafe List<FileReplacement> GetFileReplacementsForCharacter(Character character)
        {
            List<FileReplacement> replacements = new List<FileReplacement>();
            var charaPointer = character.Address;
            var objectKind = character.ObjectKind;
            var charaName = character.Name.TextValue;
            int? objIdx = GetObjIDXFromCharacter(character);

            Logger.Debug($"Character name {charaName}");
            if(objIdx == null)
            {
                Logger.Error("Unable to find character in object table, aborting search for file replacements");
                return replacements;
            }
            Logger.Debug($"Object IDX {objIdx}");

            var chara = Plugin.DalamudUtil.CreateGameObject(charaPointer)!;
            while (!Plugin.DalamudUtil.IsObjectPresent(chara))
            {
                Logger.Verbose("Character is null but it shouldn't be, waiting");
                Thread.Sleep(50);
            }

            Plugin.DalamudUtil.WaitWhileCharacterIsDrawing(objectKind.ToString(), charaPointer, 15000);

            var baseCharacter = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)(void*)charaPointer;
            var human = (Human*)baseCharacter->GameObject.GetDrawObject();
            for (var mdlIdx = 0; mdlIdx < human->CharacterBase.SlotCount; ++mdlIdx)
            {
                var mdl = (RenderModel*)human->CharacterBase.ModelArray[mdlIdx];
                if (mdl == null || mdl->ResourceHandle == null || mdl->ResourceHandle->Category != ResourceCategory.Chara)
                {
                    continue;
                }

                AddReplacementsFromRenderModel(mdl, replacements, objIdx.Value, 0);
            }

            AddPlayerSpecificReplacements(replacements, charaPointer, human, objIdx.Value);

            return replacements;
        }

        private unsafe void AddReplacementsFromRenderModel(RenderModel* mdl, List<FileReplacement> replacements, int objIdx, int inheritanceLevel = 0)
        {
            if (mdl == null || mdl->ResourceHandle == null || mdl->ResourceHandle->Category != ResourceCategory.Chara)
            {
                return;
            }

            string mdlPath;
            try
            {
                mdlPath = new ByteString(mdl->ResourceHandle->FileName()).ToString();
            }
            catch
            {
                Logger.Warn("Could not get model data");
                return;
            }
            Logger.Verbose("Checking File Replacement for Model " + mdlPath);

            FileReplacement mdlFileReplacement = CreateFileReplacement(mdlPath, objIdx);
            //DebugPrint(mdlFileReplacement, objectKind, "Model", inheritanceLevel);

            AddFileReplacement(replacements, mdlFileReplacement);

            for (var mtrlIdx = 0; mtrlIdx < mdl->MaterialCount; mtrlIdx++)
            {
                var mtrl = (Penumbra.Interop.Structs.Material*)mdl->Materials[mtrlIdx];
                if (mtrl == null) continue;

                AddReplacementsFromMaterial(mtrl, replacements, objIdx, inheritanceLevel + 1);
            }
        }

        private unsafe void AddReplacementsFromMaterial(Penumbra.Interop.Structs.Material* mtrl, List<FileReplacement> replacements, int objIdx, int inheritanceLevel = 0)
        {
            string fileName;
            try
            {
                fileName = new ByteString(mtrl->ResourceHandle->FileName()).ToString();

            }
            catch
            {
                Logger.Warn("Could not get material data");
                return;
            }

            Logger.Verbose("Checking File Replacement for Material " + fileName);
            var mtrlArray = fileName.Split("|");
            string mtrlPath;
            if (mtrlArray.Count() >= 3)
            {
                mtrlPath = fileName.Split("|")[2];
            }
            else
            {
                Logger.Warn($"Material {fileName} did not split into at least 3 partts");
                return;
            }

            if (replacements.Any(c => c.ResolvedPath.Contains(mtrlPath, StringComparison.Ordinal)))
            {
                return;
            }

            var mtrlFileReplacement = CreateFileReplacement(mtrlPath, objIdx);
            //DebugPrint(mtrlFileReplacement, objectKind, "Material", inheritanceLevel);

            AddFileReplacement(replacements, mtrlFileReplacement);

            var mtrlResourceHandle = (MtrlResource*)mtrl->ResourceHandle;
            for (var resIdx = 0; resIdx < mtrlResourceHandle->NumTex; resIdx++)
            {
                string? texPath = null;
                try
                {
                    texPath = new ByteString(mtrlResourceHandle->TexString(resIdx)).ToString();
                }
                catch
                {
                    Logger.Warn("Could not get Texture data for Material " + fileName);
                }

                if (string.IsNullOrEmpty(texPath)) continue;

                Logger.Verbose("Checking File Replacement for Texture " + texPath);

                AddReplacementsFromTexture(texPath, replacements, objIdx, inheritanceLevel + 1);
            }

            try
            {
                var shpkPath = "shader/sm5/shpk/" + new ByteString(mtrlResourceHandle->ShpkString).ToString();
                Logger.Verbose("Checking File Replacement for Shader " + shpkPath);
                AddReplacementsFromShader(shpkPath, replacements, objIdx, inheritanceLevel + 1);
            }
            catch
            {
                Logger.Verbose("Could not find shpk for Material " + fileName);
            }
        }

        private void AddReplacementsFromTexture(string texPath, List<FileReplacement> replacements, int objIdx, int inheritanceLevel = 0, bool doNotReverseResolve = true)
        {
            Logger.Debug($"Adding replacement for texture {texPath}");
            if (string.IsNullOrEmpty(texPath)) return;

            if(replacements.Any(c => c.GamePaths.Contains(texPath, StringComparer.Ordinal)))
            {
                Logger.Debug($"Replacements already contain {texPath}, skipping");
                return;
            }

            var texFileReplacement = CreateFileReplacement(texPath, objIdx, doNotReverseResolve);
            //DebugPrint(texFileReplacement, objectKind, "Texture", inheritanceLevel);

            AddFileReplacement(replacements, texFileReplacement);

            if (texPath.Contains("/--", StringComparison.Ordinal)) return;

            var texDx11Replacement =
                CreateFileReplacement(texPath.Insert(texPath.LastIndexOf('/') + 1, "--"), objIdx, doNotReverseResolve);

            //DebugPrint(texDx11Replacement, objectKind, "Texture (DX11)", inheritanceLevel);

            AddFileReplacement(replacements, texDx11Replacement);
        }

        private void AddReplacementsFromShader(string shpkPath, List<FileReplacement> replacements, int objIdx, int inheritanceLevel = 0)
        {
            if (string.IsNullOrEmpty(shpkPath)) return;

            if (replacements.Any(c => c.GamePaths.Contains(shpkPath, StringComparer.Ordinal)))
            {
                return;
            }

            var shpkFileReplacement = CreateFileReplacement(shpkPath, objIdx);
            //DebugPrint(shpkFileReplacement, objectKind, "Shader", inheritanceLevel);
            AddFileReplacement(replacements, shpkFileReplacement);
        }

        private unsafe void AddPlayerSpecificReplacements(List<FileReplacement> replacements, IntPtr charaPointer, Human* human, int objIdx)
        {
            var weaponObject = (Interop.Weapon*)((FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object*)human)->ChildObject;

            if ((IntPtr)weaponObject != IntPtr.Zero)
            {
                var mainHandWeapon = weaponObject->WeaponRenderModel->RenderModel;

                AddReplacementsFromRenderModel(mainHandWeapon, replacements, objIdx, 0);

                /*
                foreach (var item in replacements)
                {
                    _transientResourceManager.RemoveTransientResource(charaPointer, item);
                }
                */
                /*
                foreach (var item in _transientResourceManager.GetTransientResources((IntPtr)weaponObject))
                {
                    Logger.Verbose("Found transient weapon resource: " + item);
                    AddReplacement(item, objectKind, previousData, 1, true);
                }
                */


                if (weaponObject->NextSibling != (IntPtr)weaponObject)
                {
                    var offHandWeapon = ((Interop.Weapon*)weaponObject->NextSibling)->WeaponRenderModel->RenderModel;

                    AddReplacementsFromRenderModel(offHandWeapon, replacements, objIdx, 1);
                    /*
                    foreach (var item in replacements)
                    {
                        _transientResourceManager.RemoveTransientResource((IntPtr)offHandWeapon, item);
                    }

                    foreach (var item in _transientResourceManager.GetTransientResources((IntPtr)offHandWeapon))
                    {
                        Logger.Verbose("Found transient offhand weapon resource: " + item);
                        AddReplacement(item, objectKind, previousData, 1, true);
                    }
                    */
                }
            }

            AddReplacementSkeleton(((Interop.HumanExt*)human)->Human.RaceSexId, objIdx, replacements);
            try
            {
                AddReplacementsFromTexture(new ByteString(((Interop.HumanExt*)human)->Decal->FileName()).ToString(), replacements, objIdx, 0, false);
            }
            catch
            {
                Logger.Warn("Could not get Decal data");
            }
            try
            {
                AddReplacementsFromTexture(new ByteString(((Interop.HumanExt*)human)->LegacyBodyDecal->FileName()).ToString(), replacements, objIdx, 0, false);
            }
            catch
            {
                Logger.Warn("Could not get Legacy Body Decal Data");
            }
            /*
            foreach (var item in previousData.FileReplacements[objectKind])
            {
                _transientResourceManager.RemoveTransientResource(charaPointer, item);
            }
            */
        }

        private void AddReplacementSkeleton(ushort raceSexId, int objIdx, List<FileReplacement> replacements)
        {
            string raceSexIdString = raceSexId.ToString("0000");

            string skeletonPath = $"chara/human/c{raceSexIdString}/skeleton/base/b0001/skl_c{raceSexIdString}b0001.sklb";

            var replacement = CreateFileReplacement(skeletonPath, objIdx, true);
            AddFileReplacement(replacements, replacement);
            
            //DebugPrint(replacement, objectKind, "SKLB", 0);
        }

        private void AddFileReplacement(List<FileReplacement> replacements, FileReplacement newReplacement)
        {
            if (!newReplacement.HasFileReplacement)
            {
                Logger.Debug($"Replacement for {newReplacement.ResolvedPath} does not have a file replacement, skipping");
                foreach(var path in newReplacement.GamePaths)
                {
                    Logger.Debug(path);
                }
                return;
            }

            var existingReplacement = replacements.SingleOrDefault(f => string.Equals(f.ResolvedPath, newReplacement.ResolvedPath, System.StringComparison.OrdinalIgnoreCase));
            if (existingReplacement != null)
            {
                Logger.Debug($"Added replacement for existing path {existingReplacement.ResolvedPath}");
                existingReplacement.GamePaths.AddRange(newReplacement.GamePaths.Where(e => !existingReplacement.GamePaths.Contains(e, System.StringComparer.OrdinalIgnoreCase)));
            }
            else
            {
                Logger.Debug($"Added new replacement {newReplacement.ResolvedPath}");
                replacements.Add(newReplacement);
            }
        }

        private FileReplacement CreateFileReplacement(string path, int objIdx, bool doNotReverseResolve = false)
        {
            var fileReplacement = new FileReplacement(Plugin);

            if (!doNotReverseResolve)
            {
                fileReplacement.ReverseResolvePathObject(path, objIdx);
            }
            else
            {
                fileReplacement.ResolvePathObject(path, objIdx);
            }

            Logger.Debug($"Created file replacement for resolved path {fileReplacement.ResolvedPath}, hash {fileReplacement.Hash}, gamepath {fileReplacement.GamePaths[0]}");
            return fileReplacement;
        }
    }
}
