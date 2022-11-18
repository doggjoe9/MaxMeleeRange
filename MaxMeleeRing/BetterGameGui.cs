using Dalamud.Game;
using Dalamud.Interface;
using Dalamud.Logging;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MaxMeleeRing {

	
	internal sealed class BetterGameGuiAddressResolver : BaseAddressResolver {
		public IntPtr GetMatrixSingleton { get; private set; }

		protected override void Setup64Bit(SigScanner scanner) {
			GetMatrixSingleton = scanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
		}
	}

	public sealed unsafe class BetterGameGui {
		private readonly GetMatrixSingletonDelegate getMatrixSingleton;

		private readonly BetterGameGuiAddressResolver address;

		public BetterGameGui(SigScanner sigScanner) {
			address = new BetterGameGuiAddressResolver();
			address.Setup(sigScanner);

			getMatrixSingleton = Marshal.GetDelegateForFunctionPointer<GetMatrixSingletonDelegate>(address.GetMatrixSingleton);
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr GetMatrixSingletonDelegate();

		public bool WorldToScreen(Vector3 worldPos, float zbias, out Vector2 screenPos) {
			// Get base object with matrices
			IntPtr matrixSingleton = getMatrixSingleton();

			// Read current ViewProjectionMatrix plus game window size
			float[] matrixBuffer = new float[12];

			float width, height;
			var windowPos = ImGuiHelpers.MainViewport.Pos;

			float x = 0.0f, y = 0.0f, z = 0.0f;

			unsafe
			{
				var rawMatrix = (float*)(matrixSingleton + 0x1b4).ToPointer();

				x += worldPos.X * *(rawMatrix++);
				y += worldPos.X * *(rawMatrix++);
				z += worldPos.X * *(rawMatrix++);
				rawMatrix++;

				x += worldPos.Y * *(rawMatrix++);
				y += worldPos.Y * *(rawMatrix++);
				z += worldPos.Y * *(rawMatrix++);
				rawMatrix++;

				x += worldPos.Z * *(rawMatrix++);
				y += worldPos.Z * *(rawMatrix++);
				z += worldPos.Z * *(rawMatrix++);
				rawMatrix++;

				x += *(rawMatrix++);
				y += *(rawMatrix++);
				z += *(rawMatrix++);
				rawMatrix++;

				width = *(rawMatrix++);
				height = *rawMatrix;
			}

			Vector3 pCoords = new Vector3(x, y, z);

			PluginLog.Information(pCoords.ToString());

			screenPos = new Vector2(pCoords.X / pCoords.Z, pCoords.Y / pCoords.Z);

			screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
			screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

			return pCoords.Z > zbias; /*&&
				   screenPos.X > windowPos.X && screenPos.X < windowPos.X + width &&
				   screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;*/
			
		}
	}
}
