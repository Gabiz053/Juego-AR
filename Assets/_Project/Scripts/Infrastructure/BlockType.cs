// ------------------------------------------------------------
//  BlockType.cs  -  _Project.Scripts.Infrastructure
//  Defines every placeable block kind in the game.
// ------------------------------------------------------------

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Enumerates all available voxel block types.<br/>
    /// The integer value of each entry is used as the canonical index
    /// throughout the project (UI slot order, serialization, etc.).
    /// </summary>
    public enum BlockType
    {
        Sand  = 0,
        Glass = 1,
        Stone = 2,
        Wood  = 3,
        Torch = 4,
        Grass = 5
    }
}
