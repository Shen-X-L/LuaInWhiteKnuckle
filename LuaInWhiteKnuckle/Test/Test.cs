using DG.Tweening.Plugins.Core;
using LuaInWhiteKnuckle.Core;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;




class Program {
	const int ITEM_COUNT = 100000;

	public readonly static List<string> TestSet = new List<string> {
		"perfTest","listTest","typeTest"
	};

	public static void Main(string[] args) {
		var arg = string.Join(" ", args);

		switch (arg) {
			case "perfTest": PerformanceTest(); break;
			case "listTest": ListTest(); break;
			case "typeTest": TypeTest(); break;
			default: Plugin.LogTest("Usage: TestData.exe perf"); break;
		}
	}

	#region[List测试相关]

	[MoonSharpUserData]
	public class TestListData {
		public List<int> intList = new();
		public List<string> stringList = new();
		public int[] intArray;
		public string[] stringArray;
	}

	static void ListTest() {
		UserData.RegisterType<TestListData>();

		var data = new TestListData {
			intList = new List<int> { 1, 2, 3 },
			stringList = new List<string> { "A", "B", "C" },

			intArray = new[] { 10, 20, 30 },
			stringArray = new[] { "AA", "BB", "CC" }
		};

		var script = new Script(CoreModules.Preset_Complete);

		script.Globals["data"] = data;

		script.Globals["print"] = new CallbackFunction((ctx, args) => {
			StringBuilder sb = new();
			for (int i = 0; i < args.Count; i++) {
				sb.Append(args[i].ToPrintString());
			}
			Plugin.LogTest(sb.ToString());
			return DynValue.Nil;
		});

		script.DoString(@"

local function SafeCall(name, fn)

    local ok, ret = pcall(fn)

    if ok then
        print(""[OK]"", name, tostring(ret))
    else
        print(""[ERR]"", name, tostring(ret))
    end

end


local function Dump(name,obj)

    print()
    print(""=================================================="")
    print(name)
    print(""=================================================="")

    print(""type       ="",type(obj))
    print(""tostring   ="",tostring(obj))

    SafeCall(""Count"",function() return obj.Count end)
    SafeCall(""Length"",function() return obj.Length end)
    SafeCall(""Capacity"",function() return obj.Capacity end)

    print()

    print(""----- Index -----"")

    for i=1,10 do
        SafeCall(""[""..i..""]"",function()
            return obj[i]
        end)
    end

    print()

    print(""----- ipairs -----"")

    SafeCall(""ipairs"",function()

        for k,v in ipairs(obj) do
            print(k,v)
        end

    end)

    print()

    print(""----- pairs -----"")

    SafeCall(""pairs"",function()

        for k,v in pairs(obj) do
            print(type(k),k,type(v),tostring(v))
        end

    end)

    print()

end


local function TestList(name,list)

    Dump(name,list) -- 正常

    print()
    print(""===== Method Test ====="")

    SafeCall(""Contains"",function()
        return list:Contains(list[1])
    end)

    SafeCall(""IndexOf"",function()
        return list:IndexOf(list[1])
    end)

    SafeCall(""Add"",function()

        if type(list[1])==""number"" then
            list:Add(99999)
        else
            list:Add(""MoonSharp"")
        end

    end)

    SafeCall(""Insert"",function()

        if type(list[1])==""number"" then
            list:Insert(2,88888)
        else
            list:Insert(2,""Insert"")
        end

    end)

    SafeCall(""Remove"",function()

        list:Remove(list[1])

    end)

    SafeCall(""RemoveAt"",function()

        list:RemoveAt(1)

    end)

    SafeCall(""Reverse"",function()

        list:Reverse()

    end)

    SafeCall(""Sort"",function()

        list:Sort()

    end)

    SafeCall(""Clear"",function()

        list:Clear()

    end)

    print()

    print(""===== Index Write ====="")

    SafeCall(""set[1]"",function()

        if type(list[1])==""number"" then
            list[1]=123456
        else
            list[1]=""Modified""
        end

    end)

    Dump(name.."" After"",list)

end



local function TestArray(name,array)

    Dump(name,array)

    print()

    SafeCall(""set[1]"",function()

        if type(array[1])==""number"" then
            array[1]=654321
        else
            array[1]=""Changed""
        end

    end)

    Dump(name.."" After"",array)

end


----------------------------------------------------

TestList(""List<int>"",data.intList)

----------------------------------------------------

TestList(""List<string>"",data.stringList)

----------------------------------------------------

TestArray(""int[]"",data.intArray)

----------------------------------------------------

TestArray(""string[]"",data.stringArray)

");

		Plugin.LogTest("");
		Plugin.LogTest("============= CLR RESULT =============");

		Plugin.LogTest($"List<int> Count = {data.intList.Count}");
		Plugin.LogTest(string.Join(",", data.intList));

		Plugin.LogTest("");
		
		Plugin.LogTest($"List<string> Count = {data.stringList.Count}");
		Plugin.LogTest(string.Join(",", data.stringList));

		Plugin.LogTest("");

		Plugin.LogTest($"int[] Length = {data.intArray.Length}");
		Plugin.LogTest(string.Join(",", data.intArray));

		Plugin.LogTest("");

		Plugin.LogTest($"string[] Length = {data.stringArray.Length}");
		Plugin.LogTest(string.Join(",", data.stringArray));
	}

	#endregion

	#region[性能测试相关]

	public class TestData {
		public int IntValue;
		public float FloatValue;
		public bool BoolValue;
		public double DoubleValue;
		public long LongValue;
	}

	[MoonSharpUserData]
	public class ProxyItem {
		private readonly TestData _item;

		public ProxyItem(TestData item) {
			_item = item;
		}

		public int IntValue {
			get => _item.IntValue;
			set => _item.IntValue = value;
		}

		public float FloatValue {
			get => _item.FloatValue;
			set => _item.FloatValue = value;
		}

		public bool BoolValue {
			get => _item.BoolValue;
			set => _item.BoolValue = value;
		}

		public double DoubleValue {
			get => _item.DoubleValue;
			set => _item.DoubleValue = value;
		}

		public long LongValue {
			get => _item.LongValue;
			set => _item.LongValue = value;
		}
	}

	public class CopyItem {
		private readonly TestData _item;

		public int IntValue;
		public float FloatValue;
		public bool BoolValue;
		public double DoubleValue;
		public long LongValue;

		public CopyItem(TestData item) {
			_item = item;

			IntValue = item.IntValue;
			FloatValue = item.FloatValue;
			BoolValue = item.BoolValue;
			DoubleValue = item.DoubleValue;
			LongValue = item.LongValue;
		}

		public void Apply() {
			_item.IntValue = IntValue;
			_item.FloatValue = FloatValue;
			_item.BoolValue = BoolValue;
			_item.DoubleValue = DoubleValue;
			_item.LongValue = LongValue;
		}
	}

	public static void PerformanceTest() {
		Plugin.LogTest($"MoonSharp Benchmark ({ITEM_COUNT} Items)");
		Plugin.LogTest(new string('-', 60));

		UserData.RegisterType<ProxyItem>();
		UserData.RegisterType<CopyItem>();

		TestProxy();
		TestDto();
		TestDtoBatchApply();
		TestLuaTable();
		TestPureClr();
		TestLuaEmptyLoop();

		Plugin.LogTest();
		Plugin.LogTest("Finished.");
	}

	static void Print(string title, Stopwatch sw) {
		double totalMs = sw.Elapsed.TotalMilliseconds;
		double perItemUs = sw.Elapsed.TotalMilliseconds * 1000.0 / ITEM_COUNT;

		Plugin.LogTest($"{title}");
		Plugin.LogTest($"Total : {totalMs:F2} ms");
		Plugin.LogTest($"Per Item : {perItemUs:F4} us");
		Plugin.LogTest();
	}

	static void TestProxy() {
		var script = new Script();

		ProxyItem[] items = new ProxyItem[ITEM_COUNT];

		for (int i = 0; i < ITEM_COUNT; i++) {
			items[i] = new ProxyItem(new TestData() {
				IntValue = i,
				FloatValue = i,
				BoolValue = true,
				LongValue = i,
				DoubleValue = i
			});
		}

		script.Globals["items"] = items;

		Stopwatch sw = Stopwatch.StartNew();

		script.DoString(@"
for i = 1,#items do

    local item = items[i]

    -- Read
    local a = item.IntValue
    local b = item.FloatValue
    local c = item.BoolValue
    local d = item.LongValue
    local e = item.DoubleValue

    -- Modify
    item.IntValue = a + 1
    item.FloatValue = b + 1
    item.BoolValue = not c
    item.LongValue = d + 1
    item.DoubleValue = e + 1

    -- Read Again
    local aa = item.IntValue
    local bb = item.FloatValue
    local cc = item.BoolValue
    local dd = item.LongValue
    local ee = item.DoubleValue

end
");

		sw.Stop();

		Print("Proxy Property", sw);
	}

	static void TestDto() {
		var script = new Script();

		CopyItem[] items = new CopyItem[ITEM_COUNT];

		for (int i = 0; i < ITEM_COUNT; i++) {
			items[i] = new CopyItem(new TestData() {
				IntValue = i,
				FloatValue = i,
				BoolValue = true,
				LongValue = i,
				DoubleValue = i
			});
		}

		script.Globals["items"] = items;

		Stopwatch sw = Stopwatch.StartNew();

		script.DoString(@"
for i = 1,#items do

    local item = items[i]

    -- Read
    local a = item.IntValue
    local b = item.FloatValue
    local c = item.BoolValue
    local d = item.LongValue
    local e = item.DoubleValue

    -- Modify
    item.IntValue = a + 1
    item.FloatValue = b + 1
    item.BoolValue = not c
    item.LongValue = d + 1
    item.DoubleValue = e + 1

    item:Apply()

    -- Read Again
    local aa = item.IntValue
    local bb = item.FloatValue
    local cc = item.BoolValue
    local dd = item.LongValue
    local ee = item.DoubleValue

end
");

		sw.Stop();

		Print("DTO + Apply", sw);
	}

	static void TestLuaTable() {
		var script = new Script();

		script.DoString($@"

items={{}}

for i=1,{ITEM_COUNT} do

    items[i]=
    {{
        IntValue=i,
        FloatValue=i,
        BoolValue=true,
        LongValue=i,
        DoubleValue=i
    }}

end

");

		Stopwatch sw = Stopwatch.StartNew();

		script.DoString(@"
for i = 1,#items do

    local item = items[i]

    -- Read
    local a = item.IntValue
    local b = item.FloatValue
    local c = item.BoolValue
    local d = item.LongValue
    local e = item.DoubleValue

    -- Modify
    item.IntValue = a + 1
    item.FloatValue = b + 1
    item.BoolValue = not c
    item.LongValue = d + 1
    item.DoubleValue = e + 1

    -- Read Again
    local aa = item.IntValue
    local bb = item.FloatValue
    local cc = item.BoolValue
    local dd = item.LongValue
    local ee = item.DoubleValue

end
");

		sw.Stop();

		Print("Lua Table", sw);
	}

	static void TestDtoBatchApply() {
		var script = new Script();

		CopyItem[] items = new CopyItem[ITEM_COUNT];

		for (int i = 0; i < ITEM_COUNT; i++) {
			items[i] = new CopyItem(new TestData() {
				IntValue = i,
				FloatValue = i,
				BoolValue = true,
				LongValue = i,
				DoubleValue = i
			});
		}

		script.Globals["items"] = items;

		Stopwatch sw = Stopwatch.StartNew();

		script.DoString(@"
for i = 1,#items do

    local item = items[i]

    local a = item.IntValue
    local b = item.FloatValue
    local c = item.BoolValue
    local d = item.LongValue
    local e = item.DoubleValue

    item.IntValue = a + 1
    item.FloatValue = b + 1
    item.BoolValue = not c
    item.LongValue = d + 1
    item.DoubleValue = e + 1

    local aa = item.IntValue
    local bb = item.FloatValue
    local cc = item.BoolValue
    local dd = item.LongValue
    local ee = item.DoubleValue

end
");

		// 最后统一写回
		for (int i = 0; i < ITEM_COUNT; i++) {
			items[i].Apply();
		}

		sw.Stop();

		Print("DTO Batch Apply", sw);
	}

	static void TestPureClr() {
		TestData[] items = new TestData[ITEM_COUNT];

		for (int i = 0; i < ITEM_COUNT; i++) {
			items[i] = new TestData() {
				IntValue = i,
				FloatValue = i,
				BoolValue = true,
				LongValue = i,
				DoubleValue = i
			};
		}

		Stopwatch sw = Stopwatch.StartNew();

		for (int i = 0; i < ITEM_COUNT; i++) {
			var item = items[i];

			int a = item.IntValue;
			float b = item.FloatValue;
			bool c = item.BoolValue;
			long d = item.LongValue;
			double e = item.DoubleValue;

			item.IntValue = a + 1;
			item.FloatValue = b + 1;
			item.BoolValue = !c;
			item.LongValue = d + 1;
			item.DoubleValue = e + 1;

			int aa = item.IntValue;
			float bb = item.FloatValue;
			bool cc = item.BoolValue;
			long dd = item.LongValue;
			double ee = item.DoubleValue;
		}

		sw.Stop();

		Print("Pure CLR", sw);
	}

	static void TestLuaEmptyLoop() {
		var script = new Script();

		Stopwatch sw = Stopwatch.StartNew();

		script.DoString($@"
for i=1,{ITEM_COUNT} do

    local a = i
    local b = i
    local c = i
    local d = i
    local e = i

end
");

		sw.Stop();

		Print("Lua Empty Loop", sw);
	}

	#endregion

	#region[获取Lua可调用测试]

	public static class DebugApi {
		public static string[] GetLuaMembers(Type type,bool searchInterfaces) {
			var desc = UserData.GetDescriptorForType(type, searchInterfaces);

			Plugin.LogTest("desc type:" + desc.Type);

			if (desc is not StandardUserDataDescriptor std) {
				return Array.Empty<string>();
			}

			Table t = new Table(new Script());

			std.PrepareForWiring(t);

			Table members = t.Get("members").Table;

			List<string> result = new();

			foreach (var pair in members.Pairs) {
				result.Add(pair.Key.String);
			}

			result.Sort();

			return result.ToArray();
		}
	}

	public static void TypeTest() {
		foreach (var s in DebugApi.GetLuaMembers(typeof(List<int>), true))
			Plugin.LogTest(s);
	}

	#endregion
}