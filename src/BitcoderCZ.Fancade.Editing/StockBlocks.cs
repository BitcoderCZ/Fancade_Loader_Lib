// <copyright file="StockBlocks.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Utils;
using BitcoderCZ.Fancade.Partial;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;

namespace BitcoderCZ.Fancade.Editing;

/// <summary>
/// The built in fancade block.
/// </summary>
public static class StockBlocks
{
    private static PartialPrefabList? _partialPrefabList;
    private static PrefabList? _prefabList;

    private static FrozenDictionary<ushort, BlockDef>? _blocks;

    /// <summary>
    /// Gets a <see cref="Partial.PartialPrefabList"/> with all of the stock fancade prefabs.
    /// </summary>
    /// <value>A <see cref="Partial.PartialPrefabList"/> with all of the stock fancade prefabs.</value>
    public static PartialPrefabList PartialPrefabList
    {
        get
        {
            if (_partialPrefabList is null)
            {
                using var resourceStream = ResourceUtils.GetResource("stockPrefabs.fcppl");
                using var reader = new FcBinaryReader(resourceStream);
                _partialPrefabList = PartialPrefabList.Load(reader);
            }

            return _partialPrefabList;
        }
    }

    /// <summary>
    /// Gets a <see cref="PrefabList"/> with all of the stock fancade prefabs.
    /// </summary>
    /// <value>A <see cref="PrefabList"/> with all of the stock fancade prefabs.</value>
    public static PrefabList PrefabList
    {
        get
        {
            if (_prefabList is null)
            {
                using var resourceStream = ResourceUtils.GetResource("stockPrefabs.fcpl");
                using var reader = new FcBinaryReader(resourceStream);
                _prefabList = PrefabList.Load(reader);

                foreach (var prefab in _prefabList.Prefabs)
                {
                    if (TryGetBlockDef(prefab.Id, out var def) && def.Terminals.Length > 0)
                    {
                        foreach (var terminal in def.Terminals)
                        {
                            prefab.Settings.Add((ushort3)terminal.Position, new PrefabSetting(
                                0,
                                SettingTypeUtils.FromTerminalSignalType(terminal.SignalType, terminal.Type == TerminalType.In),
                                (ushort3)terminal.Position,
                                terminal.Name ?? TerminalDef.GetDefaultName(terminal.SignalType)));
                        }
                    }
                }
            }

            return _prefabList;
        }
    }

    private static FrozenDictionary<ushort, BlockDef> Blocks
    {
        get
        {
            if (_blocks is null)
            {
                _blocks = typeof(StockBlocks)
                    .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
                    .SelectMany(type => type
                        .GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Where(f => f.FieldType == typeof(BlockDef))
                        .Select(f => (BlockDef)f.GetValue(null)!))
                    .ToFrozenDictionary(def => def.Prefab.Id);
            }

            return _blocks;
        }
    }

    /// <summary>
    /// Gets the stock <see cref="BlockDef"/> with the specified id.
    /// </summary>
    /// <param name="id">Id of the <see cref="BlockDef"/> to get.</param>
    /// <returns>The stock <see cref="BlockDef"/>.</returns>
    public static BlockDef GetBlockDef(ushort id)
        => Blocks[id];

    /// <summary>
    /// Gets the stock <see cref="BlockDef"/> with the specified id.
    /// </summary>
    /// <param name="id">Id of the <see cref="BlockDef"/> to get.</param>
    /// <param name="blockDef">The stock <see cref="BlockDef"/> with the specified id.</param>
    /// <returns><see langword="true"/> if the specified <see cref="BlockDef"/> exists; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetBlockDef(ushort id, [MaybeNullWhen(false)] out BlockDef blockDef)
        => Blocks.TryGetValue(id, out blockDef);

    /// <summary>
    /// The stock blocks in the game category.
    /// </summary>
    public static class Game
    {
        /// <summary>
        /// The Win block.
        /// </summary>
        public static readonly BlockDef Win = new BlockDef("Win", 252, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Lose block.
        /// </summary>
        public static readonly BlockDef Lose = new BlockDef("Lose", 256, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Set Score block.
        /// </summary>
        public static readonly BlockDef SetScore = new BlockDef("Set Score", 260, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.In, "Coins").Add(SignalType.Float, TerminalType.In, "Score").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Set Camera block.
        /// </summary>
        public static readonly BlockDef SetCamera = new BlockDef("Set Camera", 268, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.In, "Range").Add(SignalType.Rot, TerminalType.In, "Rotation").Add(SignalType.Vec3, TerminalType.In, "Position").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Set Light block.
        /// </summary>
        public static readonly BlockDef SetLight = new BlockDef("Set Light", 274, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Rot, TerminalType.In, "Rotation").Add(SignalType.Vec3, TerminalType.In, "Position").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Screen Size block.
        /// </summary>
        public static readonly BlockDef ScreenSize = new BlockDef("Screen Size", 220, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Height").Add(SignalType.Float, TerminalType.Out, "Width"));

        /// <summary>
        /// The Accelerometer block.
        /// </summary>
        public static readonly BlockDef Accelerometer = new BlockDef("Accelerometer", 224, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Direction"));

        /// <summary>
        /// The Current Frame block.
        /// </summary>
        public static readonly BlockDef CurrentFrame = new BlockDef("Current Frame", 564, ScriptBlockType.Value, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Counter"));

        /// <summary>
        /// The Menu Item block.
        /// </summary>
        public static readonly BlockDef MenuItem = new BlockDef("Menu Item", 584, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Obj, TerminalType.In, "Picture").Add(SignalType.FloatPtr, TerminalType.In, "Variable").Add(SignalType.Void, TerminalType.In));
    }

    /// <summary>
    /// The stock blocks in the objects category.
    /// </summary>
    public static class Objects
    {
        /// <summary>
        /// The Get Position block.
        /// </summary>
        public static readonly BlockDef GetPos = new BlockDef("Get Position", 278, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Rot, TerminalType.Out, "Rotation").Add(SignalType.Vec3, TerminalType.Out, "Position").Add(SignalType.Obj, TerminalType.In, "Object"));

        /// <summary>
        /// The Set Position block.
        /// </summary>
        public static readonly BlockDef SetPos = new BlockDef("Set Position", 282, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Rot, TerminalType.In, "Rotation").Add(SignalType.Vec3, TerminalType.In, "Position").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Raycast block.
        /// </summary>
        public static readonly BlockDef Raycast = new BlockDef("Raycast", 228, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Obj, TerminalType.Out, "Hit Obj").Add(SignalType.Vec3, TerminalType.Out, "Hit Pos").Add(SignalType.Bool, TerminalType.Out, "Hit?").Add(SignalType.Vec3, TerminalType.In, "To").Add(SignalType.Vec3, TerminalType.In, "From"));

        /// <summary>
        /// The Get Size block.
        /// </summary>
        public static readonly BlockDef GetSize = new BlockDef("Get Size", 489, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Max").Add(SignalType.Vec3, TerminalType.Out, "Min").Add(SignalType.Obj, TerminalType.In, "Object"));

        /// <summary>
        /// The Set Visible block.
        /// </summary>
        public static readonly BlockDef SetVisible = new BlockDef("Set Visible", 306, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Bool, TerminalType.In, "Visible").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Create Object block.
        /// </summary>
        public static readonly BlockDef CreateObject = new BlockDef("Create Object", 316, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Obj, TerminalType.Out, "Copy").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Destroy Object block.
        /// </summary>
        public static readonly BlockDef DestroyObject = new BlockDef("Destroy Object", 320, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));
    }

    /// <summary>
    /// The stock blocks in the sound category.
    /// </summary>
    public static class Sound
    {
        /// <summary>
        /// The Play Sound block.
        /// </summary>
        public static readonly BlockDef PlaySound = new BlockDef("Play Sound", 264, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.Out, "Channel").Add(SignalType.Float, TerminalType.In, "Pitch").Add(SignalType.Float, TerminalType.In, "Volume").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Stop Sound block.
        /// </summary>
        public static readonly BlockDef StopSound = new BlockDef("Stop Sound", 397, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.In, "Channel").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The VolumePitch block.
        /// </summary>
        public static readonly BlockDef VolumePitch = new BlockDef("Volume Pitch", 391, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.In, "Pitch").Add(SignalType.Float, TerminalType.In, "Volume").Add(SignalType.Float, TerminalType.In, "Channel").Add(SignalType.Void, TerminalType.In));
    }

    /// <summary>
    /// The stock blocks in the physics category.
    /// </summary>
    public static class Physics
    {
        /// <summary>
        /// The Add Force block.
        /// </summary>
        public static readonly BlockDef AddForce = new BlockDef("Add Force", 298, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 4), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Torque").Add(SignalType.Vec3, TerminalType.In, "Apply at").Add(SignalType.Vec3, TerminalType.In, "Force").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Get Velocity block.
        /// </summary>
        public static readonly BlockDef GetVelocity = new BlockDef("Get Velocity", 288, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Spin").Add(SignalType.Vec3, TerminalType.Out, "Velocity").Add(SignalType.Obj, TerminalType.In, "Object"));

        /// <summary>
        /// The Set Velocity block.
        /// </summary>
        public static readonly BlockDef SetVelocity = new BlockDef("Set Velocity", 292, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Spin").Add(SignalType.Vec3, TerminalType.In, "Velocity").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Set Locked block.
        /// </summary>
        public static readonly BlockDef SetLocked = new BlockDef("Set Locked", 310, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Rotation").Add(SignalType.Vec3, TerminalType.In, "Position").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Set Mass block.
        /// </summary>
        public static readonly BlockDef SetMass = new BlockDef("Set Mass", 328, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.In, "Mass").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Set Friction block.
        /// </summary>
        public static readonly BlockDef SetFriction = new BlockDef("Set Friction", 332, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.In, "Friction").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Set Bounciness block.
        /// </summary>
        public static readonly BlockDef SetBounciness = new BlockDef("Set Bounciness", 336, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.In, "Bounciness").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Set Gravity block.
        /// </summary>
        public static readonly BlockDef SetGravity = new BlockDef("Set Gravity", 324, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Gravity").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Add Constraint block.
        /// </summary>
        public static readonly BlockDef AddConstraint = new BlockDef("Add Constraint", 340, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Con, TerminalType.Out, "Constraint").Add(SignalType.Vec3, TerminalType.In, "Pivot").Add(SignalType.Obj, TerminalType.In, "Part").Add(SignalType.Obj, TerminalType.In, "Base").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Linear Limits block.
        /// </summary>
        public static readonly BlockDef LinearLimits = new BlockDef("Linear Limits", 346, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Upper").Add(SignalType.Vec3, TerminalType.In, "Lower").Add(SignalType.Con, TerminalType.In, "Constraint").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Angular Limits block.
        /// </summary>
        public static readonly BlockDef AngularLimits = new BlockDef("Angular Limits", 352, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Upper").Add(SignalType.Vec3, TerminalType.In, "Lower").Add(SignalType.Con, TerminalType.In, "Constraint").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Linear Spring block.
        /// </summary>
        public static readonly BlockDef LinearSpring = new BlockDef("Linear Spring", 358, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Damping").Add(SignalType.Vec3, TerminalType.In, "Stiffness").Add(SignalType.Con, TerminalType.In, "Constraint").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Angular Spring block.
        /// </summary>
        public static readonly BlockDef AngularSpring = new BlockDef("Angular Spring", 364, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Damping").Add(SignalType.Vec3, TerminalType.In, "Stiffness").Add(SignalType.Con, TerminalType.In, "Constraint").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Linear Motor block.
        /// </summary>
        public static readonly BlockDef LinearMotor = new BlockDef("Linear Motor", 370, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Force").Add(SignalType.Vec3, TerminalType.In, "Speed").Add(SignalType.Con, TerminalType.In, "Constraint").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Angular Motor block.
        /// </summary>
        public static readonly BlockDef AngularMotor = new BlockDef("Angular Motor", 376, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.In, "Force").Add(SignalType.Vec3, TerminalType.In, "Speed").Add(SignalType.Con, TerminalType.In, "Constraint").Add(SignalType.Void, TerminalType.In));
    }

    /// <summary>
    /// The stock blocks in the control category.
    /// </summary>
    public static class Control
    {
        /// <summary>
        /// The If block.
        /// </summary>
        public static readonly BlockDef If = new BlockDef("If", 234, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Void, TerminalType.Out, "False").Add(SignalType.Void, TerminalType.Out, "True").Add(SignalType.Bool, TerminalType.In, "Condition").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Play Sensor block.
        /// </summary>
        public static readonly BlockDef PlaySensor = new BlockDef("Play Sensor", 238, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Void, TerminalType.Out, "On Play").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Late Update block.
        /// </summary>
        public static readonly BlockDef LateUpdate = new BlockDef("Late Update", 566, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Void, TerminalType.Out, "After Physics").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Box Art Sensor block.
        /// </summary>
        public static readonly BlockDef BoxArtSensor = new BlockDef("Box Art Sensor", 409, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Void, TerminalType.Out, "On Screenshot").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Touch Sensor block.
        /// </summary>
        public static readonly BlockDef TouchSensor = new BlockDef("Touch Sensor", 242, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.Out, "Screen Y").Add(SignalType.Float, TerminalType.Out, "Screen X").Add(SignalType.Void, TerminalType.Out, "Touched").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Swipe Sensor block.
        /// </summary>
        public static readonly BlockDef SwipeSensor = new BlockDef("Swipe Sensor", 248, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.Out, "Direction").Add(SignalType.Void, TerminalType.Out, "Swiped").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Button block.
        /// </summary>
        public static readonly BlockDef Button = new BlockDef("Button", 588, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Void, TerminalType.Out, "Button").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Joystick block.
        /// </summary>
        public static readonly BlockDef Joystick = new BlockDef("Joystick", 592, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.Out, "Joy Dir").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Collision block.
        /// </summary>
        public static readonly BlockDef Collision = new BlockDef("Collision", 401, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 4), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Vec3, TerminalType.Out, "Normal").Add(SignalType.Float, TerminalType.Out, "Impulse").Add(SignalType.Obj, TerminalType.Out, "2nd Object").Add(SignalType.Void, TerminalType.Out, "Collided").Add(SignalType.Obj, TerminalType.In, "1st Object").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Loop block.
        /// </summary>
        public static readonly BlockDef Loop = new BlockDef("Loop", 560, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.Out, "Counter").Add(SignalType.Void, TerminalType.Out, "Do").Add(SignalType.Float, TerminalType.In, "Stop").Add(SignalType.Float, TerminalType.In, "Start").Add(SignalType.Void, TerminalType.In));
    }

    /// <summary>
    /// The stock blocks in the math category.
    /// </summary>
    public static class Math
    {
        /// <summary>
        /// The Negate block.
        /// </summary>
        public static readonly BlockDef Negate = new BlockDef("Negate", 90, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "-Num").Add(SignalType.Float, TerminalType.In, "Num"));

        /// <summary>
        /// The Not block.
        /// </summary>
        public static readonly BlockDef Not = new BlockDef("Not", 144, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "Not Tru").Add(SignalType.Bool, TerminalType.In, "Tru"));

        /// <summary>
        /// The Inverse block.
        /// </summary>
        public static readonly BlockDef Inverse = new BlockDef("Inverse", 440, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Rot, TerminalType.Out, "Rot Inverse").Add(SignalType.Rot, TerminalType.In, "Rot"));

#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable CA1707 // Identifiers should not contain underscores
        /// <summary>
        /// The Add Numbers block.
        /// </summary>
        public static readonly BlockDef Add_Number = new BlockDef("Add Numbers", 92, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Num1 + Num2").Add(SignalType.Float, TerminalType.In, "Num2").Add(SignalType.Float, TerminalType.In, "Num1"));

        /// <summary>
        /// The Add Vectors block.
        /// </summary>
        public static readonly BlockDef Add_Vector = new BlockDef("Add Vectors", 96, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Vec1 + Vec2").Add(SignalType.Vec3, TerminalType.In, "Vec2").Add(SignalType.Vec3, TerminalType.In, "Vec1"));

        /// <summary>
        /// The Subtract Numbers block.
        /// </summary>
        public static readonly BlockDef Subtract_Number = new BlockDef("Subtract Numbers", 100, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Num1 - Num2").Add(SignalType.Float, TerminalType.In, "Num2").Add(SignalType.Float, TerminalType.In, "Num1"));

        /// <summary>
        /// The Subtract Vectors block.
        /// </summary>
        public static readonly BlockDef Subtract_Vector = new BlockDef("Subtract Vectors", 104, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Vec1 - Vec2").Add(SignalType.Vec3, TerminalType.In, "Vec2").Add(SignalType.Vec3, TerminalType.In, "Vec1"));

        /// <summary>
        /// The Multiply block.
        /// </summary>
        public static readonly BlockDef Multiply_Number = new BlockDef("Multiply", 108, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Num1 * Num2").Add(SignalType.Float, TerminalType.In, "Num2").Add(SignalType.Float, TerminalType.In, "Num1"));

        /// <summary>
        /// The Scale block.
        /// </summary>
        public static readonly BlockDef Multiply_Vector = new BlockDef("Scale", 112, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Vec * Num").Add(SignalType.Float, TerminalType.In, "Num").Add(SignalType.Vec3, TerminalType.In, "Vec"));

        /// <summary>
        /// The Rotate block.
        /// </summary>
        public static readonly BlockDef Rotate_Vector = new BlockDef("Rotate", 116, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Rot * Vec").Add(SignalType.Rot, TerminalType.In, "Rot").Add(SignalType.Vec3, TerminalType.In, "Vec"));

        /// <summary>
        /// The Combine block.
        /// </summary>
        public static readonly BlockDef Multiply_Rotation = new BlockDef("Combine", 120, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Rot, TerminalType.Out, "Rot1 * Rot2").Add(SignalType.Rot, TerminalType.In, "Rot2").Add(SignalType.Rot, TerminalType.In, "Rot1"));

        /// <summary>
        /// The Divide block.
        /// </summary>
        public static readonly BlockDef Divide_Number = new BlockDef("Divide", 124, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Num1 / Num2").Add(SignalType.Float, TerminalType.In, "Num2").Add(SignalType.Float, TerminalType.In, "Num1"));

        /// <summary>
        /// The Modulo block.
        /// </summary>
        public static readonly BlockDef Modulo_Number = new BlockDef("Modulo", 172, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "mod(a,b)").Add(SignalType.Float, TerminalType.In, "b").Add(SignalType.Float, TerminalType.In, "a"));

        /// <summary>
        /// The Power block.
        /// </summary>
        public static readonly BlockDef Power = new BlockDef("Power", 457, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Base ^ Exponent").Add(SignalType.Float, TerminalType.In, "Exponent").Add(SignalType.Float, TerminalType.In, "Base"));

        /// <summary>
        /// The Equals Numbers block.
        /// </summary>
        public static readonly BlockDef Equals_Number = new BlockDef("Equals Numbers", 132, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "Num1 = Num2").Add(SignalType.Float, TerminalType.In, "Num2").Add(SignalType.Float, TerminalType.In, "Num1"));

        /// <summary>
        /// The Equals Vectors block.
        /// </summary>
        public static readonly BlockDef Equals_Vector = new BlockDef("Equals Vectors", 136, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "Vec1 = Vec2").Add(SignalType.Vec3, TerminalType.In, "Vec2").Add(SignalType.Vec3, TerminalType.In, "Vec1"));

        /// <summary>
        /// The Equals Objects block.
        /// </summary>
        public static readonly BlockDef Equals_Object = new BlockDef("Equals Objects", 140, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "Obj1 = Obj2").Add(SignalType.Obj, TerminalType.In, "Obj2").Add(SignalType.Obj, TerminalType.In, "Obj1"));

        /// <summary>
        /// The Equals Truths block.
        /// </summary>
        public static readonly BlockDef Equals_Bool = new BlockDef("Equals Truths", 421, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "Tru1 = Tru2").Add(SignalType.Bool, TerminalType.In, "Tru2").Add(SignalType.Bool, TerminalType.In, "Tru1"));
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore SA1310 // Field names should not contain underscore

        /// <summary>
        /// The AND block.
        /// </summary>
        public static readonly BlockDef LogicalAnd = new BlockDef("AND", 146, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "Tru1 & Tru2").Add(SignalType.Bool, TerminalType.In, "Tru2").Add(SignalType.Bool, TerminalType.In, "Tru1"));

        /// <summary>
        /// The OR block.
        /// </summary>
        public static readonly BlockDef LogicalOr = new BlockDef("OR", 417, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "Tru1 | Tru2").Add(SignalType.Bool, TerminalType.In, "Tru2").Add(SignalType.Bool, TerminalType.In, "Tru1"));

        /// <summary>
        /// The Less Than block.
        /// </summary>
        public static readonly BlockDef Less = new BlockDef("Less Than", 128, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "Num1 < Num2").Add(SignalType.Float, TerminalType.In, "Num2").Add(SignalType.Float, TerminalType.In, "Num1"));

        /// <summary>
        /// The Greater Than block.
        /// </summary>
        public static readonly BlockDef Greater = new BlockDef("Greater Than", 481, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "Num1 > Num2").Add(SignalType.Float, TerminalType.In, "Num2").Add(SignalType.Float, TerminalType.In, "Num1"));

        /// <summary>
        /// The Random block.
        /// </summary>
        public static readonly BlockDef Random = new BlockDef("Random", 168, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Random").Add(SignalType.Float, TerminalType.In, "Max").Add(SignalType.Float, TerminalType.In, "Min"));

        /// <summary>
        /// The Random Seed block.
        /// </summary>
        public static readonly BlockDef RandomSeed = new BlockDef("Random Seed", 485, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out).Add(SignalType.Float, TerminalType.In, "Seed").Add(SignalType.Void, TerminalType.In));

        /// <summary>
        /// The Min block.
        /// </summary>
        public static readonly BlockDef Min = new BlockDef("Min", 176, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Smaller").Add(SignalType.Float, TerminalType.In, "Num2").Add(SignalType.Float, TerminalType.In, "Num1"));

        /// <summary>
        /// The Max block.
        /// </summary>
        public static readonly BlockDef Max = new BlockDef("Max", 180, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Bigger").Add(SignalType.Float, TerminalType.In, "Num2").Add(SignalType.Float, TerminalType.In, "Num1"));

        /// <summary>
        /// The Sin block.
        /// </summary>
        public static readonly BlockDef Sin = new BlockDef("Sin", 413, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Sin(Num)").Add(SignalType.Float, TerminalType.In, "Num"));

        /// <summary>
        /// The Cos block.
        /// </summary>
        public static readonly BlockDef Cos = new BlockDef("Cos", 453, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Cos(Num)").Add(SignalType.Float, TerminalType.In, "Num"));

        /// <summary>
        /// The Round block.
        /// </summary>
        public static readonly BlockDef Round = new BlockDef("Round", 184, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Rounded").Add(SignalType.Float, TerminalType.In, "Number"));

        /// <summary>
        /// The Floor block.
        /// </summary>
        public static readonly BlockDef Floor = new BlockDef("Floor", 186, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Floor").Add(SignalType.Float, TerminalType.In, "Number"));

        /// <summary>
        /// The Ceiling block.
        /// </summary>
        public static readonly BlockDef Ceiling = new BlockDef("Ceiling", 188, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Ceiling").Add(SignalType.Float, TerminalType.In, "Number"));

        /// <summary>
        /// The Absolute block.
        /// </summary>
        public static readonly BlockDef Absolute = new BlockDef("Absolute", 455, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "|Num|").Add(SignalType.Float, TerminalType.In, "Num"));

        /// <summary>
        /// The Logarithm block.
        /// </summary>
        public static readonly BlockDef Logarithm = new BlockDef("Logarithm", 580, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Logarithm").Add(SignalType.Float, TerminalType.In, "Base").Add(SignalType.Float, TerminalType.In, "Number"));

        /// <summary>
        /// The Normalize block.
        /// </summary>
        public static readonly BlockDef Normalize = new BlockDef("Normalize", 578, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Normalized").Add(SignalType.Vec3, TerminalType.In, "Vector"));

        /// <summary>
        /// The Dot Product block.
        /// </summary>
        public static readonly BlockDef DotProduct = new BlockDef("Dot Product", 570, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Dot Product").Add(SignalType.Vec3, TerminalType.In, "Vector").Add(SignalType.Vec3, TerminalType.In, "Vector"));

        /// <summary>
        /// The Cross Product block.
        /// </summary>
        public static readonly BlockDef CrossProduct = new BlockDef("Cross Product", 574, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Cross Product").Add(SignalType.Vec3, TerminalType.In, "Vector").Add(SignalType.Vec3, TerminalType.In, "Vector"));

        /// <summary>
        /// The Distance block.
        /// </summary>
        public static readonly BlockDef Distance = new BlockDef("Distance", 190, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Distance").Add(SignalType.Vec3, TerminalType.In, "Vector").Add(SignalType.Vec3, TerminalType.In, "Vector"));

        /// <summary>
        /// The LERP block.
        /// </summary>
        public static readonly BlockDef Lerp = new BlockDef("LERP", 194, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Rot, TerminalType.Out, "Rotation").Add(SignalType.Float, TerminalType.In, "Amount").Add(SignalType.Rot, TerminalType.In, "To").Add(SignalType.Rot, TerminalType.In, "From"));

        /// <summary>
        /// The Axis Angle block.
        /// </summary>
        public static readonly BlockDef AxisAngle = new BlockDef("Axis Angle", 200, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Rot, TerminalType.Out, "Rotation").Add(SignalType.Float, TerminalType.In, "Angle").Add(SignalType.Vec3, TerminalType.In, "Axis"));

        /// <summary>
        /// The Screen To World block.
        /// </summary>
        public static readonly BlockDef ScreenToWorld = new BlockDef("Screen To World", 216, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "World Far").Add(SignalType.Vec3, TerminalType.Out, "World Near").Add(SignalType.Float, TerminalType.In, "Screen Y").Add(SignalType.Float, TerminalType.In, "Screen X"));

        /// <summary>
        /// The World To Screen block.
        /// </summary>
        public static readonly BlockDef WorldToScreen = new BlockDef("World To Screen", 477, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Screen Y").Add(SignalType.Float, TerminalType.Out, "Screen X").Add(SignalType.Vec3, TerminalType.In, "World Pos"));

        /// <summary>
        /// The Look Rotation block.
        /// </summary>
        public static readonly BlockDef LookRotation = new BlockDef("Look Rotation", 204, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Rot, TerminalType.Out, "Rotation").Add(SignalType.Vec3, TerminalType.In, "Up").Add(SignalType.Vec3, TerminalType.In, "Direction"));

        /// <summary>
        /// The Line vs Plane block.
        /// </summary>
        public static readonly BlockDef LineVsPlane = new BlockDef("Line vs Plane", 208, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 4), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Intersection").Add(SignalType.Vec3, TerminalType.In, "Plane Normal").Add(SignalType.Vec3, TerminalType.In, "Plane Point").Add(SignalType.Vec3, TerminalType.In, "Line To").Add(SignalType.Vec3, TerminalType.In, "Line From"));

#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable CA1707 // Identifiers should not contain underscores
        /// <summary>
        /// The Make Vector block.
        /// </summary>
        public static readonly BlockDef Make_Vector = new BlockDef("Make Vector", 150, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Vector").Add(SignalType.Float, TerminalType.In, "Z").Add(SignalType.Float, TerminalType.In, "Y").Add(SignalType.Float, TerminalType.In, "X"));

        /// <summary>
        /// The Break Vector block.
        /// </summary>
        public static readonly BlockDef Break_Vector = new BlockDef("Break Vector", 156, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Z").Add(SignalType.Float, TerminalType.Out, "Y").Add(SignalType.Float, TerminalType.Out, "X").Add(SignalType.Vec3, TerminalType.In, "Vector"));

        /// <summary>
        /// The Make Rotation block.
        /// </summary>
        public static readonly BlockDef Make_Rotation = new BlockDef("Make Rotation", 162, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Rot, TerminalType.Out, "Rotation").Add(SignalType.Float, TerminalType.In, "Z angle").Add(SignalType.Float, TerminalType.In, "Y angle").Add(SignalType.Float, TerminalType.In, "X angle"));

        /// <summary>
        /// The Break Rotation block.
        /// </summary>
        public static readonly BlockDef Break_Rotation = new BlockDef("Break Rotation", 442, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 3), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Z angle").Add(SignalType.Float, TerminalType.Out, "Y angle").Add(SignalType.Float, TerminalType.Out, "X angle").Add(SignalType.Rot, TerminalType.In, "Rotation"));
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore SA1310 // Field names should not contain underscore

        /// <summary>
        /// Gets the equals block for a <see cref="SignalType"/>.
        /// </summary>
        /// <param name="type">The <see cref="SignalType"/>.</param>
        /// <returns>The equals block.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a valid <see cref="SignalType"/>.</exception>
        public static BlockDef EqualsByType(SignalType type)
            => type.ToNotPointer() switch
            {
                SignalType.Float => Equals_Number,
                SignalType.Vec3 => Equals_Vector,
                SignalType.Bool => Equals_Bool,
                SignalType.Obj => Equals_Object,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
            };

        /// <summary>
        /// Gets the break block for a <see cref="SignalType"/>.
        /// </summary>
        /// <param name="type">The <see cref="SignalType"/>.</param>
        /// <returns>The break block.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a valid <see cref="SignalType"/>.</exception>
        public static BlockDef BreakByType(SignalType type)
            => type.ToNotPointer() switch
            {
                SignalType.Vec3 => Break_Vector,
                SignalType.Rot => Break_Rotation,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
            };

        /// <summary>
        /// Gets the make block for a <see cref="SignalType"/>.
        /// </summary>
        /// <param name="type">The <see cref="SignalType"/>.</param>
        /// <returns>The make block.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a valid <see cref="SignalType"/>.</exception>
        public static BlockDef MakeByType(SignalType type)
            => type.ToNotPointer() switch
            {
                SignalType.Vec3 => Make_Vector,
                SignalType.Rot => Make_Rotation,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
            };
    }

    /// <summary>
    /// The stock blocks in the values category.
    /// </summary>
    public static class Values
    {
        /// <summary>
        /// The Number block.
        /// </summary>
        public static readonly BlockDef Number = new BlockDef("Number", 36, ScriptBlockType.Value, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Float, TerminalType.Out, "Number"));

        /// <summary>
        /// The Vector block.
        /// </summary>
        public static readonly BlockDef Vector = new BlockDef("Vector", 38, ScriptBlockType.Value, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3, TerminalType.Out, "Vector"));

        /// <summary>
        /// The Rotation block.
        /// </summary>
        public static readonly BlockDef Rotation = new BlockDef("Rotation", 42, ScriptBlockType.Value, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Rot, TerminalType.Out, "Rotation"));

        /// <summary>
        /// The True block.
        /// </summary>
        public static readonly BlockDef True = new BlockDef("True", 449, ScriptBlockType.Value, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "True"));

        /// <summary>
        /// The False block.
        /// </summary>
        public static readonly BlockDef False = new BlockDef("False", 451, ScriptBlockType.Value, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Bool, TerminalType.Out, "False"));

        /// <summary>
        /// The Comment block.
        /// </summary>
        public static readonly BlockDef Comment = new BlockDef("Comment", 15, ScriptBlockType.Value, PrefabType.Script, new int3(1, 1, 1), TerminalBuilder.Empty);

#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable CA1707 // Identifiers should not contain underscores
        /// <summary>
        /// The Inspect Number block.
        /// </summary>
        public static readonly BlockDef Inspect_Number = new BlockDef("Inspect Number", 16, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Float, TerminalType.In, "Number").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Inspect Vector block.
        /// </summary>
        public static readonly BlockDef Inspect_Vector = new BlockDef("Inspect Vector", 20, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Vec3, TerminalType.In, "Vector").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Inspect Rotation block.
        /// </summary>
        public static readonly BlockDef Inspect_Rotation = new BlockDef("Inspect Rotation", 24, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Rot, TerminalType.In, "Rotation").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Inspect Truth block.
        /// </summary>
        public static readonly BlockDef Inspect_Truth = new BlockDef("Inspect Truth", 28, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Bool, TerminalType.In, "Truth").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Inspect Object block.
        /// </summary>
        public static readonly BlockDef Inspect_Object = new BlockDef("Inspect Object", 32, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Obj, TerminalType.In, "Object").Add(SignalType.Void, TerminalType.In, "Before"));
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore SA1310 // Field names should not contain underscore

        /// <summary>
        /// Gets the value block for <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The object to get the value for.</param>
        /// <returns>The value block.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a valid literal.</exception>
        public static BlockDef ValueByType(object value)
            => value switch
            {
                float => Number,
                bool b => b ? True : False,
                float3 => Vector,
                Vector3 => Vector,
                BitcoderCZ.Fancade.Rotation => Rotation,
                _ => throw new ArgumentException($"No literal exists for type '{value.GetType().FullName}',", nameof(value)),
            };

        /// <summary>
        /// Gets the inspect block for a <see cref="SignalType"/>.
        /// </summary>
        /// <param name="type">The <see cref="SignalType"/>.</param>
        /// <returns>The inspect block.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a valid <see cref="SignalType"/>.</exception>
        public static BlockDef InspectByType(SignalType type)
            => type.ToNotPointer() switch
            {
                SignalType.Float => Inspect_Number,
                SignalType.Bool => Inspect_Truth,
                SignalType.Vec3 => Inspect_Vector,
                SignalType.Rot => Inspect_Rotation,
                SignalType.Obj => Inspect_Object,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
            };
    }

    /// <summary>
    /// The stock blocks in the variables category.
    /// </summary>
    public static class Variables
    {
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable CA1707 // Identifiers should not contain underscores
        #region Get Variable

        /// <summary>
        /// The Variable block.
        /// </summary>
        public static readonly BlockDef Get_Variable_Num = new BlockDef("Variable", 46, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.FloatPtr, TerminalType.Out, "Number"));

        /// <summary>
        /// The Variable block.
        /// </summary>
        public static readonly BlockDef Get_Variable_Vec = new BlockDef("Variable", 48, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Vec3Ptr, TerminalType.Out, "Vector"));

        /// <summary>
        /// The Variable block.
        /// </summary>
        public static readonly BlockDef Get_Variable_Rot = new BlockDef("Variable", 50, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.RotPtr, TerminalType.Out, "Rotation"));

        /// <summary>
        /// The Variable block.
        /// </summary>
        public static readonly BlockDef Get_Variable_Tru = new BlockDef("Variable", 52, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.BoolPtr, TerminalType.Out, "Truth"));

        /// <summary>
        /// The Variable block.
        /// </summary>
        public static readonly BlockDef Get_Variable_Obj = new BlockDef("Variable", 54, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.ObjPtr, TerminalType.Out, "Object"));

        /// <summary>
        /// The Variable block.
        /// </summary>
        public static readonly BlockDef Get_Variable_Con = new BlockDef("Variable", 56, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.ConPtr, TerminalType.Out, "Constraint"));
        #endregion
        #region Set Variable

        /// <summary>
        /// The Set Variable block.
        /// </summary>
        public static readonly BlockDef Set_Variable_Num = new BlockDef("Set Variable", 428, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Float, TerminalType.In, "Value").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Variable block.
        /// </summary>
        public static readonly BlockDef Set_Variable_Vec = new BlockDef("Set Variable", 430, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Vec3, TerminalType.In, "Value").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Variable block.
        /// </summary>
        public static readonly BlockDef Set_Variable_Rot = new BlockDef("Set Variable", 432, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Rot, TerminalType.In, "Value").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Variable block.
        /// </summary>
        public static readonly BlockDef Set_Variable_Tru = new BlockDef("Set Variable", 434, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Bool, TerminalType.In, "Value").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Variable block.
        /// </summary>
        public static readonly BlockDef Set_Variable_Obj = new BlockDef("Set Variable", 436, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Obj, TerminalType.In, "Value").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Variable block.
        /// </summary>
        public static readonly BlockDef Set_Variable_Con = new BlockDef("Set Variable", 438, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Con, TerminalType.In, "Value").Add(SignalType.Void, TerminalType.In, "Before"));
        #endregion
        #region Set Pointer

        /// <summary>
        /// The Set Number block.
        /// </summary>
        public static readonly BlockDef Set_Ptr_Num = new BlockDef("Set Number", 58, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Float, TerminalType.In, "Value").Add(SignalType.FloatPtr, TerminalType.In, "Variable").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Vector block.
        /// </summary>
        public static readonly BlockDef Set_Ptr_Vec = new BlockDef("Set Vector", 62, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Vec3, TerminalType.In, "Value").Add(SignalType.Vec3Ptr, TerminalType.In, "Variable").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Rotation block.
        /// </summary>
        public static readonly BlockDef Set_Ptr_Rot = new BlockDef("Set Rotation", 66, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Rot, TerminalType.In, "Value").Add(SignalType.RotPtr, TerminalType.In, "Variable").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Truth block.
        /// </summary>
        public static readonly BlockDef Set_Ptr_Tru = new BlockDef("Set Truth", 70, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Bool, TerminalType.In, "Value").Add(SignalType.BoolPtr, TerminalType.In, "Variable").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Object block.
        /// </summary>
        public static readonly BlockDef Set_Ptr_Obj = new BlockDef("Set Object", 74, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Obj, TerminalType.In, "Value").Add(SignalType.ObjPtr, TerminalType.In, "Variable").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Set Constraint block.
        /// </summary>
        public static readonly BlockDef Set_Ptr_Con = new BlockDef("Set Constraint", 78, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Obj, TerminalType.In, "Value").Add(SignalType.ConPtr, TerminalType.In, "Variable").Add(SignalType.Void, TerminalType.In, "Before"));
        #endregion
        #region List

        /// <summary>
        /// The List Number block.
        /// </summary>
        public static readonly BlockDef List_Num = new BlockDef("List Number", 82, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.FloatPtr, TerminalType.Out, "Element").Add(SignalType.Float, TerminalType.In, "Index").Add(SignalType.FloatPtr, TerminalType.In, "Variable"));

        /// <summary>
        /// The List Vector block.
        /// </summary>
        public static readonly BlockDef List_Vec = new BlockDef("List Vector", 461, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.Vec3Ptr, TerminalType.Out, "Element").Add(SignalType.Float, TerminalType.In, "Index").Add(SignalType.Vec3Ptr, TerminalType.In, "Variable"));

        /// <summary>
        /// The List Rotation block.
        /// </summary>
        public static readonly BlockDef List_Rot = new BlockDef("List Rotation", 465, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.RotPtr, TerminalType.Out, "Element").Add(SignalType.Float, TerminalType.In, "Index").Add(SignalType.RotPtr, TerminalType.In, "Variable"));

        /// <summary>
        /// The List Truth block.
        /// </summary>
        public static readonly BlockDef List_Tru = new BlockDef("List Truth", 469, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.BoolPtr, TerminalType.Out, "Element").Add(SignalType.Float, TerminalType.In, "Index").Add(SignalType.BoolPtr, TerminalType.In, "Variable"));

        /// <summary>
        /// The List Object block.
        /// </summary>
        public static readonly BlockDef List_Obj = new BlockDef("List Object", 86, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.ObjPtr, TerminalType.Out, "Element").Add(SignalType.Float, TerminalType.In, "Index").Add(SignalType.ObjPtr, TerminalType.In, "Variable"));

        /// <summary>
        /// The List Constraint block.
        /// </summary>
        public static readonly BlockDef List_Con = new BlockDef("List Constraint", 473, ScriptBlockType.Pasive, PrefabType.Script, new int3(2, 1, 2), TerminalBuilder.Create().Add(SignalType.ConPtr, TerminalType.Out, "Element").Add(SignalType.Float, TerminalType.In, "Index").Add(SignalType.ConPtr, TerminalType.In, "Variable"));
        #endregion
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore SA1310 // Field names should not contain underscore

        /// <summary>
        /// The Increase Number block.
        /// </summary>
        public static readonly BlockDef IncrementNumber = new BlockDef("Increase Number", 556, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Float, TerminalType.In, "Variable").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// The Decrease Number block.
        /// </summary>
        public static readonly BlockDef DecrementNumber = new BlockDef("Decrease Number", 558, ScriptBlockType.Active, PrefabType.Script, new int3(2, 1, 1), TerminalBuilder.Create().Add(SignalType.Void, TerminalType.Out, "After").Add(SignalType.Float, TerminalType.In, "Variable").Add(SignalType.Void, TerminalType.In, "Before"));

        /// <summary>
        /// Gets the get variable block for a <see cref="SignalType"/>.
        /// </summary>
        /// <param name="type">The <see cref="SignalType"/>.</param>
        /// <returns>The get variable block.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a valid <see cref="SignalType"/>.</exception>
        public static BlockDef GetVariableByType(SignalType type)
            => type.ToNotPointer() switch
            {
                SignalType.Float => Get_Variable_Num,
                SignalType.Bool => Get_Variable_Tru,
                SignalType.Vec3 => Get_Variable_Vec,
                SignalType.Rot => Get_Variable_Rot,
                SignalType.Obj => Get_Variable_Obj,
                SignalType.Con => Get_Variable_Con,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
            };

        /// <summary>
        /// Gets the set variable block for a <see cref="SignalType"/>.
        /// </summary>
        /// <param name="type">The <see cref="SignalType"/>.</param>
        /// <returns>The set variable block.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a valid <see cref="SignalType"/>.</exception>
        public static BlockDef SetVariableByType(SignalType type)
            => type.ToNotPointer() switch
            {
                SignalType.Float => Set_Variable_Num,
                SignalType.Bool => Set_Variable_Tru,
                SignalType.Vec3 => Set_Variable_Vec,
                SignalType.Rot => Set_Variable_Rot,
                SignalType.Obj => Set_Variable_Obj,
                SignalType.Con => Set_Variable_Con,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
            };

        /// <summary>
        /// Gets the list block for a <see cref="SignalType"/>.
        /// </summary>
        /// <param name="type">The <see cref="SignalType"/>.</param>
        /// <returns>The list block.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a valid <see cref="SignalType"/>.</exception>
        public static BlockDef ListByType(SignalType type)
            => type.ToNotPointer() switch
            {
                SignalType.Float => List_Num,
                SignalType.Bool => List_Tru,
                SignalType.Vec3 => List_Vec,
                SignalType.Rot => List_Rot,
                SignalType.Obj => List_Obj,
                SignalType.Con => List_Con,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
            };

        /// <summary>
        /// Gets the set pointer block for a <see cref="SignalType"/>.
        /// </summary>
        /// <param name="type">The <see cref="SignalType"/>.</param>
        /// <returns>The set pointer block.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="type"/> is not a valid <see cref="SignalType"/>.</exception>
        public static BlockDef SetPtrByType(SignalType type)
            => type.ToNotPointer() switch
            {
                SignalType.Float => Set_Ptr_Num,
                SignalType.Bool => Set_Ptr_Tru,
                SignalType.Vec3 => Set_Ptr_Vec,
                SignalType.Rot => Set_Ptr_Rot,
                SignalType.Obj => Set_Ptr_Obj,
                SignalType.Con => Set_Ptr_Rot,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SignalType)),
            };
    }
}
