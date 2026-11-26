using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MusicalFountainsMcpSetup
{
    static MusicalFountainsMcpSetup()
    {
        Environment.SetEnvironmentVariable("UV_CACHE_DIR", "D:/Unity/cache/uv");
        Environment.SetEnvironmentVariable("UV_PYTHON_INSTALL_DIR", "D:/Unity/tools/python");
        Environment.SetEnvironmentVariable("UV_TOOL_DIR", "D:/Unity/tools/uv-tools");
        Environment.SetEnvironmentVariable("UV_TOOL_BIN_DIR", "D:/Unity/tools/uv-bin");
        Environment.SetEnvironmentVariable("UNITY_MCP_LOG_DIR", "D:/Unity/build-work/musical-fountains/mcp-logs");
        EditorPrefs.SetString("MCPForUnity.UvxPath", "D:/Unity/tools/uv/uvx.exe");
        EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
        EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
    }

    public static void VerifyImport()
    {
        var path = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (!File.ReadAllText(path).Contains("com.coplaydev.unity-mcp"))
            throw new InvalidOperationException("Unity MCP is missing from the project manifest");
        Directory.CreateDirectory("D:/Unity/build-work/musical-fountains");
        File.WriteAllText("D:/Unity/build-work/musical-fountains/mcp-import-result.txt", "Unity MCP imported successfully in " + Application.unityVersion);
        Debug.Log("[MusicalFountainsMR] Unity MCP imported and editor auto-connect configured.");
    }
}