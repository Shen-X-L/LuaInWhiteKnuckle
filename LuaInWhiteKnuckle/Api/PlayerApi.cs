using HarmonyLib;
using LuaInWhiteKnuckle.Game;
using LuaInWhiteKnuckle.Registry;
using LuaInWhiteKnuckle.Runtime;
using LuaInWhiteKnuckle.Util;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using static BuffContainer;
using static Damageable;
using static GameEntity;
using static Perk;


namespace LuaInWhiteKnuckle.Api;

#region[API类]

[LuaApi("Player")]
[MoonSharpUserData]
public class PlayerApi {

	private static readonly AccessTools.FieldRef<EntityBuff, Dictionary<string, Buff>> _finalBuffsField =
		AccessTools.FieldRefAccess<EntityBuff, Dictionary<string, Buff>>("finalBuffs");

	internal static ENT_Player _player = null;

	// 玩家标签
	public ObjectTagger tagger { get => _player.gameObject.GetComponent<ObjectTagger>(); }

	public ENT_Player.Hand leftHand => _player.hands[0];
	public ENT_Player.Hand rightHand => _player.hands[1];

	#region[Entity属性API]

	// 火焰时间乘数
	public float fireTimeMult { get => _player.fireTimeMult; set => _player.fireTimeMult = value; }
	// 火焰伤害乘数
	public float fireDamageMult { get => _player.fireDamageMult; set => _player.fireDamageMult = value; }
	/// <summary>
	/// 给玩家造成伤害
	/// </summary>
	public void Damage(DamageInfoData damage) => _player.Damage(damage.Raw);

	#endregion

	#region[移动属性API]

	// 基础移动速度
	public float speed { get => _player.speed; set => _player.speed = value; }
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
	public int temporaryExtraJumpsRemaining {
		get => Patch_ENT_Player._temporaryExtraJumpsRemainingField(_player);
		set => Patch_ENT_Player._temporaryExtraJumpsRemainingField(_player) = value;
	}
	// 攀爬跳跃消耗
	public float climbJumpDrain { get => _player.climbJumpDrain; set => _player.climbJumpDrain = value; }

	#endregion

	#region[生命属性API]

	// 当前生命值
	public float health { get => _player.health; set => _player.health = value; }
	// 最大生命值
	public float maxHealth { get => _player.maxHealth; set => _player.maxHealth = value; }
	// 生命恢复速率
	public float healingRate { get => _player.healingRate; set => _player.healingRate = value; }
	// 生命恢复延迟
	public float healDelay { get => _player.healDelay; set => _player.healDelay = value; }

	#endregion

	#region[抓握属性API]

	// 耐力计时器
	public float gripStrengthTimer {
		get => _player.gripStrengthTimer;
		set => _player.gripStrengthTimer = value;
	}
	// 交互距离
	public float interactDistance {
		get => _player.interactDistance;
		set => _player.interactDistance = value;
	}
	// 抓取抓握点的力
	public float propHangForce {
		get => _player.propHangForce;
		set => _player.propHangForce = value;
	}
	// 抓取宽容时间
	public float grabForgive {
		get => _player.grabForgive;
		set => _player.grabForgive = value;
	}
	// 增加双手耐力
	public void AddGripStrength(float amount, bool onlyToGrabbingHands = false) =>
		_player.AddGripStrength(amount, onlyToGrabbingHands);
	// 减少双手耐力
	public void DamageGripStrength(float amount, bool onlyToGrabbingHands = false) =>
		_player.DamageGripStrength(amount, onlyToGrabbingHands);
	// 设置双手耐力
	public void SetGripStrength(float amount, bool onlyToGrabbingHands = false) =>
		_player.SetGripStrength(amount, onlyToGrabbingHands);
	// 获取当前最大耐力
	public float GetCurrentGripStrengthTimer(int id = -1) => _player.GetCurrentGripStrengthTimer(id);
	// 获取交互距离(受增益和玩家缩放影响)
	public float GetInteractDistance(int id = -1) => _player.GetInteractDistance(id);
	// 获取基础交互距离(不受手部增益影响)
	public float GetBaseInteractDistance() => _player.GetBaseInteractDistance();
	// 是否是无攀爬模式
	public bool? FocusModeOverride {
		get => Patch_ENT_Player.FocusModeOverride;
		set => Patch_ENT_Player.FocusModeOverride = value;
	}

	#endregion

	#region[物理参数API]

	// 重力加速度 -9.81
	public float gravity { get => _player.gravity; set => _player.gravity = value; }
	// 重力倍率 1
	public float gravityMult {
		get => Patch_ENT_Player._gravityMultField(_player);
		set => Patch_ENT_Player._gravityMultField(_player) = value;
	}
	// 阻力系数
	public float dragCoefficient { get => _player.dragCoefficient; set => _player.dragCoefficient = value; }
	// 摩擦力乘数
	public float frictionMultiplier {
		get => _player.frictionMultiplier;
		set => _player.frictionMultiplier = value;
	}
	// 墙壁摩擦力乘数
	public float wallfrictionMultiplier {
		get => _player.wallfrictionMultiplier;
		set => _player.wallfrictionMultiplier = value;
	}
	// 游泳摩擦力乘数
	public float swimFrictionMultiplier {
		get => _player.swimFrictionMultiplier;
		set => _player.swimFrictionMultiplier = value;
	}

	#endregion

	#region[斜坡与滑动]

	// 可攀爬坡度限制
	public float slopeLimit { get => _player.slopeLimit; set => _player.slopeLimit = value; }
	// 滑行摩擦力
	public float slideFriction { get => _player.slideFriction; set => _player.slideFriction = value; }
	// 滑行速度
	public float slideSpeed { get => _player.slideSpeed; set => _player.slideSpeed = value; }
	// 空中控制系数
	public float airControl { get => _player.airControl; set => _player.airControl = value; }

	#endregion

	#region[调试属性API]

	// 无碰撞模式
	public bool noclip { get => _player.noclip; set => _player.noclip = value; }
	// 飞行模式
	public bool fly { get => _player.fly; set => _player.fly = value; }
	// 强制随处抓取
	public bool forceGrabAnywhere {
		get => Patch_ENT_Player._forceGrabAnywhereField(_player);
		set => Patch_ENT_Player._forceGrabAnywhereField(_player) = value;
	}
	// 无限耐力
	public bool infiniteStamina {
		get => Patch_ENT_Player._infiniteStaminaField(_player);
		set => Patch_ENT_Player._infiniteStaminaField(_player) = value;
	}
	// 上帝模式
	public bool godmode {
		get => Patch_ENT_Player._godmodeField(_player);
		set => Patch_ENT_Player._godmodeField(_player) = value;
	}
	// 无限弹药
	public bool infiniteAmmo {
		get => Patch_ENT_Player._infiniteAmmoField(_player);
		set => Patch_ENT_Player._infiniteAmmoField(_player) = value;
	}
	// 无限充能
	public bool infiniteCharge {
		get => Patch_ENT_Player._infiniteChargeField(_player);
		set => Patch_ENT_Player._infiniteChargeField(_player) = value;
	}

	#endregion

	#region[Perk API]

	// perk数组
	public List<Perk> perks { get => _player.perks; }
	// 添加perk
	public Perk AddPerk(Perk perk, int stackAmount = 1, bool firstTime = true) => 
		_player.AddPerk(perk, stackAmount, firstTime);
	// 移除全部perk
	public void RemoveAllPerks(bool forceRemoveAll = true) => _player.RemoveAllPerks(forceRemoveAll);
	// 移除指定perk
	public void RemovePerk(Perk perk, bool removeAll = true) => _player.RemovePerk(perk, removeAll);
	// 通过ID移除perk
	public void RemovePerk(string perkID, bool removeAll = true) => _player.RemovePerk(perkID, removeAll);
	// 获取指定perk
	public Perk GetPerk(string perkID) => _player.GetPerk(perkID);

	#endregion

	#region[BUFF API]

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
	public void AddBuffContainer(BuffContainer buffContainer) =>
		_player.curBuffs.AddBuff(buffContainer);
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
	public void RemoveBuffContainer(BuffContainer buffContainer) =>
		_player.curBuffs.RemoveBuffContainer(buffContainer);
	public void RemoveBuffContainer(BuffContainerData buffContainerData) =>
		_player.curBuffs.RemoveBuffContainer(buffContainerData.Raw);

	#endregion

	#region[操控API]

	// 相机锁定
	public bool camLocked {
		get => Patch_ENT_Player._camLockedField(_player);
		set => Patch_ENT_Player._camLockedField(_player) = value;
	}
	// 移动锁定
	public bool moveLocked {
		get => Patch_ENT_Player._moveLockedField(_player);
		set => Patch_ENT_Player._moveLockedField(_player) = value;
	}
	public bool sprinting => _player.IsSprinting();

	public bool crouching => _player.IsCrouching();

	public bool swimming => _player.IsSwimming();

	public bool hanging => _player.IsHanging();

	#endregion

	#region[运动学API]

	// 位置向量
	public Vector3 pos => _player.transform.position;
	// 旋转四元数
	public Quaternion rot => _player.transform.rotation;
	// 速度向量
	public Vector3 vel {
		get => Patch_ENT_Player._velField(_player);
		set => Patch_ENT_Player._velField(_player) = value;
	}

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

	#endregion

	#region[其他API]

	/// <summary>
	/// 屏幕震动
	/// </summary>
	public void Shake(float strength) => CL_CameraControl.Shake(strength);

	#endregion

}

#endregion

#region[数据类]

[LuaData(typeof(ENT_Player.Hand))]
[MoonSharpUserData]
public class HandData {

	#region[基础包装]

	private readonly ENT_Player.Hand _hand;

	[MoonSharpHidden]
	public HandData(ENT_Player.Hand hand) {
		_hand = hand;
		id = hand.id;
	}

	[MoonSharpHidden]
	public ENT_Player.Hand Raw => _hand;

	#endregion

	public HandData(int index) {
		var hands = ENT_Player.GetPlayer().hands;
		if (index < 0 || index >= hands.Length)
			return;
		_hand = hands[index];
		id = hands[index].id;
	}

	// 手部ID(0=左手, 1=右手)
	public readonly int id;

	#region[耐力相关]

	// 手部耐力
	public float gripStrength { get => _hand.gripStrength; set => _hand.gripStrength = value; }
	// 最大耐力   
	public float maxGripStrength => _hand.GetGripStrengthMax();
	// 添加耐力(不超过最大值)
	public void AddGripStrength(float amount) => _hand.AddGripStrength(amount);
	// 设置耐力(不超过最大值)
	public void SetGripStrength(float amount) => _hand.SetGripStrength(amount);
	// 减小耐力
	public void DamageGripStrength(float damageAmount) => _hand.DamageGripStrength(damageAmount);

	#endregion

	#region[抓握相关]

	// 阻力倍数
	public float dragMult { get => _hand.dragMult; set => _hand.dragMult = value; }
	// 抓握延迟
	public float grabWait { get => _hand.grabWait; set => _hand.grabWait = value; }

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

	/// <summary>
	/// 手部的本地坐标
	/// </summary>
	public Vector3 holdPosition {
		get => _hand.GetHoldPosition();
		set => _hand.SetHoldPosition(value);
	}

	/// <summary>
	/// 手部的世界坐标
	/// </summary>
	public Vector3 holdWorldPosition {
		get => _hand.GetHoldWorldPosition();
		set => _hand.SetWorldHoldPosition(value);
	}

	/// <summary>
	/// 手部状态
	/// </summary>
	public string interactState => EnumStringMapper<ENT_Player.InteractType>.GetString(_hand.interactState);

	#endregion

	#region[锁定/解锁]

	// 手部交互锁定
	public bool lockHand { get => _hand.lockHand; set => _hand.lockHand = value; }
	// 交互锁定冷却时间
	public float cooldown { get => _hand.cooldown; set => _hand.cooldown = value; }
	// 是否可交互
	public bool canInteract { get => _hand.CanInteract(); set => _hand.SetCanInteract(value); }
	// 移动锁定状态
	public bool moveLocked { get => _hand.IsLocked(); set => _hand.SetLocked(value); }

	#endregion

	#region[状态访问]

	// 是否 无交互 && 无物品
	public bool IsFree() => _hand.IsFree();
	public bool IsHolding() => _hand.IsHolding();
	public bool IsHanging() => _hand.IsHanging();

	#endregion

	#region[物品相关]

	// 在手部模型处丢弃道具
	public void DropItem() => _hand.DropItem();
	// 丢弃道具到pos
	public void DropItem(Vector3 pos) => _hand.DropItem(pos);
	// 摧毁手部道具
	public void DestroyItem() => _hand.DestroyItem();
	// 获取手部物品
	public HandItem GetHandItem() => _hand.GetHandItem();

	#endregion

	#region[动画相关]

	public void ShakeHand(float amount) => _hand.ShakeHand(amount);

	public void RockHand(float amount) => _hand.RockHand(amount);

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
	public static readonly AccessTools.FieldRef<ENT_Player, bool> _infiniteChargeField =
		AccessTools.FieldRefAccess<ENT_Player, bool>("infiniteCharge");

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

	#region[专注模式]

	// 是否强制不攀爬模式
	public static bool? FocusModeOverride = null;

	[HarmonyPatch("IsFocusModeActive")]
	[HarmonyPrefix]
	public static bool Prefix(ref bool __result) {
		switch (FocusModeOverride) {
			case true:
				__result = true;
				return false;    // 跳过原函数
			case false:
				__result = false;
				return false;    // 跳过原函数
			default: return true;// 执行原函数
		}
	}

	#endregion

	#region[玩家受伤HOOK]

	[HarmonyPatch(nameof(ENT_Player.Damage))]
	[HarmonyPrefix]
	public static bool Patch_Damage(ENT_Player __instance, ref DamageInfo info) {
		const string HOOK_NAME = "OnPlayerDamage";
		if (_godmodeField(__instance)) return true;
		// 使用 using 块接 离开括号作用域时会自动调用 Dispose 清理
		using (var guard = LuaHookGuard.Enter(HOOK_NAME)) {
			// 如果发生重入 直接放行本体并退出
			if (!guard.CanExecute) return true;
			try {
				// 触发事件
				ModEventBus.TriggerEvent(HOOK_NAME, info);
				// 检查是否有修改/拦截型的 Hook
				if (!Plugin.safeLuaSandbox.Api.Hooks.Contains(HOOK_NAME)) return true;
				// 获取Lua返回 伤害数据,是否免疫
				var (damageInfoData, immunity) = ModHookBus.InvokeHook<(DamageInfoData, bool)>(
					HOOK_NAME, new DamageInfoData(info));
				// 本次伤害免疫
				if (immunity) return false;
				if (damageInfoData != null) info = damageInfoData.Raw;
			} catch (Exception e) {
				Plugin.LogError($"[LuaInWK] HOOK异常 {HOOK_NAME}: {e.Message}");
			}
			return true;
		}
	}

	#endregion

	#region[玩家死亡HOOK]

	private static float _immunityEndTime;

	[HarmonyPatch(nameof(ENT_Player.Kill))]
	[HarmonyPrefix]
	public static bool Patch_Kill(ENT_Player __instance, ref string type, ref Damageable.DamageInfo damageInfo) {
		const string HOOK_NAME = "OnPlayerKill";
		// 原版死亡机制免疫
		if (__instance.dead || CL_GameManager.gMan.IsReviving() || _godmodeField(__instance)) return true;
		// 内设死亡免疫时间
		if (_immunityEndTime > Time.time) return false;
		// 使用 using 块接 离开括号作用域时会自动调用 Dispose 清理
		using (var guard = LuaHookGuard.Enter(HOOK_NAME)) {
			// 如果发生重入 直接放行本体并退出
			if (!guard.CanExecute) return true;
			try {
				// 触发事件
				ModEventBus.TriggerEvent(HOOK_NAME, type, damageInfo);
				// 检查是否有修改/拦截型的 Hook
				if (!Plugin.safeLuaSandbox.Api.Hooks.Contains(HOOK_NAME)) return true;
				// 获取Lua返回数据
				var (typeData, damageInfoData, immunity, immunityTime) =
					ModHookBus.InvokeHook<(string, DamageInfoData, bool, float)>(
						HOOK_NAME, type, new DamageInfoData(damageInfo));
				// 本次死亡免疫
				if (immunity) {
					immunityTime = immunityTime <= 0f ? 0.1f : immunityTime;
					_immunityEndTime = Time.time + immunityTime;
					return false;
				}
				if (damageInfoData != null) damageInfo = damageInfoData.Raw;
				if (typeData != null) type = typeData;
			} catch (Exception e) {
				Plugin.LogError($"[LuaInWK] HOOK异常 {HOOK_NAME}: {e.Message}");
			}
			return true;
		}
	}

	#endregion

	#region[玩家跳跃HOOK]

	[HarmonyPatch(nameof(ENT_Player.Jump))]
	[HarmonyPrefix]
	public static bool Patch_Jump(ENT_Player __instance, ref bool canJumpBoost, ref float mult) {
		const string HOOK_NAME = "OnPlayerJump";
		// 使用 using 块接 离开括号作用域时会自动调用 Dispose 清理
		using (var guard = LuaHookGuard.Enter(HOOK_NAME)) {
			// 如果发生重入 直接放行本体并退出
			if (!guard.CanExecute) return true;
			try {
				// 触发事件
				ModEventBus.TriggerEvent(HOOK_NAME, canJumpBoost, mult);
				// 检查是否有修改/拦截型的 Hook
				if (!Plugin.safeLuaSandbox.Api.Hooks.Contains(HOOK_NAME)) return true;
				// 获取Lua返回数据
				var (canJumpBoostData, multData, isCancelled) =
					ModHookBus.InvokeHook<(bool, float,bool)>(HOOK_NAME, canJumpBoost, mult);
				// 本次跳跃取消
				if (isCancelled) return false;
				canJumpBoost = canJumpBoostData;
				mult = multData;
			} catch (Exception e) {
				Plugin.LogError($"[LuaInWK] HOOK异常 {HOOK_NAME}: {e.Message}");
			}
			return true;
		}
	}

	#endregion

}

[HarmonyPatch(typeof(ENT_Player.Hand))]
public static class Patch_Hand{

	#region[玩家抓握HOOK]

	[HarmonyPatch(nameof(ENT_Player.Hand.GrabHold))]
	[HarmonyPrefix]
	public static bool Patch_GrabHold(ENT_Player.Hand __instance, ref Transform target, ref Vector3 worldspacePos, ref Clickable.InteractionInfo info) {
		const string HOOK_NAME = "OnPlayerGrabHold";
		// 使用 using 块接 离开括号作用域时会自动调用 Dispose 清理
		using (var guard = LuaHookGuard.Enter(HOOK_NAME)) {
			// 如果发生重入 直接放行本体并退出
			if (!guard.CanExecute) return true;
			try {
				// 触发事件
				ModEventBus.TriggerEvent(HOOK_NAME, target.name, worldspacePos);
				// 检查是否有修改/拦截型的 Hook
				if (!Plugin.safeLuaSandbox.Api.Hooks.Contains(HOOK_NAME)) return true;
				// 获取Lua返回 新抓握坐标,是否抓握取消
				var (GrabPoint, isCancelled) = ModHookBus.InvokeHook<(Vector3, bool)>(
					HOOK_NAME, target.name, worldspacePos);
				// 本次抓握取消
				if (isCancelled) return false;
				worldspacePos = GrabPoint;
			} catch (Exception e) {
				Plugin.LogError($"[LuaInWK] HOOK异常 {HOOK_NAME}: {e.Message}");
			}
			return true;
		}
	}

	#endregion

}

[HarmonyPatch(typeof(EntityBuff))]
public static class Patch_EntityBuff {

	#region[玩家获得BUFF]

	[HarmonyPatch(nameof(EntityBuff.AddBuff))]
	[HarmonyPrefix]
	public static bool Patch_AddBuff(EntityBuff __instance, ref BuffContainer buffContainer) {
		const string HOOK_NAME = "OnPlayerAddBuff";
		// 不是玩家 直接返回
		if (__instance.gameEntity is not ENT_Player player)
			return true;
		// 使用 using 块接 离开括号作用域时会自动调用 Dispose 清理
		using (var guard = LuaHookGuard.Enter(HOOK_NAME)) {
			// 如果发生重入 直接放行本体并退出
			if (!guard.CanExecute)
				return true;
			try {
				// 触发事件
				ModEventBus.TriggerEvent(HOOK_NAME, buffContainer);
				// 检查是否有修改/拦截型的 Hook
				if (!Plugin.safeLuaSandbox.Api.Hooks.Contains(HOOK_NAME))
					return true;
				// 获取修改后buff 是否禁用
				var (buffContainerData, isDisabled) = ModHookBus.InvokeHook<(BuffContainerData, bool)>(
					HOOK_NAME, new BuffContainerData(buffContainer));
				// 本次buff不使用
				if (isDisabled)
					return false;
			} catch (Exception e) {
				Plugin.LogError($"[LuaInWK] HOOK异常 {HOOK_NAME}: {e.Message}");
			}
			return true;
		}
	}

	#endregion

}

#endregion