﻿using UnityEngine;

namespace ModShelter.Extensions
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
