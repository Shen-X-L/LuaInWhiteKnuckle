using HarmonyLib;
using LuaInWhiteKnuckle.Game;
using LuaInWhiteKnuckle.Registry;
using LuaInWhiteKnuckle.Runtime;
using MathNet.Numerics;
using MoonSharp.Interpreter;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using static BuffContainer;
using static Damageable;
using static GameEntity;
using static Steamworks.InventoryItem;

namespace LuaInWhiteKnuckle.Api;

#region[API类]

[LuaApi("Player")]
[MoonSharpUserData]
public class PlayerApi {

	private static readonly AccessTools.FieldRef<EntityBuff, Dictionary<string, Buff>> _finalBuffsField =
		AccessTools.FieldRefAccess<EntityBuff, Dictionary<string, Buff>>("finalBuffs");

	internal static ENT_Player _player = null;

	#region[玩家Entity属性API]

	// 当前生命值
	public float health { get => _player.health; set => _player.health = value; }
	// 最大生命值
	public float maxHealth { get => _player.maxHealth; set => _player.maxHealth = value; }
	// 火焰时间乘数
	public float fireTimeMult { get => _player.fireTimeMult; set => _player.fireTimeMult = value; }
	// 火焰伤害乘数
	public float fireDamageMult { get => _player.fireDamageMult; set => _player.fireDamageMult = value; }

	public ObjectTagger tagger { get => _player.gameObject.GetComponent<ObjectTagger>(); }

	#endregion

	#region[玩家基础属性API]

	// 基础移动速度
	public float speed { get => _player.speed ; set => _player.speed = value; }
	// 冲刺速度
	public float sprintSpeed { get => _player.sprintSpeed; set => _player.sprintSpeed = value; }
	// 蹲伏速度
	public float crouchSpeed { get => _player.crouchSpeed; set => _player.crouchSpeed = value; }
	// 游泳速度
	public float swimSpeed { get => _player.swimSpeed; set => _player.swimSpeed = value; }
	// 跳跃高度
	public float jumpHeight { get => _player.jumpHeight; set => _player.jumpHeight = value; }
	// 额外跳跃次数
	public int extraJumps { get => _player.extraJumps; set => _player.extraJumps = value; }
	// 临时额外跳跃次数
	public int temporaryExtraJumpsRemaining => Patch_ENT_Player._temporaryExtraJumpsRemainingField(_player);
	// 生命恢复速率
	public float healingRate { get => _player.healingRate; set => _player.healingRate = value; }
	// 生命恢复延迟
	public float healDelay { get => _player.healDelay; set => _player.healDelay = value; }
	// 攀爬跳跃消耗
	public float climbJumpDrain { get => _player.climbJumpDrain; set => _player.climbJumpDrain = value; }
	// 重力加速度 -9.81
	public float gravity { get => _player.gravity; set => _player.gravity = value; }
	// 重力倍率 1
	public float gravityMult => Patch_ENT_Player._gravityMultField(_player);
	// 交互距离
	public float interactDistance {
		get => _player.interactDistance;
		set => _player.interactDistance = value;
	}

	#endregion

	#region[玩家调试属性API]

	// 无碰撞模式
	public bool noclip { get => _player.noclip; set => _player.noclip = value; }
	// 飞行模式
	public bool fly { get => _player.fly; set => _player.fly = value; }
	// 强制随处抓取
	public bool forceGrabAnywhere => Patch_ENT_Player._forceGrabAnywhereField(_player);
	// 无限耐力
	public bool infiniteStamina => Patch_ENT_Player._infiniteStaminaField(_player);
	// 上帝模式
	public bool godmode => Patch_ENT_Player._godmodeField(_player);
	// 无限弹药
	public bool infiniteAmmo => Patch_ENT_Player._infiniteAmmoField(_player);

	#endregion

	#region[玩家BUFF API]

	/// <summary>
	/// 快速添加一个增益到玩家身上
	/// </summary>
	/// <param name="id">buff ID (重要 需要匹配游戏本身buff的ID)</param>
	/// <param name="amount">效率倍率</param>
	/// <param name="buffTime">buff时长 (秒)</param>
	/// <param name="loseRate">衰减速率 (每一秒减x秒)</param>
	/// <param name="loseOverTime">是否随时间衰减</param>
	public void AddBuff(
		string id, float amount, float buffTime = 1f,
		float loseRate = 0.1f, bool loseOverTime = true, string containerId = "") {

		if (_player == null) return;
		var buffContainer = new BuffContainer {
			id = containerId,
			desc = "",
			buffs = new List<Buff> {
				new Buff {
					id = id,
					maxAmount = amount,
				}
			},
			buffTime = buffTime,
			loseRate = loseRate,
			loseOverTime = loseOverTime,
		};
		_player.curBuffs.AddBuff(buffContainer);
	}

	/// <summary>
	/// 获取特定BUFF
	/// </summary>
	public float GetBuff(string id) => _player.curBuffs.GetBuff(id);

	/// <summary>
	/// 获取特定BUFF
	/// </summary>
	public Dictionary<string, Buff> GetAllBuff() {
		if (_player.curBuffs != null)
			return _finalBuffsField(_player.curBuffs);
		return null;
	}

	/// <summary>
	/// 获取全部buff容器
	/// </summary>
	public List<BuffContainer> GetAllBuffContainer() =>
		new List<BuffContainer>(_player.curBuffs.currentBuffs);

	/// <summary>
	/// 添加一个增益容器到玩家身上
	/// </summary>
	/// <param name="buffContainerData"></param>
	public void AddBuffContainer(BuffContainerData buffContainerData) =>
		_player.curBuffs.AddBuff(buffContainerData.Raw);

	/// <summary>
	/// 清空全部buff 
	/// </summary>
	public void RemoveAllBuff() => _player.curBuffs.currentBuffs.Clear();

	/// <summary>
	/// 根据ID删除容器
	/// </summary>
	public void RemoveBuffContainer(string id) => _player.curBuffs.RemoveBuffContainer(id);

	/// <summary>
	/// 删除容器
	/// </summary>
	public void RemoveBuffContainer(BuffContainerData buffContainerData) =>
		_player.curBuffs.RemoveBuffContainer(buffContainerData.Raw);

	/// <summary>
	/// 删除容器
	/// </summary>
	public void RemoveBuffContainer(BuffContainer buffContainer) =>
		_player.curBuffs.RemoveBuffContainer(buffContainer);

	#endregion

	#region[玩家操控API]

	// 相机锁定
	public bool camLocked => Patch_ENT_Player._camLockedField(_player);
	// 移动锁定
	public bool moveLocked => Patch_ENT_Player._moveLockedField(_player);

	public bool sprinting => _player.IsSprinting();

	public bool crouching => _player.IsCrouching();

	public bool swimming => _player.IsSwimming();

	public bool hanging => _player.IsHanging();

	#endregion

	public Vector3 vel => Patch_ENT_Player._velField(_player);

	public Vector3 pos => _player.transform.position;

	public Quaternion rot => _player.transform.rotation;

	/// <summary>
	/// 消除速度向量中与V方向相同的分量 再施加力
	/// </summary>
	/// <param name="v"></param>
	public void SetDirectionalForce(Vector3 v) => _player.SetDirectionalForce(v);

	/// <summary>
	/// 消除速度向量中与V方向相同的分量 再施加力
	/// </summary>
	/// <param name="v"></param>
	public void SetDirectionalForce(float x, float y, float z) =>
		_player.SetDirectionalForce(new Vector3(x, y, z));


	/// <summary>
	/// 给玩家造成伤害
	/// </summary>
	public void Damage(DamageInfoData damage) => _player.Damage(damage.Raw);


	/// <summary>
	/// 屏幕震动
	/// </summary>
	public void Shake(float strength) => CL_CameraControl.Shake(strength);
}

#endregion

#region[数据类]

[LuaData(typeof(HandData))]
[MoonSharpUserData]
public class HandData {
	private readonly ENT_Player.Hand _hand;

	[MoonSharpHidden]
	public HandData(ENT_Player.Hand hand) {
		_hand = hand;
		id = hand.id;
	}

	[MoonSharpHidden]
	public ENT_Player.Hand Raw => _hand;

	public HandData(int index) {
		var hands = ENT_Player.GetPlayer().hands;
		if (index < 0 || index >= hands.Length)
			return;
		_hand = hands[index];
		id = hands[index].id;
	}

	// 手部ID(0=左手, 1=右手)
	public readonly int id;

	#region[耐力]

	// 手部耐力
	public float gripStrength { get => _hand.gripStrength; set => _hand.gripStrength = value; }
	// 最大耐力   
	public float maxGripStrength => _hand.GetGripStrengthMax();

	#endregion

	#region[数值]

	// 阻力倍数
	public float dragMult {get => _hand.dragMult;set => _hand.dragMult = value;}
	// 抓握延迟
	public float grabWait { get => _hand.grabWait; set => _hand.grabWait = value; }

	#endregion

	#region[锁定/解锁]

	// 手部交互锁定
	public bool lockHand {get => _hand.lockHand;set => _hand.lockHand = value;}
	// 交互锁定冷却时间
	public float cooldown {get => _hand.cooldown;set => _hand.cooldown = value;}
	// 是否可交互
	public bool canInteract { get => _hand.CanInteract(); set => _hand.SetCanInteract(value); }
	// 移动锁定状态
	public bool moveLocked { get => _hand.IsLocked(); set => _hand.SetLocked(value); }

	#endregion

	#region[状态确认]

	// 是否 无交互 && 无物品
	public bool IsFree() => _hand.IsFree();
	public bool IsHolding() => _hand.IsHolding();
	public bool IsHanging() => _hand.IsHanging();

	#endregion

	/// <summary>
	/// 松开手部
	/// </summary>
	/// <param name="skipWait">是否不使用抓握延迟</param>
	public void DropHand(bool skipWait = false) => _hand.DropHand(skipWait);

	/// <summary>
	/// 移动手部到指定位置
	/// </summary>
	/// <param name="pos">目标位置</param>
	/// <param name="blend">插值混合系数</param>
	public void MoveTo(Vector3 pos, float blend = 1f) => _hand.MoveTo(pos, blend);

	#region[物品相关]

	public void DropItem() => _hand.DropItem();
	public void DropItem(Vector3 pos) => _hand.DropItem(pos);
	public void DestroyItem() => _hand.DestroyItem();
	public HandItem GetHandItem() => _hand.GetHandItem();

	#endregion

}

#endregion

#region[监听器/补丁类]

[HarmonyPatch(typeof(ENT_Player))]
public static class Patch_ENT_Player {

	public static readonly AccessTools.FieldRef<ENT_Player, bool> _godmodeField =
		AccessTools.FieldRefAccess<ENT_Player, bool>("godmode");
	public static readonly AccessTools.FieldRef<ENT_Player, bool> _forceGrabAnywhereField =
		AccessTools.FieldRefAccess<ENT_Player, bool>("forceGrabAnywhere");
	public static readonly AccessTools.FieldRef<ENT_Player, bool> _infiniteStaminaField =
		AccessTools.FieldRefAccess<ENT_Player, bool>("infiniteStamina");
	public static readonly AccessTools.FieldRef<ENT_Player, bool> _infiniteAmmoField =
		AccessTools.FieldRefAccess<ENT_Player, bool>("infiniteAmmo");
	public static readonly AccessTools.FieldRef<ENT_Player, bool> _moveLockedField =
		AccessTools.FieldRefAccess<ENT_Player, bool>("moveLocked");
	public static readonly AccessTools.FieldRef<ENT_Player, bool> _camLockedField =
		AccessTools.FieldRefAccess<ENT_Player, bool>("camLocked");

	public static readonly AccessTools.FieldRef<ENT_Player, Vector3> _velField =
		AccessTools.FieldRefAccess<ENT_Player, Vector3>("vel");

	public static readonly AccessTools.FieldRef<ENT_Player, int> _temporaryExtraJumpsRemainingField =
		AccessTools.FieldRefAccess<ENT_Player, int>("temporaryExtraJumpsRemaining");
	public static readonly AccessTools.FieldRef<ENT_Player, float> _gravityMultField =
		AccessTools.FieldRefAccess<ENT_Player, float>("gravityMult");

	[HarmonyPatch("Start")]
	[HarmonyPostfix]
	public static void Patch_Start() {
		PlayerApi._player = ENT_Player.GetPlayer();
		InventoryApi._inventory = Inventory.instance;
	}

	#region[玩家受伤HOOK]

	public const string DAMAGE_HOOK = "OnPlayerDamage";

	[HarmonyPatch(nameof(ENT_Player.Damage))]
	[HarmonyPrefix]
	public static bool Patch_Damage(ENT_Player __instance, ref DamageInfo info) {
		if (_godmodeField(__instance))
			return true;
		// 使用 using 块接 离开括号作用域时会自动调用 Dispose 清理
		using (var guard = LuaHookGuard.Enter(DAMAGE_HOOK)) {
			// 如果发生重入 直接放行本体并退出
			if (!guard.CanExecute)
				return true;
			try {
				// 触发事件
				ModEventBus.TriggerEvent(DAMAGE_HOOK, info);
				// 检查是否有修改/拦截型的 Hook
				if (!Plugin.safeLuaSandbox.Api.Hooks.Contains(DAMAGE_HOOK))
					return true;
				// 获取Lua返回 伤害数据,是否免疫
				var (damageInfoData, immunity) = ModHookBus.InvokeHook<(DamageInfoData, bool)>(
					DAMAGE_HOOK, new DamageInfoData(info));
				// 本次伤害免疫
				if (immunity)
					return false;
				if (damageInfoData != null) {
					info = damageInfoData.Raw;
				}
			} catch (Exception e) {
				Plugin.LogError($"[LuaInWK] HOOK异常 {DAMAGE_HOOK}: {e.Message}");
			}
			return true;
		}
	}

	#endregion

	#region[玩家死亡HOOK]

	public const string KILL_HOOK = "OnPlayerKill";
	private static float _immunityEndTime;

	[HarmonyPatch(nameof(ENT_Player.Kill))]
	[HarmonyPrefix]
	public static bool Patch_Kill(ENT_Player __instance, ref string type, ref Damageable.DamageInfo damageInfo) {
		// 原版死亡机制免疫
		if (__instance.dead || CL_GameManager.gMan.IsReviving() || _godmodeField(__instance))
			return true;
		// 内设死亡免疫时间
		if (_immunityEndTime > Time.time)
			return false;
		// 使用 using 块接 离开括号作用域时会自动调用 Dispose 清理
		using (var guard = LuaHookGuard.Enter(KILL_HOOK)) {
			// 如果发生重入 直接放行本体并退出
			if (!guard.CanExecute)
				return true;
			try {
				// 触发事件
				ModEventBus.TriggerEvent(KILL_HOOK, type, damageInfo);
				// 检查是否有修改/拦截型的 Hook
				if (!Plugin.safeLuaSandbox.Api.Hooks.Contains(KILL_HOOK))
					return true;
				// 获取Lua返回数据
				var (typeData, damageInfoData, immunity, immunityTime) =
					ModHookBus.InvokeHook<(string, DamageInfoData, bool, float)>(
						KILL_HOOK, type, new DamageInfoData(damageInfo));
				Plugin.LogTest($"[LuaInWK] type: {typeData} immunity: {immunity} immunityTime {immunityTime}");
				// 本次死亡免疫
				if (immunity) {
					immunityTime = immunityTime <= 0f ? 0.1f : immunityTime;
					_immunityEndTime = Time.time + immunityTime;
					return false;
				}
				if (damageInfoData != null) {
					damageInfo = damageInfoData.Raw;
				}
				if (typeData != null) {
					type = typeData;
				}
			} catch (Exception e) {
				Plugin.LogError($"[LuaInWK] HOOK异常 {KILL_HOOK}: {e.Message}");
			}
			return true;
		}
	}

	#endregion
}


[HarmonyPatch(typeof(EntityBuff))]
public static class Patch_EntityBuff {
	#region[玩家获得BUFF]

	public const string GET_BUFF_HOOK = "OnPlayerAddBuff";

	[HarmonyPatch(nameof(EntityBuff.AddBuff))]
	[HarmonyPrefix]
	public static bool Patch_AddBuff(EntityBuff __instance, ref BuffContainer buffContainer) {
		// 不是玩家 直接返回
		if (__instance.gameEntity is not ENT_Player player)
			return true;
		// 使用 using 块接 离开括号作用域时会自动调用 Dispose 清理
		using (var guard = LuaHookGuard.Enter(GET_BUFF_HOOK)) {
			// 如果发生重入 直接放行本体并退出
			if (!guard.CanExecute)
				return true;
			try {
				// 触发事件
				ModEventBus.TriggerEvent(GET_BUFF_HOOK, buffContainer);
				// 检查是否有修改/拦截型的 Hook
				if (!Plugin.safeLuaSandbox.Api.Hooks.Contains(GET_BUFF_HOOK))
					return true;
				// 获取修改后buff 是否禁用
				var (buffContainerData, isDisabled) = ModHookBus.InvokeHook<(BuffContainerData, bool)>(
					GET_BUFF_HOOK, new BuffContainerData(buffContainer));
				// 本次buff不使用
				if (isDisabled)
					return false;
			} catch (Exception e) {
				Plugin.LogError($"[LuaInWK] HOOK异常 {GET_BUFF_HOOK}: {e.Message}");
			}
			return true;
		}
	}

	#endregion
}

#endregion