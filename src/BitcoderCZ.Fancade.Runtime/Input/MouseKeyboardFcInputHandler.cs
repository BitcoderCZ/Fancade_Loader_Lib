// <copyright file="MouseKeyboardFcInputHandler.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Runtime.Exceptions;
using BitcoderCZ.Fancade.Runtime.Utils;
using System.Numerics;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Input;

/// <summary>
/// <see cref="IFcInputHandler"/> implemented using the mouse.
/// </summary>
public sealed class MouseKeyboardFcInputHandler : IFcInputHandler
{
    private const int MouseButtonCount = 3;
    private const float SwipeStartDelta1 = 20f;
    private const float SwipeStartDelta2 = 40f;
    private const int SwipeRepeat = 15;

    private static readonly FcKeyCode[] KeysJLKI = [FcKeyCode.Left2, FcKeyCode.Right2, FcKeyCode.Down2, FcKeyCode.Up2];
    private static readonly FcKeyCode[] KeysThatSimulateTouch = [FcKeyCode.Space, FcKeyCode.Action1, FcKeyCode.Action2]; // todo: wsad and arrow keys

    private readonly FcCamera _camera;

    private readonly bool[] _mouseButtons = new bool[MouseButtonCount];
    private readonly bool[] _mouseButtonsLast = new bool[MouseButtonCount];

    private readonly FcKeys _keys = new();
    private readonly List<FcKeyCode> _pressedKeys = new(3);

    private ScreenInfo _screenInfo;

    private int _nextDirectionButtonCount;
    private int _nextNormalButtonCount;
    private int _currentDirectionButtonCount;
    private int _currentNormalButtonCount;
    private int _currentDirectionButtonIndex;
    private int _currentNormalButtonIndex;

    private long _currentFrame = -1;

    private Vector2 _mousePos;

    private bool _anyButtonDown;
    private Vector2 _buttonDownPos;

    private bool _swiping;
    private long _swipeStartFrame = -1;
    private Vector2 _swipeStartPosWorld;
    private Vector2 _swipeSnap;

    /// <summary>
    /// Initializes a new instance of the <see cref="MouseKeyboardFcInputHandler"/> class.
    /// </summary>
    /// <param name="camera">A <see cref="FcCamera"/> instance.</param>
    public MouseKeyboardFcInputHandler(FcCamera camera)
    {
        ThrowIfNull(camera);

        _camera = camera;
    }

    /// <inheritdoc/>
    public int DirectionButtonCount => _currentDirectionButtonCount;

    /// <inheritdoc/>
    public int NormalButtonCount => _currentNormalButtonCount;

    private Vector2 DragDelta => _mousePos - _buttonDownPos;

    /// <summary>
    /// Updates the state of the <see cref="MouseKeyboardFcInputHandler"/>, should be called every frame..
    /// </summary>
    /// <param name="mousePosition">The current position of the mouse.</param>
    /// <param name="mouseButtons">The current state of the buttons.</param>
    /// <param name="scrollDelta">The current scroll wheel delta.</param>
    /// <param name="keys">The current keyboard state.</param>
    /// <param name="screenInfo">The current ScreenInfo.</param>
    public void Update(Vector2 mousePosition, ReadOnlySpan<bool> mouseButtons, float scrollDelta, FcKeys keys, ScreenInfo screenInfo)
    {
        _currentFrame++;

        _screenInfo = screenInfo;

        // init mouse
        _mousePos = mousePosition;

        _mouseButtons.AsSpan().CopyTo(_mouseButtonsLast);
        mouseButtons[..Math.Min(mouseButtons.Length, 3)].CopyTo(_mouseButtons);
        if (mouseButtons.Length < 3)
        {
            _mouseButtons.AsSpan(mouseButtons.Length).Clear();
        }

        // init keyboard
        keys.CopyTo(_keys);

        _currentDirectionButtonIndex = 0;
        _currentNormalButtonIndex = 0;

        _currentDirectionButtonCount = _nextDirectionButtonCount;
        _currentNormalButtonCount = _nextNormalButtonCount;
        _nextDirectionButtonCount = 0;
        _nextNormalButtonCount = 0;

        for (int i = 0; i < _pressedKeys.Count; i++)
        {
            if (!_keys[_pressedKeys[i]])
            {
                _pressedKeys.RemoveAt(i);
                i--;
            }
        }

        IEnumerable<FcKeyCode> keysToDetect;
        if (_currentDirectionButtonCount + _currentNormalButtonCount is 0)
        {
            keysToDetect = [.. KeysJLKI, .. KeysThatSimulateTouch];
        }
        else
        {
            keysToDetect = KeysJLKI;
        }

        foreach (var key in keysToDetect)
        {
            if (_keys[key] && !_pressedKeys.Contains(key))
            {
                _pressedKeys.Add(key);
            }

            if (_pressedKeys.Count >= IFcInputHandler.MaxTouchCount)
            {
                break;
            }
        }

        // mouse stuff
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

        // swipe detection
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

        // scrolling
        if (scrollDelta != 0f)
        {
            // TODO: simulate 2 touches moving away from/towards each other
        }
    }

    /// <summary>
    /// Resets the state of the <see cref="MouseKeyboardFcInputHandler"/>.
    /// </summary>
    public void Reset()
    {
        _currentFrame = -1;
        _mousePos = default;
        _mouseButtons.AsSpan().Clear();
        _mouseButtonsLast.AsSpan().Clear();

        _nextDirectionButtonCount = 0;
        _nextNormalButtonCount = 0;
        _currentDirectionButtonCount = 0;
        _currentNormalButtonCount = 0;
        _currentDirectionButtonIndex = 0;
        _currentNormalButtonIndex = 0;

        _keys.Clear();
        _pressedKeys.Clear();

        StopDrag();
    }

    // TODO: keyboard stuff:
    // - override mouse touch
    // wsad - start at center, swipe in direction (plus a bit of offset) (only if no buttons active)
    // space - touch at center (only if no buttons active)
    // zx - touch at 0.25, 0.75 touch at center (only if no buttons active)
    // jlki - touch at 1/5, 2/5, 3/5, 4/5

    /// <inheritdoc/>
    public bool TryGetTouch(TouchState state, int fingerIndex, out Vector2 touchPos)
    {
        // wsad - start at center, swipe in direction (plus a bit of offset) (only if no buttons active)
        // space - touch at center (only if no buttons active)
        // zx - touch at 0.25, 0.75 touch at center (only if no buttons active)
        // jlki - touch at 1/5, 2/5, 3/5, 4/5
        foreach (var key in _pressedKeys)
        {
            float? xPos = key switch
            {
                FcKeyCode.Left2 => 0.2f,
                FcKeyCode.Right2 => 0.4f,
                FcKeyCode.Down2 => 0.6f,
                FcKeyCode.Up2 => 0.8f,
                _ => null,
            };

            if (xPos is null && _currentDirectionButtonCount + _currentNormalButtonCount == 0)
            {
                xPos = key switch
                {
                    FcKeyCode.Action1 => 0.25f,
                    FcKeyCode.Action2 => 0.4f,
                    FcKeyCode.Space => 0.6f,
                    _ => null,
                };
            }

            if (xPos is not null)
            {
                float yPos = 0.4994753f;
                touchPos = new Vector2(xPos.Value, yPos);
                return true;
            }
        }

        // left click
        // - touch 0 at pos
        // right click
        // - touch 0 at pos
        // - touch 1 at pos + (100, 100)
        // middle click
        // - weird rotation using all 3 touches, todo
        touchPos = new Vector2(MathF.Round(_mousePos.X), MathF.Round(_mousePos.Y));

        bool stateCurrent;
        bool stateLast;
        switch (fingerIndex)
        {
            case 0:
                stateCurrent = _mouseButtons[0] || _mouseButtons[1];
                stateLast = _mouseButtonsLast[0] || _mouseButtonsLast[1];
                break;
            case 1:
                stateCurrent = _mouseButtons[1];
                stateLast = _mouseButtonsLast[1];
                touchPos += new Vector2(100, 100);
                break;
            case 2: // TODO
            default:
                touchPos = default;
                return false;
        }

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

    /// <inheritdoc/>
    public bool GetButtonPressed(ButtonType type, EnvironmentPosition blockPosition)
    {
        if (_nextDirectionButtonCount + _nextNormalButtonCount >= IFcInputHandler.MaxButtonCount)
        {
            throw TooManyControlsException.CreateTooManyControls(blockPosition);
        }

        int buttonIndex = type switch
        {
            ButtonType.Direction => _currentDirectionButtonIndex,
            ButtonType.Button => _currentNormalButtonIndex,
            _ => default,
        };
        bool pressed = GetButtonPressed(buttonIndex, type);

        switch (type)
        {
            case ButtonType.Direction:
                _nextDirectionButtonCount++;
                _currentDirectionButtonIndex++;
                break;
            case ButtonType.Button:
                _nextNormalButtonCount++;
                _currentNormalButtonIndex++;
                break;
        }

        return pressed;
    }

    /// <inheritdoc/>
    public IEnumerable<IFcInputHandler.Button> GetActiveButtons()
    {
        float sideButtonBounds = _screenInfo.IsLandscape ? IFcInputHandler.ButtonBoundsLandscape : IFcInputHandler.ButtonBoundsPortrait;
        float sideButtonBoundsHalf = sideButtonBounds * 0.5f;

        var button1Type = _currentDirectionButtonCount >= 1 ? ButtonType.Direction : ButtonType.Button;
        var button2Type = _currentDirectionButtonCount >= 2 ? ButtonType.Direction : ButtonType.Button;
        var button3Type = _currentDirectionButtonCount >= 3 ? ButtonType.Direction : ButtonType.Button;
        var button4Type = _currentDirectionButtonCount >= 4 ? ButtonType.Direction : ButtonType.Button;

        int normalButtonIndex = 0;

        switch (_currentDirectionButtonCount + _currentNormalButtonCount)
        {
            case 0:
                break;
            case 1:
                yield return new IFcInputHandler.Button(
                    GetButtonPressed(0),
                    button1Type,
                    button1Type is ButtonType.Direction
                        ? IFcInputHandler.ButtonTexture.UpArrow
                        : IFcInputHandler.ButtonTexture.CircleZ,
                    1f - sideButtonBoundsHalf);
                break;
            case 2:
                yield return new IFcInputHandler.Button(
                    GetButtonPressed(0),
                    button1Type,
                    button1Type is ButtonType.Direction
                        ? (_currentDirectionButtonCount is 1 ? IFcInputHandler.ButtonTexture.UpArrow : IFcInputHandler.ButtonTexture.LeftArrow)
                        : IFcInputHandler.ButtonTexture.CircleZ,
                    sideButtonBoundsHalf);

                if (button1Type is ButtonType.Button)
                {
                    normalButtonIndex++;
                }

                yield return new IFcInputHandler.Button(
                    GetButtonPressed(1),
                    button2Type,
                    button2Type is ButtonType.Direction
                        ? IFcInputHandler.ButtonTexture.RightArrow
                        : IFcInputHandler.ButtonTexture.CircleZ + normalButtonIndex,
                    1f - sideButtonBoundsHalf);
                break;
            case 3:
                yield return new IFcInputHandler.Button(
                    GetButtonPressed(0),
                    button1Type,
                    button1Type is ButtonType.Direction
                        ? (_currentDirectionButtonCount is 1 ? IFcInputHandler.ButtonTexture.UpArrow : IFcInputHandler.ButtonTexture.LeftArrow)
                        : IFcInputHandler.ButtonTexture.CircleZ,
                    sideButtonBoundsHalf);

                if (button1Type is ButtonType.Button)
                {
                    normalButtonIndex++;
                }

                yield return new IFcInputHandler.Button(
                    GetButtonPressed(1),
                    button2Type,
                    button2Type is ButtonType.Direction
                        ? IFcInputHandler.ButtonTexture.RightArrow
                        : IFcInputHandler.ButtonTexture.CircleZ + normalButtonIndex,
                    button2Type is ButtonType.Direction
                        ? sideButtonBoundsHalf + sideButtonBounds
                        : 1f - (sideButtonBoundsHalf + sideButtonBounds));

                if (button2Type is ButtonType.Button)
                {
                    normalButtonIndex++;
                }

                yield return new IFcInputHandler.Button(
                    GetButtonPressed(2),
                    button3Type,
                    button3Type is ButtonType.Direction
                        ? IFcInputHandler.ButtonTexture.UpArrow
                        : IFcInputHandler.ButtonTexture.CircleZ + normalButtonIndex,
                    1f - sideButtonBoundsHalf);
                break;
            case 4:
                yield return new IFcInputHandler.Button(
                    GetButtonPressed(0),
                    button1Type,
                    button1Type is ButtonType.Direction
                        ? (_currentDirectionButtonCount is 1 ? IFcInputHandler.ButtonTexture.UpArrow : IFcInputHandler.ButtonTexture.LeftArrow)
                        : IFcInputHandler.ButtonTexture.CircleZ,
                    sideButtonBoundsHalf);

                if (button1Type is ButtonType.Button)
                {
                    normalButtonIndex++;
                }

                yield return new IFcInputHandler.Button(
                    GetButtonPressed(1),
                    button2Type,
                    button2Type is ButtonType.Direction
                        ? IFcInputHandler.ButtonTexture.RightArrow
                        : IFcInputHandler.ButtonTexture.CircleZ + normalButtonIndex,
                    sideButtonBoundsHalf + sideButtonBounds);

                if (button2Type is ButtonType.Button)
                {
                    normalButtonIndex++;
                }

                // why is this so complicated??
                int directionsLeft = Math.Max(_currentDirectionButtonCount - 2, 0);

                IFcInputHandler.ButtonTexture texture1 = default;
                IFcInputHandler.ButtonTexture texture2 = default;
                switch (directionsLeft)
                {
                    case 0:
                        texture1 = IFcInputHandler.ButtonTexture.CircleZ + normalButtonIndex;
                        texture2 = IFcInputHandler.ButtonTexture.CircleZ + normalButtonIndex + 1;
                        break;
                    case 1:
                        texture1 = IFcInputHandler.ButtonTexture.CircleZ + normalButtonIndex;
                        texture2 = IFcInputHandler.ButtonTexture.UpArrow;
                        break;
                    case 2:
                        texture1 = IFcInputHandler.ButtonTexture.DownArrow;
                        texture2 = IFcInputHandler.ButtonTexture.UpArrow;
                        break;
                }

                yield return new IFcInputHandler.Button(
                    GetButtonPressed(2),
                    button3Type,
                    texture1,
                    1f - (sideButtonBoundsHalf + sideButtonBounds));

                yield return new IFcInputHandler.Button(
                    GetButtonPressed(3),
                    button4Type,
                    texture2,
                    1f - sideButtonBoundsHalf);
                break;
        }
    }

    private bool GetButtonPressed(int totalIndex)
        => GetButtonPressed(totalIndex < _currentDirectionButtonCount ? totalIndex : totalIndex - _currentDirectionButtonCount, totalIndex < _currentDirectionButtonCount ? ButtonType.Direction : ButtonType.Button);

    private bool GetButtonPressed(int index, ButtonType type)
    {
        int buttonCount = type switch
        {
            ButtonType.Direction => _currentDirectionButtonCount,
            ButtonType.Button => _currentNormalButtonCount,
            _ => default,
        };

        if (index >= buttonCount)
        {
            return false;
        }

        FcKeyCode? keyToDetect = null;
        switch (type)
        {
            case ButtonType.Direction:
                switch (_currentDirectionButtonCount)
                {
                    case 1:
                        keyToDetect = FcKeyCode.Up;
                        break;
                    case 2:
                        keyToDetect = index is 0 ? FcKeyCode.Left : FcKeyCode.Right;
                        break;
                    case 3:
                        keyToDetect = index switch
                        {
                            0 => FcKeyCode.Left,
                            1 => FcKeyCode.Right,
                            2 => FcKeyCode.Up,
                            _ => null,
                        };
                        break;
                    case 4:
                        keyToDetect = index switch
                        {
                            0 => FcKeyCode.Left,
                            1 => FcKeyCode.Right,
                            2 => FcKeyCode.Down,
                            3 => FcKeyCode.Up,
                            _ => null,
                        };
                        break;
                }

                break;
            case ButtonType.Button:
                keyToDetect = FcKeyCode.Action1 + index;
                break;
        }

        if (keyToDetect is not null && _keys[keyToDetect.Value])
        {
            return true;
        }

        // key is not pressed, detect mouse/jlki
        var buttonBounds = IFcInputHandler.GetButtonBounds(index, type, _currentDirectionButtonCount, _currentNormalButtonCount, _screenInfo);

        Span<Vector2?> touchPositions = stackalloc Vector2?[3];
        ((IFcInputHandler)this).GetTouchPositions(touchPositions);
        foreach (var item in touchPositions)
        {
            if (item is not { } pos)
            {
                continue;
            }

            if (pos.X >= buttonBounds.Min && pos.X <= buttonBounds.Max)
            {
                return true;
            }
        }

        return false;
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
