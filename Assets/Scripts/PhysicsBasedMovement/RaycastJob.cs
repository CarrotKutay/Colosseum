﻿using Unity.Jobs;
using Unity.Collections;
using Unity.Physics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;


public struct RaycastJob : IJob
{
    [ReadOnly] public CollisionWorld world;
    public NativeArray<RaycastHit> results;
    public Entity Entity;
    [ReadOnly] public ComponentDataFromEntity<LocalToWorld> getPlayerLocalToWorld;
    [ReadOnly] public ComponentDataFromEntity<Translation> getPlayerPosition;

    public void Execute()
    {
        // get player data
        var LocalToWorld = getPlayerLocalToWorld[Entity];
        var playerForward = LocalToWorld.Forward;
        var playerRight = LocalToWorld.Right;
        var playerPosition = getPlayerPosition[Entity].Value;

        var rayDirection = math.normalizesafe(math.cross(playerRight, playerForward));

        BitField32 filter = new BitField32();
        filter.SetBits(1, true, 31);
        filter.SetBits(0, false);

        RaycastInput input = new RaycastInput()
        {
            Start = playerPosition,
            End = playerPosition + rayDirection,
            Filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = filter.GetBits(0, 32), // all 1s, so all layers, exept layer 0 = player layer
                GroupIndex = 0
            }
        };

        RaycastHit hit;
        world.CastRay(input, out hit);
        results[0] = hit;

    }
}