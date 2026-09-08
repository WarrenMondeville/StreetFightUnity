namespace StreetFighter.Gameplay
{
    /// <summary>参与碰撞检测的实体。</summary>
    public interface ICollidable
    {
        float Left { get; set; }

        float Top { get; set; }

        float Width { get; }

        float Height { get; }

        /// <summary>归属角色；角色自身为 null，判定体指向其主人。</summary>
        object Master { get; }
    }
}
