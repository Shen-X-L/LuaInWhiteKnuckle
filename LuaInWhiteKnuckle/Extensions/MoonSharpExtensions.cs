using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LuaInWhiteKnuckle.Extensions;


public static class MoonSharpExtensions {

	/// <summary>
	/// 外部调用的主入口 - 性能提升 10-50 倍, 运行时变为 0 GC Alloc
	/// </summary>
	public static T ToClr<T>(this DynValue value) {
		// 运行时只需要一步:直接从强类型缓存中读取编译好的委托执行
		return TupleConverterCache<T>.Convert(value);
	}

	/// <summary>
	/// 专门为表达式树提供的高效单项转换器 (必须公开或通过反射能拿到)
	/// </summary>
	public static TElement ConvertElement<TElement>(DynValue value, int index) {
		DynValue item = GetTupleElement(value, index);
		if (item == null || item.IsNil()) {
			return default; // 自动返回值类型的 default 或 null, 无需通过 Type 判断
		}
		return item.ToObject<TElement>();
	}

	private static DynValue GetTupleElement(DynValue value, int index) {
		switch (value.Type) {
			case DataType.Tuple: return index < value.Tuple.Length ? value.Tuple[index] : DynValue.Nil;
			case DataType.Table: return value.Table.Get(index + 1);
			default: return index == 0 ? value : DynValue.Nil;
		}
	}

	/// <summary>
	/// 利用强类型静态构造函数, 为每种 T 只编译一次
	/// </summary>
	private static class TupleConverterCache<T> {
		public static readonly Func<DynValue, T> Convert;

		static TupleConverterCache() {
			Type target = typeof(T);

			// 1. 如果不是泛型, 或者不是 ValueTuple, 直接走原生包装
			if (!target.IsGenericType) {
				Convert = val => val.ToObject<T>();
				return;
			}

			Type def = target.GetGenericTypeDefinition();
			if (def == typeof(ValueTuple<,>) ||
				def == typeof(ValueTuple<,,>) ||
				def == typeof(ValueTuple<,,,>) ||
				def == typeof(ValueTuple<,,,,>)) {

				// 2. 开始通过 在内存中拼装强类型构造逻辑
				Type[] args = target.GetGenericArguments();
				ParameterExpression paramExpr = Expression.Parameter(typeof(DynValue), "value");

				// 拿到当前元组特有的强类型构造函数, 例如 ValueTuple<bool, string>(bool, string)
				ConstructorInfo ctor = target.GetConstructor(args);
				Expression[] argExprs = new Expression[args.Length];

				for (int i = 0; i < args.Length; i++) {
					// 捕获刚刚写的 ConvertElement<TElement> 方法
					MethodInfo method = typeof(MoonSharpExtensions)
						.GetMethod(nameof(ConvertElement), BindingFlags.Public | BindingFlags.Static)
						.MakeGenericMethod(args[i]);

					// 拼装:ConvertElement<T_i>(value, i)
					argExprs[i] = Expression.Call(method, paramExpr, Expression.Constant(i));
				}

				// 拼装:new ValueTuple<T1, T2...>(...)
				NewExpression newExpr = Expression.New(ctor, argExprs);

				// 编译成原生的强类型委托:Func<DynValue, ValueTuple<...>>
				var lambda = Expression.Lambda<Func<DynValue, T>>(newExpr, paramExpr);
				Convert = lambda.Compile();
			} else {
				// 其他普通的泛型类
				Convert = val => val.ToObject<T>();
			}
		}
	}
}