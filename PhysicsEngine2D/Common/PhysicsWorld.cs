﻿using System.Collections.Generic;
using System.Net.NetworkInformation;
using System;

namespace PhysicsEngine2D
{
    public class PhysicsWorld
    {
        public static Vec2 gravity = new Vec2(0, -9.8f);

        public static bool bruteForce = false;

        public List<Body> bodies = new List<Body>();

        public float timeScale = 1;

        internal HashSet<Manifold> manifolds = new HashSet<Manifold>();

        private IBroadphase broadphase;

        public int ManifoldCount
        {
            get
            {
                return manifolds.Count;
            }
        }

        private int maxIterations = 7;

        public PhysicsWorld()
        {

            // Switch the collision system here:
            //broadphase = new CollisionSystemSap();
            broadphase = new DynamicBoundsTree();
        }

        public void AddBody(Body b)
        {
            bodies.Add(b);
            broadphase.Add(b);
        }

        public void RemoveBody(Body b)
        {
            bodies.Remove(b);
            broadphase.Remove(b);
        }

        public void Clear()
        {
            bodies.Clear();
            manifolds.Clear();
            broadphase.Clear();
        }

        public bool Raycast(Vec2 origin, Vec2 direction, out RaycastResult result)
        {
            return Raycast(origin, direction, Ray2.Tmax, out result);
        }

        public bool Raycast(Vec2 origin, Vec2 direction, float distance, out RaycastResult result)
        {
            return Raycast(new Ray2(origin, direction), distance, out result);
        }

        public bool Raycast(Ray2 ray, out RaycastResult result)
        {
            return Raycast(ray, Ray2.Tmax, out result);
        }

        public bool Raycast(Ray2 ray, float distance, out RaycastResult result)
        {
            return broadphase.Raycast(ray, distance, out result);
        }

        public void Update(float dt)
        {
            dt *= timeScale;

            foreach (Body b in bodies)
                b.Update();

            broadphase.Update(bodies);

            if (bruteForce)
            {
                //Obsolete Brute Force Collisions
                for (int i = 0; i < bodies.Count - 1; i++)
                {
                    for (int j = i + 1; j < bodies.Count; j++)
                    {
                        Manifold key = new Manifold(bodies[i], bodies[j]);
                        if (!manifolds.Contains(key))
                            manifolds.Add(key);
                    }
                }
            }
            else
            {
                //Broad phase
                broadphase.ComputePairs(bodies, manifolds);
            }

            //Narrow phase
            foreach (Manifold m in manifolds)
                m.SolveContacts();

            foreach (Body b in bodies)
                b.IntegrateForces(dt);

            foreach (Manifold m in manifolds)
                m.PreStep(1 / dt);

            //Process maxIterations times for more stability
            for (int i = 0; i < maxIterations; i++)
                foreach (Manifold m in manifolds)
                    m.ApplyImpulse();

            //Integrate positions
            foreach (Body b in bodies)
                b.IntegrateVelocity(dt);
        }

        public void DebugDraw(IDebugDrawer debugDrawer)
        {
            broadphase.DebugDraw(debugDrawer);
        }
    }
}
