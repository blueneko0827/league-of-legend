using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    // Short-lived routes around groups of bodies; local steering still validates every step.
    public sealed class CrowdPathfinding
    {
        sealed class Route { public Vector3 Goal, Start; public float RetryAt; public readonly List<Vector3> Points=new List<Vector3>(); }
        readonly Dictionary<Combatant,Route> routes=new Dictionary<Combatant,Route>();
        public void Clear() { routes.Clear(); }
        public void Forget(Combatant actor) { routes.Remove(actor); }
        public static List<Vector3> PreviewRoute(LaneWorld world, Combatant actor, Vector3 start, Vector3 goal)
        {
            if (Free(world, actor, start, goal)) return new List<Vector3> { goal };
            // Farm/risk queries must not run a grid search for every possible destination.
            // Preview both sides of the blocking group; only live navigation uses A*.
            Vector3 forward = (goal - start).normalized, side = Vector3.Cross(forward, Vector3.up);
            float near = float.PositiveInfinity, far = 0, left = 0, right = 0;
            foreach (var unit in world.Units)
            {
                if (unit == null || unit == actor || !unit.Alive || !unit.gameObject.activeInHierarchy ||
                    unit.Blade != null && unit.Blade.IsDashing || unit.Sett != null && unit.Sett.Casting) continue;
                float radius = actor.Radius + unit.Radius + .12f;
                Vector3 offset = unit.Position - start;
                float along = Vector3.Dot(offset, forward), lateral = Vector3.Dot(offset, side);
                if (along < -radius || along > Vector3.Distance(start, goal) + radius || Mathf.Abs(lateral) > radius + 2) continue;
                near = Mathf.Min(near, along - radius); far = Mathf.Max(far, along + radius);
                left = Mathf.Min(left, lateral - radius); right = Mathf.Max(right, lateral + radius);
            }
            var best = new List<Vector3>(); float shortest = float.PositiveInfinity;
            if (float.IsPositiveInfinity(near)) return best;
            for (int direction = 0; direction < 2; direction++)
            {
                float lateral = direction == 0 ? left : right;
                Vector3 entry = world.Clamp(start + forward * Mathf.Max(0, near) + side * lateral, actor.Radius);
                Vector3 exit = world.Clamp(start + forward * far + side * lateral, actor.Radius);
                if (!Free(world, actor, start, entry) || !Free(world, actor, entry, exit) || !Free(world, actor, exit, goal)) continue;
                float length = Vector3.Distance(start, entry) + Vector3.Distance(entry, exit) + Vector3.Distance(exit, goal);
                if (length >= shortest) continue;
                shortest = length; best.Clear(); best.Add(entry); best.Add(exit); best.Add(goal);
            }
            return best;
        }
        public Vector3 Waypoint(LaneWorld world,Combatant actor,Vector3 goal)
        {
            if(Free(world,actor,actor.Position,goal)) { routes.Remove(actor); return goal; }
            Route route;
            if(routes.TryGetValue(actor,out route) && (route.Goal-goal).sqrMagnitude<.75f*.75f)
            {
                if(route.Points.Count>0 && (route.Points[route.Points.Count-1]-actor.Position).sqrMagnitude<=.04f*.04f) return actor.Position;
                for(int i=route.Points.Count-1;i>=0;i--)
                    if((route.Points[i]-actor.Position).sqrMagnitude>.04f*.04f && Free(world,actor,actor.Position,route.Points[i]))
                        return route.Points[i];
                // An enclosed actor must not rebuild the same failed grid every render frame.
                // Direct paths and usable cached legs are checked above, so openings react immediately.
                if (Time.time < route.RetryAt && (actor.Position - route.Start).sqrMagnitude < .25f * .25f) return goal;
            }
            route=Build(world,actor,goal); routes[actor]=route;
            for(int i=route.Points.Count-1;i>=0;i--)
                if(Free(world,actor,actor.Position,route.Points[i])) return route.Points[i];
            return goal;
        }
        struct Entry { public int Index; public float Score; public Entry(int index,float score) { Index=index; Score=score; } }
        sealed class Heap
        {
            readonly List<Entry> data=new List<Entry>();
            public int Count { get { return data.Count; } }
            public void Push(int index,float score)
            {
                var entry=new Entry(index,score); data.Add(entry); int i=data.Count-1;
                while(i>0) { int p=(i-1)/2; if(data[p].Score<=score) break; data[i]=data[p]; i=p; } data[i]=entry;
            }
            public int Pop()
            {
                int result=data[0].Index; Entry last=data[data.Count-1]; data.RemoveAt(data.Count-1);
                if(data.Count==0) return result; int i=0;
                while(i*2+1<data.Count)
                { int c=i*2+1; if(c+1<data.Count && data[c+1].Score<data[c].Score) c++; if(data[c].Score>=last.Score) break; data[i]=data[c]; i=c; }
                data[i]=last; return result;
            }
        }
        struct Obstacle { public Vector3 Point; public float Radius; }
        static List<Obstacle> Obstacles(LaneWorld world,Combatant actor)
        {
            var list=new List<Obstacle>();
            foreach(var unit in world.Units)
                if(unit!=null && unit!=actor && unit.Alive && unit.gameObject.activeInHierarchy &&
                    !(unit.Blade!=null && unit.Blade.IsDashing) && !(unit.Sett!=null && unit.Sett.Casting))
                    list.Add(new Obstacle { Point=unit.Position,Radius=actor.Radius+unit.Radius+.035f });
            return list;
        }
        public static bool Free(LaneWorld world,Combatant actor,Vector3 start,Vector3 end)
        {
            foreach(var unit in world.Units)
            {
                if(unit==null || unit==actor || !unit.Alive || !unit.gameObject.activeInHierarchy ||
                    (unit.Blade!=null && unit.Blade.IsDashing) || (unit.Sett!=null && unit.Sett.Casting)) continue;
                if(!Free(start,end,unit.Position,actor.Radius+unit.Radius+.035f)) return false;
            }
            return true;
        }
        static bool Free(Vector3 start,Vector3 end,Vector3 point,float radius)
        {
            float before=(start-point).sqrMagnitude;
            float swept=CombatRules.SegmentDistanceSquared(point.x,point.z,start.x,start.z,end.x,end.z);
            return before<radius*radius ? swept>=before-.00001f && (end-point).sqrMagnitude>before+.000001f : swept>=radius*radius;
        }
        static bool Free(List<Obstacle> obstacles,Vector3 start,Vector3 end)
        { foreach(var o in obstacles) if(!Free(start,end,o.Point,o.Radius)) return false; return true; }
        static Route Build(LaneWorld world,Combatant actor,Vector3 goal)
        {
            var route=new Route { Goal=goal, Start=actor.Position, RetryAt=Time.time+.2f }; Vector3 start=actor.Position;
            float minX=Mathf.Max(-TowerToTowerRules.HalfLength+actor.Radius,Mathf.Min(start.x,goal.x)-3);
            float maxX=Mathf.Min(TowerToTowerRules.HalfLength-actor.Radius,Mathf.Max(start.x,goal.x)+3);
            float minZ=-world.Settings.halfWidth+actor.Radius,maxZ=world.Settings.halfWidth-actor.Radius;
            int width=Mathf.Max(2,Mathf.CeilToInt((maxX-minX)/.35f)+1),height=Mathf.Max(2,Mathf.CeilToInt((maxZ-minZ)/.35f)+1),count=width*height;
            var points=new Vector3[count]; var valid=new bool[count]; var closed=new bool[count];
            var cost=new float[count]; var parent=new int[count]; var open=new Heap(); var obstacles=Obstacles(world,actor);
            int target=-1; float closest=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                points[i]=new Vector3(Mathf.Lerp(minX,maxX,(i%width)/(float)(width-1)),0,Mathf.Lerp(minZ,maxZ,(i/width)/(float)(height-1)));
                valid[i]=true; cost[i]=float.PositiveInfinity; parent[i]=-1;
                foreach(var o in obstacles) if((points[i]-o.Point).sqrMagnitude<o.Radius*o.Radius) { valid[i]=false; break; }
                if(!valid[i]) continue;
                float remaining=(points[i]-goal).sqrMagnitude;
                if(remaining<closest) { closest=remaining; target=i; }
                if((points[i]-start).sqrMagnitude<=1.2f*1.2f && Free(obstacles,start,points[i]))
                { cost[i]=Vector3.Distance(start,points[i]); open.Push(i,cost[i]+Vector3.Distance(points[i],goal)); }
            }
            if(target<0) return route;
            int best=-1; closest=float.PositiveInfinity;
            while(open.Count>0)
            {
                int current=open.Pop(); if(closed[current]) continue; closed[current]=true;
                float remaining=(points[current]-goal).sqrMagnitude;
                if(remaining<closest) { closest=remaining; best=current; }
                if(current==target) { best=current; break; }
                int x=current%width,z=current/width;
                for(int dz=-1;dz<=1;dz++) for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0 && dz==0 || x+dx<0 || x+dx>=width || z+dz<0 || z+dz>=height) continue;
                    int next=(z+dz)*width+x+dx;
                    if(!valid[next] || closed[next]) continue;
                    float candidate=cost[current]+Vector3.Distance(points[current],points[next]);
                    if(candidate>=cost[next] || !Free(obstacles,points[current],points[next])) continue;
                    parent[next]=current; cost[next]=candidate; open.Push(next,candidate+Vector3.Distance(points[next],goal));
                }
            }
            for(int i=best;i>=0;i=parent[i]) route.Points.Add(points[i]);
            route.Points.Reverse(); return route;
        }
    }
}
