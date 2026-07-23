using LuaInWhiteKnuckle.Registry;
using MoonSharp.Interpreter;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuaInWhiteKnuckle.Api;

[LuaApi("Other")]
[MoonSharpUserData]
public class OtherApi {
	private const int Z = 0;

	public void ExecuteCommand(string command) {
		CommandConsole.instance?.ExecuteCommand(command, false);
	}

	public Transform SpawnObject(string prefabName, Vector3 pos, Quaternion rot) {
		GameObject prefabObject = CL_AssetManager.GetAssetGameObject(prefabName);
		if (prefabObject != null) return UnityEngine.Object.Instantiate(prefabObject, pos, rot).transform;
		return null;
	}

	public Transform SpawnEntity(string prefabName, Vector3 pos, Vector3 forward) {
		// 获取预制体
		GameObject prefab = CL_AssetManager.GetAssetGameObject(prefabName);
		// 是否存在
		if (prefab == null) return null;
		// 是否是实体
		if (!prefab.TryGetComponent<GameEntity>(out var gameEntity)) return null;
		// 进行射线检测
		if (Physics.Raycast(pos, forward, out var hitInfo, float.PositiveInfinity)) {
			var position = hitInfo.point;
			var identity = Quaternion.LookRotation(hitInfo.normal);

			GameEntity newEntity = UnityEngine.Object.Instantiate(gameEntity, position, identity);
			if (newEntity.spawnWithBounds) {
				Collider col = newEntity.GetComponent<Collider>();
				if (col != null) {
					Bounds bounds = col.bounds;
					Vector3 offset = hitInfo.normal * bounds.extents.magnitude;
					newEntity.transform.position += offset;
				}
			}
			return newEntity.transform;
		}
		return null;
	}

	public Transform SpawnEntity(string prefabName, Vector3 pos, Quaternion rot) {
		GameObject prefab = CL_AssetManager.GetAssetGameObject(prefabName);
		if (prefab == null) return null;
		if (!prefab.TryGetComponent<GameEntity>(out var gameEntity)) return null;
		return UnityEngine.Object.Instantiate(prefab, pos, rot).transform;
	}
}