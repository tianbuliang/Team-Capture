﻿using System;
using Player;
using UnityEngine;
using Weapons;
using Logger = Core.Logging.Logger;

namespace UI
{
	/// <summary>
	/// Controller for the client UI
	/// </summary>
	internal class ClientUI : MonoBehaviour
	{
		/// <summary>
		/// Is the pause menu open
		/// </summary>
		public static bool IsPauseMenuOpen;

		/// <summary>
		/// The <see cref="Player.PlayerManager"/>
		/// </summary>
		[NonSerialized] public PlayerManager PlayerManager;

		/// <summary>
		/// The <see cref="Weapons.WeaponManager"/>
		/// </summary>
		[NonSerialized] public WeaponManager WeaponManager;

		/// <summary>
		/// The hud
		/// </summary>
		[Tooltip("The hud")]
		public Hud hud;

		/// <summary>
		/// The killfeed
		/// </summary>
		[Tooltip("The killfeed")]
		public KillFeed killFeed;

		/// <summary>
		/// The pause menu
		/// </summary>
		[Tooltip("The pause menu")]
		public MainMenuController pauseMenu;

		/// <summary>
		/// The scoreboard gameobject
		/// </summary>
		[Tooltip("The scoreboard gameobject")]
		public GameObject scoreBoardObject;

		/// <summary>
		/// Sets up the UI
		/// </summary>
		/// <param name="playerManager"></param>
		public void SetupUI(PlayerManager playerManager)
		{
			//Reset this
			IsPauseMenuOpen = false;

			hud.Setup(this);

			PlayerManager = playerManager;
			WeaponManager = playerManager.GetComponent<WeaponManager>();

			pauseMenu.gameObject.SetActive(false);

			scoreBoardObject.SetActive(false);
			scoreBoardObject.GetComponent<ScoreBoard.ScoreBoard>().clientPlayer = playerManager;

			Logger.Debug("The ClientUI is now ready.");
		}

		/// <summary>
		/// Toggles the pause menu
		/// </summary>
		public void TogglePauseMenu()
		{
			ActivatePauseMenu(!IsPauseMenuOpen);
		}

		/// <summary>
		/// Activate a pause menu
		/// </summary>
		/// <param name="state"></param>
		public void ActivatePauseMenu(bool state)
		{
			IsPauseMenuOpen = state;

			Cursor.visible = IsPauseMenuOpen;
			Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;

			pauseMenu.gameObject.SetActive(state);
			killFeed.killFeedItemsHolder.gameObject.SetActive(!state);

			if (PlayerManager.IsDead) return;
			hud.gameObject.SetActive(!state);
		}

		/// <summary>
		/// Toggles the score board
		/// </summary>
		public void ToggleScoreBoard()
		{
			scoreBoardObject.SetActive(!scoreBoardObject.activeSelf);
		}
	}
}