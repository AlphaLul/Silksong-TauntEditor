using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using ProbabilityAudioClip = RandomAudioClipTable.ProbabilityAudioClip;

namespace TauntEditor;

[BepInAutoPlugin(ID, NAME, VERSION)]
public partial class TauntEditorPlugin : BaseUnityPlugin
{
    private const string ID = "alphalul.TauntEditor";
    private const string NAME = "Taunt Editor";
    private const string VERSION = "1.0.0";

    public static TauntEditorPlugin Instance { get; private set; }
    private Harmony harmony = new(ID);

    private string clipsPath;
    private string archivedClipsPath;
    private ConfigEntry<string> clipsFolderName;
    private ConfigEntry<string> archivedClipsSubfolderName;
    private ConfigEntry<bool> disableMod;

    private const string targetBundleName = "147a11169aba20e0cec9ba43c2035542";
    private const string tauntVoiceTablePath = "Assets/Audio/Voices/Hornet_Silksong/Taunt Hornet Voice.asset";
    private const string tauntSeriouslyVoiceTablePath = "Assets/Audio/Voices/Hornet_Silksong/Taunt Seriously Hornet Voice.asset";
    private RandomAudioClipTable tauntVoiceTable;
    private RandomAudioClipTable tauntSeriouslyVoiceTable;
    private ConfigEntry<bool> configRefreshOnSaveQuit;
    
    private ConfigEntry<bool> configIncludeVanillaClips;
    private ProbabilityAudioClip[] vanillaClips;
    
    private bool initialized;
    private List<ProbabilityAudioClip> moddedClips = new();
    
    private void Awake()
    {
        if (Instance == null) Instance = this;

        clipsFolderName = Config.Bind(
            "Folders",
            "ClipsFolderName",
            "Clips",
            "Name of folder that will be searched for clips");
        archivedClipsSubfolderName = Config.Bind(
            "Folders",
            "ArchivedClipsSubfolderName",
            "Archive",
            "Name of folder that you wish to exclude from the clips search. Allows you to remove clips without deleting them");
        disableMod = Config.Bind(
            "Toggles",
            "DisableMod",
            false,
            "Whether or not to disable the mod's functionality. " + 
            "True allows you to return to vanilla functionality without moving or deleting clips");
        configRefreshOnSaveQuit = Config.Bind(
            "Toggles",
            "RefreshOnSaveQuit",
            true,
            "Whether or not to refresh the clips list after returning to the title screen. " +
            "True allows you to add or remove clips while the game is running");
        configIncludeVanillaClips = Config.Bind(
            "Toggles",
            "IncludeVanillaClips",
            false,
            "Whether or not to include Silksong's vanilla taunt clips");

        clipsPath = Path.Combine(Path.GetDirectoryName(Info.Location), clipsFolderName.Value);
        archivedClipsPath = Path.Combine(clipsPath, archivedClipsSubfolderName.Value);

        Directory.CreateDirectory(clipsPath);
        Directory.CreateDirectory(archivedClipsPath);

        if (disableMod.Value)
        {
            Logger.LogWarning("TauntEditor mod disabled");
            return;
        }
        harmony.PatchAll();
    }

    public void ExecuteTauntEditor()
    {
        if (disableMod.Value) return;
        StartCoroutine(ExecuteTauntEditorRoutine());
    }

    private IEnumerator ExecuteTauntEditorRoutine()
    {
        CacheTauntVoiceTable();
        if (!initialized) yield break;
        
        moddedClips.Clear();
        yield return LoadClipsRoutine();
        if (moddedClips.Count == 0)
        {
            tauntVoiceTable.clips = vanillaClips;
            tauntSeriouslyVoiceTable.clips = vanillaClips;
            Logger.LogInfo($"TauntEditor mod applied vanilla clips to taunt voice tables");
            yield break;
        }
        
        tauntVoiceTable.clips = moddedClips.ToArray();
        tauntSeriouslyVoiceTable.clips = moddedClips.ToArray();
        Logger.LogInfo($"TauntEditor mod applied {tauntVoiceTable.clips.Length} clips to taunt voice tables");
    }
    
    private void CacheTauntVoiceTable()
    {
        if (initialized) return;
        IEnumerable<AssetBundle> loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
        foreach (AssetBundle bundle in loadedBundles)
        {
            if (!bundle.name.Contains(targetBundleName)) continue;
            tauntVoiceTable = bundle.LoadAsset<RandomAudioClipTable>(tauntVoiceTablePath);
            tauntSeriouslyVoiceTable = bundle.LoadAsset<RandomAudioClipTable>(tauntSeriouslyVoiceTablePath);
            vanillaClips = tauntVoiceTable.clips;
            initialized = true;
        }
        if (initialized)
            Logger.LogInfo("TauntEditor started");
        else 
            Logger.LogError($"TauntEditor was unable to locate asset at path {tauntVoiceTablePath}");
        
        if (!configRefreshOnSaveQuit.Value) harmony.UnpatchSelf();
    }
    
    private IEnumerator LoadClipsRoutine()
    {
        List<AudioClip> streamedClips = new();
        string[] wavFiles = Directory.GetFiles(clipsPath, "*.wav", SearchOption.AllDirectories);
        if (wavFiles.Length == 0)
        {
            Logger.LogWarning("No wav files found in TauntEditor clips folder");
            yield break;
        }

        foreach (string wavFile in wavFiles)
        {
            if (Path.GetDirectoryName(wavFile).Equals(archivedClipsPath, StringComparison.OrdinalIgnoreCase)) continue;
            
            string uri = new Uri(wavFile).AbsoluteUri;
            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV);
            yield return req.SendWebRequest();
            
            AudioClip streamedClip = DownloadHandlerAudioClip.GetContent(req);
            streamedClip.name = Path.GetFileNameWithoutExtension(wavFile);
            streamedClip.LoadAudioData();

            streamedClips.Add(streamedClip);
        }
        ConvertClipsToProbabilityClips(streamedClips);
    }

    private void ConvertClipsToProbabilityClips(List<AudioClip> clips)
    {
        if (configIncludeVanillaClips.Value)
            moddedClips.AddRange(vanillaClips);
        foreach (AudioClip clip in clips)
        {
            moddedClips.Add(new ProbabilityAudioClip
            {
                Clip = clip,
                Probability = 1f
            });
            Logger.LogInfo($"Loaded {clip.name}.wav");
        }
        
        Logger.LogInfo($"TauntEditor mod loaded {moddedClips.Count} clips");
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.Start))]
class GameManager_Start_Patch
{
    [HarmonyPostfix]
    static void Start_Postfix(GameManager __instance)
    {
        TauntEditorPlugin.Instance.ExecuteTauntEditor();
    }
}