using LuaInWhiteKnuckle.Registry;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
			float y = 1-x*x;
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