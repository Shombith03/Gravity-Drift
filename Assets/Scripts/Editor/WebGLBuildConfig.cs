#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Configures WebGL build settings for itch.io embedding.
/// Resolution: 960x540 with auto-scale.
/// Template: ItchIO custom template.
/// Run via menu: Build > Configure WebGL for itch.io
/// </summary>
public static class WebGLBuildConfig
{
    [MenuItem("Build/Configure WebGL for itch.io")]
    public static void ConfigureWebGL()
    {
        // Resolution
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;

        // Template
        PlayerSettings.WebGL.template = "PROJECT:ItchIO";

        // Compression (Gzip for itch.io compatibility)
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;

        // Exception handling (for smaller builds)
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        // Disable decompression fallback for smaller download
        PlayerSettings.WebGL.decompressionFallback = false;

        // Linear color space
        PlayerSettings.colorSpace = UnityEngine.ColorSpace.Linear;

        UnityEngine.Debug.Log("[WebGLBuildConfig] WebGL configured for itch.io: 960x540, ItchIO template, Gzip compression.");
    }
}
#endif
