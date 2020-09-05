using UnityEngine;

namespace ModShelter
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModShelter)}__").AddComponent<ModShelter>();
        }
    }
}
