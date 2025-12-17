// <copyright file="MouseFcInputHandler.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Runtime.Utils;
using System.Numerics;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Input;

/// <summary>
/// <see cref="IFcInputHandler"/> implemented using the mouse.
/// </summary>
public sealed class MouseFcInputHandler : IFcInputHandler
{
    private const int MouseButtonCount = 3;
    private const float SwipeStartDelta1 = 20f;
    private const float SwipeStartDelta2 = 40f;
    private const int SwipeRepeat = 15;

    private readonly FcCamera _camera;

    private readonly bool[] _mouseButtons = new bool[MouseButtonCount];
    private readonly bool[] _mouseButtonsLast = new bool[MouseButtonCount];

    private long _currentFrame = -1;

    private Vector2 _mousePos;

    private bool _anyButtonDown;
    private Vector2 _buttonDownPos;

    private bool _swiping;
    private long _swipeStartFrame = -1;
    private Vector2 _swipeStartPosWorld;
    private Vector2 _swipeSnap;

    /// <summary>
    /// Initializes a new instance of the <see cref="MouseFcInputHandler"/> class.
    /// </summary>
    /// <param name="camera">A <see cref="FcCamera"/> instance.</param>
    public MouseFcInputHandler(FcCamera camera)
    {
        ThrowIfNull(camera);

        _camera = camera;
    }

    private Vector2 DragDelta => _mousePos - _buttonDownPos;

    /// <summary>
    /// Updates the state of the <see cref="MouseFcInputHandler"/>, should be called every frame..
    /// </summary>
    /// <param name="mousePosition">The current position of the mouse.</param>
    /// <param name="mouseButtons">The current state of the buttons.</param>
    /// <param name="scrollDelta">The current scroll wheel delta.</param>
    /// <param name="screenInfo">The current ScreenInfo.</param>
    public void Update(Vector2 mousePosition, ReadOnlySpan<bool> mouseButtons, float scrollDelta, ScreenInfo screenInfo)
    {
        _currentFrame++;

        _mousePos = mousePosition;

        _mouseButtons.AsSpan().CopyTo(_mouseButtonsLast);
        mouseButtons[..Math.Min(mouseButtons.Length, 3)].CopyTo(_mouseButtons);
        if (mouseButtons.Length < 3)
        {
            _mouseButtons.AsSpan(mouseButtons.Length).Clear();
        }

        bool anyButtonDown = _mouseButtons.AsSpan().AnyTrue();
        if (!anyButtonDown && _anyButtonDown)
        {
            StopDrag();
        }
        else if (anyButtonDown && !_anyButtonDown)
        {
            _anyButtonDown = true;
            _buttonDownPos = _mousePos;
        }

        if (!_swiping && _anyButtonDown)
        {
            _swiping = DragDelta.LengthSquared() > SwipeStartDelta1 * SwipeStartDelta1;
        }

        if (_swiping)
        {
            if (_swipeStartFrame == -1 && DragDelta.LengthSquared() > SwipeStartDelta2 * SwipeStartDelta2)
            {
                _swipeStartFrame = _currentFrame;
                var (origin, far) = _camera.ScreenToWorld(_buttonDownPos, screenInfo);
                var dir = Vector3.Normalize(far - origin);
                if (dir.Y == 0f)
                {
                    _swipeStartPosWorld = new Vector2(origin.X, origin.Z);
                }
                else
                {
                    float t = -origin.Y / dir.Y;
                    Vector3 hitPoint = origin + (dir * t);

                    _swipeStartPosWorld = new Vector2(hitPoint.X, hitPoint.Z);
                }
            }

            if (_swipeStartFrame != -1)
            {
                var (mouseNear, mouseFar) = _camera.ScreenToWorld(_mousePos, screenInfo);
                var mouseDir = Vector3.Normalize(mouseFar - mouseNear);
                Vector3 swipeWorldDir;
                if (mouseDir.Y == 0f)
                {
                    swipeWorldDir = Vector3.Zero;
                }
                else
                {
                    float t = -mouseNear.Y / mouseDir.Y;
                    var swipeWorldPos = mouseNear + (mouseDir * t);
                    swipeWorldDir = new Vector3(swipeWorldPos.X - _swipeStartPosWorld.X, 0f, swipeWorldPos.Z - _swipeStartPosWorld.Y);
                }

                _swipeSnap = Vector2.Zero;
                if (swipeWorldDir.X == 0f || MathF.Abs(swipeWorldDir.X) <= MathF.Abs(swipeWorldDir.Z))
                {
                    _swipeSnap.Y = swipeWorldDir.Z > 0f ? 1f : -1f;
                }
                else
                {
                    _swipeSnap.X = swipeWorldDir.X > 0f ? 1f : -1f;
                }
            }
        }

        if (scrollDelta != 0f)
        {
            // TODO: simulate 2 touches moving away from/towards each other
        }
    }

    /// <summary>
    /// Resets the state of the <see cref="MouseFcInputHandler"/>.
    /// </summary>
    public void Reset()
    {
        _currentFrame = -1;
        _mousePos = default;
        _mouseButtons.AsSpan().Clear();
        _mouseButtonsLast.AsSpan().Clear();
        StopDrag();
    }

    /// <inheritdoc/>
    public bool TryGetTouch(TouchState state, int fingerIndex, out Vector2 touchPos)
    {
        // left click - 1st finger, others - 1st and 2nd fingers
        bool stateCurrent;
        bool stateLast;
        switch (fingerIndex)
        {
            case 0:
                stateCurrent = _mouseButtons.AsSpan().AnyTrue();
                stateLast = _mouseButtonsLast.AsSpan().AnyTrue();
                break;
            case 1:
                stateCurrent = _mouseButtons.AsSpan(1).AnyTrue();
                stateLast = _mouseButtonsLast.AsSpan(1).AnyTrue();
                break;
            default: // 3rd finger cannot be triggered, unless scrolling (not yet implemented)
                touchPos = default;
                return false;
        }

        touchPos = new Vector2(MathF.Round(_mousePos.X), MathF.Round(_mousePos.Y));
        switch (state)
        {
            case TouchState.Touching:
                if (stateCurrent)
                {
                    return true;
                }

                break;
            case TouchState.Begins:
                if (stateCurrent && !stateLast)
                {
                    return true;
                }

                break;
            case TouchState.Ends:
                if (!stateCurrent && stateLast)
                {
                    return true;
                }

                break;
        }

        touchPos = default;
        return false;
    }

    /// <inheritdoc/>
    public bool TryGetSwipe(out Vector3 direction)
    {
        if (_swipeStartFrame == -1 || (_currentFrame - _swipeStartFrame) % SwipeRepeat != 0)
        {
            direction = default;
            return false;
        }

        direction = new Vector3(_swipeSnap.X, 0f, _swipeSnap.Y);
        return true;
    }

    private void StopDrag()
    {
        _anyButtonDown = false;
        _buttonDownPos = default;

        _swiping = false;
        _swipeStartFrame = -1;
        _swipeStartPosWorld = default;
        _swipeSnap = default;
    }
}
