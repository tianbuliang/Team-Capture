﻿using System.Collections;
using Core;
using Core.Networking.Messages;
using Delegates;
using Mirror;
using UI;
using UnityEngine;
using Weapons;
using Logger = Core.Logging.Logger;

namespace Player
{
	/// <summary>
	/// Handles player stuff, such as health
	/// </summary>
	public class PlayerManager : NetworkBehaviour
	{
		/// <summary>
		/// The <see cref="ClientUI"/>
		/// </summary>
		internal ClientUI ClientUi;

		/// <summary>
		/// <see cref="Behaviour"/>s to disable/enable on death and respawn
		/// </summary>
		[SerializeField] private Behaviour[] disableBehaviourOnDeath;

		/// <summary>
		/// <see cref="GameObject"/>s to disable/enable on death and respawn
		/// </summary>
		[SerializeField] private GameObject[] disableGameObjectsOnDeath;

		/// <summary>
		/// How long the invincibility lasts on spawn
		/// </summary>
		[SerializeField] private float invincibilityLastTime = 3.0f;

		/// <summary>
		/// The max health
		/// </summary>
		public int MaxHealth { get; } = 100;

		#region Sync Vars

		/// <summary>
		/// Is this player the one hosting the game?
		/// </summary>
		[field: SyncVar] public bool IsHostPlayer { get; protected set; }

		/// <summary>
		/// Is this player dead?
		/// </summary>
		[field: SyncVar] public bool IsDead { get; protected set; } = true;

		/// <summary>
		/// The username of this player
		/// </summary>
		[SyncVar] public string username = "Not Set";

		/// <summary>
		/// How much health does this player have?
		/// </summary>
		[field: SyncVar(hook = nameof(UpdateHealthUi))]
		public int Health { get; private set; }

		/// <summary>
		/// How many kills does this player have in this active game
		/// </summary>
		[field: SyncVar] public int Kills { get; private set; }

		/// <summary>
		/// How many deaths does this player have in this active game
		/// </summary>
		[field: SyncVar] public int Deaths { get; private set; }

		/// <summary>
		/// Updated every X amount of seconds and haves this player's latency
		/// </summary>
		[SyncVar] public double latency;

		[SyncEvent] public event PlayerKilledDelegate EventPlayerKilled;

		#endregion

		#region Server Variables

		/// <summary>
		/// Only set on the server!
		/// <para>The player's active <see cref="WeaponManager"/></para>
		/// </summary>
		private WeaponManager weaponManager;

		/// <summary>
		/// Only set on the server!
		/// <para>Is this player invincible?</para>
		/// </summary>
		public bool IsInvincible { get; private set; } = true;
		
		#endregion

		public override void OnStartServer()
		{
			base.OnStartServer();

			Health = MaxHealth;
			weaponManager = GetComponent<WeaponManager>();

			if (NetworkManager.singleton.mode != NetworkManagerMode.Host || netId != 1) return;

			IsHostPlayer = true;

			Logger.Debug($"Player {netId} is the host.");
		}

		/// <summary>
		/// Kills the player
		/// </summary>
		[Command(channel = 5)]
		public void CmdSuicide()
		{
			TakeDamage(Health, transform.name);
		}

		#region Death, Respawn, Damage

		/// <summary>
		/// Take damage
		/// </summary>
		/// <param name="damageAmount"></param>
		/// <param name="sourcePlayerId"></param>
		[Server]
		public void TakeDamage(int damageAmount, string sourcePlayerId)
		{
			if (string.IsNullOrWhiteSpace(sourcePlayerId))
			{
				Logger.Error("The sourcePlayerId cannot be empty or null!");
				return;
			}

			//Can't do damage on a player if they are dead or just re-spawned
			if(IsDead || IsInvincible) return;

			Health -= damageAmount;

			if (Health > 0) return;

			//Player is dead
			ServerPlayerDie(sourcePlayerId);
		}

		/// <summary>
		/// Adds health to the player
		/// </summary>
		/// <param name="amount"></param>
		[Server]
		public void AddHealth(int amount)
		{
			if(Health == amount)
				return;

			if (Health + amount > MaxHealth)
			{
				Health = MaxHealth;
				return;
			}

			Health += amount;
		}

		/// <summary>
		/// The server side method that handles a player's death
		/// </summary>
		[Server]
		private void ServerPlayerDie(string sourcePlayerId)
		{
			IsDead = true;

			//Send a message about this player's death
			NetworkServer.SendToAll(new PlayerDiedMessage
			{
				PlayerKilled = transform.name,
				PlayerKiller = sourcePlayerId,
				WeaponName = weaponManager.GetActiveWeapon().Weapon
			}, 3);

			//Remove all the weapons on the player
			weaponManager.RemoveAllWeapons();

			//Call the client side code on each player
			RpcClientPlayerDie();

			//Update the stats, for both players
			PlayerManager killer = GameManager.GetPlayer(sourcePlayerId);
			Deaths++;
			if (sourcePlayerId != transform.name)
				killer.Kills++;

			EventPlayerKilled?.Invoke(transform.name, sourcePlayerId);

			StartCoroutine(ServerPlayerRespawn());
		}

		/// <summary>
		/// Server side method that handles the player's
		/// </summary>
		/// <returns></returns>
		[Server]
		internal IEnumerator ServerPlayerRespawn(bool skipRespawnTime = false)
		{
			if(!skipRespawnTime)
				yield return new WaitForSeconds(GameManager.GetActiveScene().respawnTime);

			Health = MaxHealth;

			//TODO: This will fully work once we have server authoritative movement!
			Transform spawnPoint = NetworkManager.singleton.GetStartPosition();
			transform.position = spawnPoint.position;
			// ReSharper disable once Unity.InefficientPropertyAccess
			transform.rotation = spawnPoint.rotation;

			RpcClientRespawn();

			//Add stock weapons
			GetComponent<WeaponManager>().AddStockWeapons();

			IsDead = false;

			IsInvincible = true;
			yield return new WaitForSeconds(invincibilityLastTime);

			IsInvincible = false;
		}

		/// <summary>
		/// Disables components on each of the clients
		/// </summary>
		[ClientRpc(channel = 5)]
		private void RpcClientPlayerDie()
		{
			//Disable the collider, or the Char controller
			if (isLocalPlayer)
			{
				GetComponent<PlayerMovement>().enabled = false;
				GetComponent<CharacterController>().enabled = false;

				//Switch cams
				GameManager.GetActiveSceneCamera().SetActive(true);

				//Disable the UI
				ClientUi.hud.gameObject.SetActive(false);
			}
			else
			{
				GetComponent<CapsuleCollider>().enabled = false;
			}

			foreach (GameObject toDisable in disableGameObjectsOnDeath) toDisable.SetActive(false);

			foreach (Behaviour toDisable in disableBehaviourOnDeath) toDisable.enabled = false;
		}

		/// <summary>
		/// Client side method of enabling client side stuff per client
		/// </summary>
		[ClientRpc(channel = 5)]
		private void RpcClientRespawn()
		{
			//Enable game objects
			foreach (GameObject toEnable in disableGameObjectsOnDeath) toEnable.SetActive(true);

			foreach (Behaviour toEnable in disableBehaviourOnDeath) toEnable.enabled = true;

			//Enable the collider, or the Char controller
			if (isLocalPlayer)
			{
				GetComponent<CharacterController>().enabled = true;
				GetComponent<PlayerMovement>().enabled = true;

				//Switch cams
				GameManager.GetActiveSceneCamera().SetActive(false);

				//Enable our UI
				ClientUi.hud.gameObject.SetActive(true);
			}
			else
			{
				GetComponent<CapsuleCollider>().enabled = true;
			}
		}

#pragma warning disable IDE0060 // Remove unused parameter, yes these paramaters HAVE to be here for the hook! And yea we gotta do it for both ReSharper and VS
		// ReSharper disable UnusedParameter.Local
		private void UpdateHealthUi(int oldHealth, int newHealth)
			// ReSharper restore UnusedParameter.Local
		{
			if (isLocalPlayer && ClientUi != null)
				ClientUi.hud.UpdateHealthUI();
		}
#pragma warning restore IDE0060 // Remove unused parameter

		#endregion
	}
}