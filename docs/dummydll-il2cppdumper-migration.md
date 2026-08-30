# DummyDll 生成方案（Il2CppDumper 逻辑迁移）

- 日期：2026-08-28
- 已锁定：在本仓库现有 HSR 解析层上，**复用 Il2CppDumper 的 DummyDll 生成逻辑**（Mono.Cecil），不重建标准 `global-metadata.dat`，也不调用现成 `Il2CppDumper.exe`。若实际需要复用的 Il2CppDumper 代码较多，则将已修改、可作为 DLL 调用的 Il2CppDumper fork 以 Git submodule 嵌入本仓库，而不是继续复制大量源码。
- 本文是编码前的实施方案，不含实现。

---

## 1. 做法

Il2CppDumper 把「读官方 metadata」和「写 DummyDll」分成两层。HSR 的 metadata 是定制格式，前一层不能直接用；后一层（`DummyAssemblyGenerator`）只消费已经解开的类型/方法/字段图，可以通过适配层复用。

```
本仓库 MetadataCache / *Definition / Il2CppType
        ↓  按 DummyAssemblyGenerator 的访问方式对接
迁入的少量生成代码，或 submodule 中的 Il2CppDumper 生成层
        ↓  Mono.Cecil
DummyDll/*.dll（Il2CppDummyDll.dll + 各 image）
```

`dump.cs` / `stringliterals.json` 保持现有输出，DummyDll 是额外产物。

参考源码（已克隆）：

- 路径：`E:\dev\git\Il2CppDumper`
- 远程：`https://github.com/SwingCosmic/Il2CppDumper`
- 许可：MIT。迁入文件保留版权声明。本仓库 GPL-3.0 可以吸收 MIT。

和原版 `Il2CppDumper` 最新提交一致，额外调整了入口使其可以被当成 DLL 引用直接调用。编码前固定该 fork 的提交；若采用 submodule，建议放在 `extern/Il2CppDumper`，主仓库只保留 HSR 数据适配层和调用入口。

源码复用策略：

1. 先评估只迁入 `DummyAssemblyGenerator`、`DummyAssemblyExporter`、`MyAssemblyResolver` 及少量依赖是否足够。
2. 若为消除依赖而需要持续搬运 Il2CppDumper 的类型系统、执行器或其他大量代码，则改用 submodule，不在本仓库维护重复副本。
3. submodule 固定到已验证 commit，不跟随上游分支浮动；升级时单独验证 DummyDll 输出。
4. 无论采用哪种方式，HSR 定制 metadata 的读取和解密仍由本仓库负责，只把规范化后的数据交给生成层。

---

## 2. 现有材料

### 2.1 样本 `sample/HSR/`（已被 `.gitignore` 排除）

| 文件 | 大小 | 核对结果 |
|---|---|---|
| `GameAssembly.dll` | 536,139,040 | PE x64（`machine=0x8664`），`ImageBase=0x180000000`，与代码里写死的基址一致 |
| `StarRail_Data/il2cpp_data/Metadata/global-metadata.dat` | 100,310,060 | 文件头为 `MHY\0`（`4D 48 59 00`），不是 `0xFAB11BAF` |
| `StarRail_Data/il2cpp_data/Metadata/startup-metadata.dat` | 3,902,396 | 无标准 magic，供 image / generic class 表使用 |

本仓库入口已按「游戏目录」读取上述相对路径，样本可直接作为 `dotnet run sample/HSR` 的输入（已测试通过）。

未随样本提供版本号。解析器当前的 XOR/LCG 常量按 README 绑定 `OSPRODWin4.5.0`。调整后允许在启动参数中指定游戏/metadata 版本及可能随版本变化的 magic number；未提供时使用内置默认值，因此现有调用方式仍可运行。

### 2.2 Il2CppDumper 中要迁的文件

| 文件 | 角色 | 复用方式 |
|---|---|---|
| `Il2CppDumper/Utils/DummyAssemblyGenerator.cs` | DummyDll 主体 | 优先通过适配接口复用；若仅需该文件及少量依赖可逻辑迁入，否则从 submodule 引用 |
| `Il2CppDumper/Outputs/DummyAssemblyExporter.cs` | 写盘 | 少量迁入时改输出目录且不要 `SetCurrentDirectory`；submodule 方案则通过可调用入口传入输出目录 |
| `Il2CppDumper/Utils/MyAssemblyResolver.cs` | 程序集互引 | 随生成层一起迁入或从 submodule 引用 |
| `Il2CppDumper/Libraries/Il2CppDummyDll.dll` | `AddressAttribute` 等模板 | 嵌入本项目资源，或由 submodule 构建产物提供；启动时不得依赖开发机绝对路径 |
| `Il2CppDumper/Il2CppDumper.csproj` 中的 `Mono.Cecil 0.11.4` | 写 DLL | 本项目加 PackageReference |

少量迁入方案不迁：`Metadata.cs`、二进制 Registration 扫描、`Il2CppDecompiler`、`StructGenerator`、IDA/Ghidra 脚本、`CustomAttributeDataReader`（本仓库尚未解析 attribute blob）。采用 submodule 时这些代码可以存在于依赖仓库中，但不得进入本工具的执行链或成为 HSR 解析的前置条件。

### 2.3 本仓库已能提供给 Generator 的数据

| DummyAssemblyGenerator 用到的数据 | 本仓库来源 |
|---|---|
| image 名、typeStart、typeCount | `Il2CppImageDefinition`（startup-metadata） |
| 类型名、命名空间、flags、parent、interfaces、nested、generic | `Il2CppTypeDefinition` |
| 字段名、类型、offset、常量 | `Il2CppFieldDefinition` / `MetadataCache.FieldDefaultValues` |
| 方法名、flags、返回类型、参数、generic、method pointer | `Il2CppMethodDefinition` |
| 属性 get/set、事件 add/remove/raise | `Il2CppPropertyDefinition` / `Il2CppEventDefinition` |
| 泛型参数与约束 | `Il2CppGenericContainerDefinition` 等 |
| `Il2CppType` CLASS/VALUETYPE | `Data` 已是 TypeDef 下标 |
| GENERICINST | `startup-metadata` 8 字节（typeDefIndex + instIndex）+ `GenericInstsOffset` |
| ARRAY / SZARRAY / PTR | `Data` 仍是 VA，经 `PEHelper.RvaToOffset` |
| VAR / MVAR | `Data` 已是 generic parameter 下标 |

`DumpWriter` 已按同样数据写 `dump.cs`，可作为 DummyDll 的对照。

---

## 3. 迁移时要对齐的接口

`DummyAssemblyGenerator` 不直接读 dat，它通过下面这些调用拿数据。迁入时按左列换成右列，不要改 Cecil 的遍次（先建类型 → nested/generic/parent/interface → field/method/property/event）。

| Il2CppDumper | 本仓库 |
|---|---|
| `metadata.imageDefs` | `MetadataCache.Images` |
| `metadata.assemblyDefs[i].aname` | **无表**。程序集名用 `Path.GetFileNameWithoutExtension(image.Name)`，版本 `0.0.0.0` |
| `metadata.GetStringFromIndex` | 定义里已解密的 `Name` / `Namespace` |
| `metadata.typeDefs[i]` | `MetadataCache.TypeDefs[i]`（70 字节定制结构） |
| `metadata.nestedTypeIndices` / `interfaceIndices` | header 的 `NestedTypesOffset` / `InterfaceOffset` |
| `metadata.fieldDefs` / `methodDefs` / … | 现有 `Il2Cpp*Definition` 构造 |
| `il2Cpp.types[i]` | `Il2CppType.FromIndex(i)` |
| `executor.GetTypeDefinitionFromIl2CppType` | `(int)type.Data` 作为 TypeDef 下标 |
| `executor.GetGenericClassTypeDefinition` | startup-metadata 的 typeDefIndex |
| `executor.GetGenericParameteFromIl2CppType` | `(int)type.Data` 作为 generic param 下标 |
| `il2Cpp.GetFieldOffsetFromIndex` | `Il2CppFieldDefinition.Offset` |
| `il2Cpp.GetMethodPointer` / `GetRVA` | `MethodPointer`；RVA = VA − `0x180000000` |
| `executor.TryGetDefaultValue` | `Il2CppFieldDefinition.GetFieldStaticValue` |
| `il2CppType.byref` | **未解析**，见第 4 节 |
| `methodDef.token` / `iflags` / `slot`，`propertyDef.attrs` | 无。token 可按 RID 合成或第一阶段不写 `TokenAttribute`；iflags/attrs 用 0 |
| `CreateCustomAttribute`（v21+ 整段） | **第一阶段不迁**。`[Serializable]` 等已在 `TypeAttributes` 里 |

`GetTypeReference` 必须按 `DummyAssemblyGenerator` 那样建 Cecil 节点（`GenericInstanceType` / `ArrayType` / `PointerType` / `GenericParameter`），不能用 `Il2CppType.Name()` 字符串。分支可对照本仓库 `Il2CppType.ComputeName`，因为 HSR 的 `data` 含义与官方不同。

---

## 4. 编码前要补的解析缺口

只补 DummyDll 真正读到的字段，不去逆向剩余 header。

1. **`Il2CppType` 的 `bits`**  
   当前只读了 offset+0 `data`、+8 `attrs`、+10 `type`。Il2CppDumper 把后 4 字节当 `bits`，再拆 `byref` / `pinned` / `num_mods`。`GetTypeReferenceWithByRef` 依赖 `byref == 1`，不补则 `ref`/`out` 会丢。样本上先读第 11 字节，对照 Il2CppDumper `Il2CppType.Init`（v27.2 前后两种拆法）看哪种和 HSR 一致。

2. **`GetTypeReference` 图**  
   把 `ComputeName` 的 type 枚举改成返回 `TypeReference`。这是迁移工作量的主体，不是新的逆向。

3. **`Il2CppDummyDll.dll`**  
   从 `E:\dev\git\Il2CppDumper\Il2CppDumper\Libraries\Il2CppDummyDll.dll` 拷入并嵌入。Generator 启动时 `AssemblyDefinition.ReadAssembly` 读它，取出 `AddressAttribute`、`FieldOffsetAttribute` 等构造函数。

4. **Mono.Cecil 0.11.4**  
   写入本仓库 `HSRGlobalMetadata.csproj` 的 PackageReference（与 Il2CppDumper 同版本）。不要引用 `E:\DevTools` 里的散装 DLL。

5. **启动参数与版本化常量**  
   增加统一的解析选项对象，由命令行入口构造并传入 metadata、startup-metadata 和 PE 解析流程。至少支持显式指定游戏/metadata 版本；经验证可能随版本变化的 XOR key、LCG 参数、文件 magic、ImageBase、结构步长或表定位常量也通过该对象覆盖。所有选项均为可选，缺省时使用当前已验证样本对应的内置默认值。

   参数设计遵循以下约束：

   - 普通用户只需提供游戏目录；默认行为与当前版本兼容。
   - 版本号用于选择一组命名配置（例如 `OSPRODWin4.5.0`），单个 magic number 参数可在该配置之上覆盖，便于快速验证新版本。
   - 数值参数同时接受十进制和 `0x` 十六进制形式，并在启动时集中校验范围和冲突。
   - 启动日志打印最终生效的版本及关键常量，保证输出可复现；不得在解析代码各处直接读取命令行或散落新的硬编码。
   - 未知版本若未提供足够覆盖项，应给出明确诊断；不要静默套用错误结构后继续生成 DummyDll。

---

## 5. 仍缺 / 建议补的材料

已具备：HSR 三件套样本、Il2CppDumper 源码、DummyDll 模板 DLL、dnSpy/ILSpy（`E:\DevTools`）。

| 材料 | 状态 | 用途 |
|---|---|---|
| 样本游戏版本号 | **缺，但不阻塞默认路径** | 用于确认与默认的 `OSPRODWin4.5.0` 配置一致；也可在启动时显式指定版本和常量覆盖项 |
| Mono.Cecil 0.11.4 | 编码时加 NuGet | 写 DLL |
| 本工具在该样本上的 `dump.cs` | 可选，跑现有程序即可 | 与 DummyDll 抽查对照 |
| 若干 `.asset` / bundle（含 MonoBehaviour） | 可选，阶段 2 | 验证 AssetStudio 能否认 DummyDll |
| 内存：样本 GameAssembly 536MB + metadata 100MB | 机器需能一次载入 | 现有解析器是整文件读入 |

不需要：UnityCN `game.dat`、标准 `0xFAB11BAF` metadata、运行时注入 dump。Il2CppDumper fork 已有的 DLL 调用入口可以继续维护；若采用 submodule，只做生成层复用所需的最小修改。

---

## 6. 实施顺序（尚未开始编码）

1. 抽出统一解析选项；为版本号及可变 magic number 增加可选启动参数，并保留当前常量作为默认配置。
2. 评估 Il2CppDumper 生成层的实际依赖闭包：依赖较小时迁入少量文件；依赖明显扩散时，将已修改的 fork 以固定 commit 的 submodule 放入 `extern/Il2CppDumper`，并通过 DLL 调用入口对接。
3. csproj 加 Mono.Cecil 0.11.4；嵌入或从 submodule 构建产物取得 `Il2CppDummyDll.dll`；接入 `MyAssemblyResolver`、`DummyAssemblyExporter`。
4. 补 `Il2CppType` 的 `bits`/`byref`。
5. 按 `DummyAssemblyGenerator` 遍次实现适配：建类型 → nested/generic/parent/interface → member；`GetTypeReference` 走 Cecil 图。
6. 第一阶段跳过 custom attribute 整段；程序集名从 image 名合成；token 可选。
7. 分别用默认配置和显式参数对 `sample/HSR` 出 `DummyDll/`，确认结果一致；再用 dnSpy 打开 `mscorlib` 与游戏主程序集，对照 `dump.cs`。

验收：dnSpy 能按程序集浏览类型、字段偏移、方法 RVA；不要求 AssetStudio 第一阶段就通过。

---

## 7. 风险

- 样本版本若不是 4.5.0，默认 XOR/结构步长可能失效；应通过版本配置或启动参数覆盖，并在解析前校验 magic/范围，避免带错参数继续执行。
- 可配置 magic number 会增加错误组合。必须集中定义默认配置和覆盖优先级，并在日志中输出最终值，禁止调用链中同时存在硬编码与参数值两套来源。
- submodule 会增加克隆和构建复杂度。CI/文档需使用递归拉取或显式初始化，并固定 commit；主仓库适配层不能依赖 submodule 的内部不稳定 API。
- HSR 的 `Il2CppType.data` 混用下标与指针，必须沿用本仓库已有分支，不能按 Il2CppDumper 的 `MapVATR` 去解 CLASS/GENERICINST。
- GameAssembly 536MB，整文件加载峰值内存会很高。
- ImageBase 已用样本验证为 `0x180000000`，作为默认值保留，但允许由启动参数覆盖。
