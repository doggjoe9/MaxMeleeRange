using Dalamud.Configuration;
using System;

namespace MaxMeleeRing {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public int Version { get; set; } = 1;
		
		public int playerSegs = 3;
		public int targetSegs = 27;
		public float height = 2.0f;
		public float lineWidth = 10.0f;
		public uint colorIfClose = 0xFF00FF00;
		public uint colorIfFar = 0xFF0000FF;
		public uint northSpokeColor = 0x7F00FFFF;
		public uint cardinalSpokeColor = 0x7FFFFFFF;
		public uint intercardinalSpokeColor = 0x3FFFFFFF;
		public uint targetOrientationColor = 0x7F00007F;
		public bool relToTargetHeight = false;
		public bool dashLine = true;
		public bool drawDashLineInsideHitbox = true;
		public bool drawSpokes = true;
		public bool hideFarSpokes = true;
		public bool drawSpokeLabels = true;
		public bool bossRelativeSpokes = false;
		public bool drawTargetOrientation = true;
		public bool ringMode = true;

		// Ring Mode Settings
		public bool playerReach = false;
		public bool playerCenterDot = false;
		public bool forceDrawTargetRing = false;
		public bool drawPlayerRingInsideHitbox = false;

		// Dot Mode Settings
		public bool drawDotInsideHitbox = false;


		public Configuration() { }

		public void Save() {
			Services.PluginInterface.SavePluginConfig(this);
		}
	}
}
