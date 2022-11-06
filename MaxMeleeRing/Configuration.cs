using Dalamud.Configuration;
using System;

namespace MaxMeleeRing {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public int Version { get; set; } = 0;
		
		public int playerSegs = 150;
		public int targetSegs = 500;
		public float height = 3.0f;
		public float lineWidth = 10.0f;
		public uint colorIfClose = 0xFF00FF00;
		public uint colorIfFar = 0xFF0000FF;
		public bool drawPlayerCircle = true;

		public Configuration() { }

		public void Save() {
			Services.PluginInterface.SavePluginConfig(this);
		}
	}
}
