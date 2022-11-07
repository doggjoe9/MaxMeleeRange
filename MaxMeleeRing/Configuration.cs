using Dalamud.Configuration;
using System;

namespace MaxMeleeRing {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public int Version { get; set; } = 0;
		
		public int playerSegs = 3;
		public int targetSegs = 27;
		public float height = 2.0f;
		public float lineWidth = 10.0f;
		public uint colorIfClose = 0xFF00FF00;
		public uint colorIfFar = 0xFF0000FF;
		public bool relToTargetHeight = false;
		public bool dashLine = true;
		public bool drawDashLineInsideHitbox = true;
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
