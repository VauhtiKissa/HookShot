using System;
using Godot;

public partial class WinTrigger : Area2D
{
    // should not be the same map
    [Export]
    public PackedScene newMap;

    public void OnBodyEntered(PhysicsBody2D body)
    {
        if (body.Name == "Player")
        {
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToPacked, newMap);
        }
    }
}
