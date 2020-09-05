using UnityEngine;

namespace ModShelter
{
    /// <summary>
    /// Inject modding interface into game only in single player mode
    /// </summary>
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModShelter)}__").AddComponent<ModShelter>();
        }
    }
}
