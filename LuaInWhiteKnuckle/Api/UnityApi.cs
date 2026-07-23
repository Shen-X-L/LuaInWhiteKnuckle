using LuaInWhiteKnuckle.Registry;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace LuaInWhiteKnuckle.Api;

/// <summary>
/// 样条函数类
/// </summary>
[LuaData(typeof(AnimationCurve))]
[MoonSharpUserData]
public sealed class CurveData {
	[MoonSharpHidden]
	private AnimationCurve _curve;

	[MoonSharpHidden]
	public CurveData() {
		_curve = new AnimationCurve();
	}

	[MoonSharpHidden]
	public CurveData(AnimationCurve curve) {
		_curve = curve ?? new AnimationCurve();
	}

	public AnimationCurve Raw => _curve;

	#region[默认曲线]
	// 常数
	public static CurveData Constant(float value) {
		return new CurveData(
			new AnimationCurve(
				new Keyframe(0, value),
				new Keyframe(1, value)
			)
		);
	}
	// (x1,y1)->(x2,y2)线性
	public static CurveData Linear(float x1 = 0, float y1 = 0, float x2 = 1, float y2 = 1) =>
		new CurveData(AnimationCurve.Linear(x1, y1, x2, y2));

	// (0,1)->(1,0)反线性
	public static CurveData LinearFalloff() => new CurveData(AnimationCurve.Linear(0, 1, 1, 0));
	// 平方衰减 1-x^2
	public static CurveData SquareDecay() {
		AnimationCurve curve = new();
		// 采样点
		const int Samples = 16;
		for (int i = 0; i <= Samples; i++) {
			float x = i / (float)Samples;
			float y = 1 - x * x;
			curve.AddKey(x, y);
		}

		return new CurveData(curve);
	}
	// 指数衰减 e^(-S*x)
	public static CurveData ExponentialDecay(float strength) {
		strength = Mathf.Max(0.01f, strength);
		AnimationCurve curve = new();
		// 采样点
		const int Samples = 16;
		for (int i = 0; i <= Samples; i++) {
			float x = i / (float)Samples;
			float y = Mathf.Exp(-strength * x);
			curve.AddKey(x, y);
		}

		return new CurveData(curve);
	}

	#endregion

	//	求值
	public float Evaluate(float t) => _curve.Evaluate(t);

	// 采样
	public float[] Sample(int count) {
		if (count <= 0) return Array.Empty<float>();

		float[] values = new float[count];

		if (count == 1) {
			values[0] = _curve.Evaluate(0);
			return values;
		}

		float step = 1f / (count - 1);
		for (int i = 0; i < count; i++) values[i] = _curve.Evaluate(step * i);
		return values;
	}

	#region[控制点]

	public int count => _curve.length;

	public void Clear() => _curve.keys = Array.Empty<Keyframe>();

	public void Add(float time, float value) => _curve.AddKey(time, value);

	public void Insert(int index, float time, float value) {
		Keyframe[] keys = _curve.keys;

		index = Mathf.Clamp(index, 0, keys.Length);

		List<Keyframe> list = new(keys);
		list.Insert(index, new Keyframe(time, value));

		_curve.keys = list.ToArray();
	}

	public void Remove(int index) {
		Keyframe[] keys = _curve.keys;

		if ((uint)index >= keys.Length)
			return;

		List<Keyframe> list = new(keys);
		list.RemoveAt(index);

		_curve.keys = list.ToArray();
	}

	public CurveKeyData Get(int index) {
		if ((uint)index >= _curve.length)
			return null;

		Keyframe k = _curve[index];

		return new CurveKeyData(k.time, k.value);
	}

	public void Set(int index, float time, float value) {
		if ((uint)index >= _curve.length)
			return;

		Keyframe key = _curve[index];

		key.time = time;
		key.value = value;

		_curve.MoveKey(index, key);
	}

	#endregion

	public CurveData Clone() => new CurveData(_curve.Copy());
	public void ResetConstant(float value) => _curve = new AnimationCurve(new Keyframe(0, value), new Keyframe(1, value));
	public void ResetLinear() => _curve = AnimationCurve.Linear(0, 0, 1, 1);
	public void ResetLinearFalloff() => _curve = AnimationCurve.Linear(0, 1, 1, 0);
}

[MoonSharpUserData]
public class CurveKeyData {
	public float Time;
	public float Value;

	public CurveKeyData() { }

	public CurveKeyData(float time, float value) {
		Time = time;
		Value = value;
	}
}


[LuaData(typeof(Transform))]
[MoonSharpUserData]
public class TransformData {

	#region[基础包装]

	private readonly Transform _transform;

	[MoonSharpHidden]
	public TransformData(Transform transform) {
		_transform = transform;
	}
	[MoonSharpHidden]
	public Transform Raw => _transform;

	#endregion

	#region[基础属性]

	// 防止原版 Unity 物体已经被 Destroy 了,Lua 端还在高频调用导致报错
	private bool IsValid => _transform != null;

	public string name => IsValid ? _transform.name : null;

	public Vector3 position {
		get => IsValid ? _transform.position : Vector3.zero;
		set { if (IsValid) _transform.position = value; }
	}

	public Vector3 localPosition {
		get => IsValid ? _transform.localPosition : Vector3.zero;
		set { if (IsValid) _transform.localPosition = value; }
	}

	public Vector3 localScale {
		get => IsValid ? _transform.localScale : Vector3.one;
		set { if (IsValid) _transform.localScale = value; }
	}

	public Quaternion rotation {
		get => IsValid ? _transform.rotation : Quaternion.identity;
		set { if (IsValid) _transform.rotation = value; }
	}

	public Quaternion localRotation {
		get => IsValid ? _transform.localRotation : Quaternion.identity;
		set { if (IsValid) _transform.localRotation = value; }
	}

	#endregion

	#region [朝向向量]

	public Vector3 forward => IsValid ? _transform.forward : Vector3.forward;
	public Vector3 right => IsValid ? _transform.right : Vector3.right;
	public Vector3 up => IsValid ? _transform.up : Vector3.up;

	#endregion

	#region [状态控制]

	// 让物体朝向目标
	public void LookAt(Vector3 targetWorldPos) {
		if (IsValid) _transform.LookAt(targetWorldPos);
	}

	// 让物体移动
	public void Translate(Vector3 translation) {
		if (IsValid) _transform.Translate(translation);
	}

	// 让物体旋转
	public void Rotate(Vector3 eulerAngles) {
		if (IsValid) _transform.Rotate(eulerAngles);
	}

	#endregion

	#region[世界坐标<->本地坐标]

	// 位置+旋转+缩放
	// 本地位置->世界位置
	public Vector3 TransformPoint(Vector3 localPos)
		=> IsValid ? _transform.TransformPoint(localPos) : Vector3.zero;
	// 世界位置->本地位置
	public Vector3 InverseTransformPoint(Vector3 worldPos)
		=> IsValid ? _transform.InverseTransformPoint(worldPos) : Vector3.zero;

	// 旋转+缩放
	// 本地向量->世界向量
	public Vector3 TransformVector(Vector3 localVec)
		=> IsValid ? _transform.TransformVector(localVec) : Vector3.zero;
	// 世界向量->本地向量
	public Vector3 InverseTransformVector(Vector3 worldVec)
		=> IsValid ? _transform.InverseTransformVector(worldVec) : Vector3.zero;

	// 仅旋转
	// 本地方向->世界方向
	public Vector3 TransformDirection(Vector3 localDir)
		=> IsValid ? _transform.TransformDirection(localDir) : Vector3.zero;
	// 世界方向->本地方向
	public Vector3 InverseTransformDirection(Vector3 worldDir)
		=> IsValid ? _transform.InverseTransformDirection(worldDir) : Vector3.zero;

	#endregion

	#region[额外API]

	// 两点世界距离
	public float DistanceTo(TransformData other) {
		if (!IsValid || other == null || !other.IsValid)return float.NaN;
		return Vector3.Distance(_transform.position, other._transform.position);
	}

	public float DistanceTo(Transform other) {
		if (!IsValid || other == null)return float.NaN;
		return Vector3.Distance(_transform.position, other.position);
	}

	public float DistanceTo(Vector3 point) {
		if (!IsValid)return float.NaN;
		return Vector3.Distance(_transform.position, point);
	}


	// 朝向目标的单位向量
	public Vector3 DirectionTo(TransformData other) {
		if (!IsValid || other == null || !other.IsValid)return Vector3.zero;
		return (other._transform.position - _transform.position).normalized;
	}

	public Vector3 DirectionTo(Transform other) {
		if (!IsValid || other == null)return Vector3.zero;
		return (other.position - _transform.position).normalized;
	}

	public Vector3 DirectionTo(Vector3 point) {
		if (!IsValid)return Vector3.zero;
		return (point - _transform.position).normalized;
	}

	#endregion

}


[LuaData(typeof(RaycastHit))]
[MoonSharpUserData]
public sealed class RaycastHitData {
	public Vector3 point;// 交点
	public Vector3 normal;// 交点表明法向量
	public float distance;// 距离
	public Transform transform;// 命中物坐标

	public RaycastHitData(RaycastHit hit) {
		point = hit.point;
		normal = hit.normal;
		distance = hit.distance;
		transform = hit.transform;
	}
}

[LuaApi("PhysicsApi")]
[MoonSharpUserData]
public class PhysicsApi {

	/// <summary>
	/// 物理射线检测
	/// </summary>
	/// <param name="origin">起点</param>
	/// <param name="direction">方向</param>
	/// <param name="maxDistance">可选: 最大检测距离，默认 1000 米</param>
	/// <returns>击中时返回 RaycastHitData 实例，未击中返回 null (Lua 端为 nil)</returns>
	public RaycastHitData Raycast(Vector3 origin, Vector3 direction, float maxDistance = 1000f) {
		if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance)) 
			return new RaycastHitData(hit);
		return null;
	}
}

[LuaApi("Random")]
[MoonSharpUserData]
public class RandomApi {

	#region [均匀分布 (Uniform Distribution)]

	/// <summary>
	/// 浮点数随机数 [min, max]
	/// </summary>
	public float Range(float min, float max) => Random.Range(min, max);

	/// <summary>
	/// 整数随机数 [minInclusive, maxExclusive)
	/// </summary>
	public int RangeInt(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);

	/// <summary>
	/// 返回 0.0 到 1.0 之间的随机浮点数
	/// </summary>
	public float value => Random.value;

	/// <summary>
	/// 概率判定 (0 ~ 1)
	/// 例如 Chance(0.3) 有 30% 概率返回 true
	/// </summary>
	public bool Chance(float probability) {
		if (probability <= 0f) return false;
		if (probability >= 1f) return true;
		return Random.value < probability;
	}

	/// <summary>
	/// 返回半径为 1 的单位球体内的随机点 (Vector3)
	/// </summary>
	public Vector3 insideUnitSphere => Random.insideUnitSphere;

	/// <summary>
	/// 返回半径为 1 的单位圆内的随机点 (Vector2 -> Vector3)
	/// </summary>
	public Vector3 insideUnitCircle => Random.insideUnitCircle;

	/// <summary>
	/// 返回半径为 1 的球面上的随机点 (Vector3)
	/// </summary>
	public Vector3 onUnitSphere => Random.onUnitSphere;

	/// <summary>
	/// 返回随机旋转角度 (Quaternion)
	/// </summary>
	public Quaternion rotation => Random.rotation;

	#endregion

	#region [正态分布 / 高斯分布 (Normal / Gaussian Distribution)]

	/// <summary>
	/// 标准正态分布 (Box-Muller 变换)
	/// </summary>
	/// <param name="mean">均值 (期望值/中心点，默认 0)</param>
	/// <param name="stdDev">标准差 (偏差扩散程度，默认 1)</param>
	/// <returns>符合正态分布的随机值</returns>
	public float Gaussian(float mean = 0f, float stdDev = 1f) {
		// 避免 log(0)
		float u1 = 1.0f - Random.value;
		float u2 = 1.0f - Random.value;

		// Box-Muller 核心公式
		float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
		return mean + stdDev * randStdNormal;
	}

	/// <summary>
	/// 区间限制型正态分布 (Clamped Gaussian Range)
	/// 在 [min, max] 区间内生成正态分布随机数，数值高度集中在 (min + max) / 2 中心点附近。
	/// </summary>
	/// <param name="min">最小值</param>
	/// <param name="max">最大值</param>
	/// <param name="sigmaFactor">标准差系数 (默认 3，即 99.73% 的概率落在 min~max 之间，超出部分会被 Clamp 截断)</param>
	public float GaussianRange(float min, float max, float sigmaFactor = 3f) {
		if (min >= max) return min;

		float mean = (min + max) * 0.5f;
		float stdDev = (max - min) / (2f * sigmaFactor);

		float val = Gaussian(mean, stdDev);
		// 截断边界，保证 100% 落在 [min, max] 范围内
		return Mathf.Clamp(val, min, max);
	}

	/// <summary>
	/// 正态分布 2D 散布点 (常用于枪械弹道散布、爆炸碎片散布)
	/// 离中心点越近，落点概率越高
	/// </summary>
	public Vector3 GaussianCircle(float radius, float sigmaFactor = 3f) {
		float angle = Random.Range(0f, Mathf.PI * 2f);
		// 距离采用正态分布
		float distance = GaussianRange(0f, radius, sigmaFactor);

		return new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
	}

	#endregion
}