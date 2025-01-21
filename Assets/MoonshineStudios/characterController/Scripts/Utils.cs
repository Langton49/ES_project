using UnityEngine;
using System.IO;
using System;

public static class Utils
{
    // Config handling
    private static Config _config;
    public static string apiKey
    {
        get
        {
            if (_config == null)
            {
                LoadConfig();
            }
            return _config?.ApiKey;
        }
    }

    [Serializable]
    private class Config
    {
        public string ApiKey;
    }

    private static void LoadConfig()
    {
        // This path will work both in editor and build
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
        if (File.Exists(configPath))
        {
            string jsonContent = File.ReadAllText(configPath);
            _config = JsonUtility.FromJson<Config>(jsonContent);
        }
        else
        {
            Debug.LogError($"Config file not found at {configPath}!");
        }
    }

    // General constants
    public const string gamePrefix = "game_";

    // Server constants
    public const string identityPoolId = "us-east-1:1de43a68-b7d8-4d64-81a7-8d4e803bc0ae";
    public const string lambdaFunction = "MultiplayerFunction";

    // Game constants
    public const float timeOutAfter = 20f;
    public const float sendPlayerDataInterval = 1f;
    public const float runSpeed = 7f;
    public const float walkBack = -2f;
    public const float rotateSpeed = 50f;
    public const int hitLifeValue = 10;

    // Player constants
    public const int statusNoMove = 0;
    public const int statusMoveRun = 10;
    public const int statusMoveBack = 11;

    public const int statusNoRotate = 0;
    public const int statusRotateLeft = 1;
    public const int statusRotateRight = 2;

    public const int statusNoAction = 0;
    public const int statusActionRun = 1;
    public const int statusActionWalkBack = 2;
    public const int statusActionPunch = 10;
    public const int statusActionFlyingPunch = 11;
    public const int statusActionHitToHead = 20;
    public const int statusActionDie = 30;
}