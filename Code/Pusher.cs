using System;
using Godot;

public partial class Pusher : Area2D
{
    [Export]
    public Vector2 pushForce;

    public void OnBodyEntered(PhysicsBody2D body)
    {
        if (body.Name == "Player")
        {
            ((Player)body).pushForce += pushForce;
        }
    }

    public void OnBodyExited(PhysicsBody2D body)
    {
        if (body.Name == "Player")
        {
            ((Player)body).pushForce -= pushForce;
        }
    }
}
