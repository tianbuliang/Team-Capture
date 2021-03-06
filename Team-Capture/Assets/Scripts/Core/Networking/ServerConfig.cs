﻿using System;
using Mirror;
using Team_Capture.Logging;
using Team_Capture.Settings.Enums;

namespace Team_Capture.Core.Networking
{
	/// <summary>
	///		Config for server settings
	/// </summary>
	[Serializable]
	internal struct ServerConfig : NetworkMessage
	{
		/// <summary>
		///		The name of the game
		/// </summary>
		public string gameName;

		/// <summary>
		///		The motd mode
		/// </summary>
		public Server.ServerMOTDMode motdMode;

		/// <summary>
		///		Text for motd
		/// </summary>
		public string motdText;
	}

	internal static class ServerConfigNetwork
	{
		public static void WriteServerConfig(this NetworkWriter writer, ServerConfig config)
		{
			writer.WriteString(config.gameName);
			writer.WriteByte((byte)config.motdMode);
			if(config.motdMode == Server.ServerMOTDMode.TextOnly)
				writer.WriteString(config.motdText);
		}

		public static ServerConfig ReadServerConfig(this NetworkReader reader)
		{
			ServerConfig config = new ServerConfig
			{
				gameName = reader.ReadString(),
				motdMode = (Server.ServerMOTDMode)reader.ReadByte()
			};

			if (config.motdMode == Server.ServerMOTDMode.TextOnly)
				config.motdText = reader.ReadString();

			return config;
		}
	}
}