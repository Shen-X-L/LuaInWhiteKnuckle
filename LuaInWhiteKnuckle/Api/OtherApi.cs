using LuaInWhiteKnuckle.Registry;
using MoonSharp.Interpreter;
using UnityEngine;

namespace LuaInWhiteKnuckle.Api;

[LuaApi("Other")]
[MoonSharpUserData]
public class OtherApi {	private const int Z = 0;

	public void ExecuteCommand(string command) {
		CommandConsole.instance?.ExecuteCommand(command, false);
	}

	public void SpawnEntity(string prefabName,Vector3 pos,Quaternion rot) {
		GameObject prefabObject = CL_AssetManager.GetAssetGameObject(prefabName);
		if (prefabObject != null) UnityEngine.Object.Instantiate(prefabObject, pos, rot);
	}
}
