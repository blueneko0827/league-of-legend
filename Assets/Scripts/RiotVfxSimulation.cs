using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public struct RiotVfxPose
    {
        public Vector3 position;
        public Quaternion rotation;
        // Beam end point (VfxPrimitiveBeam), e.g. the other E blade. World space.
        public Vector3 target;
        public bool hasTarget;
        public RiotVfxPose(Vector3 position, Quaternion rotation) { this.position = position; this.rotation = rotation; target = position; hasTarget = false; }
        public RiotVfxPose(Vector3 position, Quaternion rotation, Vector3 target)
        { this.position = position; this.rotation = rotation; this.target = target; hasTarget = true; }
    }

    public struct RiotVfxView
    {
        public Vector3 position, forward, up, right;
        public RiotVfxView(Vector3 position, Quaternion rotation)
        {
            this.position = position; forward = rotation * Vector3.forward; up = rotation * Vector3.up; right = rotation * Vector3.right;
        }
    }

    public enum RiotVfxDrawKind { Quad, Mesh, Trail, AttachedMesh }

    // One draw call. Trails carry their own vertices (world space, Unity units).
    public struct RiotVfxDraw
    {
        public RiotVfxDrawKind kind;
        public RiotVfxEmitterData emitter;
        public Matrix4x4 matrix;
        public Color color;
        public Vector4 uv;          // xy = scale, zw = offset, in LoL texture space (v down)
        public Vector4 uv2;         // same, for the multiplied texture (textureMult)
        public float erosion;       // erosion drive, 0 = intact
        public int trailStart, trailCount;
    }

    // Pure particle simulation of one VfxSystemDefinitionData. It has no Unity objects, so it
    // can be stepped and inspected outside the engine; RiotVfxPlayer renders its draws.
    // Positions are simulated in LoL units on Unity axes, inside the emitter frame:
    // the live emitter pose for bound emitters (bindWeight), or the pose at birth otherwise.
    public sealed class RiotVfxInstance
    {
        public readonly RiotVfxSystemData Data;
        public float Age { get; private set; }
        public bool Stopped { get; private set; }
        public bool Finished { get; private set; }
        public float UnitScale = .01f;          // Unity units per LoL unit
        public float BoundSize = 100;           // FlexShape "bound object size", LoL units
        public float GroundHeight = 0;
        // A missile's particles live in a frame whose Y axis is the flight (the pose faces it):
        // Irelia_Base_R_Mis lays its blades along Y and trails its glows at negative Y, which is
        // how they fly in the game footage, point first. Its ground-layer parts (the rings and
        // swirl left where it starts) keep the upright frame.
        public bool MissileFrame;
        static readonly Quaternion MissileTilt = Quaternion.Euler(90, 0, 0);
        public readonly List<Vector3> TrailPositions = new List<Vector3>();
        public readonly List<Vector2> TrailUvs = new List<Vector2>();
        public readonly List<Color> TrailColors = new List<Color>();
        // Finds the systems particles spawn (childParticleSetDefinition); null disables children.
        public System.Func<string, RiotVfxSystemData> ResolveChild;
        readonly Emitter[] emitters;
        // Children whose parent particle died keep playing out their linger.
        readonly List<RiotVfxInstance> orphans = new List<RiotVfxInstance>();
        readonly System.Random random;
        RiotVfxPose pose;
        bool posed;

        sealed class Particle
        {
            public float age, life;
            public Vector3 position, velocity, drag, rotation, rotationalVelocity, scale, orbital;
            public List<RiotVfxInstance> children;
            public Color color;
            public Vector2 uvOffset, uvRate, multOffset, multRate;
            public int frame;
            public RiotVfxPose frameAtBirth;
        }

        sealed class TrailPoint { public Vector3 position; public float age, life; }

        sealed class Emitter
        {
            public RiotVfxEmitterData data;
            public float accumulator;
            public bool emittedSingle, stopped, started;
            public readonly List<Particle> particles = new List<Particle>();
            public readonly List<TrailPoint> trail = new List<TrailPoint>();
            public bool Bound { get { return data.bindWeight >= .5f; } }
        }

        public RiotVfxInstance(RiotVfxSystemData data, int seed)
        {
            Data = data; random = new System.Random(seed);
            var list = new List<Emitter>();
            if (data.emitters != null)
                foreach (RiotVfxEmitterData e in data.emitters)
                    if (e != null && !e.Skipped) list.Add(new Emitter { data = e });
            emitters = list.ToArray();
        }

        public int ParticleCount
        {
            get
            {
                int n = 0;
                foreach (Emitter e in emitters)
                {
                    n += e.particles.Count + e.trail.Count;
                    foreach (Particle p in e.particles) if (p.children != null) foreach (RiotVfxInstance c in p.children) n += c.ParticleCount;
                }
                foreach (RiotVfxInstance c in orphans) n += c.ParticleCount;
                return n;
            }
        }

        // Stops emission. Particles keep living for their linger time, or vanish without one.
        public void Stop()
        {
            if (Stopped) return;
            Stopped = true;
            foreach (Emitter e in emitters)
            {
                foreach (Particle p in e.particles) ReleaseChildren(p);
                if (e.data.linger < 0) { e.particles.Clear(); e.trail.Clear(); continue; }
                foreach (Particle p in e.particles) p.life = Mathf.Min(p.life, p.age + e.data.linger);
                foreach (TrailPoint p in e.trail) p.life = Mathf.Min(p.life, p.age + e.data.linger);
            }
        }

        // Ends the emitters that ride on the owner (bindWeight) and have no linger, e.g. a dash's
        // leading edge when the dash arrives; world-space parts (shockwaves, trails) play out.
        public void StopBound()
        {
            foreach (Emitter e in emitters)
            {
                if (!e.Bound || e.data.linger >= 0) continue;
                foreach (Particle p in e.particles) ReleaseChildren(p);
                e.emittedSingle = true; e.particles.Clear(); e.trail.Clear(); e.stopped = true;
            }
        }

        public void Tick(float dt, RiotVfxPose current)
        {
            pose = current; posed = true;
            if (Finished) return;
            dt = Mathf.Max(0, dt);
            float previous = Age; Age += dt;
            bool anyAlive = false;
            foreach (Emitter e in emitters)
            {
                RiotVfxEmitterData d = e.data;
                float local = Age - d.delay, before = previous - d.delay;
                // Existing particles advance first; new ones start at their exact age in this frame.
                Advance(e, dt);
                bool active = !Stopped && !e.stopped && local >= 0 && (d.lifetime < 0 || before < d.lifetime);
                if (active)
                {
                    float duration = d.lifetime > 0 ? d.lifetime : 1;
                    float emitterTime = Mathf.Clamp01(Mathf.Max(0, local) / duration);
                    if (d.single)
                    {
                        if (!e.emittedSingle) { e.emittedSingle = true; Spawn(e, emitterTime, Mathf.Max(0, local)); }
                    }
                    else
                    {
                        // Only the emitter's active window produces particles.
                        float start = Mathf.Max(0, before), end = d.lifetime < 0 ? local : Mathf.Min(local, d.lifetime);
                        float rate = Mathf.Max(0, RiotVfxEval.Float(d.rate, emitterTime, 0));
                        // The first particle is born when the emitter starts (W_DR's shield is there at
                        // once in LoL), not one emission interval later.
                        if (!e.started) { e.started = true; if (rate > 0) e.accumulator += 1; }
                        e.accumulator += Mathf.Max(0, end - start) * rate;
                        int count = Mathf.Min(64, Mathf.FloorToInt(e.accumulator));
                        e.accumulator -= count;
                        for (int i = 0; i < count; i++)
                        {
                            // Spread births across the frame so fast emitters stay continuous.
                            float spawnAge = (count - 1 - i) / (float)Mathf.Max(1, count) * (end - start);
                            Spawn(e, emitterTime, spawnAge);
                        }
                    }
                }
                if (e.particles.Count > 0 || e.trail.Count > 0) anyAlive = true;
                if (!Stopped && !e.stopped && (d.lifetime < 0 || Age - d.delay < d.lifetime)) anyAlive = true;
            }
            for (int i = orphans.Count - 1; i >= 0; i--)
            {
                orphans[i].Tick(dt, orphans[i].pose);
                if (orphans[i].Finished) orphans.RemoveAt(i); else anyAlive = true;
            }
            if (!anyAlive) Finished = true;
        }

        void Spawn(Emitter e, float emitterTime, float initialAge)
        {
            RiotVfxEmitterData d = e.data;
            float offsetScale = d.flexOffset > 0 ? d.flexOffset * BoundSize : 1;
            Vector3 offset = RiotVfxEval.Vector3(d.emitterPosition, emitterTime, Vector3.zero) +
                RiotVfxEval.Vector3(d.shapeOffset, emitterTime, Vector3.zero, random) * offsetScale + ShapePoint(d) * offsetScale;
            Vector3 velocity = RiotVfxEval.Vector3(d.birthVelocity, emitterTime, Vector3.zero, random);
            if (d.shapeRotationAngles != null && d.shapeRotationAxes != null)
                for (int i = 0; i < d.shapeRotationAngles.Length && i * 3 + 2 < d.shapeRotationAxes.Length; i++)
                {
                    Vector3 axis = new Vector3(d.shapeRotationAxes[i * 3], d.shapeRotationAxes[i * 3 + 1], d.shapeRotationAxes[i * 3 + 2]);
                    Quaternion spin = Quaternion.AngleAxis(RiotVfxEval.Float(d.shapeRotationAngles[i], emitterTime, 0, random), axis);
                    offset = spin * offset; velocity = spin * velocity;
                }
            // VfxShapeSphere flag 1: each particle leaves in a random direction (its birth velocity
            // turned uniformly over the sphere), e.g. the rays that burst out of Irelia at four
            // passive stacks.
            if (d.shape == "sphere" && (d.shapeFlags & 1) != 0)
            {
                Vector3 direction;
                do direction = new Vector3(Signed(), Signed(), Signed()); while (direction.sqrMagnitude > 1 || direction.sqrMagnitude < 1e-4f);
                velocity = Quaternion.FromToRotation(Vector3.right, direction.normalized) * velocity;
            }
            // A negative particle lifetime means the particle lives until the system stops.
            float life = RiotVfxEval.Float(d.particleLifetime, emitterTime, 1, random);
            life = life < 0 ? float.PositiveInfinity : Mathf.Max(.01f, life);
            if (d.IsTrail)
            {
                e.trail.Add(new TrailPoint { position = FramePoint(Frame(e), RiotVfxEval.Point(offset)), life = life, age = initialAge });
                return;
            }
            float scaleFactor = d.flexScale > 0 ? d.flexScale * BoundSize : 1;
            var p = new Particle
            {
                life = life,
                position = RiotVfxEval.Point(offset), velocity = RiotVfxEval.Point(velocity),
                drag = RiotVfxEval.Vector3(d.birthDrag, emitterTime, Vector3.zero, random),
                rotation = RiotVfxEval.Vector3(d.birthRotation, emitterTime, Vector3.zero, random),
                rotationalVelocity = RiotVfxEval.Vector3(d.rotationalVelocity, emitterTime, Vector3.zero, random),
                scale = RiotVfxEval.Vector3(d.birthScale, emitterTime, Vector3.one, random) * scaleFactor,
                color = RiotVfxEval.Color(d.birthColor, emitterTime, Color.white, random),
                uvOffset = RiotVfxEval.Vector2(d.birthUvOffset, emitterTime, Vector2.zero, random),
                uvRate = RiotVfxEval.Vector2(d.birthUvScroll, emitterTime, Vector2.zero, random),
                orbital = Pseudo(RiotVfxEval.Vector3(d.orbitalVelocity, emitterTime, Vector3.zero, random)),
                frameAtBirth = Frame(e)
            };
            if (d.HasChildren && ResolveChild != null)
                foreach (string name in d.children)
                {
                    RiotVfxSystemData child = ResolveChild(name);
                    if (child == null) continue;
                    if (p.children == null) p.children = new List<RiotVfxInstance>();
                    p.children.Add(new RiotVfxInstance(child, random.Next())
                    { UnitScale = UnitScale, BoundSize = BoundSize, GroundHeight = GroundHeight, ResolveChild = ResolveChild });
                }
            if (d.textureMult != null && d.textureMult.Present)
            {
                p.multOffset = RiotVfxEval.Vector2(d.textureMult.birthUvOffset, emitterTime, Vector2.zero, random);
                p.multRate = RiotVfxEval.Vector2(d.textureMult.birthUvScroll, emitterTime, Vector2.zero, random);
            }
            int frames = Mathf.Max(1, d.numFrames);
            p.frame = d.startFrame + (d.randomStartFrame ? random.Next(frames) : 0);
            e.particles.Add(p);
            if (initialAge > 0) Step(e, p, initialAge);
            TickChildren(e, p, 0);
        }

        // Angular velocities are pseudo-vectors: the X mirror flips their Y and Z.
        static Vector3 Pseudo(Vector3 lol) { return new Vector3(lol.x, -lol.y, -lol.z); }

        // The emitter's frame now: the live pose, tilted onto the flight for a missile's particles.
        RiotVfxPose Frame(Emitter e)
        {
            if (!MissileFrame || e.data.groundLayer) return pose;
            RiotVfxPose frame = pose; frame.rotation = pose.rotation * MissileTilt;
            return frame;
        }

        // A particle's children ride on it: their emitter frame is the particle's world pose.
        void TickChildren(Emitter e, Particle p, float dt)
        {
            if (p.children == null) return;
            RiotVfxPose frame = e.Bound ? Frame(e) : p.frameAtBirth;
            var at = new RiotVfxPose(FramePoint(frame, p.position), frame.rotation);
            foreach (RiotVfxInstance child in p.children) child.Tick(dt, at);
        }

        void ReleaseChildren(Particle p)
        {
            if (p.children == null) return;
            foreach (RiotVfxInstance child in p.children) { child.Stop(); if (!child.Finished) orphans.Add(child); }
            p.children = null;
        }

        Vector3 ShapePoint(RiotVfxEmitterData d)
        {
            if (d.shape == "box" && d.shapeSize != null && d.shapeSize.Length >= 3)
                return new Vector3(Signed() * d.shapeSize[0], Signed() * d.shapeSize[1], Signed() * d.shapeSize[2]);
            if (d.shape == "sphere" && d.shapeRadius > 0)
            {
                Vector3 v;
                do v = new Vector3(Signed(), Signed(), Signed()); while (v.sqrMagnitude > 1);
                return v * d.shapeRadius;
            }
            if (d.shape == "cylinder" && d.shapeRadius > 0)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                return new Vector3(Mathf.Cos(angle) * d.shapeRadius, (float)random.NextDouble() * d.shapeHeight, Mathf.Sin(angle) * d.shapeRadius);
            }
            return Vector3.zero;
        }

        float Signed() { return (float)random.NextDouble() * 2 - 1; }

        void Advance(Emitter e, float dt)
        {
            for (int i = e.particles.Count - 1; i >= 0; i--)
            {
                Particle p = e.particles[i];
                Step(e, p, dt);
                TickChildren(e, p, dt);
                if (p.age >= p.life) { ReleaseChildren(p); e.particles.RemoveAt(i); }
            }
            for (int i = e.trail.Count - 1; i >= 0; i--)
            {
                e.trail[i].age += dt;
                if (e.trail[i].age >= e.trail[i].life) e.trail.RemoveAt(i);
            }
        }

        void Step(Emitter e, Particle p, float dt)
        {
            RiotVfxEmitterData d = e.data;
            float t = Mathf.Clamp01(p.age / p.life);
            RiotVfxPose frame = e.Bound ? Frame(e) : p.frameAtBirth;
            // worldAcceleration is in world axes; bring it into the particle's frame.
            Vector3 acceleration = RiotVfxEval.Point(RiotVfxEval.Vector3(d.acceleration, t, Vector3.zero));
            if (acceleration != Vector3.zero) p.velocity += Quaternion.Inverse(frame.rotation) * acceleration * dt;
            Vector3 extra = RiotVfxEval.Point(RiotVfxEval.Vector3(d.velocity, t, Vector3.zero));
            if (p.drag != Vector3.zero)
                p.velocity = new Vector3(p.velocity.x * Mathf.Exp(-p.drag.x * dt), p.velocity.y * Mathf.Exp(-p.drag.y * dt), p.velocity.z * Mathf.Exp(-p.drag.z * dt));
            p.position += (p.velocity + extra) * dt;
            if (p.orbital != Vector3.zero)
                p.position = Quaternion.AngleAxis(p.orbital.magnitude * dt * Mathf.Rad2Deg, p.orbital) * p.position;
            p.uvOffset += RiotVfxEval.Vector2(d.uvScroll, t, Vector2.zero) * dt + p.uvRate * dt;
            p.multOffset += p.multRate * dt;
            p.age += dt;
        }

        static Vector3 FramePoint(RiotVfxPose frame, Vector3 local, float unitScale)
        { return frame.position + frame.rotation * (local * unitScale); }
        Vector3 FramePoint(RiotVfxPose frame, Vector3 local) { return FramePoint(frame, local, UnitScale); }

        public void Collect(List<RiotVfxDraw> draws, RiotVfxView view)
        {
            TrailPositions.Clear(); TrailUvs.Clear(); TrailColors.Clear();
            CollectInto(draws, view, this);
        }

        // Children write their trail vertices into the root instance, which the renderer reads.
        void CollectInto(List<RiotVfxDraw> draws, RiotVfxView view, RiotVfxInstance root)
        {
            if (!posed) return;
            foreach (Emitter e in emitters)
            {
                if (e.data.IsTrail) { CollectTrail(e, draws, view, root); continue; }
                foreach (Particle p in e.particles)
                {
                    draws.Add(Draw(e, p, view));
                    if (p.children != null) foreach (RiotVfxInstance child in p.children) child.CollectInto(draws, view, root);
                }
            }
            foreach (RiotVfxInstance child in orphans) child.CollectInto(draws, view, root);
        }

        RiotVfxDraw Draw(Emitter e, Particle p, RiotVfxView view)
        {
            RiotVfxEmitterData d = e.data;
            float t = Mathf.Clamp01(p.age / p.life);
            RiotVfxPose frame = e.Bound ? Frame(e) : p.frameAtBirth;
            Quaternion orientation = d.localOrientation ? frame.rotation : Quaternion.identity;
            Vector3 curve = RiotVfxEval.Vector3(d.scale, t, Vector3.one);
            Vector3 size = new Vector3(p.scale.x * curve.x, p.scale.y * curve.y, p.scale.z * curve.z);
            if (d.uniformScale) size = Vector3.one * (p.scale.x * curve.x);
            Vector3 euler = p.rotation + p.rotationalVelocity * p.age + RiotVfxEval.Vector3(d.rotation, t, Vector3.zero);
            Color color = p.color * RiotVfxEval.Color(d.color, t, Color.white) * RiotVfxEval.Color(d.colorRamp, t, Color.white);
            Vector3 center = FramePoint(frame, p.position);
            var draw = new RiotVfxDraw
            {
                emitter = d, color = color, uv = UvTransform(d, p, t), uv2 = MultTransform(d, p),
                erosion = d.erosion != null && !string.IsNullOrEmpty(d.erosion.map) ? RiotVfxEval.Float(d.erosion.drive, t, 0) : 0
            };
            if (d.IsBeam)
            {
                // The beam mesh (authored along Z, beamLength long, centered) spans emitter -> target.
                Vector3 end = pose.hasTarget ? pose.target : center;
                if (d.groundLayer) { center.y = GroundHeight + .02f; end.y = GroundHeight + .02f; }
                Vector3 span = end - center; float distance = span.magnitude;
                Vector3 axis = distance > 1e-4f ? span / distance : frame.rotation * Vector3.forward;
                float length = d.beamLength > 1e-3f ? d.beamLength : 1;
                draw.kind = RiotVfxDrawKind.Mesh;
                draw.matrix = Matrix4x4.TRS((center + end) * .5f, Quaternion.LookRotation(axis, Vector3.up),
                    new Vector3(size.x * UnitScale, size.y * UnitScale, distance / length));
                return draw;
            }
            if (d.IsRay)
            {
                // Starts at the particle and runs along its rotated +Z, turned to face the camera.
                Vector3 along = orientation * RiotVfxEval.Rotation(euler) * Vector3.forward;
                Vector3 across = Vector3.Cross(along, view.forward);
                across = across.sqrMagnitude > 1e-6f ? across.normalized : view.right;
                float rayLength = size.y * UnitScale;
                var ray = new Matrix4x4();
                ray.SetColumn(0, ToVector4(across * (size.x * UnitScale), 0));
                ray.SetColumn(1, ToVector4(along * (rayLength * .5f), 0));
                ray.SetColumn(2, ToVector4(Vector3.Cross(across, along).normalized, 0));
                ray.SetColumn(3, ToVector4(center + along * (rayLength * .5f), 1));
                draw.kind = RiotVfxDrawKind.Quad; draw.matrix = ray;
                return draw;
            }
            if (d.primitive == "mesh" || d.primitive == "attachedMesh")
            {
                draw.kind = d.primitive == "mesh" ? RiotVfxDrawKind.Mesh : RiotVfxDrawKind.AttachedMesh;
                draw.matrix = Matrix4x4.TRS(center, orientation * RiotVfxEval.Rotation(euler), size * UnitScale);
                return draw;
            }
            draw.kind = RiotVfxDrawKind.Quad;
            // Direction-oriented quads of a random-direction burst (VfxShapeSphere flag 1, e.g. the
            // rays of Irelia's four-stack passive) are drawn as streaks along their flight, facing
            // the camera, so they radiate from the center like spokes as in LoL. Drawn as world
            // quads (their birth rotation lays them flat) they formed a pale patch on the ground.
            bool spoke = d.primitive == "quad" && d.directionOriented && d.shape == "sphere" && (d.shapeFlags & 1) != 0;
            if (d.IsBillboard || spoke)
            {
                Vector3 right = view.right, up = view.up, normal = -view.forward;
                if (d.groundLayer)
                {
                    center.y = GroundHeight + .02f;
                    right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
                    up = Vector3.Cross(right, Vector3.up).normalized;
                    normal = Vector3.up;
                }
                float height = d.uniformScale ? size.x : size.y;
                Vector3 worldVelocity = frame.rotation * p.velocity;
                Vector3 along = Vector3.ProjectOnPlane(worldVelocity, normal);
                if (d.directionOriented && along.sqrMagnitude > 1e-6f)
                {
                    // Stretched along the screen-projected velocity (sparks, blood streaks).
                    up = along.normalized; right = Vector3.Cross(normal, up).normalized;
                    height += worldVelocity.magnitude * d.directionVelocityScale;
                }
                else
                {
                    // 2D particles spin around the view axis with the X angle (mirrored like Y/Z).
                    Quaternion spin = Quaternion.AngleAxis(euler.x, normal);
                    right = spin * right; up = spin * up;
                }
                var m = new Matrix4x4();
                m.SetColumn(0, ToVector4(right * (size.x * UnitScale), 0));
                m.SetColumn(1, ToVector4(up * (height * UnitScale), 0));
                m.SetColumn(2, ToVector4(normal, 0));
                m.SetColumn(3, ToVector4(center, 1));
                draw.matrix = m;
                return draw;
            }
            Quaternion rotation = orientation * RiotVfxEval.Rotation(euler);
            if (d.groundLayer)
            {
                // Ground-layer quads lie flat, turned by their Z angle. LoL draws their texture a
                // quarter turn from other quads: its top runs along the first size (birthScale X).
                // Measured on the W release in the game footage: the arrowhead of streaks
                // (W_Swipe*/Glow, Glow1; Z 90) points at the target and stretches along its
                // flight; read like other quads it pointed sideways, across the cut. The other
                // ground quads (E ring, ripples) are round, so they look the same either way.
                center.y = GroundHeight + .02f;
                rotation = orientation * Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, 0, 90 - euler.z);
                size = new Vector3(size.y, size.x, size.z);
            }
            draw.matrix = Matrix4x4.TRS(center, rotation, new Vector3(size.x, size.y, 1) * UnitScale);
            return draw;
        }

        static Vector4 ToVector4(Vector3 v, float w) { return new Vector4(v.x, v.y, v.z, w); }

        static Vector4 UvTransform(RiotVfxEmitterData d, Particle p, float t)
        {
            float columns = d.texDiv != null && d.texDiv.Length > 0 ? Mathf.Max(1, d.texDiv[0]) : 1;
            float rows = d.texDiv != null && d.texDiv.Length > 1 ? Mathf.Max(1, d.texDiv[1]) : 1;
            int frames = d.numFrames > 0 ? d.numFrames : Mathf.RoundToInt(columns * rows);
            int frame = p.frame;
            if (frames > 1 && !d.randomStartFrame)
                frame += d.frameRate > 0 ? Mathf.FloorToInt(p.age * d.frameRate) : Mathf.Min(frames - 1, Mathf.FloorToInt(t * frames));
            frame = frames > 0 ? ((frame % frames) + frames) % frames : 0;
            int column = frame % Mathf.RoundToInt(columns), row = frame / Mathf.RoundToInt(columns);
            return new Vector4(1 / columns, 1 / rows, column / columns + p.uvOffset.x, row / rows + p.uvOffset.y);
        }

        static Vector4 MultTransform(RiotVfxEmitterData d, Particle p)
        {
            RiotVfxTextureMult mult = d.textureMult;
            if (mult == null || !mult.Present) return new Vector4(1, 1, 0, 0);
            float columns = mult.texDiv != null && mult.texDiv.Length > 0 ? Mathf.Max(1, mult.texDiv[0]) : 1;
            float rows = mult.texDiv != null && mult.texDiv.Length > 1 ? Mathf.Max(1, mult.texDiv[1]) : 1;
            return new Vector4(1 / columns, 1 / rows, p.multOffset.x, p.multOffset.y);
        }

        void CollectTrail(Emitter e, List<RiotVfxDraw> draws, RiotVfxView view, RiotVfxInstance root)
        {
            RiotVfxEmitterData d = e.data;
            if (e.trail.Count < 2) return;
            float tiling = Mathf.Max(1, RiotVfxEval.Vector3(d.trailTiling, 0, Vector3.zero).x);
            Vector2 scroll = RiotVfxEval.Vector2(d.birthUvScroll, 0, Vector2.zero);
            Vector3 frameUp = pose.rotation * Vector3.up;
            List<Vector3> positions = root.TrailPositions; List<Vector2> uvs = root.TrailUvs; List<Color> colors = root.TrailColors;
            int start = positions.Count;
            float distance = 0;
            // Newest point first: the head of the ribbon is at the emitter.
            for (int i = e.trail.Count - 1; i >= 0; i--)
            {
                TrailPoint point = e.trail[i];
                if (i < e.trail.Count - 1) distance += Vector3.Distance(point.position, e.trail[i + 1].position) / UnitScale;
                Vector3 neighbour = i > 0 ? e.trail[i - 1].position : e.trail[i + 1].position;
                Vector3 tangent = i > 0 ? point.position - neighbour : neighbour - point.position;
                float t = Mathf.Clamp01(point.age / point.life);
                // Camera trails face the view; arbitrary trails (the Q streak) lie flat across the
                // path, which is how the LoL top-down view shows them.
                Vector3 axis = d.primitive == "cameraTrail" ? Vector3.Cross(tangent, view.forward) : Vector3.Cross(frameUp, tangent);
                axis = axis.sqrMagnitude > 1e-8f ? axis.normalized : pose.rotation * Vector3.right;
                float width = RiotVfxEval.Vector3(d.birthScale, 0, Vector3.one).x * RiotVfxEval.Vector3(d.scale, t, Vector3.one).x * UnitScale;
                Color color = RiotVfxEval.Color(d.birthColor, 0, Color.white) * RiotVfxEval.Color(d.color, t, Color.white);
                float u = distance / tiling + scroll.x * Age;
                positions.Add(point.position + axis * (width * .5f)); uvs.Add(new Vector2(u, 0)); colors.Add(color);
                positions.Add(point.position - axis * (width * .5f)); uvs.Add(new Vector2(u, 1)); colors.Add(color);
            }
            draws.Add(new RiotVfxDraw
            {
                kind = RiotVfxDrawKind.Trail, emitter = d, matrix = Matrix4x4.identity, color = Color.white,
                uv = new Vector4(1, 1, 0, 0), uv2 = new Vector4(1, 1, 0, 0), trailStart = start, trailCount = positions.Count - start
            });
        }
    }
}
