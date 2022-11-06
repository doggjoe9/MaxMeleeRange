using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Globalization;
using System.Numerics;

namespace MaxMeleeRing {
	public sealed class MaxMeleeRing : IDalamudPlugin {
		#region Fields
		public string Name => "Max Melee Ring";
		private const string CommandName = "/mmr";

		private int playerSegs;
		private int targetSegs;
		private float height;
		private float lineWidth;
		private uint colorIfClose;
		private uint colorIfFar;
		private Vector4 colorIfClose4;
		private Vector4 colorIfFar4;

		private bool config = false;

		private GameObject? currentTargetActor;
		#endregion

		public MaxMeleeRing(DalamudPluginInterface pluginInterface) {
			#region Service Init
			Services.Initialize(pluginInterface);
			#endregion
			#region Load Config
			LoadConfig();
			#endregion
			#region Command Init
			Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				// TODO Put some helpful message here.
				HelpMessage = "Opens the config interface for Max Melee Ring."
			});
			#endregion
			#region UI Init
			Services.PluginInterface.UiBuilder.Draw += DrawUI;
			#endregion
		}

		private void LoadConfig() {
			playerSegs = Services.Configuration.playerSegs;
			targetSegs = Services.Configuration.targetSegs;
			height = Services.Configuration.height;
			lineWidth = Services.Configuration.lineWidth;
			colorIfClose = Services.Configuration.colorIfClose;
			colorIfFar = Services.Configuration.colorIfFar;
			colorIfClose4 = ImGui.ColorConvertU32ToFloat4(colorIfClose);
			colorIfFar4 = ImGui.ColorConvertU32ToFloat4(colorIfFar);
		}

		private void SaveConfig() {
			Services.Configuration.playerSegs = playerSegs;
			Services.Configuration.targetSegs = targetSegs;
			Services.Configuration.height = height;
			Services.Configuration.lineWidth = lineWidth;
			Services.Configuration.colorIfClose = ImGui.GetColorU32(colorIfClose4);
			Services.Configuration.colorIfFar = ImGui.GetColorU32(colorIfFar4);
			Services.Configuration.Save();
			LoadConfig();
		}

		public void Dispose() {
			#region Dispose UI
			Services.PluginInterface.UiBuilder.Draw -= DrawUI;
			Services.CommandManager.RemoveHandler(CommandName);
			#endregion
		}

		#region Command
		/// <summary>
		/// Executes when the player enters the command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		private void OnCommand(string command, string args) {
			config = !config;
		}
		#endregion


		#region Draw
		private void DrawUI() {
			#region Config UI
			if (config) {
				ImGui.SetNextWindowSize(new Vector2(300, 500), ImGuiCond.FirstUseEver);
				ImGui.Begin("Max Melee Circle Config", ref config);

				ImGui.DragFloat("Height Offset", ref height);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("How man yalms above the player's feet should the rings appear?");

				ImGui.DragFloat("Line Width", ref lineWidth);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("Thickness of the lines used to draw the rings.");
				
				ImGui.ColorEdit4("Close Color", ref colorIfClose4);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("What color should the rings be when the player is within max melee range?");

				ImGui.ColorEdit4("Far Color", ref colorIfFar4);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("What color should the rings be when the player is beyond max melee range?");

				ImGui.SliderInt("Player Resolution", ref playerSegs, 3, 200);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("The amount of line segments used to draw the player's ring. Small values result in janky behavior and large values may cause performance issues.");

				ImGui.SliderInt("Target Resolution", ref targetSegs, 3, 500);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("The amount of line segments used to draw the target's ring. Small values result in janky behavior and large values may cause performance issues.");

				if (ImGui.Button("Save & Close")) {
					SaveConfig();
					config = false;
				}
				ImGui.SameLine();
				if (ImGui.Button("Save")) {
					SaveConfig();
				}
				ImGui.SameLine();
				if (ImGui.Button("Close")) {
					config = false;
				}
			}
			#endregion

			#region Filter Classes
			string cls = Services.ClientState.LocalPlayer?.ClassJob.GameData?.Abbreviation.ToString() ?? "ERR";
			switch (cls) {
				case "WHM":
				case "SCH":
				case "AST":
				case "SGE":
				case "BRD":
				case "MCH":
				case "DNC":
				case "BLM":
					return;
			}
			#endregion
			#region Render Circle
			RefreshTarget();
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
			ImGui.Begin("MaxMeleeRing",
					ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar |
					ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
			ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

			GameObject? localPlayer = Services.ClientState.LocalPlayer;
			if (currentTargetActor != null && localPlayer != null) {
				ImGuiMaxMeleeRing(currentTargetActor, localPlayer);
			}

			ImGui.End();
			ImGui.PopStyleVar();
			#endregion
		}

		private void RefreshTarget() {
			GameObject? currentTarget = Services.TargetManager.Target;
			currentTargetActor = currentTarget?.ObjectKind is ObjectKind.BattleNpc ? currentTarget : null;

		}

		private void ImGuiMaxMeleeRing(GameObject target, GameObject player) {
			float playerRadius = player.HitboxRadius;
			float targetRadius = target.HitboxRadius + 3.0f;
			float heightOffset = player.Position.Y + height;

			Vector2 playerPositionFlat = new Vector2(player.Position.X, player.Position.Z);
			Vector2 targetPositionFlat = new Vector2(target.Position.X, target.Position.Z);

			float targetDistanceToPlayerFlat = Vector2.Distance(playerPositionFlat, targetPositionFlat);

			uint color = targetDistanceToPlayerFlat <= playerRadius + targetRadius ? colorIfClose : colorIfFar;

			float seg = playerSegs * 0.5f;
			for (int i = 0; i <= playerSegs; i++) {
				Vector2 worldVertexFlat = new Vector2(
					player.Position.X + (playerRadius * (float) Math.Sin(i * Math.PI / seg)),
					player.Position.Z + (playerRadius * (float) Math.Cos(i * Math.PI / seg))
				);

				if (Vector2.Distance(worldVertexFlat, targetPositionFlat) < targetRadius * 1.0f) {
					ImGui.GetWindowDrawList().PathStroke(color, ImDrawFlags.None, lineWidth);
					continue;
				}
					

				Vector3 worldVertex = new Vector3(worldVertexFlat.X, heightOffset, worldVertexFlat.Y);
				Services.GameGui.WorldToScreen(worldVertex, out Vector2 screenVertex);
				ImGui.GetWindowDrawList().PathLineTo(screenVertex);
			}
			ImGui.GetWindowDrawList().PathStroke(color, ImDrawFlags.None, lineWidth);

			seg = targetSegs * 0.5f;
			for (int i = 0; i <= targetSegs; i++) {
				Vector2 worldVertexFlat = new Vector2(
					target.Position.X + (targetRadius * (float) Math.Sin(i * Math.PI / seg)),
					target.Position.Z + (targetRadius * (float) Math.Cos(i * Math.PI / seg))
				);

				if (Vector2.Distance(worldVertexFlat, playerPositionFlat) < playerRadius * 1.0f) {
					ImGui.GetWindowDrawList().PathStroke(color, ImDrawFlags.None, lineWidth);
					continue;
				}
					

				Vector3 worldVertex = new Vector3(worldVertexFlat.X, heightOffset, worldVertexFlat.Y);
				Services.GameGui.WorldToScreen(worldVertex, out Vector2 screenVertex);
				ImGui.GetWindowDrawList().PathLineTo(screenVertex);
			}
			ImGui.GetWindowDrawList().PathStroke(color, ImDrawFlags.None, lineWidth);
		}
		#endregion
	}
}
