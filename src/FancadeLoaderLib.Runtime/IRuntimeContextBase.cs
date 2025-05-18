using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace FancadeLoaderLib.Runtime;

public interface IRuntimeContextBase
{
    float2 ScreenSize { get; }

    float3 Accelerometer { get; }

    bool TakingBoxArt { get; }

    // **************************************** Game ****************************************
    void Win(int delay);

    void Lose(int delay);

    void SetScore(float? score, float? coins, Ranking ranking);

    void SetLight(float3? position, Quaternion? rotation);

    void MenuItem(VariableReference? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease);

    // **************************************** Sound ****************************************
    float PlaySound(float volume, float pitch, FcSound sound);

    void StopSound(float channel);

    void AdjustVolumePitch(float channel, float? volume, float? pitch);

    // **************************************** Control ****************************************
    bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos);

    bool TryGetSwipe(out float3 direction);

    bool GetButtonPressed(ButtonType type);

    float3 GetJoystickDirection(JoystickType type);

    // **************************************** Values ****************************************
    void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, ushort3 inspectBlockPosition);
}
