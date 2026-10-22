using System;
using UnityEngine;

namespace MidLane
{
    // Official Riot particle definitions exported by Tools/Import-IreliaVfx.py.
    // Field names follow VfxEmitterDefinitionData; vectors are still in LoL axes and units.
    [Serializable]
    public sealed class RiotVfxLibraryData
    {
        public int version;
        public RiotVfxSystemData[] systems;
        public RiotVfxMeshData[] meshes;
        public RiotVfxEventData[] events;
    }

    [Serializable]
    public sealed class RiotVfxSystemData
    {
        public string name;
        public string sound;
        public RiotVfxEmitterData[] emitters;
    }

    [Serializable]
    public sealed class RiotVfxMeshData
    {
        public string name;
        public float[] positions, uvs, colors;
        public int[] indices;
    }

    // Particle events of the official animation graph (clip -> system, start frame, bone).
    [Serializable]
    public sealed class RiotVfxEventData
    {
        public string clip, system, bone;
        public float frame;
        public bool scaleWithAnimation;
    }

    [Serializable]
    public sealed class RiotVfxTable
    {
        public float[] t, v;
    }

    // ValueFloat / ValueVector / ValueColor: constant, or a curve when dynamics exist.
    // Probability tables randomize birth values per component.
    [Serializable]
    public sealed class RiotVfxValue
    {
        public float[] c, t, v;
        public RiotVfxTable[] p;
        public bool Present { get { return (c != null && c.Length > 0) || (t != null && t.Length > 0); } }
    }

    [Serializable]
    public sealed class RiotVfxErosion
    {
        public string map;
        public RiotVfxValue drive;
        public float featherIn, featherOut, sliceWidth;
        public bool useAlpha;
    }

    // VfxTextureMultDefinitionData: a second texture multiplied in, with its own UV motion.
    [Serializable]
    public sealed class RiotVfxTextureMult
    {
        public string texture;
        public float[] texDiv;
        public RiotVfxValue birthUvScroll, birthUvOffset;
        public bool Present { get { return !string.IsNullOrEmpty(texture); } }
    }

    // paletteDefinition: the texture's red channel picks a color in one row of a palette texture.
    [Serializable]
    public sealed class RiotVfxPalette
    {
        public string texture;
        public int count;
        public float index;
        public bool Present { get { return !string.IsNullOrEmpty(texture) && count > 0; } }
    }

    [Serializable]
    public sealed class RiotVfxEmitterData
    {
        public string name, skip, primitive, mesh, texture, shape;
        public int blend, pass, alphaRef, numFrames, startFrame, uvMode, trailMode;
        public bool twoSided, single, uniformScale, rotationEnabled, localOrientation, particleLocalOrientation;
        public bool directionOriented, groundLayer, randomStartFrame;
        public float delay, lifetime, linger, bindWeight, flexScale, flexOffset, shapeRadius, shapeHeight;
        public int shapeFlags;
        public float directionVelocityScale, directionVelocityMinScale, frameRate, trailCutoff;
        // Beam meshes: length along Z in LoL units, stretched to the target distance.
        public float beamLength;
        public float[] texDiv, shapeSize, shapeRotationAxes;
        public RiotVfxValue rate, particleLifetime, birthColor, color, colorRamp, birthScale, scale;
        public RiotVfxValue birthRotation, rotation, rotationalVelocity, birthVelocity, velocity;
        public RiotVfxValue birthDrag, drag, acceleration, emitterPosition, shapeOffset;
        public RiotVfxValue uvScroll, birthUvScroll, birthUvOffset, uvRotation, trailTiling;
        public RiotVfxValue[] shapeRotationAngles;
        public RiotVfxErosion erosion;
        public RiotVfxTextureMult textureMult;
        public RiotVfxPalette palette;
        // Systems each particle spawns and carries (childParticleSetDefinition).
        public string[] children;
        // birthOrbitalVelocity: angular velocity (radians/s) of the particle around the emitter.
        public RiotVfxValue orbitalVelocity;

        public bool Skipped { get { return !string.IsNullOrEmpty(skip); } }
        public bool IsBillboard { get { return primitive == "billboard"; } }
        public bool IsTrail { get { return primitive == "trail" || primitive == "cameraTrail"; } }
        // VfxPrimitiveBeam: its mesh is stretched from the emitter to the system's target point.
        public bool IsBeam { get { return primitive == "beam"; } }
        // VfxPrimitiveRay: a camera-facing strip that starts at the particle and runs along its +Z.
        public bool IsRay { get { return primitive == "ray"; } }
        public bool HasChildren { get { return children != null && children.Length > 0; } }
    }

    // Evaluation of Riot values. Curves are piecewise linear; the time is normalized to the
    // particle's age for per-particle values and to the emitter's age for birth values.
    public static class RiotVfxEval
    {
        public static float Curve(float[] times, float[] values, int stride, int component, float time)
        {
            int count = times.Length;
            if (count == 0 || values == null || values.Length < count * stride) return 0;
            if (time <= times[0] || count == 1) return values[component];
            for (int i = 1; i < count; i++)
            {
                if (time > times[i]) continue;
                float span = times[i] - times[i - 1];
                float k = span > 1e-6f ? (time - times[i - 1]) / span : 1;
                return Mathf.Lerp(values[(i - 1) * stride + component], values[i * stride + component], k);
            }
            return values[(count - 1) * stride + component];
        }

        public static float Component(RiotVfxValue value, int stride, int component, float time, float fallback)
        {
            if (value == null) return fallback;
            if (value.t != null && value.t.Length > 0) return Curve(value.t, value.v, stride, component, time);
            if (value.c != null && value.c.Length > component) return value.c[component];
            return fallback;
        }

        // Probability table: maps a uniform random number to a multiplier.
        public static float Table(RiotVfxTable table, double random)
        {
            if (table == null || table.t == null || table.t.Length == 0 || table.v == null || table.v.Length < table.t.Length) return 1;
            return Curve(table.t, table.v, 1, 0, (float)random);
        }

        static float Random(RiotVfxValue value, int component, System.Random random)
        {
            if (value == null || value.p == null || component >= value.p.Length || random == null) return 1;
            return Table(value.p[component], random.NextDouble());
        }

        public static float Float(RiotVfxValue value, float time, float fallback, System.Random random = null)
        {
            if (value == null || !value.Present) return fallback;
            return Component(value, 1, 0, time, fallback) * Random(value, 0, random);
        }

        public static Vector2 Vector2(RiotVfxValue value, float time, Vector2 fallback, System.Random random = null)
        {
            if (value == null || !value.Present) return fallback;
            return new Vector2(Component(value, 2, 0, time, fallback.x) * Random(value, 0, random),
                Component(value, 2, 1, time, fallback.y) * Random(value, 1, random));
        }

        public static Vector3 Vector3(RiotVfxValue value, float time, Vector3 fallback, System.Random random = null)
        {
            if (value == null || !value.Present) return fallback;
            return new Vector3(Component(value, 3, 0, time, fallback.x) * Random(value, 0, random),
                Component(value, 3, 1, time, fallback.y) * Random(value, 1, random),
                Component(value, 3, 2, time, fallback.z) * Random(value, 2, random));
        }

        public static Color Color(RiotVfxValue value, float time, Color fallback, System.Random random = null)
        {
            if (value == null || !value.Present) return fallback;
            return new Color(Component(value, 4, 0, time, fallback.r) * Random(value, 0, random),
                Component(value, 4, 1, time, fallback.g) * Random(value, 1, random),
                Component(value, 4, 2, time, fallback.b) * Random(value, 2, random),
                Component(value, 4, 3, time, fallback.a) * Random(value, 3, random));
        }

        // LoL -> Unity axes: the character importer mirrors X, so vectors flip X and
        // Euler rotations (Unity's Z, X, Y order) flip their Y and Z angles.
        public static Vector3 Point(Vector3 lol) { return new Vector3(-lol.x, lol.y, lol.z); }
        public static Quaternion Rotation(Vector3 lolEulerDegrees)
        { return Quaternion.Euler(lolEulerDegrees.x, -lolEulerDegrees.y, -lolEulerDegrees.z); }
    }
}
