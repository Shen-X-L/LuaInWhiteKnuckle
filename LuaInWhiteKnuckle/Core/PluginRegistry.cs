using LuaInWhiteKnuckle.Collections;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LuaInWhiteKnuckle.Core;

public static class PluginRegistry {
	// API单例 对象注册表
	private static readonly Dictionary<string, object> _apis = new();
	// 扫描程序集
	private static readonly HashSet<Assembly> _assemblies = new();
	// 全局静态API 类型注册表
	private static readonly HashSet<Type> _userDataType = new();
	public static readonly object GenericRegisterLock;
	/// <summary>
	/// 扫描当前程序集
	/// </summary>
	public static void Initialize() {
		RegisterAssembly(typeof(PluginRegistry).Assembly);
		EnableJitGenericRegistration();
	}

	/// <summary>
	/// 扫描程序集LuaApiAttribute特性
	/// </summary>
	/// <param name="assembly"></param>
	public static void RegisterAssembly(Assembly assembly) {
		if (!_assemblies.Add(assembly))
			return;

		Type[] types;
		try {
			types = assembly.GetTypes();
		} catch (ReflectionTypeLoadException ex) {
			Plugin.LogError($"[LuaInWK] 扫描程序集 {assembly.FullName} 失败");

			foreach (var loader in ex.LoaderExceptions) {
				Plugin.LogError(loader.ToString());
			}
			throw;
		}

		foreach (Type type in types) {
			var apiAttr = type.GetCustomAttribute<LuaApiAttribute>();
			var dataAttr = type.GetCustomAttribute<LuaDataAttribute>();
			var moonAttr = type.GetCustomAttribute<MoonSharpUserDataAttribute>();
			if (apiAttr == null && moonAttr == null && dataAttr == null) continue;
			if (type.IsAbstract) continue;
			if (type.IsInterface) continue;
			UserData.RegisterType(type);
			if (apiAttr != null) {
				if (type.GetConstructor(Type.EmptyTypes) == null) {
					Plugin.LogError($"[LuaInWK] {type.Name} 缺少无参构造函数");
					continue;
				}
				object instance = Activator.CreateInstance(type);
				_apis[apiAttr.Name] = instance;
				Plugin.LogInfo($"[LuaInWK] 注册 {type} 到API");
			}
			if (dataAttr != null) {
				_userDataType.Add(type);
				if (type.GetConstructor(new Type[] { dataAttr.Type }) == null) {
					Plugin.LogError($"[LuaInWK] {type.Name} 缺少转换构造函数");
					continue;
				}
				RegisterProxy(type, dataAttr.Type);
				Plugin.LogInfo($"[LuaInWK] 注册 {dataAttr.Type} 到 {type} 的自动转换");
			}
		}
	}

	#region[运行期注册API对象]

	/// <summary>
	/// 基于路径注册 API 到 Lua环境, 例如 "Game.Events" 会在 Lua 中创建 Game 表,并在其下创建 Events 表
	/// </summary>
	/// <param name="script"></param>
	/// <param name="path"></param>
	/// <param name="api"></param>
	private static void RegisterPath(Script script, string path, object api) {
		string[] parts = path.Split('.');
		// 获取全局表
		Table current = script.Globals;
		// 解析路径并构建变量
		for (int i = 0; i < parts.Length - 1; i++) {
			DynValue value = current.Get(parts[i]);
			if (value.Type != DataType.Table) {
				Table table = new Table(script);
				current[parts[i]] = table;
				current = table;
			} else {
				current = value.Table;
			}
		}
		// 将API对象移至此处
		current[parts[^1]] = api;
	}

	/// <summary>
	/// 将类型的 静态函数 和 构造函数 注册到 Lua环境
	/// </summary>
	/// <param name="script"></param>
	/// <param name="type"></param>
	private static void RegisterStatic(Script script, Type type) {
		DynValue staticProxy = UserData.CreateStatic(type);
		// 创建暴露给 Lua 的类包装表 (Class Wrapper Table)
		Table classWrapper = new Table(script);
		// 创建元表 (Metatable)
		Table metaTable = new Table(script);
		// 将 __index 指向静态代理对象 当 Lua 尝试调用 MyClass.StaticMethod() 时,会自动转发给 MoonSharp 的静态代理
		metaTable["__index"] = staticProxy;
		// 核心: 配置 __call 元方法, 拦截 Lua 的实例化操作 当 Lua 执行 `MyClass(arg1, arg2)` 时,触发此回调
		metaTable["__call"] = new CallbackFunction((ctx, args) => {
			// 使用 Index 而不是 MetaIndex 来获取 __new
			DynValue newMethod = staticProxy.UserData.Descriptor.Index(
				ctx.OwnerScript, staticProxy.UserData.Object,
				DynValue.NewString("__new"), false);
			// args.GetArray() 包含: [0]=表本身, [1]=第一个参数, [2]=第二个参数...
			var ctorArgs = new List<DynValue>();
			for (int i = 1; i < args.Count; i++) {
				ctorArgs.Add(args[i]);
			}
			// 用 Callback 直接调用 __new包装
			return newMethod.Callback.Invoke(ctx, ctorArgs, false);
		});
		// 绑定元表
		classWrapper.MetaTable = metaTable;
		// 封装的包装表注册到全局变量中
		script.Globals[type.Name] = classWrapper;
	}

	// 注册 API对象 到 API注册表 会在Build后构建, Lua环境运行后无法生成
	public static void Register(string name, object api) {
		_apis[name] = api;
	}
	// 取消注册 API对象 到 API注册表 Lua环境运行后无法消除
	public static void Unregister(string name) {
		_apis.Remove(name);
	}

	/// <summary>
	/// 进行 单例对象和其路径注册 和 静态函数和构造函数 注册
	/// </summary>
	public static void Build(Script script) {
		// 单例对象和其路径注册
		foreach (var kv in _apis)
			RegisterPath(script, kv.Key, kv.Value);
		// 静态函数和构造函数
		foreach (var type in _userDataType)
			RegisterStatic(script, type);
	}

	#endregion

	#region[代理类注册]

	// 注册代理类型到 MoonSharp 的 UserData 系统中
	public static void RegisterProxy(Type proxyType, Type targetType) {
		// 获取构造函数
		ConstructorInfo ctor = proxyType.GetConstructor(new[] { targetType });
		// 构建表达式树的参数
		ParameterExpression obj = Expression.Parameter(typeof(object), "obj");
		// 构建表达式树的主体,New 调用构造函数,Convert 调用类型转换
		NewExpression body = Expression.New(ctor, Expression.Convert(obj, targetType));
		UnaryExpression cast = Expression.Convert(body, typeof(object));
		// 表达式树编译成Func<object, object>
		Func<object, object> factory =
			Expression.Lambda<Func<object, object>>(cast, obj)
				.Compile();
		// 生成代理工厂
		UserData.RegisterProxyType(new DynamicProxyFactory(proxyType, targetType, factory));
	}

	// 注册泛型代理类型到 MoonSharp 的 UserData 系统中
	public static void RegisterGenericProxy(Type proxyOpenType, Type targetOpenType, params Type[] genericArgs) {
		try {
			// 在运行时闭合泛型类型
			Type closedProxyType = proxyOpenType.MakeGenericType(genericArgs);
			Type closedTargetType = targetOpenType.MakeGenericType(genericArgs);
			// 构建闭合泛型并注册代理
			RegisterProxy(closedProxyType, closedTargetType);
			Plugin.LogInfo($"[LuaInWK] 注册泛型代理: {closedTargetType.Name} -> {closedProxyType.Name}");
		} catch (Exception ex) {
			Plugin.LogError($"[LuaInWK] 注册泛型代理失败 ({proxyOpenType.Name}): {ex.Message}");
		}
	}

	#endregion

	/// <summary>
	/// 开启运行时 JIT 泛型自动捕获注册
	/// 在脚本引擎初始化时调用一次此方法即可。
	/// </summary>
	public static void EnableJitGenericRegistration() {
		Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<IList>((script, list) => {
			Type targetType = list.GetType();
			// 类型不是泛型 || 闭合泛型的开放泛型不是List
			if (!targetType.IsGenericType || targetType.GetGenericTypeDefinition() != typeof(List<>)) 
				return UserData.Create(list);
			// 获取泛型参数
			Type[] args = targetType.GetGenericArguments();
			// 构建 闭合泛型类型
			Type proxyType = typeof(LuaList<>).MakeGenericType(args);
			// 查看该 闭合泛型类型代理类LuaList<T> 是否注册
			if (UserData.IsTypeRegistered(proxyType)) 
				return UserData.Create(list);// 已经注册过了
			// 注册 闭合泛型类型代理类LuaList<T>
			UserData.RegisterType(proxyType);
			// 调用 开放泛型代理注册 双向转换代理
			RegisterGenericProxy(typeof(LuaList<>), typeof(List<>), args);
			Plugin.LogInfo($"[JIT泛型] 动态注册了 LuaList<{args[0].Name}>");
			return UserData.Create(list);
		});
		// 同理：拦截所有 Dictionary<K, V> (通过 IDictionary 接口拦截)
		Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<IDictionary>((script, dict) => {
			Type targetType = dict.GetType();
			// 类型不是泛型 || 闭合泛型的开放泛型不是Dictionary<,>
			if (!targetType.IsGenericType || targetType.GetGenericTypeDefinition() != typeof(Dictionary<,>)) 
				return UserData.Create(dict);
			// 获取泛型参数
			Type[] typeArgs = targetType.GetGenericArguments();
			// 构建 闭合泛型类型
			Type proxyType = typeof(LuaDictionary<,>).MakeGenericType(typeArgs);
			// 查看该 闭合泛型类型代理类LuaDictionary<K,V> 是否注册
			if (UserData.IsTypeRegistered(proxyType)) 
				return UserData.Create(dict);// 已经注册过了
			// 注册 闭合泛型类型代理类LuaDictionary<K,V>
			UserData.RegisterType(proxyType);
			// 调用 开放泛型代理注册 双向转换代理
			RegisterGenericProxy(typeof(LuaDictionary<,>), typeof(Dictionary<,>), typeArgs);
			Plugin.LogInfo($"[JIT泛型] 动态捕获并注册了: Dictionary<{typeArgs[0].Name}, {typeArgs[1].Name}>");
			return UserData.Create(dict);
		});
	}

	//代理工厂类
	public class DynamicProxyFactory : IProxyFactory {
		public Type ProxyType { get; }

		public Type TargetType { get; }

		private readonly Func<object, object> _factory;

		public DynamicProxyFactory(
			Type proxyType,
			Type targetType,
			Func<object, object> factory) {
			ProxyType = proxyType;
			TargetType = targetType;
			_factory = factory;
		}

		public object CreateProxyObject(object obj) {
			return _factory(obj);
		}
	}
}

