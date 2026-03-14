// ------------------------------------------------------------
//  ISaveLoadService.cs  -  _Project.Scripts.Infrastructure
//  Contract for the garden save/load system.
// ------------------------------------------------------------

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Provides garden persistence operations: save the current world
    /// state to disk, list saved gardens, load a garden into the
    /// <c>WorldContainer</c>, and delete saved files.
    /// </summary>
    public interface ISaveLoadService
    {
        /// <summary>
        /// Serializes every block and pebble under the WorldContainer
        /// and writes the result as a JSON file.
        /// </summary>
        void SaveCurrentGarden(string gardenName);

        /// <summary>
        /// Returns the file names (without extension) of all saved
        /// gardens found on disk.  Empty array when none exist.
        /// </summary>
        string[] GetSavedGardensList();

        /// <summary>
        /// Reads and deserializes a garden JSON file.
        /// Returns <c>null</c> when the file does not exist or is corrupt.
        /// </summary>
        Core.GardenSaveData LoadGarden(string fileName);

        /// <summary>
        /// Instantiates all blocks and pebbles from <paramref name="data"/>
        /// into the WorldContainer, clearing the current world first.
        /// Undo history is wiped and harmony is recalculated.
        /// </summary>
        void ApplyGarden(Core.GardenSaveData data);

        /// <summary>Deletes a saved garden file from disk.</summary>
        void DeleteGarden(string fileName);
    }
}
