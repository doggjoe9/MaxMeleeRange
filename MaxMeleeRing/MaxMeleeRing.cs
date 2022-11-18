using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Numerics;

namespace MaxMeleeRing {
	public sealed class MaxMeleeRing : IDalamudPlugin {
		#region Fields
		public string Name => "Max Melee Ring";
		private const string CommandName = "/mmr";

		// general
		private int playerSegs;
		private int targetSegs;
		private float height;
		private float lineWidth;
		private float zbias;
		private bool relToTargetHeight;
		private bool dashLine;
		private bool drawSpokes;
		private bool drawTargetOrientation;
		private bool ringMode;

		// color
		private uint colorIfClose;
		private uint colorIfFar;
		private uint northSpokeColor;
		private uint cardinalSpokeColor;
		private uint intercardinalSpokeColor;
		private uint targetOrientationColor;
		private Vector4 colorIfClose4;
		private Vector4 colorIfFar4;
		private Vector4 northSpokeColor4;
		private Vector4 cardinalSpokeColor4;
		private Vector4 intercardinalSpokeColor4;
		private Vector4 targetOrientationColor4;
		
		// dash line
		private bool drawDashLineInsideHitbox;
		
		// spokes
		private bool hideFarSpokes;
		private bool drawSpokeLabels;
		private bool bossRelativeSpokes;

		// Ring Mode Settings
		private bool playerReach;
		private bool playerCenterDot;
		private bool forceDrawTargetRing;
		private bool drawPlayerRingInsideHitbox;

		// Dot mode Settings
		private bool drawDotInsideHitbox;
		

		//state
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
			zbias = Services.Configuration.zbias;
			colorIfClose = Services.Configuration.colorIfClose;
			colorIfFar = Services.Configuration.colorIfFar;
			northSpokeColor = Services.Configuration.northSpokeColor;
			cardinalSpokeColor = Services.Configuration.cardinalSpokeColor;
			intercardinalSpokeColor = Services.Configuration.intercardinalSpokeColor;
			targetOrientationColor = Services.Configuration.targetOrientationColor;
			colorIfClose4 = ImGui.ColorConvertU32ToFloat4(colorIfClose);
			colorIfFar4 = ImGui.ColorConvertU32ToFloat4(colorIfFar);
			northSpokeColor4 = ImGui.ColorConvertU32ToFloat4(northSpokeColor);
			cardinalSpokeColor4 = ImGui.ColorConvertU32ToFloat4(cardinalSpokeColor);
			intercardinalSpokeColor4 = ImGui.ColorConvertU32ToFloat4(intercardinalSpokeColor);
			targetOrientationColor4 = ImGui.ColorConvertU32ToFloat4(targetOrientationColor);
			relToTargetHeight = Services.Configuration.relToTargetHeight;
			dashLine = Services.Configuration.dashLine;
			drawDashLineInsideHitbox = Services.Configuration.drawDashLineInsideHitbox;
			drawSpokes = Services.Configuration.drawSpokes;
			drawTargetOrientation = Services.Configuration.drawTargetOrientation;
			hideFarSpokes = Services.Configuration.hideFarSpokes;
			drawSpokeLabels = Services.Configuration.drawSpokeLabels;
			bossRelativeSpokes = Services.Configuration.bossRelativeSpokes;
			ringMode = Services.Configuration.ringMode;
			playerReach = Services.Configuration.playerReach;
			playerCenterDot = Services.Configuration.playerCenterDot;
			forceDrawTargetRing = Services.Configuration.forceDrawTargetRing;
			drawDotInsideHitbox = Services.Configuration.drawDotInsideHitbox;
			drawPlayerRingInsideHitbox = Services.Configuration.drawPlayerRingInsideHitbox;
		}

		private void SaveConfig() {
			Services.Configuration.playerSegs = playerSegs;
			Services.Configuration.targetSegs = targetSegs;
			Services.Configuration.height = height;
			Services.Configuration.lineWidth = lineWidth;
			Services.Configuration.zbias = zbias;
			Services.Configuration.colorIfClose = ImGui.GetColorU32(colorIfClose4);
			Services.Configuration.colorIfFar = ImGui.GetColorU32(colorIfFar4);
			Services.Configuration.northSpokeColor = ImGui.GetColorU32(northSpokeColor4);
			Services.Configuration.cardinalSpokeColor = ImGui.GetColorU32(cardinalSpokeColor4);
			Services.Configuration.intercardinalSpokeColor = ImGui.GetColorU32(intercardinalSpokeColor4);
			Services.Configuration.targetOrientationColor = ImGui.GetColorU32(targetOrientationColor4);
			Services.Configuration.relToTargetHeight = relToTargetHeight;
			Services.Configuration.dashLine = dashLine;
			Services.Configuration.drawDashLineInsideHitbox = drawDashLineInsideHitbox;
			Services.Configuration.ringMode = ringMode;
			Services.Configuration.playerReach = playerReach;
			Services.Configuration.playerCenterDot = playerCenterDot;
			Services.Configuration.forceDrawTargetRing = forceDrawTargetRing;
			Services.Configuration.drawDotInsideHitbox = drawDotInsideHitbox;
			Services.Configuration.drawPlayerRingInsideHitbox = drawPlayerRingInsideHitbox;
			Services.Configuration.drawSpokes = drawSpokes;
			Services.Configuration.drawSpokeLabels = drawSpokeLabels;
			Services.Configuration.bossRelativeSpokes = bossRelativeSpokes;
			Services.Configuration.hideFarSpokes = hideFarSpokes;
			Services.Configuration.drawTargetOrientation = drawTargetOrientation;
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

				ImGui.ColorEdit4("Close Color", ref colorIfClose4);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("What color should the rings be when the player is within max melee range?");
				colorIfClose = ImGui.GetColorU32(colorIfClose4);

				ImGui.ColorEdit4("Far Color", ref colorIfFar4);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("What color should the rings be when the player is beyond max melee range?");
				colorIfFar = ImGui.GetColorU32(colorIfFar4);

				if (drawTargetOrientation) {
					ImGui.NewLine();
					ImGui.ColorEdit4("Target Orientation Color", ref targetOrientationColor4);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("What color should the target orientation be?");
					targetOrientationColor = ImGui.GetColorU32(targetOrientationColor4);
				}

				if (drawSpokes) {
					ImGui.NewLine();
					ImGui.ColorEdit4("North Spoke Color", ref northSpokeColor4);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("What color should the north facing spoke be?");
					northSpokeColor = ImGui.GetColorU32(northSpokeColor4);
					ImGui.ColorEdit4("Cardinal Spoke Color", ref cardinalSpokeColor4);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("What color should the cardinal facing spokes be?");
					cardinalSpokeColor = ImGui.GetColorU32(cardinalSpokeColor4);
					ImGui.ColorEdit4("Intercardinal Spoke Color", ref intercardinalSpokeColor4);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("What color should the intercardinal facing spokes be?");
					intercardinalSpokeColor = ImGui.GetColorU32(intercardinalSpokeColor4);
				}

				ImGui.NewLine();

				ImGui.SliderFloat("Height Offset", ref height, -15.0f, 15.0f);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("How man yalms above the player's feet should the rings appear? (CTRL + Click for manual entry)");

				ImGui.SliderFloat("Line Width", ref lineWidth, 0.0f, 50.0f);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("Thickness of the lines used to draw the rings. (CTRL + Click for manual entry)");

				ImGui.SliderFloat("Z Bias", ref zbias, 0.0f, 10.0f);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("Used for culling invisible lines. Set this higher if you see lines dancing on the screen or flickering. Set this lower if lines disappear before they go out of view. (Default: 1.0).");

				if (ringMode) {
					ImGui.SliderInt("Player Resolution", ref playerSegs, 2, 9);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("The amount of line segments used to draw the player's ring. Small values result in janky behavior and large values may cause performance issues. (CTRL + Click for manual entry)");
				}

				ImGui.SliderInt("Target Resolution", ref targetSegs, 3, 50);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("The amount of line segments used to draw the target's ring. Small values result in janky behavior and large values may cause performance issues. (CTRL + Click for manual entry)");

				ImGui.NewLine();

				ImGui.Checkbox("Lock to Target Height", ref relToTargetHeight);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("Draws the rings based on the target's elevation, rather than your own.");

				ImGui.Checkbox("Target Orientation Line", ref drawTargetOrientation);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("Draws a line in the direction the target is looking.");

				ImGui.Checkbox("Dash Line", ref dashLine);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("Draws a line between you and the boss for help with lining up dashes.");

				if (dashLine) {
					ImGui.Indent();
					ImGui.Checkbox("Inside Hitbox?", ref drawDashLineInsideHitbox);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("Draw the dash line inside of the target hitbox?.");
					ImGui.Unindent();
				}

				ImGui.Checkbox("Directional Spokes", ref drawSpokes);
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip("Draws spokes indicating the orientation of the target.");

				if (drawSpokes) {
					ImGui.Indent();
					ImGui.Checkbox("Hide Far", ref hideFarSpokes);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("Hide spokes on the opposite side of the target from you.");
					ImGui.Checkbox("Draw Labels", ref drawSpokeLabels);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("Draws letters indicating the cardinal direction of each spoke.");
					//ImGui.Checkbox("Boss Relative", ref bossRelativeSpokes);
					//if (ImGui.IsItemHovered())
					//	ImGui.SetTooltip("(UNUSED) Spokes will be rotated with the boss. Labels will still display the closest true cardinal direction.");
					ImGui.Unindent();
				}

				ImGui.NewLine();

				if (ringMode) {
					if (ImGui.Button("Toggle Dot Mode")) {
						ringMode = false;
					}

					ImGui.Checkbox("Inside Hitbox?  ", ref drawPlayerRingInsideHitbox);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("If disabled, the player's hitbox ring will not render when inside the target's hitbox.");

					ImGui.Checkbox("Player Relative", ref playerReach);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("Displays max melee range on the player's hitbox instead of the target's.");

					ImGui.Checkbox("Player Center Dot", ref playerCenterDot);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("Displays a dot at the player's current position.");

					if (playerCenterDot) {
						ImGui.Indent();
						ImGui.Checkbox("Inside Hitbox? ", ref drawDotInsideHitbox);
						if (ImGui.IsItemHovered())
							ImGui.SetTooltip("If disabled, the center dot will be hidden when you are inside the hitbox.");
						ImGui.Unindent();
					}

					ImGui.Checkbox("Force Target Ring", ref forceDrawTargetRing);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("If enabled, the entire target ring will always be drawn, regardless of whether the player ring is intersecting it.");
				} else {
					if (ImGui.Button("Toggle Ring Mode")) {
						ringMode = true;
					}

					ImGui.Checkbox("Draw Dot Inside Hitbox", ref drawDotInsideHitbox);
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("If disabled, the center dot will be hidden when you are inside the hitbox.");
				}

				ImGui.NewLine();

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
				case "PLD": // Tanks
				case "WAR":
				case "DRK":
				case "GNB":
				case "MNK": // Melees
				case "DRG":
				case "NIN":
				case "SAM":
				case "RPR":
				case "SMN": // Special
				case "RDM":
				case "BLU":
					break; // only allow the above classes
				default:
					return; // otherwise, skip rendring the circle
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
			if (currentTargetActor != null && localPlayer != null)
				ImGuiMaxMelee(currentTargetActor, localPlayer);

			ImGui.End();
			ImGui.PopStyleVar();
			#endregion
		}

		private void RefreshTarget() {
			GameObject? currentTarget = Services.TargetManager.Target;
			currentTargetActor = currentTarget?.ObjectKind is ObjectKind.BattleNpc ? currentTarget : null;

		}

		private float angleDelta(float a, float b) => (a - b + 3.0f * MathF.PI) % (2.0f * MathF.PI) - MathF.PI;

		private void DrawImGuiCircleSegmentBezier(Vector3 center, Vector2 start, Vector2 end, float delta, uint color) {
			Vector3 arcStart = new Vector3(
				start.X,
				0.0f,
				start.Y
			); ;
			Vector3 arcEnd = new Vector3(
				end.X,
				0.0f,
				end.Y
			);

			float bezierConstant = (4.0f / 3.0f) * MathF.Tan(delta / 4.0f);
			Vector3 startWeight = new Vector3(arcStart.X - bezierConstant * arcStart.Z, 0.0f, arcStart.Z + bezierConstant * arcStart.X);
			Vector3 endWeight = new Vector3(arcEnd.X + bezierConstant * arcEnd.Z, 0.0f, arcEnd.Z - bezierConstant * arcEnd.X);

			bool validSegment = true;
			validSegment &= Services.BetterGameGui.WorldToScreen(center + arcStart, zbias, out Vector2 arcStartPoint);
			validSegment &= Services.BetterGameGui.WorldToScreen(center + arcEnd, zbias, out Vector2 arcEndPoint);
			validSegment &= Services.BetterGameGui.WorldToScreen(center + startWeight, zbias, out Vector2 startWeightPoint);
			validSegment &= Services.BetterGameGui.WorldToScreen(center + endWeight, zbias, out Vector2 endWeightPoint);

			if (validSegment)
				ImGui.GetWindowDrawList().AddBezierCubic(arcStartPoint, startWeightPoint, endWeightPoint, arcEndPoint, color, lineWidth);
		}

		private void DrawImGuiCircleSegmentBezier(Vector3 center, float startAngle, float endAngle, float radius, uint color) {
			Vector3 arcStart = new Vector3(
				radius * MathF.Cos(startAngle),
				0.0f,
				radius * MathF.Sin(startAngle)
			);
			Vector3 arcEnd = new Vector3(
				radius * MathF.Cos(endAngle),
				0.0f,
				radius * MathF.Sin(endAngle)
			);

			float bezierConstant = (4.0f / 3.0f) * MathF.Tan(MathF.Abs(angleDelta(endAngle, startAngle)) / 4.0f);
			Vector3 startWeight = new Vector3(arcStart.X - bezierConstant * arcStart.Z, 0.0f, arcStart.Z + bezierConstant * arcStart.X);
			Vector3 endWeight = new Vector3(arcEnd.X + bezierConstant * arcEnd.Z, 0.0f, arcEnd.Z - bezierConstant * arcEnd.X);

			bool validSegment = true;
			validSegment &= Services.BetterGameGui.WorldToScreen(center + arcStart, zbias, out Vector2 arcStartPoint);
			validSegment &= Services.BetterGameGui.WorldToScreen(center + arcEnd, zbias, out Vector2 arcEndPoint);
			validSegment &= Services.BetterGameGui.WorldToScreen(center + startWeight, zbias, out Vector2 startWeightPoint);
			validSegment &= Services.BetterGameGui.WorldToScreen(center + endWeight, zbias, out Vector2 endWeightPoint);

			if (validSegment)
				ImGui.GetWindowDrawList().AddBezierCubic(arcStartPoint, startWeightPoint, endWeightPoint, arcEndPoint, color, lineWidth);
			// Debug
			//ImGui.GetWindowDrawList().AddCircleFilled(arcStartPoint, lineWidth, 0xFFFFFFFF);
			//ImGui.GetWindowDrawList().AddCircleFilled(arcEndPoint, lineWidth, 0xFFFFFFFF);
			//ImGui.GetWindowDrawList().AddCircleFilled(startWeightPoint, lineWidth, 0xFF00FF00);
			//ImGui.GetWindowDrawList().AddCircleFilled(endWeightPoint, lineWidth, 0xFF00FF00);
		}

		private Vector2 rotate(Vector2 v, float angle) {
			float ca = MathF.Cos(angle);
			float sa = MathF.Sin(angle);
			return new Vector2(ca * v.X - sa * v.Y, sa * v.X + ca * v.Y);
		}

		private void DrawCircleAdvanced(Vector3 center, Vector2 toTargetNorm, float thisRadius, float targetRadius, float distanceToTarget, uint color, int numSegments) {
			float R = thisRadius;
			float r = targetRadius;
			float d = distanceToTarget;
			float x = 0.5f * (d*d - r*r + R*R) / d;
			float halfa = 0.5f * MathF.Sqrt((-d+r-R)*(-d-r+R)*(-d+r+R)*(d+r+R)) / d;
			Vector2 toTargetPerpNorm = new Vector2(-toTargetNorm.Y, toTargetNorm.X);
			Vector2 iSect1v = (x * toTargetNorm + halfa * toTargetPerpNorm);
			Vector2 iSect2v = (x * toTargetNorm - halfa * toTargetPerpNorm);
			float iSectAng = 2.0f * MathF.Acos(Vector2.Dot(iSect2v, toTargetNorm) / iSect1v.Length());


			if (float.IsNaN(iSectAng)) {
				// simple circle will suffice
				DrawBezierCircleSimple(center, thisRadius, color, numSegments);
				return;
			}

			Vector2 initialVector = iSect1v;
			float stride = (2.0f * MathF.PI - iSectAng) / numSegments;
			for (int i = 0; i < numSegments; i++) {
				float startang = stride * i;
				float endang = stride * (i+1);
				DrawImGuiCircleSegmentBezier(center, rotate(initialVector, startang), rotate(initialVector, endang), stride, color);
			}
		}

		private void DrawBezierCircleSimple(Vector3 center, float radius, uint color, int numSegments) {
			float halfNumSegments = 0.5f * numSegments;
			for (int i = 0; i < numSegments; i++)
				DrawImGuiCircleSegmentBezier(center, i * MathF.PI / halfNumSegments, (i + 1) * MathF.PI / halfNumSegments, radius, color);
		}

		private void DrawSpoke(float spokeAngle, Vector2 pointAtPlayerFlatNorm, Vector3 target, float targetRadius, Vector2 screenSpaceTargetCenter, uint color, string label) {
			Vector2 spoke = new Vector2(MathF.Sin(spokeAngle), -MathF.Cos(spokeAngle));

			if (hideFarSpokes && Vector2.Dot(spoke, pointAtPlayerFlatNorm) < -0.382683432365f) // = cos(5pi/8 radians) = cos(112.5 degrees)
				return;

			bool spokeVisible = Services.BetterGameGui.WorldToScreen(target + targetRadius * new Vector3(spoke.X, 0.0f, spoke.Y), zbias, out Vector2 screenSpaceSpoke);

			if (!spokeVisible)
				return;

			ImGui.GetWindowDrawList().AddLine(screenSpaceTargetCenter, screenSpaceSpoke, color, lineWidth);
			if (drawSpokeLabels) {
				ImGui.GetWindowDrawList().AddText((screenSpaceTargetCenter + screenSpaceSpoke) * 0.5f, 0xFFFFFFFF, label);
			}
		}

		private void ImGuiMaxMelee(GameObject target, GameObject player) {
			float playerRadius = ringMode ? player.HitboxRadius : 0.0f;
			float targetRadius = ringMode ? target.HitboxRadius : target.HitboxRadius + player.HitboxRadius;

			if (ringMode && playerReach)
				playerRadius += 3.0f;
			else
				targetRadius += 3.0f;

			float heightOffset = relToTargetHeight ? target.Position.Y : player.Position.Y;
			heightOffset += height;

			Vector2 playerPositionFlat = new Vector2(player.Position.X, player.Position.Z);
			Vector2 targetPositionFlat = new Vector2(target.Position.X, target.Position.Z);

			float targetDistanceToPlayerFlat = Vector2.Distance(playerPositionFlat, targetPositionFlat);
			Vector2 toPlayerNorm = Vector2.Normalize(playerPositionFlat - targetPositionFlat);

			bool isInHitbox = targetDistanceToPlayerFlat <= targetRadius;
			bool isInMaxMelee = targetDistanceToPlayerFlat <= playerRadius + targetRadius;
			uint color = isInMaxMelee ? colorIfClose : colorIfFar;

			Vector3 playerCenter = new Vector3(player.Position.X, heightOffset, player.Position.Z);
			Vector3 targetCenter = new Vector3(target.Position.X, heightOffset, target.Position.Z);

			bool targetCenterVisible = Services.BetterGameGui.WorldToScreen(targetCenter, zbias, out Vector2 screenSpaceTargetCenter);
			Vector2 pointAtPlayerFlatNorm = Vector2.Normalize(playerPositionFlat - targetPositionFlat);

			if (drawTargetOrientation && targetCenterVisible)
				DrawSpoke(MathF.PI - target.Rotation, pointAtPlayerFlatNorm, targetCenter, targetRadius, screenSpaceTargetCenter, targetOrientationColor, "");

			if (drawSpokes && targetCenterVisible) {
				DrawSpoke(0.000000000000f, pointAtPlayerFlatNorm, targetCenter, targetRadius, screenSpaceTargetCenter, northSpokeColor, "N");
				DrawSpoke(1.570796326790f, pointAtPlayerFlatNorm, targetCenter, targetRadius, screenSpaceTargetCenter, cardinalSpokeColor, "E");
				DrawSpoke(3.141592653590f, pointAtPlayerFlatNorm, targetCenter, targetRadius, screenSpaceTargetCenter, cardinalSpokeColor, "S");
				DrawSpoke(4.712388980380f, pointAtPlayerFlatNorm, targetCenter, targetRadius, screenSpaceTargetCenter, cardinalSpokeColor, "W");
				DrawSpoke(0.785398163397f, pointAtPlayerFlatNorm, targetCenter, targetRadius, screenSpaceTargetCenter, intercardinalSpokeColor, "NE");
				DrawSpoke(2.356194490190f, pointAtPlayerFlatNorm, targetCenter, targetRadius, screenSpaceTargetCenter, intercardinalSpokeColor, "SE");
				DrawSpoke(3.926990816990f, pointAtPlayerFlatNorm, targetCenter, targetRadius, screenSpaceTargetCenter, intercardinalSpokeColor, "SW");
				DrawSpoke(5.497787143780f, pointAtPlayerFlatNorm, targetCenter, targetRadius, screenSpaceTargetCenter, intercardinalSpokeColor, "NW");
			}

			if (ringMode) {
				if (forceDrawTargetRing)
					DrawBezierCircleSimple(targetCenter, targetRadius, color, targetSegs);
				else
					DrawCircleAdvanced(targetCenter, toPlayerNorm, targetRadius, playerRadius, targetDistanceToPlayerFlat, color, targetSegs);

				if (drawPlayerRingInsideHitbox || targetDistanceToPlayerFlat >= targetRadius - playerRadius)
					DrawCircleAdvanced(playerCenter, -toPlayerNorm, playerRadius, targetRadius, targetDistanceToPlayerFlat, color, playerSegs);
			} else {
				DrawBezierCircleSimple(targetCenter, targetRadius, color, targetSegs);
			}

			if (dashLine && (drawDashLineInsideHitbox || !isInHitbox)) {
				Vector2 pointAtPlayerFlat = targetPositionFlat + targetRadius * pointAtPlayerFlatNorm;
				Vector2 pointAtTargetFlat;
				if (isInMaxMelee) {
					//if (playerReach)
					//	pointAtPlayerFlat = targetPositionFlat;
					pointAtTargetFlat = playerPositionFlat;
				} else {
					pointAtTargetFlat = playerPositionFlat - playerRadius * pointAtPlayerFlatNorm;
				}
				bool aboveHeadVisible = Services.BetterGameGui.WorldToScreen(new Vector3(pointAtTargetFlat.X, heightOffset, pointAtTargetFlat.Y), zbias, out Vector2 screenSpaceAboveHead);
				bool targetRingClosestVisible = Services.BetterGameGui.WorldToScreen(new Vector3(pointAtPlayerFlat.X, heightOffset, pointAtPlayerFlat.Y), zbias, out Vector2 screenSpaceTargetRingClosest);
				if (aboveHeadVisible && targetRingClosestVisible) {
					ImGui.GetWindowDrawList().AddLine(screenSpaceTargetRingClosest, screenSpaceAboveHead, color, lineWidth);
				}
				
				if (isInMaxMelee && aboveHeadVisible && (!playerCenterDot || !drawDotInsideHitbox))
					ImGui.GetWindowDrawList().AddCircleFilled(screenSpaceAboveHead, lineWidth, color);
				if (targetRingClosestVisible && !forceDrawTargetRing && Math.Abs(targetDistanceToPlayerFlat - targetRadius) < playerRadius)
					ImGui.GetWindowDrawList().AddCircleFilled(screenSpaceTargetRingClosest, lineWidth, color);
			}

			if ((playerCenterDot || !ringMode) && (drawDotInsideHitbox || !isInHitbox)) {
				if (Services.BetterGameGui.WorldToScreen(playerCenter, zbias, out Vector2 screenSpaceAboveHead))
					ImGui.GetWindowDrawList().AddCircleFilled(screenSpaceAboveHead, lineWidth, color);
			}
		}
		#endregion
	}
}
