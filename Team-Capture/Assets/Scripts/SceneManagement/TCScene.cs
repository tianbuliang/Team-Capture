﻿using Mirror;
using UnityEngine;
using Weapons;

namespace SceneManagement
{
    [CreateAssetMenu(fileName = "New TC Scene", menuName = "Team Capture/TCScene")]
    public class TCScene : ScriptableObject
    {
        public string displayName;

        [Tooltip("Is this scene playable?")] public bool enabled = true;

        public GamemodeSettings gamemodeSettings;
        [Scene] public string sceneName;

        public TCWeapon[] stockWeapons;
    }
}