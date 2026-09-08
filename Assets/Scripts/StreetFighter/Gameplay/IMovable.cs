namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 可被 <see cref="Mover"/> 驱动的对象：需要提供可读写的位置、地面基准 Y，以及水平边界钳制。
    /// </summary>
    public interface IMovable
    {
        /// <summary>包围盒左边界（画布像素，y 向下）。</summary>
        float Left { get; set; }

        /// <summary>包围盒上边界（画布像素，y 向下）。</summary>
        float Top { get; set; }

        /// <summary>站立时的 top 基准值。</summary>
        float FloorTop { get; }

        /// <summary>把 left 钳制到合法范围并返回钳制后的值。</summary>
        float CrossBorder(float left);
    }
}
