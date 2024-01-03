using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using UnityEditor;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
[CreateAssetMenu(fileName = "Game Settings", menuName = "Scriptable Objects/Game Settings")]
[Serializable]
public class GameSettings : MyScriptableObject
{
    public const int minTrackLibrarySize = 5;
    public const int minCollectibleTracks =5;
    // I want to display the constant values as a read only property.
    [Header("Audio Settings")]
    [SerializeField]
    [ReadOnly]
    private int minimumLibrarySize = minTrackLibrarySize;
    [SerializeField]
    [ReadOnly]
    private int minimumCollectibleTracks = minCollectibleTracks;

    // we can keep a list of  the default songs, but they will not be loaded into memory until we confirm the configuration file does not specify other songs.
    // this way we avoid loading unused audio clips into memory.
    public string[] initTrackLibrary = new string[minTrackLibrarySize]; // the tracks here will be loaded onto the walkman at the start of them game 
    public string[] collectibleTracks = new string[minCollectibleTracks]; // list of tracks the player will be able to pick up throughout the game
    public string startingTrack;

    [Header("Player Settings")]
    public string playerName = "Reut";
    public string playerId { get { return GetPlayerID(); } }
    public int gameSessionIndex {get { return GetGameSessionIndex(); } }

    [Header("Build Settings")]
    [HideInInspector]
    public string configurationID = "Default";
    [HideInInspector]
    public bool configFileLoaded = false;
    [SerializeField]
    public string buildID = "1";

    [Header("Asset Settings")]
    // keep an adressable reference to all possible tracks so we can just grab the needed ones at loading time
    public List<TrackDataReference> trackDataReferences;
    // so that I can also know the order of insertion of the references
    public List<string> trackDataKeys;
    int GetGameSessionIndex()
    {
        return PlayerPrefs.GetInt("GameSessionIndex");
    }
    public void IncreaseGameSessionIndex()
    {
        PlayerPrefs.SetInt("GameSessionIndex", gameSessionIndex+1);
    }
    
    string GetPlayerID()
    {
        return SystemInfo.deviceUniqueIdentifier.ToString();
    }
}