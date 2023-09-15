using System;
using Godot;

namespace UnityEngine;

public class GodotUnityLogger : IDebug
{
    public void Assert(bool assertion)
    {
        if (!assertion)
        {
            throw new ApplicationException("Assert failed");
        }
    }

    public void Log(string info)
    {
        GD.Print(info);
    }

    public void Log(string info, params object[] args)
    {
        GD.Print(info, args);
    }

    public void LogWarning(string warning)
    {
        GD.Print(warning);
    }

    public void LogWarning(string warning, params object[] args)
    {
        GD.Print(warning, args);
    }

    public void LogException(Exception ex)
    {
        GD.PrintErr(ex);
    }

    public void LogError(Exception ex)
    {
        GD.PrintErr(ex);
    }

    public void LogError(string error)
    {
        GD.PrintErr(error);
    }

    public void LogError(string error, params object[] args)
    {
        GD.PrintErr(error);
    }

    public void LogFormat(string format, params object[] args)
    {
        GD.Print(string.Format(format, args));
    }
}