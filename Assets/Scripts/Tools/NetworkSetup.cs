using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Diagnostics;
using UnityEditor;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;

#endif

#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif

public class NetworkSetup : MonoBehaviour
{
    private int playerPrefabIndex = 0;
    private bool isServer = false;
    void Start()
    {
        // Parse command line arguments
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--server")
            {
                // --server found, this should be a server application
                isServer = true;
            }
        }
        if (isServer)
            StartCoroutine(StartAsServerCR());
    }

    IEnumerator StartAsServerCR()
    {
        SetWindowTitle("Starting up as server...");
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;
        // Wait a frame for setups to be done
        yield return null;
        if (networkManager.StartServer())
        {
            SetWindowTitle("Server");
            UnityEngine.Debug.Log($"Serving on port {transport.ConnectionData.Port}...");

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        }
        else
        {
            SetWindowTitle("Failed to connect as server...");
            UnityEngine.Debug.LogError($"Failed to serve on port {transport.ConnectionData.Port}...");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // if (!NetworkManager.Singleton.IsServer) return;

        // UnityEngine.Debug.Log($"Player {clientId} connected, prefab index = {playerPrefabIndex}!");

        // // Check a free spot for this player
        // var spawnPos = Vector3.zero;
        // var currentPlayers = FindObjectsByType<Wyzard>(FindObjectsSortMode.None);
        // foreach (var playerSpawnLocation in spawnPoints)
        // {
        //     var closestDist = float.MaxValue;
        //     foreach (var player in currentPlayers)
        //     {
        //         float d = Vector3.Distance(player.transform.position, playerSpawnLocation.position);
        //         closestDist = Mathf.Min(closestDist, d);
        //     }
        //     if (closestDist > 20)
        //     {
        //         spawnPos = playerSpawnLocation.position;
        //         break;
        //     }
        // }
        // // Spawn player object
        // var spawnedObject = Instantiate(playerPrefabs[playerPrefabIndex], spawnPos, Quaternion.identity);
        // var prefabNetworkObject = spawnedObject.GetComponent<NetworkObject>();
        // prefabNetworkObject.SpawnAsPlayerObject(clientId, true);
        // prefabNetworkObject.ChangeOwnership(clientId);
        // playerPrefabIndex = (playerPrefabIndex + 1) % playerPrefabs.Length;

    }

    private void OnClientDisconnected(ulong clientId)
    {
        UnityEngine.Debug.Log($"Player {clientId} disconnected!");
    }

    IEnumerator StartAsClientCR()
    {
        SetWindowTitle("Starting up as client...");
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;
        // Wait a frame for setups to be done
        yield return null;
        if (networkManager.StartClient())
        {
            SetWindowTitle("Client");
            UnityEngine.Debug.Log($"Connecting on port {transport.ConnectionData.Port}...");
        }
        else
        {
            SetWindowTitle("Failed to connect as client...");
            UnityEngine.Debug.LogError($"Failed to connect on port {transport.ConnectionData.Port}...");
        }
    }

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowText(IntPtr hWnd, string lpString);
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    static extern IntPtr EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    // Delegate to filter windows
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private static IntPtr FindWindowByProcessId(uint processId)
    {
        IntPtr windowHandle = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            uint windowProcessId;
            GetWindowThreadProcessId(hWnd, out windowProcessId);
            if (windowProcessId == processId)
            {
                windowHandle = hWnd;
                return false; // Found the window, stop enumerating
            }
            return true; // Continue enumerating
        }, IntPtr.Zero);
        return windowHandle;
    }
    static void SetWindowTitle(string title)
    {
#if !UNITY_EDITOR
        uint processId = (uint)Process.GetCurrentProcess().Id;
        IntPtr hWnd = FindWindowByProcessId(processId);
        if (hWnd != IntPtr.Zero)
        {
            SetWindowText(hWnd, title);
        }
#endif
    }
#else
    static void SetWindowTitle(string title)
    {
    }
#endif

#if UNITY_EDITOR
    [MenuItem("Tools/Build Windows (x64)", priority = 0)]
    public static bool BuildGame()
    {
        // Specify build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = EditorBuildSettings.scenes
          .Where(s => s.enabled)
          .Select(s => s.path)
          .ToArray();
        buildPlayerOptions.locationPathName = Path.Combine("Builds", "NetpetBattlerz.exe");
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;
        // Perform the build
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        // Output the result of the build
        UnityEngine.Debug.Log($"Build ended with status: {report.summary.result}");
        // Additional log on the build, looking at report.summary
        return report.summary.result == BuildResult.Succeeded;
    }

    private static void Run(string path, string args)
    {
        // Start a new process
        Process process = new Process();
        // Configure the process using the StartInfo properties
        process.StartInfo.FileName = path;
        process.StartInfo.Arguments = args;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal; // Choose the window style: Hidden, Minimized, Maximized, Normal
        process.StartInfo.RedirectStandardOutput = false; // Set to true to redirect the output (so you can read it in Unity)
        process.StartInfo.UseShellExecute = true; // Set to false if you want to redirect the output
                                                  // Run the process
        process.Start();
    }

    [MenuItem("Tools/Build and Launch (Server)", priority = 10)]
    public static void BuildAndLaunch1()
    {
        CloseAll();
        if (BuildGame())
        {
            Launch1();
        }
    }
    [MenuItem("Tools/Build and Launch (Server + Client)", priority = 20)]
    public static void BuildAndLaunch2()
    {
        CloseAll();
        if (BuildGame())
        {
            Launch2();
        }
    }
    [MenuItem("Tools/Build and Launch (Client + Client)", priority = 30)]
    public static void BuildAndLaunch3()
    {
        CloseAll();
        if (BuildGame())
        {
            Launch3();
        }
    }
    [MenuItem("Tools/Launch (Server) _F11", priority = 40)]
    public static void Launch1()
    {
        Run("Builds\\NetpetBattlerz.exe", "--server");
    }
    [MenuItem("Tools/Launch (Server + Client)", priority = 50)]
    public static void Launch2()
    {
        Run("Builds\\NetpetBattlerz.exe", "--server");
        Run("Builds\\NetpetBattlerz.exe", "");
    }
    [MenuItem("Tools/Launch (Client + Client)", priority = 60)]
    public static void Launch3()
    {
        Run("Builds\\NetpetBattlerz.exe", "");
        Run("Builds\\NetpetBattlerz.exe", "");
    }
    [MenuItem("Tools/Close All", priority = 100)]
    public static void CloseAll()
    {
        // Get all processes with the specified name
        Process[] processes = Process.GetProcessesByName("NetpetBattlerz");
        foreach (var process in processes)
        {
            try
            {
                // Close the process
                process.Kill();
                // Wait for the process to exit
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                // Handle exceptions, if any
                // This could occur if the process has already exited or you don't have permission to kill it
                UnityEngine.Debug.LogWarning($"Error trying to kill process {process.ProcessName}: {ex.Message}");
            }
        }
    }
#endif

}
