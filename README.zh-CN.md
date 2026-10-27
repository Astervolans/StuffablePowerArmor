# 可更换材质的动力装甲（Stuffable Power Armor）

[English](README.md) | 简体中文

RimWorld 1.7 模组：让帝国/机械师动力装甲可以像普通可制作物品一样选择材料。材料会实际
影响成品的属性，而使用玻璃钢制作时与原版完全一致。

## 支持的物品

| 套装 | 护甲 | 头盔 |
|---|---|---|
| 斥候装甲 | `Apparel_ArmorRecon` | `Apparel_ArmorHelmetRecon` |
| 海军装甲 | `Apparel_PowerArmor` | `Apparel_PowerArmorHelmet` |
| 骑士（圣甲）装甲 | `Apparel_ArmorCataphract` | `Apparel_ArmorHelmetCataphract` |
| 圣装斥候 | `Apparel_ArmorReconPrestige` | `Apparel_ArmorHelmetReconPrestige` |
| 圣装海军 | `Apparel_ArmorMarinePrestige` | `Apparel_ArmorMarineHelmetPrestige` |
| 圣装骑士 | `Apparel_ArmorCataphractPrestige` | `Apparel_ArmorHelmetCataphractPrestige` |
| 掷弹装甲 | `Apparel_ArmorMarineGrenadier` | — |
| 飞蝗装甲 | `Apparel_ArmorLocust` | — |
| 凤凰装甲 | `Apparel_ArmorCataphractPhoenix` | — |
| 机械领主装甲 | `Apparel_MechlordSuit` | `Apparel_ArmorHelmetMechlordHelmet` |
| 机械指挥官头盔 | — | `Apparel_ArmorHelmetMechCommander` |

## 功能

1. **用材料替代大部分玻璃钢。** 配方固定保留 **20 个玻璃钢** 作为结构材料，其余玻璃钢
   数量替换为 `原版玻璃钢数量 - 20` 的自选材料。其他材料（零部件、铀、金、化合燃料、
   特殊芯片）保持不变。因此选择玻璃钢时，成本与原版一致。
2. **玻璃钢 = 原版。** 使用玻璃钢时，护甲值、绝缘、最大耐久、制作工作量、质量、易燃性、
   美观度和市场价值都与原版一致。
3. **材料影响比例可在 20%–100% 调整（默认 100%）。** 材料差异以玻璃钢为基准按比例缩放：
   - 锐器/钝器/热能护甲值；
   - 冷暖绝缘；
   - 最大耐久；
   - 制作工作量；
   - 质量。

   美观度和市场价值仍然使用游戏原生的材料效果。物品属性中的 **“材料效果乘数”** 会显示
   实际应用的比例（例如 20% 或 100%）。
4. **耐久比例保持。** 最大耐久会随材料和比例变化。调整比例或使用清理按钮清除材料时，
   当前耐久会按“当前耐久 / 旧最大耐久”的比例换算到新的最大耐久，而不是保留旧绝对值：
   - 20% 铀掷弹装甲 303/333（91%）清理后约为 309/340（91%）；
   - 20% → 100% 后，303/333 会保持约 91% 的比例。
   存档会在 `<game>` 节点中记录 `<SPA_EffectFraction>`，加载时自动检查并按需换算。旧版本
   没有该节点的存档不会自动换算（用新版本保存一次即可记录）。
5. **材料类别可配置。** 金属始终启用；木头、织物与皮革、石砖可分别开启。类别修改在下次
   启动游戏时生效。
6. **中途加入。** 旧存档中没有材料记录的受支持护甲会按玻璃钢处理（`defaultStuff` 和
   `Thing.ExposeData` 前缀），不会报“缺少材料”的红字。
7. **中途移除。** 物品存档中只写入原版材料的 `<stuff>` 引用（如 `Steel`、`WoodLog`），
   不新增自定义 ThingDef 或 Comp；存档级 `<SPA_EffectFraction>` 只是一个普通浮点数。
   设置页面提供 **“清理所有受支持护甲的材料设置”** 按钮：
   - 扫描地图、容器、小人（穿戴/装备/背包/携带）、商队、世界小人和物品容器；
   - 清除所有受支持护甲的材料选择，并按比例保留当前耐久；
   - 再次扫描，清零后显示 **“可以移除mod，请直接保存并退出。”**
   - 在游戏内执行清理，然后保存并退出即可安全移除模组。
8. **英文 + 简体中文。** About、设置页和清理对话框均已本地化。

## 常见问题

**Q：这个 mod 是否支持存档中途加入或移除？**

A：是的。支持存档中途加入和移除；已有装甲默认视作玻璃钢材质。如果需要中途移除，点击
mod 设置中的“清理”按钮并保存即可安全移除。

**Q：这个 mod 支持 CE 吗？**

A：是的，支持 Combat Extended 环境（CE 不是必须的）。

**Q：这个 mod 兼容性如何？**

A：从原理上，本 mod 应该只会与修改了原版护甲相关配方或属性的 mod 冲突。我在常用的 mod
环境中未发现明显问题；如果遇到意料之外的冲突，欢迎反馈。

## 安装

- 需要 **RimWorld 1.7** 和 **Harmony**（packageId `brrainz.harmony`）。
- 将 `StuffablePowerArmor` 文件夹（或解压 zip）放入 `<RimWorld>/Mods/`。
- 在 Harmony 和 DLC 之后启用 **Stuffable Power Armor**。
- Royalty / Biotech 的内容仅在对应 DLC 启用时生效；没有 DLC 时 Core 护甲仍可使用。

## 设置与注意事项

- 修改材料影响比例或材料类别后，请**保存设置并重启游戏**。设置窗口不会实时重算物品
  属性：RimWorld 1.7 的 `DeleteStatCache()` 会把缓存字典置空，从滑条直接触碰缓存会刷出
  `NullReferenceException` 红字；因此本 mod 选择在重启后统一应用。
- 旧版本（1.0.0/1.0.1）存档没有材料系数节点。升级到 1.0.2 后，建议先用**原来的比例**
  进入存档并保存一次，再修改比例并重启；这样才能自动换算耐久比例。
- **Combat Extended：** 内置 CE 兼容补丁，会避免 CE 的 `ApparelAutoPatcher` 把护甲值
  替换成 2 mm；CE 环境下的护甲按毫米 RHA 计算，材料差异会缩放到 CE 的护甲量级。
  已在 CE 16.7.4.0 上测试。
- 易燃性对受支持护甲保持原版值，不随材料缩放；这是为了避免 1.7 世界生成中的原生统计
  异常。
- 石砖没有原生护甲加成，石制护甲几乎不会获得额外防护，这是预期行为。
- 模组只会在内存中修改原版 def，不会改写磁盘上的原版文件。

## 源码 / 构建

仓库内已包含可直接使用的 `Assemblies/StuffablePowerArmor.dll`。如需从源码构建，请准备
RimWorld 1.7 的引用程序集（游戏目录中的 `Assembly-CSharp.dll`、`UnityEngine.CoreModule.dll`
或等价的 `UnityStubs.dll`，以及 Harmony 的 `0Harmony.dll`），放在同一目录并通过
`RimWorldRefsDir` 传入：

```
dotnet build Source\StuffablePowerArmor.csproj -c Release /p:RimWorldRefsDir=C:\path\to\refs
```

默认值 `..\..\tools\refs` 仅用于本项目的开发工作区。

## 版本

1.0.2 — packageId `locals.stuffablepowerarmor`

## 许可证

MIT，详见 [LICENSE](LICENSE)。
