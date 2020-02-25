﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Core.Logger;
using UnityEngine;

namespace Core.Console
{
	public class ConsoleInterface : MonoBehaviour
	{
		private static readonly Dictionary<string, ConsoleCommand> Commands = new Dictionary<string, ConsoleCommand>();

		public static void RegisterCommands()
		{
			const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
			{
				foreach (MethodInfo method in type.GetMethods(bindingFlags))
				{
					if (!(Attribute.GetCustomAttribute(method, typeof(ConCommand)) is ConCommand attribute))
						continue;

					MethodDelegate methodDelegate =
						(MethodDelegate) Delegate.CreateDelegate(typeof(MethodDelegate), method);

					AddCommand(attribute.Name, attribute.Summary, methodDelegate);
				}
			}

			ConfigFilesLocation = Directory.GetParent(Application.dataPath).FullName + "/Cfg/";
		}

		public static void AddCommand(string commandName, string summary, MethodDelegate method)
		{
			commandName = commandName.ToLower();

			Logger.Logger.Log($"Added command `{commandName}`.", LogVerbosity.Debug);

			Commands.Add(commandName, new ConsoleCommand
			{
				CommandName = commandName,
				CommandSummary = summary,
				CommandMethod = method
			});
		}

		public static void ExecuteCommand(string command)
		{
			List<string> tokens = Tokenize(command);
			if (tokens.Count < 1)
				return;

			if (Commands.TryGetValue(tokens[0].ToLower(), out ConsoleCommand conCommand))
			{
				string[] arguments = tokens.GetRange(1, tokens.Count - 1).ToArray();
				conCommand.CommandMethod.Invoke(arguments);

				return;
			}

			Logger.Logger.Log($"Unknown command: {tokens[0]}", LogVerbosity.Error);
		}

		#region Argument Parsing

		private static List<string> Tokenize(string input)
		{
			int pos = 0;
			List<string> res = new List<string>();
			int c = 0;
			while (pos < input.Length && c++ < 10000)
			{
				SkipWhite(input, ref pos);
				if (pos == input.Length)
					break;

				if (input[pos] == '"' && (pos == 0 || input[pos - 1] != '\\'))
				{
					res.Add(ParseQuoted(input, ref pos));
				}
				else
					res.Add(Parse(input, ref pos));
			}
			return res;
		}

		private static void SkipWhite(string input, ref int pos)
		{
			while (pos < input.Length && " \t".IndexOf(input[pos]) > -1)
			{
				pos++;
			}
		}

		private static string ParseQuoted(string input, ref int pos)
		{
			pos++;
			int startPos = pos;
			while (pos < input.Length)
			{
				if (input[pos] == '"' && input[pos - 1] != '\\')
				{
					pos++;
					return input.Substring(startPos, pos - startPos - 1);
				}
				pos++;
			}
			return input.Substring(startPos);
		}

		private static string Parse(string input, ref int pos)
		{
			int startPos = pos;
			while (pos < input.Length)
			{
				if (" \t".IndexOf(input[pos]) > -1)
				{
					return input.Substring(startPos, pos - startPos);
				}
				pos++;
			}
			return input.Substring(startPos);
		}
		
		#endregion

		#region File Executuion

		private static string ConfigFilesLocation;

		[ConCommand(Name = "exec", Summary = "Executes a file and runs all the commands")]
		public static void ExecuteFile(string[] args)
		{
			if (args.Length != 1)
			{
				Logger.Logger.Log("Invalid arguments!", LogVerbosity.Error);
				return;
			}

			string fileName = args[0] + ".cfg";
			if (!File.Exists(ConfigFilesLocation + fileName))
			{
				Logger.Logger.Log($"`{fileName}` doesn't exist! Not executing.", LogVerbosity.Error);
				return;
			}

			string[] lines = File.ReadAllLines(ConfigFilesLocation + fileName);
			foreach (string line in lines)
			{
				if(line.StartsWith("//")) continue;

				ExecuteCommand(line);
			}
		}
		
		#endregion
	}
}