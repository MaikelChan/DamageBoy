﻿using DamageBoy.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace DamageBoy.Graphics;

abstract class BaseRenderer : IDisposable
{
    public PipelineState PipelineState { get; }

    readonly protected GlobalUniforms globalUniforms;

    public BaseRenderer()
    {
        // This is required for AOT before any GL calls
        GL.LoadBindings(new GLFWBindingsContext());

        Utils.Log(LogType.Info, "Vendor: " + GL.GetString(StringName.Vendor));
        Utils.Log(LogType.Info, "Renderer: " + GL.GetString(StringName.Renderer));
        Utils.Log(LogType.Info, "Version: " + GL.GetString(StringName.Version));
        Utils.Log(LogType.Info, "Shading Language Version: " + GL.GetString(StringName.ShadingLanguageVersion));

        GetGLInfo();

#if DEBUG
        InitializeDebugMessages();
#endif

        PipelineState = new PipelineState(this);
        globalUniforms = new GlobalUniforms();
    }

    public virtual void Dispose()
    {

    }

    public abstract void Render(double deltaTime);
    public abstract void Resize(int width, int height);
    public abstract void ScreenUpdate(byte[] vram);

    #region GL Info

    public bool KHRDebugExtension { get; private set; }
    public int MaxTextureImageUnits { get; private set; }

    void GetGLInfo()
    {
        int extensionCount = GL.GetInteger(GetPName.NumExtensions);

        //Utils.Log("Extensions: ");

        for (int e = 0; e < extensionCount; e++)
        {
            string extensionName = GL.GetString(StringNameIndexed.Extensions, e);
            if (extensionName == "GL_KHR_debug") KHRDebugExtension = true;
            //Utils.Log($"{e:000}: {extensionName}");
        }

        MaxTextureImageUnits = GL.GetInteger(GetPName.MaxTextureImageUnits);
    }

    #endregion

    #region Debug

#if DEBUG

    DebugProc DebugProcCallback;

    void InitializeDebugMessages()
    {
        if (!KHRDebugExtension) return;

        GL.Enable(EnableCap.DebugOutput);
        DebugProcCallback = DebugMessage;
        GL.DebugMessageCallback(DebugProcCallback, IntPtr.Zero);
    }

    void DebugMessage(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
    {
        if (severity == DebugSeverity.DebugSeverityNotification) return;

        string msg = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message, length);

        LogType logType;

        switch (severity)
        {
            case DebugSeverity.DebugSeverityLow:
            case DebugSeverity.DebugSeverityMedium:
                logType = LogType.Warning;
                break;
            case DebugSeverity.DebugSeverityHigh:
                logType = LogType.Error;
                break;
            default:
                logType = LogType.Info;
                break;
        }

        Utils.Log(logType, $"{severity} {type} {msg}");
    }

#endif

    #endregion
}