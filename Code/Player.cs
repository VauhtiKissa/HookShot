using System;
using Godot;

public partial class Player : CharacterBody2D
{
    [Export]
    public Vector2 hookPosition = Vector2.Zero;

    [Export]
    public float maximumRadius = 250;
    private float currentRadius = 250;

    [Export]
    public float climbingSpeed = 10;

    [Export]
    public float swingSpeed = 10;
    public bool hooked = false;
    public Vector2 pushForce;
    private RayCast2D hookRaycast;
    private Line2D hookRope;
    private Line2D rangeIndicator;
    private TileMap tileMap;

    const int rangeIndicatorPointCount = 128;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _Ready()
    {
        hookRaycast = GetNode<RayCast2D>("./HookRayCast2D");
        hookRaycast.TargetPosition = new Vector2(maximumRadius, 0);
        hookRope = GetNode<Line2D>("./Rope");

        tileMap = GetNode<TileMap>("../groundTileMap");

        rangeIndicator = GetNode<Line2D>("./RangeIndicator");
        Vector2[] RangeIndicatorPoints = new Vector2[rangeIndicatorPointCount];

        for (int i = 0; i < rangeIndicatorPointCount; i++)
        {
            RangeIndicatorPoints[i] =
                Vector2.FromAngle(2 * Mathf.Pi / rangeIndicatorPointCount * i) * maximumRadius;
        }
        rangeIndicator.Points = RangeIndicatorPoints;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        velocity += pushForce;
        Vector2 mousePosition = GetLocalMousePosition();

        if (Input.IsActionJustPressed("launch rope"))
        {
            hookRaycast.TargetPosition = mousePosition.Normalized() * maximumRadius;

            hookRaycast.ForceRaycastUpdate();

            if (
                hookRaycast.IsColliding()
                && tileMap
                    .GetCellTileData(
                        -1,
                        tileMap.LocalToMap(
                            (
                                hookRaycast.GetCollisionPoint()
                                - tileMap.Position
                                - hookRaycast.GetCollisionNormal()
                            ) / 8
                        )
                    )
                    .GetCustomData("hookable")
                    .AsBool() == true
            )
            {
                hooked = true;
                hookPosition = hookRaycast.GetCollisionPoint();
                currentRadius = (hookPosition - Position).Length();
                hookRope.Visible = true;
            }
        }

        if (Input.IsActionJustPressed("release rope"))
        {
            hooked = false;
            hookRope.Visible = false;
        }

        Vector2 hook = hookPosition - Position;

        float distance = hook.Length();

        if (Input.IsActionPressed("right") && hooked && !IsOnFloor())
        {
            velocity += Vector2.FromAngle(hook.Angle() + 0.5f * Mathf.Pi) * swingSpeed;
        }
        if (Input.IsActionPressed("left") && hooked && !IsOnFloor())
        {
            velocity += Vector2.FromAngle(hook.Angle() + 0.5f * Mathf.Pi) * -swingSpeed;
        }

        // rope pulling
        if (Input.IsActionPressed("up") && hooked && !IsOnCeiling())
        {
            hookRaycast.TargetPosition = hook * maximumRadius;
            hookRaycast.ForceRaycastUpdate();
            float miniumLength = 0;
            if (hookRaycast.IsColliding())
            {
                miniumLength = (hookRaycast.GetCollisionPoint() - hookPosition).Length() + 32;
            }
            currentRadius -= climbingSpeed;
            currentRadius = Mathf.Max(currentRadius, miniumLength);
        }

        velocity += Vector2.Down * gravity;

        // -1 to get rid of floating point error edge cases
        if (distance >= currentRadius - 1 && hooked)
        {
            Position += hook * (1 - currentRadius / distance);

            // remove the component of velocity that opposes the rope
            float angleToVelocity = Mathf.Abs(velocity.Angle() - hook.Angle());

            if (angleToVelocity > 0.5f * Mathf.Pi)
            {
                float opposingForceMagnitude =
                    Mathf.Cos(Mathf.Pi - angleToVelocity) * velocity.Length();
                velocity -= hook.Normalized() * -opposingForceMagnitude;
            }
        }

        // rope loosening
        if (
            Input.IsActionPressed("down")
            && distance >= currentRadius - 1
            && hooked
            && currentRadius <= maximumRadius
            && !IsOnFloor()
        )
        {
            float change = Mathf.Min(currentRadius + climbingSpeed, maximumRadius) - currentRadius;
            currentRadius += change;
            Position += hook.Normalized() * -change;
        }

        if (IsOnFloor())
        {
            velocity = new Vector2(velocity.X * 0.9f, velocity.Y);
        }

        Velocity = velocity;
        MoveAndSlide();

        hookRope.Points = new Vector2[] { new Vector2(0, 0), hookPosition - Position };
    }
}
