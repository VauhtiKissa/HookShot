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
    private RayCast2D hookRaycast;
    private Line2D hookRope;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _Ready()
    {
        hookRaycast = GetNode<RayCast2D>("./HookRayCast2D");
        hookRaycast.TargetPosition = new Vector2(maximumRadius, 0);
        hookRope = GetNode<Line2D>("./Rope");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        if (Input.IsActionJustPressed("launch rope"))
        {
            Vector2 mousePosition = GetLocalMousePosition();

            hookRaycast.TargetPosition = mousePosition.Normalized() * maximumRadius;

            hookRaycast.ForceRaycastUpdate();

            if (hookRaycast.IsColliding())
            {
                hooked = true;
                hookPosition = hookRaycast.GetCollisionPoint();
                currentRadius = (hookPosition - Position).Length();
                hookRope.Visible = true;
            }
            else
            {
                hooked = false;
                hookRope.Visible = false;
            }
        }

        if (Input.IsActionJustPressed("release rope"))
        {
            hooked = false;
            hookRope.Visible = false;
        }

        Vector2 hook = hookPosition - Position;

        float distance = hook.Length();

        if (Input.IsActionPressed("right") && hooked)
        {
            velocity += Vector2.FromAngle(hook.Angle() + 0.5f * Mathf.Pi) * swingSpeed;
        }
        if (Input.IsActionPressed("left") && hooked)
        {
            velocity += Vector2.FromAngle(hook.Angle() + 0.5f * Mathf.Pi) * -swingSpeed;
        }

        // rope pulling
        if (Input.IsActionPressed("up") && hooked && currentRadius > 25)
        {
            currentRadius -= climbingSpeed;
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
        )
        {
            currentRadius += climbingSpeed;
            Position += hook.Normalized() * -climbingSpeed;
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
